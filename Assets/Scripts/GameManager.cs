using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using static AnswerSheet;

public class GameManager : NetworkBehaviour
{
    //========================================================================
    public static GameManager singleton;

    private bool m_serverLock = false;
    private int m_serverLockResponseCount;

    //========================================================================
    public static event System.Action QuestionCardsUpdated;
    public List<int> currentQuestionCards = new();
    public FixedString128Bytes[] possibleQuestionCards = {
        "one",
        "two",
        "three",
        "four",
        "five",
        "six",
        "seven"
    };

    //========================================================================
    public static event System.Action StartAnswering;
    public static event System.Action<AnswerSheet> NewPendingAnswerSheet;
    public static event System.Action NoMorePendingAnswerSheets;
    public List<AnswerSheet> answerSheets = new();

    //========================================================================
    public static event System.Action StartPresentation;
    public static event System.Action PresentNextSheet;
    public static event System.Action PresentationFinished;

    private int m_firstPresentedSheet;
    private int m_presentationSheetIndex;
    private int m_presentationAnswerIndex;

    //========================================================================
    public static event System.Action<int /*sheetIndex*/, int /*answerIndex*/> RevealAnswer;
    public static event System.Action LastSheetAnswerRevealed;

    public static event System.Action FavoriteAnswerConfirmed;

    public static event System.Action GuessConfirmed;
    public List<int> playerScores = new();


    //========================================================================
    private void Awake()
    {
        singleton = this;

        PlayerManager.PlayerAdded += AddPlayerScore;
    }

    public void SubscribeEventsForServer()
    {
        PlayerManager.PlayerAdded += AddQuestionCard;
        PlayerManager.PlayerAdded += CheckServerLockResponses;
    }

    //========================================================================
    /// <summary>
    /// <para>Implemented on functions that multiple clients can call on the server. 
    /// Prevents multiple calls to a server call.</para> 
    /// <para>Have the recievers call "RespondToServerLock" on the server for unlocking.</para>
    /// </summary>
    /// <param name="lockServer"></param>
    /// <returns>the previous lock state</returns>
    bool LockServer()
    {
        bool previousState = m_serverLock;
        m_serverLock = true;
        return previousState;
    }
    /// <summary>
    /// Responds to the server that a server locked function has finished execution.
    /// </summary>
    [Rpc(SendTo.Server)]
    void RespondToServerLock_ServerRpc()
    {
        m_serverLockResponseCount++;
        CheckServerLockResponses();
    }
    /// <summary>
    /// Checks if all players have responded yet. If so, it will unlock.
    /// </summary>
    void CheckServerLockResponses()
    {
        if (m_serverLockResponseCount >= PlayerManager.singleton.playerCount)
        {
            m_serverLock = false;
            m_serverLockResponseCount = 0;
        }
    }

    //========================================================================
    void AddQuestionCard()
    {
        //add the amount of question cards that a player can answer for others but not themself
        if (PlayerManager.singleton.playerCount == 1)
            return; //do not create a question card for the first player

        currentQuestionCards.Add(currentQuestionCards.Count);

        QuestionCardsUpdated?.Invoke();

        SyncCurrentQuestionCardsOnClients_Host();
    }
    public List<FixedString128Bytes> GetCurrentQuestionCards()
    {
        List<FixedString128Bytes> questionCards = new();
        foreach (int cardIndex in currentQuestionCards)
        {
            questionCards.Add(possibleQuestionCards[cardIndex]);
        }
        return questionCards;
    }
    public void ChangeQuestionCard_Host(int cardIndex)
    {
        if (++currentQuestionCards[cardIndex] >= possibleQuestionCards.Length)
        {
            currentQuestionCards[cardIndex] = 0;
        }

        QuestionCardsUpdated?.Invoke();

        SyncCurrentQuestionCardsOnClients_Host();
    }
    void ChangeQuestionCard_Client(int cardIndex, int card)
    {
        if (cardIndex == currentQuestionCards.Count)
            currentQuestionCards.Add(card);
        else
            currentQuestionCards[cardIndex] = card;
    }

    //========================================================================
    void SyncCurrentQuestionCardsOnClients_Host()
    {
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            SyncSingleQuestionCard_ClientRpc(i, currentQuestionCards[i]);
        }
        FinishedQuestionCardSync_ClientRpc();
    }
    [Rpc(SendTo.NotServer)]
    void SyncSingleQuestionCard_ClientRpc(int cardIndex, int card)
    {
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        ChangeQuestionCard_Client(cardIndex, card);
    }
    [Rpc(SendTo.NotServer)]
    void FinishedQuestionCardSync_ClientRpc()
    {
        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    public void SyncCurrentQuestionCards_TargetClient()
    {
        SyncCurrentQuestionCards_TargetRpc(RpcTarget.Owner);
    }
    [Rpc(SendTo.Server, AllowTargetOverride = true)]
    void SyncCurrentQuestionCards_TargetRpc(RpcParams targetRpc)
    {
        RpcParams target = RpcTarget.Single(targetRpc.Receive.SenderClientId, RpcTargetUse.Temp);
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            SyncSingleQuestionCard_TargetRpc(i, currentQuestionCards[i], target);
        }
        FinishedQuestionCardSync_TargetRpc(target);
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void SyncSingleQuestionCard_TargetRpc(int cardIndex, int card, RpcParams targetRPC)
    {
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        ChangeQuestionCard_Client(cardIndex, card);
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void FinishedQuestionCardSync_TargetRpc(RpcParams targetRPC)
    {
        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void StartAnswering_Rpc()
    {
        answerSheets.Clear();
        //answer sheet for each player
        for (int i = 0; i < PlayerManager.singleton.playerCount; i++)
        {
            answerSheets.Add(new AnswerSheet(currentQuestionCards.Count, i));
        }
        StartAnswering?.Invoke();

        //have this player take the answer sheet of the player before
        int firstAnswerSheetIndex = Player.owningPlayer.playerIndex - 1;
        if (firstAnswerSheetIndex < 0)
            firstAnswerSheetIndex = answerSheets.Count - 1; //last index

        NewPendingAnswerSheet?.Invoke(answerSheets[firstAnswerSheetIndex]);
    }

    //========================================================================
    public void SyncAllAnswerSheets_Host()
    {
        //send all the answer sheets to sync with the clients
        for (int i = 0; i < answerSheets.Count; i++)
        {
            for (int cardIndex = 0; cardIndex < answerSheets[i].cardAnswers.Count; cardIndex++)
            {
                SyncAnswerSheet_ClientRpc(
                    i,
                    cardIndex,
                    answerSheets[i].cardAnswers[cardIndex].answeringPlayerIndex,
                    answerSheets[i].cardAnswers[cardIndex].answerString);
            }
        }
    }
    public void SyncAnswerSheet(int answerSheetIndex, int cardIndex, FixedString128Bytes newAnswer)
    {
        SyncAnswerSheet_ServerRpc(
            answerSheetIndex, 
            cardIndex, 
            Player.owningPlayer.playerIndex, 
            newAnswer, 
            RpcTarget.Owner);

    }
    [Rpc(SendTo.Server, AllowTargetOverride = true)]
    void SyncAnswerSheet_ServerRpc(
        int answerSheetIndex, 
        int cardIndex, 
        int answeringPlayerIndex,
        FixedString128Bytes newAnswer,
        RpcParams targetRpc)
    {
        //apply modifications to the answer sheets
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answeringPlayerIndex = answeringPlayerIndex;
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answerString = newAnswer;

        if (IsAllAnswerSheetsFinished_Host())
        {
            AllAnswersFinished_Host();
            return;
        }

        
        int nextAnsweringPlayerIndex = PlayerManager.singleton.GetPlayerIndex(answeringPlayerIndex, 1);

        //will the next player recieving this answer sheet NOT also be the target player
        if (nextAnsweringPlayerIndex != answerSheetIndex)
        {
            //send answer sheet to next player
            if (nextAnsweringPlayerIndex == Player.owningPlayer.playerIndex)
            {
                NewPendingAnswerSheet?.Invoke(answerSheets[answerSheetIndex]);
            }
            else
            {
                RpcParams nextAnsweringClientRpc = PlayerManager.singleton.GetPlayerRpcParams(nextAnsweringPlayerIndex);

                for (int i = 0; i < answerSheets[answerSheetIndex].cardAnswers.Count; i++)
                {
                    SyncAnswer_TargetRpc(
                        answerSheetIndex,
                        i,
                        answerSheets[answerSheetIndex].cardAnswers[i].answeringPlayerIndex,
                        answerSheets[answerSheetIndex].cardAnswers[i].answerString,
                        nextAnsweringClientRpc);
                }
                FinishedAddingPendingAnswerSheet_TargetRpc(answerSheetIndex, nextAnsweringClientRpc);
            }
        }
        else
        {
            RpcParams target = RpcTarget.Single(targetRpc.Receive.SenderClientId, RpcTargetUse.Temp);
            NoMorePendingAnswerSheets_TargetRpc(target);
        }
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void SyncAnswer_TargetRpc(
        int answerSheetIndex,
        int cardIndex,
        int answeringPlayerIndex,
        FixedString128Bytes newAnswer,
        RpcParams targetRpc)
    {
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answeringPlayerIndex = answeringPlayerIndex;
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answerString = newAnswer;
    }
    [Rpc(SendTo.NotServer)]
    void SyncAnswerSheet_ClientRpc(
        int answerSheetIndex,
        int cardIndex,
        int answeringPlayerIndex,
        FixedString128Bytes newAnswer)
    {
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answeringPlayerIndex = answeringPlayerIndex;
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answerString = newAnswer;
    }

    //========================================================================
    [Rpc(SendTo.SpecifiedInParams)]
    void FinishedAddingPendingAnswerSheet_TargetRpc(
        int answerSheetIndex,
        RpcParams targetRpc)
    {
        NewPendingAnswerSheet?.Invoke(answerSheets[answerSheetIndex]);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void NoMorePendingAnswerSheets_TargetRpc(RpcParams targetRpc)
    {
        NoMorePendingAnswerSheets?.Invoke();
    }

    //========================================================================
    bool IsAllAnswerSheetsFinished_Host()
    {
        foreach (AnswerSheet sheet in answerSheets)
        {
            foreach (CardAnswer answer in sheet.cardAnswers)
            {
                if (answer.answeringPlayerIndex == -1)
                    return false; //this answer hasn't been answered yet
            }
        }
        return true; //all answers have been answered
    }
    //the last player answering a question has finished, presentation will now begin
    void AllAnswersFinished_Host()
    {
        SyncAllAnswerSheets_Host();

        //start presentation
        int randomPlayer = Random.Range(0, PlayerManager.singleton.playerCount);
        StartPresentation_Rpc(0);
    }

    //========================================================================
    //the last player answering a question has finished, presentation will now begin
    [Rpc(SendTo.Everyone)]
    void StartPresentation_Rpc(int startPlayerIndex)
    {
        m_firstPresentedSheet = startPlayerIndex;
        m_presentationSheetIndex = startPlayerIndex;
        m_presentationAnswerIndex = 0;

        StartPresentation?.Invoke();
    }
    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void RevealAnswer_Rpc()
    {
        RevealAnswer?.Invoke(m_presentationSheetIndex, m_presentationAnswerIndex);

        m_presentationAnswerIndex++;
        if (m_presentationAnswerIndex >= answerSheets[m_presentationSheetIndex].cardAnswers.Count)
        {
            m_presentationAnswerIndex = 0;

            LastSheetAnswerRevealed?.Invoke();
        }
    }
    [Rpc(SendTo.Everyone)]
    public void ConfirmFavoriteAnswer_Rpc(int favoritedAnswerIndex)
    {
        answerSheets[m_presentationSheetIndex].favoriteAnswerIndex = favoritedAnswerIndex;
        playerScores[answerSheets[m_presentationSheetIndex].GetFavoritedPlayerIndex()]++;
        
        FavoriteAnswerConfirmed?.Invoke();
    }
    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void ConfirmPlayerGuess_Rpc(int playerGuess)
    {
        answerSheets[m_presentationSheetIndex].guessedPlayerIndex = playerGuess;

        //add points to targeted player (guesser) if they guessed correctly
        if (playerGuess == answerSheets[m_presentationSheetIndex].GetFavoritedPlayerIndex())
        {
            playerScores[m_presentationSheetIndex]++;
        }

        GuessConfirmed?.Invoke();
    }
    //========================================================================
    void AddPlayerScore()
    {
        playerScores.Add(0);
    }
    [Rpc(SendTo.Server)]
    public void ConfirmScoreBoard_ServerRpc()
    {
        if (LockServer())
            return;

        ConfirmScoreBoard_Rpc();
    }
    [Rpc(SendTo.Everyone)]
    void ConfirmScoreBoard_Rpc()
    {
        RespondToServerLock_ServerRpc();

        //present the next sheet
        m_presentationSheetIndex++;
        if (m_presentationSheetIndex >= answerSheets.Count)
            m_presentationSheetIndex = 0;

        if (m_presentationSheetIndex != m_firstPresentedSheet) //the next sheet is a new sheet?
        {
            PresentNextSheet?.Invoke();
        }
        else //all sheets have been presented
        {
            PresentationFinished?.Invoke();
        }
    }
    //========================================================================
    public int GetPresentingSheetIndex() => m_presentationSheetIndex;
    public bool IsLastPresentingSheet() => m_presentationSheetIndex == answerSheets.Count - 1;
    public string GetPresentingTargetPlayerName() => PlayerManager.singleton.GetPlayerName(m_presentationSheetIndex).ToString();
    public string GetGuessedPlayerName() => PlayerManager.singleton.GetPlayerName(answerSheets[m_presentationSheetIndex].guessedPlayerIndex).ToString();
    public string GetFavoritedPlayerName() => PlayerManager.singleton.GetPlayerName(answerSheets[m_presentationSheetIndex].GetFavoritedPlayerIndex()).ToString();
    public bool IsGuessCorrect() => answerSheets[m_presentationSheetIndex].IsGuessCorrect();

    //========================================================================
}

[System.Serializable]
public class AnswerSheet
{
    [System.Serializable]
    public class CardAnswer
    {
        public int answeringPlayerIndex = -1;
        public FixedString128Bytes answerString;
    }

    public int targetPlayerIndex;
    public int favoriteAnswerIndex = -1;
    public int guessedPlayerIndex = -1;
    public List<CardAnswer> cardAnswers = new();

    public AnswerSheet(int cardCount, int targetPlayerIndex)
    {
        this.targetPlayerIndex = targetPlayerIndex;
        for (int i = 0; i < cardCount; i++)
        {
            cardAnswers.Add(new CardAnswer());
        }
    }
    public void Reset()
    {
        favoriteAnswerIndex = -1;
        guessedPlayerIndex = -1;
        for (int i = 0; i < cardAnswers.Count; i++)
        {
            cardAnswers[i].answeringPlayerIndex = -1;
        }
    }

    public int GetFavoritedPlayerIndex() => cardAnswers[favoriteAnswerIndex].answeringPlayerIndex;
    public bool IsGuessCorrect() => guessedPlayerIndex == GetFavoritedPlayerIndex();
}

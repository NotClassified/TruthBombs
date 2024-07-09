using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using static AnswerSheet;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{
    //========================================================================
    public static GameManager singleton;
    static int m_gameVersion;

    public static bool signedIn;
    public string currentJoinCode;
    public int maxPlayers;

    public System.Action<int, int> IncompatiblePlayerVersion;
    event System.Action<Player> m_Spawned;
    event System.Action<Player> m_Spawned_GameState;

    //========================================================================
    public event System.Action SyncedGameState;

    public bool disconnectedDueToOnGoingGame;
    public bool isPlayingGame;
    System.Action m_PlayGameCallback;
    System.Action m_StopGameCallback;

    //========================================================================
    private bool m_serverLock = false;
    private int m_serverLockTargetResponseCount;
    private int m_serverLockResponseCount;

    //========================================================================
    public event System.Action PlayerNameConfirmed;
    public int confirmedPlayerNameCount;

    //========================================================================
    public event System.Action QuestionCardsUpdated;
    public List<int> currentQuestionCards = new();
    public List<FixedString128Bytes> currentCustomQuestions = new();

    public event System.Action<int /*editingPlayerIndex*/> NewCustomEditingPlayer;
    public bool customEditModeInUse = false;

    //========================================================================
    public event System.Action StartAnswering;
    public event System.Action<AnswerSheet> NewPendingAnswerSheet;
    public event System.Action NoMorePendingAnswerSheets;
    public List<AnswerSheet> answerSheets = new();

    //========================================================================
    public event System.Action StartPresentation;
    public event System.Action PresentNextSheet;

    private int m_firstPresentedSheet;
    private int m_presentationSheetIndex;
    private int m_presentationAnswerIndex;

    //========================================================================
    public event System.Action<int /*sheetIndex*/, int /*answerIndex*/> RevealAnswer;
    public event System.Action LastSheetAnswerRevealed;

    public event System.Action FavoriteAnswerConfirmed;

    public event System.Action GuessConfirmed;
    public event System.Action<int> GuesserSelectedPlayer;

    //========================================================================
    public event System.Action WinOrTieScreen;
    public event System.Action StartNewGame;
    public event System.Action StartTieBreaker;
    public event System.Action TieDataSynced;
    public event System.Action<int /*questionIndex*/> NewTieQuestion;
    public bool isTieDataSynced;

    public List<int> playerScores = new();
    public int playerScoreMax;

    public int playerWinIndex = -1;
    public TieData currentTieData;
    public bool playersTied;
    public bool allPlayersTied;

    //========================================================================
    public event System.Action<int /*tieIndex*/> TieAnswerConfirmed;
    public event System.Action AllTieAnswersConfirmed;

    int m_tieAnswerCount;
    int m_voteCount;

    //========================================================================
    private void Awake()
    {
        singleton = this;

        Player.OwnerSpawned += CheckGameState;

        m_gameVersion = int.Parse(Application.version);

        m_PlayGameCallback = () => { isPlayingGame = true; };
        StartAnswering += m_PlayGameCallback;
        m_StopGameCallback = () => { isPlayingGame = false; };
        StartNewGame += m_StopGameCallback;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        Player.OwnerSpawned -= CheckGameState;
        StartAnswering -= m_PlayGameCallback;
        StartNewGame -= m_StopGameCallback;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m_Spawned?.Invoke(Player.owningPlayer);

        if (!IsServer)
            SyncMaxPlayerCount_ServerRpc();
    }

    public void SubscribeEventsForServer()
    {
        PlayerManager.singleton.PlayerAdded += CheckVersionCompatibility;
        PlayerManager.singleton.PlayerAdded += (Player _) => { AddQuestionCard(); };
        PlayerManager.singleton.PlayerAdded += (Player _) => { CheckServerLockResponses(); };
    }

    //========================================================================
    /// <summary>
    /// <para>Implemented on functions that multiple clients can call on the server. 
    /// Prevents multiple calls to a server call.</para> 
    /// <para>Have the recievers call "RespondToServerLock" on the server for unlocking.</para>
    /// </summary>
    /// <param name="lockServer"></param>
    /// <returns>the previous lock state</returns>
    bool LockServer(int targetResponses)
    {
        if (m_serverLock)
            return true; //already locked

        m_serverLockTargetResponseCount = targetResponses;

        m_serverLock = true;
        return false;
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
        if (m_serverLockResponseCount >= m_serverLockTargetResponseCount)
        {
            m_serverLock = false;
            m_serverLockResponseCount = 0;
            m_serverLockTargetResponseCount = 0;
        }
    }

    //========================================================================
    void CheckVersionCompatibility(Player player)
    {
        if (player != Player.owningPlayer)
            return;

        if (!IsSpawned)
        {
            m_Spawned += CheckVersionCompatibility;
            return;
        }
        m_Spawned -= CheckVersionCompatibility;

        if (!IsServer)
            CheckVersionCompatibility_ServerRpc(Player.owningPlayer.playerIndex, m_gameVersion);
    }
    [Rpc(SendTo.Server)]
    void CheckVersionCompatibility_ServerRpc(int ownerIndex, int version)
    {
        if (version != m_gameVersion)
        {
            if (version == 0)
                IncompatiblePlayerVersion?.Invoke(PlayerManager.singleton.playerCount, version);

            IncompatiblePlayerVersion?.Invoke(ownerIndex, version);
        }
    }

    [Rpc(SendTo.Server)]
    void SyncMaxPlayerCount_ServerRpc()
    {
        SyncMaxPlayerCount_ClientRpc(maxPlayers);
    }
    [Rpc(SendTo.NotServer)]
    void SyncMaxPlayerCount_ClientRpc(int maxPlayerCount)
    {
        maxPlayers = maxPlayerCount;
    }

    void CheckGameState()
    {
        if (!IsSpawned)
        {
            m_Spawned_GameState = (Player _) => { CheckGameState(); };
            m_Spawned += m_Spawned_GameState;
            return;
        }
        m_Spawned -= m_Spawned_GameState;

        if (!IsServer)
            CheckGameState_ServerRpc();
    }
    [Rpc(SendTo.Server)]
    public void CheckGameState_ServerRpc()
    {
        SyncGameState_ClientRpc(isPlayingGame);
    }
    [Rpc(SendTo.NotServer)]
    public void SyncGameState_ClientRpc(bool playingGame)
    {
        this.isPlayingGame = playingGame;
        SyncedGameState?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void DisconnectClient_ServerRpc(ulong clientID)
    {
        NetworkManager.Singleton.DisconnectClient(clientID);
    }

    //========================================================================
    [Rpc(SendTo.Server)]
    public void ChangePlayerName_ServerRpc(int playerIndex, FixedString32Bytes newName)
    {
        PlayerManager.singleton.allPlayers[playerIndex].playerName = newName;
        confirmedPlayerNameCount++;
        PlayerNameConfirmed?.Invoke();
        SyncAllPlayerNames_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    void SyncAllPlayerNames_ServerRpc()
    {
        for (int i = 0; i < PlayerManager.singleton.allPlayers.Count; i++)
        {
            SyncPlayerName_ClientRpc(i, PlayerManager.singleton.allPlayers[i].playerName);
        }
        FinishedPlayerNameSync_ClientRpc();
    }
    [Rpc(SendTo.NotServer)]
    void SyncPlayerName_ClientRpc(int playerIndex, FixedString32Bytes playerName)
    {
        PlayerManager.singleton.allPlayers[playerIndex].playerName = playerName;
    }
    [Rpc(SendTo.NotServer)]
    void FinishedPlayerNameSync_ClientRpc()
    {
        PlayerNameConfirmed?.Invoke();
    }

    //========================================================================
    void AddQuestionCard()
    {
        //add the amount of question cards that a player can answer for others but not themself
        if (PlayerManager.singleton.playerCount == 1)
            return; //do not create a question card for the first player

        currentQuestionCards.Add(DataManager.singleton.GetRandomQuestion(currentQuestionCards));
        currentCustomQuestions.Add("");

        QuestionCardsUpdated?.Invoke();

        SyncCurrentQuestionCards_ServerRpc();
    }
    public List<FixedString128Bytes> GetCurrentQuestionCards()
    {
        List<FixedString128Bytes> questionCards = new();
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            if (currentQuestionCards[i] != -1)
                questionCards.Add(DataManager.singleton.GetQuestion(currentQuestionCards[i]));
            else
                questionCards.Add(currentCustomQuestions[i]);
        }
        return questionCards;
    }

    //========================================================================
    [Rpc(SendTo.Server)]
    public void RequestCustomQuestionEditMode_ServerRpc(int requestingPlayerIndex)
    {
        if (customEditModeInUse)
            return;

        if (LockServer(PlayerManager.singleton.playerCount))
            return;

        SyncCustomEditModeUse_Rpc(requestingPlayerIndex);
    }
    [Rpc(SendTo.Everyone)]
    void SyncCustomEditModeUse_Rpc(int editingPlayerIndex)
    {
        customEditModeInUse = true;
        NewCustomEditingPlayer?.Invoke(editingPlayerIndex);
        RespondToServerLock_ServerRpc();
    }
    [Rpc(SendTo.Everyone)]
    public void CancelCustomQuestionEdit_Rpc()
    {
        customEditModeInUse = false;
        QuestionCardsUpdated?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void SetCustomQuestionCard_ServerRpc(int cardIndex, FixedString128Bytes customQuestion)
    {
        currentQuestionCards[cardIndex] = -1;
        currentCustomQuestions[cardIndex] = customQuestion;

        SyncCurrentQuestionCards_ServerRpc();
    }
    [Rpc(SendTo.Server)]
    public void ChangeQuestionCard_ServerRpc(int cardIndex)
    {
        if (customEditModeInUse)
            return;

        if (LockServer(PlayerManager.singleton.playerCount))
            return;

        currentQuestionCards[cardIndex] = DataManager.singleton.GetRandomQuestion(currentQuestionCards);
        currentCustomQuestions[cardIndex] = "";

        SyncCurrentQuestionCards_ServerRpc();
    }
    //========================================================================
    [Rpc(SendTo.Server)]
    public void SyncCurrentQuestionCards_ServerRpc()
    {
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            if (currentQuestionCards[i] != -1)
                SyncSingleQuestionCard_ClientRpc(i, currentQuestionCards[i]);
            else
                SyncSingleCustomQuestionCard_ClientRpc(i, currentCustomQuestions[i]);
        }
        FinishedQuestionCardSync_ClientRpc();
    }
    [Rpc(SendTo.NotServer)]
    void SyncSingleQuestionCard_ClientRpc(int cardIndex, int card)
    {
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        if (cardIndex == currentQuestionCards.Count)
        {
            currentQuestionCards.Add(card);
            currentCustomQuestions.Add("");
        }
        else
        {
            currentQuestionCards[cardIndex] = card;
            currentCustomQuestions[cardIndex] = "";
        }
    }
    [Rpc(SendTo.NotServer)]
    void SyncSingleCustomQuestionCard_ClientRpc(int cardIndex, FixedString128Bytes card)
    {
        if (cardIndex > currentCustomQuestions.Count)
            Debug.LogError("question cards not updating in order");

        if (cardIndex == currentQuestionCards.Count)
        {
            currentQuestionCards.Add(-1);
            currentCustomQuestions.Add(card);
        }
        else
        {
            currentQuestionCards[cardIndex] = -1;
            currentCustomQuestions[cardIndex] = card;
        }
    }
    [Rpc(SendTo.Everyone)]
    void FinishedQuestionCardSync_ClientRpc()
    {
        customEditModeInUse = false;
        QuestionCardsUpdated?.Invoke();
        RespondToServerLock_ServerRpc();
    }

    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void StartAnswering_Rpc()
    {
        playerScoreMax = PlayerManager.singleton.playerCount;
        playerScores.Clear();
        answerSheets.Clear();
        //answer sheet and score for each player
        for (int i = 0; i < PlayerManager.singleton.playerCount; i++)
        {
            playerScores.Add(0);
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

        int nextAnsweringPlayerIndex = PlayerManager.GetPlayerIndex(answeringPlayerIndex, 1);

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
        StartPresentation_Rpc(randomPlayer);
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

        int favoritedPlayerIndex = answerSheets[m_presentationSheetIndex].GetFavoritedPlayerIndex();
        if (playerScores[favoritedPlayerIndex] < playerScoreMax)
            playerScores[favoritedPlayerIndex]++;
        
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
    [Rpc(SendTo.Everyone)]
    public void GuesserSelectPlayer_Rpc(int playerIndex)
    {
        GuesserSelectedPlayer?.Invoke(playerIndex);
    }
    //========================================================================
    [Rpc(SendTo.Server)]
    public void ConfirmScoreBoard_ServerRpc()
    {
        if (LockServer(PlayerManager.singleton.playerCount))
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
            CalculateScores();
        }
    }
    //========================================================================
    void CalculateScores()
    {
        int winIndex = 0;
        int highestScore = playerScores[0];

        List<int> playerTieIndexes = new() { 0 };
        playersTied = false;
        allPlayersTied = false;

        for (int i = 1; i < playerScores.Count; i++)
        {
            if (playerScores[i] == highestScore)
            {
                playerTieIndexes.Add(i);
                playersTied = true;
            }
            else if (playerScores[i] > highestScore)
            {
                winIndex = i;
                highestScore = playerScores[i];

                playerTieIndexes.Clear();
                playerTieIndexes.Add(i);
                playersTied = false;
            }
        }

        if (playersTied)
        {
            isTieDataSynced = false;
            currentTieData = new();
            m_voteCount = 0;

            //check if all players tied, if so, it will go to win screen instead of a tie breaker
            if (playerTieIndexes.Count == PlayerManager.singleton.playerCount)
            {
                allPlayersTied = true;
            }
            else if (IsServer)
            {
                currentTieData = new TieData(playerTieIndexes, DataManager.singleton.GetRandomQuestion(currentQuestionCards));
                SyncTieQuestion_ClientRpc(currentTieData.questionIndex);
                foreach (TieData.TiePlayer tiePlayer in currentTieData.tiePlayers)
                {
                    SyncTiePlayer_ClientRpc(tiePlayer.playerIndex);
                }
                FinishedTieDataSync_Rpc();
            }
        }
        else
        {
            playerWinIndex = winIndex;
        }
        WinOrTieScreen?.Invoke();
    }
    [Rpc(SendTo.NotServer)]
    void SyncTieQuestion_ClientRpc(int questionIndex)
    {
        currentTieData.questionIndex = questionIndex;
    }
    [Rpc(SendTo.NotServer)]
    void SyncTiePlayer_ClientRpc(int playerIndex)
    {
        currentTieData.tiePlayers.Add(new TieData.TiePlayer(playerIndex));
    }
    [Rpc(SendTo.Everyone)]
    void FinishedTieDataSync_Rpc()
    {
        isTieDataSynced = true;
        TieDataSynced?.Invoke();
    }
    [Rpc(SendTo.Server)]
    public void ChangeTieQuestion_ServerRpc()
    {
        if (LockServer(PlayerManager.singleton.playerCount))
            return;

        int newQuestionIndex = DataManager.singleton.GetRandomQuestion(currentQuestionCards);
        SyncTieQuestion_Rpc(newQuestionIndex);
    }
    [Rpc(SendTo.Everyone)]
    void SyncTieQuestion_Rpc(int newQuestionIndex)
    {
        RespondToServerLock_ServerRpc();

        NewTieQuestion?.Invoke(newQuestionIndex);
    }

    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void ConfirmVote_Rpc(int tieIndex)
    {
        currentTieData.tiePlayers[tieIndex].votes++;

        m_voteCount++;
        if (m_voteCount >= PlayerManager.singleton.playerCount - currentTieData.tiePlayers.Count)
        {
            CalculateVotes();
        }
    }
    [Rpc(SendTo.Everyone)]
    public void ConfirmTieAnswer_Rpc(int playerIndex, FixedString128Bytes newAnswer)
    {
        int tieIndex = currentTieData.GetTieIndex(playerIndex);
        currentTieData.tiePlayers[tieIndex].answer = newAnswer;
        TieAnswerConfirmed?.Invoke(tieIndex);

        m_tieAnswerCount++;
        if (m_tieAnswerCount >= currentTieData.tiePlayers.Count)
        {
            AllTieAnswersConfirmed?.Invoke();
        }
    }
    void CalculateVotes()
    {
        int winIndex = 0;
        int highestVote = currentTieData.tiePlayers[0].votes;

        List<int> playerTieIndexes = new() { 0 };
        playersTied = false;
        allPlayersTied = false;

        for (int i = 1; i < currentTieData.tiePlayers.Count; i++)
        {
            if (currentTieData.tiePlayers[i].votes == highestVote)
            {
                playerTieIndexes.Add(i);
                playersTied = true;
            }
            else if (currentTieData.tiePlayers[i].votes > highestVote)
            {
                winIndex = i;
                highestVote = currentTieData.tiePlayers[i].votes;

                playerTieIndexes.Clear();
                playersTied = false;
            }
        }

        if (playersTied)
        {
            isTieDataSynced = false;
            currentTieData = new();
            m_voteCount = 0;

            //check if all players tied, if so, it will go to win screen instead of a tie breaker
            if (playerTieIndexes.Count == PlayerManager.singleton.playerCount)
            {
                allPlayersTied = true;
            }
            else if (IsServer)
            {
                //initialize tie data
                int randomQuestion = DataManager.singleton.GetRandomQuestion(currentQuestionCards, allowTokens: false);
                currentTieData = new TieData(playerTieIndexes, randomQuestion);
                //sync tie data
                SyncTieQuestion_ClientRpc(currentTieData.questionIndex);
                foreach (TieData.TiePlayer tiePlayer in currentTieData.tiePlayers)
                {
                    SyncTiePlayer_ClientRpc(tiePlayer.playerIndex);
                }
                FinishedTieDataSync_Rpc();
            }
        }
        else
        {
            playerWinIndex = currentTieData.tiePlayers[winIndex].playerIndex;
        }
        WinOrTieScreen?.Invoke();
    }
    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void StartNewGame_Rpc()
    {
        playerScores.Clear();
        for (int i = 0; i < PlayerManager.singleton.playerCount; i++)
        {
            playerScores.Add(0);
        }

        StartNewGame?.Invoke();
    }
    [Rpc(SendTo.Everyone)]
    public void StartTieBreaker_Rpc()
    {
        StartTieBreaker?.Invoke();
    }

    //========================================================================
    /// <returns>The presenting sheet which is also the presenting target player</returns>
    public static int GetPresentingSheetIndex() => singleton.m_presentationSheetIndex;
    public static bool IsLastPresentingSheet() => singleton.m_presentationSheetIndex == singleton.answerSheets.Count - 1;
    public static string GetPresentingTargetPlayerName() => PlayerManager.GetPlayerName(singleton.m_presentationSheetIndex).ToString();
    public static string GetGuessedPlayerName() => PlayerManager.GetPlayerName(singleton.answerSheets[singleton.m_presentationSheetIndex].guessedPlayerIndex).ToString();
    public static string GetFavoritedPlayerName() => PlayerManager.GetPlayerName(singleton.answerSheets[singleton.m_presentationSheetIndex].GetFavoritedPlayerIndex()).ToString();
    public static bool IsGuessCorrect() => singleton.answerSheets[singleton.m_presentationSheetIndex].IsGuessCorrect();

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

[System.Serializable]
public class TieData
{
    [System.Serializable]
    public class TiePlayer
    {
        public int playerIndex;
        public int votes = 0;
        public FixedString128Bytes answer;

        public TiePlayer(int index)
        {
            playerIndex = index;
        }
    }
    public List<TiePlayer> tiePlayers = new();
    public int questionIndex;

    public TieData()
    {
        questionIndex = -1;
    }
    public TieData(List<int> playerIndexes, int question)
    {
        foreach (int index in playerIndexes)
        {
            tiePlayers.Add(new TiePlayer(index));
        }
        questionIndex = question;
    }

    public int GetTieIndex(int findPlayerIndex)
    {
        for (int i = 0; i < tiePlayers.Count; i++)
        {
            if (tiePlayers[i].playerIndex == findPlayerIndex)
            {
                return i;
            }
        }
        Debug.LogError("could not find player index " + findPlayerIndex + " in TieData");
        return 0;
    }

    public string GetPlayerName(int tieIndex) => PlayerManager.GetPlayerName(tiePlayers[tieIndex].playerIndex).ToString();
}
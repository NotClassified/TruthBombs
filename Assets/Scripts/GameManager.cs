using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using static AnswerSheet;
using static UnityEngine.GraphicsBuffer;

public class GameManager : NetworkBehaviour
{
    //========================================================================
    public static GameManager singleton;

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
    public static event System.Action<AnswerSheet> NewPendingAnswerSheet;
    public static event System.Action NoMorePendingAnswerSheets;
    public List<AnswerSheet> answerSheets = new();

    //========================================================================
    public static event System.Action AllPlayersFinishedAnswering;

    //========================================================================
    private void Awake()
    {
        singleton = this;
    }

    public void SubscribeEventsForServer()
    {
        PlayerManager.PlayerAdded += AddQuestionCard;
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
        //answer sheet for each player
        for (int i = 0; i < PlayerManager.singleton.playerCount; i++)
        {
            answerSheets.Add(new AnswerSheet(currentQuestionCards.Count, i));
        }

        UIManager.singleton.ChangeUIState<State_AnswerSheet>();

        //have this player take the answer sheet of the player before
        int firstAnswerSheetIndex = Player.owningPlayer.playerIndex - 1;
        if (firstAnswerSheetIndex < 0)
            firstAnswerSheetIndex = answerSheets.Count - 1; //last index

        NewPendingAnswerSheet?.Invoke(answerSheets[firstAnswerSheetIndex]);
    }

    //========================================================================
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
        print("SyncAnswerSheet_ServerRpc");
        //apply modifications to the answer sheets
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answeringPlayerIndex = answeringPlayerIndex;
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answerString = newAnswer;

        if (IsAllAnswerSheetsFinished_Host())
        {
            AllAnswersFinished_Rpc();
            return;
        }

        int nextAnsweringPlayerIndex = answeringPlayerIndex + 1;
        if (nextAnsweringPlayerIndex >= answerSheets.Count)
            nextAnsweringPlayerIndex = 0;

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
        print("SyncAnswer_TargetRpc");
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answeringPlayerIndex = answeringPlayerIndex;
        answerSheets[answerSheetIndex].cardAnswers[cardIndex].answerString = newAnswer;
    }

    //========================================================================
    [Rpc(SendTo.SpecifiedInParams)]
    void FinishedAddingPendingAnswerSheet_TargetRpc(
        int answerSheetIndex,
        RpcParams targetRpc)
    {
        print("FinishedAddingPendingAnswerSheet_TargetRpc");
        NewPendingAnswerSheet?.Invoke(answerSheets[answerSheetIndex]);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void NoMorePendingAnswerSheets_TargetRpc(RpcParams targetRpc)
    {
        print("NoMorePendingAnswerSheets_TargetRpc");
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
    [Rpc(SendTo.Everyone)]
    void AllAnswersFinished_Rpc()
    {
        AllPlayersFinishedAnswering?.Invoke();
    }
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
    public List<CardAnswer> cardAnswers = new();

    public AnswerSheet(int cardCount, int targetPlayerIndex)
    {
        this.targetPlayerIndex = targetPlayerIndex;
        for (int i = 0; i < cardCount; i++)
        {
            cardAnswers.Add(new CardAnswer());
        }
    }
}

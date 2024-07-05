using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using System;

namespace UIState
{
    public class TieBreaker : StateBase
    {
        //========================================================================
        public Button questionCard;
        System.Action<int> m_removeQuestionCardListeners;

        public GameObject playerAnswerCardsParent;
        public Transform playerAnswerCardsContent;
        public GameObject playerAnswerCardPrefab;
        List<Transform> m_playerAnswerCards = new();

        int m_selectedCardIndex;

        public TMP_InputField answerInput;
        string m_currentAnswerInput;

        //========================================================================
        public TextMeshProUGUI headerMessageText;
        public Button actionButton;
        bool m_confirming;

        //========================================================================
        private void Awake()
        {
            GameManager.NewTieQuestion += SetQuestionCard;

            m_removeQuestionCardListeners = (int _) => { questionCard.onClick.RemoveAllListeners(); };
        }
        private void OnDestroy()
        {
            GameManager.NewTieQuestion -= SetQuestionCard;
            GameManager.AllTieAnswersConfirmed -= AllowVoting;
            GameManager.AllTieAnswersConfirmed -= WaitForVotes;
            GameManager.TieAnswerConfirmed -= m_removeQuestionCardListeners;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            m_confirming = false;
            m_selectedCardIndex = -1;

            bool isTiePlayer = false;
            foreach (TieData.TiePlayer player in GameManager.singleton.currentTieData.tiePlayers)
            {
                if (player.playerIndex == Player.owningPlayer.playerIndex)
                {
                    isTiePlayer = true;
                    break;
                }
            }

            SetQuestionCard(GameManager.singleton.currentTieData.questionIndex);
            playerAnswerCardsParent.SetActive(false);

            answerInput.gameObject.SetActive(isTiePlayer);

            //a player that will vote on the tying players
            if (!isTiePlayer)
            {
                headerMessageText.SetText("Waiting for Answers...");

                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Waiting for Answers...");

                GameManager.AllTieAnswersConfirmed += AllowVoting;
            }
            else //one of the tying players
            {
                headerMessageText.SetText("Answer the Question about Yourself");

                answerInput.interactable = true;
                answerInput.text = "";
                m_currentAnswerInput = "";

                actionButton.interactable = true;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Answer");
                actionButton.onClick.AddListener(ConfirmAnswer);

                GameManager.AllTieAnswersConfirmed += WaitForVotes;
            }

            if (Player.owningPlayer.IsOwnedByServer)
            {
                questionCard.onClick.AddListener(GameManager.singleton.ChangeTieQuestion_ServerRpc);
                GameManager.TieAnswerConfirmed += m_removeQuestionCardListeners;
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            actionButton.onClick.RemoveAllListeners();
            GameManager.NewTieQuestion -= SetQuestionCard;
            GameManager.AllTieAnswersConfirmed -= AllowVoting;
            GameManager.AllTieAnswersConfirmed -= WaitForVotes;
            GameManager.TieAnswerConfirmed -= m_removeQuestionCardListeners;
        }

        //========================================================================
        void SelectPlayer(int playerIndex)
        {
            UnselectPlayer(m_selectedCardIndex); //reset previous selected card

            m_selectedCardIndex = playerIndex;
            SetPlayerColor(m_playerAnswerCards[playerIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Vote");
        }
        void UnselectPlayer(int playerIndex)
        {
            if (m_selectedCardIndex == -1)
                return; //there wasn't a previous selected Player

            SetPlayerColor(m_playerAnswerCards[playerIndex], UIManager.singleton.unselectedUIColor);
        }
        void SetPlayerColor(Transform player, Color newColor)
        {
            player.GetComponent<Button>().image.color = newColor;
        }

        //========================================================================
        void ConfirmAnswer()
        {
            if (m_confirming)
                return;
            m_confirming = true;

            headerMessageText.SetText("Waiting for Answers");

            answerInput.interactable = false;
            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Answer Confirmed");
            actionButton.onClick.RemoveListener(ConfirmAnswer);

            GameManager.singleton.ConfirmTieAnswer_Rpc(Player.owningPlayer.playerIndex, m_currentAnswerInput);
        }
        void WaitForVotes()
        {
            headerMessageText.SetText("Waiting for Votes");
            answerInput.gameObject.SetActive(false);

            SetUpPlayerAnswerCards(true);
        }
        void AllowVoting()
        {
            GameManager.AllTieAnswersConfirmed -= AllowVoting;

            headerMessageText.SetText("Vote on a Favorite");

            SetUpPlayerAnswerCards(false);

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Player");
            actionButton.onClick.AddListener(ConfirmVote);
        }
        void ConfirmVote()
        {
            if (m_confirming)
                return;
            m_confirming = true;

            headerMessageText.SetText("Waiting for Other Votes");

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Vote Confirmed");
            actionButton.onClick.RemoveListener(ConfirmVote);

            GameManager.singleton.ConfirmVote_Rpc(m_selectedCardIndex);
        }

        //========================================================================
        public void SetAnswerInput(string newInput)
        {
            if (newInput.Length > 128)
                return; //prevent a name that exceeds the "FixedString128Bytes" size

            m_currentAnswerInput = newInput;
        }

        //========================================================================
        private void SetQuestionCard(int questionIndex)
        {
            string newQuestion = DataManager.singleton.GetQuestion(questionIndex).ToString();
            questionCard.GetComponentInChildren<TextMeshProUGUI>().SetText(newQuestion);
        }
        void SetUpPlayerAnswerCards(bool isTiePlayer)
        {
            print("SetUpPlayerAnswerCards");

            foreach (Transform button in m_playerAnswerCards)
            {
                if (button == null)
                    continue;

                Destroy(button.gameObject);
            }
            m_playerAnswerCards.Clear();

            for (int i = 0; i < GameManager.singleton.currentTieData.tiePlayers.Count; i++)
            {
                Transform cardObject = Instantiate(playerAnswerCardPrefab, playerAnswerCardsContent).transform;
                m_playerAnswerCards.Add(cardObject);

                if (!isTiePlayer)
                {
                    int index = i;
                    cardObject.GetComponent<Button>().onClick.AddListener(
                        () => { SelectPlayer(index); }
                    );
                    SetPlayerColor(cardObject, UIManager.singleton.unselectedUIColor);
                }

                TextMeshProUGUI playerNameTexxt = cardObject.GetChild(0).GetComponent<TextMeshProUGUI>();
                playerNameTexxt.SetText(GameManager.singleton.currentTieData.GetPlayerName(i));

                TextMeshProUGUI answerCardText = cardObject.GetChild(1).GetComponent<TextMeshProUGUI>();
                answerCardText.SetText(GameManager.singleton.currentTieData.tiePlayers[i].answer.ToString());
            }

            playerAnswerCardsParent.SetActive(true);
        }

        //========================================================================
    }
}

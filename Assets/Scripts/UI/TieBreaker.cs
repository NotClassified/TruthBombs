using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;

namespace UIState
{
    public class TieBreaker : StateBase
    {
        //========================================================================
        public GameObject playerSelectVisibilityParent;
        public Transform playerSelectParent;
        public GameObject playerSelectPrefab;
        List<Transform> m_playerSelectButtons = new();

        int m_selectePlayerIndex;

        public TMP_InputField answerInput;
        string m_currentAnswerInput;

        //========================================================================
        public Transform questionAnswerCardParent;
        public GameObject questionAnswerCardPrefab;
        Transform m_questionAnswerCard;

        //========================================================================
        public TextMeshProUGUI headerMessageText;
        public Button actionButton;

        //========================================================================
        private void Awake()
        {
            GameManager.TieAnswerConfirmed += UpdateAnswer;
        }
        private void OnDestroy()
        {
            GameManager.TieAnswerConfirmed -= UpdateAnswer;
            GameManager.AllTieAnswersConfirmed -= AllowVoting;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            m_selectePlayerIndex = -1;

            bool isTiePlayer = false;
            foreach (TieData.TiePlayer player in GameManager.singleton.currentTieData.tiePlayers)
            {
                if (player.playerIndex == Player.owningPlayer.playerIndex)
                {
                    isTiePlayer = true;
                    break;
                }
            }

            playerSelectVisibilityParent.SetActive(!isTiePlayer);
            answerInput.gameObject.SetActive(!isTiePlayer);
            answerInput.interactable = true;
            answerInput.text = "";
            m_currentAnswerInput = "";

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

                actionButton.interactable = true;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Answer");
                actionButton.onClick.AddListener(ConfirmAnswer);
            }


            //set cards on all players
            {
                if (m_questionAnswerCard == null)
                    m_questionAnswerCard = Instantiate(questionAnswerCardPrefab, questionAnswerCardParent).transform;

                TextMeshProUGUI questionCardText = m_questionAnswerCard.GetChild(0).GetComponent<TextMeshProUGUI>();
                questionCardText.SetText(DataManager.singleton.GetQuestion(GameManager.singleton.currentTieData.questionIndex).ToString());

                TextMeshProUGUI answerCardText = m_questionAnswerCard.GetChild(1).GetComponent<TextMeshProUGUI>();
                answerCardText.SetText("...");
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void SelectPlayer(int playerIndex)
        {
            UnselectPlayer(m_selectePlayerIndex); //reset previous selected card

            m_selectePlayerIndex = playerIndex;
            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Vote");
        }
        void UnselectPlayer(int playerIndex)
        {
            if (m_selectePlayerIndex == -1)
                return; //there wasn't a previous selected Player

            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.unselectedUIColor);
        }
        void SelectPlayer_NonGuesser(int playerIndex)
        {
            foreach (Transform selectButton in m_playerSelectButtons)
            {
                if (selectButton == null)
                    continue;
                SetPlayerColor(selectButton, UIManager.singleton.defaultUIColor);
            }
            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.selectedUIColor);
        }

        void SetPlayerColor(Transform player, Color newColor)
        {
            player.GetComponent<Button>().image.color = newColor;
        }

        //========================================================================
        void AllowVoting()
        {
            GameManager.AllTieAnswersConfirmed -= AllowVoting;

            headerMessageText.SetText("Vote on a Favorite");

            foreach (Transform button in m_playerSelectButtons)
            {
                if (button == null)
                    continue;

                Destroy(button.gameObject);
            }
            m_playerSelectButtons.Clear();

            for (int i = 0; i < GameManager.singleton.currentTieData.tiePlayers.Count; i++)
            {
                Transform selectButton = Instantiate(playerSelectPrefab, playerSelectParent).transform;
                m_playerSelectButtons.Add(selectButton);

                int index = i;
                m_playerSelectButtons[i].GetComponent<Button>().onClick.AddListener(
                    () => { SelectPlayer(index); }
                );
                SetPlayerColor(selectButton, UIManager.singleton.unselectedUIColor);

                TextMeshProUGUI questionCardText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
                questionCardText.text = GameManager.singleton.currentTieData.GetPlayerName(i);
            }

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Player");
            actionButton.onClick.AddListener(ConfirmVote);
        }
        void ConfirmVote()
        {
            headerMessageText.SetText("Waiting for Votes");

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Vote Confirmed");
            actionButton.onClick.RemoveListener(ConfirmVote);

            GameManager.singleton.ConfirmVote_Rpc(m_selectePlayerIndex);
        }
        void ConfirmAnswer()
        {
            headerMessageText.SetText("Waiting for Answers/Votes");

            answerInput.interactable = false;
            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Answer Confirmed");
            actionButton.onClick.RemoveListener(ConfirmAnswer);

            GameManager.singleton.ConfirmTieAnswer_Rpc(Player.owningPlayer.playerIndex, m_currentAnswerInput);
        }

        //========================================================================
        public void SetAnswerInput(string newInput)
        {
            if (newInput.Length > 128)
                return; //prevent a name that exceeds the "FixedString128Bytes" size

            m_currentAnswerInput = newInput;
        }
        void UpdateAnswer(int tieIndex)
        {
            TextMeshProUGUI answerCardText = m_questionAnswerCard.GetChild(1).GetComponent<TextMeshProUGUI>();
            answerCardText.SetText(GameManager.singleton.currentTieData.tiePlayers[tieIndex].answer.ToString());
        }

        //========================================================================
    }
}

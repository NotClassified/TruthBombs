using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System;
using TMPro;
using System.Reflection;


namespace UIState
{

    public class AnswerSheet : StateBase
    {
        //========================================================================
        public TextMeshProUGUI targetPlayerNameText;

        //========================================================================
        public Transform questionCardParent;
        public GameObject questionCardPrefab;
        List<Button> m_questionCardButtons = new();

        //========================================================================
        int m_selectedQuestionCardIndex = -1;

        List<global::AnswerSheet> m_pendingAnswerSheets = new();

        //========================================================================
        public TMP_InputField answerInput;
        string m_currentAnswerInput;

        public Button actionButton;

        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            GameManager.singleton.NewPendingAnswerSheet += AddAnswerSheet;

            //set question cards
            {
                foreach (FixedString128Bytes card in GameManager.singleton.GetCurrentQuestionCards())
                {
                    GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
                    m_questionCardButtons.Add(cardObject.GetComponent<Button>());

                    cardObject.GetComponent<Button>().image.color = UIManager.singleton.unselectedUIColor;

                    TextMeshProUGUI cardText = cardObject.GetComponentInChildren<TextMeshProUGUI>();
                    cardText.text = card.ToString();
                }

                for (int i = 0; i < m_questionCardButtons.Count; i++)
                {
                    int index = i;
                    m_questionCardButtons[i].onClick.AddListener(() => { SelectQuestionCard(index); });
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            GameManager.singleton.NewPendingAnswerSheet -= AddAnswerSheet;

            for (int i = 0; i < m_questionCardButtons.Count; i++)
            {
                Destroy(m_questionCardButtons[i].gameObject);
            }
            m_questionCardButtons.Clear();
        }

        //========================================================================
        void AddAnswerSheet(global::AnswerSheet newSheet)
        {
            m_pendingAnswerSheets.Add(newSheet);

            //first/only pending answer sheet?
            if (m_pendingAnswerSheets.Count == 1)
            {
                SetCurrentAnswerSheet();
            }
        }
        void SetCurrentAnswerSheet()
        {
            //question card indexes and answer indexes should match
            for (int i = 0; i < m_pendingAnswerSheets[0].cardAnswers.Count; i++)
            {
                //is this question card already answered?
                if (m_pendingAnswerSheets[0].cardAnswers[i].answeringPlayerIndex != -1)
                {
                    m_questionCardButtons[i].interactable = false;
                    m_questionCardButtons[i].image.color = UIManager.singleton.unavailableUIColor;
                }
                else
                {
                    m_questionCardButtons[i].interactable = true;
                    m_questionCardButtons[i].image.color = UIManager.singleton.unselectedUIColor;
                }
            }

            //set questions and replace tokens in questions
            List<FixedString128Bytes> questions = GameManager.singleton.GetCurrentQuestionCards();
            int targetPlayerIndex = m_pendingAnswerSheets[0].targetPlayerIndex;
            for (int i = 0; i < m_questionCardButtons.Count; i++)
            {
                TextMeshProUGUI cardText = m_questionCardButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>();
                cardText.SetText(DataManager.ReplaceTokens(questions[i].ToString(), targetPlayerIndex));
            }

            //target player's name
            string targetName = PlayerManager.GetPlayerName(m_pendingAnswerSheets[0].targetPlayerIndex).ToString();
            targetPlayerNameText.SetText("Answer for: " + targetName);

            //reset various elements
            m_selectedQuestionCardIndex = -1;

            answerInput.interactable = true;
            answerInput.text = "";
            m_currentAnswerInput = "";

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Please Select a Card");


#if UNITY_EDITOR
            answerInput.text = Player.owningPlayer.playerName.ToString();
            m_currentAnswerInput = Player.owningPlayer.playerName.ToString();

            for (int i = 0; i < m_questionCardButtons.Count; i++)
            {
                if (m_questionCardButtons[i].interactable)
                {
                    SelectQuestionCard(i);
                    break;
                }
            }
#endif
        }

        //========================================================================
        void SelectQuestionCard(int cardIndex)
        {
            UnselectCard(m_selectedQuestionCardIndex); //reset previous selected card

            m_selectedQuestionCardIndex = cardIndex;
            m_questionCardButtons[cardIndex].image.color = UIManager.singleton.selectedUIColor;

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Your Answer");
        }
        void UnselectCard(int cardIndex)
        {
            if (cardIndex == -1)
                return; //there wasn't a previous selected card

            m_questionCardButtons[cardIndex].image.color = UIManager.singleton.unselectedUIColor;
        }

        //========================================================================
        public void ConfirmAnswer()
        {
            //Player.owningPlayer.AddAsnwer_ServerRpc(Player.owningPlayer.playerIndex, currentAnswerSheetindex, selectedQuestionCardIndex, answerInput.text);

            if (m_pendingAnswerSheets.Count == 0)
                return; //there are no pending answer sheets
            if (m_selectedQuestionCardIndex == -1)
                return; //there isn't a selected card

            GameManager.singleton.SyncAnswerSheet(
                m_pendingAnswerSheets[0].targetPlayerIndex,
                m_selectedQuestionCardIndex,
                m_currentAnswerInput);

            m_pendingAnswerSheets.RemoveAt(0); //remove the confirmed answer sheet
                                               //is there still pending answer sheets left?
            if (m_pendingAnswerSheets.Count > 0)
            {
                SetCurrentAnswerSheet(); //go to the next answer sheet
            }
            else
            {
                WaitForAnswerSheet();
            }
        }
        void WaitForAnswerSheet()
        {
            //disable all input:
            for (int i = 0; i < m_questionCardButtons.Count; i++)
            {
                m_questionCardButtons[i].interactable = false;
                m_questionCardButtons[i].image.color = UIManager.singleton.unavailableUIColor;
            }

            answerInput.interactable = false;
            answerInput.text = "";
            m_currentAnswerInput = "";

            actionButton.interactable = false;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Waiting For Sheet");
        }

        //========================================================================
        public void SetAnswerInput(string newInput)
        {
            if (newInput.Length > 128 - 3)
            {
                answerInput.text = m_currentAnswerInput;
                return; //prevent a name that exceeds the "FixedString128Bytes" size
            }

            m_currentAnswerInput = newInput;
        }

        //========================================================================
    }

}
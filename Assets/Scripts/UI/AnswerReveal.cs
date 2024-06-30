using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace UIState
{
    public class AnswerReveal : StateBase
    {
        //========================================================================
        public TextMeshProUGUI targetPlayerNameText;

        public Transform questionAnswerCardParent;
        public GameObject questionAnswerCardPrefab;
        List<Transform> m_questionAnswerCards = new();

        int m_selectedCardIndex;

        public Button actionButton;

        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            GameManager.RevealAnswer += RevealAnswer;

            m_selectedCardIndex = -1;
            GameManager.LastSheetAnswerRevealed -= AllAnswersRevealed;

            if (GameManager.singleton.GetPlayerReaderIndex() == Player.owningPlayer.playerIndex
                || GameManager.singleton.GetPresentingSheetIndex() == Player.owningPlayer.playerIndex)
            {
                GameManager.LastSheetAnswerRevealed += AllAnswersRevealed;
            }

            if (GameManager.singleton.GetPlayerReaderIndex() == Player.owningPlayer.playerIndex)
            {
                actionButton.interactable = true;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Reveal Answer");
                actionButton.onClick.AddListener(RequestAnswerReveal);
            }
            else
            {
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Presenting...");
            }

            //set cards
            {
                foreach (Transform card in m_questionAnswerCards)
                {
                    Destroy(card.gameObject);
                }
                m_questionAnswerCards.Clear();

                foreach (FixedString128Bytes question in GameManager.singleton.GetCurrentQuestionCards())
                {
                    Transform cardObject = Instantiate(questionAnswerCardPrefab, questionAnswerCardParent).transform;
                    m_questionAnswerCards.Add(cardObject);

                    SetCardColor(cardObject, UIManager.singleton.unselectedUIColor);

                    TextMeshProUGUI questionCardText = cardObject.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
                    questionCardText.SetText(question.ToString());
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void SelectCard(int cardIndex)
        {
            UnselectCard(m_selectedCardIndex); //reset previous selected card

            m_selectedCardIndex = cardIndex;
            SetCardColor(m_questionAnswerCards[cardIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Favorite Answer");
        }
        void UnselectCard(int cardIndex)
        {
            if (m_selectedCardIndex == -1)
                return; //there wasn't a previous selected card

            SetCardColor(m_questionAnswerCards[cardIndex], UIManager.singleton.unselectedUIColor);
        }

        void SetCardColor(Transform card, Color newColor)
        {
            //child 0 is the question card, child 1 is the answer card
            card.GetChild(0).GetComponent<Button>().image.color = newColor;
            card.GetChild(1).GetComponent<Button>().image.color = newColor;
        }

        //========================================================================
        void RequestAnswerReveal()
        {
            GameManager.singleton.RevealAnswer_Rpc();
        }
        void RevealAnswer(int sheetIndex, int answerIndex)
        {
            string revealedAnswer = GameManager.singleton.answerSheets[sheetIndex].cardAnswers[answerIndex].answerString.ToString();
            m_questionAnswerCards[answerIndex].GetChild(1).GetComponentInChildren<TextMeshProUGUI>().SetText(revealedAnswer);
        }

        void AllAnswersRevealed()
        {
            if (GameManager.singleton.GetPlayerReaderIndex() == Player.owningPlayer.playerIndex)
            {
                actionButton.onClick.RemoveListener(RequestAnswerReveal);
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Presenting...");
            }
            else //this is the target player
            {
                for (int i = 0; i < m_questionAnswerCards.Count; i++)
                {
                    int index = i;
                    m_questionAnswerCards[i].GetComponentInChildren<Button>().onClick.AddListener(
                        () => { SelectCard(index); }
                    );
                }

                actionButton.onClick.AddListener(ConfirmFavoriteAnswer);
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Favorite Answer");
            }
        }

        //========================================================================
        void ConfirmFavoriteAnswer()
        {
            GameManager.singleton.ConfirmFavoriteAnswer_Rpc(m_selectedCardIndex);
        }

        //========================================================================
    }
}

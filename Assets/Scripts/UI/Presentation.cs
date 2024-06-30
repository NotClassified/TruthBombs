using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace UIState
{

    public class Presentation : StateBase
    {
        //========================================================================
        public TextMeshProUGUI targetPlayerNameText;

        //========================================================================
        public GameObject cardVisibilityParent;

        public Transform questionAnswerCardParent;
        public GameObject questionAnswerCardPrefab;
        List<Transform> m_questionAnswerCards = new();

        int m_selectedCardIndex;

        //========================================================================
        public GameObject playerSelectVisibilityParent;

        public Transform playerSelectParent;
        public GameObject playerSelectPrefab;
        List<Transform> m_playerSelectButtons = new();

        int m_selectePlayerIndex;

        //========================================================================
        public GameObject scoreBoardParent;

        //========================================================================
        enum PresentationState
        {
            Cards, PlayerSelect, ScoreBoard
        }
        PresentationState currentState;
        System.Action ChangeToScoreBoardState;
        System.Action ChangeToCardState;

        public Button actionButton;


        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            GameManager.RevealAnswer += RevealAnswer;

            ChangeToScoreBoardState = () => { ChangeState(PresentationState.ScoreBoard); };
            GameManager.GuessConfirmed += ChangeToScoreBoardState;

            ChangeToCardState = () => { ChangeState(PresentationState.Cards); };
            GameManager.PresentedNextSheet += ChangeToCardState;


            ChangeState(PresentationState.Cards);

            //set cards and player select buttons
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


                foreach (Transform button in m_playerSelectButtons)
                {
                    Destroy(button.gameObject);
                }
                m_playerSelectButtons.Clear();


                foreach (Player player in PlayerManager.singleton.allPlayers)
                {
                    Transform selectButton = Instantiate(playerSelectPrefab, playerSelectParent).transform;
                    m_playerSelectButtons.Add(selectButton);

                    SetPlayerColor(selectButton, UIManager.singleton.unselectedUIColor);

                    TextMeshProUGUI questionCardText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
                    questionCardText.text = player.playerName.ToString();
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            GameManager.RevealAnswer -= RevealAnswer;
            GameManager.GuessConfirmed -= ChangeToScoreBoardState;
            GameManager.PresentedNextSheet -= ChangeToCardState;

            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void ChangeState(PresentationState newState)
        {
            switch (newState)
            {
                case PresentationState.Cards:
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
                    break;

                case PresentationState.PlayerSelect:
                    m_selectePlayerIndex = -1;

                    actionButton.onClick.RemoveListener(ConfirmFavoriteAnswer);
                    actionButton.onClick.AddListener(ConfirmPlayerGuess);

                    actionButton.interactable = false;
                    actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Player");
                    break;

                case PresentationState.ScoreBoard:
                    actionButton.onClick.RemoveListener(ConfirmPlayerGuess);
                    actionButton.onClick.AddListener(GameManager.singleton.ConfirmScoreBoard_Rpc);

                    actionButton.interactable = true;
                    actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm");
                    break;
            }

            cardVisibilityParent.SetActive(newState == PresentationState.Cards);
            playerSelectVisibilityParent.SetActive(newState == PresentationState.PlayerSelect);
            scoreBoardParent.SetActive(newState == PresentationState.ScoreBoard);
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
        void SelectPlayer(int playerIndex)
        {
            UnselectPlayer(m_selectePlayerIndex); //reset previous selected card

            m_selectePlayerIndex = playerIndex;
            SetPlayerColor(m_questionAnswerCards[playerIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Guess");
        }
        void UnselectPlayer(int playerIndex)
        {
            if (m_selectePlayerIndex == -1)
                return; //there wasn't a previous selected Player

            SetPlayerColor(m_questionAnswerCards[playerIndex], UIManager.singleton.unselectedUIColor);
        }

        void SetPlayerColor(Transform player, Color newColor)
        {
            player.GetComponentInChildren<Button>().image.color = newColor;
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

            ChangeState(PresentationState.PlayerSelect);
        }
        void ConfirmPlayerGuess()
        {
            GameManager.singleton.ConfirmPlayerGuess_Rpc(m_selectePlayerIndex);
        }

        //========================================================================
    }

}
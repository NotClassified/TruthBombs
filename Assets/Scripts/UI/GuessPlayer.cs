using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace UIState
{
    public class GuessPlayer : StateBase
    {
        //========================================================================
        public Transform playerSelectParent;
        public GameObject playerSelectPrefab;
        List<Transform> m_playerSelectButtons = new();

        int m_selectePlayerIndex;

        //========================================================================
        public Transform questionAnswerCardParent;
        public GameObject questionAnswerCardPrefab;
        List<Transform> m_questionAnswerCards = new();

        //========================================================================
        public TextMeshProUGUI targetPlayerNameText;
        public Button actionButton;

        //========================================================================
        private void OnDestroy()
        {
            GameManager.GuesserSelectedPlayer -= SelectPlayer_NonGuesser;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            m_selectePlayerIndex = -1;

            targetPlayerNameText.SetText(GameManager.GetPresentingTargetPlayerName() + " Has Selected a Favorite!");

            //set player select buttons
            {
                foreach (Transform button in m_playerSelectButtons)
                {
                    if (button == null)
                        continue;

                    Destroy(button.gameObject);
                }
                m_playerSelectButtons.Clear();


                foreach (Player player in PlayerManager.singleton.allPlayers)
                {
                    if (player.playerIndex == GameManager.GetPresentingSheetIndex())
                    {
                        m_playerSelectButtons.Add(null); //placeholder to make indexes match player indexes
                        continue; //the presenting target player cannot choose themselves
                    }

                    Transform selectButton = Instantiate(playerSelectPrefab, playerSelectParent).transform;
                    m_playerSelectButtons.Add(selectButton);

                    SetPlayerColor(selectButton, UIManager.singleton.defaultUIColor);

                    TextMeshProUGUI questionCardText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
                    questionCardText.text = player.playerName.ToString();
                }
            }


            //is this the target player that will be guessing who made their favorite answer?
            if (GameManager.GetPresentingSheetIndex() == Player.owningPlayer.playerIndex)
            {
                for (int i = 0; i < m_playerSelectButtons.Count; i++)
                {
                    if (m_playerSelectButtons[i] == null)
                        continue;

                    int index = i;
                    m_playerSelectButtons[i].GetComponent<Button>().onClick.AddListener(
                        () => { SelectPlayer(index); }
                    );
                    SetPlayerColor(m_playerSelectButtons[i], UIManager.singleton.unselectedUIColor);
                }

                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Player");
                actionButton.onClick.AddListener(ConfirmPlayerGuess);
            }
            else //not the target player
            {
                GameManager.GuesserSelectedPlayer += SelectPlayer_NonGuesser;

                actionButton.interactable = false;
                string targetPlayerName = GameManager.GetPresentingTargetPlayerName();
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText(targetPlayerName + " is Guessing");
            }


            //set cards
            {
                foreach (Transform card in m_questionAnswerCards)
                {
                    Destroy(card.gameObject);
                }
                m_questionAnswerCards.Clear();

                //set text questions and answers
                int presentingSheet = GameManager.GetPresentingSheetIndex();
                List<FixedString128Bytes> questions = GameManager.singleton.GetCurrentQuestionCards();
                for (int i = 0; i < questions.Count; i++)
                {
                    Transform cardObject = Instantiate(questionAnswerCardPrefab, questionAnswerCardParent).transform;
                    m_questionAnswerCards.Add(cardObject);

                    //is this card the favorite?
                    if (i == GameManager.singleton.answerSheets[presentingSheet].favoriteAnswerIndex)
                        cardObject.GetComponent<Button>().image.color = UIManager.singleton.selectedUIColor;
                    else
                        cardObject.GetComponent<Button>().image.color = UIManager.singleton.defaultUIColor;

                    TextMeshProUGUI questionCardText = cardObject.GetChild(0).GetComponent<TextMeshProUGUI>();
                    questionCardText.SetText(questions[i].ToString());

                    TextMeshProUGUI answerCardText = cardObject.GetChild(1).GetComponent<TextMeshProUGUI>();
                    answerCardText.SetText(GameManager.singleton.answerSheets[presentingSheet].cardAnswers[i].answerString.ToString());

                }
                
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            GameManager.GuesserSelectedPlayer -= SelectPlayer_NonGuesser;
            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void SelectPlayer(int playerIndex)
        {
            UnselectPlayer(m_selectePlayerIndex); //reset previous selected card

            m_selectePlayerIndex = playerIndex;
            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Guess");

            GameManager.singleton.GuesserSelectPlayer_Rpc(playerIndex);
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
        void ConfirmPlayerGuess()
        {
            GameManager.singleton.ConfirmPlayerGuess_Rpc(m_selectePlayerIndex);
        }

        //========================================================================
    }

}
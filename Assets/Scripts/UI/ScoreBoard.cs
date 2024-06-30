using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIState
{
    public class ScoreBoard : StateBase
    {
        //========================================================================
        public TextMeshProUGUI guessFeedbackText;

        public Transform scoreBoardContent;
        public GameObject scoreBoardItemPrefab;
        List<Transform> m_scoreBoardItems = new();

        public Button actionButton;

        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            actionButton.onClick.AddListener(ProceedToNextSheet);

            actionButton.interactable = true;
            if (GameManager.singleton.IsLastPresentingSheet())
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Proceed To Next Player");
            else
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Start Next Round");

            //set score board
            {
                foreach (Transform item in m_scoreBoardItems)
                {
                    Destroy(item.gameObject);
                }
                m_scoreBoardItems.Clear();


                foreach (Player player in PlayerManager.singleton.allPlayers)
                {
                    Transform scoreItem = Instantiate(scoreBoardItemPrefab, scoreBoardContent).transform;
                    m_scoreBoardItems.Add(scoreItem);

                    TextMeshProUGUI questionCardText = scoreItem.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
                    questionCardText.text = player.playerName.ToString();
                }

                UpdateScoreBoard();
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void UpdateScoreBoard()
        {
            string targetPlayerName = GameManager.singleton.GetPresentingTargetPlayerName();
            string guessedPlayerName = GameManager.singleton.GetGuessedPlayerName();
            if (GameManager.singleton.IsGuessCorrect())
            {
                guessFeedbackText.SetText("Correct!\n" 
                    + targetPlayerName + " Guessed " + guessedPlayerName);
            }
            else
            {
                guessFeedbackText.SetText("Wrong!\n"
                    + targetPlayerName + " Guessed " + guessedPlayerName
                    + "\n" + GameManager.singleton.GetFavoritedPlayerName() + " was the Answerer");
            }

            for (int i = 0; i < m_scoreBoardItems.Count; i++)
            {
                int pointAmount = GameManager.singleton.playerScores[i];
                Color currentColor = UIManager.singleton.selectedUIColor; //indicates the player has the point

                for (int pointIndex = 1; pointIndex < m_scoreBoardItems[i].childCount; pointIndex++)
                {
                    if (pointIndex - 1 == pointAmount) //player does NOT have the point
                        currentColor = UIManager.singleton.unavailableUIColor; 

                    m_scoreBoardItems[i].GetChild(pointIndex).GetComponent<Image>().color = currentColor;
                }
            }
        }

        //========================================================================
        void ProceedToNextSheet()
        {
            GameManager.singleton.ConfirmScoreBoard_ServerRpc();
        }

        //========================================================================
    }
}

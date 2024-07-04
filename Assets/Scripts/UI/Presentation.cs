using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace UIState
{

    public class Presentation : StateBase
    {
        //========================================================================
        StateBase m_currentState;

        private void OnDestroy()
        {
            GameManager.PresentNextSheet -= ChangeUIState<AnswerReveal>;
            GameManager.FavoriteAnswerConfirmed -= ChangeUIState<GuessPlayer>;
            GameManager.GuessConfirmed -= ChangeUIState<ScoreBoard>;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            GameManager.PresentNextSheet += ChangeUIState<AnswerReveal>;
            GameManager.FavoriteAnswerConfirmed += ChangeUIState<GuessPlayer>;
            GameManager.GuessConfirmed += ChangeUIState<ScoreBoard>;

            ChangeUIState<AnswerReveal>();
        }
        public override void OnExit()
        {
            base.OnExit();

            GameManager.PresentNextSheet -= ChangeUIState<AnswerReveal>;
            GameManager.FavoriteAnswerConfirmed -= ChangeUIState<GuessPlayer>;
            GameManager.GuessConfirmed -= ChangeUIState<ScoreBoard>;

            m_currentState = null;
        }

        public void ChangeUIState<NewState>() where NewState : StateBase
        {
            m_currentState?.OnExit();
            m_currentState = transform.GetComponentInChildren<NewState>(true);
            m_currentState?.OnEnter();
        }

        //========================================================================
    }

}
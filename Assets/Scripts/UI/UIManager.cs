using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIState;

public class UIManager : MonoBehaviour
{
    public static UIManager singleton;

    public Color selectedUIColor;
    public Color unselectedUIColor;
    public Color unavailableUIColor;

    StateBase m_currentState;

    private void Awake()
    {
        singleton = this;

        EnterPlayerName.NameConfirmed += ChangeUIState<SetupQuestionCards>;
        GameManager.PresentationFinished += ChangeUIState<SetupQuestionCards>;
        GameManager.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation += ChangeUIState<Presentation>;
    }
    private void Start()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        ChangeUIState<EnterPlayerName>();
    }

    public void ChangeUIState<NewState>() where NewState : StateBase
    {
        m_currentState?.OnExit();
        m_currentState = transform.GetComponentInChildren<NewState>(true);
        m_currentState?.OnEnter();
    }

    public bool IsCurrentState<State>() => m_currentState.GetType() == typeof(State);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager singleton;

    public Color selectedUIColor;
    public Color unselectedUIColor;
    public Color unavailableUIColor;

    StateBase currentState;

    private void Awake()
    {
        singleton = this;

        GameManager.StartPresentation += ChangeUIState<State_Presentation>;
        GameManager.PresentationFinished += ChangeUIState<State_SetupQuestionCards>;
    }
    private void Start()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        singleton.ChangeUIState<State_EnterPlayerName>();
    }

    public void ChangeUIState<NewState>() where NewState : StateBase
    {
        currentState?.OnExit();
        currentState = transform.GetComponentInChildren<NewState>(true);
        currentState?.OnEnter();
    }

    public bool IsCurrentState<State>() => currentState.GetType() == typeof(State);
}

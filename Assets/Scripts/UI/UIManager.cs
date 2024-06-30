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

    StateBase currentState;

    private void Awake()
    {
        singleton = this;

        GameManager.StartPresentation += ChangeUIState<Presentation>;
        GameManager.PresentationFinished += ChangeUIState<SetupQuestionCards>;
    }
    private void Start()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        singleton.ChangeUIState<EnterPlayerName>();
    }

    public void ChangeUIState<NewState>() where NewState : StateBase
    {
        currentState?.OnExit();
        currentState = transform.GetComponentInChildren<NewState>(true);
        currentState?.OnEnter();
    }

    public bool IsCurrentState<State>() => currentState.GetType() == typeof(State);
}

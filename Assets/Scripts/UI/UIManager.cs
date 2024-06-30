using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIState;

public class UIManager : MonoBehaviour
{
    public static UIManager singleton;

    System.Action<string> m_NameConfirmedCallback;

    public Color defaultUIColor;
    public Color selectedUIColor;
    public Color unselectedUIColor;
    public Color unavailableUIColor;
    [Space]

    public Transform[] nonStateChildren;
    public TextMeshProUGUI playerName;

    StateBase m_currentState;

    private void Awake()
    {
        singleton = this;

        FindObjectOfType<Unity.Netcode.NetworkManager>().OnClientStarted += ChangeUIState<EnterPlayerName>;
        FindObjectOfType<Unity.Netcode.NetworkManager>().OnServerStarted += ChangeUIState<EnterPlayerName>;

        m_NameConfirmedCallback = (string name) => 
        {
            playerName.SetText(name);
            ChangeUIState<SetupQuestionCards>();
        };
        EnterPlayerName.NameConfirmed += m_NameConfirmedCallback;

        GameManager.PresentationFinished += ChangeUIState<SetupQuestionCards>;
        GameManager.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation += ChangeUIState<Presentation>;
    }
    private void Start()
    {
        foreach (Transform child in transform)
        {
            bool isStateChild = true;
            foreach (Transform nonStateChild in nonStateChildren)
            {
                if (nonStateChild == child)
                {
                    isStateChild = false;
                    break;
                }
            }
            if (isStateChild)
                child.gameObject.SetActive(false);
        }

        ChangeUIState<ConnectOnline>();
    }

    public void ChangeUIState<NewState>() where NewState : StateBase
    {
        m_currentState?.OnExit();
        m_currentState = transform.GetComponentInChildren<NewState>(true);
        m_currentState?.OnEnter();
    }

    public bool IsCurrentState<State>() => m_currentState.GetType() == typeof(State);
}

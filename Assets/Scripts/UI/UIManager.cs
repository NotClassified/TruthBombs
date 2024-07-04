using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIState;
using System.Runtime.CompilerServices;

public class UIManager : MonoBehaviour
{
    //========================================================================
    public static UIManager singleton;

    //========================================================================
    public Color defaultUIColor;
    public Color selectedUIColor;
    public Color unselectedUIColor;
    public Color unavailableUIColor;
    [Space]

    //========================================================================
    StateBase m_currentState;
    public Transform[] nonStateChildren;

    public TextMeshProUGUI playerName;

    //========================================================================
    public GameObject pauseScreen;
    bool restartingGame = false;

    //========================================================================
    public GameObject disconnectedScreenParent;

    public TextMeshProUGUI playerDisconnectText;
    public TextMeshProUGUI waitMessageText;
    public Transform host_WaitForPlayer;
    public GameObject host_RestartGame;

    //========================================================================
    private void Awake()
    {
        singleton = this;

        Player.Disconnected += DisconnectGame;

        Player.OwnerSpawned += ChangeUIState<EnterPlayerName>;
        EnterPlayerName.NameConfirmed += PlayerNameConfirmed;
        GameManager.PresentationFinished += ChangeUIState<SetupQuestionCards>;
        GameManager.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation += ChangeUIState<Presentation>;
    }

    private void OnDestroy()
    {
        Player.Disconnected -= DisconnectGame;

        Player.OwnerSpawned -= ChangeUIState<EnterPlayerName>;
        EnterPlayerName.NameConfirmed -= PlayerNameConfirmed;
        GameManager.PresentationFinished -= ChangeUIState<SetupQuestionCards>;
        GameManager.StartAnswering -= ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets -= ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation -= ChangeUIState<Presentation>;
    }

    private void Start()
    {
        pauseScreen.SetActive(false);

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

    //========================================================================
    public void ChangeUIState<NewState>() where NewState : StateBase
    {
        m_currentState?.OnExit();
        m_currentState = transform.GetComponentInChildren<NewState>(true);
        m_currentState?.OnEnter();
    }

    public bool IsCurrentState<State>() => m_currentState.GetType() == typeof(State);

    //========================================================================
    void PlayerNameConfirmed(string newName)
    {
        playerName.SetText(newName);
        ChangeUIState<SetupQuestionCards>();
    }

    //========================================================================
    public void PauseButton(bool pause)
    {
        pauseScreen.SetActive(pause);
    }
    public void DisconnectButton()
    {
        DisconnectGame(Player.owningPlayer.playerIndex);
    }
    void DisconnectGame(int disconnectingPlayerIndex)
    {
        if (restartingGame)
            return;
        restartingGame = true;

        Player.disconnectingPlayerIndex = Player.owningPlayer.playerIndex;

        FindObjectOfType<Unity.Netcode.NetworkManager>().Shutdown();
        Destroy(FindObjectOfType<Unity.Netcode.NetworkManager>().gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    //========================================================================
}

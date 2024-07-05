using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIState;
using System.Runtime.CompilerServices;
using System;

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
    public TextMeshProUGUI versionText;
    public GameObject disconnectMessage;

    //========================================================================
    public GameObject pauseScreen;
    bool restartingGame = false;
    public Button pauseActionButton;

    //========================================================================
    private void Awake()
    {
        singleton = this;

        Player.OwnerSpawned += ChangeUIState<EnterPlayerName>;
        EnterPlayerName.NameConfirmed += PlayerNameConfirmed;
        GameManager.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation += ChangeUIState<Presentation>;
        GameManager.WinOrTieScreen += ChangeUIState<WinScreen>;
        GameManager.StartTieBreaker += ChangeUIState<TieBreaker>;
        GameManager.StartNewGame += ChangeUIState<SetupQuestionCards>;

        GameManager.SyncedGameState += SyncedGameState;
        versionText.SetText(Application.version);
    }

    private void OnDestroy()
    {
        Player.OwnerSpawned -= ChangeUIState<EnterPlayerName>;
        EnterPlayerName.NameConfirmed -= PlayerNameConfirmed;
        GameManager.StartAnswering -= ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets -= ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation -= ChangeUIState<Presentation>;
        GameManager.WinOrTieScreen -= ChangeUIState<WinScreen>;
        GameManager.StartTieBreaker -= ChangeUIState<TieBreaker>;
        GameManager.StartNewGame -= ChangeUIState<SetupQuestionCards>;

        GameManager.StartAnswering -= HideIncompatibleVersionMessage;
        GameManager.StartNewGame -= HideIncompatibleVersionMessage;

        GameManager.SyncedGameState -= SyncedGameState;
    }

    private void Start()
    {
        PlayerManager.singleton.PlayerAdded += PlayerAdded;

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

        GameManager.singleton.IncompatiblePlayerVersion += IncompatiblePlayerVersion;
        GameManager.StartAnswering += HideIncompatibleVersionMessage;
        GameManager.StartNewGame += HideIncompatibleVersionMessage;
        disconnectMessage.SetActive(false);
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
        if (m_currentState.GetType() == typeof(ConnectOnline))
        {
            pauseActionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Quit Game");
            pauseActionButton.onClick.RemoveAllListeners();
            pauseActionButton.onClick.AddListener(Application.Quit);
        }
        else
        {
            pauseActionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Disconnect");
            pauseActionButton.onClick.RemoveAllListeners();
            pauseActionButton.onClick.AddListener(() => { DisconnectGame(Player.owningPlayer.playerIndex); });
        }
    }
    private void PlayerAdded(Player player)
    {
        player.Disconnected += DisconnectGame;
    }
    void DisconnectGame(int disconnectingPlayerIndex)
    {
        if (restartingGame)
            return;
        restartingGame = true;

        Player.disconnectingPlayerIndex = Player.owningPlayer.playerIndex;

        Unity.Netcode.NetworkManager.Singleton.Shutdown();
        Destroy(Unity.Netcode.NetworkManager.Singleton.gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    //========================================================================
    void IncompatiblePlayerVersion(int playerIndex, int version)
    {
        disconnectMessage.SetActive(true);
        disconnectMessage.GetComponentInChildren<TextMeshProUGUI>().SetText(
            "Player " + playerIndex + " has an incompatible version (" + version + ").\nPlease disconnect.");
    }
    void HideIncompatibleVersionMessage() => disconnectMessage.gameObject.SetActive(false);

    private void SyncedGameState()
    {
        GameManager.disconnectedDueToOnGoingGame = GameManager.singleton.playingGame;

        //game already ongoing?
        if (GameManager.disconnectedDueToOnGoingGame)
        {
            DisconnectGame(Player.owningPlayer.playerIndex);
        }
    }

    //========================================================================
    public void Editor_FocusState(StateBase newState)
    {
        foreach (StateBase state in GetComponentsInChildren<StateBase>(true))
        {
            if (state == newState)
            {
                state.gameObject.SetActive(true);
            }
            else if (state.GetType() == typeof(Presentation))
            {
                state.gameObject.SetActive(false);
                foreach (StateBase pState in state.GetComponentsInChildren<StateBase>(true))
                {
                    if (pState == newState)
                    {
                        state.gameObject.SetActive(true);
                        pState.gameObject.SetActive(true);
                    }
                }
            }
            else
                state.gameObject.SetActive(false);
        }
    }

    //========================================================================
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIState;
using System.Runtime.CompilerServices;
using System;
using Unity.Netcode;

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
    public bool restartingGame = false;
    public Button quitButton;
    public Button pauseActionButton;

    //========================================================================
    private void Awake()
    {
        singleton = this;

        Player.OwnerSpawned += OwnerSpawned;
        Player.NameConfirmed += PlayerNameConfirmed;
    }

    private void OnDestroy()
    {
        Player.OwnerSpawned -= OwnerSpawned;
        Player.NameConfirmed -= PlayerNameConfirmed;
    }

    private void Start()
    {
        PlayerManager.singleton.PlayerAdded += PlayerAdded;

        GameManager.singleton.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.singleton.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.singleton.StartPresentation += ChangeUIState<Presentation>;
        GameManager.singleton.WinOrTieScreen += ChangeUIState<WinScreen>;
        GameManager.singleton.StartTieBreaker += ChangeUIState<TieBreaker>;
        GameManager.singleton.StartNewGame += ChangeUIState<SetupQuestionCards>;

        GameManager.singleton.SyncedGameState += SyncedGameState;

        //non state UI
        {
            GameManager.singleton.IncompatiblePlayerVersion += IncompatiblePlayerVersion;
            GameManager.singleton.StartAnswering += HideIncompatibleVersionMessage;
            GameManager.singleton.StartNewGame += HideIncompatibleVersionMessage;
            disconnectMessage.SetActive(false);

            versionText.SetText("version: " + Application.version);
            quitButton.onClick.AddListener(Application.Quit);
            pauseScreen.SetActive(false);
        }

        //state UI
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
        if (m_currentState.GetType() == typeof(ConnectOnline))
        {
            pauseActionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Restart Game");
            pauseActionButton.onClick.RemoveAllListeners();
            pauseActionButton.onClick.AddListener(ForceRestartNetworkManager);
            quitButton.gameObject.SetActive(true);
        }
        else
        {
            pauseActionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Disconnect");
            pauseActionButton.onClick.RemoveAllListeners();
            pauseActionButton.onClick.AddListener(() => { DisconnectGame(Player.owningPlayer.playerIndex); });
            quitButton.gameObject.SetActive(false);
        }
    }
    private void PlayerAdded(Player player)
    {
        if (GameManager.singleton.IsServer)
            player.ClientDisconnected += DisconnectGame;
    }
    private void OwnerSpawned()
    {
        ChangeUIState<EnterPlayerName>();
        if (NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.OnServerStopped += ReloadScene;
        else
            NetworkManager.Singleton.OnClientStopped += ReloadScene;
    }

    void DisconnectGame(int disconnectingPlayerIndex)
    {
        if (restartingGame)
            return;
        restartingGame = true;

        if (GameManager.singleton.IsServer)
            NetworkManager.Singleton.Shutdown();
        else
            GameManager.singleton.DisconnectClient_ServerRpc(Player.owningPlayer.OwnerClientId);
    }
    void ReloadScene(bool _) => ReloadScene();
    void ReloadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
    void ForceRestartNetworkManager()
    {
        NetworkManager.Singleton.Shutdown();
        Invoke(nameof(ReloadScene), .05f);
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
        GameManager.singleton.SyncedGameState -= SyncedGameState;

        GameManager.singleton.disconnectedDueToOnGoingGame = GameManager.singleton.isPlayingGame;

        //game already ongoing?
        if (GameManager.singleton.disconnectedDueToOnGoingGame)
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

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
    System.Action<string> m_NameConfirmedCallback;

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

        m_NameConfirmedCallback = (string name) => 
        {
            playerName.SetText(name);
            ChangeUIState<SetupQuestionCards>();
        };
        EnterPlayerName.NameConfirmed += PlayerNameConfirmed;

        Player.Disconnected += PlayerDisconnected;
        Player.Reconnected += PlayerReconnected;

        Player.OwnerSpawned += OwnerSpawned;

        GameManager.PresentationFinished += ChangeUIState<SetupQuestionCards>;
        GameManager.StartAnswering += ChangeUIState<UIState.AnswerSheet>;
        GameManager.NoMorePendingAnswerSheets += ChangeUIState<WaitForAnswers>;
        GameManager.StartPresentation += ChangeUIState<Presentation>;
    }

    private void OnDestroy()
    {
        EnterPlayerName.NameConfirmed -= PlayerNameConfirmed;

        Player.Disconnected -= PlayerDisconnected;
        Player.Reconnected -= PlayerReconnected;

        Player.OwnerSpawned -= OwnerSpawned;
        GameManager.WaitingForPlayerReconnection -= Client_WaitForPlayerReconnection;

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

    private void OwnerSpawned()
    {
        if (Player.owningPlayer.IsServer)
        {
            ChangeUIState<EnterPlayerName>();
        }
        else //client
        {
            GameManager.WaitingForPlayerReconnection += Client_WaitForPlayerReconnection;

            GameManager.RespondReconnectionStatus += RespondReconnectionStatus;
        }
    }

    //========================================================================
    private void RespondReconnectionStatus(bool status)
    {
        if (status)
        {
            GameManager.PlayerHasReconnected += FinishedPlayerReconnection;
        }
        else
            ChangeUIState<EnterPlayerName>();

    }
    private void FinishedPlayerReconnection()
    {
        GameManager.PlayerHasReconnected -= FinishedPlayerReconnection;
        ChangeUIState<SetupQuestionCards>();
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
        if (restartingGame)
            return;
        restartingGame = true;

        FindObjectOfType<Unity.Netcode.NetworkManager>().Shutdown();
        Destroy(FindObjectOfType<Unity.Netcode.NetworkManager>().gameObject);

        ReloadScene();
    }
    void ReloadScene() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

    //========================================================================
    public void PlayerDisconnected(int playerIndex)
    {
        if (Player.owningPlayer.playerIndex == playerIndex) //this owner is the disconnecting Player
            return;

        GameManager.SyncedPlayers -= RestartGame;
        GameManager.SyncedPlayers += RestartGame;

        string disconnectedPlayerName = PlayerManager.singleton.GetPlayerName(playerIndex).ToString();
        string hostName = PlayerManager.singleton.GetHostName().ToString();

        playerDisconnectText.SetText(disconnectedPlayerName + " Has Disconnected");

        waitMessageText.SetText("Waiting for " + hostName + "'s Response");
        host_WaitForPlayer.GetChild(0).GetComponent<TextMeshProUGUI>().SetText("Wait for " + disconnectedPlayerName + " to Reconnect?");


        bool isHost = Player.owningPlayer.IsOwnedByServer;
        waitMessageText.gameObject.SetActive(!isHost);
        host_WaitForPlayer.gameObject.SetActive(isHost);
        host_RestartGame.SetActive(isHost);

        disconnectedScreenParent.SetActive(true);
    }
    public void Host_WaitForPlayer()
    {
        host_WaitForPlayer.gameObject.SetActive(false);
        host_RestartGame.SetActive(false);

        waitMessageText.gameObject.SetActive(true);
        waitMessageText.SetText("Waiting for Reconnection");

        GameManager.singleton.WaitForPlayerReconnection_ServerRpc();
    }
    public void Client_WaitForPlayerReconnection()
    {
        waitMessageText.SetText("Waiting for Reconnection");
    }
    void PlayerReconnected(Player player)
    {
        disconnectedScreenParent.SetActive(false);
    }
    public void Host_RestartGame()
    {
        if (restartingGame)
            return;
        restartingGame = true;

        waitMessageText.SetText("Restarting...");

        waitMessageText.gameObject.SetActive(true);
        host_WaitForPlayer.gameObject.SetActive(false);
        host_RestartGame.SetActive(false);

        GameManager.singleton.SyncPlayers_Rpc();
    }
    void RestartGame()
    {
        restartingGame = false;
        ChangeUIState<SetupQuestionCards>();

        disconnectedScreenParent.SetActive(false);
    }

    //========================================================================
}

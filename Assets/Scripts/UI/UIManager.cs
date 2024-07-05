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

        FindObjectOfType<Unity.Netcode.NetworkManager>().Shutdown();
        Destroy(FindObjectOfType<Unity.Netcode.NetworkManager>().gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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

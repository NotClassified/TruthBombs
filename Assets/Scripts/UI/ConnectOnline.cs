using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using System.Reflection;
using UnityEngine.UI;

namespace UIState
{
    public class ConnectOnline : StateBase
    {
        //========================================================================
        public NetworkManager networkManagerPrefab;

        public TextMeshProUGUI connectionFeedbackText;
        public TMP_InputField maxPlayerInput;
        public TMP_InputField joinCodeInput;

        public TextMeshProUGUI encryptionToggleText;
        public static string encryptionType = "";

        bool m_connecting = false;

        bool m_isUsingRelay;

        //========================================================================
        private async void Start()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
                networkManager = Instantiate(networkManagerPrefab);

            networkManager.OnTransportFailure += ConnectionFailure;

            m_isUsingRelay = networkManager.GetComponent<UnityTransport>().Protocol == UnityTransport.ProtocolType.RelayUnityTransport;

            if (!m_isUsingRelay)
            {
                ConnectionFailure("Not using Unity Relay Service");
                return;
            }

            if (GameManager.signedIn)
                return;

            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () => { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            GameManager.signedIn = true;
        }
        public override void OnEnter()
        {
            base.OnEnter();
            maxPlayerInput.text = "4";
            joinCodeInput.text = "";
            connectionFeedbackText.gameObject.SetActive(false);

            if (encryptionType != "")
                encryptionToggleText.SetText(encryptionType);
            else
                ToggleEncryption();

            if (GameManager.singleton.disconnectedDueToOnGoingGame)
            {
                connectionFeedbackText.gameObject.SetActive(true);
                connectionFeedbackText.SetText("That Game Has Already Started");
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            NetworkManager.Singleton.OnTransportFailure -= ConnectionFailure;
        }

        //========================================================================
        public void StartHost_Button()
        {
            if (m_connecting)
                return;
            m_connecting = true;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connecting...");

            StartHost();
        }
        public void StartClient_Button()
        {
            if (m_connecting)
                return;
            m_connecting = true;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connecting...");

            StartClient();
        }
        async void StartHost()
        {
            int playerAllocations;
            if (!int.TryParse(maxPlayerInput.text, out playerAllocations))
            {
                ConnectionFailure("Invalid Player Count");
                return;
            }
            if (playerAllocations > 50)
            {
                ConnectionFailure("Too Many Players");
                return;
            }
            if (playerAllocations <= 1)
            {
                ConnectionFailure("Need More Players");
                return;
            }
            GameManager.singleton.maxPlayers = playerAllocations;

            try
            {
                if (!m_isUsingRelay)
                {
                    NetworkManager.Singleton.StartHost();
                    return;
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerAllocations - 1);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                GameManager.singleton.currentJoinCode = joinCode;

                TextEditor te = new TextEditor();
                te.text = joinCode;
                te.SelectAll();
                te.Copy();

                RelayServerData relayServerData = new(allocation, encryptionToggleText.text);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartHost();
            }
            catch (RelayServiceException e)
            {
                ConnectionFailure();
                Debug.Log(e);
            }
        }
        async void StartClient()
        {
            string joinCode = joinCodeInput.text;
            if ((joinCode == null || joinCode == "") && m_isUsingRelay)
            {
                ConnectionFailure("Please Input Join Code");
                return;
            }

            try
            {
                if (!m_isUsingRelay)
                {
                    NetworkManager.Singleton.StartClient();
                    return;
                }

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                RelayServerData relayServerData = new(joinAllocation, encryptionToggleText.text);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                GameManager.singleton.currentJoinCode = joinCode;

                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                ConnectionFailure();
                Debug.Log(e);
            }
        }

        //========================================================================
        void ConnectionFailure() => ConnectionFailure("Connection Failure");
        void ConnectionFailure(string message)
        {
            m_connecting = false;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText(message);
        }

        public void ToggleEncryption()
        {
            if (encryptionType == "udp")
                encryptionType = "dtls";
            else
                encryptionType = "udp";

            encryptionToggleText.SetText(encryptionType);
        }

        //========================================================================
    }
}

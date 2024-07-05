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

namespace UIState
{
    public class ConnectOnline : StateBase
    {
        //========================================================================
        public TextMeshProUGUI connectionFeedbackText;
        public TMP_InputField maxPlayerInput;
        public TMP_InputField joinCodeInput;

        bool connecting = false;

        //========================================================================
        private async void Start()
        {
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

            NetworkManager.Singleton.OnTransportFailure += ConnectionFailure;

            if (GameManager.disconnectedDueToOnGoingGame)
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
            if (connecting)
                return;
            connecting = true;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connecting...");

            StartHost();
        }
        public void StartClient_Button()
        {
            if (connecting)
                return;
            connecting = true;

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

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerAllocations - 1);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                GameManager.singleton.currentJoinCode = joinCode;

                RelayServerData relayServerData = new(allocation, "dtls");
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
            if (joinCode == null || joinCode == "")
            {
                ConnectionFailure("Please Input Join Code");
                return;
            }

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                RelayServerData relayServerData = new(joinAllocation, "dtls");
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
            connecting = false;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText(message);
        }

        //========================================================================
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

namespace UIState
{
    public class ConnectOnline : StateBase
    {
        public TextMeshProUGUI connectionFeedbackText;

        public override void OnEnter()
        {
            base.OnEnter();
            connectionFeedbackText.gameObject.SetActive(false);

            FindObjectOfType<NetworkManager>().OnTransportFailure += ConnectionFailure;
        }
        public override void OnExit()
        {
            base.OnExit();

            FindObjectOfType<NetworkManager>().OnTransportFailure -= ConnectionFailure;
        }

        public void StartHost_Button()
        {
            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Loading..."); 
            Invoke(nameof(StartHost), .05f);

        }
        public void StartClient_Button()
        {
            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Loading...");
            Invoke(nameof(StartClient), .05f);
        }
        void StartHost()
        {
            FindObjectOfType<NetworkManager>().StartHost();
        }
        void StartClient()
        {
            FindObjectOfType<NetworkManager>().StartClient();
        }
        void ConnectionFailure()
        {
            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connection Failure");
        }
    }
}

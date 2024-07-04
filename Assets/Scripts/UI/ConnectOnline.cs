using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEditor.MemoryProfiler;

namespace UIState
{
    public class ConnectOnline : StateBase
    {
        //========================================================================
        public TextMeshProUGUI connectionFeedbackText;

        bool connecting = false;

        //========================================================================
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

        //========================================================================
        public void StartHost_Button()
        {
            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connecting..."); 
            Invoke(nameof(StartHost), .05f);

        }
        public void StartClient_Button()
        {
            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connecting...");
            Invoke(nameof(StartClient), .05f);
        }
        void StartHost()
        {
            if (connecting)
                return;
            connecting = true;

            FindObjectOfType<NetworkManager>().StartHost();
        }
        void StartClient()
        {
            if (connecting)
                return;
            connecting = true;

            FindObjectOfType<NetworkManager>().StartClient();
        }

        //========================================================================
        void ConnectionFailure()
        {
            connecting = false;

            connectionFeedbackText.gameObject.SetActive(true);
            connectionFeedbackText.SetText("Connection Failure");
        }

        //========================================================================
    }
}

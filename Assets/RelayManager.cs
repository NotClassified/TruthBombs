using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class RelayManager : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

}

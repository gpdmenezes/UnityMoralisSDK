using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MoralisWeb3ApiSdk;
using Moralis.Platform.Objects;
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.Unity;

public class GameManager : MonoBehaviour {

    [Header("Moralis Setup")]
    [SerializeField] MoralisController moralisController = null;
    [SerializeField] WalletConnect walletConnect = null;

    [Header("UI Setup")]
    [SerializeField] GameObject QRCodePanel = null;
    [SerializeField] Text walletAddressText = null;

    private async void Start() {
        if (moralisController != null) {
            await moralisController.Initialize();
        } else {
            Debug.LogError("Couldn't find MoralisController.");
        }
    }

    private void OnDisable() {
        LogOut();
    }

    public async void WalletConnectHandler (WCSessionData sessionData) {
        string address = sessionData.accounts[0].ToLower();
        string appID = MoralisInterface.GetClient().ApplicationId;
        long serverTime = 0;

        Dictionary<string, object> serverTimeResponse = await MoralisInterface.GetClient().Cloud.RunAsync<Dictionary<string, object>>("getServerTime", new Dictionary<string, object>());
        
        if (serverTimeResponse == null || !serverTimeResponse.ContainsKey("dateTime") || !long.TryParse(serverTimeResponse["dateTime"].ToString(), out serverTime)) {
            Debug.Log("Couldn't retrieve Server Time from Moralis Server.");
        }

        Debug.Log("Sending sign request for " + address);

        string signMessage = $"Moralis Authentication\n\nId: {appID}:{serverTime}";
        string response = await walletConnect.Session.EthPersonalSign(address, signMessage);
        Debug.Log($"Signature {response} for {address} was returned.");

        Dictionary<string, object> authData = new Dictionary<string, object> { { "id", address }, { "signature", response }, { "data", signMessage } };
        Debug.Log("Logging in user.");

        var user = await MoralisInterface.LogInAsync(authData);

        if (user != null) {
            UserLoggedInHandler();
            Debug.Log($"User {user.username} logged in successfully. ");
        } else {
            Debug.Log("Couldn't log in user.");
        }
    }

    async void UserLoggedInHandler () {
        QRCodePanel.SetActive(false);
        var user = await MoralisInterface.GetUserAsync();
        if (user != null) {
            string address = user.authData["moralisEth"]["id"].ToString();
            walletAddressText.text = address;
            walletAddressText.gameObject.SetActive(true);
        }
    }

    async void LogOut () {
        Debug.Log("Logging out.");
        await walletConnect.Session.Disconnect();
        walletConnect.CLearSession();
        await MoralisInterface.LogOutAsync();
    }

}

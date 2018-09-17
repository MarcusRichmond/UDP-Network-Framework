using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//Script to control the very basic UI we have going on
public class NetworkConnectUI : MonoBehaviour {
    [SerializeField] private InputField nameInput;
    [SerializeField] private InputField ipInput;
    [SerializeField] private Text udpPacketText;


    //Takes the input from the field and connects to the server
    public void ConnectToServerButtonPressed() {
        GameNetworkHandler.instance.StartAndConnectClient(ipInput.text, nameInput.text);
        this.gameObject.SetActive(false);
    }


    //Starts the server
    public void StartServerButtonPressed() {
        GameNetworkHandler.instance.StartServer();
        for (int i = 0; i < this.transform.childCount; i++) {
            this.transform.GetChild(i).gameObject.SetActive(false);
        }
    }


    //If we want to we can hook this up to a text box to display UDP packages in game
    public void DisplayUdpPacketData(string data) {
        string currentUiText = udpPacketText.text;
        udpPacketText.text = data;
    }
}

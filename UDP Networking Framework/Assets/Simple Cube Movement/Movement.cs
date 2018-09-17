using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpSpeed;

    private bool isLocalPlayer;

    private Rigidbody rBody;

    private void Start () {
        rBody = GetComponent<Rigidbody>();
    }


    //Gets the input and see if its notattatched to the localPlayer. If so Destroy itself
    private void Update() {
        if (GameNetworkHandler.instance.serverStarted && !GameNetworkHandler.instance.isServer) {
            if (!isLocalPlayer) {
                NetworkPlayer playa = GameNetworkHandler.instance.FindPlayer(this.gameObject);
                if (playa != null) {
                    // Not Gonna Work Cause Player is Spawned then the Local Player is Set 
                    if (!playa.Equals(GameNetworkHandler.instance.myLocalPlayer)) {
                        Destroy(this);
                    }
                    else {
                        isLocalPlayer = true;
                    }
                }
            }
            SendInput();
        }
    }


    //Sends player input to server
    private void SendInput() {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float jump = Input.GetAxis("Jump");
             NetworkCommands.instance.MOVEINPUT(h, v, jump);
    }


    //Applys that input once it reaches the server
    public void ServerMove(float horizontal, float vertical, float jump) {
        if (GameNetworkHandler.instance.isServer && GameNetworkHandler.instance.serverStarted) {
            Vector3 forceVector = new Vector3(horizontal * movementSpeed, jumpSpeed * jump, vertical * movementSpeed);
            rBody.AddForce(forceVector);
        }
    }
}

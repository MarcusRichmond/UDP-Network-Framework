using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(NetworkObject))]
public class ProjectileSpawner : MonoBehaviour {

    private Camera cam;


    //Initialization
    private void Awake() {
        GameNetworkHandler.instance.networkStartDelegate += DestroySelf;
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update () {
		if (Input.GetButtonDown("Fire1")) {
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z);
            Vector3 mouseWorldPoint = cam.ScreenToWorldPoint(mousePos);
            Vector3 dir = mouseWorldPoint - transform.position;
            NetworkCommands.instance.ATK(dir);
        }
	}

    //Destroy self when collides with something if isServer
    private void DestroySelf() {
        if (GameNetworkHandler.instance.isServer) {
            Destroy(this);
        }
        NetworkObject netObj = GetComponent<NetworkObject>();
        Debug.Log("Destroy Self Is being called do i have a netowrk object? " + netObj);
        if (GameNetworkHandler.instance.myLocalPlayer != null) {
            Debug.Log("I have a netObj " + netObj);
            if (netObj.ID != GameNetworkHandler.instance.myLocalPlayer.ID) {
                Destroy(this);
            }
        }
    }

    private void OnDestroy() {
        GameNetworkHandler.instance.networkStartDelegate -= DestroySelf;
    }
}
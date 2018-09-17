using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationSync : MonoBehaviour {

    //This function is to sync a objects location across the network not there velocity.
    //Velocity syncing is handled by another script


    [SerializeField] private float updatesPerSecond;

    private Rigidbody rBody;
    private Vector3 nextPos;
    private Quaternion nextRot;
    private float currentPercent = 0f;
    private float lastRecievedtime = 0f;


    //Initialize get required objects
    private void Awake () {
        rBody = GetComponent<Rigidbody>();
        GameNetworkHandler.instance.networkStartDelegate += StartSync;
    }

    //StartSyncing when the server tells us its ready
    private void StartSync() {
        Debug.Log("Start Sync Is Being Called");
        if (!GameNetworkHandler.instance.isServer) {
            if (rBody != null) {
                Destroy(rBody);
            }
        } else {
            StartCoroutine(SendPosition());
        }
    }


    //Send position IEnumerator since its done every (1f / updatesPerSecond)
    private IEnumerator SendPosition() {
        if (GameNetworkHandler.instance.FindNetworkObject(gameObject) != null) {
            NetworkCommands.instance.POS(GetComponent<NetworkObject>(), gameObject.transform.position, gameObject.transform.rotation);
            yield return new WaitForSeconds(1f / updatesPerSecond);
            StartCoroutine(SendPosition());
        }
    }


    //Recieve position from server if is the client
    public void RecievePosition(Vector3 pos, Quaternion rot, float time) {
        if (!GameNetworkHandler.instance.isServer && time > lastRecievedtime) {
            lastRecievedtime = time;
            nextPos = pos;
            nextRot = rot;
        }
    }

    //Smooths the position from the current to the one the server gave us
    private void Update() {
        if (!GameNetworkHandler.instance.isServer) {
            currentPercent += Time.deltaTime / updatesPerSecond;
            transform.position = Vector3.Lerp(transform.position, nextPos, currentPercent);
            transform.rotation = Quaternion.Lerp(transform.rotation, nextRot, currentPercent);
        }
    }

    //OnDestroy remove self from the list and stop all the Coroutines
    private void OnDestroy() {
        GameNetworkHandler.instance.networkStartDelegate -= StartSync;
        StopAllCoroutines();
    }
}

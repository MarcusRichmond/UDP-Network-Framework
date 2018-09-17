using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This Class despawns a gameobject over the network after a certain amount of time
public class DespawnAfter : MonoBehaviour {
    [SerializeField] private float despawnAfter;

    private void Start() {
        if (GameNetworkHandler.instance.isServer) {
            Invoke("DIE", despawnAfter);
        } else {
            Destroy(this);
        }
    }

    private void DIE() {
        NetworkCommands.instance.DESPAWN(gameObject.GetComponent<NetworkObject>());
    }

}

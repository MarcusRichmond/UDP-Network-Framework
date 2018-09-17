using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    public GameObject owner;

    [SerializeField] private float speed;

    private Rigidbody rBody;

	// Use this for initialization
	void Start () {
        rBody = GetComponent<Rigidbody>();	
	}

    private void Update() {
        if (GameNetworkHandler.instance.isServer) {
            rBody.AddForce(transform.forward * speed);
        }
    }

    
    private void OnCollisionStay(Collision collision) {

    }
}

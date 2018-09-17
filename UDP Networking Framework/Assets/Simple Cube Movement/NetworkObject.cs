using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This class is a identifier for network objects
[System.Serializable]
public class NetworkObject : MonoBehaviour {
    public int ID;
    public float lastRecievedTime = 0f;
    public GameObject instance = null;
}

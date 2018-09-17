using System;
using UnityEngine;

public class ServerOnly : Attribute {
    public ServerOnly() {
        Debug.Log("This Should Only Be Called On Server");
        
    }
}

class MainClass {
    static void Main() {
        typeof(MainClass).GetCustomAttributes(false);
    }
}

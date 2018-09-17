using System.Net;
using System.Net.Sockets;
using UnityEngine;


//This class is an identifier for networkPlayer objects
[System.Serializable]
public class NetworkPlayer : NetworkObject {
    public IPEndPoint playerEndpoint = null;
    public TcpClient playerTcp = null;
    public string playerName = null;

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

//Not quite sure how to do this but i think data will be
// |JoeShmoe:POS:X,Y,Z:X,Y,Z,W|
//stop name command location followed by quaternion rotation stop
// |JoeShmoe:ATK:X,Y,Z:,X,Y,Z,W|
//stop name  command   location hit on my end stop
// bit at end cause it returned true then we check again on server
// |SERVER:ATKRES:1:30:0:NAME|
// this is server replying on ATK you got a hit 1 did 30 damage no kill 0
// server is always right if player returned 0 but server gets 1 its a hit and vice versa


public class GameNetworkHandler : MonoBehaviour {

    [SerializeField]private int serverRefreshRate;

    //Inspector Variables
    public List<GameObject> networkSpawnGameObjects;
    public NetworkPlayer myLocalPlayer;
    public int localPlayerID;

    // Non Inspector Variables
    public IPAddress serverIP;
    public IPEndPoint serverEndpoint;
    public static GameNetworkHandler instance;
    
    public bool isServer { get { return m_isServer; } }
    private bool m_isServer;
    
    public bool serverStarted { get { return m_serverStarted; } }
    private bool m_serverStarted;

    public bool isConnected { get { return m_isConnected; } }
    private bool m_isConnected;
    public bool isConnecting;

    public delegate void NetworkStartDelegate();
    public NetworkStartDelegate networkStartDelegate;

    public float startTime { get { return m_startTime;  } }
    private float m_startTime;


    public List<NetworkPlayer> players;

    //Non Player Network Sync Objects Players are going to be on this list as well
    public List<NetworkObject> networkObjects;

    private UdpClient UdpConnection;

    private int currentID;

    //Add commands to this list then send every HZ
    private List<string> QueuedCommands;


    [HideInInspector] public NetworkSpawnLocation[] spawnLocations;

    //Shared initialization stuff
    private void Awake() {
        QueuedCommands = new List<string>();
        QueuedCommands.Add("");
        localPlayerID = -1;
        Application.runInBackground = true;
        instance = this;
        spawnLocations = FindObjectsOfType<NetworkSpawnLocation>();
        players = new List<NetworkPlayer>();
        networkObjects = new List<NetworkObject>();
        myLocalPlayer = new NetworkPlayer();
    }

    //Done once every frame
    private void Update() {
        if (isConnecting && !isConnected) {
            //NetworkCommands.instance.PLAYERINFO(myLocalPlayer.playerName);
        } if (isConnected) {
            StopCoroutine(SendPlayerData(myLocalPlayer.playerName));
        }
        if (!isServer && !isConnected && serverStarted) {

        } if (isServer) {//Disconnect players if they timeout
            for (int i = 0; i < players.Count; i++) {
                if ((Time.time - players[i].lastRecievedTime) >= 10f) {
                    Debug.Log(players[i].playerName + " has timed out and disconnected what a duck");
                    RemovePlayer(players[i]);
                }
            }
        } if (localPlayerID != -1) {
            NetworkPlayer playa = FindPlayer(localPlayerID);
            if (playa != null) {
                myLocalPlayer = playa;
                localPlayerID = -1;
            }
        }
    }

    private IEnumerator CallNetworkStartDelegate() {
        yield return new WaitForEndOfFrame();
        networkStartDelegate();
    }

    //Server initialization stuff
    public void StartServer() {
        this.m_isServer = true;
        m_startTime = Time.time;
        UdpConnection = new UdpClient(GlobalVariables.port); // Server needs a specified port
        UdpConnection.BeginReceive(OnRecieve, null);
        instance.m_serverStarted = true;
        serverIP = IPAddress.Parse("127.0.0.1");
        serverEndpoint = new IPEndPoint(serverIP, GlobalVariables.port);
        if (networkStartDelegate != null) {
            networkStartDelegate();
        }
        StartCoroutine(BroadCastData());
    }

    //Client initialization stuff
    public void StartAndConnectClient(string ip, string playerName) {
        myLocalPlayer = new NetworkPlayer();
        myLocalPlayer.playerName = playerName;
        this.m_isServer = false;
        serverIP = IPAddress.Parse(ip);
        UdpConnection = new UdpClient();//Auto bind port for client
        serverEndpoint = new IPEndPoint(serverIP, GlobalVariables.port);
        UdpConnection.BeginReceive(OnRecieve, null);
        instance.m_serverStarted = true;
        StartCoroutine(SendPlayerData(playerName));
        if (networkStartDelegate != null) {
            StartCoroutine(CallNetworkStartDelegate());
        }
        isConnecting = true;
    }

    //Send players name to the server
    private IEnumerator SendPlayerData(string playerName) {
        NetworkCommands.instance.PLAYERINFO(playerName);
        yield return new WaitForSeconds(0.5f);
        if (!isConnected) {
            StartCoroutine(SendPlayerData(playerName));
        }
    }

    //Adds a new player to the list
    public void AddNetworkPlayer(NetworkPlayer player) {
        Debug.Log("Adding " + player.playerName);
        player.lastRecievedTime = Time.time;
        GameNetworkHandler.instance.players.Add(player);;
        AddNetworkObject(player);
        if (networkStartDelegate != null) {
            StartCoroutine(CallNetworkStartDelegate());
        }
        Debug.Log("There is now this many players " + players.Count);
    }

    //Adds a new network object to the list. Everything that is synced over the network is a network object including Players but not everything is a player.
    public void AddNetworkObject(NetworkObject obj) {
        GameNetworkHandler.instance.networkObjects.Add(obj);
        if (networkStartDelegate != null) {
            StartCoroutine(CallNetworkStartDelegate());
        }
    }

    //Removes a player from the server and clients
    public void RemovePlayer(NetworkPlayer playerToRemove) {
        //NetworkCommands.instance.DESPAWN(playerToRemove);
        //Gotta do it by ip otherwise you can have people telling the server to boot everyone
        players.Remove(players.Find(q => q.playerEndpoint == playerToRemove.playerEndpoint));
        Destroy(playerToRemove.instance);
    }

    //Removes a network object from the server and clients
    public void RemoveNetObj(NetworkObject netObj) {
        networkObjects.Remove(networkObjects.Find(q => q.ID == netObj.ID));
        Destroy(netObj.instance);
    }

    //Return a player using the ipendpoint
    public NetworkPlayer FindPlayer(IPEndPoint ipEndpoint) {
        if (players.Count > 0) {
            for (int i = 0; i < GameNetworkHandler.instance.players.Count; i++) {
                //The ip endpoint doesn't exist so you can't compare them on the client
                if (GameNetworkHandler.instance.players[i].playerEndpoint.Equals(ipEndpoint)) {
                    return GameNetworkHandler.instance.players[i];
                }
            }
            Debug.Log("Couldn't Find Player:" + ipEndpoint + " Looped through: " + players.Count + " Returning Null");
            return null;
        } else {
            return null;
        }
    }

    //Return a player using an ID
    public NetworkPlayer FindPlayer(int ID) {
        if (players.Count > 0) {
            for (int i = 0; i < GameNetworkHandler.instance.players.Count; i++) {
                if (GameNetworkHandler.instance.players[i].ID.Equals(ID)) {
                    return GameNetworkHandler.instance.players[i];
                }
            }
            return null;
        }else {
            return null;
        }
    }

    //Return a player using a gameObject
    public NetworkPlayer FindPlayer(GameObject playerInstance) {
        if (players.Count > 0) {
            for (int i = 0; i < GameNetworkHandler.instance.players.Count; i++) {
                if (GameNetworkHandler.instance.players[i].instance.Equals(playerInstance)) {
                    return GameNetworkHandler.instance.players[i];
                }
            }
            Debug.Log("Couldn't Find Player:" + playerInstance + " Looped through: " + players.Count + " Returning Null");
            return null;
        }else {
            return null;
        }
    }

    //Return a network object using a ID
    public NetworkObject FindNetworkObject(int ID) {
        NetworkObject obj = networkObjects.Find(q => q.ID == ID);
        if (obj != null) {
            return obj;
        } else {
            return null;
        }
    }

    //Return a network object using a gameObject
    public NetworkObject FindNetworkObject(GameObject instance) {
        NetworkObject obj = networkObjects.Find(q => q.instance == instance);
        if (obj != null) {
            return obj;
        }
        else {
            return null;
        }
    }

    //Return a networkSpawnGameObject using a name
    public GameObject FindNetworkSpawnGameObject(string name) {
        for (int i = 0; i < networkSpawnGameObjects.Count; i++) {
            if (networkSpawnGameObjects[i].name == name) {
                return networkSpawnGameObjects[i];
            }
        }
        return null;
    }

    //Queue up string commands to send every (1f/serverRefreshRate) seconds
    public void QueueBroadCastData(string message) {
        if (QueuedCommands.Count > 0) {
            if (QueuedCommands[QueuedCommands.Count-1].Length < 256) {
                QueuedCommands[QueuedCommands.Count-1] += message;
            }
            else {
                QueuedCommands.Add(message);
            }
        } else {
            QueuedCommands.Add(message);
        }
    }

    //Broadcast all the queued commands to all the players
    private IEnumerator BroadCastData() {
        if (isServer && serverStarted) {
            if (GameNetworkHandler.instance.players != null) {
                foreach (NetworkPlayer end in GameNetworkHandler.instance.players) {
                    for (int q = 0; q < QueuedCommands.Count; q++) {
                        SendUDP(QueuedCommands[q], end.playerEndpoint);
                    } 
                }
            }
        }
        QueuedCommands.RemoveAll(item => true);
        yield return new WaitForSeconds(1f/serverRefreshRate);
        StartCoroutine(BroadCastData());
    }

    //When we are sending a command directly to a specific player we dont queue it we just send it.
    public void SendUDP(string message, IPEndPoint recipient) {
        byte[] data = Encoding.ASCII.GetBytes(message);
        UdpConnection.Send(data, data.Length, recipient);
    }


    //Recieve UDP data
    void OnRecieve(IAsyncResult ar) {
        try {
            IPEndPoint ipEndpoint = null;
            byte[] data = UdpConnection.EndReceive(ar, ref ipEndpoint);

            //AddPlayer(ipEndpoint);

            string message = Encoding.ASCII.GetString(data);
            UnityMainThreadDispatcher.Instance().Enqueue(ProcessRecievedUdpData(message, ipEndpoint));
        }
        catch (SocketException e) {
            // this happens when a client disconnects as we fail to send a port
            Debug.LogWarning("This has happened " + e);
        }

        UdpConnection.BeginReceive(OnRecieve, null);
    }

    
    //Process the recieved UDP data
    private IEnumerator ProcessRecievedUdpData(string data, IPEndPoint sender) {
        string[] commands = data.Split('|');
        if (isServer) {
            NetworkPlayer playa = FindPlayer(sender);
            if (playa != null) {
                playa.lastRecievedTime = Time.time;
            }
        }
        for (int i = 0; i < commands.Length; i++) {
            Command cmd = new Command(commands[i], sender);
            if (isServer) { // server recieves commands from client so thats why these are all CLIENT_COMMANDS
                if (cmd.commanddName == CLIENT_COMMANDS.ATK) {
                    NetworkCommands.instance.RECIEVE_ATK(cmd);
                }
                if (cmd.commanddName == CLIENT_COMMANDS.MOVEINPUT) {
                    NetworkCommands.instance.RECIEVE_MOVEINPUT(cmd);
                } if (cmd.commanddName == CLIENT_COMMANDS.PLAYERINFO) {
                    NetworkPlayer player = NetworkCommands.instance.RECIEVE_PLAYERINFO(cmd);
                } if (cmd.commanddName == CLIENT_COMMANDS.DISCONNECT) {
                    NetworkCommands.instance.RECIEVE_DISCONNECT(cmd);
                }
            } else { // client recieves commands from server so thats why these are all SERVER_COMMANDS
                if (cmd.commanddName == SERVER_COMMANDS.POS) {
                    NetworkCommands.instance.RECIEVE_POS(cmd);
                } if (cmd.commanddName == SERVER_COMMANDS.SPAWNPLAYER) {
                    isConnecting = false;
                    m_isConnected = true;
                    AddNetworkPlayer(NetworkCommands.instance.RECIEVE_SPAWNPLAYER(cmd));
                } if (cmd.commanddName == SERVER_COMMANDS.DESPAWN) {
                    NetworkCommands.instance.RECIEVE_DESPAWN(cmd);
                } if (cmd.commanddName == SERVER_COMMANDS.LOCALPLAYERID) {
                    NetworkCommands.instance.RECIEVE_LOCALPLAYERID(cmd);
                } if (cmd.commanddName == SERVER_COMMANDS.SPAWNNETWORKOBJ) {
                    AddNetworkObject(NetworkCommands.instance.RECIEVE_SPAWNNETWORKOBJ(cmd));
                }
            }
        }
        yield return null;
    }

    //Get the next Players ID
    public int GetID() {
        currentID++;
        return currentID;
    }

    //Get the next spawn location to give to the player
    public Transform GetNextSpawnPos() {
        return spawnLocations[UnityEngine.Random.Range(0, spawnLocations.Length)].transform;
    }


    // Shut the server down when we quit
    private void OnApplicationQuit() {
        if (!isServer) {
            NetworkCommands.instance.DISCONNECT(myLocalPlayer);
        }
    }
}

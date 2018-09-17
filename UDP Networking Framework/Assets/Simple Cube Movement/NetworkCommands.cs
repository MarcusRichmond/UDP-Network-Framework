using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;


public class NetworkCommands : MonoBehaviour {

    public static NetworkCommands instance;
    void Awake() {
        instance = this;
    }

    /**************************************************************************************************
     Command Structure Reference
     Give the command just the raw command object and not the hand selected data then if you want to add a command its super easy. Don't even have to touch the GameNetworkHandler just this.
     Each command has a DEFAULT and a RECIEVE_DEFAULT the is the send and the RECIEVE_DEFAULT is the result
     **************************************************************************************************/

    //Commands that the Client calls and sends to the Server
    #region Client Commands

    public void PLAYERINFO(string name) {
        string data = name + ":" + CLIENT_COMMANDS.PLAYERINFO + "|";
        GameNetworkHandler.instance.SendUDP(data, GameNetworkHandler.instance.serverEndpoint);
    }

    public void MOVEINPUT(float horizontal, float vertical, float jump) {
        string message = GameNetworkHandler.instance.myLocalPlayer.playerName + ":" + CLIENT_COMMANDS.MOVEINPUT + ":" + horizontal.ToString() + ":" + vertical.ToString() + ":" + jump.ToString() + "|";
        GameNetworkHandler.instance.SendUDP(message, GameNetworkHandler.instance.serverEndpoint);
    }

    //NAME:DISCONNECT:ID|
    public void DISCONNECT (NetworkPlayer player) {
        string message = player.playerName + ":" + CLIENT_COMMANDS.DISCONNECT + ":" + player.ID +"|";
        GameNetworkHandler.instance.SendUDP(message, GameNetworkHandler.instance.serverEndpoint);
    }

    //NAME:ATK:DIRECTION|
    public void ATK(Vector3 dir) {
        if (GameNetworkHandler.instance.myLocalPlayer != null) {
            string message = GameNetworkHandler.instance.myLocalPlayer.playerName + ":" + CLIENT_COMMANDS.ATK + ":" + HelperFunctions.ConvertVector3ToString(dir) + "|";
            GameNetworkHandler.instance.SendUDP(message, GameNetworkHandler.instance.serverEndpoint);
        }
    }


    #region End Recieve Client Commands

    //JoeShmoe:MOVEINPUT:HOR:VERT:JUMP|
    public void RECIEVE_MOVEINPUT(Command cmd) {
        float horizontal = float.Parse(cmd.commandData[0]);
        float vertical = float.Parse(cmd.commandData[1]);
        float jump = float.Parse(cmd.commandData[2]);
        GameObject obj = GameNetworkHandler.instance.FindPlayer(cmd.sender).instance;
        if (obj != null) { 
            Movement movement = obj.GetComponent<Movement>();
            movement.ServerMove(horizontal, vertical, jump);
        }
    }

    public NetworkPlayer RECIEVE_PLAYERINFO(Command cmd) {
        GameObject playerObj = null;
        if (GameNetworkHandler.instance.FindPlayer(cmd.sender) == null) {
            Debug.Log("Recieved Palyer Info trying to decide what to do with it");
            GameNetworkHandler.instance.isConnecting = false;
            string playerName = cmd.invokerName;
            //Before we generate the new player send the old ones
            for (int i = 0; i < GameNetworkHandler.instance.players.Count; i++) {
                if (GameNetworkHandler.instance.players[i].playerEndpoint != null) {
                //This stuff is being called correctly and is spawning the other players but not the connecting player
                    SPAWNPLAYER_SINGLE(GameNetworkHandler.instance.players[i].playerName, GameNetworkHandler.instance.players[i].ID, cmd.sender);
                }
            }
            int ID = GameNetworkHandler.instance.GetID();
            playerObj = NetworkCommands.instance.SPAWNPLAYER_BROADCAST(playerName, ID);// we spawn the player before he is on the list possible bug
            NetworkPlayer playa = playerObj.GetComponent<NetworkPlayer>();
            playa.ID = ID;
            playa.playerEndpoint = cmd.sender;
            playa.playerName = playerName;
            playa.instance = playa.gameObject;
            //This Is the problem
            GameNetworkHandler.instance.AddNetworkPlayer(playa);
            //Because of the syncing this the player is being added to the list and then he is sent to spawn himself twice which is broken gotta fix this
            //SPAWNPLAYER_SINGLE(playerName, ID, cmd.sender);
            //Doing this doesnt work because this is sent way before the other one and so there is no player to set to local player
            LOCALPLAYERID(ID, cmd.sender);
            // we arent sending the info to the player cause its no on the list
            return playa;
        } else {
            return null;
        }
    }

    public void RECIEVE_DISCONNECT (Command cmd) {
        NetworkPlayer playa;
        playa = GameNetworkHandler.instance.FindPlayer(cmd.sender);
        if (playa != null) {
            GameNetworkHandler.instance.RemovePlayer(playa);
        }
    }

    public void RECIEVE_ATK(Command cmd) {
        //This Wont Do anything on the CLient
        Vector3 dir = HelperFunctions.ConvertStringToVector3(cmd.commandData[0]);
        NetworkPlayer owner = GameNetworkHandler.instance.FindPlayer(cmd.sender);
        Vector3 pos = owner.instance.transform.position;
        SPAWNNETWORKOBJ_BROADCAST(GameNetworkHandler.instance.FindNetworkSpawnGameObject("Projectile"), pos, Quaternion.LookRotation(dir, Vector3.up), "Projectile");
    }

    #endregion

    #endregion

    //Commands that the Server Calls and sends to the Client
    #region Server Commands
        //SERVER:SPAWNPLAYER:JoeShmoe:32:Position:Rotation|
    public GameObject SPAWNPLAYER_BROADCAST(string playerName, int ID) {
        Transform spawnTrans = GameNetworkHandler.instance.GetNextSpawnPos();
        string message = "SERVER" + ":" +SERVER_COMMANDS.SPAWNPLAYER + ":" + playerName + ":" + ID + ":" + HelperFunctions.ConvertVector3ToString(spawnTrans.position) + ":" + HelperFunctions.ConvertQuatToString(spawnTrans.rotation) +  "|";
        GameNetworkHandler.instance.QueueBroadCastData(message);
        GameObject player = Instantiate(GameNetworkHandler.instance.FindNetworkSpawnGameObject("Player"), spawnTrans.position, spawnTrans.rotation);
        player.name = playerName;
        return player;
    }

    //SERVER:SPAWNPLAYER:JoeShmoe:32:Position:Rotation:IsLocalPlayer|
    public void SPAWNPLAYER_SINGLE(string playerName, int ID, IPEndPoint recipeint) {
        Transform spawnTrans = GameNetworkHandler.instance.GetNextSpawnPos();
        string message = "SERVER" + ":" + SERVER_COMMANDS.SPAWNPLAYER + ":" + playerName + ":" + ID + ":" + HelperFunctions.ConvertVector3ToString(spawnTrans.position) + ":" + HelperFunctions.ConvertQuatToString(spawnTrans.rotation) + "|";
        GameNetworkHandler.instance.SendUDP(message, recipeint);
    }


    //SERVER:SPAWNNETWORKOBJ:Bullet:0,1.5,3:0.5,1,0,0.75|
    public void SPAWNNETWORKOBJECTOBJ_SINGLE(GameObject toSpawn, Vector3 pos, Quaternion rot, string objectName, IPEndPoint recipient) {
        if (GameNetworkHandler.instance.FindNetworkSpawnGameObject(toSpawn.name) != null) {
            string message = "SERVER" + ":" + SERVER_COMMANDS.SPAWNNETWORKOBJ + ":" + toSpawn.name + ":" + HelperFunctions.ConvertVector3ToString(pos) + ":" + HelperFunctions.ConvertQuatToString(rot) + ":" + objectName +  "|";
            GameNetworkHandler.instance.SendUDP(message, recipient);
        }
        else {
            Debug.LogWarning("Object not in networkSpawnObject list must be added to the build");
        }
    }

    //                              ID   POS      ROT
    //SERVER:SPAWNNETWORKOBJ:Bullet:69:0,1.5,3:0.5,1,0,0.75|
    public GameObject SPAWNNETWORKOBJ_BROADCAST(GameObject toSpawn, Vector3 pos, Quaternion rot, string objectName) {
        if (GameNetworkHandler.instance.FindNetworkSpawnGameObject(toSpawn.name) != null) {
            int ID = GameNetworkHandler.instance.GetID();
            GameObject spawnedObject = Instantiate(toSpawn, pos, rot);
            spawnedObject.name = objectName;
            NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
            netObj.ID = ID;
            netObj.name = spawnedObject.name;
            netObj.instance = netObj.gameObject;
            string message = "SERVER" + ":" + SERVER_COMMANDS.SPAWNNETWORKOBJ + ":" + toSpawn.name + ":" + ID + ":"+ HelperFunctions.ConvertVector3ToString(pos) + ":" + HelperFunctions.ConvertQuatToString(rot) + "|";
            GameNetworkHandler.instance.AddNetworkObject(netObj);
            GameNetworkHandler.instance.QueueBroadCastData(message);
            return spawnedObject;
        }
        else {
            Debug.LogWarning("Object not in networkSpawnObject list must be added to the build");
            return null;
        }
    }

    //SERVER:DESPAWN:JoseShmose:69|
    public void DESPAWN(NetworkObject netObj) {
        string message = "SERVER" + ":" + SERVER_COMMANDS.DESPAWN + ":" + netObj.instance.name + ":" + netObj.ID + "|";
        NetworkPlayer playa = GameNetworkHandler.instance.FindPlayer(netObj.ID);
        if (netObj != null) {
            GameNetworkHandler.instance.RemoveNetObj(netObj);
            if (playa != null) {
                GameNetworkHandler.instance.RemovePlayer(playa);
            }
        }
        GameNetworkHandler.instance.QueueBroadCastData(message);
    }

    // We should also just sync rigidbody velocity maybe
    // or have a whole new command that does rigibody sync stuff
    // which would only work on like an object that doesnt turn
    //SERVER:POS:Name:ID:Pos:Rot:Time
    public void POS(NetworkObject netObj, Vector3 pos, Quaternion rot) {
        string data = "SERVER" + ":" + SERVER_COMMANDS.POS + ":" + netObj.name + ":"+ netObj.ID +":" + HelperFunctions.ConvertVector3ToString(pos) + ":" + HelperFunctions.ConvertQuatToString(rot) + ":" + Time.time.ToString() +"|";
        GameNetworkHandler.instance.QueueBroadCastData(data);
    }

    //SERVER:VELOCITY:Bullet:ID:VELOCITY:TIME
    public void VELOCITY(NetworkObject netObj, Vector3 velocity) {
        string message = "SERVER:" + SERVER_COMMANDS.VELOCITY + ":" + netObj.name + ":" + netObj.ID + ":" + HelperFunctions.ConvertVector3ToString(velocity) + ":" + Time.time + "|";
        GameNetworkHandler.instance.QueueBroadCastData(message);
    }

    //SERVER:LOCALPLAYERID:96
    public void LOCALPLAYERID(int ID, IPEndPoint localPlayerEndpoint) {
        string message = "SERVER" + ":" + SERVER_COMMANDS.LOCALPLAYERID + ":" + ID + "|";
        GameNetworkHandler.instance.SendUDP(message, localPlayerEndpoint);
    }

    #region End Recieve Server Commands
    //SERVER:SPAWNNETWORKOBJ: Bullet:69:0,1.5,3:0.5,1,0,0.75|
    public NetworkObject RECIEVE_SPAWNNETWORKOBJ(Command cmd) {
        GameObject prefabToSpawn = GameNetworkHandler.instance.FindNetworkSpawnGameObject(cmd.commandData[0]);
        Vector3 spawnLocation = HelperFunctions.ConvertStringToVector3(cmd.commandData[2]);
        Quaternion spawnRotation = HelperFunctions.ConvertStringToQuat(cmd.commandData[3]);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnLocation, spawnRotation);
        spawnedObject.transform.name = cmd.commandData[0];
        NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
        netObj.ID = int.Parse(cmd.commandData[1]);
        netObj.instance = netObj.gameObject;
        return netObj;
    }

    //JoeShmoe:POS:JoeShmoe:Position:Rotation or SERVER:POS:Grenade:Position:Rotation|
    public void RECIEVE_POS(Command cmd) {
        string nameOfTransform = cmd.commandData[0];
        int ID = int.Parse(cmd.commandData[1]);
        float time = float.Parse(cmd.commandData[4]);
        Vector3 position = HelperFunctions.ConvertStringToVector3(cmd.commandData[2]);
        Quaternion rotation = HelperFunctions.ConvertStringToQuat(cmd.commandData[3]);
        
        if (GameNetworkHandler.instance.FindNetworkObject(ID) != null) {
            if (GameNetworkHandler.instance.FindNetworkObject(ID).instance != null) {
                GameObject obj = GameNetworkHandler.instance.FindNetworkObject(ID).instance;
                LocationSync sync = obj.GetComponent<LocationSync>();
                if (sync != null) {
                    sync.RecievePosition(position, rotation, time);
                }
            }
        }
    }
    //Recieves the VELOCITY command and assings it to rigidBody object with ID
    //SERVER:VELOCITY:Bullet:ID:VELOCITY:TIME
    public void RECIEVE_VELOCITY(Command cmd) {
        Vector3 velocity = HelperFunctions.ConvertStringToVector3(cmd.commandData[3]);
        int ID = int.Parse(cmd.commandData[2]);
    }

    //Spawns the player on the client
    //SERVER:SPAWNPLAYER:JoeShmoe:32:Position:Rotation|
    public NetworkPlayer RECIEVE_SPAWNPLAYER(Command cmd) {
        Debug.Log("Spawning Player");
        GameObject spawnedPlayer = Instantiate(GameNetworkHandler.instance.FindNetworkSpawnGameObject("Player"), HelperFunctions.ConvertStringToVector3(cmd.commandData[2]), HelperFunctions.ConvertStringToQuat(cmd.commandData[3]));
        spawnedPlayer.transform.name = cmd.commandData[0];
        NetworkPlayer player = spawnedPlayer.GetComponent<NetworkPlayer>();
        player.instance = spawnedPlayer;
        player.playerName = cmd.commandData[0];
        player.ID = int.Parse(cmd.commandData[1]);
        return player;
    }

    //Despawns the object with ID on the clients
    //SERVER:DESPAWN:NAME:ID|
    public void RECIEVE_DESPAWN(Command cmd) {
        string name = cmd.commandData[0];
        int ID = int.Parse(cmd.commandData[1]);
        NetworkObject netObj = GameNetworkHandler.instance.FindNetworkObject(ID);
        NetworkPlayer playa = GameNetworkHandler.instance.FindPlayer(ID);
        if (netObj!=null) {
            GameNetworkHandler.instance.RemoveNetObj(netObj);
            if (playa != null) {
                GameNetworkHandler.instance.RemovePlayer(playa);
            }
        }
    }

    //Recieves the ID of the localPlayer on the client so the client knows which player to control
    //SERVER:LOCALPLAYERID:96
    public void RECIEVE_LOCALPLAYERID(Command cmd) {
        int ID =  int.Parse(cmd.commandData[0]);
        NetworkPlayer playa = GameNetworkHandler.instance.FindPlayer(ID);
        GameNetworkHandler.instance.localPlayerID = ID;
        if (playa != null) {
            GameNetworkHandler.instance.myLocalPlayer = playa;
        }
    }

    #endregion

    #endregion
}


//Command Class to make it simple and easy to depackage UDP packages
public class Command {
    public IPEndPoint sender;
    public string invokerName;
    public string commanddName;
    public float commandTime;
    public string[] commandData;

    public Command(string dataPackage, IPEndPoint m_sender) {
        sender = m_sender;
        // if standard command protocol is followed we can jsut do all the data splitting here
        if (dataPackage.Contains(":")) {
            string[] m_Data = dataPackage.Split(':');
            invokerName = m_Data[0];
            commanddName = m_Data[1];
            commandData = new string[m_Data.Length - 2];
            for (int i = 0; i < m_Data.Length; i++) {
                if (i != 0 && i != 1) {
                    commandData[i - 2] = m_Data[i];
                }
            }
        }
    }
}

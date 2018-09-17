
public sealed class SERVER_COMMANDS {
    public static string POS = "POS";
    public static string VELOCITY = "VELOCITY";
    public static string ATKRES = "ATKRES";
    public static string TOGGLEOBJ = "TOGGLEOBJ";
    public static string PLAYERINFOREQ = "PLAYERINFOREQUEST";
    public static string PLAYERINFO = "PLAYERINFO";
    public static string SPAWN = "SPAWN";
    public static string SPAWNPLAYER = "SPAWNPLAYER";
    public static string SPAWNNETWORKOBJ = "SPAWNNETWORKOBJ";
    public static string DESPAWN = "DESPAWN";
    public static string LOCALPLAYERID = "LOCALPLAYERID";
}
//public enum CLIENT_COMMANDS { MOVEINPUT, ATK };

public sealed class CLIENT_COMMANDS {
    public static string MOVEINPUT = "MOVEINPUT";
    public static string ATK = "ATK";
    public static string INFOREQREPLY = "INFOREQREPLY";
    public static string PLAYERINFO = "PLAYERINFO";
    public static string DISCONNECT = "DISCONNECT";
}

public static class GlobalVariables {
    public static int port = 56789;
    
}

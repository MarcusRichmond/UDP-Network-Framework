using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;


//Couple helper functions that would just clutter up the other classes
static class HelperFunctions {
	
    //All of these functions literally do what the name suggests.


    public static int BoolIntValue(bool toConvert) {
        return toConvert ? 1 : 0;
    }

    public static Vector3 ConvertStringToVector3(string toConvert) {
        string[] axis = toConvert.Split(',');
        return new Vector3(float.Parse(axis[0]), float.Parse(axis[1]), float.Parse(axis[2]));
    }

    public static string ConvertVector3ToString(Vector3 toConvert) {
        string x = toConvert.x.ToString("0.000");
        string y = toConvert.y.ToString("0.000");
        string z = toConvert.z.ToString("0.000");
        return (x + "," + y + "," + z);
    }

    public static Quaternion ConvertStringToQuat(string toConvert) {
        string[] axis = toConvert.Split(',');
        for (int i = 0; i < axis.Length; i++) {
            Regex.Replace(axis[i], "[^.0-9]", "");
        }
        return new Quaternion(float.Parse(axis[0]), float.Parse(axis[1]), float.Parse(axis[2]), float.Parse(axis[3]));
    }

    public static string ConvertQuatToString(Quaternion toConvert) {
        string x = toConvert.x.ToString("0.0000");
        string y = toConvert.y.ToString("0.0000");
        string z = toConvert.z.ToString("0.0000");
        string w = toConvert.w.ToString("0.0000");
        return (x + "," + y + "," + z + "," + w);
    }
}

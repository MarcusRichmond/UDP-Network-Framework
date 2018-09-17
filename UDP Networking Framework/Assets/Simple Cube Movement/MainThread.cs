using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//Fixes the whole multithread thing thats going on with the serialization
//Honestly not quite sure how an IEnumerator can communicate between threads but it works and I'm going to leave it at that
public class MainThread : MonoBehaviour {

    class CallInfo {
        public Function func;
        public object parameter;
        public CallInfo(Function Func, object Parameter) {
            func = Func;
            parameter = Parameter;
        }
        public void Execute() {
            func(parameter);
        }
    }

    public delegate void Function(object parameter);
    public delegate void Func();

    static List<CallInfo> calls = new List<CallInfo>();
    static List<Func> functions = new List<Func>();

    static Object callsLock = new Object();
    static Object functionsLock = new Object();

    void Start() {
        calls = new List<CallInfo>();
        functions = new List<Func>();

        StartCoroutine(Executer());
    }

    public static void Call(Function Func, object Parameter) {
        lock (callsLock) {
            calls.Add(new CallInfo(Func, Parameter));
        }
    }
    public static void Call(Func func) {
        lock (functionsLock) {
            functions.Add(func);
        }
    }

    IEnumerator Executer() {
        while (true) {
            yield return new WaitForSeconds(0.01f);

            while (calls.Count > 0) {
                calls[0].Execute();
                lock (callsLock) {
                    calls.RemoveAt(0);
                }
            }

            while (functions.Count > 0) {
                functions[0]();
                lock (functionsLock) {
                    functions.RemoveAt(0);
                }
            }
        }
    }
}

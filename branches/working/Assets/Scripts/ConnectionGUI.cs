﻿using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Threading;
public class Trace : UnityEngine.Debug { }
public class ConnectionGUI : Base
{
    public string Nick { get { return PlayerPrefs.GetString("Nick"); } set { PlayerPrefs.SetString("Nick",value); } }
    const int port = 5300;
    public string ip { get { return PlayerPrefs.GetString("ip"); } set { PlayerPrefs.SetString("ip", value); } }
    private void InitServer()
    {
        Network.useNat = false;
        Network.InitializeServer(32, port);
        Connected();
    }
    
    protected override void OnGUI()
    {
        
        if (GUILayout.Button("Active"))  
            Screen.lockCursor = true;
        if (Network.peerType == NetworkPeerType.Disconnected)
        {

            ip = GUILayout.TextField(ip);

            if (GUILayout.Button("Connect") && Nick.Length > 0)
                Network.Connect(ip, port);
            Nick = GUILayout.TextField(Nick);

            if (GUILayout.Button("host") && Nick.Length > 0)
                InitServer();
        }
    } 

    protected override void OnConnectedToServer()
    { 
        Connected();                     
    }

    private void Connected()
    {
        
        foreach (GameObject go in FindObjectsOfType(typeof(GameObject)))
            go.SendMessage("OnNetworkLoadedLevel", SendMessageOptions.DontRequireReceiver);
    }
    protected override void OnApplicationQuit()
    {
        enabled = false;
    }

    protected override void OnDisconnectedFromServer()
    {
        if (enabled)
            Application.LoadLevel(Application.loadedLevel);
    }
}
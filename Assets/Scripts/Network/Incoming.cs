//#define SERVER_BUILD

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

[RequireComponent(typeof(NetworkView))]
public class Incoming : MonoBehaviour
{
    class PlayerInfo
    {
        public struct TransmittedData
        {
            public String name;
            public String data;

            public string PrettyText()
            {
                return data;
            }

            public string XMLString()
            {
                //return "DATA:<" + data + ">";
                return data;
            }
        }

        public string playerName;
        public string userid;
        public string sessionid;
        public string filePath;
        public bool overwrite;
        public StreamWriter writer;
        public NetworkPlayer player;
        public NetworkViewID playerID;
        public List<TransmittedData> data;
        public List<TransmittedData> log;

        public PlayerInfo()
        {
            Init();
        }

        public PlayerInfo(NetworkPlayer player, NetworkViewID playerID)
        {
            this.player = player;
            this.playerID = playerID;
            Init();
        }

        public void Init()
        {
            // written to screen
            log = new List<TransmittedData>();
            // written to file
            data = new List<TransmittedData>();
            //
            overwrite = false;
        }

        public void AddLog(String name, String data)
        {
            // this could be a dictionary
            TransmittedData newData = new TransmittedData();
            newData.name = name;
            newData.data = System.DateTime.Now + " : " + data;
            this.log.Add(newData);
        }

        public void AddData(String name, String data)
        {
            TransmittedData newData = new TransmittedData();
            newData.name = name;
            newData.data = data;
            this.data.Add(newData);
        }

        TransmittedData block;

        public void BlockStart(String name, bool overwrite)
        {
            // start the block
            block = new TransmittedData();
            block.name = name;
            // set writing flag
            this.overwrite = overwrite;
        }

        public void BlockAdd(String name, String data)
        {
            block.data += data;
        }

        public void BlockEnd(String name)
        {
            this.data.Add(block);
        }
    };

    List<PlayerInfo> playerInfo;
    PlayerInfo localPlayer;

    void Awake()
    {
        localPlayer = new PlayerInfo();
        playerInfo = new List<PlayerInfo>();
    }

    void OnServerInitialized()
    {
        playerInfo.Clear();
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        if (Network.isServer)
        {
            PlayerInfo info = new PlayerInfo(player, Network.AllocateViewID());
            info.playerName = info.player.ipAddress;
            info.filePath = "saves/" + info.playerName;
            playerInfo.Add(info);

            // Call RPC function to send NetworkPlayer info to connecting player.
            networkView.RPC("InitPlayer", player, info.player, info.playerID);

            // check that saves folder exists
            if (!Directory.Exists("saves"))
                Directory.CreateDirectory("saves");

            // Check if player folder exists
            if (!Directory.Exists(info.filePath))
                Directory.CreateDirectory(info.filePath);
        }
    }

    void OpenFile(PlayerInfo info, string filename, bool overwrite)
    {       
#if SERVER_BUILD
        if (overwrite == true)
        {
            string test = info.filePath + "/" + info.userid + "-" + info.sessionid + "-" + filename;
            info.writer = System.IO.File.CreateText(test);
            info.AddLog("debug", "OpenFile(overwrite:=" + test + ")");
        }
        else
        {
            // get extension
            string extention = Path.GetExtension(filename);

            // make filename
            filename = Path.GetFileNameWithoutExtension(filename);

            int i = 0;
            while (true)
            {
                string test = info.filePath + "/" + info.userid + "-" + info.sessionid + "-" + filename + "-" + i.ToString() + extention;
                if (!File.Exists(test))
                {
                    info.AddLog("debug", "OpenFile(test=" + test + ")");
                    info.writer = File.CreateText(test);
                    break;
                }
                i++;
            }
        }
#endif
    }

    void WriteFile(PlayerInfo info, string data)
    {
        if (info.writer != null)
            info.writer.WriteLine(data);
        else
            info.AddLog("debug", "WriteFile: info.writer = null");
    }

    void CloseFile(PlayerInfo info)
    {
        if (info.writer != null)
        {
            info.writer.WriteLine(info.writer.NewLine);
            info.writer.Close();
            info.writer = null;
        }
        else
            info.AddLog("debug", "WriteFile: info.writer = null");
    }

    void FlushFile(PlayerInfo info)
    {
        if (info.writer != null)
            info.writer.Flush();
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        if (Network.isServer)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    if (info == selectedInfo)
                        selectedInfo = null;

                    CloseFile(info);

                    playerInfo.Remove(info);
                    return;
                }
            }
        }
    }

    [RPC]
    void InitPlayer(NetworkPlayer player, NetworkViewID id)
    {
        PlayerInfo info = new PlayerInfo(player, id);
        localPlayer = info;
    }

    [RPC]
    void ReceiveLog(NetworkPlayer player, String logName, String logData)
    {
        LogMgr logMgr = LogMgr.GetInstance();
        if (logMgr != null)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    // Add to struct
                    info.AddLog(logName, logData);
                }
            }
        }
    }

    /*
    [RPC]
    void ReceiveTransmission(NetworkPlayer player, String logName, String logData)
    {
        LogMgr logMgr = LogMgr.GetInstance();
        if (logMgr != null)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    // Add to struct
                    info.AddData(logName, logData);

                    // Add to file
                    if (info.writer != null)
                        info.writer.WriteLine(info.data[info.data.Count - 1].XMLString());
                }
            }
        }
    }
     * */

    [RPC]
    void ReceiveBlockStart(NetworkPlayer player, String logName, bool overwrite)
    {
        Debug.Log("ReceiveBlockStart: " + logName);

        LogMgr logMgr = LogMgr.GetInstance();
        if (logMgr != null)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    // Add to struct
                    info.BlockStart(logName, overwrite);
                }
            }
        }
    }

    [RPC]
    void ReceiveBlock(NetworkPlayer player, String logName, String logData)
    {
        Debug.Log("ReceiveBlock: " + logName + " : total=" + logData.Count());

        LogMgr logMgr = LogMgr.GetInstance();
        if (logMgr != null)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    // Add to struct
                    info.BlockAdd(logName, logData);
                }
            }
        }
    }

    [RPC]
    void ReceiveBlockEnd(NetworkPlayer player, String logName)
    {
        Debug.Log("ReceiveBlockEnd: " + logName);

        LogMgr logMgr = LogMgr.GetInstance();
        if (logMgr != null)
        {
            foreach (PlayerInfo info in playerInfo)
            {
                if (info.player == player)
                {
                    // write end
                    info.BlockEnd(logName);

                    // open file
                    OpenFile(info, info.data[info.data.Count - 1].name, info.overwrite);

                    // write data
                    WriteFile(info, info.data[info.data.Count - 1].XMLString());

                    // close file
                    CloseFile(info);

                    /*
                    // write to DB at least until we get a public access database
                    info.AddLog(logName, "DBWrite <" + info.userid + "," + info.sessionid + ">");
                    StartCoroutine(DatabaseMgr.GetInstance().DBWrite(info.userid, info.sessionid, info.data[info.data.Count - 1].XMLString()));
                    info.AddLog(logName, "DBWriteResult:<" + DBWriteResult + "> DBWriteError: <" + DBErrorString + ">");
                     * */
                }
            }
        }
    }

    [RPC]
    void ReplyBack(NetworkPlayer player, String data)
    {
        Debug.Log("[RPC]ReplyBack(" + data + ")");
    }

    [RPC]
    void PutMessage(NetworkPlayer player, String data)
    {
        Debug.Log("[RPC]GetMessage(" + data + ")");
        // reply back
        networkView.RPC("ReplyBack", RPCMode.All, player, data);
    }

    [RPC]
    void SetUserSessionID(NetworkPlayer player, String userid, String sessionid)
    {
        foreach (PlayerInfo info in playerInfo)
        {
            if (info.player == player)
            {
                info.userid = userid;
                info.sessionid = sessionid;
            }
        }
    }

    public void SetUserSessionID(String userid, String sessionid)
    {
        if (networkView != null && Network.peerType != NetworkPeerType.Disconnected)
        {
            networkView.RPC("SetUserSessionID", RPCMode.Server, localPlayer.player, userid, sessionid);
        }
    }

    public void SendLogEntry(String logName, LogItem logData)
    {
        if (networkView != null && Network.peerType != NetworkPeerType.Disconnected)
        {
            networkView.RPC("ReceiveLog", RPCMode.Server, localPlayer.player, logName, logData.XMLString());
            networkView.RPC("PutMessage", RPCMode.Server, localPlayer.player, logData.XMLString());
        }
    }

    static int MAX_BLOCKSIZE = 2048;

    public void PutFile(String userid, String sessionid, String filename, String logData, bool overwrite)
    {
        //Debug.Log("Incoming.SendString(" + filename + ") : logData size = " + logData.Count());

        SetUserSessionID(userid, sessionid);

        if (networkView != null && Network.peerType != NetworkPeerType.Disconnected)
        {
            int count = logData.Count();
            int index = 0;

            // send start command
            networkView.RPC("ReceiveBlockStart", RPCMode.Server, localPlayer.player, filename, overwrite);

            // do middle blocks
            while (count > 0)
            {
                int blocksize = Math.Min(count, MAX_BLOCKSIZE);
                string data = logData.Substring(index, blocksize);

                //Debug.Log("Incoming.SendString() : send block size = " + blocksize);

                networkView.RPC("ReceiveBlock", RPCMode.Server, localPlayer.player, filename, data);
                index += blocksize;
                count -= blocksize;

                //Debug.Log("Incoming.SendString() : ReceiveBlock done");
            }

            // send end command
            networkView.RPC("ReceiveBlockEnd", RPCMode.Server, localPlayer.player, filename);
        }
    }

    float timeTilUpdate = 5.0f;
    void Update()
    {
        // Clear data if the network isn't running
        if (playerInfo.Count > 0 && Network.peerType == NetworkPeerType.Disconnected)
        {
            foreach (PlayerInfo info in playerInfo)
                CloseFile(info);

            selectedInfo = null;
            playerInfo.Clear();
        }

        // Flush buffers after 5 seconds
        if (Time.realtimeSinceStartup >= timeTilUpdate)
        {
            foreach (PlayerInfo info in playerInfo)
                FlushFile(info);

            timeTilUpdate = Time.realtimeSinceStartup + 5.0f;
        }
    }

    int DummySession=666;

    Vector2 scrollPos1 = new Vector2(), scrollPos2 = new Vector2();
    PlayerInfo selectedInfo;
    void OnGUI()
    {
        if (Network.isServer)
        {
            GUILayout.Space(30);
            if (GUILayout.Button("DBWrite Now"))
            {
                /*
                // write to DB at least until we get a public access database
                StartCoroutine(DatabaseMgr.GetInstance().DBWrite("rob.hafey@email.sitel.org", DummySession.ToString(), "large amount of data"));
                StartCoroutine(DatabaseMgr.GetInstance().DBGet("rob.hafey@email.sitel.org"));
                DummySession++;
                 * */
            }
            int i = 0;
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            scrollPos1 = GUILayout.BeginScrollView(scrollPos1, false, false, GUILayout.Width(150));
            GUILayout.BeginVertical();
            GUILayout.Label("Players Connected : " + playerInfo.Count.ToString());
            for (i = 0; i < playerInfo.Count; i++)
            {
                if (GUILayout.Button(i.ToString() + " : " + playerInfo[i].player.ipAddress))
                {
                    selectedInfo = playerInfo[i];
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            scrollPos2 = GUILayout.BeginScrollView(scrollPos2, false, false);
            GUILayout.BeginVertical();
            if (selectedInfo != null)
            {
                GUILayout.Label("Player : " + selectedInfo.player.ipAddress);
                GUILayout.Space(10);
                for (i = selectedInfo.log.Count - 1; i >= 0; i--)
                {
                    GUILayout.Label(selectedInfo.log[i].PrettyText());
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
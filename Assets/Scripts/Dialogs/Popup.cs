using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PopupMsg : DialogMsg
{
    public bool hasCancel;
    public string commandString;
    public PopupMsg()
        : base()
    {
        time = 0.0f;

        commandString = "";
        this.hasCancel = hasCancel;
        title = "Info";
        w = 450;
        h = 120;
        x = Screen.width / 2 - w / 2;
        y = Screen.height / 2 - h / 2;
    }
}

public class Popup : Dialog
{
    protected string textbox;
    //float timeout;

    static Popup instance;
    bool hasCancel;
    public string commandString;

    public Popup()
        : base()
    {
        Name = "Popup";

        textbox = "";
        instance = this;
        timeout = 0.0f;

        visible = false;
        hasCancel = true;
    }

    public static Popup GetInstance()
    {
        return instance;
    }

    public override void OnOpen()
    {
        Brain.GetInstance().PlayAudio("INFO:OPEN");
    }

    public override void OnGUI()
    {
        if (!visible)
            return;

        GUI.skin = gSkin;
        GUI.depth = -2;

        int w = 300;
        int x = Screen.width / 2 - (w / 2);
        int h = 125;
        int y = Screen.height / 2 - (h / 2);
        GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.customStyles[0]);
        GUILayout.BeginVertical();
        GUILayout.Space(4);
        GUILayout.Label(textbox, GUI.skin.customStyles[4], GUILayout.Height(70), GUILayout.Width(285));
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (hasCancel)
        {
            GUILayout.Space(30);
            if (GUILayout.Button("OK", GUI.skin.customStyles[1], GUILayout.Width(50), GUILayout.Height(20)))
            {
                StringMsg msg = new StringMsg();
                msg.message = commandString;
                //DoctorBrain.GetInstance().PutMessage(msg);
                print(commandString);
            }
            GUILayout.Space(w - 180);
            if (GUILayout.Button("Cancel", GUI.skin.customStyles[1], GUILayout.Width(50), GUILayout.Height(20)))
                visible = false;
        }
        else
        {
            GUILayout.Space(120);
            if (GUILayout.Button("Yes", GUI.skin.customStyles[1], GUILayout.Width(50), GUILayout.Height(20)))
            {
                // go back to main menu
                Application.LoadLevel("finish");
                Application.ExternalCall("finishLMS");
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    override public void PutMessage(GameMsg msg)
    {
        PopupMsg dialogmsg = msg as PopupMsg;
        if (dialogmsg != null)
        {
            // only call base if this message is for us
            base.PutMessage(msg);

            textbox = dialogmsg.text;
            x = dialogmsg.x;
            y = dialogmsg.y;
            w = dialogmsg.w;
            h = dialogmsg.h;
            visible = true;
            this.hasCancel = dialogmsg.hasCancel;
            this.commandString = dialogmsg.commandString;
            title = dialogmsg.title;
        }
    }
}
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ResizeDialogMsg : DialogMsg
{
    public ResizeDialogMsg() : base()
    {
        time = 0.0f;
    }
}

public class ResizeDialogue : Dialog
{
    protected string textbox;
    public GUISkin fullscreenSkin;

    static ResizeDialogue instance;

    public GUISkin topLeft, horizontal, topRight, vertical, center, bottomLeft, bottomRight;
    public GUISkin stretch;

    public int borderSize;

    public ResizeDialogue()
        : base()
    {
        Name = "ResizeDialogue";

        title = "Resizeable Dialogue";
        textbox = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        instance = this;
    }

    public static ResizeDialogue GetInstance()
    {
        return instance;
    }

    public override void OnGUI()
    {
        if (IsVisible() == false)
            return;

        if (fullscreenSkin)
            GUI.skin = fullscreenSkin;
        else
            base.OnGUI();

        x = 100;
        y = 100;
        w = Screen.width - 200;
        h = Screen.height - 200;

        GUI.BeginGroup(new Rect(x, y, w, h));
        {

            if (center)
                GUI.skin = center;
            GUI.Box(new Rect(-x, -y, Screen.width, Screen.height), "");
        }

        GUI.EndGroup();

        if (stretch)
            GUI.skin = stretch;

        GUI.Box(new Rect(x, y, w, h), "");

        if (exit == true)
        {
            if (GUI.Button(new Rect(x + w - 50, y + 15, 30, 20), ""))
            {
                SetVisible(false);
            }
        }

        // draw text in box
        GUI.Label(new Rect(x + 20, y + 40, w - 45, h - 40), textbox);
    }

    override public void PutMessage(GameMsg msg)
    {
        if (IsActive() == false)
            return;

        ResizeDialogMsg dialogmsg = msg as ResizeDialogMsg;
        if (dialogmsg != null)
        {
            // only call base if this message is for us
            base.PutMessage(msg);

            textbox = dialogmsg.text;
        }        
    }
}

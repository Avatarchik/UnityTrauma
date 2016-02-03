using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ErrorDialogMsg : DialogMsg
{
    public ErrorDialogMsg() : base()
    {
        time = 5.0f;
    }
}

public class ErrorDialogue : Dialog
{
    protected string textbox;
    static ErrorDialogue instance;
    public GUISkin errorSkin;

    public ErrorDialogue()
        : base()
    {
        Name = "Error Dialogue";

        title = "Error!";
        textbox = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
        instance = this;
    }

    public static ErrorDialogue GetInstance()
    {
        return instance;
    }

    public override void OnGUI()
    {
        if (IsVisible() == false)
            return;

        gSkin = errorSkin;

        GUI.skin = gSkin;

        w = GUI.skin.box.normal.background.width;
        h = GUI.skin.box.normal.background.height;

        base.OnGUI();

        // draw text in box
        GUI.Label(new Rect(x + 25, y + 40, w - 45, h - 15), textbox);
    }

    override public void PutMessage(GameMsg msg)
    {
        if (IsActive() == false)
            return;

        ErrorDialogMsg dialogmsg = msg as ErrorDialogMsg;
        if (dialogmsg != null)
        {
            // only call base if this message is for us
            base.PutMessage(msg);

            textbox = dialogmsg.text;
        }        
    }
}
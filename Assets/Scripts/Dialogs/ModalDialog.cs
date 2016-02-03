using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ModalDialogMsg : DialogMsg
{
}

public class ModalDialog : Dialog
{
    protected string textbox;
    Color color;

    public Color DimColor;

    public ModalDialog()
        : base()
    {
        instance = this;
        Name = "ModalDialog";
        title = "Modal Dialog";
        textbox = "";
        color = new Color(0.0f,0.0f,0.0f);
        DimColor = new Color(0.1f, 0.1f, 0.1f);
    }

    static ModalDialog instance;
    static public ModalDialog GetInstance()
    {
        return instance;
    }

    public override void OnGUI()
    {
        if (IsVisible() == false)
            return;

        w = 400;
        h = 300;
        x = Screen.width/2 - w/2;
        y = Screen.height/2 - h/2;

        base.OnGUI();

        // draw text in box
        GUI.Label(new Rect(x + 20, y + 30, w - 40, h - 40), textbox);
    }

    override public void PutMessage(GameMsg msg)
    {
        if (IsActive() == false)
            return;

        ModalDialogMsg dialogmsg = msg as ModalDialogMsg;
        if (dialogmsg != null)
        {
            // only call base if this message is for us
            base.PutMessage(msg);

            textbox = dialogmsg.text;

            // set the color on the open command
            if ( dialogmsg.command == DialogMsg.Cmd.open )
                color = RenderSettings.ambientLight;
        }
    }

#if USE_DIM
    public override void Update()
    {
        base.Update();

        if (IsVisible() == true)
        {
            RenderSettings.ambientLight = DimColor;
        }
        else
        {
            if ( color != new Color(0.0f,0.0f,0.0f) )
                RenderSettings.ambientLight = color;
        }
    }
#endif
}

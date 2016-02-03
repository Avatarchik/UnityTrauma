using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class QuickInfoMsg : DialogMsg
{
    public float timeout;

    public bool editbox;
    public string editboxlabel;
    public string editboxprompt;

    public QuickInfoMsg()
        : base()
    {
        time = 0.0f;

        timeout = 2.0f;
        title = "Info";
        w = 450;
        h = 120;

        editbox = false;
        editboxlabel = "";
        editboxprompt = "username:";
    }
}

public class QuickInfoDialog : BaseObject
{
    static QuickInfoDialog instance;
    GUIScreen Screen;

    float timeout;

    public QuickInfoDialog()
        : base()
    {
        Name = "QuickInfoDialog";

        instance = this;
        timeout = 0.0f;
    }

    public void Update()
    {
        if (timeout != 0.0f && Time.time > timeout)
        {
            if ( Screen != null )
            {
                GUIManager.GetInstance().Remove(Screen.Parent);
                Screen = null;
            }
        }
    }

    public static QuickInfoDialog GetInstance()
    {
        return instance;
    }

    override public void PutMessage(GameMsg msg)
    {
        QuickInfoMsg dialogmsg = msg as QuickInfoMsg;
        if (dialogmsg != null)
        {
			// only call base if this message is for us
			base.PutMessage(msg);

			switch ( dialogmsg.command )
			{
			case DialogMsg.Cmd.open:
				{
	            if (dialogmsg.timeout == 0.0f)
	                timeout = 0.0f;
	            else
	                timeout = Time.time + dialogmsg.timeout;

	            DialogLoader dl = DialogLoader.GetInstance();
	            if (dl != null)
	            {
					DialogMsg dmsg = new DialogMsg();
					dmsg.className = "GUIDialog";
					dmsg.xmlName = "dialog.quickinfo.template";
					dmsg.modal = false;
					Screen = GUIManager.GetInstance().LoadDialog(dmsg);
	                Screen.SetLabelText("titleBarText", dialogmsg.title);
	                Screen.SetLabelText("contentText", dialogmsg.text);
	            }
            	LogMgr.GetInstance().Add(new ParamLogItem(Time.time, "QuickInfoMsg", dialogmsg.text));
				}
				break;
			case DialogMsg.Cmd.close:
				GUIManager.GetInstance().Remove(Screen.Parent);
				Screen = null;
				break;
			}
        }
    }
}
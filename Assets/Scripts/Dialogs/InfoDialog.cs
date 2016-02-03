#define NEW_INFO_DIALOG

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class InfoDialogMsg : DialogMsg
{
	public bool AutoClose;
	public float CloseTime;
	public int MaxLines;
	public bool Reverse;
	public bool Scroll;

    public InfoDialogMsg() : base()
    {
        time = 0.0f;
		MaxLines = 5;
    }
}

public class InfoDialog : BaseObject
{
    public InfoDialog()
        : base()
    {
        Name = "InfoDialog";
        instance = this;
    }

    static InfoDialog instance;
    public static InfoDialog GetInstance()
    {
        return instance;
    }

    public override void PutMessage(GameMsg msg)
    {
        //InfoDialogMsg idmsg = msg as InfoDialogMsg;
        //if (idmsg != null)
        //{
        //    idmsg.screenName = "dialogGeneric";
        //    idmsg.xmlName = "InfoDialog";
        //    idmsg.screenType = "InfoDialogGUI";
        //    idmsg.modal = false;
        //    GUIManager.GetInstance().PutMessage(idmsg);
        //}
    }
}

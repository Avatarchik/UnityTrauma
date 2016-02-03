using UnityEngine;
using System.Collections;

public class ObjectCallGUI : Object3D 
{
    public TextAsset file;
    public string guiType = "GUIScreen";

	GUIDialog dialog;
	public GUIDialog Dialog
	{
		get { return dialog; }
	}

	
    //// Use this for initialization
    //void Start () {
	
    //}
	
    //// Update is called once per frame
    //void Update () {
	
    //}

    virtual public void OnMouseUp()
    {
		/*
        if(ObjectInteractionMgr.GetInstance() != null)
            if (ObjectInteractionMgr.GetInstance().Clickable == false)
                return; */

        if (WithinRange() == false || IsActive() == false || Enabled == false || guiType == null || file == null)
            return;

        // Pop up GUI
        GUIManager guiMgr = GUIManager.GetInstance();
        if (guiMgr != null)
        {
            DialogMsg msg = new DialogMsg();
            msg.command = DialogMsg.Cmd.open;
            msg.modal = true;
            msg.xmlName = file.name;
            msg.className = guiType;
            dialog = guiMgr.LoadDialog(msg) as GUIDialog;
        }
    }
}
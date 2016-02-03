using UnityEngine;
using System.Collections;

public class InfoDialogLoader : MonoBehaviour 
{
	public bool AutoClose=true;
	public float CloseTime=10.0f;
	public int MaxLines=5;
	public bool Reverse=true;
	public bool Scroll=true;

    private static InfoDialogLoader instance;

    public TextAsset xmlFile;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public static InfoDialogLoader GetInstance()
    {
        return instance;
    }

    public void PutMessage(InfoDialogMsg msg)
    {
        //Debug.Log("InfoDialogLoader.PutMessage()");

        msg.modal = false;
        msg.xmlName = xmlFile.name;
        msg.className = "InfoDialogGUI";
		msg.AutoClose = AutoClose;
		msg.CloseTime = CloseTime;
		msg.MaxLines = MaxLines;
		msg.Reverse = Reverse;
		msg.Scroll = Scroll;
        GUIManager.GetInstance().LoadDialog(msg);        
    }
}

using UnityEngine;
using System.Collections;

public class InteractDialogLoader : MonoBehaviour {

    private static InteractDialogLoader instance;

    public TextAsset xmlFile;
	public string className = "InteractMenu";

    void Awake()
    {
        if(instance == null)
            instance = this;
    }

    public static InteractDialogLoader GetInstance()
    {
        return instance;
    }

    public void PutMessage(InteractDialogMsg msg)
    {
        msg.modal = false;
        msg.xmlName = xmlFile.name;
        msg.className = className;
        GUIDialog dialog = GUIManager.GetInstance().LoadDialog(msg) as GUIDialog;
		if ( dialog != null )
		{
			// find the title
			GUILabel label = dialog.Find("title") as GUILabel;
			if ( label != null )
			{
				label.text = msg.title;
			}
		}
    }
}

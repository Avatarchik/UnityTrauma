using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Xml;
using System.Xml.Serialization;
	
public class BodyPart {
	public int R;
	public int G;
	public int B;
	public string Name;
	public Color32 color;
	public string XML;
	
	public void PreLoad() {
		color = new Color32(Convert.ToByte(R), Convert.ToByte(G), Convert.ToByte(B), 255);
	}
	
	public void Debug()
    {
        UnityEngine.Debug.Log("COLORPICKER: Color: " + color + " = " + Name);
    }
}

public class ColorPicker : MonoBehaviour {
	
	List<BodyPart> cData = new List<BodyPart>();
	
	public Camera useCamera;
	
	private TextureClick tc;
	private BodyPartTest bpt;
	private Color32 pickedColor;
	private string bodyPart;
	bool clicked = false;	
	public GameObject parent;
	
	public void Start () {
		LoadXML("XML/BodyPartsIndex");
	}
	
	public void Awake()
	{
		tc = gameObject.GetComponent<TextureClick>();
		bpt = gameObject.GetComponent<BodyPartTest>();
	}
	
	public void Click()
	{
		pickedColor = tc.GetHitMouse(useCamera);
		bodyPart = GetPart(pickedColor);
		string XML = GetPartXML(pickedColor);
		
		if(XML == null || XML == "")
			return;
		InteractDialogMsg msg = new InteractDialogMsg();
        msg.command = DialogMsg.Cmd.open;
        msg.title = bodyPart;
        msg.modal = true;
		msg.baseobj = parent.GetComponent<ObjectInteraction>();
		msg.LoadXML(XML);
		msg.baseXML = XML;
        print(InteractDialog.GetInstance().title);
		//parent.GetComponent<Patient>().UpdateMenu(msg);
		//parent.GetComponent<ObjectInteraction>().DoInteractMenu();
		InteractDialogLoader.GetInstance().PutMessage(msg);
		
		UnityEngine.Debug.Log("Color: " + pickedColor + " = " + bodyPart);
		clicked = true;
	}
	
	public string GetPartXML(Color32 color) {
		foreach(BodyPart part in cData) {
			if(part.color.Equals(pickedColor)) {
				return part.XML;
			}
		}
		return "";
	}
	
	public void LoadXML( string filename )
    {
        Serializer<List<BodyPart>> serializer = new Serializer<List<BodyPart>>();
        cData = serializer.Load(filename);
        if (cData != null) {
			foreach(BodyPart part in cData) {
				part.PreLoad();
                // don't store name
                ObjectInteraction objint = parent.GetComponent<ObjectInteraction>();
                if (objint != null)
                {
                    // don't overwrite name (KLUDGE ALERT!)
                    string name = objint.Name;
                    objint.LoadXML(part.XML);
                    objint.Name = name;
                }
			}
#if DEBUG_COLOR_PICKER
			Debug(cData); 
#endif
		}
    }
	
	public string GetPart(Color32 pickedColor) {
		foreach(BodyPart part in cData) {
			if(part.color.Equals(pickedColor)) {
				return part.Name;
			}
		}
		return "";
	}				
	
    public void Debug(List<BodyPart> bData)
    {
        foreach (BodyPart data in bData)
            data.Debug();
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

#if UNITY_EDITOR

public class GUIEdit : MonoBehaviour
{
	public GUIEdit()
	{		
	}
	
	public string saveName;
	
	
	GUIEditObject PasteObject=null;	
	public void SetPasteObject( GUIEditObject paste )
	{
		PasteObject = paste;
	}
	
	public GUIEditObject GetPasteObject()
	{
		return PasteObject;
	}
	
	public void ShowInspectorGUI()
	{}
	
	
}

#endif

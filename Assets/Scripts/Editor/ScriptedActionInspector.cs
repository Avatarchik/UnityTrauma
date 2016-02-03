using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CustomEditor(typeof(ScriptedAction))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class ScriptedActionInspector : Editor 
{
	protected bool onSelected = false;
	public bool showHelp = false;
	protected bool showDefaultInspector = false;
	ScriptedAction myObject = null;
	
	public void Awake(){
		// load up the interactions and tasks this should be done elsewhere, and only once
		InteractionMgr.GetInstance().LoadXML("XML/interactions/interactions");
		if (TaskMaster.GetInstance() != null)
			TaskMaster.GetInstance().LoadXML("XML/Tasks");
		StringMgr.GetInstance().Load ();
	}
	
	virtual public void OnSelected() // look at a particular instance object
		
	{
		myObject = target as ScriptedAction;
		//base.OnSelected();
	}
	
	public override void OnInspectorGUI()
	{
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		
		Color oldColor = GUI.color;
//		GUI.color = Color.cyan;
		
		myObject.ShowInspectorGUI("");		
		
		// show help checkbox
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show ScriptedAction Help");
		GUILayout.FlexibleSpace();
		showHelp = GUILayout.Toggle(showHelp, "");
		GUILayout.EndHorizontal();
		if (showHelp) myObject.ShowHelp();
		
		GUI.color = oldColor;
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show Default Inspector");
		GUILayout.FlexibleSpace();

		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
		GUILayout.EndHorizontal();
		if (showDefaultInspector)
			DrawDefaultInspector();

		return;
	}
	
	public void Update()
	{
	}
	
	bool DeleteButton(string label){
		Color old = GUI.color;
		GUI.color = Color.red;
		bool clicked = GUILayout.Button (label,GUILayout.ExpandWidth(false));
		GUI.color = old;
		return clicked;
	}
	
	public void SyncDataFields(){
		if (myObject.objectToAffect	!= null && myObject.objectName == "")
			myObject.objectName = myObject.objectToAffect.name;
		if (myObject.objectName	!= "" && myObject.objectToAffect == null)
			myObject.objectToAffect = GameObject.Find(myObject.objectName);
		
	}
}


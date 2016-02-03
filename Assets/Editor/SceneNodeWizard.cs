//  2012 Trailzen Designs

/*
	
*/
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;

public class SceneNodeWizard : ScriptableWizard {
	
	
	void OnWizardCreate() { // do we need another button for something ?

	}
	
	void OnWizardOtherButton(){
		foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
		{
			if (go.name == "navNode")
			{
				GameObject par = go.transform.parent.gameObject;
				if (par.GetComponent<SceneNode>() == null)
					par.AddComponent<SceneNode>();
				if (par.name.Substring(0,4) != "path")
					par.GetComponent<SceneNode>().isNav = false; // this is what we usually want
			}
			if (go.name.Substring(0,4) == "path") // some nav node children are disabled.
			{
				if (go.GetComponent<SceneNode>() == null)
					go.AddComponent<SceneNode>();
			}
		}
	}

	void OnWizardUpdate() {
		isValid = true;
		errorString = "";

	}
	
	void OnEnable(){

	}
	
	[MenuItem("Custom/SceneNodes/Populate...")]
	public static void MenuItemHandler() {
		ScriptableWizard.DisplayWizard("Populate...", typeof(SceneNodeWizard), "Create","Build");
	}
}

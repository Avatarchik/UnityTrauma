using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(CaseConfigurator))]

public class CaseConfiguratorInspector : Editor
{
	public CaseConfiguratorInspector ()
	{
	}
	
	CaseConfigurator myObject;
	bool onSelected=false;
	
	virtual public void OnSelected() // look at a particular instance object		
	{
		myObject = target as CaseConfigurator;
	}
			
	public override void OnInspectorGUI()
	{
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		DrawDefaultInspector();
	}
}


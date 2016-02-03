using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(CaseConfiguratorSettings))]

public class CaseConfiguratorSettingsInspector : Editor
{
	public CaseConfiguratorSettingsInspector ()
	{
	}
	
	CaseConfiguratorSettings myObject;
	bool onSelected=false;

	virtual public void OnSelected() // look at a particular instance object		
	{
		myObject = target as CaseConfiguratorSettings;
	}
			
	public override void OnInspectorGUI()
	{
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		DrawDefaultInspector();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button ("Load Case Order" ) )
		{
			Serializer<List<string>> serializer = new Serializer<List<string>>();
			myObject.CaseOrder = serializer.Load ("XML/CaseOrder");
		}
		if ( GUILayout.Button ("Save Case Order" ) )
		{
			Serializer<List<string>> serializer = new Serializer<List<string>>();
			serializer.Save (Application.dataPath + "/Resources/XML/CaseOrder.xml",myObject.CaseOrder);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if ( myObject.UseLocalData == false )
		{
			if ( GUILayout.Button ("Save WEB Cases to CaseInfo.xml") )
			{
				if ( CaseConfiguratorMgr.GetInstance().CaseList == null || CaseConfiguratorMgr.GetInstance().CaseList.Count == 0 )
					UnityEngine.Debug.LogError ("NO CASES, go to case selection screen first!");
				else
					CaseConfiguratorMgr.GetInstance().SaveXML (Application.dataPath + "/Resources/XML/CaseInfo.xml");
			}
		}
		else
		{
			if ( GUILayout.Button ("Save Selected Case to WEB") )
			{
				if ( CaseConfiguratorMgr.GetInstance().CaseList == null || CaseConfiguratorMgr.GetInstance().CaseList.Count == 0 )
					UnityEngine.Debug.LogError ("NO CASES, go to case selection screen first!");
				else
					// takes the current case and saves it to the database
					CaseConfiguratorMgr.GetInstance().SaveCaseConfiguration(null);
			}
		}
		GUILayout.EndHorizontal();
	}
}


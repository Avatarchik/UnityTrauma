using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CustomEditor(typeof(InteractionScript))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class InteractionScriptInspector : Editor 
{
	protected bool onSelected = false;
	protected bool showDefaultInspector = true;
	bool commandVariationsUpdated = false;
	bool b1 = false;
	bool b2 = false;
//	int insertPoint = 0; // for inserting a blank script line space
	InteractionScript myObject = null;
	string newScriptName = "";
	
	public void Awake(){
	}
	
	virtual public void OnSelected(){ // look at a particular instance object
		myObject = target as InteractionScript;
		commandVariationsUpdated = false;
	}
	
	public void OnInspectorUpdate(){
		Debug.Log ("OnInspectorUpdate");
		if (Application.isPlaying && ScriptViewWindow.instance != null && ScriptViewWindow.instance.myScript == myObject)
			ScriptViewWindow.Init(myObject);
	}
	
	
	public override void OnInspectorGUI()
	{
		if (!onSelected) // 
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
/*		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show Default Inspector");
		GUILayout.FlexibleSpace();
		Color oldColor = GUI.color;
		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
		GUILayout.EndHorizontal();
*/
		Color oldColor = GUI.color;
		GUI.color = Color.cyan;
		ShowArgumentValues();
		ShowAttributeValues();
		GUI.color = oldColor;

		// add a control and a button to insert a blank spot in the scripts array
/*
		if (myObject.scriptLines != null && myObject.scriptLines.Count() > 0){
			GUILayout.BeginHorizontal();
			insertPoint = EditorGUILayout.IntField("EDIT Script line:",insertPoint);
			if (GUILayout.Button("INSERT")){
				ScriptedAction[] tmp = new ScriptedAction[ myObject.scriptLines.Count()];
				for (int i=0; i < myObject.scriptLines.Count(); i++){
					tmp[i]= myObject.scriptLines[i];
				}
				myObject.scriptLines = new ScriptedAction[tmp.Count()+1];
				for (int i=0; i < insertPoint; i++){
					myObject.scriptLines[i] = tmp[i];
				}
				for (int i=insertPoint+1; i < tmp.Count()+1; i++){
					myObject.scriptLines[i] = tmp[i-1];
				}
				
			}
			if (GUILayout.Button("DELETE")){
				ScriptedAction[] tmp = new ScriptedAction[ myObject.scriptLines.Count()];
				for (int i=0; i < myObject.scriptLines.Count(); i++){
					tmp[i]= myObject.scriptLines[i];
				}
				myObject.scriptLines = new ScriptedAction[tmp.Count()-1];
				for (int i=0; i < insertPoint; i++){
					myObject.scriptLines[i] = tmp[i];
				}
				for (int i=insertPoint+1; i < tmp.Count(); i++){
					myObject.scriptLines[i-1] = tmp[i];
				}
				
			}
			if (GUILayout.Button("ScriptView")){
				ScriptViewWindow.Init(myObject); 
			}
			GUILayout.EndHorizontal();
 
			}
*/
			if (GUILayout.Button("Open In ScriptView")){
				ScriptViewWindow.Init(target as InteractionScript);
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Rename Script",GUILayout.ExpandWidth(false))){
				RenameScript(target as InteractionScript,newScriptName);
				newScriptName = "";
			}
//			GUILayout.Label("NAME:",GUILayout.ExpandWidth(false));
			newScriptName = EditorGUILayout.TextField("NAME:",newScriptName);
			GUILayout.EndHorizontal();
			
//			if (GUILayout.Button("Populate ALL Script command variations")){
//				PopulateCommandVariations();
//			}
			Color old = GUI.color;
			if (!b1)
			{
				GUI.color = Color.yellow;
				if (GUILayout.Button("CommandVariation TOOLS")) 
				{
					b1=true;
				b2=true;
				}
			
			}
			else{
				if (!b2){
					GUI.color = Color.green;
					if (GUILayout.Button("REPORT scene CmdVariations vs. XML")){
						ReportMissingCommandVariations();
					}
					GUI.color = Color.red;
					if (GUILayout.Button("DANGEROUS ADVANCED TOOLS")) b2=true;
					if (GUILayout.Button("Hide These Tools from me.")){
						b1=false;
						b2=false;
					}
				}
				else{
					GUI.color = Color.green;
					GUILayout.Label("Use ONLY in Trauma Editing Scene");
					if (GUILayout.Button("Hide These Tools from me.")){
						b1=false;
						b2=false;
					}			
#if USE_COMMAND_VARIATIONS_NEW
					if (GUILayout.Button("MERGE ALL scene & XML command variations")){
						MergeCommandVariations();
						b1=false;
						b2=false;
					}
#endif
					if (GUILayout.Button("IMPORT & REPLACE all command variations from CommandVarations.xml.")){
						ReplaceCommandVariations();
						b1=false;
						b2=false;
					}
					if (GUILayout.Button("EXPORT all command variations to CommandVariations.xml."))
					{
						ExportCommandVariations();
						b1=false;
						b2=false;
					}


				InteractionScript droppedIS = null;
				droppedIS = (InteractionScript)EditorGUILayout.ObjectField("Copy Variations From",droppedIS,typeof(InteractionScript),true,GUILayout.Width(375));
				if (droppedIS != null){
					CopyCommandVariationsFrom(droppedIS);
				}
				ScriptedObject so = null;
				if (myObject.transform.parent != null)
					so = myObject.transform.parent.GetComponent<ScriptedObject>();
				if (so != null){
					GUILayout.Label("owned by "+so.name);
					if (commandVariationsUpdated){
						if (GUILayout.Button("SAVE XML & Prefab (changed) ")){
							SaveUpdatedPrefab( so);
						}
					} else {
						if (GUILayout.Button("SAVE XML & Prefab ")){
							SaveUpdatedPrefab( so);
						}
					}
				}

#if USE_COMMAND_VARIATIONS_NEW
					if (GUILayout.Button("Update CommandVariations Object from XML")){
						LoadSceneObjectFromXML();
					}
					if (GUILayout.Button("SAVE CommandVariations Object to New XML")){
						SaveSceneObjectToXML();
					}
#endif
				}
			}
			GUI.color = old;
		
//		if (showDefaultInspector)
			DrawDefaultInspector();
		

		return;
	}
	
	void ShowArgumentValues(){
		if (myObject != null && myObject.args.Count > 0){
			GUILayout.Label("Current Arguments");
			foreach (KeyValuePair<string,string> kvp in myObject.args){
				GUILayout.BeginHorizontal();
				GUILayout.Label(kvp.Key);	
				GUILayout.Label(kvp.Value);
				GUILayout.EndHorizontal();
			}
		}
	}
	
	void ShowAttributeValues(){
		if (myObject != null && myObject.myObject != null){
			BaseObject bob = myObject.myObject.GetComponent<BaseObject>();
			if (bob != null && bob.attributes.Count > 0){
				GUILayout.Label("Current Attributes");
				foreach (KeyValuePair<string,string> kvp in bob.attributes){
					GUILayout.BeginHorizontal();
					GUILayout.Label(kvp.Key);	
					GUILayout.Label(kvp.Value);
					GUILayout.EndHorizontal();
				}
			}
		}
	}
	
	void PopulateCommandVariations(){
		Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
		List<FilterInteractions.CommandVariation> variations = serializer2.Load("XML/CommandVariations");
		
		InteractionScript[] scripts = GameObject.FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[];
		
		foreach (InteractionScript s in scripts){
			foreach (FilterInteractions.CommandVariation v in variations){
				if (s.name == v.Cmd){
					s.commandVariation = v;
					break;
				}
			}
		}
	}
	
	void ReportMissingCommandVariations(){
		Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
		List<FilterInteractions.CommandVariation> variations = serializer2.Load("XML/CommandVariations");
		
		InteractionScript[] scripts = GameObject.FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[];
		
		foreach (InteractionScript s in scripts){
			bool found = false;
			foreach (FilterInteractions.CommandVariation v in variations){
				if (s.name == v.Cmd){
					found=true;
					break;
				}
			}
			if (!found && s.AddToMenu){
				string message = s.name+" not found in CommandVariations.xml";
				Transform t = s.transform.parent;
				while (t!= null){
					message = t.name+"/"+message;
					t=t.parent;
				}
				Debug.LogWarning(message);
			}
		}
		foreach (FilterInteractions.CommandVariation v in variations)
		{
			bool found = false;
			foreach (InteractionScript s in scripts){
				if (s.name == v.Cmd){
					found=true;
					break;
				}
			}
			if (!found)
				Debug.LogWarning("XML: "+v.Cmd+" not matched to any scene interaction");
		}		
		
	}	
	
	
	void ReplaceCommandVariations(){
		Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
		List<FilterInteractions.CommandVariation> variations = serializer2.Load("XML/CommandVariations");
		
		InteractionScript[] scripts = GameObject.FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[];
		
		foreach (InteractionScript s in scripts){
			foreach (FilterInteractions.CommandVariation v in variations){
				if (s.name == v.Cmd){
					if ( s.commandVariation != v )
					{
						Debug.Log("Updating command variations from XML for "+s.name);
						s.commandVariation = v;
						// ensure that we don't have blank fields for cmd and cmdString
						if (s.commandVariation.Cmd == "" || s.commandVariation.Cmd == null)
							s.commandVariation.Cmd = s.name;
						if (s.commandVariation.CmdString == "" || s.commandVariation.CmdString == null) {
							if ( s.commandVariation.Variations != null && s.commandVariation.Variations.Count > 0){
								s.commandVariation.CmdString = s.commandVariation.Variations[0];
							}
							else{
								Debug.LogWarning(s.name+" XML had NO variations!");
							}
						}
						// we've updated this script, so re-save the parent scripted object to xml, if it has one
						if (s.transform.parent != null){
							ScriptedObject so = s.transform.parent.GetComponent<ScriptedObject>();
							if (so != null){
								if (so.XMLName != null && so.XMLName != ""){
									so.SaveToXML(so.XMLName);
								}
								// now find and update the prefab that the so is part of.
								GameObject rootGO = PrefabUtility.FindRootGameObjectWithSameParentPrefab(s.gameObject);
								if (rootGO != null){
									UnityEngine.Object prefab = PrefabUtility.GetPrefabParent(rootGO);
									if (prefab != null)
										PrefabUtility.ReplacePrefab(rootGO,prefab);
								}
							}
						}
						break;
					}
				}
			}
		}
	}
	
	void MergeCommandVariations(){
		// merge both from xml and to xml, to all the instances in the scene
		Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
		List<FilterInteractions.CommandVariation> variations = serializer2.Load("XML/CommandVariations");
		
		InteractionScript[] scripts = GameObject.FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[];
		
		foreach (InteractionScript s in scripts){
			if ( s.commandVariation == null || s.commandVariation.Variations == null )
				continue;
			
			bool vFound = false;
			foreach (FilterInteractions.CommandVariation v in variations){
				if (v.CmdString == null && v.Variations != null && v.Variations.Count > 0 ) v.CmdString = v.Variations[0];
				if (s.name == v.Cmd){
					vFound=true;
					// match, now merge
					//s.commandVariation = v;
					foreach (string st in v.Variations){
						bool found = false;
						foreach (string st2 in s.commandVariation.Variations){
							if (st == st2){
								found = true;
								break;
							}
						}
						if (!found)
							s.commandVariation.Variations.Add(st);
					}
					foreach (string st in s.commandVariation.Variations){
						bool found = false;
						foreach (string st2 in v.Variations){
							if (st == st2){
								found = true;
								break;
							}
						}
						if (!found)
							v.Variations.Add(st);
					}
					break;
				}
			}
			if (!vFound && s.commandVariation.Cmd!=""){
				
				variations.Add(s.commandVariation);
			}
		}
		// perform the loop a second time, to merge any late additions into early commands
		foreach (InteractionScript s in scripts){
			if ( s.commandVariation == null || s.commandVariation.Variations == null )
				continue;
			foreach (FilterInteractions.CommandVariation v in variations){
				if (v.CmdString == null && v.Variations != null && v.Variations.Count > 0 ) v.CmdString = v.Variations[0];
				if (s.name == v.Cmd){
					// match, now merge
					//s.commandVariation = v;
					foreach (string st in v.Variations){
						bool found = false;
						foreach (string st2 in s.commandVariation.Variations){
							if (st == st2){
								found = true;
								break;
							}
						}
						if (!found)
							s.commandVariation.Variations.Add(st);
					}
					foreach (string st in s.commandVariation.Variations){
						bool found = false;
						foreach (string st2 in v.Variations){
							if (st == st2){
								found = true;
								break;
							}
						}
						if (!found)
							v.Variations.Add(st);
					}
					break;
				}
			}
		}
		// then write the results out to the xml
		serializer2.Save("Assets/Resources/XML/CommandVariationsNew.xml",variations);
	}	

	void ExportCommandVariations()
	{
		// merge both from xml and to xml, to all the instances in the scene
		Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();

		InteractionScript[] scripts = GameObject.FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[];

		List<FilterInteractions.CommandVariation> variations = new List<FilterInteractions.CommandVariation>();

		// add each command variation to the list
		foreach (InteractionScript s in scripts){
			if ( s.commandVariation != null && s.commandVariation.Cmd != "" )
			{
				// check for duplicates
				bool found = false;
				foreach( FilterInteractions.CommandVariation item in variations )
				{
					if ( item.Cmd == s.commandVariation.Cmd )
						found = true;
				}
				if ( found == false )
					variations.Add(s.commandVariation);
			}
		}
		// then write the results out to the xml
		serializer2.Save("Assets/Resources/XML/CommandVariations.xml",variations);
	}

	void CopyCommandVariationsFrom(InteractionScript droppedIS){
		myObject.commandVariation.Cmd = droppedIS.commandVariation.Cmd;
		myObject.commandVariation.CmdString = droppedIS.commandVariation.CmdString;
		myObject.commandVariation.Variations = new List<string>();
		foreach (string s in droppedIS.commandVariation.Variations)
			myObject.commandVariation.Variations.Add (s);
		commandVariationsUpdated = true;
	}


	void SaveUpdatedPrefab(ScriptedObject so){
		so.SaveToXML(so.XMLName);
		so.SaveToPrefab();
	}	

	
	void LoadSceneObjectFromXML(){
		FilterInteractions obj = FindObjectOfType(typeof (FilterInteractions)) as FilterInteractions;
		if (obj != null){
			Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
			obj.variations = serializer2.Load("XML/CommandVariations");
		}
	}
	
	void SaveSceneObjectToXML(){
		FilterInteractions obj = FindObjectOfType(typeof (FilterInteractions)) as FilterInteractions;
		if (obj != null){
			Serializer<List<FilterInteractions.CommandVariation>> serializer2 = new Serializer<List<FilterInteractions.CommandVariation>>();
			serializer2.Save("Assets/Resources/XML/CommandVariationsNew.xml",obj.variations);
		}		
	}
	
	void RenameScript(InteractionScript script, string newName){
		if (newName == "") return;
		
		// check for a unique name on this parent
		if (script.transform.parent != null){
			ScriptedObject so = script.transform.parent.GetComponent<ScriptedObject>();
			if (so != null && so.scripts != null){
				foreach (InteractionScript s in so.scripts){
					if (s.name == newName){
						Debug.LogWarning("Scriptename "+newName+" is already in use in this scene.");
						return;
					}
				}
			}
		}	

		// create a new child game object with a default scripted action and link it in	
		script.name = newName;
		// initialize some fields to meaningful defaults 
		script.item = newName;
		script.triggerStrings = new string[1];
		script.triggerStrings[0] = newName;
		script.triggerOnStatus = false;

		EditorUtility.SetDirty(script);
		// go ahead and bring it up in the editor
		ScriptViewWindow.Init(script);
	}
	
	public void Update()
	{
	}
}
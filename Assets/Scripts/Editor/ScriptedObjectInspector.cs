using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

[CustomEditor(typeof(ScriptedObject))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class ScriptedObjectInspector : Editor 
{
	protected bool onSelected = false;
	protected bool showDefaultInspector = true;
	private string newScriptName = "";
	private string scriptNameWarning = "";
	private string addScriptButtonLabel = "ADD NEW SCRIPT"; // Make My Life Easier!
	public string XMLDirectory = "Assets/Resources/XML/";
	
	public void Awake(){
	}
	
	virtual public void OnSelected() // look at a particular instance object
	{
	}
	
	public override void OnInspectorGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show Default Inspector");
		GUILayout.FlexibleSpace();
		Color oldColor = GUI.color;
		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
		GUILayout.EndHorizontal();
		
		if (showDefaultInspector)
			DrawDefaultInspector();
		
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		if ( GUILayout.Button("Log Overriding Voicemaps") )
			LogVoiceOverrides();
		GUI.color = Color.cyan;
		ShowScriptSerialization();
		if (GUILayout.Button ("Edit Menu Categories"))
		{
			MenuEditWindow.Init(target as ScriptedObject);
		}
		if (GUILayout.Button ("Edit Roles"))
		{
			RoleEditorWindow.Init(target as ScriptedObject);
		}
		GUI.color = oldColor;
		return;
	}
	
	void ShowScriptSerialization(){
		// don't put these buttons up while running...
		if (EditorApplication.isPlaying || EditorApplication.isPaused) return;
		ScriptedObject so = target as ScriptedObject;
		if (so != null){
			
			if (scriptNameWarning != ""){
				GUI.color = Color.red;
				GUILayout.Label(scriptNameWarning);
				CheckScriptName(so);
			}
			
			GUILayout.BeginHorizontal();
				if (GUILayout.Button(addScriptButtonLabel,GUILayout.ExpandWidth(false))){
					// be sure we have a valid unique script name
					if (CheckScriptName(so)){
						AddScript(so,newScriptName);
						newScriptName = "";
					}			
				}
				GUILayout.Label("SCRIPT NAME:",GUILayout.ExpandWidth(false));
				newScriptName = GUILayout.TextField(newScriptName);
			GUILayout.EndHorizontal();
			so.XMLDirectory = EditorGUILayout.TextField("XML Directory:",so.XMLDirectory);
			GUILayout.BeginHorizontal();
			GUILayout.Label("XML File:",GUILayout.ExpandWidth(false));
			if (so.XMLName == null) so.XMLName = "";
			if (so.XMLName == "") so.XMLName = "ScriptedObjects/"+so.name+".xml";
			if (so.XMLName.Contains(so.XMLDirectory)) so.XMLName = so.XMLName.Replace(so.XMLDirectory,"");
			so.XMLName = GUILayout.TextField(so.XMLName);
			GUILayout.EndHorizontal();
			if (GUILayout.Button("SAVE XML & Prefab ")){
				so.SaveToXML(so.XMLName);
				so.SaveToPrefab();
			}			
			if (so.PrefabNeedsUpdate()){
				Color c = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("XML newer than Prefab!");
				GUI.color = c;
			}
			GUILayout.BeginHorizontal();
				if (GUILayout.Button("SAVE PFB")){
					so.SaveToPrefab();
				}				
				if (GUILayout.Button("SAVE XML")){
					so.SaveToXML(so.XMLName);
				}
				if (GUILayout.Button("LOAD XML")){
					so.LoadFromXML(so.XMLName);
				}
			GUILayout.EndHorizontal();			
		}
	}
	
	bool CheckScriptName(ScriptedObject so){
		if (newScriptName == ""){
			scriptNameWarning = "You must Supply a ScriptName";
			return false;	
		}
		if (so.scripts != null)
			foreach (InteractionScript script in so.scripts){ // should really check globally across all scripts.
				if (script.name == newScriptName){
					scriptNameWarning = "ScriptName already used";
					return false;
				}
			}
		scriptNameWarning = "";
		addScriptButtonLabel = "ADD SCRIPT";
		return true;
	}
	
	void AddScript(ScriptedObject so,string newName){
		// create a new child game object with a default scripted action and link it in	
		GameObject newChild = new GameObject(newName);
		InteractionScript newIS = newChild.AddComponent("InteractionScript") as InteractionScript;
		// maybe initialize some fields to meaningful defaults ?
		newIS.item = newName;
		newIS.triggerStrings = new string[1];
		newIS.triggerStrings[0] = newName;
		newIS.triggerOnStatus = false;
		newIS.scriptLines = new ScriptedAction[0];
		newChild.transform.parent = so.transform;
		if (so.scripts == null) so.scripts = new InteractionScript[0];
		InteractionScript[] tmp = new InteractionScript[ so.scripts.Length];
		for (int i=0; i < so.scripts.Length; i++){
			tmp[i]= so.scripts[i];
		}
		so.scripts = new InteractionScript[tmp.Length+1];
		for (int i=0; i < tmp.Length; i++){
			so.scripts[i] = tmp[i];
		}
		so.scripts[tmp.Length] = newIS;
		EditorUtility.SetDirty(newIS);
		// go ahead and bring it up in the editor
		ScriptViewWindow.Init(newIS);
	}

	void LogVoiceOverrides()
	{
		string output = "Voice Overrides in Case Configurations:\r\n\r\n";
		{
			foreach (ContentLinker cl in (FindObjectsOfType(typeof (ContentLinker)) as ContentLinker[])){
				if (cl.voiceLists.Count > 0) output+= cl.transform.parent.name+"."+cl.name+":\r\n";
				foreach (VoiceList vl in cl.voiceLists)
					output += vl.ToString();
			}
			foreach (ScriptedObject so in (FindObjectsOfType(typeof (ScriptedObject)) as ScriptedObject[])){
				if (so.voiceList.VoiceMaps.Count > 0 || so.voiceLists.Count > 0) output+= so.name+":\r\n";
				if (so.voiceList.VoiceMaps.Count > 0 )
					output += so.voiceList.ToString();
				foreach (VoiceList vl in so.voiceLists)
					output += vl.ToString();
			}
		}
		System.IO.File.WriteAllText(Application.dataPath+"/VoiceOverrides.txt",output);
	}
	
	public void Update()
	{
	}
}

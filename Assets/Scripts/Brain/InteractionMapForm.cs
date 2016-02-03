using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public class InteractionMapForm : MonoBehaviour {
	// item=name of interaction.  all lists of interactions are found in interaction.xml.
    // once an interaction gets dispatched, a InteractStatusMsg is broadcast to all registered items in system
    public string item = "";                 
    // response and response title are used for the display of a default interaction dialog.  not required
    public string response = "";             
    public string response_title = "";
    // tooltip text for this InteractionMap
    public string tooltip = "";
    // default note for this interaction
    public string note = "";
    // sfx played on interaction
    public string sound = "";
    // task used by TaskMaster to manage task handling and for TASK:COMPLETE msgs.
    public string task = "";
    // if list is present this interaction points to an InteractionList which is found in the interactions.xml file
    public string list = "";
    // time of interaction used for logging
    public float time;
    // should we log this item or not
    public bool log;
    // max number of this type of interaction per run
    public int max;

    // ask for confirmation of this interaction with dialog
    public bool confirm;
    // confirmation dialog when answer is YES
    public string confirm_audio = "";
	// enabled, for dynamic menu, will be greyed out if false
	public bool Enabled; // caps to avoid conflict with monoBehaviour.enabled
	
	public string scriptName;
	public string objectName;
	
	// prereq of interactions required to trigger this interaction
    public List<string> prereq;
	public List<string> category;
	// List of parameters, primarily for passing values into interaction scripts like drug dosage, etc.
	public List<string> param;
	
	public void InitFromMap(InteractionMap map){
		item = map.item;                 
    	response = map.response;             
    	response_title = map.response_title;
    	tooltip = map.tooltip;
    	note = map.note;
    	sound = map.sound;
    	task = map.task;
    	list = map.list;
    	time = map.time;
    	log = map.log;
    	max = map.max;
		confirm = map.confirm;
		confirm_audio = map.confirm_audio;
		Enabled = map.Enabled;
		scriptName = map.scriptName;
		objectName = map.objectName;
		
		if (map.prereq != null){
			prereq = new System.Collections.Generic.List<string>();
			for (int i = 0; i<map.prereq.Count; i++)
				prereq.Add (map.prereq[i]);
		}
		if (map.category != null){
			category = new System.Collections.Generic.List<string>();
			for (int i = 0; i<map.category.Count; i++)
				category.Add (map.category[i]);
		}
		if (map.param != null){
			param = new System.Collections.Generic.List<string>();
			for (int i = 0; i<map.param.Count; i++)
				param.Add (map.param[i]);
		}
	}
	
	public InteractionMap GetMap(){
		InteractionMap map = new InteractionMap(item,response,response_title,note,tooltip,sound,task,log);
		map.objectName = objectName;
		map.scriptName = scriptName;
		if (prereq != null){
			map.prereq = new System.Collections.Generic.List<string>();
			for (int i = 0; i<prereq.Count; i++)
				map.prereq.Add (prereq[i]);
		}
		if (category != null){
			map.category = new System.Collections.Generic.List<string>();
			for (int i = 0; i<category.Count; i++)
				map.category.Add (category[i]);
		}
		if (param != null){
			map.param = new System.Collections.Generic.List<string>();
			for (int i = 0; i<param.Count; i++)
				map.param.Add (param[i]);
		}
		
		return map;
	}
	
	// this lets us view and edit an InteractionMap from the Unity Editor
	// maybe we could write something that uses reflection to make a general purpose on of these ?
#if UNITY_EDITOR
	SerializedObject serializedObject;
	
	public bool ShowInspectorGUI(string label){
		if (serializedObject == null) serializedObject = new SerializedObject(this);
		
		EditorGUI.BeginChangeCheck();
		GUILayout.Label(label+": V- InteractionMap -V");
		if (item == null) item = "";
		item = EditorGUILayout.TextField(label+".item",item);
		if (response == null) response = "";
		response = EditorGUILayout.TextField(label+".response",response);
		if (response_title == null) response_title = "";
		response_title = EditorGUILayout.TextField(label+".response_title",response_title);
		if (tooltip == null) tooltip = "";
		tooltip = EditorGUILayout.TextField(label+".tooltip",tooltip);
		if (note == null) note = "";
		note = EditorGUILayout.TextField(label+".note",note);
		if (sound == null) sound = "";
		sound = EditorGUILayout.TextField(label+".sound",sound);
		if (task == null) task = "";
		task = EditorGUILayout.TextField(label+".task",task);
		if (list == null) list = "";
		list = EditorGUILayout.TextField(label+".list",list);
		if (item == null) item = "";
		time = EditorGUILayout.FloatField(label+".time",time);
		log = EditorGUILayout.Toggle(label+".log",log);
		max = EditorGUILayout.IntField(label+".max",max);
		confirm = EditorGUILayout.Toggle(label+".confirm",confirm);
		if (confirm_audio == null) confirm_audio = "";
		confirm_audio = EditorGUILayout.TextField(label+".confirm_audio",confirm_audio);
		if (scriptName == null) scriptName = "";
		scriptName = EditorGUILayout.TextField(label+".scriptName",scriptName);
		if (objectName == null) objectName = "";
		objectName = EditorGUILayout.TextField(label+".objectName",objectName);
		
	//	prereq = EditorGUILayout.TextField("item",item);
// I had no idea how to write this, but thanks to Unity forums, here is a nice Array custom inspector!
// strangely, it shows all the serialized fields from the found property to the end of the class.
		
	serializedObject.Update();
    EditorGUIUtility.LookLikeInspector();

    SerializedProperty myIterator = serializedObject.FindProperty("prereq");
    while (true){
        Rect myRect = GUILayoutUtility.GetRect(0f, 16f);
        bool showChildren = EditorGUI.PropertyField(myRect, myIterator);
		if (!myIterator.NextVisible(showChildren)) break;
	}

    serializedObject.ApplyModifiedProperties()	;
				
	EditorGUIUtility.LookLikeControls();		
		
		bool dirty = EditorGUI.EndChangeCheck();
		if (dirty) EditorUtility.SetDirty(this);
		return dirty;
	}	
#endif
}


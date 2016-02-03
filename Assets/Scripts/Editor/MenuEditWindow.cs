using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MenuEditWindow : EditorWindow {

	public static MenuEditWindow instance = null;
	public ScriptedObject myObject = null;
	public ScriptMenuTreeNode myTree = null; 
//	string indent = "   ";
	Vector2 scrollPos = Vector2.zero;
	SerializedObject serializedObject;
//	int confirmDelete = -1;
	bool rebuild = false;
	
	public static void Init(ScriptedObject SO){
		instance = (MenuEditWindow)EditorWindow.GetWindow(typeof(MenuEditWindow));
		// be sure we don't blow away and edits when switching scripts...

		instance.myObject = SO;
		instance.title = "Menu Category Editor";
		instance.CleanScripts();
		instance.myTree = ScriptMenuTreeNode.BuildMenu(SO);
	}

	
	void OnGUI(){
		
		Color oldColor = GUI.color;
		
		if (rebuild){
			CleanScripts();
			myTree = ScriptMenuTreeNode.BuildMenu(myObject);
			rebuild = false;
		}

		if (myTree== null || myTree.scripts == null){
			GUILayout.Label("I lost my Scripts ... what was that ?");
			return;
		}
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		ShowCategories("",myTree);
		EditorGUILayout.EndScrollView();

		GUI.color = oldColor;
/*
		string pName = "NO ScriptedObject";
		if (myParent != null){ pName = myParent.name;
			GUILayout.Label ("EDITING "+pName+"->"+myScript.name,GUILayout.ExpandWidth(false));
			if (myScript.waitingForDebugger){
				if (GUILayout.Button ("STEP",GUILayout.ExpandWidth(false))){
					myScript.singleStepping = true;
					myScript.waitingForDebugger = false;
				}
				if (GUILayout.Button ("RUN to Break",GUILayout.ExpandWidth(false))){
					myScript.singleStepping = false;
					myScript.waitingForDebugger = false;
				}
			}
			myScript.debug = GUILayout.Toggle(myScript.debug,"dbg",GUILayout.ExpandWidth(false));
			if (AnyBreakpoints() && GUILayout.Button("-bkpts",GUILayout.ExpandWidth(false))) ClearAllBreakpoints();
			if (hasChanged && !hasBeenRunning){
				if (GUILayout.Button ("SAVE to",GUILayout.ExpandWidth(false))){
					myParent.SaveToXML(myParent.XMLName);
					hasChanged = false;
				}
				myParent.XMLName = GUILayout.TextField (myParent.XMLName);
			}
			else
			{
				GUILayout.Button ("UNCHANGED",GUILayout.ExpandWidth(false));	
//			myParent.XMLName = GUILayout.TextField (myParent.XMLName);
			}
		}
		GUILayout.EndHorizontal();
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		indent = ""; // prevent runaway!
		for (int i=0; i< myScript.scriptLines.Length; i++){
			if (confirmDelete == i){
				GUILayout.Label("CONFIRM you want to delete this line? Cannot UNDO (yet)!");
				if (GUILayout.Button("YES, DELETE FOREVER!")){
					myScript.DeleteLineAt(i);
					hasChanged = true;
					confirmDelete = -1;
				}
				if (GUILayout.Button("Umm, no, sorry, I didn't mean it.  Keep the line")){
					confirmDelete = -1;
				}
				
			}
			bool expand = false;
			GUILayout.BeginHorizontal();
			GUI.backgroundColor = Color.grey;
			if (GUILayout.Button (new GUIContent("X",null,"delete line"),GUILayout.ExpandWidth(false))){
//				myScript.DeleteLineAt(i);
//				hasChanged = true;
				confirmDelete = i;
				break; // we've affected the array, don't draw any more gui this frame.
			}
			if (GUILayout.Button (new GUIContent("+",null,"insert line before"),GUILayout.ExpandWidth(false))){
				myScript.InsertLineAt(i);
				hasChanged=true;
				break;
			}
			if (Selection.activeObject != myScript.scriptLines[i].gameObject){
				GUI.backgroundColor = Color.yellow;
				if (GUILayout.Button (new GUIContent(">",null,"EDIT line"),GUILayout.ExpandWidth(false)))
					Selection.activeGameObject = myScript.scriptLines[i].gameObject;
			} else {
				GUI.backgroundColor = Color.red;
				expand = true;
				if (GUILayout.Button (new GUIContent("V",null,"CLOSE line"),GUILayout.ExpandWidth(false)))
					Selection.activeGameObject = myScript.gameObject;
			}
			string prefix = i<10?"  ":"";
			PreSetIndent(myScript.scriptLines[i]);
			if (myScript.scriptLines[i].hasExecuted) GUI.color = Color.yellow;
			if (myScript.readyState == InteractionScript.readiness.executing && myScript.currentLine == i) GUI.color = Color.green;
			GUILayout.Label(prefix+i+":"+indent+myScript.scriptLines[i].PrettyPrint(),GUILayout.Width(450));
			GUI.color = Color.white;
			ShowFlowIcon(myScript.scriptLines[i]);
			PostSetIndent(myScript.scriptLines[i]);
			// if it's a noop, provide a copy from drop target
			if (myScript.scriptLines[i].type == ScriptedAction.actionType.wait 
				&& myScript.scriptLines[i].fadeLength == 0 
				&& myScript.scriptLines[i].stringParam == ""
				&& myScript.scriptLines[i].block == ScriptedAction.blockType.none){
				// copy from dropped scriptedAction
				ScriptedAction droppedSA = null;
				droppedSA = (ScriptedAction)EditorGUILayout.ObjectField("CopyFrom",droppedSA,typeof(ScriptedAction),true,GUILayout.Width(200));
				if (droppedSA != null){
					ScriptedAction.ScriptedActionInfo info = droppedSA.ToInfo(droppedSA);
					myScript.scriptLines[i].InitFrom(info);
				}
			}
			if (GUILayout.Button (new GUIContent("?",null,"show help"),GUILayout.ExpandWidth(false))) helpTarget = myScript.scriptLines[i];
			GUILayout.EndHorizontal();
			if (expand){ // if this is reliable, we may not need the change the current selection
				myScript.scriptLines[i].name = EditorGUILayout.TextField("name:",myScript.scriptLines[i].name);
				myScript.scriptLines[i].ShowInspectorGUI("");
			}
		}
		if (indent != ""){
			GUI.color = Color.red;
			GUILayout.Label ("WARNING: Unbalanced {} - Missing END }");
			GUI.color = Color.white;
		}
		GUI.backgroundColor = Color.grey;
		if (GUILayout.Button ("+",GUILayout.ExpandWidth(false))){
			myScript.InsertLineAt(myScript.scriptLines.Length);
		}
		EditorGUILayout.EndScrollView();
		hasChanged |= GUI.changed;
		if (GUI.changed) EditorUtility.SetDirty(myScript); // see if this helps keep changes
		
		ShowHelp();
*/
	}
	
	void ShowCategories(string indent,ScriptMenuTreeNode node){
		
		bool topLevelScripts = (node == myTree);
		if (topLevelScripts){
			GUI.color = Color.white;
			GUILayout.BeginHorizontal();
			GUILayout.Label ("SCRIPTS WITH NO CATEGORY----------------------",GUILayout.ExpandWidth(false));
				GUI.color = Color.cyan;
				InteractionScript droppedSA = null;
				droppedSA = (InteractionScript)EditorGUILayout.ObjectField("Add Script:",droppedSA,typeof(InteractionScript),true,GUILayout.Width(200));
				if (droppedSA != null && Validate(droppedSA)){
					// add this category name to the script
					AddScript(droppedSA);
					rebuild = true; // rebuild ?
				}			
				GUI.color = Color.white;			
			GUILayout.EndHorizontal();
		}
			
		foreach (InteractionScript script in node.scripts)
		{
			if (script.AddToMenu==true)
				GUI.color = Color.green;
			else
				GUI.color = Color.grey; // not displayed in menus
			// some quick buttons to re-order or remove from category
			GUILayout.BeginHorizontal();
			if (!topLevelScripts){
				if (GUILayout.Button (new GUIContent("X",null,"delete from category"),GUILayout.ExpandWidth(false))){
					if (node.parentName == "")
						script.category.Remove(node.name);
					else
						script.category.Remove(node.parentName+"/"+node.name);
					rebuild = true;
					break; // we've affected the array, don't draw any more gui this frame.
				}			
				if (GUILayout.Button (new GUIContent("^",null,"move up"),GUILayout.ExpandWidth(false))){
					// move up not yet implemented...
					RaiseScript (script);
					rebuild = true;
					break; // we've affected the array, don't draw any more gui this frame.
				}
				if (GUILayout.Button (new GUIContent("v",null,"move down"),GUILayout.ExpandWidth(false))){
					// move up not yet implemented...
					LowerScript (script);
					rebuild = true;
					break; // we've affected the array, don't draw any more gui this frame.
				}
			}
			else
			{
				Color oc = GUI.color;
				GUI.color = Color.red;
				if (GUILayout.Button (new GUIContent("X",null,"remove entirely"),GUILayout.ExpandWidth(false))){
					// move up not yet implemented...
					RemoveScript (script);
					rebuild = true;
					break; // we've affected the array, don't draw any more gui this frame.
				}
				GUI.color = oc;
			}

			GUILayout.Label (indent+"  >"+script.name,GUILayout.ExpandWidth(false));
			if (script.roleKeyString!= null
				&& script.roleKeyString.Length>0 
				&& script.roleKeyString[0] != null){
				GUI.color = Color.red;
				GUILayout.Label ("...only if ",GUILayout.ExpandWidth(false));
				if (script.roleKeyString[0] == ""){
					if (GUILayout.Button (new GUIContent("X",null,"Remove Condition"),GUILayout.ExpandWidth(false))){
					script.roleKeyString = null;
					break;
					}
				}
				script.roleKeyString[0] = GUILayout.TextField(script.roleKeyString[0]);
			}
			else
			{
				if (GUILayout.Button (new GUIContent("?",null,"Add Condition"),GUILayout.ExpandWidth(false))){
					script.roleKeyString = new string[1];
					script.roleKeyString[0] = "";
				}
			}
			GUILayout.EndHorizontal ();
		}		
		
		GUI.color = Color.cyan;
		GUILayout.BeginHorizontal();
		if (GUILayout.Button (new GUIContent("Add Subcategory",null,"Add SubCategory"),GUILayout.ExpandWidth(false))){
			// validate name != ""
			ScriptMenuTreeNode newNode = new ScriptMenuTreeNode();
			newNode.subcategories = new Dictionary<string, ScriptMenuTreeNode>();
			newNode.scripts = new List<InteractionScript>();
			if (node.parentName == "")
				newNode.parentName = node.name;
			else
				newNode.parentName = node.parentName+"/"+node.name;
			newNode.name = node.newSubcategory;
			
			node.subcategories.Add (node.newSubcategory, newNode);
			node.newSubcategory = "";
			// don't rebuild, as we would lose this change.
			// do we have to add SOME script to this category just to keep it real ?
		}
		node.newSubcategory = EditorGUILayout.TextField(node.parentName+"/"+node.name+"/",node.newSubcategory,GUILayout.ExpandWidth(false),GUILayout.Width(400));
		GUILayout.EndHorizontal ();
		
		GUI.color = Color.white;
		foreach (KeyValuePair<string,ScriptMenuTreeNode> kvp in node.subcategories){
			GUILayout.BeginHorizontal();
			GUILayout.Label(indent+kvp.Value.parentName+"/"+kvp.Value.name,GUILayout.ExpandWidth(false));
			//DropTarget for new script in this category
				GUI.color = Color.cyan;
				InteractionScript droppedSA = null;
				droppedSA = (InteractionScript)EditorGUILayout.ObjectField("Add Script to category:",droppedSA,typeof(InteractionScript),true,GUILayout.Width(200));
				if (droppedSA != null  && Validate(droppedSA)){
					// add this category name to the script
					string newName = null;
					if (kvp.Value.parentName == "")
						newName = kvp.Value.name;
					else
						newName = kvp.Value.parentName+"/"+kvp.Value.name;
					// be sure we're not already in there first...
					droppedSA.category.Add (newName);
					rebuild = true; // rebuild ?
				}			
				GUI.color = Color.white;
			GUILayout.EndHorizontal ();
			ShowCategories(indent+"   ",kvp.Value);
		}

		GUI.color = Color.white;
	}
	
	// utilities for rearranging scripts
	
	bool RemoveScript(InteractionScript script){
		int index=-1;
		for (int i=0;i<myObject.scripts.Length;i++){
			if (myObject.scripts[i] == script){
				index = i;
				break;
			}
		}
		if (index < 0){
			return false;	
		}
		InteractionScript[] newScripts = new InteractionScript[myObject.scripts.Length-1];
		for (int i=0; i<index; i++){
			newScripts[i] = myObject.scripts[i];	
		}
		for (int i=index+1; i<myObject.scripts.Length; i++){
			newScripts[i-1] = myObject.scripts[i];	
		}
		myObject.scripts = newScripts;
		return true;
	}
	
	bool AddScript(InteractionScript script){
		int index=-1;
		for (int i=0;i<myObject.scripts.Length;i++){
			if (myObject.scripts[i] == script){
				index = i;
				break;
			}
		}
		if (index >= 0){ //script already in list, don't add
			return false;	
		}
		
		InteractionScript[] newScripts = new InteractionScript[myObject.scripts.Length+1];
		for (int i=0; i<myObject.scripts.Length; i++){
			newScripts[i] = myObject.scripts[i];	
		}
		newScripts[myObject.scripts.Length] = script;	

		myObject.scripts = newScripts;
		return true;
	}
	
	bool RaiseScript(InteractionScript script){
		int index=-1;
		for (int i=0;i<myObject.scripts.Length;i++){
			if (myObject.scripts[i] == script){
				index = i;
				break;
			}
		}
		if (index < 1){
			return false;	
		}
		InteractionScript temp = myObject.scripts[index-1];
		myObject.scripts[index-1] = myObject.scripts[index];
		myObject.scripts[index] = temp;
		return true;
	}
	
	bool LowerScript(InteractionScript script){
		int index=-1;
		for (int i=0;i<myObject.scripts.Length;i++){
			if (myObject.scripts[i] == script){
				index = i;
				break;
			}
		}
		if (index < 0 || index > myObject.scripts.Length-2){
			return false;	
		}
		InteractionScript temp = myObject.scripts[index+1];
		myObject.scripts[index+1] = myObject.scripts[index];
		myObject.scripts[index] = temp;
		return true;
	}
	
	bool CleanScripts(){ // remove any null script
		int index=-1;
		for (int i=0;i<myObject.scripts.Length;i++){
			if (myObject.scripts[i] == null){
				index = i;
				break;
			}
			else{
				// this should only be invoked when the order is wrong, but for now...
				myObject.scripts[i].menuOrder = i;
			}
		}
		if (index < 0){
			return false;	
		}
		InteractionScript[] newScripts = new InteractionScript[myObject.scripts.Length-1];
		for (int i=0; i<index; i++){
			newScripts[i] = myObject.scripts[i];	
		}
		for (int i=index+1; i<myObject.scripts.Length; i++){
			newScripts[i-1] = myObject.scripts[i];	
		}
		myObject.scripts = newScripts;
		return true;
	}
			
	bool Validate(InteractionScript script){
		return (script.transform.parent.gameObject == myObject.gameObject);			
	}

	
	
	public class ScriptMenuTreeNode
	{
		public Dictionary<string, ScriptMenuTreeNode> subcategories; // categories beneath this
		public List<InteractionScript> scripts;
		public ScriptMenuTreeNode parent = null;
		public string parentName = "";
		public string name = "";
		public string newSubcategory = "";
//		bool expanded = false; // is the menu level or interaction shown in detail?
//		bool expandList = false; // for interactions, is the interactionlist at this level shown in detail
		
		ScriptMenuTreeNode newNode; // used when building up new menus
//		bool valid = true; // when adding a new node, set to true when all required fields have values.
		
		// these local varaibles are for the editor GUI, letting you open one task,list or stringmap per node for editing.
		
//		string editingTaskKey = ""; // the task at this node open for edit
//		string editingStrmapKey = "";
//		StringMap editStrmap = new StringMap(); // this persistent value lets us edit a stringmap
		
		public static ScriptMenuTreeNode BuildMenu( ScriptedObject SO ) // pass in the scripted object
	    {
			ScriptMenuTreeNode returnNode = new ScriptMenuTreeNode();
	
			returnNode.subcategories = new Dictionary<string, ScriptMenuTreeNode>();
			returnNode.scripts = new List<InteractionScript>();
	
	        foreach (InteractionScript script in SO.scripts)
		    {
				ScriptMenuTreeNode currentNode = returnNode;
					
				if (script.category == null || script.category.Count==0)
				{
					currentNode.scripts.Add(script);	
				}
				else
				{
					foreach (string cat in script.category)
					{ 
						// find or build out the category tree to point to the desired category
						currentNode = GetNode(returnNode, cat);	
						// add this script to that node's script list
						currentNode.scripts.Add(script);
			                // it's a terminal node, an interaction
					}
				}
		    }
	
			return returnNode;
	    }
		
		public static ScriptMenuTreeNode GetNode(ScriptMenuTreeNode baseNode, string category){
			if (category == "") return baseNode;
				
			ScriptMenuTreeNode currentNode = baseNode;
	
			string[] level = category.Split ('/');
			for (int i = 0; i < level.Length; i++)
			{
				if (currentNode.subcategories.ContainsKey(level[i]))
				{
					currentNode = currentNode.subcategories[level[i]];
				}
				else
				{
					ScriptMenuTreeNode newNode = new ScriptMenuTreeNode();
					newNode.subcategories = new Dictionary<string, ScriptMenuTreeNode>();
					newNode.scripts = new List<InteractionScript>();
					if (currentNode.parentName == "")
						newNode.parentName = currentNode.name;
					else
						newNode.parentName = currentNode.parentName+"/"+currentNode.name;
					newNode.name = level[i];
					currentNode.subcategories.Add(level[i],newNode); 
					currentNode = currentNode.subcategories[level[i]];
				}
			}
			return currentNode;
		}
  	}
}

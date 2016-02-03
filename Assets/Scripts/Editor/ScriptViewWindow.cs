using UnityEngine;
using UnityEditor;
using System.Collections;

public class ScriptViewWindow : EditorWindow {
	public InteractionScript myScript = null;
	public InteractionScript requestedScript = null;
	ScriptedObject myParent = null;
	public string scriptName = "";
	public int scriptInstanceID = 0;
	public static ScriptViewWindow instance = null;
	string indent = "   ";
	Vector2 scrollPos = Vector2.zero;
	bool hasChanged = false;
	bool ignoreChangeOnce = false;
	bool hasBeenRunning = false;
	bool hasLostScript = false;
	bool confirmChange = false;
	SerializedObject serializedObject;
	float refreshtime = 0;
	int confirmDelete = -1;
	ScriptedAction helpTarget = null; // we should generalize this to Object
	Rect helpWindowRect = new Rect(10,10,500,300);
	Vector2 helpScrollPos;
	
	
	public static void Init(InteractionScript script){
		instance = (ScriptViewWindow)EditorWindow.GetWindow(typeof(ScriptViewWindow));
		// be sure we don't blow away and edits when switching scripts...
		instance.requestedScript = script;

		// what if we start running before saving ? we should probably warn for that too.
		if (!(EditorApplication.isPlaying || EditorApplication.isPaused) &&
			instance.hasChanged && 
			instance.myScript != null && 
			instance.myScript!=instance.requestedScript){
			// don't load up yet, put up a confirmation dialog first.
			instance.confirmChange=true;
			return;
		}
		//EditorUtility.InstanceIDToObject
		instance.scriptInstanceID = script.GetInstanceID();
		
		instance.myScript = script;
		instance.title = "Script View";
		instance.scriptName = script.name;
		if (script.transform.parent != null){
			instance.myParent = script.transform.parent.GetComponent<ScriptedObject>();	
		}
		instance.hasChanged = false;
		instance.hasBeenRunning = false;
		EditorApplication.playmodeStateChanged += instance.PlaymodeCallback;
	}
	
//	void Awake(){
//		EditorApplication.playmodeStateChanged += PlaymodeCallback;
//	}
	
	void Update(){
		// if a script hits a breakpointed action, it posts itself in the static atBreakpoint, and waits for a single step command
		if (EditorApplication.isPlaying && InteractionScript.atBreakpoint != null){	
			Init (InteractionScript.atBreakpoint);
			InteractionScript.atBreakpoint = null;		
		}
	}
	

	
	void OnInspectorUpdate(){
		
		if (Application.isPlaying && refreshtime < Time.time && myScript != null){
			// reference ourselves through a child object to init our stale copy at runtime
			Init (myScript.scriptLines[0].transform.parent.GetComponent<InteractionScript>());
			refreshtime = Time.time+1.0f;
		}
	}
	
    public void PlaymodeCallback () {
       // if we have been running, but now we are stopped, get rid of our script because it is stale,
		// and any edits made to that stale copy will be lost.  force the user to open the script again,
		// or re-open it ourselves.
		if (hasBeenRunning && !(EditorApplication.isPlaying || EditorApplication.isPaused)){
			//myScript = null;
			Init (myScript.scriptLines[0].transform.parent.GetComponent<InteractionScript>());
		}
		
    }
	
	
	
	void OnGUI(){
		
		if (confirmChange){
			GUILayout.Label("EXIT WITHOUT SAVING CHANGES?");
			if (GUILayout.Button("YES, EXIT")){
				myScript = requestedScript;
				confirmChange = false;
				Init (myScript);
			}
			if (GUILayout.Button("NO! SAVE!")){
				myParent.SaveToXML(myParent.XMLName);
				hasChanged = false;
				myScript = requestedScript;
				confirmChange = false;
				Init (myScript);
			}
			
			return;	
		}
		
		if (Application.isPlaying) hasBeenRunning = true;
		
		if (myScript != null && hasLostScript){
			// we've gotten our instance back, reinitialize.
			if (myParent != null){
				InteractionScript reconnectInstance = null;
				reconnectInstance = EditorUtility.InstanceIDToObject(scriptInstanceID) as InteractionScript;
				if (reconnectInstance != null){
					Init (reconnectInstance);	
					hasLostScript = false;
				}
			}
			//Init (myScript); // this reconnected with a stale instance
			//hasLostScript = false;
			return;
		}
		
		if (myScript== null || myScript.scriptLines == null){
			GUILayout.Label("I lost my Script when we stopped running... what was it ?");
			hasLostScript = true;
			return;
		}
		GUI.color = Color.white;
		GUILayout.BeginHorizontal();
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
				if (Time.timeScale == 1.0){
					if (GUILayout.Button ("1/10X",GUILayout.ExpandWidth(false))){
						Time.timeScale = 0.1f;
					}
				}
				else
				{
					if (GUILayout.Button ("1X",GUILayout.ExpandWidth(false))){
						Time.timeScale = 1.0f;
					}
				}
			}
			myScript.debug = GUILayout.Toggle(myScript.debug,"dbg",GUILayout.ExpandWidth(false));
			if (AnyBreakpoints() && GUILayout.Button("-bkpts",GUILayout.ExpandWidth(false))) ClearAllBreakpoints();
			if (hasChanged && !hasBeenRunning){
				if (GUILayout.Button ("SAVE to",GUILayout.ExpandWidth(false))){
					myParent.SaveToXML(myParent.XMLName);
					hasChanged = false;
					ignoreChangeOnce = true;
				}
				myParent.XMLName = GUILayout.TextField (myParent.XMLName);
			}
			else
			{
				if (GUILayout.Button ("UNCHANGED (SAVE)",GUILayout.ExpandWidth(false))){
					myParent.SaveToXML(myParent.XMLName);
					hasChanged = false;
					ignoreChangeOnce = true;					
				}
				if (myParent != null){
					if (myParent.XMLName == null) myParent.XMLName = "";
					myParent.XMLName = GUILayout.TextField (myParent.XMLName); // keep the same number of controls ?
				}
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
					ignoreChangeOnce = true;
				}
				
			}
			bool expand = false;
			GUILayout.BeginHorizontal();
			GUI.backgroundColor = Color.grey;
			if (GUILayout.Button (new GUIContent("X",null,"delete line"),GUILayout.ExpandWidth(false))){
//				myScript.DeleteLineAt(i);
//				hasChanged = true;
				confirmDelete = i;
				ignoreChangeOnce = true;
				break; // we've affected the array, don't draw any more gui this frame.
			}
			if (GUILayout.Button (new GUIContent("+",null,"insert line before"),GUILayout.ExpandWidth(false))){
				myScript.InsertLineAt(i);
				hasChanged=true;
				break;
			}
			if (Selection.activeObject != myScript.scriptLines[i].gameObject){
				GUI.backgroundColor = Color.yellow;
				if (GUILayout.Button (new GUIContent(">",null,"EDIT line"),GUILayout.ExpandWidth(false))){
					Selection.activeGameObject = myScript.scriptLines[i].gameObject;
					// Clear keyboard focus to get around text field value retention bug in unity
					GUIUtility.keyboardControl = 0;
					ignoreChangeOnce = true;
				}
			} else {
				GUI.backgroundColor = Color.red;
				expand = true;
				if (GUILayout.Button (new GUIContent("V",null,"CLOSE line"),GUILayout.ExpandWidth(false))){					
					Selection.activeGameObject = myScript.gameObject;
					ignoreChangeOnce = true;
				}
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
			if (GUILayout.Button (new GUIContent("?",null,"show help"),GUILayout.ExpandWidth(false))){
				helpTarget = myScript.scriptLines[i];
				ignoreChangeOnce = true;
			}
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
		if (ignoreChangeOnce)
		{
			hasChanged = false;
			ignoreChangeOnce = false;
		}
		else
		{
			hasChanged |= GUI.changed;
			if (GUI.changed) EditorUtility.SetDirty(myScript); // see if this helps keep changes
		}
		
		ShowHelp();
	}
	
	void PreSetIndent(ScriptedAction line){
		if (line.block == ScriptedAction.blockType.endIfThenElse 
			|| line.block == ScriptedAction.blockType.beginElse){
			if (indent == ""){
				GUI.color = Color.red;
				GUILayout.Label ("WARNING: Unbalanced {} - Extra END }");
				GUI.color = Color.white;
			}
			if (indent.Length > 3)
				indent = indent.Substring(3);
			else {
				indent = "";
			}
		}
	}
	
	void PostSetIndent(ScriptedAction line){
		if (line.type == ScriptedAction.actionType.ifThenElse 
			|| line.block == ScriptedAction.blockType.beginElse
			|| (line.type == ScriptedAction.actionType.putMessage
				&& line.gameMsgForm != null 
				&& line.gameMsgForm.msgType == GameMsgForm.eMsgType.dialogMsg
				&& line.dialogIfThen))
			indent += "|  ";
		
	}
	
	void ClearAllBreakpoints(){
		myScript.debug = false;
		foreach (ScriptedAction sa in myScript.scriptLines){
			sa.breakpoint = false;	
		}
	}
	bool AnyBreakpoints(){
		foreach (ScriptedAction sa in myScript.scriptLines){
			if (sa == null)
				EditorUtility.DisplayDialog ("MISSING LINE!","ScriptLines contains null pointer. Cannot Edit","OK");
			if (sa.breakpoint) return true;	
		}
		return false;
	}
	
	void ShowFlowIcon(ScriptedAction sa){
		if (sa.preAttributes!="" ){
			GUI.color = Color.yellow;
			GUILayout.Label (new GUIContent("~[",null,sa.preAttributes),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		if (sa.postAttributes!="" ){
			GUI.color = Color.yellow;
			GUILayout.Label (new GUIContent("]~",null,sa.postAttributes),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		if (sa.attachmentOverride!="" ){
			GUI.color = Color.cyan;
			GUILayout.Label (new GUIContent("@",null,sa.attachmentOverride),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		
		if (sa.breakpoint ){
			GUI.color = Color.red;
			GUILayout.Label (new GUIContent("*",null,"breakpoint set"),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		if (sa.type == ScriptedAction.actionType.wait && sa.stringParam != ""){
			GUI.color = Color.red;
			GUILayout.Label (new GUIContent("X",null,"waits for condition"),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		if (sa.sequenceEnd){
			GUI.color = Color.red;
			GUILayout.Label (new GUIContent("!",null,"animation event ends"),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
		if (sa.waitForCompletion == false){
			GUI.color = Color.green;
			GUILayout.Label (new GUIContent("O",null,"no wait for completion"),GUILayout.ExpandWidth(false));
			GUI.color = Color.white;
		}
	}
	
	void ShowHelp(){
		if (helpTarget == null) return;
        // Begin Window
        BeginWindows ();        
        // All GUI.Window or GUILayout.Window must come inside here
        helpWindowRect = GUILayout.Window (1, helpWindowRect, DoHelpWindow, "HELP is on the way!");                
        // Collect all the windows between the two.
        EndWindows (); 
    }
    
    // The window function. This works just like ingame GUI.Window
    void DoHelpWindow (int id) {
        if (GUILayout.Button ("Close")){ helpTarget = null; return;}
        helpScrollPos = GUI.BeginScrollView (
            new Rect (0, 40, helpWindowRect.width, helpWindowRect.height), 
            helpScrollPos, 
            new Rect (0, 0, helpWindowRect.width, 2000)
        );
		helpTarget.ShowHelp();        
		GUI.EndScrollView();
		GUI.DragWindow(); 
    }
}

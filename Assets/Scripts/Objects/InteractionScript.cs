using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionScript : MonoBehaviour {
	
	public static InteractionScript atBreakpoint = null; 
	public enum readiness{
		unknown,		// after we've run, until readyFor is called again...
		unavailable,	// keys not met
		readyToRun,		// keys satisfied and characters available
		readyToQueue,	// keys are satisfied, but characters needed might not be ready
		queued,			// script has already been queued to run
		executing,		// script is currently running
		stale,			// we've been queued, but our key is no longer met, so we are stale...
	}
	public string[] triggerStrings = new string[0]; // the string that causes this script to start running
	public bool triggerOnStatus = true; // should we listen to InteractStatusMessages for our trigger ?
	public string startingArgs = "";
	// the scriptedAction lines can be built any way you like, but they often make the most sense attached to individual
	// game objects with names, or you can just attach them all to a single game object, but it's harder to figure out in
	// the editor that way, as they are only distinguished by their order in this array.
	public ScriptedAction[] scriptLines; // the normal execution lines
	public ScriptedAction[] abortBranch; // the way out, if the user chooses not to complete this script
	public ScriptedAction[] errorBranch; // what to do if the script hangs, times out, encounters an error...
	public string owningRole = ""; // overrides ScriptedObject DropTarget on a per-script basis.
	public List<string> roles; // The Roles this script needs to fill so it can run
	public List<ObjectInteraction> actorObjects; // this will contain any secondary actors in the script
	public Dictionary<string,string> roleMap;
	public string[] roleKeyString;  // enter this as a string in the editor, it gets parsed into a node tree on awake.
	BinaryExpressionNode[] roleKeys; // parsed version of above for faster evaluation
	//  binaryExpressionNode userKey;  expression the user must
	public float autoExecuteInterval = -1; // how often we may perform. <=0 means perform once
	public float autoExecuteTimer = 0;
	public float autoExecuteProbability = 0; // 0 = never, 1 = always
	public InteractionSet interactionSet = null;
	public FilterInteractions.CommandVariation commandVariation = null;
	public bool hasExecuted = false;
	public bool callbackConfirmVoice = false; // should character callback, and require voice confirmation.  this is a serious interaction.
	public int currentLine = 0; // just public for debugging
//	public int nestingDepth = 0; // nested if then else depth for End statement handling, incremented by ScriptedAction IfThenElse
//	int[] currentLineForRole;
	// for MultiMesh-TeamRoles scripts, need currentLine per role
	public string nextLineLabel = ""; // used to pass branch results back to script on complete
	bool isRunning = false;
//	bool hasRun = false;
	public bool waitingForDebugger = false;
	public bool singleStepping = false;
	public ScriptedObject caller; // the scripted actions like to affect this guy as a deault
	public ObjectInteraction myOI = null; // ObjectInteraction component of my caller...
	public GameObject myObject;
	public Dictionary<string,string> args = new Dictionary<string, string>(); // key value pairs for this execution. key=value key=value space delimited
									// these are referenced from within the script by "@key" for args where a name can work
									// args can also be set from within the script for passing between roles
	// stuff to create a menu item for this script...
	public bool AddToMenu = true;
	public int menuOrder = 0;
	public int queueCount = 0; // how many times we have been queued, managed by scriptedObject
	
	// maybe we should use an InteractionMap here. for that, we need to use a custom inspector and a map form object.
	public string item = "OBJECT:MENUCLICK";
	public string prettyname = "Text for the Menu";
	public string response = "You clicked me";
	public string response_title="RESPONSE TITLE";
	public string note = "";
	public string tooltip = "Click Me";
	public string sound= "AUDIO:SOUND";
	public string task = "OBJECT:MENUCLICK";
	public bool  log = true;
	public readiness readyState;  // since an object reference is required to calc this, no getter.
	public int startPriority = 4; // 0 is the lowest
	public bool cancellable = false; // for keep-busy scripts that can be stopped for real work
//	bool cancelled = false; // stop at the end of the next action.  Call Cancel() to set.
	public Vector3[] skillVector = null;
	public float extraCost = 0;
	public List<string> prereq;
	public List<string> category;
	public List<string> param;

	public bool debug = false; // print out debug logging while running
	
//	private ArrayList actorList; // all the ObjectInteraction classes that take part in this script
	
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	public class InteractionScriptInfo
	{
		public InteractionScriptInfo(){
		}
		
		public string unityObjectName;
		
	public string[] triggerStrings; // the string that causes this script to start running
	public bool triggerOnStatus; // should we listen to InteractStatusMessages for our trigger ?
	public string startingArgs;
	public ScriptedAction.ScriptedActionInfo[] scriptLines; // the normal execution lines
	public ScriptedAction.ScriptedActionInfo[] abortBranch; // the way out, if the user chooses not to complete this script
	public ScriptedAction.ScriptedActionInfo[] errorBranch; // what to do if the script hangs, times out, encounters an error...
	public string owningRole; // per script override of who should get this script
	public List<string> roles; // if null, then just one role for this script
	public string[] roleKeyString;  // enter this as a string in the editor, it gets parsed into a node tree on awake.
	public float autoExecuteInterval = -1; // 0 = once
	public float autoExecuteProbability = 0;
	public bool AddToMenu = true;
	public int menuOrder = 0;
	public InteractionSet interactionSet = null;
	public FilterInteractions.CommandVariation commandVariation = null;
	public bool	callbackConfirmVoice = false;
	public string item;
	public string prettyname;
	public string response;
	public string response_title;
	public string note;
	public string tooltip;
	public string sound;
	public string task;
	public bool  log;
	public readiness readyState;  // since an object reference is required to calc this, no getter.
	public int startPriority; // 0 is the lowest
	public bool cancellable = false; // is this a task that can be cancelled by a higher priority script
	public Vector3[] skillVector; // for each role, a point in a normal R3 'skill space' for this task
	public float extraCost;
	public List<string> prereq;
	public List<string> category;
	public List<string> param;
	public bool debug; // print out debug logging while running
	}
		
	public InteractionScriptInfo ToInfo(InteractionScript script){ // saves values to an info for serialization (to XML)
		InteractionScriptInfo info = new InteractionScriptInfo();
		
		info.unityObjectName = script.name;
		
		info.triggerStrings = script.triggerStrings; // the string that causes this script to start running
		info.triggerOnStatus = script.triggerOnStatus; // should we listen to InteractStatusMessages for our trigger ?
		info.startingArgs = script.startingArgs;
		info.scriptLines = new ScriptedAction.ScriptedActionInfo[script.scriptLines.Length];
		for (int i = 0; i<script.scriptLines.Length; i++){
			if (script.scriptLines[i] != null){
				info.scriptLines[i] = script.scriptLines[i].ToInfo(script.scriptLines[i]);
			}
			else 
			{
				Debug.Log("Script "+script.name+"contains null line at index "+i);
#if UNITY_EDITOR
				EditorUtility.DisplayDialog("Save Failed","Script "+script.name+"contains null line at index "+i,"OK");
#endif
			}
		}
//	public ScriptedAction.ScriptedActionInfo[] abortBranch; // the way out, if the user chooses not to complete this script
//	public ScriptedAction.ScriptedActionInfo[] errorBranch; // what to do if the script hangs, times out, encounters an error...
		info.owningRole = script.owningRole;
		if (script.roles != null)
			info.roles = new List<string>(script.roles); // if null, then just one role for this script
		info.roleKeyString = script.roleKeyString;  // enter this as a string in the editor, it gets parsed into a node tree on awake.
		info.autoExecuteInterval = script.autoExecuteInterval;
		info.autoExecuteProbability = script.autoExecuteProbability;
		info.AddToMenu = script.AddToMenu;
		info.menuOrder = script.menuOrder;
		info.interactionSet = script.interactionSet;
		info.commandVariation = script.commandVariation;
		info.callbackConfirmVoice = script.callbackConfirmVoice;
		info.item = script.item;
		info.prettyname = script.prettyname;
		info.response = script.response;
		info.response_title = script.response_title;
		info.note = script.note;
		info.tooltip = script.tooltip;
		info.sound = script.sound;
		info.task = script.task;
		info.log = script.log;
		info.readyState = script.readyState;  // since an object reference is required to calc this, no getter.
		info.startPriority = script.startPriority; // 0 is the lowest
		info.cancellable = script.cancellable;
		info.skillVector = script.skillVector;
		info.extraCost = script.extraCost;
		info.prereq = script.prereq;
		info.category = script.category;
		info.param = script.param;
		info.debug = false; // always clear this when serializing.// print out debug logging while running		
		
		return info;
	}
	
	public void InitFrom(InteractionScriptInfo info){
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName; // this should only be done if we have gameobject per script
		
		triggerStrings = info.triggerStrings; // the string that causes this script to start running
		triggerOnStatus = info.triggerOnStatus; // should we listen to InteractStatusMessages for our trigger ?
		startingArgs = info.startingArgs;
		
		scriptLines = new ScriptedAction[info.scriptLines.Length];
		for (int i = 0; i<info.scriptLines.Length; i++){
			GameObject go = new GameObject(info.scriptLines[i].unityObjectName);
			go.transform.parent = this.transform;
			scriptLines[i] = go.AddComponent("ScriptedAction") as ScriptedAction;
			scriptLines[i].InitFrom(info.scriptLines[i]);	
		}
		// resolve any task sync cross references
		for (int j = 0; j<info.scriptLines.Length; j++){
			if (scriptLines[j].syncToTasks != null && scriptLines[j].syncToTasks.Length > 0){
				for (int k=0; k<scriptLines[j].syncToTasks.Length; k++){
					scriptLines[j].syncToTasks[k] = scriptLines[scriptLines[j].syncToTasksIndex[k]];
				}
			}
		}
		
//	public ScriptedAction.ScriptedActionInfo[] abortBranch; // the way out, if the user chooses not to complete this script
//	public ScriptedAction.ScriptedActionInfo[] errorBranch; // what to do if the script hangs, times out, encounters an error...
		owningRole = info.owningRole;
		roles = new List<string>(info.roles);
//		info.roles.CopyTo(roles);
//		roles = info.roles; // if null, then just one role for this script
		roleKeyString = info.roleKeyString;  // enter this as a string in the editor, it gets parsed into a node tree on awake.
		autoExecuteInterval = info.autoExecuteInterval;
		autoExecuteProbability = info.autoExecuteProbability;
		AddToMenu = info.AddToMenu;
		menuOrder = info.menuOrder;
		interactionSet = info.interactionSet;
		commandVariation = info.commandVariation;
		// failsafe in case of blank Cmd but variations present, default to use trigger string or script name
		if (commandVariation != null &&  commandVariation.Cmd == "" && commandVariation.Variations.Count>0){
			commandVariation.Cmd = name; // this is the convention we're using.  Trigger string might contain multiples
			Debug.LogWarning(name+" was missing commandVariations.Cmd value ");
		}
		if (commandVariation != null && commandVariation.Cmd != null && commandVariation.Cmd != "")		
			if (Application.isPlaying)
				FilterInteractions.GetInstance().AddVariation(commandVariation);
		callbackConfirmVoice = info.callbackConfirmVoice;
		item = info.item;
		prettyname = info.prettyname;
		response = info.response;
		response_title = info.response_title;
		note = info.note;
		tooltip = info.tooltip;
		sound = info.sound;
		task = info.task;
		log = info.log;
		readyState = info.readyState;  // since an object reference is required to calc this, no getter.
		startPriority = info.startPriority; // 0 is the lowest
		cancellable = info.cancellable;
		skillVector = info.skillVector;
		extraCost = info.extraCost;
		prereq = info.prereq;
		category = info.category;
		param = info.param;
		debug = info.debug; // print out debug logging while running		
	}
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */	
	
	public void Awake(){
		roleMap = new Dictionary<string, string>();
		roles = new List<string>();
		actorObjects = new List<ObjectInteraction>();		
	}
	

	// Use this for initialization
	public void Start () {
		// we should disable all our scriptedActions so they don't get Update calls, then enable each one as called, letting it disable when required...
		// some lines have to keep running to trigger other stuff, like move calling IsInPosition
		
		// create binary nodes from any key strings
		if (roleKeyString != null && roleKeyString.Length > 0){
			roleKeys = new BinaryExpressionNode[roleKeyString.Length];
			for (int k = 0; k<roleKeyString.Length; k++){
				roleKeys[k] = BinaryExpressionNode.BuildTree(roleKeyString[k]);
			}
		}


		// start with our owner in case we are all about defaulting to our owner !
		if (transform.parent != null){
			ObjectInteraction poi = transform.parent.GetComponent<ObjectInteraction>();
			if (poi != null){
				if (!roles.Contains(poi.Name)) roles.Add (poi.Name);
			}
		}
		
		foreach (ScriptedAction a in scriptLines){
			// if the scripted action specifies a object to affect, and that object has an ObjectInteraction component,
			// add it to our list of actor
			if (a.type == ScriptedAction.actionType.characterTask && a.objectName != ""){
				if (!roles.Contains(a.objectName))
					roles.Add (a.objectName);
			}
			a.enabled = false;
		}
		
		Dispatcher.GetInstance().FillRoles(this);  // temporarily initialze roles
		// Fill Roles also fills out actorObjects
		

	}
	
	public bool isReady(){
		// later, check our user key, etc.
		return !isRunning;
	}
	
	public bool isReadyFor(BaseObject obj){

		if (obj == null) return true;
		// this is going to be more complicated if we go to multiple roles, meaning multiple entities to check against
//		if (debug)
//			Debug.Log (name+" Script Checking isReadyFor "+obj.name);
		if (roleKeys != null && roleKeys.Length > 0)
			return roleKeys[0].Evaluate(obj);
		
		// here, we should check that there is some character who could take each role,
		// a character who is not currently reserved for a higher priority script.
		
		
		// no keys?
		return true; // don't consider executing in this anymore...!isRunning;
	}
	
	public bool isReadyToQueue(BaseObject obj){
		// the script is qualified to run
return true; // we allow queuing of scripts even if they are not ready, the must be ready when dequeued.
//		return isReadyFor (obj);
	}
	
	public bool isReadyToRun(BaseObject obj,bool reserve){
		// the script is qualified to run, and all the actors it needs are available	
		if (obj == null) return true;
		if (!isReadyFor (obj)) return false;
		
		ObjectInteraction oi = obj.GetComponent<ObjectInteraction>();
		if (reserve) oi.NeededFor(this);
		if (oi != null && !oi.IsAvailableFor(this))
			return false;
		bool allAvailable = true;
		if (actorObjects != null){
			foreach (ObjectInteraction actor in actorObjects){
				if (reserve) actor.NeededFor(this);
				if (!actor.IsAvailableFor(this)) allAvailable = false;
			}
		}
		return allAvailable;
	}
	
	public readiness ReadyState(BaseObject obj,bool reserve){
		// figure out how ready we are, and have we been queued or whatever.
		// if we are executing, that trumps all
		if (readyState == readiness.executing) return readyState;
		// if we are queued, we might be stale, otherwise, return queued
		if (queueCount > 0){
			if (isReadyFor(obj)){
				readyState = readiness.queued;
				return readyState;
			}
			else{
				readyState = readiness.stale;
				return readyState;
			}
		}
		// else, just how ready are we ?
		if (isReadyFor(obj)){
			// if obj and all our actors are available, we can run
			if (isReadyToRun (obj,reserve)){
				readyState = readiness.readyToRun;
				return readyState;
			}
			else{
				readyState = readiness.readyToQueue;
				return readyState;					
			}
		}
		
		readyState = readiness.unavailable;
		return readyState;	
	}
	
	public float GetCost(ObjectInteraction obj){
		// lets try something based on:  skill fit, number of lines, distance to the character?
		
		Vector3 skillLocation = new Vector3(0,0,0);
		if (skillVector != null && skillVector.Length>0) skillLocation = skillVector[0];
		
		float cost = extraCost;
		float skillFactor = 1;
		if (obj != null) skillFactor = (1.0f+obj.SkillMetric(skillLocation));
		cost+= scriptLines.Length*skillFactor;
		
		// we could add in a factor for distance to travel to perform this task
		if (obj != null)
			cost += MoveDistance(obj.gameObject);

		// add a penalty if the script isn't ready
		if (!isReadyFor(obj)) cost+= 10;
		if (!isReadyToRun(obj,false)) cost+= 10;
		
		return cost;
	}
	
	public float MoveDistance(GameObject obj){
		// initially, we'll ignore roles, and add up all the charactrer task positions
		float distance=0;
		Vector3 current = obj.transform.position;
		for (int i=0; i< scriptLines.Length; i++){
			if (scriptLines[i].type == ScriptedAction.actionType.characterTask){
				if (scriptLines[i].moveToName != null && scriptLines[i].moveToName != ""){
					GameObject node = GameObject.Find(scriptLines[i].moveToName);
                    if (node != null)
                    {
						distance+=Vector3.Distance(current,node.transform.position);
						current = node.transform.position;
					}
				}
			}
		}
		return distance;
	}
	
	public void Execute(ScriptedObject thisCaller,string argString="", GameObject obj=null){
		caller = thisCaller;

		foreach (ObjectInteraction actor in actorObjects){
			actor.reservedForScript = null; // incase anyone got reserved who doesn't get picked by the dispatcher
		}
		
		// here, we must find a character for each role, assign and reserve them
		Dispatcher.GetInstance().FillRoles(this); // bail if fail
		
		if (obj == null) myObject = caller.gameObject; // the scripted object by default
		else myObject = obj;
		args.Clear(); // clear the dictionary to start
		SetArgs (startingArgs);
		SetArgs(argString);
		currentLine = 0;
		nextLineLabel = "";
		isRunning = true;
		readyState = readiness.executing;
		myOI = caller.GetComponent<ObjectInteraction>();

//Debug.LogWarning(name+" reserving "+myOI.name);

		foreach (ObjectInteraction actor in actorObjects){
			actor.actingInScript = this;
			actor.reservedForScript = null;
		}
		foreach (ScriptedAction sa in scriptLines)
			sa.hasExecuted = false; // really just for debug use, would break 'executeOnlyOnce'
		
		// if there's an interaction set specified, send it to the interaction manager.
		if (interactionSet != null && interactionSet.Name != null && interactionSet.Name != "")
			InteractionMgr.GetInstance().CurrentSet = interactionSet;
		
		if (debug) Debug.Log ("Script "+name+" execution started");
		
		// we're going to have to handle multiple current lines for roles 
		
		if (scriptLines[currentLine] != null)
			scriptLines[currentLine].Execute(this);
		else
			OnScriptComplete("");
	}
	
	public void ExecuteEvent(ScriptedObject thisCaller,string eventName){
		// intended to be called from animation Events, start execution at the named line, and
		// run until an action flagged sequenceEnd=true;
		
		// Look for the line. fail if not found
		int startingIndex=-1;
		for (int i=0;i<scriptLines.Length;i++){
			if (scriptLines[i].name == eventName){
				startingIndex=i;
				break;
			}
		}
		if (startingIndex <0){
			Debug.LogError("AnimationEvent using "+name+" could not find line "+eventName);
			return;
		}
		
		// caller is only used by ExecuteScript and onScriptComplete, so we might go without it...
		caller = thisCaller;
		myObject = caller.gameObject;
		args.Clear(); // do we need any args for events ?
		SetArgs (startingArgs);
		
		currentLine = startingIndex;
		nextLineLabel = "";
		isRunning = true;
		readyState = readiness.executing;
		myOI = caller.GetComponent<ObjectInteraction>();

		if (debug) Debug.Log ("Script "+name+" execution started");
	
		if (scriptLines[currentLine] != null)
			scriptLines[currentLine].ForceExecute(this);
		else
			OnScriptComplete("");
	}
	
	public void Cancel(){
		if (!cancellable) return;
//		cancelled = true;
		// be sure any scripted action, such as a character task or wait for... gets cancelled
		
		// if we're running, call cancel on our current line
		if (readyState == readiness.executing && scriptLines[currentLine] != null)
			scriptLines[currentLine].Cancel(); // this will result in OnScriptComplete("abort") on the next update
	}
	
	public void OnLineComplete(ScriptedAction line){
		// start the next line if there is one	
		if (debug) Debug.Log ("Script "+name+" OnLineComplete from "+line.name+" at "+Time.time+"["+currentLine+"]"+line.error);
		
		if (line.error != ""){
			if (line.error == "abort"){
				OnScriptComplete ("abort");
				return;
			}
			if (line.error == "sequenceEnd"){
				// case for animation events calling scripts.  probably some special cleanup needed here.
				//OnScriptComplete ("sequenceEnd");
				return;
			}		
			
			Debug.Log ("ERROR from script "+gameObject.name+" line "+currentLine+":"+line.error);			
		}
		
		
		if (nextLineLabel != ""){ // label "" moves you to the next line, so 'if' can use that
			if (debug) Debug.Log ("Script "+name+" branching to "+nextLineLabel);
			int next = FindNextLine(nextLineLabel,currentLine+1);
			if (next >= 0)
				currentLine = next;
			else
				currentLine++;
			nextLineLabel = "";
		}
		else
			currentLine++;
		if (scriptLines.Length > currentLine &&  scriptLines[currentLine] != null){
			if (debug) Debug.Log ("Script "+name+" running "+scriptLines[currentLine].name+" ["+currentLine+"]");
			scriptLines[currentLine].Execute(this);
		}
		else
		{
			OnScriptComplete("");
		}
		
	}
	
	public GameObject FindObjectToAffect(ScriptedAction action){ // default, or we could try looking up the name again...
		GameObject objectToAffect = null;	
		string searchName = ResolveArgs(action.objectName).Replace ("\"","");
		if (action.objectName != ""){
				// see if the object name can be mapped through the Roles dictionary for this run...
				if (roleMap.ContainsKey(searchName)){
					searchName = roleMap[searchName];
				}
				objectToAffect = GameObject.Find(searchName);
				// we have a problem with two names here, one used by unity, one by the 						
				if (objectToAffect == null){
					objectToAffect = ObjectManager.GetInstance().GetGameObject(searchName);
				}
			}
			else {
				objectToAffect = myObject; // this is basically the script owner as default.
			}
		return objectToAffect;
		}
	
	
	int FindNextLine(string label,int startingLine){
		// if label is "else", go to the next line of blockType 'else', or 'endifthenelse', whichever is first ( TODO: handle nesting)
		int numeric = 0;
		// allow scary but powerful numeric next line
		if (int.TryParse(label,out numeric)){
			if (numeric >= 0 && numeric < scriptLines.Length)
				return numeric;
		}
				
		
		if (label == "else"){
			for (int i=startingLine;i<scriptLines.Length;i++){
				i=SkipNestedBlocks(i);
				if (scriptLines[i].block == ScriptedAction.blockType.beginElse) return i;
				if (scriptLines[i].block == ScriptedAction.blockType.endIfThenElse ){
					// in the line after the end is an else, it's an else from a previous if, and we have to skip it
					if (scriptLines.Length > i+1 && scriptLines[i+1].block == ScriptedAction.blockType.beginElse)
						return FindNextLine ("endIf",i+2);
					else
						return i+1;	
				}
			}
			return -1; // not found	
		}
		// if Label is "endif", find the next 'endifthenelse'
		if (label == "endIf"){ // there's definitely an else clause
			for (int i=startingLine;i<scriptLines.Length;i++){
				i=SkipNestedBlocks(i);
				if (scriptLines[i].block == ScriptedAction.blockType.endIfThenElse ) return i+1; // execute the line after the marked line	
			}
			return -1; // not found	
		}
		// if Label is "abort", then later we will run the abort branch, but for now we just point to the end.
		if (label == "abort"){  // this has actually been handled by using the 'error' field of the line, so control should never come here.
//Debug.LogError("Script got to nextlinelabel =abort, this should never happen"); // this is now legal from goToLine
			return scriptLines.Length; // one past the end.
		}
		return -1; // not found
	}
				
	int SkipNestedBlocks(int i){
		int newIndex=i;
		
		if (scriptLines[newIndex].type==ScriptedAction.actionType.ifThenElse ||
			(scriptLines[newIndex].type==ScriptedAction.actionType.putMessage &&
			 scriptLines[newIndex].gameMsgForm != null &&
			 scriptLines[newIndex].gameMsgForm.msgType == GameMsgForm.eMsgType.dialogMsg &&
			 scriptLines[newIndex].dialogIfThen)){
			
			newIndex++;
			//newIndex = SkipNestedBlocks(newIndex);
			for (int n = newIndex; n<scriptLines.Length-1;n++){
				n = SkipNestedBlocks(n);
				newIndex = n+1;
				if (scriptLines[n].block == ScriptedAction.blockType.endIfThenElse)
					break;
			}
			
		}
if (debug && newIndex!=i) Debug.Log ("Skipping Nested Block from "+i+" to "+newIndex);
		if (newIndex!=i) return SkipNestedBlocks(newIndex); //be sure we didn't land on another if!
		else return newIndex; 
	}
	
	public void SetArgs(string argString){
		// see if there are any arguments
		// this can be called from a scriptedAction setAttributes #key=value
		if (argString.Length > 0){
			string[] tokens = argString.Split (' ');
			foreach (string s in tokens){
				// handle += and -=
				if (s.Contains("+=")){
					string[]p = s.Split ('=');
					p[0] = p[0].Substring(0,p[0].Length-1);
					float val,inc;
					string sval = args[p[0]];
					if (float.TryParse(sval,out val) && float.TryParse(p[1],out inc)){
						args[p[0]]=(val+inc).ToString();
					}
				}
				else if (s.Contains("-=")){
					string[]p = s.Split ('=');
					p[0] = p[0].Substring(0,p[0].Length-1);
					float val,inc;
					string sval = args[p[0]];
					if (float.TryParse(sval,out val) && float.TryParse(p[1],out inc)){
						args[p[0]]=(val-inc).ToString();
					}
				}
				// each should be in the form key=value
				else if (s.Contains("=")){
					string[] p = s.Split('=');
					p[1] = p[1].Replace(" ",""); // don't allow blanks in these values
					args[p[0]]=p[1];
				}
			}
		}
	}
	
	public string GetArgs(){
		// return a string of the form "name=value name=value"... for current args	
		string retVal = "";
		foreach (KeyValuePair<string,string> kvp in args){
			retVal+=kvp.Key+"="+kvp.Value+" ";			
		}
		if (retVal.Length > 0)
			retVal = retVal.Substring(0,retVal.Length-1);
		return retVal;
	}
	
	public string ResolveArgs(string attributeString){
		if (attributeString == null || attributeString == "") return attributeString;
		
		// this handles both Left and RightHandSide replacement, 
		// which is appropriate for evaluating attribute strings, but not setting them.
		// Only use RHS when setting.
		attributeString = ResolveRHSArgs( attributeString);
		attributeString = ResolveLHSArgs( attributeString);

		return attributeString;
	}
	
	public string ResolveRHSArgs(string attributeString){
		if (attributeString == null || attributeString == "") return attributeString;
				
		// substitute current arg values for any <key>=#arg found in the attribute string
		while (attributeString.Contains("=#")){
			int insertIndex = attributeString.IndexOf ("=#");
			int tailIndex = -1;
			string key = attributeString.Substring(insertIndex+2);
				
			if (key.IndexOf(" ") >= 0){
				key = key.Substring(0,key.IndexOf(" "));
				tailIndex = insertIndex+2+key.Length;
			}
			string val = "null";
			if (args.ContainsKey(key))
				val = args[key];// if the arg isnt found, can we set an empty string for value?
			string tailString = "";
			if (tailIndex >=0)
				tailString = attributeString.Substring(tailIndex); 
			attributeString = attributeString.Substring(0,insertIndex+1) + val + tailString;
		}
		
		return attributeString;
	}	
	
	public string ResolveLHSArgs(string attributeString){
		if (attributeString == null || attributeString == "") return attributeString;
				
		// this code assumes any RHS occurences of # have already been processed
		// substitute current arg values for any #arg=<val> found in the attribute string
		while (attributeString.Contains("#")){
			int insertIndex = attributeString.IndexOf ("#");
			int tailIndex = -1;
			string key = attributeString.Substring(insertIndex+1);
			char[] delims = new char[]{'+','-','<','>','=',' '};
			if (key.IndexOfAny(delims) >= 0){
				key = key.Substring(0,key.IndexOfAny(delims));
				tailIndex = insertIndex+1+key.Length; // this won't be right for "="
			}
			string val = "null";
			
			if (args.ContainsKey(key))
				val = args[key];// if the arg isnt found, can we set an empty string for value?
			string tailString = "";
//Debug.Log ("ResolveArgs converted "+attributeString+" to "+key+val);
//			val = val.Replace("\"","");  // with this string compares fail, without it 
			if (tailIndex >=0)
				tailString = attributeString.Substring(tailIndex); 
			attributeString = attributeString.Substring(0,insertIndex) + val + tailString;
		}
		
		// syntax collision here.  In SetAttributes,
		// any occurence of #arg=<value> should set this script's arg=value, and be stripped from attributeString
		// while in evaluate conditions, #arg should be replaced by its current value, as this routine does.
		
		return attributeString;
	}
	
	public void ExecuteScript(InteractionScript script, string passedArgs, GameObject obj, bool passArgs){
		// this is called by a scripted action, and gives us a chance to add our args to be passed into the called script
		if (passArgs){
			foreach (KeyValuePair<string,string> pair in args){
				if (pair.Value != ""){
					passedArgs = pair.Key+"="+pair.Value+" "+passedArgs;
				}
				else
				{
					passedArgs = pair.Key+" "+passedArgs;
				}
			}
		}
		caller.ExecuteScript(script,passedArgs,obj);
	}
	
	public void QueueScript(InteractionScript script, string passedArgs, GameObject obj, bool passArgs){
		// this is called by a scripted action, and gives us a chance to add our args to be passed into the called script
		if (passArgs){
			foreach (KeyValuePair<string,string> pair in args){
				if (pair.Value != ""){
					passedArgs = pair.Key+"="+pair.Value+" "+passedArgs;
				}
				else
				{
					passedArgs = pair.Key+" "+passedArgs;
				}
			}
		}
		//TODO Are we putting this script onto the wrong object's queue ?  If obj has a scripted object, shouldnt it go onto that queue ?
		ScriptedObject targetSO = obj.GetComponent<ScriptedObject>();
		if (targetSO != null && targetSO != caller){
			Debug.LogWarning("Check if intended behavior, queuing script"+script.name+" for other task character from "+this.name);
			targetSO.QueueScript(script,passedArgs,obj,script.startPriority);
        }else{
			caller.QueueScript(script,passedArgs,obj,script.startPriority); // priority passing added 4/22/13 PAA
		}
	}

	public void OnScriptComplete(string error){
		if (debug) Debug.Log ("Script "+name+" OnScriptComplete");
		
		// we're done.  Assuming no errors, see if we need to send a :COMPLETE message
		string triggerString = "none";
		if (args.ContainsKey("trigger"))
			triggerString = args["trigger"];
		if (triggerString.Contains(":"))
		{
			// send :COMPLETE msg to brain
			string completion;
			if (error == null || error == "") completion = ":COMPLETE";
			else completion = ":"+error.ToUpper();
            InteractStatusMsg msg = new InteractStatusMsg(triggerString + completion);
            Brain.GetInstance().PutMessage(msg);
		}
//		hasRun = true;
		isRunning = false;
		readyState = readiness.unknown; // will update on the next readiness test.
		currentLine = scriptLines.Length; // so don't update a line by accident
		ScriptedObject myCaller = caller;
		caller = null; // have to do this before, because OnScriptComplete could start us up again!
		// release character task actors

		foreach (ObjectInteraction actor in actorObjects){
			actor.ReleasedBy(this, myCaller);
		}

		
		if (myCaller == null)
			Debug.LogWarning("Script Complete but caller was null!!"+name);
		else{
			// added this block 6/19/14 to keep queue from hanging when scripts owned by another
			// object interaction are run from an objects queue
			if (!actorObjects.Contains(myCaller.ObjectInteraction))
				myCaller.ObjectInteraction.ReleasedBy (this, myCaller);
			myCaller.OnScriptComplete(this,error);
		}

		roleMap.Clear(); // clear the assigned roles so they don't get reserved by accident
		actorObjects.Clear();
	}

	public bool SanityCheck()
	{
		if (scriptLines.Length > currentLine &&  scriptLines[currentLine] != null)
			return true;
		else
			return false;
	}
	
	// Update is called once per frame from the controlling ScriptedObject
	public void UpdateScript () {
		if (scriptLines.Length > currentLine &&  scriptLines[currentLine] != null)
			scriptLines[currentLine].UpdateLine();	
	}
	
	public void Abort() {
		if (scriptLines.Length > currentLine &&  scriptLines[currentLine] != null)
			scriptLines[currentLine].Cancel();	
	}
	
	public InteractionMap ToMap(string objectName){
		InteractionMap map = new InteractionMap(item,prettyname,response_title,note,tooltip,sound,task,log); // using prettyname for response PAA 7/3/15
				
		map.scriptName = name;
		map.objectName = objectName;
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
		map.readyState = readyState; // assume this got set the last time isReady<> was called
		map.startPriority = startPriority;
		
		return map;
	}
	
	public void InsertLineAt(int index){
		// create a new child game object with a default scripted action and link it in	
		GameObject newChild = new GameObject("newScriptLine-noop");
		ScriptedAction newSA = newChild.AddComponent("ScriptedAction") as ScriptedAction;
		newSA.type = ScriptedAction.actionType.wait;
		newSA.fadeLength = 0;
		newChild.transform.parent = transform;
		ScriptedAction[] tmp = new ScriptedAction[ scriptLines.Length];
		for (int i=0; i < scriptLines.Length; i++){
			tmp[i]= scriptLines[i];
		}
		scriptLines = new ScriptedAction[tmp.Length+1];
		for (int i=0; i < index; i++){
			scriptLines[i] = tmp[i];
		}
		scriptLines[index] = newSA;
		for (int i=index+1; i < tmp.Length+1; i++){
			scriptLines[i] = tmp[i-1];
		}		
	}
	
	public void DeleteLineAt(int index){
		// remove a linked script line and destroy the child game object 
		
		DestroyImmediate(scriptLines[index].gameObject);
		ScriptedAction[] tmp = new ScriptedAction[ scriptLines.Length];
		for (int i=0; i < scriptLines.Length; i++){
			tmp[i]= scriptLines[i];
		}
		scriptLines = new ScriptedAction[tmp.Length-1];
		for (int i=0; i < index; i++){
			scriptLines[i] = tmp[i];
		}
		for (int i=index+1; i < tmp.Length; i++){
			scriptLines[i-1] = tmp[i];
		}
				
	}
}

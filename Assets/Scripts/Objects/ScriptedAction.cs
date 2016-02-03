//#define DEBUG_SCRIPTING
//#define DEBUG_DIALOG_CALLBACK

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
//using System.IO;
//using System.Xml.Serialization;

public class ScriptedAction : MonoBehaviour {
	
	public enum actionType{ // extend this to include new types, and add them to the execute and update as needed
		enableInteraction,
		playAudio,
		playAnimation,
		putMessage,
		move,		// implemented for camera only at this point...
		fade,
		ifThenElse, // use this for goto: if true label else label
		executeScript,
		wait,		// wait -1 means wait for all roles to catch up.
		characterTask, // a task like legacy Trauma_05 tasks, with Position, Posture, AnimatedIntercation and cross-action synchronization
		attach, 
		spawn,
		destroy,
		unityMessage,
		lockPosition,  // hold avatar in position and 
		goToLine,			// used to end the script (abort), or to carefully branch...
		queueScript,   // add a script to the queue for later execution
		cancelScript, // searches for a script, aborts it if it is running, dequeues it if it's queued.
		setIKTarget,	// set or clear the left or right hand IK target on a task character
	}
	public enum blockType{ // used to enclose if/then/else blocks
		none,
		beginElse, // we'll jump here if false
		endIfThenElse, // we'll jump here if false and no else clause
	}
	
	public string comment = "";
	public GameObject objectToAffect;
	public string objectName = "";
	public actionType type;
	public string role = ""; // in a multi-role script, which role performs this action
	public string stringParam = "";  // used for a lot of different things
//	public Transform myParent;
	public AudioClip audioClip = null;
	public float fadeLength = 0.0f; // also used for wait, move
	public float desiredAlpha = 0.0f;
	public Color desiredColor = new Color(0.5f,0.5f,0.5f,1);
	public Texture2D texture2D = null; // used for enableInteraction;
	public Transform moveTo;
	public string moveToName = "";
	public Vector3 offset = Vector3.zero; // offset from transform for move, in transform's reference frame
	public Vector3 orientation = Vector3.zero;
	public InteractionScript scriptToExecute; // the name of this script should be in stringParam2
	public bool ease = true;
	public bool negate = false; // use to turn enable to disable, stop audio, etc.
//	bool isFading = false;
	public bool loop = false;
	public bool waitForCompletion = true; // signal complete after audio, fade, etc.
	public bool sequenceEnd = false; // indicates this is the final line of a sequence for animationScriptEvents
	public bool executeOnlyOnce = false;
	public blockType block = blockType.none;
	public bool dialogIfThen = false;
	public bool breakpoint = false;
	//	used to +set or -clear, can set to expression  key=value space delimited
	// use #key=value to set a script argument for later use - this might be syntactically broken by resolveArgs
	public string preAttributes = ""; // processed when the line is begun
	public string postAttributes = ""; // processed when the line completes
	public string stringParam2 = "";
	public string stringParam3 = "";
	public string stringParam4 = "";
	public string attachmentOverride = "";
	public string eventScript = "";
	public string voiceTag = "";
	public float heading = 0;
	public float speed = 1.0f;
	public bool taskReady = false; // to synchronize animated characterTasks until we have roles...
	public bool hasExecuted = false;
	public string error = ""; // could use to flag an error
	bool forceExecute = false; // used to execute without waiting for character idle
	float navStartTime = 0;
	bool waitingForNav = false; // for a move command, we are waiting... FadeTime could be a timeout.
	bool characterTaskPending = false;
	bool waitingForAnim = false; // could merge some of these flags...
	float animEndTime = -1;
	bool waitingForCondition = false;
	bool ignoreTimeout=false;
	bool waitingForUpdate = false; // a single update call will complete us
	bool waitingForDialog = false;
//	bool performingFade = false;
//	Color fadeBeginColor;
//	float fadeBeginTime = 0;
	bool runIndependentUpdates = false; // used to update navigation, etc, if this line doesn't wait for completion and needs updates
	BinaryExpressionNode conditionNode = null; 
	bool pingTaskCharacter = false;
	bool trackCameraLookat = false;
	NavMeshAgentWrapper navWrapper = null;
	TaskCharacter taskChar = null;
	float postureChangeStartTime;
	NavMeshAgentWrapper nmaWrapper = null;
	GameObject dummyGO; //used for move
	public InteractionScript executedBy; // runtime use, public for debug display access by custom inspector
//	public GameMsg gameMsg; // the custom ShowInspectorGUI below allows us to view and edit these
	public GameMsgForm gameMsgForm;
//	public Dialog dialog;
	public ScriptedAction[] syncToTasks; // this has to be the last public field for the inspector thingy to work...
	public int[] syncToTasksIndex; // need this to restore from XML for forward referenced actions
	
	string desiredPosture = "";	
#if UNITY_EDITOR
	SerializedObject serializedObject;
#endif
	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	public class ScriptedActionInfo
	{
		public ScriptedActionInfo(){
		}
		
		public string unityObjectName;
		
//			public GameObject objectToAffect;
	public string comment;
	public string objectName;
	public actionType type;
	public string role; // in a multi-role script, which role performs this action
	public string stringParam;  // used for a lot of different things
	public string audioClipName;
	public float fadeLength; // also used for wait, move
	public float desiredAlpha;
	public Color desiredColor;
	public string texture2Dname; // used for enableInteraction;
//	public Transform moveTo;
	public string moveToName;
	public Vector3 offset; // offset from transform for move, in transform's reference frame
	public Vector3 orientation;
//	public string scriptToExecuteName; // the name of a script is in stringParam2 already.
	public bool ease;
	public bool negate; // use to turn enable to disable, stop audio, etc.
	public bool loop;
	public bool waitForCompletion; // signal complete after audio, fade, etc.
	public bool executeOnlyOnce;
	public bool sequenceEnd;
	public blockType block;
	public bool dialogIfThen;
	public bool breakpoint;
	public string preAttributes; // processed when the line is begun
	public string postAttributes; // processed when the line completes
	public string stringParam2;
	public string stringParam3;
	public string stringParam4;
	public string attachmentOverride;
	public string eventScript;
	public string voiceTag;
	public float heading=0;
	public float speed=1.0f;
		
	public GameMsgForm.GameMsgFormInfo gameMsgFormInfo;
	public InteractionMap map;


//	public GameMsgForm gameMsgForm;
	public int[] syncToTasksIndex; // this has to be the last public field for the inspector thingy to work...
	}
		
	public ScriptedActionInfo ToInfo(ScriptedAction sa){ // saves values to an info for serialization (to XML)
		ScriptedActionInfo info = new ScriptedActionInfo();
		
		info.unityObjectName = sa.name;
		
		info.comment = sa.comment;
		info.objectName = sa.objectName;
		info.type = sa.type;
		info.role = sa.role; // in a multi-role script, which role performs this action
		info.stringParam = sa.stringParam;  // used for a lot of different things
//info.audioClipName = sa.audioClipName;
		info.fadeLength = sa.fadeLength; // also used for wait, move
		info.desiredAlpha = sa.desiredAlpha;
		info.desiredColor = sa.desiredColor;
//	public Texture2Dname texture2D = null; // used for enableInteraction;
//	public Transform moveTo;
		info.moveToName = sa.moveToName;
		info.offset = sa.offset; // offset from transform for move, in transform's reference frame
		info.orientation = sa.orientation;
		if (scriptToExecute != null)
			info.stringParam2 = scriptToExecute.name; //name is in
		info.ease = sa.ease;
		info.negate = sa.negate; // use to turn enable to disable, stop audio, etc.
		info.loop = sa.loop;
		info.waitForCompletion = sa.waitForCompletion; // signal complete after audio, fade, etc.
		info.sequenceEnd = sa.sequenceEnd;
		info.executeOnlyOnce = sa.executeOnlyOnce;
		info.block = sa.block;
		info.dialogIfThen = sa.dialogIfThen;
		info.breakpoint = false; // always clear when saving. sa.breakpoint;
		info.preAttributes = sa.preAttributes; // processed when the line is begun
		info.postAttributes = sa.postAttributes; // processed when the line completes
		info.stringParam2 = sa.stringParam2;
		info.stringParam3 = sa.stringParam3;
		info.stringParam4 = sa.stringParam4;
		info.attachmentOverride = sa.attachmentOverride;
		info.eventScript = sa.eventScript;
		info.voiceTag = sa.voiceTag;
		info.heading = sa.heading;
		info.speed = sa.speed;
		
		if (sa.gameMsgForm != null){
			info.gameMsgFormInfo = sa.gameMsgForm.ToInfo(sa.gameMsgForm);
//			info.gameMsg = sa.gameMsgForm.ToGameMsg(this); // how to handle message subclasses ?
			if (sa.gameMsgForm.map != null){
				info.map = sa.gameMsgForm.map.GetMap(); 
			}
		}
		if (sa.type==actionType.characterTask && sa.syncToTasks != null && sa.syncToTasks.Length > 0){
			info.syncToTasksIndex = new int[sa.syncToTasks.Length];
			for (int t = 0; t<sa.syncToTasks.Length; t++){
				if (sa.syncToTasks[t]==null){
					Debug.LogError("null sync task encountered in scripted action"+name);
					info.syncToTasksIndex[t] = sa.GetLineNumber();
				}
				else
				{
					info.syncToTasksIndex[t] = sa.syncToTasks[t].GetLineNumber();
				}
			}
		}
		
//	info.gameMsg = gameMsgForm.ToMessage();
//gameMsg;
//map;
		
		return info;
	}
	
	public int GetLineNumber(){
		// return what line we are in our parent Script
		InteractionScript IS = transform.parent.GetComponent<InteractionScript>();
		if (IS != null){
			for (int i=0; i< IS.scriptLines.Length; i++){
				if (IS.scriptLines[i] == this)
					return i;
			}
		}
		return -1;
	}
	
	public void InitFrom(ScriptedActionInfo info){
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName;
		
		comment = info.comment;
		objectName = info.objectName;
		type = info.type;
		role = info.role; // in a multi-role script, which role performs this action
		stringParam = info.stringParam;  // used for a lot of different things
//info.audioClipName = sa.audioClipName;
		fadeLength = info.fadeLength; // also used for wait, move
		desiredAlpha = info.desiredAlpha;
		desiredColor = info.desiredColor;
//	public Texture2Dname texture2D = null; // used for enableInteraction;
//	public Transform moveTo;
		moveToName = info.moveToName;
		offset = info.offset; // offset from transform for move, in transform's reference frame
		orientation = info.orientation;
//		if (info.scriptToExecuteName != null && info.scriptToExecuteName){
			// this probably need a redesign, where the search occurs at Start() time, since the script
//			info.scriptToExecuteName = scriptToExecute.name;
//		}
		ease = info.ease;
		negate = info.negate; // use to turn enable to disable, stop audio, etc.
		loop = info.loop;
		waitForCompletion = info.waitForCompletion; // signal complete after audio, fade, etc.
		sequenceEnd = info.sequenceEnd;
		executeOnlyOnce = info.executeOnlyOnce;
		block = info.block;
		dialogIfThen = info.dialogIfThen;
		breakpoint = info.breakpoint;
		preAttributes = info.preAttributes; // processed when the line is begun
		postAttributes = info.postAttributes; // processed when the line completes
		stringParam2 = info.stringParam2;
		stringParam3 = info.stringParam3;
		stringParam4 = info.stringParam4;
		attachmentOverride = info.attachmentOverride;
		eventScript = info.eventScript;
		voiceTag = info.voiceTag;	
		heading = info.heading;
		speed = info.speed;
		
		if (info.gameMsgFormInfo != null){
			gameMsgForm = gameObject.AddComponent<GameMsgForm>();
			gameMsgForm.InitFrom(info.gameMsgFormInfo);
			
//			info.gameMsg = sa.gameMsgForm.ToGameMsg(this); // how to handle message subclasses ?
			if (info.map != null){
				gameMsgForm.map = gameObject.AddComponent<InteractionMapForm>();
				gameMsgForm.map.InitFromMap(info.map);
			}
		}
		syncToTasksIndex = info.syncToTasksIndex;
		if (syncToTasksIndex != null)
			syncToTasks = new ScriptedAction[syncToTasksIndex.Length]; // will init when script is fully loaded.
		else
			syncToTasks = new ScriptedAction[0];
	}
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */	
	// Use this for initialization
	void Start () {
		
	}
	
	void Awake(){

	}
	
	// routine to pause execution of this script line until the debugger says to continue or step
	// the script editor will clear this flag when a button is pressed.
	IEnumerator WaitForDebugger(InteractionScript script){
		while (script.waitingForDebugger){
			yield return new WaitForSeconds(1);
		}		
	}
	
	// Update is called once per frame
	void Update() {
		if (runIndependentUpdates) // this is done when a script line doesn't 'WaitForCompletion', but still needs
									// updates to occur after the executing script has moved on to updating its next line
			UpdateLine();
	}
	
	public void UpdateLine() { // called from the owning script, from the controlling ScriptedObject
		// there will be stuff here for fade and move
		
		if (waitingForUpdate){ // we called a subroutine script, and control has returned to us, so we can continue
			waitingForUpdate = false;
			OnComplete();
			Cleanup ();
		}
		
		// handle legacy characterTask  is there a LookAt somewhere here ?  We should have that for move and animate, too.
		if (characterTaskPending){
			// we wait for position, wait for posture, wait for everybody else to be ready, then we all animate together...
			if (!taskReady){
				// should compute and save the substituted value of movetoname as this is called every frame while moving
				if (moveToName == "" || taskChar.IsInPosition(SubstituteArgsAndAttributes(moveToName))){
					// moved this next line from outside the 'if' because it seems correct in here.
					if (nmaWrapper != null) nmaWrapper.HoldPosition(true); // i am in place, dont move me.
					if (postureChangeStartTime == 0){
						postureChangeStartTime = Time.time;	
						desiredPosture = ProcessIKCommands(stringParam);
					}
					if (desiredPosture == "" || taskChar.IsInPosture(desiredPosture)){
						taskReady = true;
						// this seems like a really good time to play the voice ?
						// we could send a message the voice manager will intercept, or we could just play the thing...
						if (voiceTag != ""){
							VoiceMgr.GetInstance().Play (objectToAffect.name, voiceTag);	
						}
if (executedBy.debug) Debug.Log ("ScriptedAction "+name+" inPosition "+moveToName+" and inPosture "+desiredPosture);
					}
					else
					{
						// check for posture switch hang.  this should be moved into the animation manager
						if (stringParam != "" && (Time.time - postureChangeStartTime) > 5.0f){
							taskChar.GetComponent<AnimationManager>().NextPosture(desiredPosture, true);
							taskChar.ChangePostureInstant(desiredPosture);	
							taskReady = true;
							// this seems like a really good time to play the voice ?
							// we could send a message the voice manager will intercept, or we could just play the thing...
							if (voiceTag != ""){
								VoiceMgr.GetInstance().Play (objectToAffect.name, voiceTag);	
							}
						}
					}
				}
			}
			else
			{
				bool allReady = true;
				foreach ( ScriptedAction s in syncToTasks){
					if (!s.taskReady)
					{
						allReady = false;
						break;
					}
				}
				if (allReady){
					// Now it's just animation
					characterTaskPending = false;
					
					// see if there's a look at ?  // this has been set in the original parsing of the action...
					// and also for TTS
					// the voice manager will have set this to the audio clip length and lookAt from the voicemap...
					// we need to get the map and set it outselves here if we want this to be right  PAA 4/16/15
					if (stringParam2 != "" && taskChar != null){
						VoiceMap vm = null;
						if (voiceTag != "") vm = VoiceMgr.GetInstance().Find( taskChar.Name, voiceTag);
						if (vm == null){
							taskChar.LookAt(stringParam2, Time.time + 5); // we need to figure out how long, really.
						} else {
							float length = 5;
							if (vm.Clip != null) length = vm.Clip.length;
							if (vm.Clip != null && vm.Clip.name != null && vm.Text != null &&
								vm.Clip.name.Contains ("missingAudio") && vm.Text != "") length = 2 + vm.Text.Length/15.0f;
							taskChar.LookAt(stringParam2, Time.time + length);
						}
					}
					
					
					if (stringParam3 != "")
					{
						// possibly handle <animation> IK <side> <target> <blend> <hold>
						string[] animargs = stringParam3.Split (' ');
						if (animargs.Length > 1 && animargs[1].ToLower() == "ik"){
	
							IKArmController ctlr = taskChar.IKArmRight;
							if (animargs[2].ToLower().Contains ("left"))
								ctlr = taskChar.IKArmLeft;
							
							if (animargs[3].ToLower() == "null"){
								// we are clearing the target
								ctlr.target = null;
							}
							else
							{	
								GameObject targetGo = GameObject.Find(animargs[3]);
								ctlr.target = targetGo.transform;
								HandPoser poser = targetGo.GetComponent<HandPoser>();
								if (poser != null)
									poser.Setup(ctlr.hand);
							}
							float blendTime = 0;
							float.TryParse(animargs[4], out blendTime);
							ctlr.blendTime = blendTime;
			
							ctlr.offset = offset;
							ctlr.orientation = Quaternion.Euler(orientation);
							if (stringParam2 == null || stringParam2 == "") stringParam2 = "0";
							float releaseTime = 0;
							float.TryParse(animargs[5], out releaseTime);
							ctlr.releaseTime = releaseTime;							
						}
						
						taskChar.Animate(SubstituteArgsAndAttributes(animargs[0]));	
if (executedBy.debug) Debug.Log ("ScriptedAction "+name+" character task calling animate "+animargs[0]);
						// cleanup, and possibly OnComplete, after the animation finishes.
						if (waitForCompletion){
							waitingForAnim = true;
							animEndTime = -1; // failsafe for hung waiting for anim to end...
							AnimationState ast = taskChar.GetComponent<AnimationManager>().body.animation[SubstituteArgsAndAttributes(animargs[0])];
							if (ast != null && ast.clip != null)
								animEndTime = Time.time + ast.clip.length + 0.05f;; 
						}
						else
						{   // added 11/15/13  allow script to continue while animation plays
							// if we're not waiting for completion, then clear the hold position
							if (nmaWrapper != null) nmaWrapper.HoldPosition(false);
							Cleanup();
						}

					}
					else
					{
						// no animation, we can leave
						if (nmaWrapper != null) nmaWrapper.HoldPosition(false); // everyone should be in place now.
						if (waitForCompletion) OnComplete ();
						Cleanup();
					}
				}
			}
		}
		
		if (waitingForNav){
			// are we there yet ?
			if (taskChar.IsInPosition(moveTo.name)){
				waitingForNav = false;
if (executedBy.debug) Debug.Log ("ScriptedAction "+name+" navigated to "+moveToName);
				if (waitForCompletion) OnComplete();
				Cleanup ();
			}
			else
			{
				// should we timeout and just snap there?
				if ((fadeLength > 0) && (Time.realtimeSinceStartup-navStartTime > fadeLength)){
					waitingForNav = false;
					navWrapper.transform.position = moveTo.position;
					navWrapper.transform.rotation = moveTo.rotation;
					if (waitForCompletion) OnComplete();
					Cleanup ();
				}
			}
		}
		if (pingTaskCharacter){ // this is just to cause LERP if we didn't wait for completion
			pingTaskCharacter = !taskChar.IsInPosition (moveTo.name); // this ping is needed to trigger alignment
			if (!pingTaskCharacter){
				Cleanup ();
				
			}
		}
		
		if (trackCameraLookat){ // this is just used when moving the camera back to it's rail
			CameraRailCoordinator crc = FindObjectOfType(typeof(CameraRailCoordinator)) as CameraRailCoordinator; 
			crc.MovingLookAt(moveTo.position);
			//crc.LookingAt();
			// there is a co-routine that will clear trackCameraLookat, and then Cleanup, disabling this scriptedAction.
		}
		
		if (waitingForAnim){
			if (taskChar){
				waitingForAnim = taskChar.animating; // have observed standWalk looping when animation
														// should have been playing here.
														// could we detect and avoid ? (PlayNext?)
				// character should NOT be moving at this point...
				if (waitingForAnim &&
				    taskChar.GetComponent<AnimationManager>() != null &&
					taskChar.GetComponent<AnimationManager>().AnimState == CharacterAnimState.Moving)
				{
					taskChar.GetComponent<AnimationManager>().StopWalk(0.5f);
					// PHIL, I HAD TO DISABLE THIS BECAUSE IT WAS HAPPENING A LOT...ENOUGH TO NOT ALLOW US TO RUN!
					Debug.LogError(taskChar.name+" was in Moving state while waiting for animation to end."+stringParam3);
				}
				if (waitingForAnim && animEndTime > 0 && Time.time > animEndTime){
					Debug.LogError(taskChar.name+" was still animating after expected time for animation to end."+stringParam3);
					taskChar.animating = false; // can we ?
				}
			}
			else
				waitingForAnim = objectToAffect.animation.isPlaying;// need to be more specific
			
			if (!waitingForAnim){
				if (nmaWrapper != null) nmaWrapper.HoldPosition(false); // everyone should be in place now.
if (executedBy.debug) Debug.Log ("ScriptedAction "+name+" animation completed.");
				if (waitForCompletion) OnComplete ();
				Cleanup ();
			}
		}
		if (waitingForCondition){
			if (conditionNode.Evaluate(objectToAffect.GetComponent<BaseObject>())){
				if (loop){ // clear the hold position flag
					NavMeshAgentWrapper w = objectToAffect.GetComponent<NavMeshAgentWrapper>();	
					if (w != null)
						w.HoldPosition(false);
				}
				ignoreTimeout=true;
				OnComplete ();
				Cleanup ();
			}
		}
	}
	public void ForceExecute(InteractionScript callingScript){
		forceExecute = true;
		Execute(callingScript);
	}
	
	public void Execute(InteractionScript callingScript){
//Debug.Log ("XXX"+Time.time+" "+name+type);		
		if ((breakpoint || callingScript.singleStepping)  && callingScript.debug){
			// place a breakpoint here and set breakpoint=true to trap on execute of a particular line of script
			Debug.Log ("Hit Execute Breakpoint for "+name+" of "+callingScript.name);
			InteractionScript.atBreakpoint = callingScript;
			callingScript.waitingForDebugger = true;
			Debug.Break ();
			WaitForDebugger (callingScript);
Debug.Log ("Ran Right Past the Call");
			
		}		
		
		executedBy = callingScript;
/*		moved to Calling script to handle Role mapping
 * 
		if (objectToAffect == null){ // default, or we could try looking up the name again...
			if (objectName != ""){
				objectToAffect = GameObject.Find(executedBy.ResolveArgs(objectName).Replace ("\"",""));
				// we have a problem with two names here, one used by unity, one by the 						
				if (objectToAffect == null){
					objectToAffect = ObjectManager.GetInstance().GetGameObject(objectName);
				}
			}
			else
				objectToAffect = executedBy.myObject; 
		}
*/
		
		if (executedBy != null)
			objectToAffect = executedBy.FindObjectToAffect(this);
		
		
		if (type != actionType.putMessage && objectToAffect != null) // don't need a taskCharacter to send a message...
			taskChar = objectToAffect.GetComponent<TaskCharacter>();
		
		if (!forceExecute){
			if (taskChar != null && taskChar.executingScript != null && taskChar.executingScript != this){
				// this character is already busy, so wait until the current line completes	
				StartCoroutine (ExecuteWhenIdle(callingScript));
				return;
			}
			
			if (taskChar != null){
				taskChar.executingScript = this;
				if (taskChar.actingInScript != null && taskChar.actingInScript != executedBy){// && CanCompleteImmediately()){
					// don't add me, I'll be done before you know it...
				}
				else
				{
				taskChar.actingInScript = executedBy; // this could get cleared by this character doing an executeScript.
					// since we set it, be sure we're in the list of actor objects so we'll be released
					if (!executedBy.actorObjects.Contains(taskChar as ObjectInteraction))
						executedBy.actorObjects.Add (taskChar as ObjectInteraction);
				}
			}
		}
		forceExecute = false;
		
		
		
#if DEBUG_SCRIPTING
		Debug.Log ("ScriptedAction execute "+name);
#endif
		if (breakpoint && callingScript.debug){
			// place a breakpoint here and set breakpoint=true to trap on execute of a particular line of script
			Debug.Log ("Hit Execute Breakpoint for "+name+" of "+callingScript.name);	
		}
		
		if (hasExecuted && executeOnlyOnce){
			error = "already executed";
			OnComplete();	
		}


		characterTaskPending = false;
		waitingForUpdate = false; // a single update call will complete us
		waitingForDialog = false;
		taskReady = false;
		waitingForNav = false;
		waitingForCondition = false;
		ignoreTimeout = false;
		waitingForAnim = false;
		runIndependentUpdates = false;
		trackCameraLookat = false;
		postureChangeStartTime = 0;
		executedBy = callingScript;
		error = "";
		this.enabled = true; // need updates until we are through
		
		// Temporary hack to add any InteractMessage map to the character to avoid an error
		if (objectToAffect != null && 
			type == actionType.putMessage && 
			gameMsgForm.msgType == GameMsgForm.eMsgType.interactMsg){
			// we are going to add this interaction to the objects AllMaps so it doesn't get an error
			ObjectInteraction OI = objectToAffect.GetComponent<ObjectInteraction>();
			if (OI != null)
				OI.AddToAllMaps(gameMsgForm.map.GetMap());
		}
		
		if (preAttributes != "") SetAttributes(objectToAffect,preAttributes);
		
		// can't do this here, need to do it when we use the values, and don't overwrite the original ones!
//		stringParam = SubstituteArgsAndAttributes(stringParam); // update parameter strings with current #args, $attrs
//		stringParam2 = SubstituteArgsAndAttributes(stringParam2);
//		stringParam3 = SubstituteArgsAndAttributes(stringParam3);
//		stringParam4 = SubstituteArgsAndAttributes(stringParam4);
//		attachmentOverride = SubstituteArgsAndAttributes(attachmentOverride);
		
		if (type == actionType.enableInteraction){

			if (objectName == "Dispatcher"){
				// this will cause all interaction tags except the space delimited list to be rejected
				// until an interaction on the list is hit, which then re-enables all interactions.
				// (needed so scripts can use tags to trigger things when running)
				// an empty list will allow all interactions again.
				Dispatcher td = FindObjectOfType<Dispatcher>();
				if (td != null){
					td.LimitInteractions( stringParam, negate, loop );
				}

				OnComplete();
				Cleanup ();
			}
			else
			{
				// look for an ObjectInteraction component on ObjectToAffect
				if (objectToAffect != null){
					if (objectToAffect.GetComponent<ObjectInteraction>() != null){
						ObjectInteraction OI = objectToAffect.GetComponent<ObjectInteraction>();
						OI.Enabled = !negate;
						
						if (ease || texture2D!= null){ //hackfully abuse the 'ease' boolean to force clear the icon texture
							if (ease) OI.iconTexture = null;
							else if (texture2D!= null) OI.iconTexture = texture2D;
						}
					}
					else // adding handling of nav mesh obstacle here... 
					if (objectToAffect.GetComponent<NavMeshObstacle>() != null){
						NavMeshObstacle NMO = objectToAffect.GetComponent<NavMeshObstacle>();
						NMO.enabled = !negate;
					}					
					
					OnComplete();
					Cleanup ();
				}
				else
				{
					error = "no objectInteraction to enable";
					OnComplete();
					Cleanup ();
				}
			}
		}
		if (type == actionType.playAnimation){
			if (objectToAffect != null){
				taskChar = objectToAffect.GetComponent<TaskCharacter>();
				if (taskChar != null){
					taskChar.Animate(stringParam);	
					// figure out if we should wait...  we could wait for the characterAnimState
					if (waitForCompletion){
						waitingForAnim = true;
						animEndTime = -1; // failsafe for hung waiting for anim to end...
						AnimationState ast = taskChar.GetComponent<AnimationManager>().body.animation[stringParam];
						if (ast != null && ast.clip != null)
							animEndTime = Time.time + ast.clip.length + 0.05f;; 
					}
					else
						OnComplete (); 
				}
				else
				{
					// handle ?speed= ?time= ?wieght=
					string[] p = stringParam.Split ('?');
					string animationName = p[0];
					
					float animSpeed = 1; // for overrideing defaults
					float animWeight = 1;
					float animTime = 0;
					
					int start=1;
					// process speed=  weight= time= possibly mixing transform...
					while (start < p.Length){
						if (p.Length > start && p[start].ToLower().Contains("speed=")){
							string[] q = p[start].Split('=');
							float.TryParse(q[1],out animSpeed);
						}
						if (p.Length > start && p[start].ToLower().Contains("weight=")){
							string[] q = p[start].Split('=');
							float.TryParse(q[1],out animWeight);
						}
						if (p.Length > start && p[start].ToLower().Contains("time=")){
							string[] q = p[start].Split('=');
							float.TryParse(q[1],out animTime);
						}
						start++;
					}

					if (objectToAffect.animation != null){
						if (animTime == 0)
							objectToAffect.animation.Rewind(animationName);
						if (animTime > 0) objectToAffect.animation[animationName].time = animTime;
						objectToAffect.animation[animationName].speed = animSpeed;
						objectToAffect.animation[animationName].weight = animWeight;
						objectToAffect.animation.clip = objectToAffect.animation[animationName].clip;
						objectToAffect.animation.Play();

						if (waitForCompletion)
							waitingForAnim = true;
						else
							OnComplete ();
					}
					
				}
			}
			else
			{
				error = "no object to play animation on";
				OnComplete();
				Cleanup ();
			}
		}
		if (type == actionType.playAudio){
			// object to affect should have an audio source
			AudioSource src = null;
			if (objectToAffect != null)
				src = objectToAffect.GetComponent<AudioSource>();
			if (src == null)
				src = objectToAffect.AddComponent<AudioSource>() as AudioSource;
		
			if (src != null){
				float timeToWait = fadeLength;
				if (audioClip == null){

					// find the audio clip, looking thru the sound map for this character?

					if (stringParam != ""){
						VoiceMap vm = VoiceMgr.GetInstance().Find(objectToAffect.name, stringParam);
						if (vm != null){
							vm.Clip = SoundMgr.GetInstance().GetClip(vm.Audio);
							if (vm.Clip != null) timeToWait += vm.Clip.length;
							VoiceMgr.GetInstance().Play (objectToAffect.name, stringParam);	
							if (stringParam2 != "" && taskChar != null && vm.Clip != null)
								taskChar.LookAt(stringParam2, Time.time + vm.Clip.length);
						}
						else{
							audioClip = SoundMgr.GetInstance().Get(stringParam);
						}
					}
				}
				if (audioClip != null){ // will still be null if we sent this to the voice manager
					src.clip = audioClip;
					src.Play();
					timeToWait += audioClip.length;
					if (stringParam2 != "" && taskChar != null)
						taskChar.LookAt(stringParam2, Time.time + audioClip.length);
				}
				
				
				if (waitForCompletion)
					StartCoroutine(CompleteAfterDelay (timeToWait));
				else {
					OnComplete();
					Cleanup ();
				}
			}
			else
			{
				error = "no audiosource for playAudio";
				OnComplete();
			}
		}
		if (type == actionType.putMessage){
			// hack to avoid 'I'm too busy messages... 
			if (taskChar != null)
				taskChar.executingScript = null;
			StartCoroutine(SendMessageAfterDelay(fadeLength));
		}
		if (type == actionType.move){ // 
			// lets handle the camera move first:
			if (moveTo == null){ // translate the name, if presesnt
				if (moveToName != "" && GameObject.Find(moveToName)!= null ){
					moveTo = GameObject.Find(moveToName).transform;
				}
			}
			
			CameraLERP cameraLERP = objectToAffect.GetComponent<CameraLERP>();
			if (cameraLERP != null){
				// strangely in Unity, we're not allowed to create new Transforms, so we have to make a dummy
				if (moveTo != null){
					if (offset == Vector3.zero){
						// assume this is a return to the spline
						// reset the camera rail controller so we go to the starting position...
				//		CameraRailCoordinator crc = FindObjectOfType<CameraRailCoordinator>();
				//		if (crc != null)
				//			crc.Reset();

						//
						cameraLERP.MoveTo(moveTo, fadeLength,true,false, 0);
						trackCameraLookat = true;
						if (!waitForCompletion) runIndependentUpdates = true;
						StartCoroutine( EndTrackCameraLookat(fadeLength));
						if (!waitForCompletion) OnComplete ();
						return;
					}
					else
					{
						dummyGO = new GameObject("dummyGO");
						dummyGO.transform.position = moveTo.position 
								+ offset.x*dummyGO.transform.forward
								+ offset.y*dummyGO.transform.up
								+ offset.z*dummyGO.transform.right;
						dummyGO.transform.LookAt(moveTo);
						cameraLERP.MoveTo(dummyGO.transform, fadeLength,true,true, 0);	
					}
				}
				else
				{
					if (fadeLength <= 0)
						cameraLERP.Return(); // snap back
					else
					{
						GameObject dummyGO = new GameObject("dummyGO");
						dummyGO.transform.position = cameraLERP.oldWorldPos; 
						dummyGO.transform.rotation = cameraLERP.oldWorldRot;	
						cameraLERP.MoveTo(dummyGO.transform, fadeLength,true,false, 0);
					}
				}
				//Destroy (dummyGO);  destroy this later on Completed
				if (waitForCompletion && fadeLength > 0)
					StartCoroutine(CompleteAfterDelay (fadeLength));
				else {
					OnComplete();
					//Cleanup ();
				}
			}
			else
			{
				// if the moveTo NAME is a valid Node Name, we can use the TaskCharacter to move there
				taskChar = objectToAffect.GetComponent<TaskCharacter>();
				if (taskChar != null){
//					bool bResult = 
					taskChar.IsInPosition (moveTo.name); // this should start off the nav
				// lets see if we have something with a NavMeshAgentWrapper...
				//	navWrapper = objectToAffect.GetComponent<NavMeshAgentWrapper>();
				//	if (navWrapper != null){ // lets ignore the offset for now...
				//			navWrapper.MoveToGameObject(moveTo.gameObject,2.0f);
					if (waitForCompletion){
						navStartTime = Time.realtimeSinceStartup; // we may want to time out
						waitingForNav = true; 
						return;
					}
					else{
						runIndependentUpdates = true;
						pingTaskCharacter = true;
						OnComplete();
					}
				//	}
				}
				else
				{
					error = "move only implemented for camera, no cameraLERP found";
					OnComplete();	
				}
			}
		}
		
		if (type == actionType.fade){ // 
			if (objectToAffect == null){
				Debug.LogWarning("Null object "+objectName+" for fade by "+name);
				OnComplete();
				Cleanup();
				return;				
			}
			// if the thing is a the GUIManager then fade it
			GUIManager gm = objectToAffect.GetComponent<GUIManager>();
			// test it					
			if (gm != null) {
					gm.SetFadeCurtain(desiredColor.a, fadeLength);
				OnComplete();
				Cleanup ();
				return;
			}
			// we need to have an object with a renderer.
			if (objectToAffect != null && objectToAffect.renderer != null){
				
			// if string param has something, look for a resource by that name that is a mesh or material to swap in
				
			Material newMaterial = null;
//			Mesh newMesh = null;
			if (stringParam != ""){
				newMaterial = Resources.Load(stringParam) as Material;
//				newMesh = Resources.Load(stringParam) as Mesh;
			}
			
			// fade can be an instant change, or take some time.
			// desired color should override desired alpha
			if (stringParam=="current"){
				// should check new material and use it's color if provided
				desiredColor = objectToAffect.renderer.material.color;
				desiredColor.a = desiredAlpha;
			}
			desiredColor.a = desiredAlpha;
			
			if (fadeLength > 0){
				// get components
				// if the thing has a color changer, lets make use of that.
				ColorChanger cc = objectToAffect.GetComponent<ColorChanger>();

				if (cc != null){
					cc.ChangeColor(desiredColor, fadeLength);
				}
				else
				{
//					fadeBeginTime = Time.time;
//					fadeBeginColor = objectToAffect.renderer.material.color;
					if (desiredAlpha > 0){
						objectToAffect.renderer.enabled = true;
						// temporarily just jam the final result until the fade interpolate is in place	
						objectToAffect.renderer.material.color = desiredColor;	
					}
					// if the final alpha is zero, then set the alpha, and turn off the renderer
					else
					{
						objectToAffect.renderer.enabled = false;
						objectToAffect.renderer.material.color = desiredColor;							
					}	
				}
			}
			else { // instant fade
				if (newMaterial != null)
					renderer.material = newMaterial;
				// if final alpha is > 0, turn the renderer on and set the alpha
				if (desiredAlpha > 0){
					objectToAffect.renderer.enabled = true;
					objectToAffect.renderer.material.color = desiredColor;	
				}
				// if the final alpha is zero, then set the alpha, and turn off the renderer
				else
				{
					objectToAffect.renderer.enabled = false;
					objectToAffect.renderer.material.color = desiredColor;							
				}
			}
			}
			else
			{ // missing object or renderer
				error = "Fade has no object with renderer specified";
			}
			OnComplete();
			Cleanup ();
		}
		
		if (type == actionType.ifThenElse){ // 
			
			BaseObject bob = null;
			if (objectToAffect != null && objectToAffect.GetComponent<BaseObject>() != null)
				bob = objectToAffect.GetComponent<BaseObject>();
			if (bob != null){
				// build a binaryExpressionNode out of the string for our testEntity and evaluate.
				
				// perform any arg substitutions
				string newString = executedBy.ResolveArgs(stringParam);
				BinaryExpressionNode condition = BinaryExpressionNode.BuildTree(newString);
				if (condition.Evaluate(bob))
					executedBy.nextLineLabel = ""; // just go on to the next statement
				else
					executedBy.nextLineLabel = "else"; // this will find either the next 'else' block or the next 'endIfThenElse' block
			}
			else
			{
				error = "no baseObject for ifThenElse to test";
			}
//			executedBy.nestingDepth += 1;
			OnComplete();
			Cleanup ();
		}
		
		if (type == actionType.executeScript){ // our execution will be stacked and we will pend until this script completes
			if (scriptToExecute == null && stringParam2 != null && stringParam2 != ""){
				// First, look for a script on the character running this interaction
				if (taskChar != null){
					ScriptedObject tcso = taskChar.GetComponent<ScriptedObject>();
					if (tcso!=null){
						foreach (InteractionScript tcis in tcso.scripts){
							if (tcis.name == stringParam2){
								scriptToExecute = tcis;
								break;
							}
						}
					}
				}
				
				if (scriptToExecute == null){
					// try finding the named game object and look for an interaction script there
					GameObject isGO = GameObject.Find(stringParam2);
					if (isGO != null)
						scriptToExecute = isGO.GetComponent<InteractionScript>();
				}
			}
			
			if (scriptToExecute == null){
				
				Debug.LogError("scriptedAction could not find script to execute at "+name+executedBy.name);
			
				OnComplete ();
				Cleanup ();
			}
			
			if (waitForCompletion){ // run this as a subroutine, continuing when it's done
				//build a string with our script's args and add to stringParam args...
				if (taskChar != null) taskChar.executingScript = null;  // need to clear this for the next scrip
				executedBy.ExecuteScript(scriptToExecute, executedBy.ResolveArgs(stringParam)+" trigger="+name, objectToAffect, ease);
				// yield until we get an update. which will complete us.  we HAVE to wait, no multi threading support yet.
				waitingForUpdate = true;
			}
			else
			{ // don't wait for completion, so we will JUMP and not return to this line
				if (taskChar != null) taskChar.executingScript = null;  // need to clear this for the next scrip
				executedBy.QueueScript(scriptToExecute, executedBy.ResolveArgs(stringParam)+" trigger="+name, objectToAffect, ease);
				// yield until we get an update. which will complete us.  we HAVE to wait, no multi threading support yet.
				executedBy.nextLineLabel = "abort"; // terminate this script
				error="abort";
				OnComplete();
				Cleanup ();
			}
		}
		
		if (type == actionType.queueScript){
			// no script and loop set means flush all scripts from the queue
			if (scriptToExecute == null && loop && (stringParam2 == null || stringParam2 == "")){
				// remove all scripts that are not currently on the stack from the queue
				executedBy.caller.FlushScriptQueue();
				
				OnComplete();
				Cleanup ();
				return;				
			}
			
			if (scriptToExecute == null && stringParam2 != null && stringParam2 != ""){
				// try finding the named game object and look for an interaction script there
				GameObject isGO = GameObject.Find(stringParam2);
				if (isGO != null)
					scriptToExecute = isGO.GetComponent<InteractionScript>();
			}			
			if (scriptToExecute != null)
				executedBy.QueueScript(scriptToExecute, executedBy.ResolveArgs(stringParam)+" trigger="+name, objectToAffect, ease);
			else
				Debug.LogError("FAILED TO FIND script named \""+stringParam2+"\"");
			OnComplete();
			Cleanup ();
			return;	
		}
			
		if (type == actionType.wait){ // we ignore wait for completion on this one...
			// set the HoldPosition flag if requested
			if (loop){
				NavMeshAgentWrapper w = objectToAffect.GetComponent<NavMeshAgentWrapper>();	
				if (w != null)
					w.HoldPosition(true); // if there is no condition, holdPosition will stick till the next character task
			}
			// specifying wait 0 and delay = #delay lets you pass a delay into the script.
			if (fadeLength==0 && stringParam.ToLower().Contains("delay=")){
				string delayString = SubstituteArgsAndAttributes(stringParam);
				if (!float.TryParse(delayString.Replace("delay=",""),out fadeLength)){
					Debug.LogWarning("bad delay time in scripted action "+ name + stringParam);
					OnComplete();
					Cleanup ();
				}
				else {
					StartCoroutine(CompleteAfterDelay (fadeLength));
					return;
				}
			}
			// let's see about waiting for a TAG:NAME 
			if (stringParam.Contains(":") && !stringParam.Contains ("=")){ // this could be better at excluding other things that contain ":"
				// assume this thing is a tag, and post a listener
				//GUIManager.GetInstance().AddInteractCallback(null,myInteractCallback);
				Brain.GetInstance().AddCallback(myInteractCallback);
				// fadelength should be a timeout here if > 0
				if (fadeLength > 0)
					StartCoroutine(CompleteAfterDelay (fadeLength));
			}
			else
			{
				if (stringParam == "" || stringParam == null){
					StartCoroutine(CompleteAfterDelay (fadeLength));
				}
				else
				{
					waitingForCondition = true;
					conditionNode = BinaryExpressionNode.BuildTree(stringParam);
					if (fadeLength > 0)
						StartCoroutine(CompleteAfterDelay (fadeLength)); // this starts a timeout
				}
			}
		}
		
		if (type == actionType.characterTask){ // 
			taskChar = objectToAffect.GetComponent<TaskCharacter>();
			nmaWrapper = objectToAffect.GetComponent<NavMeshAgentWrapper>();
			if (taskChar != null){ 
				taskChar.Init(); // clears flags
				taskChar.executingScript = this; // restore this flag
				// overload 'ease' for random pathnode/animation 
				if (moveToName.ToLower()=="random"){
					ease=true;
					moveToName = SceneNode.GetRandomUnlockedNode().name; // this could return a null, BOOM!
				}
				else
				{
					ease=false;
				}
				
				
				StartCoroutine(CharacterTaskAfterDelay(fadeLength));
				if (!waitForCompletion){
					runIndependentUpdates = true;
					OnComplete ();
				}
			}
		}
		if (type == actionType.attach){ // 
			TaskCharacter tc = objectToAffect.GetComponent<TaskCharacter>();
			if (negate){ // this is a detach, which leaves the object loose at the top level of the hierarchy.
				if (tc != null){
					tc.Detach(SubstituteArgsAndAttributes(attachmentOverride));
				}
			}
			else
			{	// this is an attach, and it's usually better to attach to a new parent than to just detach.
				bool attachingToTcBone = true;
				if (tc != null){
					// if there's an attachment override, and you can find the object and a bone,
					// then place the object at the bone plus offset location
					string substituted = attachmentOverride;
					if (attachmentOverride != "" && attachmentOverride.Contains(" ")){
						string[]p = attachmentOverride.Split (' ');
						GameObject targetObject = GameObject.Find (SubstituteArgsAndAttributes(p[0]).Replace ("\"",""));
						Transform parentBone = tc.GetComponent<AnimationManager>().GetBone(p[1].Replace ("\"",""));
						attachingToTcBone = (parentBone != null);
						if (targetObject == null){
							Debug.LogError(executedBy.name+": "+name+" Script Attachment found no target object in "+attachmentOverride);
							OnComplete();
							Cleanup ();
							return;
						}
						if (!attachingToTcBone){
							// name is not a bone, see if there's a game object by this name
							GameObject parentObject = GameObject.Find (SubstituteArgsAndAttributes(p[1]).Replace ("\"",""));
							if (parentObject != null)
								parentBone = parentObject.transform;
							tc.Detach(targetObject.name); // remove this from the attached Objects list...
						}
						substituted = targetObject.name+" "+p[1].Replace ("\"","");
						if (offset != new Vector3(-1,-1,-1)){
							if (targetObject != null && parentBone != null){
								// if there's a delay, then we can do the lerp in a co-routine 
								if (fadeLength > 0){
									StartCoroutine (AttachAfterDelay(targetObject,parentBone,tc,substituted,attachingToTcBone));
									return;  // don't cleanup or complete until after delayed lerp
								}
								else
								{
									targetObject.transform.position = parentBone.TransformPoint(offset);//position+offset;
									targetObject.transform.rotation = parentBone.rotation*Quaternion.Euler(orientation);
									if (!attachingToTcBone) targetObject.transform.parent = parentBone;
								}
							}
						}
						else
						{  // performing attach using current position
							if (!attachingToTcBone) targetObject.transform.parent = parentBone;
						}
					}
					if (attachingToTcBone)
						tc.Attach(substituted);	
				}
			}
			OnComplete();
			Cleanup ();
		}
		if (type == actionType.spawn){
			GameObject newObject = Instantiate(Resources.Load(stringParam), 
								objectToAffect.transform.position, objectToAffect.transform.rotation)  as GameObject;
			newObject.name = SubstituteArgsAndAttributes(stringParam2);
			if (stringParam3 != ""){
				GameObject newParent = GameObject.Find (stringParam3);
				if (newParent != null){
					newObject.transform.parent = newParent.transform;
					newObject.transform.localPosition = offset;
				}
			}
			executedBy.args["spawnedname"]=newObject.name;
			OnComplete();
			Cleanup ();
		}
		if (type == actionType.destroy){
			Destroy(objectToAffect);
			OnComplete();
			Cleanup ();
		}
		if (type == actionType.unityMessage){ // 
			if (objectToAffect != null)
				objectToAffect.SendMessage(stringParam,stringParam2);
			OnComplete();
			Cleanup ();
		}
		if (type == actionType.lockPosition){
			if (negate){
				NavMeshAgentWrapper w = objectToAffect.GetComponent<NavMeshAgentWrapper>();	
				if (w != null)
					w.LockPosition(false); 
				ScriptedObject so = objectToAffect.GetComponent<ScriptedObject>();
				if (so != null)
					so.executePriorityLock = -1;
			}
			else
			{
				NavMeshAgentWrapper w = objectToAffect.GetComponent<NavMeshAgentWrapper>();	
				if (w != null)
					w.LockPosition(true);
				if (stringParam != ""){
					int lockPriority;
					if (int.TryParse(stringParam,out lockPriority)){
						ScriptedObject so = objectToAffect.GetComponent<ScriptedObject>();
						if (so != null)
							so.executePriorityLock = lockPriority;
					}
				}
				// test unlocking any nodes we have locked, so they are not blocking.
				// this is specifically for BVM, but might be good overall.
				SceneNode.UnlockLocation(objectToAffect.transform.position,0.5f); // radius is completely arbitrary 0.44f is probably a good value.
			}
			OnComplete();
			Cleanup ();
			
		}
		if (type == actionType.goToLine){ // 
			executedBy.nextLineLabel = stringParam; // untested, and very scary
			OnComplete();
			Cleanup ();
		}
		if (type == actionType.setIKTarget){ //
			TaskCharacter tc = objectToAffect.GetComponent<TaskCharacter>();
			if (tc != null){
				IKArmController ctlr = tc.IKArmRight;
				if (stringParam.ToLower().Contains ("left"))
					ctlr = tc.IKArmLeft;
				
				if ((moveToName == null) || moveToName == ""){
					// we are clearing the target
					ctlr.target = null;
				}
				else
				{	
					GameObject targetGo = GameObject.Find(moveToName);
					if (targetGo != null){
						ctlr.target = targetGo.transform;
						HandPoser poser = targetGo.GetComponent<HandPoser>();
						if (poser != null)
							poser.Setup(ctlr.hand);
					}
				}
				ctlr.blendTime = fadeLength;

				ctlr.offset = offset;
				ctlr.orientation = Quaternion.Euler(orientation);
				if (stringParam2 == null || stringParam2 == "") stringParam2 = "0";
				float releaseTime = 0;
				float.TryParse(stringParam2, out releaseTime);
				ctlr.releaseTime = releaseTime;
			}
			
			OnComplete(); // we'll worry about wait for completion later  TODO
			Cleanup ();
		}
	}
	
	string ProcessIKCommands(string commandString){
		// parse and execute any IK commands found in the string, return any leading string up to the first IK arg.
		// Commands of the form: space delimited.
		// IKHelper=<taskCharacter.>IKHelper:<anim> 
		// IKLeft=<target>:blend:hold:offset:orientation (children of IKhelper if supplied)
		// IKRight=<target>:blend:hold:offset:orientation
		string retVal = "";
		int iCmd = 0;
		GameObject helper = null;
		string[] subs = commandString.Split (' ');
		if (subs.Length == 0) return null;
		
		if (!subs[0].Contains("IK")){
			retVal = subs[0];
			iCmd = 1;
		}
		
		while (iCmd < subs.Length){
			if (subs[iCmd].Contains("=")){
				string[] cmd = subs[iCmd].Split('=');
				string[] args = cmd[1].Split(':');
				if (cmd[0] == "IKHelper"){
					// find the helper object, and if present, play an animation on it
					// args[0] is either <taskcharacter.bone> or a game object name of the animated IKHelper
					if (args[0].Contains(".")){  // <TaskCharacter>.bone:snapBone:animation
						string[]p = args[0].Split ('.');
						GameObject tcGO = ObjectManager.GetInstance().GetGameObject(p[0]);
//??						string[] aa = p[1].Split(':');
						helper = FindInChildren( tcGO, p[1]).gameObject; // find the IKHelper game object
						// reparent the helper to the snap bone
						if (args.Length > 1){
							Transform snapBone = tcGO.GetComponent<AnimationManager>().GetBone(args[1]);
							if (snapBone != null){
								helper.transform.position = snapBone.transform.position;
								helper.transform.rotation = snapBone.transform.rotation;
								helper.transform.parent = snapBone;
							}
							if (args.Length > 2)
								helper.GetComponent<Animation>().Play (args[2]);
						}
					}
					else
					{   // here, snap bone would just be a game object, too ?
						helper = GameObject.Find (args[0]);// find the IKHelper game object
						// reparent the helper to the snap bone
						if (args.Length > 1){
							Transform snapBone = GameObject.Find (args[1]).transform;
							if (snapBone != null){
								helper.transform.position = snapBone.transform.position;
								helper.transform.rotation = snapBone.transform.rotation;
								helper.transform.parent = snapBone;
							}
							if (args.Length > 2)
								helper.GetComponent<Animation>().Play (args[2]);
						}
					}
					
				}
				else
				{   // either IKLeft or IKRight <target>:blend:hold:offset:orientation
					TaskCharacter tc = objectToAffect.GetComponent<TaskCharacter>();
					IKArmController ctlr = tc.IKArmRight;
					GameObject cmdIKTarget = null;
					if (helper != null){
						// try and find the target as a child of helper
						cmdIKTarget = FindInChildren(helper,args[0]).gameObject;
					}
					else
						cmdIKTarget = GameObject.Find (args[0]);

					if (cmd[0] == "IKLeft"){
						ctlr = tc.IKArmLeft;
					}
					else
					{ // assume IKRight
					}
					ctlr.target = cmdIKTarget.transform;
					HandPoser poser = cmdIKTarget.GetComponent<HandPoser>();
					if (poser != null)
						poser.Setup(ctlr.hand);
					
					float blendTime = 0;
					float.TryParse(args[1],out blendTime);
					ctlr.blendTime = blendTime;
					
					float releaseTime = 0;
					float.TryParse(args[2],out releaseTime);
					ctlr.releaseTime = releaseTime;
				//	ctlr.offset = offset;
				//	ctlr.orientation = Quaternion.Euler(orientation);
				}
			}
			
			iCmd++;	
		}
		return retVal;
	}
	
	IEnumerator CharacterTaskAfterDelay(float delay){
		yield return new WaitForSeconds(delay);
		if (ignoreTimeout) yield break;
		if (stringParam2 != "")
			taskChar.LookAt(SubstituteArgsAndAttributes(stringParam2), Time.time + 3.0f); // need to add the look duration
		taskChar.AttachmentOverride(SubstituteArgsAndAttributes(attachmentOverride)); // may override attach/detach from the transition animations
		NavMeshAgentWrapper wrapper = objectToAffect.GetComponent<NavMeshAgentWrapper>();
		if (wrapper != null){
			wrapper.heading = heading;
			wrapper.speed = speed;
		}
		
		characterTaskPending = true;
	}
	
	IEnumerator EndTrackCameraLookat(float delay){
		yield return new WaitForSeconds(delay);
		CameraRailCoordinator crc = FindObjectOfType(typeof(CameraRailCoordinator)) as CameraRailCoordinator; 
		crc.LookAt();
		trackCameraLookat = false;
		runIndependentUpdates = false;
		if (waitForCompletion) OnComplete ();
		Cleanup();
	}

	public bool myInteractCallback(GameMsg msg){
		string tag = "";
		InteractStatusMsg ism = msg as InteractStatusMsg;
		if (ism != null) {
			tag = ism.InteractName;
		} else {
			InteractMsg im = msg as InteractMsg;
			if (im != null)
				tag = im.map.item;
		}
		if (tag.Contains ( stringParam)){ // allows partial match like ORDER:BLOOD
			OnComplete();
			Cleanup ();
			ignoreTimeout = true;
			// unregister our callback.
			//GUIManager.GetInstance().RemoveInteractCallbacks(null);
			Brain.GetInstance().RemoveCallback(this.myInteractCallback); // moved to OnComplete ?
			return true;
		}
		return false;
	}
	
	// we could make this the main entry point to avoid overrun, and allow multi threading...
	public IEnumerator ExecuteWhenIdle(InteractionScript caller){ // this is kind of a failsafe in case scripts are written where no-wait overrun occurs.
		// initially, yield to allow the previous line to complete
		yield return new WaitForSeconds(0.01f);
		// if this is just sending a message, go ahead and do it		
		if (type == actionType.putMessage){// maybe some other types as well...
			Execute (caller);
			return true;
		}
		while (taskChar.executingScript != null){
			if (ignoreTimeout) yield break;
			if (executedBy.debug) Debug.Log (name+" waiting for idle "+taskChar.name+", still running "+taskChar.executingScript.name+" at "+Time.time);
			yield return new WaitForSeconds(0.5f);
		}
		taskChar.executingScript = this; // grab the character immediately
		Execute (caller);
	}
	
	IEnumerator SendMessageAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		if (ignoreTimeout) yield break;
		if (gameMsgForm != null){
			if ((gameMsgForm.gameObjectName == null || gameMsgForm.gameObjectName == "") &&
				 objectToAffect != null)
				gameMsgForm.gameObjectName = objectToAffect.name;
			
			//executedBy.nestingDepth += 1; // see if this is going to be an if-then-else dialog

			// add ability to wait for a dialog to be closed before proceeding...this is implemented
			// to avoid having a lockup on multiple dialog waits.  
			if ( gameMsgForm.waitForDialogClosed == true )
			{
				StartCoroutine(WaitForDialogClosed(this,gameMsgForm));
				yield break;
			}
			else
				gameMsgForm.PutMessage(this);
		}
		else
		{
			// default to just sending an interact message
			InteractMsg imsg = new InteractMsg( objectToAffect, stringParam);
			imsg.map.task = stringParam;
			(objectToAffect.GetComponent<BaseObject>() as BaseObject).PutMessage(imsg);
		}
		if (waitForCompletion)
		{
			if ( gameMsgForm.msgType == GameMsgForm.eMsgType.dialogMsg )
				waitingForDialog = true;
			else
				StartCoroutine(CompleteAfterDelay(gameMsgForm.time));
		} else
			OnComplete ();

		Cleanup ();
	}

	bool hasDialog( string dialogString )
	{
		// get all screens that need to be waited for
		string[] screens = dialogString.Split (',');
		
		bool found = false;
		foreach( string item in screens )
		{
			// if any of the screens aren't null then set true
			if ( GUIManager.GetInstance().FindScreen (item) != null )
			{
				UnityEngine.Debug.LogError("ScriptedAction.hadDialog(" + dialogString + ") dialog <" + item + "> is visible!");
				return true;
			}
		}
		return false;
	}
	
	IEnumerator WaitForDialogClosed( ScriptedAction action, GameMsgForm form )
	{
		UnityEngine.Debug.LogWarning("ScriptedAction.WaitForDialogClosed() : <" + form.xmlName + "> is waiting for dialog <" + form.waitForDialogName + ">");

		// waitForDialogName is a CSV string with dialog names
		while( hasDialog(form.waitForDialogName) )
			yield return new WaitForSeconds(.1f);

		UnityEngine.Debug.LogWarning("ScriptedAction.WaitForDialogClosed() : <" + form.xmlName + "> is waiting for dialog <" + form.waitForDialogName + "> done!");

		form.PutMessage (action);
		if (waitForCompletion)
		{
//			if ( form.msgType == GameMsgForm.eMsgType.dialogMsg ) // It seems like this will always be true in this co routine ?
				waitingForDialog = true;
//			else
//				StartCoroutine(CompleteAfterDelay(form.time));
		} else
			OnComplete ();
		Cleanup ();
	}
	
	IEnumerator AttachAfterDelay(GameObject targetObject, Transform parentBone, TaskCharacter tc, string substituted, bool attachingToTcBone){
		float t=0;
		float update = 0.1f;
		Vector3 initialPosition = targetObject.transform.position;
		Vector3 finalPosition = parentBone.TransformPoint(offset);
		Quaternion	initialRotation = targetObject.transform.rotation;
		Quaternion finalRotation = parentBone.rotation * Quaternion.Euler(orientation);
		
		while (t<fadeLength){
			targetObject.transform.position = Vector3.Lerp( initialPosition, finalPosition, t/fadeLength);
			targetObject.transform.rotation = Quaternion.Lerp( initialRotation, finalRotation, t/fadeLength);
			yield return new WaitForSeconds(update);
			t+=update;
		}
		targetObject.transform.position = finalPosition;
		targetObject.transform.rotation = finalRotation;
			
		if (!attachingToTcBone) targetObject.transform.parent = parentBone;
		else tc.Attach(substituted);
		
		OnComplete ();
		Cleanup ();
	}
	
	
	IEnumerator CompleteAfterDelay(float delay){
		if (executedBy.debug){
			Debug.Log (name+" of "+executedBy.name+" starting Delay at "+Time.time);	
		}
		yield return new WaitForSeconds(delay);
		if (!ignoreTimeout){ // if this was a wait for, and the condition occurred, we're already done
			OnComplete ();
			Cleanup();
		}
	}
	
	
	
	public bool CanCompleteImmediately(){
		// test if this line can complete and return without need a script to sequence it or wait for anything.
		
		// actually, it's quite concievable, and could be very useful, to allow a whole set of immediate scripts lines to be run
		// without invoking a script object to control it.
		
		if (!sequenceEnd) return false;  // think about changing this, we'd have to do it from the script level?
		
		if (type == actionType.enableInteraction ||
			type == actionType.putMessage ||
			type == actionType.attach ||
			type == actionType.spawn ||
			type == actionType.destroy ||
			type == actionType.unityMessage ||
			type == actionType.lockPosition)
			return true;
		
		if (!waitForCompletion &&
			(type == actionType.playAudio ||
			 type == actionType.playAnimation))
			return true;

		return false;
	}
	
	public void Cancel(){
		Cleanup ();
		ignoreTimeout = true; // don't let any "afterDelay" timeouts activate
		// cause this line to call OnComplete
		executedBy.nextLineLabel = "abort";
		error = "abort";
		hasExecuted = true;
		executedBy.OnLineComplete(this);
		runIndependentUpdates = false;
	}
	
	void Cleanup(){
		if (breakpoint && executedBy.debug){
			Debug.Log ("Hit Cleanup Breakpoint for "+name+" of "+executedBy.name);	
		}
		// clean up any mess this may have left...
		if (taskChar != null ){ // we could test for InPosition or something to be sure we need to Init
			taskChar.executingScript = null;  // clears InPosition flag like at the end of a task. and the executingScript flag		
			if (type == actionType.characterTask && ease==true){
				moveToName="random";
			}
		}
		if (dummyGO != null)
			Destroy (dummyGO);
		this.enabled = false;  // stop getting updates now that we're done.
	}
	
	public void OnComplete(){
		// tell our scripted object that this action is done and to go on
		hasExecuted = true;
		// we might only want to set these if error == ""
		if (postAttributes != "") SetAttributes(objectToAffect,postAttributes);
		
		if (type == actionType.characterTask){
			if (taskChar != null){
				// need to clear out a few things, but not a full Init();
				// without this, GO HOME in the task master might think we are already InPosition(home)
				taskChar.inPosition = false;
			}
			if (stringParam4 != ""){
				// send :COMPLETE msg to brain
	            InteractStatusMsg msg = new InteractStatusMsg( stringParam4 );
	            Brain.GetInstance().PutMessage(msg);
			}
		}
		
		// see if we are at the end of an "if" block, so we need to jump over the else
		if (((executedBy.currentLine+1) < executedBy.scriptLines.Length) &&
			executedBy.scriptLines[executedBy.currentLine+1].block == blockType.beginElse){
			executedBy.nextLineLabel = "endIf"; // will cause the script to move forward to the next endIfThenElse
		}
//		if (block == blockType.endIfThenElse)
//			executedBy.nestingDepth -= 1; // this could be done up in the script, but we increment in this class, so...
		
		if (sequenceEnd){
			executedBy.nextLineLabel = "sequenceEnd";	
			error="sequenceEnd";
		}
		// be sure our callback doesn't occur now that we are done.
		Brain.GetInstance().RemoveCallback(this.myInteractCallback);
		
		executedBy.OnLineComplete(this);
	}
	
	string SubstituteArgsAndAttributes(string param){
		// perform any #arg script argument substitutions
		param = executedBy.ResolveArgs(param);
		// now for $attribute substitutions
		if (param.Contains ("$")){
			// do a string substitution with the attribute value?  written for naming spawned objects uniquely
			string[] p = param.Split('$');
			param = p[0];
			if (p.Length>1){
				GameObject attributeSource = objectToAffect;
				if (p[1].Contains(".")){
					string[] q = p[1].Split('.');
					attributeSource = GameObject.Find(q[0]);
					p[1]=q[1];
				}
				param += attributeSource.GetComponent<BaseObject>().GetAttribute(p[1]);//.Substring(1);
			}
		}
		return param;
	}
	
	public void SetAttributes(GameObject obj, string attributeExpression){
		// adds, removes or sets multiple attributes from a space delimited string
		// +prefix to add -prefix to remove or key=value is required in each opeation
		
		// perform any key=#arg substitutions
		attributeExpression = executedBy.ResolveRHSArgs(attributeExpression);
		
		string[] tokens = attributeExpression.Split (' ');
		BaseObject myBob = null;
		if (obj != null && obj.GetComponent<BaseObject>() != null)
			myBob = obj.GetComponent<BaseObject>();
		foreach (string st in tokens){
			BaseObject bob = myBob;
			string s = st; // s can be modified, st can't because it's 'foreach'
			
			// check the RHS for an object.attribute and substitute if found
			if (s.Contains("=")){ // if the lhs is not where the object reference is, preserve it
				string[] sides = s.Split('=');
				if (sides[1].Contains(".") && !sides[1].Contains("\"")){ //Dont substitute if in quotes
					sides[1]=ResolveObjectAttribute(sides[1]);//substitute the value
					s=sides[0]+"="+sides[1];
				}
				else
				{ // the RHS doesn't have an object reference. could it be an attribute of the executing entity ?
					if (bob != null && bob.HasAttribute(sides[1])){
						sides[1]=bob.GetAttribute(sides[1]);//substitute the value
						s=sides[0]+"="+sides[1];
					}
				}
			}
			// now if there's an object reference, it's in the LHS telling who's attribute to set
			if (s.Contains(".")){ // we should look for an object by this name to set the attribute on
				s = s.Replace ("\"",""); // strip these.
				string objName = s.Substring(0,s.IndexOf ("."));
				
				GameObject targetObj = GameObject.Find(objName);
				if (targetObj != null)
					bob = targetObj.GetComponent<BaseObject>();
				if (bob == null){
					bob = myBob; //failsafe if specified object not found, should warn in this case...
					Debug.LogError("ScriptedAction "+name+" couldnt find object for attributes named "+objName);
					// leave the reference in place, maybe it means something else ?
				}
				else  // found object, so delete its reference from the expression
					s = s.Substring(s.IndexOf (".")+1);
			}
			
			if (s.Length > 0){	
				if (s[0] == '+'){
					if (bob != null)
						bob.SetAttribute(s.Substring(1),"");
				}
				else if (s[0] == '-'){
					if (bob != null)
						bob.RemAttribute(s.Substring(1));
				}
				// this next one might have been broken by resolveargs above,
				// need to figure out which operation is more useful...
				else if (s[0] == '#'){ // this one sets a script argument for later use like #xray=chest
					executedBy.SetArgs(s.Substring(1));
				}
				else if (s.Contains("+=")){
					if (bob != null){
						string[]p = s.Split ('=');
						p[0] = p[0].Substring(0,p[0].Length-1);
						float val,inc;
						string sval = bob.GetAttribute(p[0]);
						if (float.TryParse(sval,out val) && float.TryParse(p[1],out inc)){
							bob.SetAttribute(p[0],(val+inc).ToString());
						}
					}
				}
				else if (s.Contains("-=")){
					if (bob != null){
						string[]p = s.Split ('=');
						p[0] = p[0].Substring(0,p[0].Length-1);
						float val,inc;
						string sval = bob.GetAttribute(p[0]);
						if (sval!= null && float.TryParse(sval,out val) && float.TryParse(p[1],out inc)){
							bob.SetAttribute(p[0],(val-inc).ToString());
						}
						else
						{
							Debug.LogError("Error setting attribute "+st+" on "+bob.name);
						}
					}
				}
				else if (s.Contains("=")){
					if (bob != null){
						string[]p = s.Split ('=');
						bob.SetAttribute(p[0],p[1]);
					}
				}
				else
					bob.SetAttribute(s,"true");
			}
		}
	}
	
	string ResolveObjectAttribute(string expression){
		// lookup object.attribute and return it's value if found
		if (!expression.Contains(".")) return expression;
		// return immediately if the expression is a valid float, like 30.0
		float fv;	
		if (float.TryParse(expression,out fv)) return expression;
	
		string[] p=expression.Split('.');
		
		GameObject targetObj = GameObject.Find(p[0]);
		BaseObject bb = null;
		if (targetObj != null)
			bb = targetObj.GetComponent<BaseObject>();
		if (bb == null) return expression;
		string sval = bb.GetAttribute(p[1]);
		
		if (sval == "") return expression;
		int iv;
		bool bv;

		bool isString=true;
		if (int.TryParse(sval,out iv) || float.TryParse(sval,out fv) || bool.TryParse(sval,out bv)) isString=false;
		
		
		if (isString) sval = "\""+sval+"\"";//embed string in quotes
		
		return sval; 
	}
	
	private Transform FindInChildren(GameObject go, string childName){
		foreach (Transform t in go.GetComponentsInChildren<Transform>()){
			if (t.name == childName) return t;
		}
		return null;
	}
		
	
	public void DialogCallback( string status )
	{
#if DEBUG_DIALOG_CALLBACK
		UnityEngine.Debug.Log("ScriptedAction.DialogCallback("  + status + ") waitingForDialog=" + waitingForDialog);		
#endif		
		if ( waitingForDialog == false )
			return;
		
		// process (button, interact, script action)
		bool handled = ProcessMessage(status);

		// if handled then this line is finished
		if ( handled == true )
		{
			waitingForDialog = false;
			OnComplete();
			Cleanup();
		}
	}

    public static bool GetToken(string arg, string key, out string value)
    {
        string[] args = arg.Split(' ');
        for (int i = 0; i < args.Length; i++)
        {
            string[] keyvalue = args[i].Split('=');
            if (keyvalue.Length == 2)
            {
                if (keyvalue[0].ToLower() == key.ToLower ()) // can this be case insensitive without breaking anything ?
                {
					if (keyvalue[0] != key)
						Debug.LogWarning("ScriptedAction.GetToken perfomed case insensitive match on "+key);
                    value = keyvalue[1];
                    return true;
                }
            }
        }
        value = "";
        return false;
    }
	
	// handles messages coming back from the GUI.  these messages are setup in the DialogMsg,
	// and passed when the button button specified.  The messages can be either an "onbutton=" event or
	// a default "button" event
    public bool ProcessMessage(string msg)
    {
		bool handled = false;

		// make lower case version
		string msgLower = msg.ToLower();
		
		if (msgLower.Contains("onbutton=") || 
		    msgLower.Contains("interact=") || 
		    msgLower.Contains("script=") || 
		    msgLower.Contains("audio=") || 
		    msgLower.Contains("action="))
		{
			// check name of pressed button
			string buttonName;
			if ( GetToken(msgLower, "onbutton", out buttonName) == true )
			{
				if ( msgLower.Contains("pressed"))
				{
					string pressedName;
					if ( GetToken(msgLower,"pressed", out pressedName) == true )
					{
						// if button pressed is not equal to this action, return
						if ( buttonName != pressedName )
							return false;
					}
				}
			}
			
	        if (msgLower.Contains("interact="))
        	{
	            // get rid of tag
            	string map;
            	if (GetToken(msg, "interact", out map) == true) // this used to process msgLower, but produced unmatchable tags
            	{
                   	// create interaction map
                  	InteractionMap imap;
					// lookup interaction map
                    imap = InteractionMgr.GetInstance().Get(map);
                    if (imap == null)
                    {
	                       // not found, generic...make one
                       	imap = new InteractionMap(map, null, null, null, null, null, null, true);
                    }
					// get object to send to....if no object then assume that we are sending msg to script owner
					BaseObject obj = null;
					// first look for object token
					string _object;
                	if (GetToken(msgLower, "object", out _object) == true)
                	{
						// we have an object, go find it
	                    // get object
                    	obj = ObjectManager.GetInstance().GetBaseObject(_object);
                	}
					else
					{
						// sending msg to script owner
						if ( executedBy != null && executedBy.caller != null )
							obj = executedBy.caller.ObjectInteraction;
					}
					// send to object if we have one!
                    if (obj != null)
                    {
						if ( executedBy.debug == true )
		            		UnityEngine.Debug.Log("GUIButton.ProcessMessage() : obj=" + obj.name + " : map=" + map);
                       	// create InteractMsg
                       	InteractMsg imsg = new InteractMsg(obj.gameObject, imap);
                       	imsg.map.confirm = false;
                       	obj.PutMessage(imsg);
                    }
					else
					{
						if ( executedBy.debug == true )
			            	UnityEngine.Debug.Log("GUIButton.ProcessMessage() : can't find obj=" + obj.Name + " : map=" + map);
					}
            	}
			}

			// this is the audio handling block.  Allows audio to play on button
			if (msgLower.Contains("audio"))
			{
				string audio;
               	if (GetToken(msgLower, "audio", out audio) == true)
               	{	
					// play some audio
					Brain.GetInstance().PlayAudio(audio);
				}
			}	
				
			// this is the script handling block.  the specified script will be executed on either the
			// calling object or on the object=obj specified.  system queues the script on the ScriptedObject
			// to be completed after the object is finished with the current script.
        	if (msgLower.Contains("script="))
        	{
				string _script;
				// NOTE!! don't use lower case here because scripts need to be literal
               	if (GetToken(msg, "script", out _script) == true)
               	{	
					BaseObject obj=null;
					
					// get object to send script too
					string _object;
               		if (GetToken(msgLower, "object", out _object) == true)
               		{
						// we have an object, go find it
                   		obj = ObjectManager.GetInstance().GetBaseObject(_object);
               		}
					else
					{
						// no target, sending msg to script owner
						if ( executedBy != null && executedBy.caller != null )
							obj = executedBy.caller.ObjectInteraction;
					}	

					// split to see if we have multiple scripts
					string[] scripts = _script.Split(',');

					// process all of them
					foreach ( string item in scripts )
					{
						BaseObject thisObj = obj;
						string thisItem = item; // so we can assign
						// added feature to allow sending the object name along with the script name, using  "@"
						if (item.Contains("@")){
							string[] parts = item.Split('@');
							thisItem = parts[1];
							if (ObjectManager.GetInstance().GetBaseObject(parts[0])!= null)
								thisObj = ObjectManager.GetInstance().GetBaseObject(parts[0]);
						}
						// send it
						if ( thisObj != null )
						{
							// we have a valid object and script....execute it!
							if ( executedBy.debug == true )
								UnityEngine.Debug.Log("ScriptedAction.ProcessMessage(" + msgLower + ") script=" + thisItem + " : object=" + thisObj.Name);							
							// try finding the named game object and look for an interaction script there
							GameObject isGO = GameObject.Find(thisItem);
							if (isGO != null)
								scriptToExecute = isGO.GetComponent<InteractionScript>();
							
							if (scriptToExecute != null){					
								//build a string with our script's args and add to stringParam args...
								string paramString = "trigger=" + "DIALOG" ;
								ScriptedObject sObj = thisObj.GetComponent<ScriptedObject>();
								if (sObj != null)
									sObj.QueueScript(scriptToExecute, paramString, thisObj.gameObject);
								else
									executedBy.caller.QueueScript(scriptToExecute, paramString, thisObj.gameObject);
								// yield until we get an update. which will complete us.  we HAVE to wait, no multi threading support yet.
								waitingForUpdate = true;
							}
							else
							{
								if ( executedBy.debug == true )
									Debug.LogError("scriptedAction could not find script to execute at "+name+executedBy.name);
								waitingForUpdate = true;
								handled = true; // should probably do this in the other branches as well...s
							}
						}
					}
				}
        	}
						
			// all buttons execpt for close must provide either action=abort, or action=ok to
			// make the dialog continue execution.  We do this because we have to have the buttons
			// be able to continue scripts
			if (msgLower.Contains("action"))
			{
	            string action;
           		if (GetToken(msgLower, "action", out action) == true)
           		{
					if ( executedBy.debug == true )
						UnityEngine.Debug.Log("ScriptedAction.ProcessMessage() : action=" + action);
					// set specific lines based on action
					if ( action == "abort" )
					{
						executedBy.nextLineLabel = "abort"; // this is setting the abort case
						error = "abort";					// this is error case
						handled = true;
					}
					if ( action == "else" )
					{
						if (dialogIfThen){ // either treat like if-then, or abort
							executedBy.nextLineLabel = "else";
						}
						else{
							executedBy.nextLineLabel = "abort"; 
							error = "abort";
						}
						handled = true;
					}
					if ( action == "ok" || action == "close" )
					{
						executedBy.nextLineLabel = ""; 		// this means just continue execution
						handled = true;
					}
				}
			}
			// this is not a closer
			return handled;
       	}		

		// Default button handling...
		if ( msgLower.Contains("button") )
		{
            string button;
       		if (GetToken(msgLower, "button", out button) == true)
        	{
				if ( executedBy.debug == true )
					UnityEngine.Debug.Log("ScriptedAction.ProcessMessage() : button=" + button);
				switch( button.ToLower() )
				{
				case "close":
				case "cancel":
				case "buttonnext": // traumaQuickFast sends this
					if (dialogIfThen){ // either treat like if-then, or abort
						executedBy.nextLineLabel = "else";
					}
					else{
						executedBy.nextLineLabel = "abort"; 
						error = "abort";
					}
					handled = true;
					break;
				case "ok":
					executedBy.nextLineLabel = ""; 
					handled = true;
					break;
				}
			}
		}
		
		return handled;		
    }

	public void SyncNamesObjects(){ // this fragment never got used... TODO we should call this from the inspector
		// any fields that have objects and names, keep them in sync, objects have priority
		if (objectToAffect != null)
			objectName = objectToAffect.name;
		else
		{
			if (objectName != null && objectName != ""){
				objectToAffect = GameObject.Find(objectName);	
			}
		}
	}
#if UNITY_EDITOR	
	public void ShowInspectorGUI(string GUIlabel){  // should probably return the dirty bit from here...
		// lets extend this, to show only the fields required by the selected action type.  
		// that will make it a lot friendlier in the editor
		if (serializedObject == null) serializedObject = new SerializedObject(this);
		
		EditorGUI.BeginChangeCheck();
		type = (actionType)EditorGUILayout.EnumPopup("Action type",(System.Enum)type);
		
		if (type == actionType.enableInteraction){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			stringParam = EditorGUILayout.TextField("InteractionTAG",stringParam);// used for a lot of different things
			negate = EditorGUILayout.Toggle("disable",negate); // use to turn enable to disable, stop audio, etc.
			loop = EditorGUILayout.Toggle("append",loop);
			texture2D = (Texture2D)EditorGUILayout.ObjectField("iconTexture",texture2D,typeof(Texture2D),true);
			ease = EditorGUILayout.Toggle("remove icon",ease); // hackfully abuse this boolean
		}
		if (type == actionType.playAudio){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToPlay",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("MappedSoundName",stringParam);// used for a lot of different things
			audioClip = (AudioClip)EditorGUILayout.ObjectField("audioClip",audioClip,typeof(AudioClip),true);
			fadeLength = EditorGUILayout.FloatField("delay After",fadeLength);
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("LookAt",stringParam2);
//			fadeLength = EditorGUILayout.FloatField("fadeLength",fadeLength); // also used for wait, move
//			ease = EditorGUILayout.Toggle("ease",ease); // ease or linear movement/fade
			negate = EditorGUILayout.Toggle("stop",negate); // use to turn enable to disable, stop audio, etc.
			loop = EditorGUILayout.Toggle("loop",loop);
		}
		if (type == actionType.playAnimation){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAnimate",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("animation",stringParam);// used for a lot of different things
			if (eventScript == null) eventScript = "";
			eventScript = EditorGUILayout.TextField("eventScript",eventScript);
//			fadeLength = EditorGUILayout.FloatField("fadeLength",fadeLength); // also used for wait, move
//			desiredAlpha = EditorGUILayout.FloatField("desiredAlpha",desiredAlpha);
			loop = EditorGUILayout.Toggle("loop",loop);
		}
		if (type == actionType.putMessage){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
					
			fadeLength = EditorGUILayout.FloatField("delay",fadeLength);
			
			// see about adding local gameMessage to use in this script
			if (gameMsgForm == null){
				gameMsgForm = gameObject.AddComponent<GameMsgForm>();//gameObject.AddComponent<GameMsgForm>();
				// could just do this when we know we'll need one...
				gameMsgForm.map = gameObject.AddComponent<InteractionMapForm>(); //gameObject.AddComponent<InteractionMapForm>();	

				EditorUtility.SetDirty(this);
			}
			else
			{
				if (gameMsgForm.ShowInspectorGUI())
					EditorUtility.SetDirty(this);
			}
			
			if (gameMsgForm.msgType==GameMsgForm.eMsgType.dialogMsg)
				dialogIfThen = EditorGUILayout.Toggle("Treat As If-Then-Else",dialogIfThen);
			
//			stringParam = EditorGUILayout.TextField("message tag",stringParam);// used for a lot of different things


		}
		if (type == actionType.move){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToMove",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			fadeLength = EditorGUILayout.FloatField("moveTime",fadeLength); // also used for wait, move
			moveTo = (Transform)EditorGUILayout.ObjectField("moveTo",moveTo,typeof(Transform),true);
			if (moveToName == null) moveToName = "";
			moveToName = EditorGUILayout.TextField("moveToName",moveToName);
			offset = EditorGUILayout.Vector3Field("offset",offset); // in moveTo reference frame
			ease = EditorGUILayout.Toggle("ease",ease); // ease or linear movement/fade
		}
		if (type == actionType.fade){			
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
//			stringParam = EditorGUILayout.TextField("stringParam",stringParam);// used for a lot of different things
			fadeLength = EditorGUILayout.FloatField("fadeLength",fadeLength); // also used for wait, move
			desiredAlpha = EditorGUILayout.FloatField("desiredAlpha",desiredAlpha);
			desiredColor = EditorGUILayout.ColorField("desiredColor",desiredColor);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("New Matl or Mesh",stringParam);
			ease = EditorGUILayout.Toggle("ease",ease); // ease or linear movement/fade
		}
		if (type == actionType.ifThenElse){			
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToTest",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = ValidateConditionalString("attribute conditional",stringParam);// used for a lot of different things
		}
		if (type == actionType.executeScript){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToExecute",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			scriptToExecute = (InteractionScript)EditorGUILayout.ObjectField("scriptToExecute",scriptToExecute,typeof(InteractionScript),true);
			if (scriptToExecute != null) stringParam2 = scriptToExecute.name;
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("script name",stringParam2);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("args",stringParam);// used for a lot of different things
			ease = EditorGUILayout.Toggle("pass args",ease); // reused boolean
		}
		if (type == actionType.queueScript){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToExecute",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			scriptToExecute = (InteractionScript)EditorGUILayout.ObjectField("scriptToExecute",scriptToExecute,typeof(InteractionScript),true);
			if (scriptToExecute != null) stringParam2 = scriptToExecute.name;
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("script name",stringParam2);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("args",stringParam);// used for a lot of different things
			ease = EditorGUILayout.Toggle("pass args",ease); // reused boolean
			loop = EditorGUILayout.Toggle("check to flush",loop); // reused boolean
			if (loop) stringParam2 = ""; //flush takes empty script
		}		
		
		if (type == actionType.wait){
			if (stringParam == null) stringParam = "";
			stringParam = ValidateConditionalString("wait Condition",stringParam);
			if (stringParam != "" && stringParam != null)
				fadeLength = EditorGUILayout.FloatField("Timeout Value",fadeLength);
			else
				fadeLength = EditorGUILayout.FloatField("wait Time",fadeLength); // also used for wait, move
			if (preAttributes!="" || postAttributes != "" || stringParam != ""){
				objectToAffect = (GameObject)EditorGUILayout.ObjectField("object for Attributes",objectToAffect,typeof(GameObject),true);
				if (objectName == null) objectName = "";
				objectName = EditorGUILayout.TextField("object Name for attributes",objectName);
			}
			loop = EditorGUILayout.Toggle("Hold Position",loop); // reused boolean
		}
		if (type == actionType.characterTask){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("characterToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("characterName",objectName);
			if (moveToName == null) moveToName = "";
			moveToName = EditorGUILayout.TextField("Position",moveToName);// used for a lot of different things
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("Posture",stringParam);
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("LookAt",stringParam2);
			if (stringParam3 == null) stringParam3 = "";
			stringParam3 = EditorGUILayout.TextField("Animation",stringParam3);
			fadeLength = EditorGUILayout.FloatField("delay",fadeLength); 
			if (voiceTag == null) voiceTag = "";
			voiceTag = EditorGUILayout.TextField("voiceTag",voiceTag);
			if (stringParam4 == null) stringParam4 = "";
			stringParam4 = EditorGUILayout.TextField(":COMPLETE msg?",stringParam4);
			heading = EditorGUILayout.FloatField("Heading",heading);
			speed = EditorGUILayout.FloatField ("Speed",speed);
			if (attachmentOverride == null) attachmentOverride = "";
			attachmentOverride = EditorGUILayout.TextField("attachmentOverride",attachmentOverride);
			if (eventScript == null) eventScript = "";
			eventScript = EditorGUILayout.TextField("eventScript",eventScript);
// I had no idea how to write this, but thanks to Unity forums, here is a nice Array custom inspector!
	serializedObject.Update();
    EditorGUIUtility.LookLikeInspector();

    SerializedProperty myIterator = serializedObject.FindProperty("syncToTasks");
    while (true){
        Rect myRect = GUILayoutUtility.GetRect(0f, 16f);
        bool showChildren = EditorGUI.PropertyField(myRect, myIterator);
		if (!myIterator.NextVisible(showChildren)) break;
	}
    serializedObject.ApplyModifiedProperties()	;
				
	EditorGUIUtility.LookLikeControls();
// end of Thanks to the Forums for the code!				

		}
		if (type == actionType.attach){
			negate = EditorGUILayout.Toggle("check for DETACH",negate);
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (attachmentOverride == null) attachmentOverride = "";
			if (negate)
				attachmentOverride = EditorGUILayout.TextField("reparentOverride",attachmentOverride);
			else
				attachmentOverride = EditorGUILayout.TextField("target parent Override",attachmentOverride);
			offset = EditorGUILayout.Vector3Field("offset",offset);
			orientation = EditorGUILayout.Vector3Field("orientation",orientation);
			fadeLength = EditorGUILayout.FloatField("lerpTime",fadeLength);

//			stringParam = EditorGUILayout.TextField("parent name",stringParam);// used for a lot of different things
		}
		if (type == actionType.spawn){
			
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("SpawnLocationObject",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("LocationObjectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("template",stringParam);// used for a lot of different things
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("newName",stringParam2);
			if (stringParam3 == null) stringParam3 = "";
			stringParam3 = EditorGUILayout.TextField("newParent?",stringParam3);
//			loop = EditorGUILayout.Toggle("attach To Location?",loop);
			offset = EditorGUILayout.Vector3Field("attach offset",offset);
		}
		if (type == actionType.destroy){
			
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
		}
		if (type == actionType.unityMessage){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAnimate",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("method",stringParam);// used for a lot of different things
			if (stringParam2 == null) stringParam2 = "";
			stringParam2 = EditorGUILayout.TextField("argumentString",stringParam2);// used for a lot of different things
		}
		if (type == actionType.lockPosition){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToLock",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("objectName",objectName);
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("lockScriptPriority",stringParam);// used for a lot of different things
			negate = EditorGUILayout.Toggle("check for UNLOCK",negate);			
		}
		if (type == actionType.goToLine){
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("abort or line#",stringParam);
			GUILayout.Label("BE CAREFUL WITH THOSE LINE NUMBERS!");
		}
		if (type == actionType.setIKTarget){
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("taskCharacter",objectToAffect,typeof(GameObject),true);
			if (objectName == null) objectName = "";
			objectName = EditorGUILayout.TextField("taskCharacterName",objectName);
			if (moveToName == null) moveToName = "";
			moveToName = EditorGUILayout.TextField("targetName",moveToName);// used for a lot of different things
			if (stringParam == null) stringParam = "";
			stringParam = EditorGUILayout.TextField("Left or Right",stringParam);
			fadeLength = EditorGUILayout.FloatField("blendTime",fadeLength);	
			offset = EditorGUILayout.Vector3Field("offset",offset);
			orientation = EditorGUILayout.Vector3Field("orientation",orientation);
			if (stringParam2 == null) stringParam2 = "0";
			stringParam2 = EditorGUILayout.TextField("Hold Time",stringParam2);
		}
		
		
		GUILayout.Space(10);
		if (comment == null) comment = "";
		comment = EditorGUILayout.TextField("comment",comment);
		if (preAttributes == null) preAttributes = "";
		preAttributes = EditorGUILayout.TextField("preAttributes",preAttributes); // set when line starts
		if (postAttributes == null) postAttributes = "";
		postAttributes = EditorGUILayout.TextField("postAttributes",postAttributes); // set when line completes
		block = (blockType)EditorGUILayout.EnumPopup("IfThenElse bracket",(System.Enum)block);
		SetNamePrefix(block);
		executeOnlyOnce = EditorGUILayout.Toggle("executeOnlyOnce",executeOnlyOnce);
		waitForCompletion = EditorGUILayout.Toggle("waitForCompletion",waitForCompletion);
		sequenceEnd = EditorGUILayout.Toggle("sequenceEnd",sequenceEnd);
		breakpoint = EditorGUILayout.Toggle("debug breakpoint",breakpoint);
		
		bool dirty = EditorGUI.EndChangeCheck();
		if (dirty){
			if (objectToAffect != null && objectName == ""){ // default, or we could try looking up the name again...
				objectName = objectToAffect.name;
			}
			if (moveTo != null && moveToName == ""){ // default, or we could try looking up the name again...
				moveToName = moveTo.name;
			}
			if (scriptToExecute != null && stringParam2 == ""){ // default, or we could try looking up the name again...
				stringParam2 = scriptToExecute.name;
			}
			
			EditorUtility.SetDirty(this);
		}
		
/*			cut and paste reference for new inspector types
 * 
			objectToAffect = (GameObject)EditorGUILayout.ObjectField("objectToAffect",objectToAffect,typeof(GameObject));
			objectName = EditorGUILayout.TextField("objectName",objectName);
			stringParam = EditorGUILayout.TextField("stringParam",stringParam);// used for a lot of different things
			audioClip = (AudioClip)EditorGUILayout.ObjectField("audioClip",audioClip,typeof(AudioClip));
			fadeLength = EditorGUILayout.FloatField("fadeLength",fadeLength); // also used for wait, move
			desiredAlpha = EditorGUILayout.FloatField("desiredAlpha",desiredAlpha);
			desiredColor = EditorGUILayout.ColorField("desiredColor",desiredColor);
			moveTo = (Transform)EditorGUILayout.ObjectField("moveTo",moveTo,typeof(Transform));
			ease = EditorGUILayout.Toggle("ease",ease); // ease or linear movement/fade
			negate = EditorGUILayout.Toggle("negate",negate); // use to turn enable to disable, stop audio, etc.
			loop = EditorGUILayout.Toggle("loop",loop);
			waitForCompletion = EditorGUILayout.Toggle("waitForCompletion",waitForCompletion);
			executeOnlyOnce = EditorGUILayout.Toggle("executeOnlyOnce",executeOnlyOnce);
			ifBranch = EditorGUILayout.TextField("ifBranch",ifBranch);
			elseBranch = EditorGUILayout.TextField("elseBranch",elseBranch);
*/
/*
		if (GUILayout.Button("SERIALIZE")){
//			ScriptedActionInfo info = ToInfo (this);
			ScriptedObject pop = this.transform.parent.parent.GetComponent<ScriptedObject>();
			ScriptedObject.ScriptedObjectInfo info = pop.ToInfo (pop);
			XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObject.ScriptedObjectInfo));
 			FileStream stream = new FileStream("TESTSOSERIAL.xml", FileMode.Create);
 			serializer.Serialize(stream, info);
 			stream.Close();
		}
*/
		
	}
	
	string ValidateConditionalString(string label, string cond){
		
		string work = EditorGUILayout.TextField(label,cond);
		if (work == cond) return cond;
		
		// remove any incorrect operators
		work = work.Replace("==","=");
		work = work.Replace("&&","&");
		work = work.Replace("||","|");
		work = work.Replace("^","!");
		// ensure that any tokens have the required space delimiters
		work = work.Replace("("," ( ");
		work = work.Replace(")"," ) ");
		work = work.Replace("&"," & ");
		work = work.Replace("|"," | ");
		
		// remove any double spaces
		while (work.Contains("  "))
			work = work.Replace("  "," ");
	
		return work;
	}
	void SetNamePrefix(blockType block){
		// as at least some kind of visual aid,
		// Maintain "IF", "ENDIF", "ELSE" or "END" as prefix of lines with block controls set for readability
		if (name[0]!='?' && type != actionType.ifThenElse && block == blockType.none)
			return;
		// otherwise, strip off prefixes and set any appropriate ones

		
		if (type == actionType.ifThenElse && !name.Contains ("?IF")){
			StripPrefix ();
			name = "?IF"+name;
			return;
		}
		if (block == blockType.beginElse  && !name.Contains ("?ELSE")){
			StripPrefix ();
			name = "?ELSE"+name;
			return;
		}
		if (block == blockType.endIfThenElse  && !name.Contains ("?END")){
			StripPrefix ();
			name = "?END"+name;
			return;
		}
		
		//if the block type is other than	
		if (name[0]=='?' && type != actionType.ifThenElse && block == blockType.none)
			StripPrefix ();
	}
	
	void StripPrefix(){
		name = name.Replace("?IF","");
		name = name.Replace("?ENDIF","");
		name = name.Replace("?ELSE","");
		name = name.Replace("?END","");
	}
	
	public string PrettyPrint(){ // make a line or two for a readable script output.
		string output = "";
		// possibly indent here ?
		if (block == blockType.beginElse)
			output += "} ELSE { ";
		if (block == blockType.endIfThenElse)
			output += "} ";
		
		output += type.ToString();
		switch(type){
		case actionType.attach:
			break;
		case actionType.characterTask:
			if (syncToTasks != null && syncToTasks.Length > 0)
				output += " sync["+(syncToTasks.Length+1)+"]";
			break;
		case actionType.destroy:
			break;
		case actionType.enableInteraction:
			break;
		case actionType.executeScript:
			output += " "+stringParam2+" ["+stringParam+"]";
			break;
		case actionType.fade:
			break;
		case actionType.goToLine:
			output += " ["+stringParam+"]";
			break;
		case actionType.ifThenElse:
			output += objectName+"( "+stringParam+" ) { ";
			break;
		case actionType.lockPosition:
			break;
		case actionType.move:
			break;
		case actionType.playAnimation:
			break;
		case actionType.playAudio:
			break;
		case actionType.putMessage:
			break;
		case actionType.spawn:
			break;
		case actionType.unityMessage:
			break;
		case actionType.wait:
			if (fadeLength == 0 && stringParam == "")
				output = output.Replace("wait",""); // noop
			else
				output += "("+stringParam+" "+fadeLength.ToString()+")";
			break;
		}
		if (comment == null || comment == "") output += "//"+name;
		else output += "//"+comment;
		
		return output;
		
		
	}
	
	public void ShowHelp(){
		
		switch(type){
			
		case actionType.characterTask:
			GUILayout.TextArea(
				"CharacterTask is a functional replacement for XML based Tasks, and contains the same fields as the CharacterTask data. " +
				"The CharacterTask action is intended to operate the same way that CharacterTasts did, providing a migration path." +
		  		"Since Scripts are much more flexible than CharacterTasks were," +
		   		"future interactions should be able to make use of other script action types to accomplish behaviors" +
		    	"which were difficult using only fixed character task structures.  " +
		    	"\nThe CompletMessage value will be sent as an interactStatusMessage if supplied. " +
		     	"\nThe SyncToTasks array provides synchronization between character tasks, " +
		     	"and should contain other character tasks which are intended to execute in parallel with this one.  " +
		     	"All parallel tasks except the one with the highest script index should have 'WaitForCompletion' set to false." +
		       	"The highest index CharacterTask should have it set to true." + 
				"\nSee the Attach action description for details about the AttachmentOverride string.");
			break;
		case actionType.destroy:
			GUILayout.TextArea(
			""
			);
			break;
		case actionType.enableInteraction:
			GUILayout.TextArea(
			"EnableInteraction Sets or clears the 'Enabled' property of an " +
			"ObjectInteraction component on the designated gameObject. " +
			"(not to be confused with the 'enabled' property of a Unity component) "+
			"\nCheck 'diable' to clear the property. "+
			"\nThe iconTexture is used as a GUI signal to draw user attention to the object in the 3d scene.  " +
			"Currently, if the objectInteraction is enabled, and the texture is non null, " +
			"it will be displayed as a bouncing icon.  " +
			"\nCheck RemoveIcon to clear the icon independently of enabling or disabling the object "
			);
			break;
		case actionType.executeScript:
			GUILayout.TextArea(
			"ExecuteScript - the named ObjectInteraction, or the script owner, will run the named Script.  " +
			"\nIf 'WaitForCompletion' is true, execution of this script will pause until the named script completes. " +
			"(this assumes the same object is running both scripts, unknown results otherwise).  " +
			"\nIf WaitForCompletion is false, the current script will end at this point.  " +
			"\nAdditional arguments can be passed in the 'args' string, in the form 'arg=value arg=value'  " +
			"\nAdditionally, the current script's arguments can also be passed on to the named script."
			);
			break;
		case actionType.fade:
			GUILayout.TextArea(
			"Not yet implemented.  Imagine fading objects in and out, or the scene to black..."
			);
			break;
		case actionType.goToLine:
			GUILayout.TextArea(
			"Will move execution to a numbered script line (careful!) or if the line is 'abort', will terminate the script. " +
			 "This can be used to create loops."
			);
			break;
		case actionType.ifThenElse:
			GUILayout.TextArea(
			"IfThenElse - allows script to branch based on testing attributes of an ObjectInteraction component. "+
			"attributeConditional is of the form <exp> & | <exp> with parenthesis " +
			"(and single space delimiters until i fix the parser) " +
			"where <exp> is:  <key> or key=value or   key>value  or  key<value" +
			"\n\nTo indicate the END of the IF branch, set the IfThenElse bracket property of the last line you want to run " +
			"to either BeginElse or EndIfThenElse. (any action type except IfThenElse can be the End). "+
			"\nIf you include an else branch, then terminate that by setting the IfThenElse bracket to " +
			"EndIfThenElse on the last line of the else branch.  " +
			"It may be convenient to use a noop (wait with time=0) with the end bracket on it in some cases. Nesting is allowed."
			);
			break;
		case actionType.lockPosition:
			GUILayout.TextArea(
			"This action sets a special flag on the Character's NavMeshAgent wrapper, " +
			"so it won't try and get out of anyone's way, used to keep someone who is animating in place.  " +
			"Remember to unlock the position later when appropriate." +
			"lockScriptPriority, if supplied, keeps scripts below this priority from starting." +
			"This probably should have been done using the UnityMessage action type, a more general purpose construct."
			);
			break;
		case actionType.move:
			GUILayout.TextArea(
			"Move - currently only implemented for a camera with a CamerLERP component," +
			 "but should be coming soon for characters too.  " +
			 "Drag in a transform from the scene, and an offset in the local coordinate system " +
			 "relative to that transform determines the move target.  " +
			 "The ease parameter is currently ignored but will result in smooth camera movement when implemented.   " +
			 "The camera will be rotated to face the transform location, or if the offset is (0,0,0), aligned with that transform. " +
			"LookAt can also be used for the look target.  "+
			"Wait for Completion will respect the specified move time."
			);
			break;
		case actionType.playAnimation:
			GUILayout.TextArea(
			"If used on a TaskCharacter calss object, this will pass the animation name to TaskCharacter.Animate.  " +
			"When a non TaskCharacter object is specified, " +
			"the action will attempt to play the named clip using the object's animation component."
			);
			break;
		case actionType.playAudio:
			GUILayout.TextArea(
			"PlayAudio will play the assigned AudioClip on an audio source of the designated object.  " +
			"If no audio source exists, one will be added.  " +
			"The audio can be set to loop, and stop can be used to stop a looping audio without designating a clip. " +
			"(play with a null clip might be a better way to signify this)"
			);
			break;
		case actionType.putMessage:
			GUILayout.TextArea(
				"Calls PutMessage on the designated object with the message type specified." +
				  "The fields of the selected message type are displayed.  " +
				  "Some of these fields may be ignored by the object"
			);
			break;
		case actionType.spawn:
			GUILayout.TextArea(
			"Spawning an object requires knowing what to spawn, where to spawn it, and what to call it."+
 			"The template is what to spawn, and it is the name of a prefab with the path relative to the Resources folder.  " +
 			"You'll find two items there, blood and saline bags, complete with the scripts they need to run, etc."+
 			"Location Object name is the name of any uniquely named game object in the scene.  " +
			"Since scripts are serialized, they can't retain actual references to objects in the scene, and must find those objects by name when they are loaded.  " +
			"An offset can be specified to position the newly spawned object relative to it's spawn point.  " +
			"The object will become a child of the 'newParent', which could be used to make use of the default attach events animations are used to using."+
 			"\nThe new name:  For Blood and saline bags, each one spawned has to have a unique name so they can be tracked, manipulated, etc.  To achieve that, I had to add a special 'string building' feature, which is accessed with the $ in the name field.  The part of the name after the $ is a variable reference.   " +
			"In the case of bags, the variable is 'fab_bloodFridge.bagindex'."
			);
			break;
		case actionType.unityMessage:
			GUILayout.TextArea(
			"This will perform a Unity <namedObject>.SendMessage(method, argumentString), " +
			"which is equivalent to calling that method with a string argument.  " +
			"Should only be used to call methods which expecte a single string argument.  " +
			"It will be called on all components of the named game Object"
			);
			break;
		case actionType.wait:
			GUILayout.TextArea(
			"Wait - wait for a designated time, or for an attribute condition to be true for a designated object.  " +
			"This actionType is useful as a noop for setting attributes by using a wait time of 0."
			);
			break;
		case actionType.attach:
			GUILayout.TextArea(
			"Attach - reparent an object in the scene, usually to a bone within the character performing this action.  " +
			"Attach normally uses the TaskCharacter.Attach() method, and if no override is supplied, " +
			"the currentPosture.TargetObject is attached to currentPosture.ParentObject. " +
			"(see the animation subsystem for further details)  " +
			"\nAn AttachmentOverride string can supply a specific child and parent bone name for this attachment, " +
			"supply one or both names with a space between. "+
			"\nNOTE that the attachment overide can also be supplied for character tasks. "+
			"\nAn offset and Rotation relative to the new parent can be specified, and " +
			"the offset should be set to (-1,-1,-1) to use the 'in situ' relative position at the time of attachment. "+				
			"\nDetaching works similarly, and the override can name the specific object to be detached, and a new parent for that object."
			);
			break;				
		}


		
		

		if (type == actionType.putMessage){
			GUILayout.TextArea("putMessage uses either a message object filled out below, or a string in stringParam that tags a Message defined elsewhere,as in XML");
		}
		
		if (type == actionType.enableInteraction){
			GUILayout.TextArea("changes an objectinteraction to enabled or disabled if checked. you can set or clear the icon texture, which shows over the object if enabled and set");
		}
		if (type == actionType.playAudio){
			GUILayout.TextArea("plays the selected audio clip on the specified object's audio source, will create an audio source if none exists");
		}

		if (type == actionType.putMessage){
			GUILayout.TextArea("choose a message type, fill in fields, calls PutMessage on specified baseobject");
		}
		if (type == actionType.move){
			GUILayout.TextArea("implemented for the camera, null moveto will execute 'return' for the camera");
		}
		if (type == actionType.ifThenElse){
			GUILayout.TextArea("test the conditional expression of attributes on the BaseObject class, execute the next line or 'then label' if true or the 'else label' if false.  You can use @arg values. like if @xray=chest <not implemented yet>");
		}
		if (type == actionType.wait){
			// just delay for a number of seconds
			GUILayout.TextArea("wait for a number of seconds, or wait -1 to wait for all roles to reach this point");
		}
		GUILayout.FlexibleSpace();
		GUILayout.TextArea("ScriptedAction is used in InteractionScript by ScriptedObject to perform actual work." +
		"Select the type of action you want above to see details for that type.");
		GUILayout.TextArea("preAttributes are single words or key=value pairs set on objectToAffect or script owner when a line begins to execute.  you can use key=#arg to get an argument value from the calling script");
		GUILayout.TextArea("postAttributes get set when the line completes");
				
		GUILayout.FlexibleSpace();
// general help about scriptedActions---------------------------
		GUILayout.TextArea(
			"\nScriptedAction:  This class performs the actual work of the scripts. Each type of action has different fields, and new action types will be added as more capabilities are required.  All scripted actions are of the same class, but their appearance in the editor depends on the actionType and messageType fields, due to a custom Inspector for this class in the Unity Editor folder.  This way only the applicable fields of the action are displayed for the selected action type.  Fields which apply to all or most action types are discussed here.  Each specific action type may be documented separately below as required."+

"\nAttributes, Arguments, Decision Variables "+
"\nScriptedActions can TEST variables in IfThenElse( condition) and in Wait( condition) which affect script progression.  These same variables can be SET in attribute expressions which occur either at the start or the end of execution of a ScriptedAction. "+
"\nThere are three types of variables: "+
"\n # "+
"\nArguments are passed to a script when it is invoked, normally as parameters in the interact message that triggers the script.  They take the form <argname>=<argvalue>. "+
"\nThe value can be a boolean True or False, an integer or floating point constant, a string constant which must include surrounding quotes, as in arg=\"expression\" "+
"\nArgument references must be prefixed with the '#' character, i.e.  ( #units>=0 ) "+
"\n% "+
"\nDecisionVariables are public members of an object class with simple types (int, float, bool) exposed through reflection by their names.  They can set or tested, and references must begin with the '%' character. "+
"\n\nattribute "+ 
"\nAny name not preceeded by #,% or @, and not elclosed in quotes, is assumed to be an attribute name.  Attributes can have boolean, integer, float, or string values, and can be associated with any object (derived from baseObject) simply by assigning them a value.  For instance, if you write a script that places a cervical collar on a patient, you can simply write Patient.collar=True into postAttributes, then any script or script line can test Patient.collar=True to enable the script or take a specific branch.  A scripted action could be waiting for that condition and would begin running once the condition it met. "+

"\n\nObject.attribute "+
"\nBy default, attribute and decision variables refer to the object a particular script line is affecting, usually the owner of the script.  Values of other object will be used if preceded by the GameObject name, i.e. Patient.collar, or BloodFridge.bloodbags, or Patient.%BloodBagsIV. (note that the % designator appears next to the variable name, not the object name)."+

"\n@nodename "+
"\nrefers to a navigation node, to check if the location is occupied, compare to \"locked\" or \"unlocked\" (this feature is not fully implemented yet)."+

"\n\nActionType determines what this action does when performed."+

"\n\nObjectToAffect (or variant) refers to a GameObject to enable, test or use according to the actionType.  Leaving this unassigned will default to the gameObject of the  ObjectInteraction class owning this script, which is often the right thing to do.  For delivering a message to a particular character, etc, drag the character's game object here to assign it."+

"\n\nObjectName is the name of the above object, automatically filled in if this field is empty when the object assignment is made.  This is used to connect prefabs to named gameobjects in the scene when a prefab is dragged in.  ObjectToAffect overrides this name, if they are different.  Best practice is to clear this field before assigning an object to keep the two unambiguous."+

"\n\nVarious action specific fields are displayed depending on the action type"+

"\n\npreAttributes contains a string expression to be applied to ObjectToAffect's attributes BEFORE the line is executed.  The expression can include: +key -key key=value key=#arg in a space delimited string.  If you just want to set attributes with a ScriptedAction, best practice is to use actionType=wait and time=0, as a noop action."+

"\n\npostAttributes are identical, but applied AFTER the line executes.  For example, you might use +OnThePhone in preAttributes and -OnThePhone in postAttributes,  to flag that key while an animation or navigation was running."+

"\n\nIfThenElse bracket  is a setting to identify this line as a control for conditional branching.  Normally set to None, it should be set to 'BeginElse' on the first line to be executed if the condition is false, and 'EndIfThenElse' on the last line in the else branch.  Think of these lines as brackets around script lines to be executed.  For ease of reading scripts, lines with these flags set also are automatically renamed appropriately to start with the flag values, as it makes reading the scripts much simpler.  "+

"\n\nExecuteOnlyOnce causes a line to be skipped once it has run, and is only reset when the level starts again.  This could be useful for assessment that might be triggered from multiple places, and in this case, the same ScriptedAction could be referenced from multiple scripts, but would only send the message that logs the action once."+

"\n\nWaitForCompletion means the next line of the script won't run till this one is complete.  This is not currently implemented for all types, like sending a message that causes something to happen."
		);
		
	// use @key=value to set a script argument for later use
	}
#endif
}

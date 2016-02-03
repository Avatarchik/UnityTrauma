#define MAKE_ISM_FROM_IM

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

public class Dispatcher : ObjectInteraction {
	
	static Dispatcher instance = null;
//	private bool initialized = false;
	private List<string> scriptsToAssign = new List<string>();
	private List<ScriptedObject.ScriptedObjectInfo> scriptedObjectInfos = new List<ScriptedObject.ScriptedObjectInfo>();
	protected List<ObjectInteraction> actors = new List<ObjectInteraction>(); 
	// they have to have at least IsValidInteraction and PutMessage
	private ObjectInteraction scribeNurse = null; // send record commands to her.

    public static Dispatcher GetInstance()
    {
		if (instance == null){
			instance = FindObjectOfType(typeof(Dispatcher)) as Dispatcher;
			if (instance == null){ // no dispatcher in the level, add one
				GameObject dgo = new GameObject("Dispatcher");
				instance = dgo.AddComponent<Dispatcher>();
			}
		}
  	    return instance;
    }
	
    public override void Awake()
    {
		base.Awake();
		
        instance = this;
		if (scriptsToAssign == null)
			scriptsToAssign = new List<string>();
    }	
	
	// Use this for initialization
	public override void Start () {
		// we need to be sure all the objects have registered with ObjectManager
//		StartCoroutine(FindActors());
	}
/*	
	IEnumerator FindActors(){ // deprecated?
		yield return new WaitForSeconds(3); // would be better if this were somehow event based...
		List<BaseObject> list = ObjectManager.GetInstance().GetObjectList();

		foreach (BaseObject obj in list){
			if ((obj as ObjectInteraction) != null && !actors.Contains(obj as ObjectInteraction))
				actors.Add (obj as ObjectInteraction);
		}
		initialized = true;
		
		// assign any queued script xml now that the actors are all registered
		foreach (string xmlPath in scriptsToAssign)
			AssignScripts (xmlPath);
		
		scriptsToAssign.Clear();
	}
*/
	
	public void RegisterObject(BaseObject obj){
		// add this to our actor list, and assign any scripts we already know about
		ObjectInteraction actor = obj as ObjectInteraction;
		
		if (actor != null &&
			!actors.Contains(actor)){
			actors.Add (actor);
			if (actor.name == "ScribeNurse")
				scribeNurse = actor;
			
			AssignScripts (actor);
		}
	}

	public bool IsCommandAvailable( string command )
	{
		InteractionMap map = InteractionMgr.GetInstance().Get(command);
		if (map == null){
			map = new InteractionMap();
			map.item = command;
		}
		foreach(ObjectInteraction testActor in actors){
			ScriptedObject sco = null;
			if (testActor.IsValidInteraction(map)){
				sco = testActor.GetComponent<ScriptedObject>();
				foreach (InteractionMap imap in sco.QualifiedInteractions()){
					if (imap.item == command)
						return true;
				}
			}
		}	
		return false;
	}

	public bool IsCommandQueued( string command )
	{
		// return true if the command is in someone's queue or already running...
		InteractionMap map = InteractionMgr.GetInstance().Get(command);
		if (map == null){
			map = new InteractionMap();
			map.item = command;
		}
		foreach(ObjectInteraction testActor in actors){
			ScriptedObject sco = null;
			if (testActor.IsValidInteraction(map)){
				sco = testActor.GetComponent<ScriptedObject>();

				//In the Que ?
				foreach (object obj in sco.scriptArray){
					foreach (string trigger in((obj as ScriptedObject.QueuedScript).script.triggerStrings)){
						if (trigger == command)
							return true;
					}
				}
				// already runnning ?
				foreach (ScriptedObject.QueuedScript qs in sco.scriptStack){
					foreach (string trigger in qs.script.triggerStrings){
						if (trigger == command)
							return true;
					}
				}
			}
		}	
		return false;
	}
	
	public bool ExecuteCommand(InteractionMap map){

		// menu commands are coming thru here
		InteractionMgr.GetInstance().EvaluateInteractionSet( map.item);

		GameObject preferredGO = ObjectManager.GetInstance().GetGameObject(map.objectName);	
	
	    InteractMsg msg = new InteractMsg( preferredGO, map);
		
		return DispatchMessage (  msg );		
		
	}


	protected ObjectInteraction GetCharacter(string command){
		string characterName = "";
		if (command.Contains (":PN")) characterName = "PrimaryNurse";
		if (command.Contains (":SN")) characterName = "ScribeNurse";
		if (command.Contains (":PR")) characterName = "ProcedureResident";
		if (command.Contains (":AM")) characterName = "AirwayMD";
		if (command.Contains (":RT")) characterName = "RespiratoryTech";
		if (command.Contains (":XT")) characterName = "XrayTech";

		if (characterName == "")
						return null;

		foreach (ObjectInteraction oi in actors) {
			if (oi.Name == characterName)
				return oi;
		}
		return null;
	}

	public virtual void LimitInteractions(string interactionList, bool remove = false ,bool append = false){
		// traumaDispatcher implements this
	}

	public virtual bool ExecuteCommand( string command, string preferredHandler = ""){
		// commands initialed thru the NLU, Menu, or Filter should pass thru this for processing

		InteractionMgr.GetInstance().EvaluateInteractionSet(command); //		
		
		InteractionMap map = InteractionMgr.GetInstance().Get(command);
		if (map == null){
			Debug.LogWarning("InteractionMgr found no map for "+command);
			map = new InteractionMap();
			map.item = command;
			// return false;
		}
		
		GameObject preferredGO = null;
		if (preferredHandler != null){
			preferredGO = ObjectManager.GetInstance().GetGameObject(preferredHandler);	
		}
		
	    InteractMsg msg = new InteractMsg( preferredGO, map);
		bool dispatched = DispatchMessage (  msg );

		if (dispatched && scribeNurse != null && scribeNurse.GetComponent<ScriptedObject>().scriptArray.Count<1){
			// have the scribe nurse write this down.
			InteractionMap recordMap = new InteractionMap();
			recordMap.item = "RECORD:RESULT";
			recordMap.param = new List<string>();
			recordMap.param.Add(command);
			InteractMsg recordMsg = new InteractMsg( scribeNurse.gameObject, recordMap);
			recordMsg.scripted = true;
			scribeNurse.PutMessage(recordMsg);
		}

		return dispatched;
	
//		PutMessage (msg); // use our normal code to find the best character to do this.
	}
	// an Interact message sent to the 'dispatcher' object will be forwarded to the best character 
	// to perform the associated interaction
    public override void PutMessage( GameMsg msg ) 
    {
		// see if any of our actors can process this interaction
        InteractMsg interactMsg = msg as InteractMsg;
        if (interactMsg != null)
        {
			DispatchMessage (  interactMsg );
        }
    }
	
	private bool DispatchMessage ( InteractMsg msg ){

		// this routine includes triggering the Generic Response System voice cues
		
		ObjectInteraction preferredHandler = null;
//		bool phIsValidInteraction = false;
		int phQc = -1; // flags that we have not checked this yet
		float lowestCost=99999;
		List<ObjectInteraction> actorsHavingInteraction = new List<ObjectInteraction>();
		
		if (msg.gameObject != null && msg.gameObject != "" && msg.gameObject != "null" && msg.gameObject.ToLower() != "dispatcher"){ // here. gameObject is just a string.
			preferredHandler = ObjectManager.GetInstance().GetBaseObject(msg.gameObject) as ObjectInteraction;
			if (preferredHandler != null){
				
				// The message asked for SOMEBODY in particular to do this
		
				if (!preferredHandler.IsValidInteraction(msg.map)){
					// the preferred handler doesn't know how to do this interaction.
					
					// if it's a valid interaction, then respond "VOICE:MISMATCH"
					if (InteractionMgr.GetInstance ().Get(msg.map.item)!= null)
						VoiceMgr.GetInstance().Play (msg.gameObject,"VOICE:MISMATCH:*"); //* is a wildcard for multiple responses
					// If it's not recognised by the interaction manager, then respond "VOICE:BAD:COMMAND"
					else
						VoiceMgr.GetInstance().Play (msg.gameObject,"VOICE:BAD:COMMAND:*");
					return false;
				}
				ScriptedObject phSo = preferredHandler.GetComponent<ScriptedObject>();
				if (phSo != null){
					phQc = phSo.scriptArray.Count;
					if (phQc < 2){ // then we're going to let our ph do this.
						if (phQc == 0){
							//VoiceMgr.GetInstance().Play (msg.gameObject,"VOICE:ACKNOWLEDGE");
							VoiceMgr.GetInstance().Play(msg.gameObject,"VOICE:ACKNOWLEDGE:*");
						}
						else{
							//VoiceMgr.GetInstance().Play (msg.gameObject,"VOICE:BUSY:QUEUED");
							//Brain.GetInstance().PlayAudio("AUDIO:ACKNOWLEDGE");
							VoiceMgr.GetInstance().Play(msg.gameObject,"VOICE:BUSY:QUEUED:*");
						}
						preferredHandler.PutMessage(msg);
						return true;
					}
					else // Qc>= 2: The requested person is pretty busy, is there someone else who can do this?
					{
						ObjectInteraction handoffActor = null;
						foreach(ObjectInteraction testActor in actors){
							if (testActor.IsValidInteraction(msg.map)){
								actorsHavingInteraction.Add(testActor);
							}
						}
						foreach (ObjectInteraction testActor in actorsHavingInteraction){
							// add in cost if we're the scribe nurse
							float cost = ( testActor == scribeNurse )?100:0;
							ScriptedObject taSo = testActor.GetComponent<ScriptedObject>();
							if (taSo != null){
								int taQc = taSo.scriptArray.Count;
								if (phQc == -1 || taQc < phQc){ // shorter queue, choose this one
									handoffActor = testActor;
									phQc = taQc;
									lowestCost = testActor.GetCost(msg.map)+cost;
								}
								else
								{
									if (taQc == phQc){ // same q length, base on cost
										if ((testActor.GetCost(msg.map)+cost) < lowestCost){ // same q. lower cost, choose
											handoffActor = testActor;
											lowestCost = testActor.GetCost(msg.map);
										}
									}
								}
							}	
						}
						if (handoffActor != null){
							// say the handoff message
							// can we set the lookat ?
							CharacterBuilder cb = handoffActor.GetComponent<CharacterBuilder>();
							Transform lookAt;
							if (cb != null)
								lookAt = cb.bodyHeadbone;
							else
								lookAt = handoffActor.transform; // could search for that lookAt child...

							if (preferredHandler as Character != null){
	                  		  	(preferredHandler as Character).LookAt(lookAt, Time.time + 3);
								// set talk time
								(preferredHandler as Character).TalkTime = Time.time + 3;
							}
							
							VoiceMgr.GetInstance().Play(msg.gameObject,"VOICE:BUSY:HANDOFF:*");
							// say the acknowledge for the handoff actor
							if (phQc == 0)
								VoiceMgr.GetInstance().Play(handoffActor.Name,"VOICE:ACKNOWLEDGE:*");
							else if (phQc == 1)
								VoiceMgr.GetInstance().Play(handoffActor.Name,"VOICE:BUSY:QUEUED:*");
							else
								VoiceMgr.GetInstance().Play(handoffActor.Name,"VOICE:BUSY:DELAYED:*");
							// send the handoff actor the message
							handoffActor.PutMessage(msg);
							return true;
						}
						
						// no one else to do this, so put it on the queue.
						VoiceMgr.GetInstance().Play(msg.gameObject,"VOICE:BUSY:DELAYED:*");
						preferredHandler.PutMessage(msg);
						return true;
					}
					// here, we have a preferred handler with a q > 2, so we can
					// ask about priorities or hand off
				}				
			} // end the handler specified was valid
		} // end message specified a specific handler
		
		// we've handled all the cases if the handler was specified already.
		
		
		if (preferredHandler == null){ // this should always be true.
			// No valid preferred handler was given, build list of who can do this interaction.
			foreach(ObjectInteraction testActor in actors){
				if (testActor.IsValidInteraction(msg.map))
					actorsHavingInteraction.Add(testActor);
			}	
			// if no one knows how to do this, we just bail.
			if (actorsHavingInteraction.Count == 0){
				Brain.GetInstance().PlayAudio("AUDIO:BAD:COMMAND");
				return false;
			}
			// if someone can do this, pick the best one, weighting Q length the heaviest.
			// and set them up as the preferred handler
			foreach (ObjectInteraction testActor in actorsHavingInteraction)
			{
				// if we're the scribe have a higher cost
				float cost = (testActor == scribeNurse)?100:0;
				//
				ScriptedObject taSo = testActor.GetComponent<ScriptedObject>();
				if (taSo != null){
					int taQc = taSo.scriptArray.Count;
					if (phQc == -1 || taQc+2 < phQc){ // much shorter queue, choose this one
						preferredHandler = testActor;
						phQc = taQc;
						lowestCost = testActor.GetCost(msg.map)+cost;
					}
					else
					{
						if (taQc <= phQc){ // nearly same q length, base on cost
							if ((testActor.GetCost(msg.map)+cost) < lowestCost){ // same q. lower cost, choose
								preferredHandler = testActor;
								phQc = taQc;
								lowestCost = testActor.GetCost(msg.map);
							}
						}
					}
				}	
			}		
		}	
		// we should be guaranteed a preferred handler at this point.
		// we have picked THE best person to perform the task, so we just need to acnowledge appropriately
		if (preferredHandler != null ){

			ScriptedObject phSo = preferredHandler.GetComponent<ScriptedObject>();
			if (phSo != null){
				phQc = phSo.scriptArray.Count;
				if (phQc == 0){ // then we're going to let our ph do this.
						VoiceMgr.GetInstance().Play (preferredHandler.name,"VOICE:ACKNOWLEDGE:*");
				}
				else 
				{	if (phQc == 1) {
						VoiceMgr.GetInstance().Play (preferredHandler.name,"VOICE:BUSY:QUEUED:*");
					}
					else
					{
						VoiceMgr.GetInstance().Play (preferredHandler.name,"VOICE:BUSY:DELAYED:*");
					}
				}
				preferredHandler.PutMessage(msg);
				return true;
			}
		}

#if MAKE_ISM_FROM_IM
		// always dispatch an ISM for each IM (for AssessmentMgr) NOTE!! if we don't have a handler
		InteractStatusMsg ismsg = new InteractStatusMsg(msg);
		Brain.GetInstance().PutMessage(ismsg);
#endif

		// either no preferredHandler, or too busy to do this. Make a list of everyone who can do this.
		return false;
		
		
	}
	
	public void AssignScripts(string xmlPath){
			
		ScriptedObject.ScriptedObjectInfo info=null;
/*		
		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObject.ScriptedObjectInfo));
			FileStream stream = new FileStream(xmlPath, FileMode.Open);
			info = serializer.Deserialize(stream) as ScriptedObject.ScriptedObjectInfo;
			stream.Close();
		}
		else
*/
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<ScriptedObject.ScriptedObjectInfo> serializer = new Serializer<ScriptedObject.ScriptedObjectInfo>();
			string pathname = "XML/"+xmlPath.Replace (".xml","");
			info = serializer.Load(pathname);
			if ( info == null )
			{
				UnityEngine.Debug.LogError ("Dispatcher.AssignScripts(" + xmlPath + ") error serializing!");
				return;
			}
		}
		
		scriptedObjectInfos.Add (info);
		// determine which registered actor(s) this script should be attached to, so that the menu will support triggereing.
		// currently, this is done based on the SO's DropTargetName, and all scripts are placed under those game objects.
		// TODO this really should be based on the owning role of the individual script, and could default to the drop target.
		
// refactor: we want each script to be able to get assigned to a different owner based on the owningRole property, if set.		
		
		foreach (InteractionScript.InteractionScriptInfo scriptInfo in info.scripts){
			string targetString = info.dropTargetName;
			if (scriptInfo.owningRole != null && scriptInfo.owningRole != "") 
				targetString = scriptInfo.owningRole;
			
			// handle multiple drop targets
			string[] targets = targetString.Split (',');
			foreach (string target in targets){
				foreach (ObjectInteraction actor in actors){
					if (actor.Roles.Contains(target)){
						GameObject go = new GameObject(scriptInfo.unityObjectName);
						InteractionScript newScript = go.AddComponent("InteractionScript") as InteractionScript;
						//link script to drop target NOW so roles[0] can be resolved
						go.transform.parent = actor.transform;
						newScript.InitFrom(scriptInfo);
						newScript.Start ();
						AssignScript (newScript, actor);
					}
				}
			}
		}
		// see if there's a startup script to link
		// this is going to break the concept of role per script...	
			// switch these to use auto execute !
/*			
			
		if (info.startupScriptName != null && info.startupScriptName != ""){
			for (int i = 0; i<scripts.Length; i++){
				if (scripts[i].name == info.startupScriptName){
					startupScript = scripts[i];
					break;
				}
			}
		}
*/

	}
	
	// assign any supported, registered scripts to a new actor
	void AssignScripts(ObjectInteraction actor){
		
		foreach (ScriptedObject.ScriptedObjectInfo info in scriptedObjectInfos){
			// handle multiple drop targets
			foreach (InteractionScript.InteractionScriptInfo scriptInfo in info.scripts){
				string targetString = info.dropTargetName;
				if (scriptInfo.owningRole != null && scriptInfo.owningRole != "") 
					targetString = scriptInfo.owningRole;
	
				// handle multiple drop targets
				string[] targets = targetString.Split (',');
				
				foreach (string target in targets){
					if (actor.Roles.Contains(target)){
							GameObject go = new GameObject(scriptInfo.unityObjectName);
							InteractionScript newScript = go.AddComponent("InteractionScript") as InteractionScript;
							//link script to drop target NOW so roles[0] can be resolved
							go.transform.parent = actor.transform;
							newScript.InitFrom(scriptInfo);
							newScript.Start ();
		//					if (newScript.roles == null || newScript.roles.Length==0){
		//						newScript.roles = new string[1];
		//						newScript.roles[0] = info.dropTargetName; // temporary hack until roles are properly edited
		//					}
		//					if (newScript.menuOrder == 0)
		//						newScript.menuOrder = 99999; // add at end, hack until menu orders are set...
							AssignScript (newScript, actor);

					}
				}
			}
		}
	}
	
	// Assign a single script to any registered actor who supports the scripts primary role
	public bool AssignScript(InteractionScript script){
		bool assigned = false;
		
		foreach(ObjectInteraction oi in actors){
				assigned |= AssignScript( script,  oi);
		}
		return assigned;
	}
	
	// Add a new instance of a script to an actor if the primary role is supported.
	public bool AssignScript(InteractionScript script, ObjectInteraction oi){
		bool assigned = false;

		if (oi.Roles.Contains(script.roles[0])){ //assigned by primary role
			// be sure we have a scriptedObject
			ScriptedObject so = oi.GetComponent<ScriptedObject>();
			if (so == null){
				so = oi.gameObject.AddComponent<ScriptedObject>();
				so.scripts = new InteractionScript[0];
				// prettyname!
			}
			// perform replacement if a script with the same name already exists.
			// this supports overriding base scripts with later loads
			foreach (InteractionScript scr in so.scripts){
				if (scr.name == script.name){
					so.RemoveScript(scr);
					DestroyImmediate (scr.gameObject);
					break;	
				}
			}
			
			so.InsertScriptSorted(script); // this sets the parent, we need to duplicate the script at this point.
			assigned = true;
		}
		return assigned;		
	}
	
	// the dispatcher is responsible for choosing the best candidate for each role in a script when it starts.
	public bool FillRoles(InteractionScript script){
		
		script.roleMap.Clear();
		script.actorObjects.Clear();
		
		// choose the best available character for each role
		// we could easily make use of the skill vector for choosing between available actors
		foreach (string role in script.roles){
		// initailize the roleMap dictionary with the role--objectName pair
			
			// get the cost for each actor who can perform the role, and choose the lowest cost
			// (could it be possible for one actor to perform multiple roles in a single script ?)
			script.roleMap[role] = role;			
			// for now, just pick anyone who can do it
			if (actors != null){
				foreach(ObjectInteraction oi in actors){
					if (oi.Roles.Contains(role)){
						script.roleMap[role] = oi.Name;
						script.actorObjects.Add(oi);
						break;
					}
				}
			}
		}
		return true;
	}
	
	// add new animations to any character who supports the role (called when asset bundles are loaded)
	public void AssignAnimations(Animation anim, string role){
		if (actors != null){
			foreach(ObjectInteraction oi in actors){
				if (oi.Roles.Contains(role)){
					// need to dig to find the REAL animation component...

					Animation actorAnim = oi.GetComponent<AnimationManager>().body.GetComponent<Animation>();
					foreach (AnimationState state in anim){
						if (actorAnim[state.name] == null)
							actorAnim.AddClip( state.clip, state.name);						
					}
				}
			}
		}		
	}
}

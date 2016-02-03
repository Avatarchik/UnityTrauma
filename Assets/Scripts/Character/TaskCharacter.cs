//#define DEBUG_TASK
//#define DEBUG_POSTURE
//#define DEBUG_ISDONE
//#define DEBUG_ATTACH

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//I am awesome.

public class TaskCharacter : ObjectInteraction 
{
	public string charName;
	public string charTag = "";
	public bool executingTask = false;
	public ScriptedAction executingScript = null;
	public Task currentTask;
	public string gotoNode;
	public bool inPosition;
    public float delay = 0.0f;
	List<Posture> postures = new List<Posture>();
	List<AnimatedInteraction> aInteractions;
	Posture currentPosture = new Posture();
	public IKArmController IKArmRight;
	public IKArmController IKArmLeft;
	public bool moving = false;
	public float navStartTime = 0; // used as a timeout to detect lockouts.
	float navTimeout = 25;
	public bool changingPosture = false;
	public bool animating = false;
    public bool waiting = false;
	public GameObject body;
	Vector3 oldPosition = new Vector3();
	public bool waitPostureChange = false;
	public bool lerp = false;

    public float distance = 0.0f;
    public float rotation = 0.0f;

    public float MoveScale = -1.0f;

	GameObject targetNode = null;
	GameObject atNode = null;

	public string atNodeName="";
	public string targetNodeName
	{
		get {
			if (inPosition == false )
				return gotoNode;
			else
				return "no target";
		}
	}

	float smooth = 5;
	public bool rotated = false;
	public float endTime = 0f;
	public GameObject homePosition;
	string currentNode = "";
	string currentAnimation = "";
	string attachmentOverride = ""; // used to override posture dependent Object and Parent for attach/detach methods
//	Transform oldParent; // this was unused
//	float oldScale;
//	bool hasCollider;
//	bool hasFootprint;
//	string attachObject; // this is unused
	private MeshToggle[] meshToggles;
	public List<GameObject> attachedObjects = new List<GameObject>(); // seems like a list of AttachableObjects might be good here,
																// if we needed oldScale, has footprint, etc, they could go there
																// but i don't really think we need those.
	public GameObject lastDetachedObject = null;  // temp hack to allow reparenting after animation event detach
	

	public override void Start() 
    {
		base.Start();
		
		charName = gameObject.name;  // this is referenced only by TaskMaster, we could probably simplfy
		if (charTag == "") charTag = gameObject.name.ToUpper();
		//LoadInteractionXML("XML/Interactions/testNurse");
		meshToggles = transform.GetComponentsInChildren<MeshToggle>();
		
		homePosition = GameObject.CreatePrimitive(PrimitiveType.Cube);
		homePosition.transform.position = transform.position;
		homePosition.transform.rotation = transform.rotation;
        homePosition.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        homePosition.collider.enabled = false;
		homePosition.renderer.enabled = false;
		homePosition.name = "HOME";
		atNode = homePosition;
		atNodeName = "HOME";
		gotoNode = "no target";
		
		Register();
		CharacterSelectorDialogue.GetInstance().Register(this);
		
	}

	public override void Awake() 
    {
		base.Awake();
        if (gameObject.GetComponent<AnimationManager>() != null)
        {
            LoadPostureXML("XML/Postures");
            currentPosture = postures[0];
            LoadAnimationXML("XML/Animations");
        }
		if (body != null)
		SetupIK();
	}

    // Object is already moving into position, Arrive callback happens from
    // nav manager when object arrives at node.
    public void Move()
    {
        if (moving)
        {
            float distance = Vector3.Distance(transform.position, oldPosition);
            oldPosition = transform.position;
            //UnityEngine.Debug.LogError("Distance " + distance + " at deltaTime of " + Time.deltaTime);
			if (Time.deltaTime > 0)
            	gameObject.GetComponent<AnimationManager>().CurrentWalkSpeed = (distance / Time.deltaTime);
			else
				gameObject.GetComponent<AnimationManager>().CurrentWalkSpeed = 0;
        }
    }

    // once in position, lerp rotation and position to exact node position
    public void Lerp()
    {
        if (lerp)
        {
            // test > 1.0f
            if (endTime > 1.0f)
                endTime = 1.0f;
			if (targetNode != null )
			{
	            // lerp position/rotation
	            transform.position = Vector3.Lerp(transform.position, targetNode.transform.position, endTime);
	            transform.rotation = Quaternion.Lerp(transform.rotation, targetNode.transform.rotation, endTime);
	            // increment endTIme
	            endTime += Time.deltaTime * 2.0f; //.01f;
	            // test distance/rotation
	            distance = Vector3.Distance(transform.position, targetNode.transform.position);
	            rotation = Quaternion.Angle(transform.rotation, targetNode.transform.rotation);
	            if (Mathf.Abs(distance) < 0.01f && Mathf.Abs(rotation) < 0.01f)
	            {
	                lerp = false;
	                rotated = true;
	                //gameObject.GetComponent<FootprintComponent>().m_scale = 0;	
					if (rigidbody != null)
	                rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ |
	                                        RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
	            }
			}
			else
				UnityEngine.Debug.LogError("TaskCharacter.Lerp() character<" + this.charName + "> targetNode=NULL!");
        }
    }



    public void Setup(CharacterTask characterTask, Task task)
    {
        if (executingTask == false)
        {
            // do lookat
            LookAt(characterTask.lookAt, Time.time + characterTask.lookAtTime);
            // set flaggage
            executingTask = true;
            // start animation
            Animate(characterTask.animatedInteraction);
        }
    }

	// Reset everything in case something went wrong during a script
	// this will be followed by a Task Master command to GoHome.
	public void Reset(){
		// reset anything in the object interaction class

		executingTask = false;
		executingScript = null;
		currentTask = null;
		gotoNode = "no target";
		inPosition = false;
		//currentPosture = //something.  base posture ?
		moving = false;
		navStartTime = 0; // used as a timeout to detect lockouts.

		changingPosture = false;
		animating = false;
		waiting = false;
		waitPostureChange = false;
		lerp = false;
		MoveScale = -1.0f;
		targetNode = null;
		atNode = null;
		
		atNodeName="";
		rotated = false;
		endTime = 0f;
		currentNode = "";
		currentAnimation = "";
		attachmentOverride = "";

		// put any attached objects back to their home positions, when that becomes possible.  they don't have homes implemented yet

		// reset anyhting in the ScriptedObject component if we have one ?

		// put our animation manager back to stand.  it if had a reset, that would be preferable
		AnimationManager am = GetComponent<AnimationManager>();
		if (am != null)
			am.InitAnimations("stand");  // force posture to stand so they can walk home

	}
	
	//Updates for walk speed and lerping for end of walk
    public override void Update()
    {
        base.Update();

        Move();
        Lerp();
    }

    public bool UpdateTask( Task task, CharacterTask characterTask )
    {
        if (animating == true)
        {
            // not finished
            return false;
        }

		// clear current

        return true;
    }
	
	//reset flag for same frame anim calls
	public void LateUpdate() {
		waitPostureChange = false;
	}
	
	public void LoadInteractionXML(string name)
    {
        // load base interactions
        base.LoadXML(name);
    }
	
	public void LookAt(string target, float stopTime=0) 
    {
		if (target == null || target == "" || target.ToLower() == "ignore")
        {
            return;
        }
		
        if (target.ToLower() == "camera")
        {
            LookAt(Camera.main.transform,stopTime);
        }
        else
        {
            GameObject targetObject = GameObject.Find(target);
            if (targetObject != null)
                LookAt(targetObject.transform,stopTime);
        }
	}

    public void LookAt(Transform transform, float stopTime)
    {
        gameObject.GetComponent<AnimationManager>().LookStart(transform, stopTime);
    }

    public void LookStop()
    {
        gameObject.GetComponent<AnimationManager>().LookStop();
    }
	
	//Use Navigation Agent to pathfind
	public bool IsInPosition(string nodeName) 
    {
		if (navStartTime == 0)
						navStartTime = Time.time;
        // check nodeName.  If empty we are in position
		if(nodeName == "")
		{
			atNode = null;
			return true;
		}

		// set where we are going
		gotoNode = nodeName;
		
		// if we are already here, don't queue up a move. just very rotate and go on
		if (atNode != null && atNode.name == nodeName){
			inPosition = true;
			navStartTime = 0;
			rotated=true;
			return true;  // can we assume we are already rotated
		}
        // if we're in position check to make sure
        // that we are rotated into the correct position
		if(inPosition && !lerp && !rotated) 
        {
			if(nodeName == "HOME")
				targetNode = homePosition;
			else				
				targetNode = GameObject.Find(nodeName);
			lerp = true;
			atNode = null;
		}
        // if we're inPosition and rotated correctly then
        // we are GOOD
		if(inPosition && rotated)
		{
			if (targetNode == null){
				if(nodeName == "HOME")
					targetNode = homePosition;
				else				
					targetNode = GameObject.Find(nodeName);
			}
			if (targetNode == null){
				UnityEngine.Debug.LogError("TaskCharacter.InPosition has null target");
			}
			atNode = targetNode;
			atNodeName = targetNode.name;
			return true;
		}
		else
			atNodeName = "";

        // if we're not already moving and not in the right position
        // then call the NAV agent to move us to the right spot
		if(!moving && !inPosition && nodeName != "") 
        {
			if (nodeName == currentNode || (atNode != null && nodeName == atNode.name)){
				waitPostureChange = true;
				moving = false;
				inPosition = true;
				navStartTime = 0;
				return false;
			}
				
            // check for node occupied
			if(nodeName != "HOME" && TaskMaster.GetInstance().CheckNode(this,nodeName) && nodeName != currentNode) 
            {
                // node is occupied, see if we can free it...
                bool freed = TaskMaster.GetInstance().FreeNode(nodeName, name);
				if (!freed && (Time.time-navStartTime) > navTimeout){
					// we've been waiting to free this node for longer than 15 sec, 
					//TODO pick a new node, or force us to be in position...
					UnityEngine.Debug.LogWarning(name+" locked out of node "+nodeName+", bypassing nodelock.");
					SceneNode goal = SceneNode.Get(nodeName);
					// just put us there.
					transform.position = goal.transform.position;
					moving = false;
					inPosition = true;
					navStartTime = 0;
					return false;
				}
                // we're blocking here...
				atNode = null;
				return false;
			}

#if DEBUG_TASK
            UnityEngine.Debug.Log("TaskCharacter.IsInPosition(" + nodeName + ") : name=" + Name + " : moving=" + moving);
#endif

			NavMeshAgentWrapper nac = gameObject.GetComponent<NavMeshAgentWrapper>();
			if(nac != null) 
            {
                // start walk animation if ok with animation manager
				if ( gameObject.GetComponent<AnimationManager>().Walk() == true )
				{
					// node is free and walk is cleared, so start moving
					moving = true;
					
					// don't let the body rotate
					if (rigidbody != null)
						rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
					// if w're not going home then unlock our current node
				    TaskMaster.GetInstance().UnlockNode(this,currentNode);
	                // set our new destination node
					currentNode = nodeName;
	                // if the node is set to home then move to the home position
					if(nodeName == "HOME")
						nac.MoveToGameObject(homePosition, .001f);
					else 
	                {
	                    // we're going to a specific node, look up the node object
	                    GameObject node = GameObject.Find(nodeName);
	                    if (node != null)
	                    {
	                        // lock it
	                        TaskMaster.GetInstance().LockNode(this,nodeName);
	                        // move to it
	                        bool yesno = nac.MoveToGameObject(GameObject.Find(nodeName), .001f);
	#if DEBUG_TASK
	                        UnityEngine.Debug.LogWarning("TaskCharacter.IsInPosition(" + Name + ") nac.MoveToGameObject(" + nodeName + ") : ok=" + yesno);
	#endif
	                    }
	                    else
	                    {
	                        UnityEngine.Debug.LogError("TaskCharacter.IsInPosition(" + Name + ") can't find node : [" + nodeName + "]");
							// just go nowhere and continue as if we've arrived
							atNode = null;
							moving = false;
							rotated = true;
							inPosition = true;
							navStartTime = 0;
							targetNode = null;
							GetComponent<AnimationManager>().StopWalk(0.5f);
							return true;
						}
	                }
				}
				atNode = null;
				return false;
			} 
            else 
            {
#if DEBUG_TASK
                UnityEngine.Debug.Log("TaskCharacter.IsInPosition(" + nodeName + ") NAC=null : name=" + Name + " : moving=" + moving);
#endif
                moving = false;
				inPosition = true;
				navStartTime = 0;
			}
		}
#if DEBUG_TASK
        UnityEngine.Debug.Log("TaskCharacter.IsInPosition(" + Name + ") RETURNING FALSE, moving=" + moving + " : inPosition=" + inPosition + " : rotated=" + rotated + " : nodeName=" + nodeName);
#endif
		return false;
	}
	
	//Called at end of pathfinding
	public void Arrive() 
    {
        if(moving) 
        {
            waitPostureChange = true;
			inPosition = true;
			navStartTime = 0;
			moving = false;
#if DEBUG_TASK
            UnityEngine.Debug.Log("TaskCharacter.Arrive(" + Name + ") : moving=" + moving + " : inPosition=" + inPosition);
#endif
        }
	}
	
	public void ChangePosture(Posture posture)
    {
#if DEBUG_TASK
        Debug("TaskCharacter.ChangePosture(" + Name + ") : posture=" + posture.Name);
#endif
        gameObject.GetComponent<AnimationManager>().NextPosture(posture.Name);
		currentPosture = posture;
	}
	
	//Check posture against available postures for this character
	public bool IsInPosture(string targetPosture)
    {
		if(waitPostureChange || changingPosture) 
			return false;
		if (targetPosture == null || targetPosture == "") return true;// consider no posture specified a pass condition
		if(currentPosture != null)
		if(!moving && !changingPosture) {
			Posture tPosture = new Posture(); // don't set this to new here, tPosture won't be null below // 
			foreach(Posture posture in postures) {
				if(posture.Name == targetPosture) {
#if DEBUG_POSTURE
                    UnityEngine.Debug.Log("TaskCharacter.IsInPosture(" + Name + ") : posture=" +  targetPosture + ")");
#endif
                    tPosture = posture;
					break;
				}
			}
			if(tPosture != null) {
#if DEBUG_POSTURE
                Debug("TaskCharacter.IsInPosture(" + Name + ") Checking posture " + currentPosture.Name + " against " + tPosture.Name);
#endif
                if (currentPosture.Name != tPosture.Name)
                {
					changingPosture = true;
					ChangePosture(tPosture); // if there is no transition, this may call EndPostureChange)();
					return false;
				}
				else if(!changingPosture) {
#if DEBUG_POSTURE
                    Debug("TaskCharacter.IsInPosture(" + Name + ") Is in posture=" + currentPosture.Name );
#endif
                    return true;
				}
			}
			else {
#if DEBUG_POSTURE
                UnityEngine.Debug.LogError("TaskCharacter.IsInPosture(" + Name + ") Cannot find posture: " + targetPosture);
#endif
				return false;
			}
		}
		return false;
	}
	
	// this was needed when SwitchPosture in an animation is called to keep the tc synchronized
	public void ChangePostureInstant(string targetPosture){
		Posture tPosture = new Posture();
		foreach(Posture posture in postures) {
			if(posture.Name == targetPosture) {
				tPosture = posture;
				break;
			}
		}
		if(tPosture != null) {
			currentPosture = tPosture;
			// this next line interferes with posture changing transition timing,
			// stand_to_push4 some animation event SwitchPosture("stand") occurs during the transition, causing move to occur before transition completes
			if (tPosture.Name != "stand") // hacked this in without fully understand the problem, but it seems to work 3/11/13
				changingPosture = false;  //added 2/15/13 ? this was to fix timeout delay for a transition we don't have ?
		}
	}
	
	public void GetState() 
    {
		//Debug.Log("State");
	}
	
	//Called from AnimationMaganger at end of change
	public void EndPostureChange() 
    {
		if(changingPosture) 
        {
			waitPostureChange = true;
			changingPosture = false;
		}
	}
	
	//Called at end of animation
	public virtual void EndAnimatedInteraction() 
    {
		animating = false;
	}
	
	public void Animate(string animation) 
    {
        if (animation == null || animation == "")
        {
            if (waitPostureChange || !changingPosture)
            {
                animating = false;
            }
			return;
		}
		currentAnimation = animation;
		if(waitPostureChange){	// this would not be a good thing to do, we would lose this animation call
			UnityEngine.Debug.LogWarning("Animate Call IGNORED due to waitPostureChange");
			return;
		}

		animating = true;
        gameObject.GetComponent<AnimationManager>().DoInteraction(animation);
    }
	
	public void Animate(AnimatedInteraction newAI) 
    {
        if (newAI == null)
        {
            if (waitPostureChange || !changingPosture)
            {
                animating = false;
            }
			return;
		}
		currentAnimation = newAI.Name; // this is not referenced anywhere
		if(waitPostureChange){	// this would not be a good thing to do, we would lose this animation call
			UnityEngine.Debug.LogWarning("Animate Call IGNORED due to waitPostureChange");
			return;
		}

		animating = true;
        gameObject.GetComponent<AnimationManager>().DoInteraction(newAI);
    }
	
#if LATER
	public void InterruptTask() {
		executingTask = false;
		currentTask = null;
		GameObject.Find(name).animation.Stop();
		animating = false;
	}
#endif
	
public void AttachmentOverride(string val){
		// this string will be used to override default behavior for the next attach/detach call
		// supply "TargetObject ParentObject" when overriding Attach, or just "ParentObject" for detach
		// the value is used once then discarded, so it only affects the next attach or detach event.
		
		attachmentOverride = val;	
	}
	
	public void Attach(string val){
		AttachmentOverride(val);
		Attach ();
	}
	
	public void Attach()
	{
		bool snapToNewParent=false;
		// allow skipping the event
		if (attachmentOverride == "skip"){
			attachmentOverride = "";
			return;
		}
		if (attachmentOverride.Contains ("snap")){
			snapToNewParent=true;
			attachmentOverride = attachmentOverride.Replace(" snap","");
			attachmentOverride = attachmentOverride.Replace("snap ","");
			attachmentOverride = attachmentOverride.Replace("snap","");	
		}

		GameObject obj;
		
		string targetObject = currentPosture.TargetObject;
		string parentObject = currentPosture.ParentObject;
		if (attachmentOverride != ""){
			string[] p = attachmentOverride.Split(' ');
			targetObject = p[0].Replace("\"","");
			if (p.Count () > 1)
				parentObject = p[1].Replace("\"","");
			attachmentOverride = ""; // reset so this is only used once
		}
		
#if DEBUG_ATTACH
		UnityEngine.Debug.Log("TaskCharacter.Attach() : " + gameObject.name + "attaching...");
#endif
		
		if(targetObject == "nodeParent")
		{
#if DEBUG_ATTACH
            UnityEngine.Debug.Log("TaskCharacter.Attach() : " + gameObject.name + "attaching to nodeParent.");
#endif
			if(atNode != null)
				obj = atNode.transform.parent.gameObject;
			else
			{
                UnityEngine.Debug.LogError("TaskCharacter.Attach() : " + gameObject.name + "trying to attach to nodeParent, but not at node.");
				return;
			}
		}
		else
			obj = GameObject.Find(targetObject);
		
		if(obj == null)
		{
            UnityEngine.Debug.LogError("TaskCharacter.Attach() : " + gameObject.name + " could not find attachable object: " + targetObject);
			return;
		}

#if DEBUG_ATTACH
        UnityEngine.Debug.Log("TaskCharacter.Attach() : " + gameObject.name + " attaching to: " + obj.name);
#endif
		
		// we should only add this to 'attachedObjects' if the new parent is a bone of ours,
		// otherwise, we should remove this object from our list of attached objects if it is there.
		
		attachedObjects.Add(obj);
/*
		FootprintComponent foot = obj.GetComponent<FootprintComponent>();
		hasFootprint = false;
		if(foot != null) {
			hasFootprint = true;
			oldScale = foot.m_scale;
			obj.GetComponent<FootprintComponent>().m_scale = MoveScale;
		}
*/
//		hasCollider = false;
		if(obj.GetComponent<BoxCollider>() != null) {
//			hasCollider = true;
			obj.GetComponent<BoxCollider>().enabled = false;
		}
		Transform parent = this.GetComponent<AnimationManager>().GetBone(parentObject);
		if(parent != null)
			obj.transform.parent = parent;
		else
			obj.transform.parent = gameObject.transform; // should probably look for the game object by name here as well...
//		attachObject = parentObject;	// unused
		if( snapToNewParent ){
			obj.transform.localPosition = Vector3.zero; //
			obj.transform.localRotation = Quaternion.identity;
		}
	}
	
	bool isChild(GameObject obj){
		Transform check = obj.transform;
		while (check!= null){
			if (check.gameObject == gameObject)
				return true;
			check = check.parent;
		}
		return false;	
	}
	
	public void Detach(string val){
		// if a named object isn't attached, then ignore this call.
		// Scripted actions may call this when performing their own reparenting...
		string objName = val;
		if (objName.Contains(" ")){
			string[] p = objName.Split (' ');
			objName = p[0];
		}
		if (objName != ""){
			GameObject obj = null;
			foreach (GameObject go in attachedObjects){
				if (go.name == objName){
					obj = go;
					break;
				}
			}
			if (obj == null)
				return;
		}
		AttachmentOverride(val);
		Detach ();
	}	
	
	public void Detach() 
    {
		bool snapToNewParent=false;
		// allow skipping the event
		if (attachmentOverride == "skip"){
			attachmentOverride = "";
			return;
		}
		if (attachmentOverride.Contains ("snap")){
			snapToNewParent=true;
			attachmentOverride = attachmentOverride.Replace(" snap","");
			attachmentOverride = attachmentOverride.Replace("snap ","");
			attachmentOverride = attachmentOverride.Replace("snap","");
		}
		if (attachedObjects == null || attachedObjects.Count == 0) return;
		GameObject obj;
		
		// attachmentOverride might contain the attachedObject name, a new parent name, or both
		string objName="";
		string newParentName="";
		if (attachmentOverride != ""){
			if (attachmentOverride.Contains(" ")){
				string[] p = attachmentOverride.Split(' ');
				objName = p[0];
				newParentName = p[1];
			}
			else objName = attachmentOverride; // try first as object name
		}
		obj = attachedObjects.Last();// would the most recently attached object make more sense here?
		if (objName != ""){
			foreach (GameObject go in attachedObjects){
				if (go.name == objName){
					obj = go;
					objName = "";
					break;
				}
			}
		}
		if (newParentName == "" && objName != ""){ // we didnt find an object by the single argument name so it's a bone.
			newParentName = objName;
			objName = "";
		}
//UnityEngine.Debug.Log (name+" Detaching "+obj.name+" to "+newParentName);
//		if(hasFootprint) // no longer using simplePath
//			obj.GetComponent<FootprintComponent>().m_scale = oldScale;
		if(obj.GetComponent<BoxCollider>()!=null)
			obj.GetComponent<BoxCollider>().enabled = true;
		if (newParentName != ""){
			// find a game object to re-parent to
			GameObject newParent = GameObject.Find(newParentName);
			// new parent was null here during blanket cover, attachment override was "\spawnedbag3\" which no longer existed...
			// TODO must have failed to clear attachment override after swapping out pressure bag for normal bag
			if (newParent != null){
				obj.transform.parent = newParent.transform;
				if (snapToNewParent){
					obj.transform.localPosition= Vector3.zero;
					obj.transform.localRotation= Quaternion.identity;
				}
			}
			else{
				UnityEngine.Debug.LogError("TaskCharacter.Detach, failed to find new parent named "+attachmentOverride+" "+newParentName);
				obj.transform.parent = null; // this is an error condition
			}
		}
		else if(atNode != null && atNode.transform.parent != null )
			obj.transform.parent = atNode.transform.parent.transform;
		else
			obj.transform.parent = null;
		attachedObjects.Remove(obj);
		lastDetachedObject = obj; // temp hack to override animation event detach
	}
		
    // reset character
    public void Init()
    {
        // clear all the flags
        currentAnimation = "";
        executingTask = false;
		executingScript = null;
        currentTask = null;
        inPosition = false;
        moving = false;
        changingPosture = false;
        lerp = false;
        rotated = false;
        targetNode = null;
        endTime = 0;
//     oldParent = null;
//        oldScale = 0;
//        hasCollider = false;
//        hasFootprint = false;
        animating = false;
		// clear out any leftover IK targets
		if (IKArmLeft != null)
			IKArmLeft.target = null;
		if (IKArmRight != null)
			IKArmRight.target = null;
    }
	
	//Reset character for next task
	public void EndTask( Task task )
    {
        // if we're null we're not doing anything
        if (currentTask == null)
            return;

        // make sure we're ending the right task
        if (currentTask.data.name != task.data.name)
            return;

#if DEBUG_TASK
        UnityEngine.Debug.Log("TaskManager.EndTask() : Name=" + currentTask.data.name + " : Character=" + this.Name);
#endif

        // TODO....fix this
		//if(gameObject.GetComponent<AnimationManager>() != null)
        //  gameObject.GetComponent<AnimationManager>().LookStop();

        // tell everyone we're finished
        TaskCompleteMsg tc = new TaskCompleteMsg(currentTask.data.name);
        ObjectManager.GetInstance().PutMessage(tc);

        // reset for next action
        Init();
    }

    // TAKE TASK ASSIGNMENT OUT OF HERE!!
    // Check if character is ready for the next task
    public bool IsReady(CharacterTask characterTask, Task task)
    {
        waiting = false;

        // set task if not assigned already
        if (currentTask == null)
        {
            delay = Time.time + characterTask.delay;
            currentTask = task;
        }

		/*
        if (TaskMaster.GetInstance().CheckNode(nodeName) == true)
        {
#if DEBUG_TASK_HIGH
                Debug("Node <" + nodeName + "> is locked...");
#endif
            break;
        }
         */
        if (currentTask.data.CheckCondition() == false)
        {
#if DEBUG_TASK_HIGH
                Debug("TaskMaster.IsCharacterReady(" + characterName + ") char=" + characterName + ") task condition <" + currentTask.data.condition.Name + "> not met...");
#endif
            waiting = true;
            return false;
        }
        if (delay > Time.time)
        {
#if DEBUG_TASK_HIGH
            Debug("TaskMaster.IsCharacterReady(" + task.data.name + ") char=" + Name + ")  delayed...");
#endif
            waiting = true;
            return false;
        }
        if (currentTask.trackingName != task.trackingName)
        {
#if DEBUG_TASK_HIGH
            Debug("TaskMaster.IsCharacterReady(" + task.data.name + ") char=" + Name + " trackingName<" + currentTask.trackingName + "> != task.trackingName<" + task.trackingName + ">");
#endif
            return false;
        }
        if (!IsInPosition(characterTask.nodeName))
        {
#if DEBUG_TASK_HIGH
		    Debug("TaskMaster.IsCharacterReady(" + task.data.name + ") char=" + Name + " not in position: " + nodeName);
#endif
            return false;
        }
        if (!IsInPosture(characterTask.posture))
        {
#if DEBUG_TASK_HIGH
            Debug("TaskMaster.IsCharacterReady(" + task.data.name + ") char=" + Name + " not in posture");
#endif
            return false;
        }
        return true;
    }

    public virtual bool IsDone() 
    {
		if (currentTask!=null || executingScript != null || actingInScript != null)
			return false;
		// for brief windows, executing script can be null but we are still waiting for someone else...
		ScriptedObject so = GetComponent<ScriptedObject>();
		if (so != null && so.scriptStack!=null && so.scriptStack.Count > 0)
			return false;
		NavMeshAgentWrapper wrapper = GetComponent<NavMeshAgentWrapper>();
		if (wrapper != null && ( wrapper.isNavigating || wrapper.holdPosition) )
			return false;		
		
		return true;
	}

    public virtual bool IsWaiting()
    {
        return waiting;
    }

    public virtual bool IsChangingPosture()
    {
        return changingPosture;
    }

    public virtual bool IsMoving()
    {
        return moving;
    }

    public virtual bool IsAnimating()
    {
        return animating;
    }
	
	//Load all the postures into this character
	public void LoadPostureXML( string filename )
    {
        Serializer<List<PostureData>> serializer = new Serializer<List<PostureData>>();
		List<PostureData> pData = serializer.Load(filename);
        if (pData != null) {
			foreach(PostureData p in pData) 
			{
				//if(p.characters.Contains(gameObject.name))
				if(body.animation[p.name]!=null) // load posture if base clip is present
					AddPosture(p);
			}
			if(postures != null) {
				foreach(Posture posture in postures)
					posture.BuildTransitionAnims(postures, posture.keys, body.animation);
			}
			//availableTasks.Add(SetupTask(info));
			gameObject.GetComponent<AnimationManager>().Postures = postures;
		}
    }
	
	//Load animation-string pairs into this character
	public void LoadAnimationXML( string filename )
    {
        Serializer<List<AnimatedInteractionData>> serializer = new Serializer<List<AnimatedInteractionData>>();
		List<AnimatedInteractionData> aData = serializer.Load(filename);
        if (aData != null) {
			foreach(AnimatedInteractionData a in aData) {
				AddAnimatedInteraction(a);
			}
			gameObject.GetComponent<AnimationManager>().Interactions = aInteractions;
		}
    }
	
	public void AddAnimatedInteraction(AnimatedInteractionData data) 
    {
		if(aInteractions == null)
			aInteractions = new List<AnimatedInteraction>();
		aInteractions.Add(new AnimatedInteraction(data.name, data.clips, body.animation));
		if(data.attachments != null && data.attachments.Count > 0)
			foreach(AttachmentData aData in data.attachments)
				aInteractions[aInteractions.Count - 1].AttachableObjects.Add(aData.name, new AttachableObject(aData.targetObject, aData.targetBone));
		if(data.subAnims != null && data.subAnims.Count > 0)
			foreach(SubAnimData sData in data.subAnims)
				aInteractions[aInteractions.Count - 1].SubAnims.Add(sData.name, new SubAnim(sData.targetObject, sData.animName));
		
		// see if there's a script with a matching or specified name
		string eventScriptName = data.name;
		if (data.eventScript != null && data.eventScript != "")
			eventScriptName = data.eventScript;
		// now search the scene for a script of this name
		foreach (InteractionScript IS in ( FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[])){
			if (IS.name == eventScriptName){
				aInteractions[aInteractions.Count - 1].EventScript = IS;
				break;
			}
		}
		//aInteractions.Last().Debug();
	}
	
	public void AddPosture(PostureData data) 
    {
		if(postures == null)
			postures = new List<Posture>();
		if(data.hasAttachObject)
			postures.Add(new Posture(data.name, data.idleRate, data.idleDev, data.walkSpeed, body.animation, 
				true, data.targetObject, data.parentObject));
		else
			postures.Add(new Posture(data.name, data.idleRate, data.idleDev, data.walkSpeed, body.animation));
		if(data.subAnims != null && data.subAnims.Count > 0)
			foreach(SubAnimData sData in data.subAnims)
				postures[postures.Count - 1].SubAnims.Add(sData.name, new SubAnim(sData.targetObject, sData.animName));
        //postures.Last().Debug();
		postures.Last().keys = data.keys;
	}
	
	private Transform FindInChildren(GameObject go, string childName){
		foreach (Transform t in go.GetComponentsInChildren<Transform>()){
			if (t.name == childName) return t;
		}
		return null;
	}
	
	private void SetupIK(){
		if (IKArmLeft == null){
			Transform leftShoulder = FindInChildren( body, "sitelHuman_LArmUpperarm1");
			if (leftShoulder == null) return;
			Transform leftHand = FindInChildren( leftShoulder.gameObject, "sitelHuman_LArmPalm");
			GameObject goLeftIK = new GameObject("IKArmLeft");
			goLeftIK.transform.position = leftShoulder.position;
			goLeftIK.transform.rotation = leftShoulder.rotation;
			goLeftIK.transform.parent = transform;
			IKArmLeft = goLeftIK.AddComponent<IKArmController>();
			IKArmLeft.Setup(body,"left");
		}
		if (IKArmRight == null){
			Transform rightShoulder = FindInChildren( body, "sitelHuman_RArmUpperarm1"); //sitelHuman_RArmUpperarm1
			if (rightShoulder == null) return;
			Transform rightHand = FindInChildren( rightShoulder.gameObject, "sitelHuman_RArmPalm");
			GameObject goRightIK = new GameObject("IKArmRight");
			goRightIK.transform.position = rightShoulder.position;
			goRightIK.transform.rotation = rightShoulder.rotation;
			goRightIK.transform.parent = transform;
			IKArmRight = goRightIK.AddComponent<IKArmController>();
			IKArmRight.Setup(body,"right");
		}		
	}
	
	
	public override void PutMessage( GameMsg msg ) 
    {
        base.PutMessage(msg);

		InteractMsg interact = msg as InteractMsg;
        if (interact != null)
        {
            if (interact.map.item.Contains("TASK:"))
            {
#if DEBUG_TASK
                UnityEngine.Debug.Log("TaskCharacter.PutMessage(Interact=" + interact.map.item + ") : name=" + this.name);
#endif
// Scripting now	TaskMaster.GetInstance().PutMessage(msg);
            }
		}
		InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
			if (ismsg.InteractName.Contains(charTag)){
				// Handle mesh toggle messages
				if (ismsg.InteractName.Contains(":ON") || ismsg.InteractName.Contains(":OFF")){
					foreach (MeshToggle m in meshToggles){
						m.HandleTrigger(ismsg.InteractName);
					}
				}
			}
		}
		
	}

    string last;
    public void Debug(string text)
    {
        if (text != last)
        {
            last = text;
            UnityEngine.Debug.Log(text);
        }
    }

    //Calls the taskmaster to register at start
	public void Register() 
    {
		TaskMaster.GetInstance().RegisterCharacter(this);
	}
}
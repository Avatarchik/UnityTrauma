using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

public class ObjectInteractionInfo
{
    public string Name; 
    public string DialogTitle;
    public List<string> DialogItems;
    public string VoiceList;

    protected List<InteractionMap> map;
	[XmlIgnoreAttribute] // this keeps the system from trying to serialize this accessor
    public List<InteractionMap> ItemResponse
    {
        get 
        {
            // make a list of InteractionMap from string names
            if (map == null)
            {
#if DEBUG_OBJECTINTERACTION
                UnityEngine.Debug.Log("ItemResponse.Get() : DialogItems count=" + DialogItems.Count);
#endif

                map = new List<InteractionMap>();
                foreach (string item in DialogItems)
                {
                    InteractionMap imap = InteractionMgr.GetInstance().Get(item);
                    if (imap != null)
                    {
#if DEBUG_OBJECTINTERACTION
                        UnityEngine.Debug.Log("ItemResponse.Get() : map add=" + imap.item);
#endif
                        map.Add(imap);
                    }
                }
            }
            return map; 
        }
    }

    public ObjectInteractionInfo()
    { 
    }
}

public class ObjectInteraction : Object3D 
{
    public Vector2 topToolbarPos;

    public List<InteractionMap> ItemResponse;
    public List<InteractionMap> AllMaps;
	public bool onTeamMenu = false;
	public string interactOverrideTrigger = ""; // if present, trigger on click instead of displaying menu
	public Vector3 skillVector;
    public string prettyname;
	public string originXML = "";
	public string VoiceListXML = "";
	public Texture2D iconTexture = null;
    public bool DebugEnabled;
	public List<string> Roles = new List<string>(); // roles this object can take in scripts
	public InteractionScript actingInScript = null;
	public InteractionScript reservedForScript = null; // the highest priority script waiting for us.
    float idleTime = 0; // exposed time since we last were running any script (used for idles)
    public float IdleTime
    {
        get { return idleTime; }
        set { idleTime = value; }
    }	

    protected List<InteractionMap> Interactions;
	
/*  http://answers.unity3d.com/questions/32413/using-constructors-in-unity-c.html
    public ObjectInteraction()
        : base()
    {
        prettyname = "Interact";

    }
*/
    public override void Awake()
    {
//		Roles.Clear (); // where are these going to come from? case configuration ?		
		Roles.Add(name); // temporary, for initial backward compatibility, role==charactername	
		// roles needs to be initialized before we register with the object manager->dispatcher
		
        base.Awake();
		
        ItemResponse = new List<InteractionMap>();
        Interactions = new List<InteractionMap>();
        AllMaps = new List<InteractionMap>();
        DebugEnabled = false;		
		
        ItemResponse.Clear();
        Interactions.Clear();
		AllMaps.Clear();
    }

    public override void Start()
    {
        base.Start();

        highlight = true;
        HighlightObject(false);
		
		// be sure we have a collider, and that it's a trigger, and that our range is a resonable number
		Collider box = GetComponent<Collider>();
		if (box == null){
			box = gameObject.AddComponent<BoxCollider>() as Collider;
			box.isTrigger = true;	
		}
		
		// load voice lists (if any)
		if (VoiceListXML != null && VoiceListXML != "")
        	VoiceMgr.GetInstance().LoadXML(VoiceListXML);

		// Get the IconTag
		iconTag = GetComponentInChildren<IconTag>();
		// get scripted object
		SO = GetComponent<ScriptedObject>() as ScriptedObject;		
		
    }

    void OnLevelWasLoaded()
    {
		if ( ItemResponse != null )
        	ItemResponse.Clear();
    }

    public bool IsValidInteraction(InteractionMap map)
    {
        return IsValidInteraction(map.item);
    }

    public virtual bool IsValidInteraction(string InteractName)
    {
    	ScriptedObject so = this.GetComponent<ScriptedObject>();
        if (so != null)
		{
        	List<InteractionMap> maps = so.AllInteractions();
//			List<InteractionMap> maps = so.QualifiedInteractionsFor(gameObject);
			foreach( InteractionMap map in maps )
			{
				if ( InteractName == map.item )
					return true;
			}
		}
		return false;
    }
	
	public float GetCost(InteractionMap map){
		// currently, only InteractionScripts have a meaningful cost metric
    	ScriptedObject so = this.GetComponent<ScriptedObject>();
        if (so != null)
		{
			InteractionScript s = so.TriggeredScript(map.item,this as BaseObject);
			if (s != null){
				return s.GetCost(this);
			}
		}	
		return 99999; // we don't know how to do this.
	}
	
	public float SkillMetric(Vector3 skillLocation){
		// The simple case is, how far are my skills from what this task requires
		// default tasks will be at (0,0,0), so the more skilled I am, the more it will cost
		// to have me do this task.  Suggested skill vectors are in the [-1,+1] interval on all zxes
		// A more complex function here could handle multiple skillsets, etc.
		return Vector3.Distance(skillVector,skillLocation);	
	}
	
	public bool IsAvailableFor(InteractionScript script){
//		NeededFor (script); // this should only be done when we actually queue a script for execution.
		// if we are idle and reserved for noone, or reserved for the caller, then true
		if (actingInScript != null) return false;
		// if locked because performing some operations (BVM, for example)
// this fails, once locked, a character cannot become unlocked. re-think this.
/*
		NavMeshAgentWrapper wrapper = GetComponent<NavMeshAgentWrapper>();
		if (wrapper != null &&
			(wrapper.lockPosition || wrapper.holdPosition)) return false;
*/
		
		if (reservedForScript == null || reservedForScript == script) return true;
		return false;
	}
	
	public void NeededFor(InteractionScript script){
		// if the calling script is higher priority than anyone already needing us, reserve us for that.
		if (reservedForScript == null || reservedForScript == script){
			reservedForScript = script;
			return;
		}
		if (script.startPriority > reservedForScript.startPriority)
			reservedForScript = script;
	}
	
	public void ReleasedBy(InteractionScript script, ScriptedObject caller){
//		Debug.LogError(name+" Released by "+script.name);
		if (reservedForScript == script)
			reservedForScript = null; // we cleared this when we started the script...
		if (actingInScript == script){
			// if the releasing script was stacked, and will release to a higher script, we must not clear this
			// or it opens a window wher another waiting script could take control
			ScriptedObject so = caller; //GetComponent<ScriptedObject>();
			if (so == null) so = GetComponent<ScriptedObject>();
			if (so == null) return;
			if (so.scriptStack.Count <= 1){ // if the script that was running was not stacked
				// should we be setting this null here if we don't release ?
//				actingInScript = null; // now our character is free to run scripts. 
				int checkCount = GetComponent<ScriptedObject>().scriptStack.Count;
				if (GetComponent<ScriptedObject>().scriptStack.Count == 0){ // or maybe 1 ?
					GetComponent<ScriptedObject>().ReleasedBy(script);
					actingInScript = null; // now our character is free to run scripts. 
				}
				else
				{  // what should this branch do ?
					GetComponent<ScriptedObject>().ReleasedBy(script);
					actingInScript = null;
				}
			}
			else{
				ScriptedObject.QueuedScript temp = so.scriptStack.Pop ();
				InteractionScript callingScript = so.scriptStack.Peek ().script;
				so.scriptStack.Push (temp);
				// release any actors not needed by the calling script
				if (callingScript.actorObjects.Contains (this)){
					actingInScript = callingScript;
				}
				else
				{
					// reenabled these two lines 11/19/15 to get queued blanket scripts to run in parallel
					actingInScript = null;
					GetComponent<ScriptedObject>().ReleasedBy(script);
					// this was added to fix leaving a hole when roll patient finished in exam bleeding
					// add me to the calling script's actors so i will be released when it is done
					// but maybe we shold fix exam bleeding by adding the needed characters instead
					// of this fix, which prevents actors from being released after sub scripts...
//11/19/15					callingScript.actorObjects.Add (this);
//11/19/15					actingInScript = callingScript;
				}
			}
		}
	}

    virtual public void DoInteractMenu()
    {
        // causes object to dim
        OnMouseExit();
		
		if (interactOverrideTrigger != null && interactOverrideTrigger != ""){ // allow object to trigger a script instead of putting up a menu
			InteractMsg im = new InteractMsg( gameObject, interactOverrideTrigger, true);
			PutMessage(im);
			return;
		}

		// interact menu
		/* // disabling these menus PAA 6/22/15
        InteractDialogMsg msg = new InteractDialogMsg();
        msg.command = DialogMsg.Cmd.open;
        msg.baseobj = this;
        msg.title = prettyname;
        msg.x = (int)Input.mousePosition.x;
        msg.y = (int)Input.mousePosition.y;
		ScriptedObject so = GetComponent<ScriptedObject>(); // collect a list of items for the menu to show
		if (so != null)
			msg.items = so.QualifiedInteractions();
		else
			msg.items = ItemResponse; // this is where added items get placed on the menu, because they are in here...
        msg.modal = true;
		msg.baseXML = originXML;
        InteractDialogLoader.GetInstance().PutMessage(msg);

        Brain.GetInstance().PlayAudio("OBJECT:INTERACT:CLICK");
        */
    }

    virtual public void OnMouseUp()
    {
        if (ObjectInteractionMgr.GetInstance().Clickable == false)
            return;

        if (WithinRange() == false)
            return;

		if ( GUIManager.GetInstance().MouseOverGUI(Input.mousePosition) == true )
			return;

		if (IsActive() == true && Enabled == true)
        {
            if (InteractDialogLoader.GetInstance() != null)
            {
                DoInteractMenu();
            }
        }
    }

    public void Add( InteractionMap map )
    {
        InteractionMap imap = new InteractionMap();
        imap = map;
        imap.time = Time.time;
        Interactions.Add(imap);
    }
	
	// this temporary method is just to keep characters from saying they don't know how to do tasks PAA
	public void AddToAllMaps( InteractionMap map){
		if (!AllMaps.Contains(map)) // this is almost certain to pass, as this is a different instance
			AllMaps.Add ( map );	
	}

    public override void PutMessage( GameMsg msg ) 
    {
        // speech
        SpeechMsg speechmsg = msg as SpeechMsg;
        if (speechmsg != null)
        {
            HandleSpeech(speechmsg);
            return;
        }

        // log it
        InteractMsg interactMsg = msg as InteractMsg;
        if (interactMsg != null)
        {
            HandleInteractMsg(interactMsg);
            HandleResponse(interactMsg);

            if (IsActive() == true)
            {
                Brain.GetInstance().PutMessage(msg);
            }
        }
		// let our scriptedObject see this msg  I admit this isn't very clean yet.
		if (GetComponent<ScriptedObject>() != null) GetComponent<ScriptedObject>().PutMessage(msg);
    }

    virtual public void HandleInteractMsg( InteractMsg interactMsg )
    {
        // handle default
        ObjectInteractionMgr.GetInstance().HandleInteractMsg(this.name,interactMsg);
        // save interaction
        Add(interactMsg.map);
    }

    virtual public void HandleSpeech( SpeechMsg msg )
    {
        float result;

        // go through all InteractionMap messages and find one that matches
        foreach (InteractionMap map in ItemResponse)
        {
            // get name of each item
            string name = StringMgr.GetInstance().Get(map.item).ToLower();
            // Debug.Log("HandleSpeech() : compare <" + msg.Utterance + "> with <" + name + ">");
            if ((result=SpeechProcessor.GetInstance().CheckUtterance(msg.Utterance, name)) > 0.5f)
            {
                // add to speech msg
                msg.Stats.Add(new SpeechMsg.Info(this, map, result));

                // we're done
                return;
            }
        }

        // handle displaying menu by prettyname
        string menu = prettyname.ToLower() + " menu";
        // Debug.Log("HandleSpeech() : compare <" + msg.Utterance + "> with <" + name + ">");
        if ((result = SpeechProcessor.GetInstance().CheckUtterance(msg.Utterance, menu)) > 0.5f)
        {
            DoInteractMenu();
        }
    }

    public void HandleTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                RaycastHit hit = new RaycastHit();
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.gameObject == gameObject)
                    {
                        OnMouseUp();
                        OnMouseEnter();
                    }
                }
            }
            if (Input.GetTouch(i).phase == TouchPhase.Ended)
            {
                OnMouseExit();
            }
        }
    }

	public bool DetectClickThrough=true;

	public void HandleClicks()
	{
		if ( Input.GetMouseButtonUp(0) == true )
		{
			// if DetectClickThrough is true, then get all collisions
			// along the ray and check for ME
			if ( DetectClickThrough == true )
			{
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit[] hits = Physics.RaycastAll(ray);
				foreach( RaycastHit hit in hits )
				{
					if (hit.transform.gameObject == gameObject)
					{
						OnMouseUp();
					}
				}
			}
			// otherwise, allow Unity to block something that is
			// in front of us...
			else
			{
				RaycastHit hit = new RaycastHit();
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (Physics.Raycast(ray, out hit))
				{
					if (hit.transform.gameObject == gameObject)
					{
						OnMouseUp();
					}
				}
			}
		}
	}

	public enum State
	{
		none,
		idle,
		busy,
		talking
	}
	public State CurrentState;
	State lastState=State.none;
	public Texture2D texBusy,texTalking,texListening;
	IconTag iconTag;
	ScriptedObject SO;
	int checkCount = 0;

	public void HandleStatus()
	{
	/* These calls are terribly expensive, and are eating 50% of our CPU runtime  TODO Find a place where they can be done once */
		// get the icontag
		if (iconTag == null && checkCount < 100) {
			iconTag = GetComponentInChildren<IconTag> ();
			checkCount++;
		}

		// get scripted object
		if (SO == null && checkCount < 100) {
			SO = GetComponent<ScriptedObject> () as ScriptedObject;	
			checkCount++;
		}
	/*  */
		if ( SO != null )
		{
			// set busy
			if ( SO.GetCurrentScript() != null )
				CurrentState=State.busy;
			else
				CurrentState=State.idle;
			// set talking
			Brain.AudioInfo ai = Brain.GetInstance().CurrentAudioInfo;
			if ( ai != null )
			{
				if ( ai.name == Name )
				{
					CurrentState = State.talking;
					// turn on tag if talking
					if ( iconTag != null )
						iconTag.renderer.enabled = true;
				}
			}

			// reset iconTag to highlight status
			if ( iconTag != null )
			{
				iconTag.renderer.enabled = highlight;
				if ( CurrentState == State.talking )
					iconTag.renderer.enabled = true;
			}

			// check to see if we need updating
			if ( CurrentState == lastState )
				return;
			lastState = CurrentState;

			// change state
			if ( iconTag != null )
			{
				// set material
				switch (CurrentState) 
				{
				case State.busy:
					if ( texBusy == null )
						texBusy = Resources.Load<Texture2D>("GUI/npcStatus-busy");
					if ( texBusy != null )
						iconTag.renderer.material.mainTexture = texBusy;
					break;
				case State.talking:
					if ( texTalking == null )
						texTalking = Resources.Load<Texture2D>("GUI/npcStatus-talking");
					if ( texTalking != null )
						iconTag.renderer.material.mainTexture = texTalking;
					// force on if talking
					iconTag.renderer.enabled = true;
					break;
				case State.idle:
					if ( texListening == null )
						texListening = Resources.Load<Texture2D>("GUI/npcStatus-listening");
					if ( texListening != null )
						iconTag.renderer.material.mainTexture = texListening;
					break;
				}
			}
		}
	}

    public override void Update()
    {
        // ramp color
        if (IsActive() == true && Enabled == true)
        {
            base.Update();
        }

		HandleClicks();
        HandleTouches();
		HandleStatus();
    }

    public string Translate(string str)
    {
        // response string
        string translation = StringMgr.GetInstance().Get(str);
        return translation;
    }

    public virtual void HandleResponse(GameMsg msg)
    {
        ObjectInteractionMgr.GetInstance().HandleResponse(msg);
    }

    public void ChildClicked()
    {
        OnMouseUp();
    }

    public void ChildMouseOver(bool yesno)
    {
        if (yesno)
            OnMouseEnter();
        else
            OnMouseExit();
    }

    public virtual void LoadXML( string filename )
    {
        if (filename == null || filename == "")
        {
#if DEBUG_OBJECT_INTERACTION
            UnityEngine.Debug.Log("ObjectInteraction.LoadXML() : filename is NULL");
#endif
            return;
        }

        if (ItemResponse == null)
            ItemResponse = new List<InteractionMap>();

        // read
        Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
        ObjectInteractionInfo info;
        info = serializer.Load(filename);
        if (info == null)
        {
#if DEBUG_OBJECT_INTERACTION
            UnityEngine.Debug.Log("ObjectInteraction.LoadXML(" + filename + ") : info is NULL");
#endif
            return;
        }

        // grab info
        Name = info.Name;
        ItemResponse = info.ItemResponse;
        prettyname = info.DialogTitle;

#if DEBUG_OBJECT_INTERACTION
        UnityEngine.Debug.Log("ObjectInteraction.LoadXML(" + filename + ") : count=" + info.ItemResponse.Count + " : name=" + Name);
#endif

        // load voice lists (if any)
        VoiceMgr.GetInstance().LoadXML(info.VoiceList);

        // get all the maps
        if ( AllMaps == null )
            AllMaps = new List<InteractionMap>();
        GetInteractionMaps(ItemResponse, AllMaps);
    }

    public virtual void GetInteractionMaps( List<InteractionMap> items, List<InteractionMap> maps )
    {
        foreach (InteractionMap map in items)
        {
            if (map.item.Contains("XML:"))
            {
                // remove XML:
                string newXML = map.item.Remove(0, 4);
                // load this new one
                Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
                ObjectInteractionInfo info;
                info = serializer.Load("XML/Interactions/" + newXML);
                if (info != null)
                {
                    // parse new one
                    GetInteractionMaps(info.ItemResponse, maps);
                }
            }
            else
            {
                // add it
                maps.Add(map);
            }
        }
    }

    public void AddItem(InteractionMap item)
    {
        if (ItemResponse == null)
            ItemResponse = new List<InteractionMap>();

        // check to see if item is already here
        foreach (InteractionMap i in ItemResponse)
        {
            if (i.item == item.item)
            {
                // item already here, cya!
                return;
            }
        }

        // add it
        ItemResponse.Add(item);
		AllMaps.Add (item); // this needs to be in here, too.
		
    }

    public void DeleteItem(string item)
    {
        if (ItemResponse == null)
            ItemResponse = new List<InteractionMap>();

        foreach (InteractionMap map in ItemResponse)
        {
            if (map.item == item)
            {
                ItemResponse.Remove(map);
                return;
            }
        }
    }
		
	public void OnGUI(){ // this is really slow, we should use another mechanism, plus, is a base class using this?
		if (Enabled && iconTexture!= null){
			// assuming we are attached to something of interest
			Vector3 fromCam = Camera.main.transform.position - transform.position;
			float cameraDistance = fromCam.magnitude;
			if (cameraDistance < ActivateDistance && Vector3.Dot(fromCam,Camera.main.transform.forward)<0){ // close enough and in front of the camera
				float highlightSize = 1.0f;
				if (cameraDistance > 1.0f){ // shrink the highlight as it gets farther away
					highlightSize = 1.2f-(cameraDistance/ActivateDistance);
				}
				highlightSize *= 100f/iconTexture.width; // proportion to texture size
				// map world to screen,
				// bounce = ConstantOffsetFromPivot + BounceMagnitude * Mathf.Abs(Mathf.Sin( BounceRate *Time.realtimeSinceStartup));
				float bounce = 1.0f+0.125f*Mathf.Abs(Mathf.Sin(2.0f*Time.realtimeSinceStartup));
				Vector3 screenPos = Camera.main.WorldToScreenPoint (transform.position+new Vector3(0,bounce,0));
				GUI.DrawTexture(new Rect(screenPos.x-highlightSize*iconTexture.width/2,Screen.height - screenPos.y-highlightSize*iconTexture.height/2,highlightSize*iconTexture.width,highlightSize*iconTexture.height),iconTexture);
			}
		}
	}
}

class ObjectInteractionMgr
{
    public ObjectInteractionMgr()
    {
    }

    static ObjectInteractionMgr instance;
    public static ObjectInteractionMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new ObjectInteractionMgr();
        }
        return instance;
    }

    public List<ObjectInteraction> GetEligibleObjects(string InteractName)
    {
        List<ObjectInteraction> eligible = new List<ObjectInteraction>();

        List<BaseObject> objlist = ObjectManager.GetInstance().GetObjectList();
        if ( objlist == null )
            return eligible;

        foreach (BaseObject baseobject in objlist)
        {
            ObjectInteraction objint = baseobject as ObjectInteraction;
            if (objint != null)
            {
                if ( objint.IsValidInteraction(InteractName) == true )
                {
#if DEBUG_OBJECTINTERACTION
                    UnityEngine.Debug.Log("ObjectInteractionMgr.GetEligibleObjects(" + InteractName + ") : found in character=<" + objint.Name + ">");
#endif
                    eligible.Add(objint);
                }
            }
        }

        return eligible;
    }

	public List<ObjectInteraction> GetCharacters()
	{
		List<ObjectInteraction> eligible = new List<ObjectInteraction>();
		foreach (BaseObject item in ObjectManager.GetInstance().GetObjectList())
		{
			Character character = item as Character;
			if ( character != null )
				eligible.Add (character);
		}
		return eligible;
	}

    public string Translate(string str)
    {
        // response string
        string translation = StringMgr.GetInstance().Get(str);
        return translation;
    }

    virtual public void HandleInteractMsg(string name, InteractMsg interactMsg)
    {
		if ( interactMsg == null || interactMsg.map == null )
			return;
		
        // translate
        string response = Translate(interactMsg.map.response);
        // log interaction
        if (interactMsg.log == true)
        {
            InteractLogItem logitem = new InteractLogItem(Time.time, interactMsg.gameObject, interactMsg.map.item, StringMgr.GetInstance().Get(response), interactMsg);
            LogMgr.GetInstance().Add(logitem);
        }
    }

    public virtual void HandleResponse(GameMsg msg)
    {
        InteractMsg interactmsg = msg as InteractMsg;
        if (interactmsg == null || interactmsg.map == null)
            return;
		
        //UnityEngine.Debug.Log("ObjectInteractionMgr.HandleResponse(" + interactmsg.map.item + ") : name=<" + interactmsg.gameObject + "> : sound=<" + interactmsg.map.sound + ">");
        //interactmsg.map.Debug();

        // play audio
        Brain.GetInstance().PlayAudio(interactmsg.map.sound);

        // send interaction to the voice manager
        VoiceMgr.GetInstance().Play(interactmsg);

        // check for a response
        string str = interactmsg.map.response;
        if (str != null && str != "" && str.Length != 0)
        {
            // this translates any snapshot specific text
            string text = Translate(str);

            // check special case, option to display dialog
            if (text.Contains("DIALOGUE:"))
            {
                // remove the tag string
                text = text.Replace("DIALOGUE:", "");

                // load dialog, if ok then return
                if (DialogueTree.GetInstance().GoToDialogue(text, true) == true)
                {
                    return;
                }
            }

            // default to normal dialog
            InfoDialogMsg infomsg1 = new InfoDialogMsg();

            // grab title from map otherwise just use object's prettyname
            if (interactmsg.map.response_title != null && interactmsg.map.response_title != "")
                infomsg1.title = StringMgr.GetInstance().Get(interactmsg.map.response_title);
            else
                infomsg1.title = "response_title";

            if (InfoDialogLoader.GetInstance() != null)
            {
                infomsg1.command = DialogMsg.Cmd.open;
                infomsg1.text = text;
//                InfoDialogLoader.GetInstance().PutMessage(infomsg1);
            }
        }
    }

    public bool Highlight = true;
    public bool Clickable = true;
	
	
}


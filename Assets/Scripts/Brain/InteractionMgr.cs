#define DEBUG_INTERACTIONLIST

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

public class InteractionMap
{    
    // item=name of interaction.  all lists of interactions are found in interaction.xml.
    // once an interaction gets dispatched, a InteractStatusMsg is broadcast to all registered items in system
    public string item;                 
    // response and response title are used for the display of a default interaction dialog.  not required
    public string response;             
    public string response_title;
    // tooltip text for this InteractionMap
    public string tooltip;
    // default note for this interaction
    public string note;
    // sfx played on interaction
    public string sound;
    // task used by TaskMaster to manage task handling and for TASK:COMPLETE msgs.
    public string task;
    // if list is present this interaction points to an InteractionList which is found in the interactions.xml file
    public string list;
    // time of interaction used for logging
    public float time;
    // should we log this item or not
    public bool log;
    // max number of this type of interaction per run
    public int max;
    // set when a menu is built on click, how should the menu display this interaction
	public InteractionScript.readiness readyState = InteractionScript.readiness.readyToRun;
	// how important is this interaction, for choosing if multiples are queued, highest is picked
	public int startPriority = 4; // run priority could come later with interruptions...
	// prereq of interactions required to trigger this interaction
    public List<string> prereq;
	// an optional interactionScript which might perform this interaction
	public string scriptName;
	// an optional character/baseObject which might perform this interaction
	public string objectName;
    // ask for confirmation of this interaction with dialog
    public bool confirm;
    // confirmation dialog when answer is YES
    public string confirm_audio;
	// list of menu categories this interaction should be shown in  category.subcategory.subsubcategory.etc
	public List<string> category;
	// enabled, for dynamic menu, will be greyed out if false
	public bool Enabled; // caps to avoid conflict with monoBehaviour.enabled
	// List of parameters, primarily for passing values into interaction scripts like drug dosage, etc.
	public List<string> param;

    public InteractionMap()
    {
        log = true;
        max = 0;
        confirm = false;
		Enabled = true;
    }

    public InteractionMap(string item, string response, string response_title, string note, string tooltip, string sound, string task, bool log)
    {
        //Debug.Log("new InteractionMap(" + item + ")");
        this.item = item;
        this.response = response;
        this.response_title = response_title;
        this.note = note;
        this.tooltip = tooltip;
        this.sound = sound;
        this.task = task;
        this.log = log;
        this.time = 0.0f;
        this.list = null;
		Enabled = true;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("InteractionMap[" + item + "] : sound=<" + sound + ">");
    }
}

[System.Serializable]
public class InteractionSet
{
	// An InteractionSet is meant to represent a state where a group of interactions should
	// all be executed, then the state is exited.  
	// If ANY OTHER interaction, not part of the SET, is executed, then the state is left.
	// lists of scripts (tags) are provided to be executed on transitions into and out of the state,
	// both for normal completion exit, and for abnormal out-of-set exit.
	// a persistent set of booleans is maintained to track if all set elements have been executed,
	// so the state can be resumed after leaving it in order to complete the set.
	
	public string Name;
	public List<string> Maps;
	public List<string> ScriptsEnter;
	public List<string> ScriptsExit;
	public List<string> ScriptsAbort;
	public List<string> ValidInteractors;  // if non null, only interactions these objects have will be considered
	[XmlIgnore]
	public Dictionary<string,ObjectInteraction> objectInteractions; // to save on the lookup
	List<bool> executed = null;
	
	public InteractionSet()
	{
		Init();
	}
	
	public void Init()
	{
		if (Maps != null){
			executed = new List<bool>(Maps.Count); // This doesnt actaully create list elements, just internal capacity
			for (int i = 0; i<Maps.Count;i++)
				executed.Add (false);
		}
		if (ValidInteractors != null && ValidInteractors.Count > 0) {
			objectInteractions = new Dictionary<string,ObjectInteraction >();
			foreach (string name in ValidInteractors){
				GameObject go = GameObject.Find(name);
				if (go != null && go.GetComponent<ObjectInteraction>() != null){
					objectInteractions[name] = go.GetComponent<ObjectInteraction>();
				}
			}
		}
	}
	
	public void CheckInteraction( string name )
	{

		
		// check whether this command is in the list
		// and set executed if found
		bool found = false;
		if (executed == null || executed.Count < Maps.Count) 
			Init();

		// if only certain characters are to be considered for this set, be sure one has the interaction in question
		if (ValidInteractors != null && ValidInteractors.Count > 0) {
			bool isValid = false;
			foreach (ObjectInteraction OI in objectInteractions.Values){
				if (OI.IsValidInteraction( name)){
					isValid = true;
					break;
				}
			}
			if (!isValid) return;
	    }
		
		for (int i=0 ; i<Maps.Count ; i++)
		{
			if ( Maps[i] == name )
			{
				executed[i] = true;						
				found = true;
			}
		}

		// if found then check to see if all the interactions were 
		// executed in the set
		if ( found == true )
		{
			foreach( bool item in executed )
			{
				// if item not done then just return
				if ( item == false )
					return;
			}
			// all items executed, call EXIT
			DoExit();
		}
		else
		{
			// call abort scripts 
			DoAbort();			
		}
	}
	
	public virtual void DoEnter(){
		if (executed == null) Init (); // be sure we have a valid executed list
}
	
	public virtual void DoExit()
	{
		// send the exit list thru the dispatcher for queuing
		foreach (string tag in ScriptsExit){
			InteractionMap map = new InteractionMap(tag, null, null, null, null, null, null, true );
			InteractMsg msg = new InteractMsg(null, map); // who should be the game object here? the dispatcher will figure that out.
			Dispatcher.GetInstance().PutMessage( msg);	
		}
		// clear current set

		//InteractionMgr.GetInstance().CurrentSet = null;
		InteractionMgr.GetInstance ().AbortCurrentSet(); // this sets current set null and calls init for next time
	}
	
	public virtual void DoAbort()
	{
		// clear current set - try doing this before running the abort scripts instead of after
		InteractionMgr.GetInstance().AbortCurrentSet();
		// send the abort list thru the dispatcher for queuing
		foreach (string tag in ScriptsAbort){
			InteractionMap map = new InteractionMap(tag, null, null, null, null, null, null, true );
			InteractMsg msg = new InteractMsg(null, map); // who should be the game object here? the dispatcher will figure that out.
			Dispatcher.GetInstance().PutMessage( msg);	
		}
		// clear current set - moved to before ?
		//InteractionMgr.GetInstance().AbortCurrentSet();
	}
	
}

public class InteractionInfo
{
    public InteractionInfo() {}
    public List<InteractionMap> Interactions;
    public List<InteractionList> InteractionLists;
}

public class Interaction
{
    public string Name;         // InteractionMap name
    public string Character;    // character to execute this interaction
    public float WaitTime;      // delay before this interaction happens
    public bool WaitTask;       // wait for this interaction to complete before executing next interaction

    public Interaction() 
    {
        WaitTime = 0;
        WaitTask = false;
    }

    private InteractionMap map;
    public InteractionMap Map
    {
        get
        {
            if (map == null)
            {
                map = InteractionMgr.GetInstance().Get(Name);
                if (map == null)
                {
                    UnityEngine.Debug.Log("Interaction.Map.get : ineraction(" + Name + ") not found");
                }
            }
            return map;
        }
    }

    public void Debug()
    {
        if ( Map == null )
            return;

        UnityEngine.Debug.Log("Interaction : Name=" + Name + " : Map=" + Map.item);
    }
}

public class InteractListMsg : InteractMsg
{
    public InteractListMsg(GameObject obj, InteractionMap item) : base(obj,item)
    {
    }

    public InteractListMsg(GameObject obj, InteractionMap item, bool log) : base(obj,item,log)
    {
    }

    public InteractListMsg(GameObject obj, string item, bool log) : base(obj,item,log)
    {
    }
}

public class InteractionList
{
    public InteractionList() 
    {
        Interactions = new List<Interaction>();
        Interrupt = true;
        current = null;
    }

    public InteractionList(InteractionMap map, InteractionList list)
    {
        current = null;

        this.Map = map;
        this.Name = list.Name;
        this.Loop = list.Loop;
        this.Interrupt = list.Interrupt;

#if DEBUG_INTERACTIONLIST
        UnityEngine.Debug.Log("InteractionList() : Interaction=" + map.item + " : List=" + list.Name);
#endif

        Interactions = new List<Interaction>();
        foreach (Interaction interaction in list.Interactions)
        {
            Interactions.Add(interaction);
        }
    }

    public string Name;                     // name of List
    public InteractionMap Map;              // original interaction which owns this list, also used to generate :COMPLETE msg
    public bool Loop;                       // loop list
    public bool Interrupt;                  // is interruptable
    public List<Interaction> Interactions;  // list of Interactions  

    protected int index;                    // current index
    protected Interaction current;          // current Ineraction Object
    protected ObjectInteraction target;     // target (who) is doing this list
    protected float elapsedTime;            // total time

    public Interaction Current
    {
        get { return current; }
    }

    public void PutMessage()
    {
#if DEBUG_INTERACTIONLIST
        UnityEngine.Debug.Log("InteractionList.PutMessage() : index=" + index + " : Map=" + current.Map.item + " : target="+target);
#endif
        // send current message
        if (current != null)
        {
            if (current.Character != null && current.Character != "")
            {
                // selecting a different target
                ObjectInteraction newtarget = ObjectManager.GetInstance().GetBaseObject(current.Character) as ObjectInteraction;
                if (newtarget != null)
                {
                    // got our new target object, issue the interaction
#if DEBUG_INTERACTIONLIST
                                UnityEngine.Debug.Log("InteractionMgr.PutMessage() : changing target to <" + current.Character + ">, map=<" + current.Map.item + ">");
#endif
                    newtarget.PutMessage(new InteractListMsg(newtarget.gameObject, current.Map));
                }
            }
       
        else if (target != null)
        {
            // no character override, just execute from the owner of the list
            target.PutMessage(new InteractListMsg(target.gameObject, current.Map));
        }
		}
//        if (target != null && current != null)
//        {
//            if (current.Character != null && current.Character != "")
//            {
//                // selecting a different target
//                ObjectInteraction newtarget = ObjectManager.GetInstance().GetBaseObject(current.Character) as ObjectInteraction;
//                if (newtarget != null)
//                {
//                    // got our new target object, issue the interaction
//#if DEBUG_INTERACTIONLIST
//                    UnityEngine.Debug.Log("InteractionMgr.PutMessage() : changing target to <" + current.Character + ">, map=<" + current.Map.item + ">");
//#endif
//                    newtarget.PutMessage(new InteractListMsg(newtarget.gameObject, current.Map));
//                }
//            }
//            else
//            {
//                // no character override, just execute from the owner of the list
//                target.PutMessage(new InteractListMsg(target.gameObject, current.Map));
//            }
//        }
    }

    public void SetTarget(ObjectInteraction obj)
    {
        target = obj;
    }

    public bool IsDone()
    {
        if (current == null)
            return true;
        else
            return false;
    }

    public void Start()
    {
        elapsedTime = 0;

        if (Interactions != null && Interactions.Count > 0)
        {
            // set current to first
            index = 0;
            current = Interactions[index];
#if DEBUG_INTERACTIONLIST
            UnityEngine.Debug.Log("InteractionList.Start() : Current=" + current.Name + " : Loop=" + Loop);
#endif
            PutMessage();
        }
    }

    public void Next()
    { 
        elapsedTime = 0.0f;

        if (++index >= Interactions.Count)
        {
#if DEBUG_INTERACTIONLIST
            UnityEngine.Debug.Log("InteractionList.Next(), at end, current=" + current.Name + " : count=" + Interactions.Count); 
#endif
            // send msg to brain
            InteractStatusMsg msg = new InteractStatusMsg(Map.item + ":COMPLETE");
            Brain.GetInstance().PutMessage(msg);  

            // if loop then restart it, otherwise we are done
            if (Loop == true)
                Start();
            else
            {
                current = null;
            }
        }
        else
        {
            // put next interaction
            current = Interactions[index];
            PutMessage();
        }
    }

    float debugTime = 0.0f;

    public void Update(float deltaTime)
    {
        if (current == null)
            return;

#if DEBUG_INTERACTIONLIST
        if (Time.time > debugTime)
        {
            debugTime = Time.time + 2.0f;
            if ( current.WaitTask == true )
                UnityEngine.Debug.Log("InteractionList.Update() : waiting for task finished=" + current.Name);
        }
#endif

        elapsedTime += deltaTime;
        // if wait flag is not set then go ahead and execute next if we're over the wait time
        if (elapsedTime > current.WaitTime && !current.WaitTask)
        {
            Next();
        }
    }

    // call this when Task is complete
    public void TaskComplete( string taskname )
    {
        if (current == null)
        {
#if DEBUG_INTERACTIONLIST
            UnityEngine.Debug.Log("InteractionList.TaskComplete(NULL) : taskname=" + taskname);
#endif
            return;
        }

#if DEBUG_INTERACTIONLIST
        UnityEngine.Debug.Log("InteractionList.TaskComplete(" + current.Name + ") : taskname=" + taskname);
#endif
        if (taskname == current.Name && current.WaitTask == true)
        {
            Next();
        }
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("InteractionList : Name=" + Name + " : Loop=" + Loop);
        foreach (Interaction i in Interactions)
        {
            i.Debug();
        }
    }
}

public class InteractionMgr
{
    public List<InteractionMap> Interactions;           // list of all interactions registered in the system
    public List<InteractionList> InteractionLists;      // list of all InteractionLists!
    public List<InteractionMap> Log;                    // log of interactions
	
	public InteractionMgr()
	{
    	Interactions = new List<InteractionMap>();		
	}

    static InteractionMgr instance;
    public static InteractionMgr GetInstance()
    {
        if (instance == null)
            instance = new InteractionMgr();
        return instance;
    }

    public void LoadXML(string filename)
    {
        if (Interactions == null)
            Interactions = new List<InteractionMap>();

        Serializer<InteractionInfo> serializer = new Serializer<InteractionInfo>();
        InteractionInfo info;
        info = serializer.Load(filename);
        if (info != null)
        {
//           Interactions = info.Interactions; // we don't need to load this any more
            InteractionLists = info.InteractionLists;
            //Debug();
        }
        else
            UnityEngine.Debug.Log("InteractionMgr.LoadXML(" + filename + ") : interactions=null");
    }
	
    public void SaveXML(string filename)
    {	// saves the current set of interactions and lists to file
		InteractionInfo sInfo = new InteractionInfo();
		sInfo.Interactions = Interactions;
		sInfo.InteractionLists = InteractionLists;
		Serializer<InteractionInfo> iSerializer = new Serializer<InteractionInfo>();
		iSerializer.Save(filename, sInfo);
	}
	
// <phil says> I'm leaning toward keeping my interaction set on the script that sets is, and passing it in when I 
// execute that script, rather than maintaining a separate XML file with all the info in there...
	List<InteractionSet> InteractionSets;
	public void LoadInteractionSetXML( string filename )
	{
		Serializer<List<InteractionSet>> serializer = new Serializer<List<InteractionSet>>();
		InteractionSets = serializer.Load(filename);
	}
	
	InteractionSet currentSet=null;

	// clear current set without causing a loop!
	public void AbortCurrentSet(){
		if (currentSet != null)
			currentSet.Init();
		currentSet = null;
	}

	public InteractionSet CurrentSet
	{
		set { 
			if (currentSet == value) return;
			// if we're doing a set then abort it
			if ( currentSet != null ){
				InteractionSet tmp = currentSet;
				currentSet = null;
				tmp.DoAbort();
			}
			// check and see if we have this set, if not then add it and set
			InteractionSet existingSet = null;
			if (value != null) existingSet = GetInteractionSet(value.Name);
			if ( existingSet == null )
				AddInteractionSet(value);
			// call enter method
			if (value != null) value.DoEnter();
			// set current
			currentSet = value; 
		}
	}
	
	public void AddInteractionSet( InteractionSet set )
	{
		if ( InteractionSets == null )
			InteractionSets = new List<InteractionSet>();
		InteractionSets.Add(set);
	}
	
	public void RemoveInteractionSet( string name )
	{
		foreach( InteractionSet set in InteractionSets )
		{
			if ( set.Name == name )
			{
				InteractionSets.Remove(set);
				return;
			}
		}		
	}
	
	public InteractionSet GetInteractionSet( string name )
	{
		if (InteractionSets == null) return null;
		
		foreach( InteractionSet set in InteractionSets )
		{
			if (set == null){
				UnityEngine.Debug.LogWarning("null interaction set encountered when searching for "+name);
			}
			else
			{
				if ( set.Name == name )
					return set;
			}
		}
		return null;
	}
	
	public void EvaluateInteractionSet( string interactName )
	{
		if ( currentSet != null )
			currentSet.CheckInteraction(interactName);
	}
	
	public void EvaluateInteractionSet( GameMsg msg )
	{
		if ( currentSet == null )
			return;
		
		InteractStatusMsg ismsg = msg as InteractStatusMsg;
		if ( ismsg != null )
		{
			EvaluateInteractionSet(ismsg.InteractName);
		}
	}
		
    public void Debug()
    {
        if (Interactions != null)
        {
            foreach (InteractionMap map in Interactions)
                map.Debug();
        }
        else
            UnityEngine.Debug.Log("InteractionMgr.Debug() : Interactions=null");

        if (InteractionLists != null)
        {
            foreach (InteractionList list in InteractionLists)
                list.Debug();
        }
        else
            UnityEngine.Debug.Log("InteractionMgr.Debug() : InteractionLists=null");
    }

    public InteractionMap Get(string name)
    {
        if (Interactions == null)
            return null;

        foreach (InteractionMap map in Interactions)
        {
            if (map.item == name)
                return map;
        }
		if(name.StartsWith("XML:"))
		   return new InteractionMap(name, "", "", "", "", "", "", true);
        return null;
    }

    public InteractionList GetList(string name)
    {
        if (InteractionLists == null)
            return null;

        foreach (InteractionList list in InteractionLists)
        {
            if (list.Name == name)
                return list;
        }
        return null;
    }
	
	// build a list of interactions an entity could do right now.
	// this would be to build a menu or as a starting place for planning.
	public List<InteractionMap> GetValidInteractionsFor(ObjectInteraction obj){
		// go thru all the known interactions, checking each one with obj
		List<InteractionMap> validList = new List<InteractionMap>();
		foreach (InteractionMap map in Interactions){
			if (obj.IsValidInteraction(map.item)) // this is just a placeholder test for now
			{
				
				validList.Add (map);
			}
		}
		return validList;
	}

    public void Add(InteractionMap imap)
    {
        foreach (InteractionMap map in Interactions)
        {
            if (map.item == imap.item)
                return;
        }
        Interactions.Add(imap);
    }
	
    public void UpdateOrAdd(InteractionMap imap)
    {
		// I probably should have used a different vern than 'Update' for this method, as Update already means something else...
		// overwrite an existing iMap with the info from a new one...
		// we can't assign to map if we use foreach to iterate, so walk thru with an index.
        for (int i = 0; i< Interactions.Count;i++)
        { 
            if (Interactions[i].item == imap.item)
			{	
				// we probaqbly need to do this field by field, including the lists
				// but it's worth a shot trying the assignment...
				Interactions[i] = imap;
                return;
			}
        } // if the imap wasn't found, just add it as a new one.
        Interactions.Add(imap);
    }	

    public void Add(List<InteractionMap> list)
    {
        foreach (InteractionMap map in list)
            Add(map);
    }

    public void AddLog(InteractionMap map)
    {
        if (Log == null)
            Log = new List<InteractionMap>();
        Log.Add(map);
    }

    public void ClearLog()
    {
        Log = new List<InteractionMap>();
    }

    public InteractMsg SendInteractionMap(string objname, string mapname)
    {
        return SendInteractionMap(objname, Get(mapname));
    }

    public InteractMsg SendInteractionMap(string objname, InteractionMap map)
    {
        InteractMsg imsg = new InteractMsg(null, map);

        // log it
        if (map.log == true)
        {
            InteractLogItem logitem = new InteractLogItem(Time.time, objname, imsg.map.item, StringMgr.GetInstance().Get(imsg.map.response), imsg);
            LogMgr.GetInstance().Add(logitem);
        }

        // send to brain
        Brain.GetInstance().PutMessage(imsg);

        return imsg;
    }
}



//#define DEBUG_INTERACTIONLIST

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TaskCompleteMsg : GameMsg 
{
    public string TaskName;
	public TaskCompleteMsg( string name ) 
    {
        TaskName = name;
    }
}

public class Character : TaskCharacter
{
/*  http://answers.unity3d.com/questions/32413/using-constructors-in-unity-c.html
    public Character()
        : base()
    {
        InteractionList = null;
    }
*/

    public InteractionList InteractionList;

    public string XMLName;
    public void LoadInteractionXML(string name)
    {
        // load base interactions
        base.LoadXML(name);

        // register this object
        ObjectManager.GetInstance().RegisterObject(this);
    }
	
	public override void Awake() {
		base.Awake();
		originXML = XMLName;
	}
	
	public override void Start() {
		base.Start();
	}
	
	public override bool IsDone() 
    {
        if (InteractionList == null && base.IsDone())
        {
            if (DebugEnabled == true)
                Debug("Character<" + Name + "> TRUE : base.IsDone()=" + base.IsDone() + " : InteractionList==null");
            return true;
        }
        else if ((InteractionList != null && InteractionList.IsDone()) && base.IsDone())
        {
            if (DebugEnabled == true)
                Debug("Character<" + Name + "> TRUE : base.IsDone()=" + base.IsDone() + " : InteractionList.IsDone()=" + InteractionList.IsDone());
            return true;
        }
        else
        {
            if (DebugEnabled == true)
            {
                if ( InteractionList != null )
                    Debug("Character<" + Name + "> FALSE : base.IsDone()=" + base.IsDone() + " : InteractionList.IsDone()=" + InteractionList.IsDone());
                else
                    Debug("Character<" + Name + "> FALSE : base.IsDone()=" + base.IsDone() + " : InteractionList==null");
            }
            return false;
        }
	}

    public override void Update()
    {
        base.Update();

        if (InteractionList != null)
        {
            InteractionList.Update(Time.deltaTime);
        }

        HandleInteractMsgQueue();
    }
	
	// Talking flags
	float talkTime = 0.0f;
	public float TalkTime
	{
		set 
		{ 
			talkTime = value; 
			talking = true;
		}
		get
		{
			return talkTime;
		}
	}

	bool talking = false;
	public bool Talking
	{
		get 
		{
			// check if current time is greater than talkTime, if so
			// we're done talking...
			if ( Time.time > talkTime )
				talking = false;
			else
				talking = true;
			return talking;
		}
	}

    public override void HandleInteractMsg(InteractMsg msg)
    {
        //UnityEngine.Debug.Log("Character.HandleInteractMsg(" + msg.GetType() + ") : target=" + this.Name);

        base.HandleInteractMsg(msg);

        // special case : this handles all messages coming from an interaction list.  
        InteractListMsg imsg = msg as InteractListMsg;
        if (imsg != null)
        {
#if DEBUG_INTERACTIONLIST
            imsg.map.Debug();
#endif
            // handle InteractionList case
            if (imsg.map.list != null)
            {
                InteractionList tmp = InteractionMgr.GetInstance().GetList(imsg.map.list);
                if (tmp != null)
                {
                    // only create a list if the current list is null or we have a list and it is interruptable
                    if (InteractionList == null || (InteractionList != null && InteractionList.Interrupt == true))
                    {
                        // make a copy
                        InteractionList = new InteractionList(imsg.map,tmp);
#if DEBUG_INTERACTIONLIST
                        UnityEngine.Debug.Log("Character.HandleInteractMsg(" + imsg.map.item + ") : Name=" + Name + "List=" + imsg.map.list);
                        InteractionList.Debug();
#endif
                        InteractionList.SetTarget(this);
                        InteractionList.Start();
                    }
                }
            }
        }
        else
        {
            // check to see if we can interrupt the list in progress with this incoming message
            if (InteractionList != null)
            {
#if DEBUG_INTERACTIONLIST
                UnityEngine.Debug.Log("Character.PutMessage(InteractMsg) : InteractionList=" + InteractionList.Name + " : Interrupt=" + InteractionList.Interrupt);
#endif
                if (InteractionList.Interrupt == true)
                    InteractionList = null;
            }
        }

        // handle InteractionList case
        if (msg.map.list != null)
        {
            InteractionList = InteractionMgr.GetInstance().GetList(msg.map.list);
            if (InteractionList != null)
            {
                // make a copy
                InteractionList = new InteractionList(msg.map, InteractionList);

#if DEBUG_INTERACTIONLIST
                UnityEngine.Debug.Log("Provider.HandleInteractMsg(" + msg.map.item + ") : Name=" + Name + "List=" + msg.map.list);
                InteractionList.Debug();
#endif

//  lets not ow that we are script driven              InteractionList.SetTarget(this);
//                InteractionList.Start();
            }
        }

    }
	
    public int GetInteractionCount(string name)
    {
        List<InteractLogItem> items = LogMgr.GetInstance().FindLogItems<InteractLogItem>();
        // check for 'name' items
        int count = 0;
        foreach (InteractLogItem item in items)
        {
            if (item.InteractName == name)
                count++;
        }
        return count;
    }

    public virtual void HandleInteractionError(InteractMsg imsg, string error)
    {
        UnityEngine.Debug.LogError("Character.HandleInteractionError() : name=" + Name + " : InteractName=" + imsg.map.item + " : error=" + error);
        VoiceMgr.GetInstance().Play(Name, error);
    }

    List<InteractMsg> InteractQueue;

    public void QueueInteractMsg(InteractMsg msg)
    {
        if (InteractQueue == null)
            InteractQueue = new List<InteractMsg>();

        InteractQueue.Add(msg);
    }

    public void HandleInteractMsgQueue()
    {
		if (InteractQueue == null || InteractQueue.Count == 0 ) return; // quickest exit in most cases
		
        // can we even do something else right now?
        if (IsDone() == false)
            return;

        // check to see if we're in the middle of an interaction list
        if (InteractionList != null && InteractionList.IsDone() == false)
            return;

        // anything else to do?
        if (InteractQueue != null && InteractQueue.Count > 0)
        {
            // get first item
            InteractMsg imsg = InteractQueue[0];
            // remove from list
            InteractQueue.RemoveAt(0);
            // execute!!
            PutMessage(imsg);
        }
    }

    public virtual bool CheckValidInteraction( GameMsg msg)
    {
        // don't worry about InteractListMsgs
        if (msg.GetType() == typeof(InteractListMsg))
        {
            //UnityEngine.Debug.LogWarning("Character.CheckValidInteraction(" + msg.GetType() + ") : ignoring type InteractListMsg");
            return true;
        }

        // check InteractMsg
        InteractMsg imsg = msg as InteractMsg;
        if (imsg != null && imsg.map != null )
        {
            // check prereq
            if (imsg.map.prereq != null && imsg.map.prereq.Count() > 0)
            {
                foreach (string item in imsg.map.prereq)
                {
                    if (GetInteractionCount(item) < 1)
                    {
                        HandleInteractionError(imsg, "VOICE:MISSING:PREREQ");
                        return false;
                    }
                }
            }
            // check for exceeding max commands
            if (imsg.map.max != 0)
            {
                if (GetInteractionCount(imsg.map.item) >= imsg.map.max)
                {
                    HandleInteractionError(imsg, "VOICE:EXCEEDED:MAX:INTERACTION");
                    // we're done!
                    return false;
                }
            }
            if (IsValidInteraction(imsg.map.item) == false)
            {
                HandleInteractionError(imsg, "VOICE:WRONG:PERSON");
                // we're done!
                return false;
            }
/* go ahead and queue this, let the dispatcher handle it..
            // check if we're busy
            if (IsDone() == false)
            {
                // Let everyone know we are busy but will get to it later ...
//                HandleInteractionError(imsg, "VOICE:BUSY"); // this happens too often with the scripting system, lets revisit.
                // Queue 
                QueueInteractMsg(imsg);
                return false;
            }	
*/		
        }

        return true;
    }
	
	public void ConfirmInteractDialogCallback( string msg )
	{
		// obj should be us, and the button
		
		bool handled = false;
		
		if (msg.Contains("onbutton="))
		{
	        if (msg.Contains("interact="))
        	{
	            // get rid of tag
            	string map;
            	if (ScriptedAction.GetToken(msg, "interact", out map) == true)
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
                	if (ScriptedAction.GetToken(msg, "object", out _object) == true)
                	{
						// we have an object, go find it
	                    // get object
                    	obj = ObjectManager.GetInstance().GetBaseObject(_object);
                	}

					// send to object if we have one!
                    if (obj != null)
                    {
						// create InteractMsg
                       	InteractMsg imsg = new InteractMsg(obj.gameObject, imap);
                       	imsg.map.confirm = false;
                       	obj.PutMessage(imsg);
                    }
            	}
			}
		}

	}

    public override void PutMessage(GameMsg msg)
    {
        // check for correct person, not busy, etc.
        if (CheckValidInteraction(msg) == false)
            return;

        TaskCompleteMsg complete = msg as TaskCompleteMsg;
        if ( complete != null )
        {
            if (InteractionList != null)
            {
#if DEBUG_INTERACTIONLIST
                UnityEngine.Debug.Log("Character.PutMessage() : TaskCompleteMsg, Name=" + Name + " : InteractionList=" + InteractionList.Name + " : task=" + InteractionList.Current.Name);
#endif
                InteractionList.TaskComplete(complete.TaskName);
            }
            else
            {
#if DEBUG_INTERACTIONLIST
                UnityEngine.Debug.Log("Character.PutMessage() : TaskCompleteMsg, Name=" + Name + " : InteractionList=null");
#endif
            }
        }
        
        // handle base message
        base.PutMessage(msg);
    }

    string lastText;
    public void Debug(string text)
    {
        if (text != lastText)
        {
            lastText = text;
            UnityEngine.Debug.Log(text);
        }
    }
}
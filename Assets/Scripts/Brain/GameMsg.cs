
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GameMsg
{
    public GameMsg()
    {
    }
}

public class StringMsg : GameMsg
{
    public StringMsg() : base() { }
    public string message;
    public StringMsg(string command)
    {
        message = command;
    }
}

public class InteractMsg : GameMsg
{
    public InteractionMap map;
    public string gameObject;
    public bool log;
	public bool scripted;

	public InteractMsg() : base()
	{}

    public InteractMsg(GameObject obj, InteractionMap item)
        : base()
    {
        string name = (obj != null) ? obj.name : "null";
        this.gameObject = name;
        this.map = item;
        this.log = true;
    }

    public InteractMsg(GameObject obj, InteractionMap item, bool log)
    {
        string name = (obj != null) ? obj.name : "null";
        this.gameObject = name;
        this.map = item;
        this.log = log;
    }

    public InteractMsg(GameObject obj, string item, bool log)
    {
        string name = (obj != null) ? obj.name : "null";
        this.gameObject = name;
        this.map = new InteractionMap(item, null, null, null, null, null, null, log);
        this.log = log;
    }

    public InteractMsg(GameObject obj, string item)
    {
        string name = (obj != null) ? obj.name : "null";
        this.gameObject = name;
        this.map = new InteractionMap(item, null, null, null, null, null, null, true);
        this.log = true;
    }
}

// msg sent around system when an interaction happens
public class InteractStatusMsg : GameMsg
{
    public string InteractName;
    public InteractionMap InteractMap;
	public List<string> Params;

    public InteractStatusMsg(string name)
    {
        // save name
        InteractName = name;
        // lookup interact
        InteractMap = InteractionMgr.GetInstance().Get(name);
		// create new param list
		Params = new List<string>();
    }

    public InteractStatusMsg(InteractMsg msg)
    {
        // save name
        InteractName = msg.map.item;
        // save msg
        InteractMap = msg.map;
		// create new param list
		Params = new List<string>();
    }
}

public class AnimateMsg : GameMsg
{
	public AnimateMsg(string objectName, eAnimateState state) : base()
	{
		this.name = objectName;
		this.state = state;
	}
	
	public eAnimateState state;
	public string name;
}

public class TaskMsg : GameMsg
{
	public TaskMsg(string objectName, Task task) : base()
	{
		this.name = objectName;
		this.task = task;
	}
	
	public Task task;
	public string name;
}

public class ChangeStateMsg : GameMsg
{
    public ChangeStateMsg(string state, params string[] args)
        : base()
    {
        this.state = state;
        this.args = args;
    }

    public ChangeStateMsg(string state)
    {
        this.state = state;
        this.args = null;
    }

    public string state;
    public string[] args;
}

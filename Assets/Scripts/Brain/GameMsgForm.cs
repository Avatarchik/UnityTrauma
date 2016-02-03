using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GameMsgForm : MonoBehaviour
{
	public enum eMsgType{ // which type of message to represent
		interactMsg,
		interactStatusMsg,
		animateMsg,
		taskMsg,
		dialogMsg,
		errorDialogMsg,
		interactDialogMsg,
		quickInfoDialogMsg,
		popupMsg,
		guiScreenMsg // just adds ScreenName to GameMsg
	}

	// this class contains all the parameters needed by all message types in an editor friendly container
	// make the desired message at runtime from it.
	// interact message
	public eMsgType msgType; // type of message to construct from this form
	public InteractionMapForm map; // this probably isnt going to work as is...
    public string gameObjectName = "";
    public bool log;
	// interact status message
	public string interactName = "";
	// animate message
	public eAnimateState state;
	public new string name = ""; // this HIDES unity object name
	// task message
	public Task task;
//	public string name;
	// change state message
//    public string state;
    public string[] args;
	// dialog message
	public DialogMsg.Cmd command;
    public int x;
    public int y;
    public int w;
    public int h;
    public string text = "";
    public string title = "";
    public float time;
    public bool modal;
	public string xmlName;
	public string className;
	public string dialogName;
	public string anchor = "centered";
	public List<string> arguments;
	public int numArguments;
	// dialog wait
	public bool waitForDialogClosed;
	public string waitForDialogName;
	//quickinfomsg
	public float timeout;
    public bool editbox;
    public string editboxlabel = "";
    public string editboxprompt = "";
	//AssessmentItemMsg
	public bool sendMap = true;
	public InteractionMap Map;
    public AssessmentList List;
    public AssessmentItem Item;
	//AssessmentListMsg
//	public AssessmentList List;
	//AssessmentScenarioMsg
	public ScenarioAssessment Scenario;
	//AssessmentMgrDialogMsg
	public ScenarioAssessmentReport Report;
//  public List<AssessmentItemMsg> List;
	//interact dialog
	public List<InteractionMap> items;
    public ObjectInteraction baseobj;
	public string baseXML = "";
	//MedAdministrationDialog
	public Provider provider;
    public Patient patient;
	//popupmsg
	public bool hasCancel;
    public string commandString = "";
	public string[] Params; // used for interact status message
	// GUIScreenMsg
	public string ScreenName;
#if UNITY_EDITOR
	SerializedObject serializedObject;
#endif
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	public class GameMsgFormInfo
	{
		public GameMsgFormInfo(){
		}
		
	public eMsgType msgType; // type of message to construct from this form
//	public InteractionMapForm map; // this probably isnt going to work as is...
    public string gameObjectName = "";
    public bool log;
	// interact status message
	public string interactName = "";

	// dialog message
	public DialogMsg.Cmd command;
    public int x;
    public int y;
    public int w;
    public int h;
    public string text = "";
    public string title = "";
    public float time;
    public bool modal;
	public string xmlName;
	public string className;
	public string dialogName; // for sending later position, anchor, close messages
	public string anchor; // relative,centered,upper_left,lower_left,upper_right,lower_right,upper_middle,lower_middle,left_middle,right_middle
	public List<string> arguments;
	public int numArguments;
	// dialog wait arguments
	public bool waitForDialogClosed=false; 
	public string waitForDialogName;
	//quickinfomsg
	public float timeout = 2.0f;
    public bool editbox;
    public string editboxlabel = "";
    public string editboxprompt = "";
	public bool sendMap = true;
	//interact dialog
	public List<InteractionMap> items;
//    public ObjectInteraction baseobj;
	public string baseXML = "";

	//popupmsg
	public bool hasCancel;
    public string commandString = "";
	public string[] Params; // used for interact status message
	// GUIScreenMsg
	public string ScreenName;
		
	}
		
	public GameMsgFormInfo ToInfo(GameMsgForm gmf){ // saves values to an info for serialization (to XML)
		GameMsgFormInfo info = new GameMsgFormInfo();
		
				
		info.msgType = gmf.msgType; // type of message to construct from this form

		info.gameObjectName = gmf.gameObjectName;
		info.log = gmf.log;
			// interact status message
		info.interactName = gmf.interactName;

			// dialog message
		info.command = gmf.command;
		info.x = gmf.x;
		info.y = gmf.y;
		info.w = gmf.w;
		info.h = gmf.h;
		info.text = gmf.text;
		info.title = gmf.title;
		info.time = gmf.time;
		info.modal = gmf.modal;
		info.xmlName = gmf.xmlName;
		info.className = gmf.className;
		info.dialogName = gmf.dialogName;
		info.waitForDialogClosed = gmf.waitForDialogClosed;
		info.waitForDialogName = gmf.waitForDialogName;
		info.anchor = gmf.anchor;
		info.arguments = gmf.arguments;
		info.numArguments = gmf.numArguments;
			//quickinfomsg
		info.timeout = gmf.timeout;
		info.editbox = gmf.editbox;
		info.editboxlabel = gmf.editboxlabel;
		info.editboxprompt = gmf.editboxprompt;
		info.sendMap = gmf.sendMap;
			//interact dialog
		info.items = gmf.items;
		//info.baseobj = gmf.baseobj;
		info.baseXML = gmf.baseXML;

			//popupmsg
		info.hasCancel = gmf.hasCancel;
		info.commandString = gmf.commandString;
		info.Params = gmf.Params; // used for interact status message	
			// GUIScreenMsg
		info.ScreenName = gmf.ScreenName;
	
		return info;
	}
	
	public void InitFrom(GameMsgFormInfo info){

		msgType = info.msgType; // type of message to construct from this form

		gameObjectName = info.gameObjectName;
		log = info.log;
			// interact status message
		interactName = info.interactName;

			// dialog message
		command = info.command;
		x = info.x;
		y = info.y;
		w = info.w;
		h = info.h;
		text = info.text;
		title = info.title;
		time = info.time;
		modal = info.modal;
		xmlName = info.xmlName;
		className = info.className;
		dialogName = info.dialogName;
		anchor = info.anchor;
		arguments = info.arguments;
		numArguments = info.numArguments;
		waitForDialogClosed = info.waitForDialogClosed;
		waitForDialogName = info.waitForDialogName;
			//quickinfomsg
		timeout = info.timeout;
		editbox = info.editbox;
		editboxlabel = info.editboxlabel;
		editboxprompt = info.editboxprompt;
		sendMap = info.sendMap;
			//interact dialog
		items = info.items;
		//info.baseobj = gmf.baseobj;
		baseXML = info.baseXML;

			//popupmsg
		hasCancel = info.hasCancel;
		commandString = info.commandString;
		Params = info.Params; // used for interact status message	
			// GUIScreenMsg
		ScreenName = info.ScreenName;

	}
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */	
	
	
	
	public void PutMessage( ScriptedAction scriptedAction ){
		// create a message of the appropriate type, and send it to the singleton, or	
		if (msgType == eMsgType.interactMsg){
			BaseObject bo = ObjectManager.GetInstance().GetBaseObject(gameObjectName);
			if (bo == null){
				Debug.LogWarning("GameMsgForm "+name+" could not send message to '"+gameObjectName+"', not known to ObjectManager.");
				return;
			}
			GameObject target = bo.gameObject;
			if (target != null){
				InteractMsg newMsg = new InteractMsg(target,map.GetMap());
				// add flag to let everyone know that this command as generated internally
				newMsg.scripted = true;
				//	newMsg.map.task = map.task; // Task master faults if this is null

				for(int i=0;i<newMsg.map.param.Count;i++)
				{
					if ( newMsg.map.param[i] != null && newMsg.map.param[i] != "" )
						newMsg.map.param[i]= scriptedAction.executedBy.ResolveArgs(newMsg.map.param[i]); // substitute any #values
				}
				// this is problematic, because BaseObject.PutMessage does NOTHING! TODO
				//target.GetComponent<BaseObject>().PutMessage(newMsg);
				ObjectManager.GetInstance ().GetBaseObject(gameObjectName).PutMessage(newMsg);
			}
		}
		if (msgType == eMsgType.interactStatusMsg){
			GameObject target = GameObject.Find (gameObjectName);
			if (target != null){
				InteractMsg newMsg;
				if (sendMap)
					newMsg = new InteractMsg(target,map.GetMap());
				else
					newMsg = new InteractMsg(target,interactName,log);
				// add flag to let everyone know that this command as generated internally
				newMsg.scripted = true;
				//newMsg.map.task = map.task; // Task master faults if this is null
				if (sendMap)
					for(int i=0;i<newMsg.map.param.Count;i++)
					{
						if ( newMsg.map.param[i] != null && newMsg.map.param[i] != "" )
							newMsg.map.param[i]= scriptedAction.executedBy.ResolveArgs(newMsg.map.param[i]); // substitute any #values
					}
				InteractStatusMsg newisMsg = new InteractStatusMsg(newMsg);
				if (Params != null && Params.Length > 0){
					newisMsg.Params=new List<string>();
					for(int i=0;i<Params.Length;i++)
					{
						if ( Params[i] != null && Params[i] != "" )
							newisMsg.Params.Add (scriptedAction.executedBy.ResolveArgs(Params[i])); // substitute any #values
					}
				}
				// send to all objects
			//	ObjectManager.GetInstance().PutMessage(newisMsg);  // the brain sends to the object manager
				// send to the brain
				Brain.GetInstance().PutMessage(newisMsg);
			}
		}
		if (msgType == eMsgType.animateMsg){
			
		}
		if (msgType == eMsgType.taskMsg){
			
		}
		if (msgType == eMsgType.errorDialogMsg){
			
		}
		if (msgType == eMsgType.interactDialogMsg){
			
		}
		if (msgType == eMsgType.quickInfoDialogMsg){
			QuickInfoMsg newMsg = new QuickInfoMsg();
		
			newMsg.x = x;
			newMsg.y = y;
			newMsg.w = w;
			newMsg.h = h;
			newMsg.text = text;
			newMsg.title = title;
			newMsg.time = time;
			// all the QuickInfo's had a timeout of 0 which was not getting passed, so if you see that, leave it alone
			// treat -1 as the value to leave the dialog up.
			if (timeout == 0) timeout = 2;
			if (timeout == -1) timeout = 0;
			newMsg.timeout = timeout;
			newMsg.modal = modal;
			newMsg.command = command;
			
			QuickInfoDialog.GetInstance().PutMessage( newMsg );
		}
		if (msgType == eMsgType.popupMsg){
			
		}
		if (msgType == eMsgType.dialogMsg){
			
			DialogMsg newMsg = new DialogMsg();

			newMsg.x = x;
			newMsg.y = y;
			newMsg.w = w;
			newMsg.h = h;
			newMsg.text = text;
			newMsg.title = title;
			newMsg.time = time;
			newMsg.modal = modal;
			newMsg.command = command;
			newMsg.className = className;
			newMsg.name = dialogName;
			newMsg.anchor = anchor;
			newMsg.xmlName = xmlName;
			newMsg.arguments = new List<string>();
			newMsg.callback += scriptedAction.DialogCallback;
			foreach( string arg in arguments )
			{
				if ( arg != null && arg != "" )
					newMsg.arguments.Add (StringLookup(scriptedAction.executedBy.ResolveArgs(arg))); // substitute any #values
			}
			// fire off the dialog
			GUIManager.GetInstance().PutMessage( newMsg );
		}	
		if (msgType == eMsgType.guiScreenMsg){
			
			GUIScreenMsg newMsg = new GUIScreenMsg();
		
			newMsg.ScreenName = ScreenName;

			foreach( string arg in arguments )
			{
				if ( arg != null && arg != "" )
					newMsg.arguments.Add (StringLookup(scriptedAction.executedBy.ResolveArgs(arg))); // substitute any #values
			}
			// fire off the dialog
			GUIManager.GetInstance().PutMessage( newMsg );
		}		
	}

	public string StringLookup(string argstring){
		string result = "";
		if (argstring.Contains("$")){
			string[] p = argstring.Split('$');
			result = p[0];
			// in each piece, pass anything up to a delimiting blank thru the string table
			for (int i=1; i<p.Length; i++){
				string toLookup = "";
				string tailString = "";
				// look for delimiting blank, if none then lookup whole string
				if (p[i].Contains(" ")){
					toLookup = p[i].Substring(0,p[i].IndexOf(' ')-1);
					tailString = p[i].Substring(p[i].IndexOf(' '));
				}
				else
				{
					toLookup = p[1];
				}
				result += "\""+StringMgr.GetInstance().Get (toLookup)+"\"" + tailString;
			}
		}
		else result = argstring;
		return result;	
	}
	
	public GameMsg ToGameMsg( ScriptedAction scriptedAction ){
		// create a message of the appropriate type, and send it to the singleton, or	
		if (msgType == eMsgType.interactMsg){
			GameObject target = GameObject.Find (gameObjectName); // cant use ObjectManager from the editor
			if (target != null){
				InteractMsg newMsg = new InteractMsg(target,map.GetMap());
				//	newMsg.map.task = map.task; // Task master faults if this is null

				for(int i=0;i<newMsg.map.param.Count;i++)
				{
					if ( newMsg.map.param[i] != null && newMsg.map.param[i] != "" )
						newMsg.map.param[i]= scriptedAction.executedBy.ResolveArgs(newMsg.map.param[i]); // substitute any #values
				}
				// this is problematic, because BaseObject.PutMessage does NOTHING! TODO
				//target.GetComponent<BaseObject>().PutMessage(newMsg);
				return newMsg as GameMsg;
			}
		}
		if (msgType == eMsgType.interactStatusMsg){
			GameObject target = GameObject.Find (gameObjectName);
			if (target != null){
				InteractMsg newMsg;
				if (sendMap)
					newMsg = new InteractMsg(target,map.GetMap());
				else
					newMsg = new InteractMsg(target,interactName,log);
				//newMsg.map.task = map.task; // Task master faults if this is null
				if (sendMap)
					for(int i=0;i<newMsg.map.param.Count;i++)
					{
						if ( newMsg.map.param[i] != null && newMsg.map.param[i] != "" )
							newMsg.map.param[i]= scriptedAction.executedBy.ResolveArgs(newMsg.map.param[i]); // substitute any #values
					}
				InteractStatusMsg newisMsg = new InteractStatusMsg(newMsg);
				if (Params.Length > 0){
					newisMsg.Params=new List<string>();
					for(int i=0;i<Params.Length;i++)
					{
						if ( Params[i] != null && Params[i] != "" )
							newisMsg.Params.Add (scriptedAction.executedBy.ResolveArgs(Params[i])); // substitute any #values
					}
				}
				return newisMsg as GameMsg;
			}
		}
		if (msgType == eMsgType.quickInfoDialogMsg){
			DialogMsg newMsg = new QuickInfoMsg();
		
			newMsg.x = x;
			newMsg.y = y;
			newMsg.w = w;
			newMsg.h = h;
			newMsg.text = text;
			newMsg.title = title;
			newMsg.time = time;
			newMsg.modal = modal;
			newMsg.command = command;
			
			return newMsg as GameMsg;
		}
		if (msgType == eMsgType.dialogMsg){
			
			DialogMsg newMsg = new DialogMsg();
		
			newMsg.x = x;
			newMsg.y = y;
			newMsg.w = w;
			newMsg.h = h;
			newMsg.text = text;
			newMsg.title = title;
			newMsg.time = time;
			newMsg.modal = modal;
			newMsg.command = command;
			newMsg.className = className;
			newMsg.xmlName = xmlName;
			newMsg.arguments = new List<string>();
			newMsg.callback += scriptedAction.DialogCallback;
			foreach( string arg in arguments )
			{
				if ( arg != null && arg != "" )
					newMsg.arguments.Add (scriptedAction.executedBy.ResolveArgs(arg)); // substitute any #values
			}
			// fire off the dialog
			return newMsg as GameMsg;
		}	
		if (msgType == eMsgType.guiScreenMsg){
			
			GUIScreenMsg newMsg = new GUIScreenMsg();
		
			newMsg.ScreenName = ScreenName;

			foreach( string arg in arguments )
			{
				if ( arg != null && arg != "" )
					newMsg.arguments.Add (scriptedAction.executedBy.ResolveArgs(arg)); // substitute any #values
			}
			// fire off the dialog
			return newMsg as GameMsg;
		}
		return null;
	}	
	
	
	
	
#if UNITY_EDITOR
	public bool ShowInspectorGUI(){
		// this is called from the ScriptedAction custom inspector, and only shows the fields needed for the selected type
		bool dirty = false;
		if (serializedObject == null) serializedObject = new SerializedObject(this); // needed to display arrays in editor like fashion
		msgType = (eMsgType)EditorGUILayout.EnumPopup("msgType",(Enum)msgType);
		EditorGUI.BeginChangeCheck();
		if (msgType == eMsgType.interactMsg){
			if (gameObjectName == null) gameObjectName = "";
			gameObjectName = EditorGUILayout.TextField("gameObject",gameObjectName);
			if (map != null) dirty |= map.ShowInspectorGUI("map");		
			log = EditorGUILayout.Toggle("log",log);
		}
		if (msgType == eMsgType.interactStatusMsg){
			if (gameObjectName == null) gameObjectName = "";
			gameObjectName = EditorGUILayout.TextField("gameObject",gameObjectName);
			sendMap = EditorGUILayout.Toggle("send Map?",sendMap);
			if (sendMap && map != null) 
				dirty |= map.ShowInspectorGUI("map");		
			else
				interactName = EditorGUILayout.TextField("interactName",interactName);
			log = EditorGUILayout.Toggle("log",log);
			
			// to show a string array in the editor:
			serializedObject.Update();
    		EditorGUIUtility.LookLikeInspector();
    		SerializedProperty myIterator = serializedObject.FindProperty("Params");
   			while (true){
        	Rect myRect = GUILayoutUtility.GetRect(0f, 16f);
        	bool showChildren = EditorGUI.PropertyField(myRect, myIterator);
			if (!myIterator.NextVisible(showChildren)) break;
			}
    		serializedObject.ApplyModifiedProperties()	;
			EditorGUIUtility.LookLikeControls();
			
		}
		if (msgType == eMsgType.animateMsg){
			state = (eAnimateState)EditorGUILayout.EnumPopup("state",(Enum)state);
			if (name == null) name = "";
			name = EditorGUILayout.TextField("name",name);
		}
		if (msgType == eMsgType.taskMsg){
			
		}
		if (msgType == eMsgType.dialogMsg 
			|| msgType == eMsgType.errorDialogMsg 
			|| msgType == eMsgType.quickInfoDialogMsg
			|| msgType == eMsgType.popupMsg )
		{
			command = (DialogMsg.Cmd)EditorGUILayout.EnumPopup("command",(Enum)command);
			x = EditorGUILayout.IntField("x",x);
			y = EditorGUILayout.IntField("y",y);
			w = EditorGUILayout.IntField("w",w);
			h = EditorGUILayout.IntField("h",h);
			text = EditorGUILayout.TextField("text",text);	
			title = EditorGUILayout.TextField("title",title);
			time = EditorGUILayout.FloatField("time",time);
			modal = EditorGUILayout.Toggle("modal",modal);
		}
		if (msgType == eMsgType.dialogMsg) 
		{
			if (xmlName == null) xmlName = "";
			xmlName = EditorGUILayout.TextField ("xmlName",xmlName);
			if (className == null) className = "";
			className = EditorGUILayout.TextField("className",className);
			dialogName = EditorGUILayout.TextField("dialogName",dialogName);
			waitForDialogClosed = EditorGUILayout.Toggle("waitForDialogClosed",waitForDialogClosed);
			if ( waitForDialogClosed == true )
				waitForDialogName = EditorGUILayout.TextField("waitForDalogName",waitForDialogName);
			anchor = EditorGUILayout.TextField("anchor",anchor);
			numArguments = EditorGUILayout.IntField("numArguments",numArguments);
			// display number of arguments...change number of fields on change
			if (arguments.Count != numArguments )
			{
				List<string> newlist = new List<string>();
				if ( numArguments > arguments.Count )
				{
					// new is greater than current...just copy old to new
					foreach( string arg in arguments )
						newlist.Add(arg);
					// add blank(s) at the end
					for( int i=0 ; i<(numArguments-arguments.Count) ; i++)
						newlist.Add("");
				}
				else
				{
					// new is less than current...just copy old-1 to new
					foreach( string arg in arguments )
					{
						if ( newlist.Count < numArguments )
							newlist.Add(arg);
					}
				}
				// reassign
				arguments = newlist;
			}
			for (int i=0 ; i<arguments.Count ; i++)
			{
				arguments[i] = EditorGUILayout.TextField ("argument" + (i+1).ToString (),arguments[i]);
			}
		}
		if (msgType == eMsgType.guiScreenMsg) 
		{
			if (ScreenName == null) ScreenName = "";
			ScreenName = EditorGUILayout.TextField ("xmlName",ScreenName);
			numArguments = EditorGUILayout.IntField("numArguments",numArguments);
			// display number of arguments...change number of fields on change
			if (arguments.Count != numArguments )
			{
				List<string> newlist = new List<string>();
				if ( numArguments > arguments.Count )
				{
					// new is greater than current...just copy old to new
					foreach( string arg in arguments )
						newlist.Add(arg);
					// add blank(s) at the end
					for( int i=0 ; i<(numArguments-arguments.Count) ; i++)
						newlist.Add("");
				}
				else
				{
					// new is less than current...just copy old-1 to new
					foreach( string arg in arguments )
					{
						if ( newlist.Count < numArguments )
							newlist.Add(arg);
					}
				}
				// reassign
				arguments = newlist;
			}
			for (int i=0 ; i<arguments.Count ; i++)
			{
				arguments[i] = EditorGUILayout.TextField ("argument" + (i+1).ToString (),arguments[i]);
			}
		}
		
		
		
		if (msgType == eMsgType.errorDialogMsg){
			
		}
		if (msgType == eMsgType.interactDialogMsg){
			
		}
		if (msgType == eMsgType.quickInfoDialogMsg){
			timeout = EditorGUILayout.FloatField("timeout",timeout);
			editbox = EditorGUILayout.Toggle("editbox",editbox);
			if (editboxlabel == null) editboxlabel = "";
			editboxlabel = EditorGUILayout.TextField("editboxlabel",editboxlabel);
			if (editboxprompt == null) editboxprompt = "";
			editboxprompt = EditorGUILayout.TextField("editboxprompt",editboxprompt);
		}
		if (msgType == eMsgType.popupMsg){
			hasCancel = EditorGUILayout.Toggle("hasCancel",hasCancel);
			if (commandString == null) commandString = "";
			commandString = EditorGUILayout.TextField("commandString",commandString);
		}

		dirty |= EditorGUI.EndChangeCheck();
		if (dirty) EditorUtility.SetDirty(this);
		return dirty;
	}
#endif
}
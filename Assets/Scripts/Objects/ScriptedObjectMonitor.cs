#define USE_WIDTH

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScriptedObjectMonitor : MonoBehaviour 
{
	float nameWidth=100;
	float animWidth=100;
	float animStateWidth=100;
	float scriptWidth=100;
	float targetNodeWidth=100;
	float navWidth=100;

	public bool showAnim=true;
	public bool showScript=true;
	public bool showTarget=true;
	public bool showNav=true;

	public bool PauseOnError=true;

	void Awake()
	{
		instance = this;
	}

	public void UseWindowWidth( float width )
	{
		float count=1;
		if ( showAnim )
			count += 2;
		if ( showScript )
			count += 1;
		if ( showTarget )
			count += 2;
		if ( showNav )
			count += 1;

		width /= count;
		nameWidth = width;
		animWidth = width;
		animStateWidth = width;
		scriptWidth = width;
		targetNodeWidth = width;
		navWidth = width;
	}

	static ScriptedObjectMonitor instance;
	public static ScriptedObjectMonitor GetInstance()
	{
		return instance;
	}

	public class MonitorInfo
	{	
		public ScriptedObjectMonitor sm;

		public GameObject go;
		public ScriptedObject so;
		public AnimationManager am;
		public NavMeshAgentWrapper nm;
		public TaskCharacter tc;
		public bool race;
		public bool noscript;
		public string name;

		public void Display()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label (go.name, GUILayout.Width (sm.nameWidth));
			if ( sm.showAnim == true )
			{
				if ( am != null )
				{
#if USE_WIDTH
					GUILayout.Label (am.CurrentAnim,GUILayout.Width (sm.animWidth));
					GUILayout.Label (am.AnimState.ToString (), GUILayout.Width (sm.animStateWidth));
#else
					GUILayout.Label (am.CurrentAnim);
					GUILayout.Label (am.AnimState.ToString ());
#endif
				}
				else
				{
#if USE_WIDTH
					GUILayout.Label ("no animation",GUILayout.Width (sm.animWidth));
					GUILayout.Label ("no animstate",GUILayout.Width (sm.animStateWidth));
#else
					GUILayout.Label ("no animation");
					GUILayout.Label ("no animstate");
#endif
				}
			}
			if ( sm.showScript == true )
			{
				if ( so != null )
				{
					if ( so.GetCurrentScript() != null )
					{
						noscript = false;
						string stackReport="";
						if (so.scriptStack.Count > 1)
							stackReport += "[S"+(so.scriptStack.Count-1).ToString()+"]";
						if (so.scriptArray.Count > 1)
							stackReport += "[Q"+(so.scriptArray.Count-1).ToString()+"]";

#if USE_WIDTH
						GUILayout.Label (so.GetCurrentScript().script.name+stackReport,GUILayout.Width (sm.scriptWidth));
#else
						GUILayout.Label (so.GetCurrentScript().script.name+stackReport);
#endif
					}
					else
					{
						noscript = true;
						string scriptText = "no script";
						string stackReport="";
						Color oldColor = GUI.color;
						if (so.ObjectInteraction != null){
							if (so.ObjectInteraction.actingInScript != null){
								scriptText = "[A]"+so.ObjectInteraction.actingInScript.name;
								GUI.color = Color.green;
							}
							else
							{
								if (so.ObjectInteraction.reservedForScript != null){
									scriptText = "[R]"+so.ObjectInteraction.reservedForScript.name;
									GUI.color = Color.yellow;
								}
							}
							if (so.scriptArray.Count > 0)
								stackReport += "[Q"+(so.scriptArray.Count).ToString()+"]";
						}

						
#if USE_WIDTH
							GUILayout.Label (scriptText+stackReport,GUILayout.Width (sm.scriptWidth));
#else
							GUILayout.Label (scriptText+stackReport);
#endif
							GUI.color = oldColor;
					}
				}
			}
			if ( sm.showTarget == true )
			{
				if ( tc != null )
				{
					Color color = GUI.color;
					if ( race == true )
						GUI.color = Color.red;

#if USE_WIDTH
					GUILayout.Label (tc.atNodeName,GUILayout.Width (sm.targetNodeWidth));
					GUILayout.Label (tc.targetNodeName,GUILayout.Width (sm.targetNodeWidth));
#else
					GUILayout.Label (tc.atNodeName );
					GUILayout.Label (tc.targetNodeName );
#endif

					if ( race == true )
						GUI.color = color;
				}
			}
			if ( sm.showNav == true )
			{
				if ( nm != null )
				{
					string status="";
					if ( nm.isNavigating == true )
						status += "isNav ";
					if ( nm.isAvoiding == true )
						status += "isAvoid ";
					if ( nm.holdPosition == true )
						status += "hold ";
					GUILayout.Label (status,GUILayout.Width (sm.navWidth));
				}
			}
			GUILayout.EndHorizontal();			
		}
	}



	TaskMaster tm;

	public List<MonitorInfo> Info;

	// Use this for initialization
	void Start () {
		tm = GameObject.Find ("Brain").GetComponent<TaskMaster>() as TaskMaster;
	}

	public void Init()
	{
		ScriptedObject[] tmp = GameObject.Find ("_Characters").GetComponentsInChildren<ScriptedObject>();

		Info = new List<MonitorInfo>();
		foreach( ScriptedObject item in tmp )
		{
			MonitorInfo info = new MonitorInfo();
			info.so = item;
			info.go = item.gameObject;
			info.name = info.go.name;
			info.am = info.go.GetComponent<AnimationManager>();
			info.nm = info.go.GetComponent<NavMeshAgentWrapper>();
			info.tc = info.go.GetComponent<TaskCharacter>();
			info.sm = this;
			if ( info.name != "Player")
				Info.Add(info);
		}
	}

	public void CheckNodeRaceCondition()
	{
		foreach( MonitorInfo item1 in Info )
		{
			foreach( MonitorInfo item2 in Info )
			{
				if ( item1 != item2 )
				{
					TaskCharacter char1AT = tm.GetLockingObject(item1.tc.atNodeName);
					TaskCharacter char1TG = tm.GetLockingObject(item1.tc.targetNodeName);
					TaskCharacter char2AT = tm.GetLockingObject(item2.tc.atNodeName);
					TaskCharacter char2TG = tm.GetLockingObject(item2.tc.targetNodeName);

					if ( char1AT == char2TG || char2AT == char1TG )
					{
						item1.race = true;
						item2.race = true;
						FlagError("Race Condition Detected!!");
					}
				}
			}
		}
	}

	// this initializes everything.  wait until the patient is available, then 
	// the system is ready.
	Patient patient=null;
	void CheckInit()
	{
		if ( patient != null )
			return;

		if ( patient == null )
			patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;

		if ( patient != null )
			Init ();
	}

	float checkTime;
	public float checkEmptyTime=1.0f;

	public bool DoDummyCommands=false;
	public List<string> DummyCommands;
	int currentDummy=0;

	public void CheckNoScripts()
	{
		if ( Info == null || DoDummyCommands == false )
			return;

		if ( Time.time < checkTime )
			return;

		checkTime = Time.time + checkEmptyTime;

		bool noscript=true;
		foreach( MonitorInfo item in Info )
		{
			if ( item.noscript == false )
				noscript = false;
		}
		if ( noscript == true && DummyCommands.Count > 0)
		{
			// make a command
			Dispatcher.GetInstance().ExecuteCommand(DummyCommands[currentDummy]);
			UnityEngine.Debug.Log ("ScriptedObjectMonitor.CheckNoScripts() : start : " + DummyCommands[currentDummy]);

			if ( ++currentDummy >= DummyCommands.Count )
				currentDummy = 0;

		}
	}

	string error="no error";
	public void FlagError( string error )
	{
		this.error = error;
		UnityEngine.Debug.LogError ("ScriptedObjectMonitor.FlagError(" + error + ")");
		Pause ();
	}

	public void Pause()
	{
		if ( PauseOnError == true )
			Time.timeScale = 0.0f;
	}

	public void Display()
	{
		if ( Info == null )
			return;

		UseWindowWidth(Screen.width);

		GUILayout.Button ("Avatar Status");
		GUI.color = Color.green;
		GUILayout.BeginHorizontal();
		GUILayout.Label ("AVATAR",GUILayout.Width (nameWidth));
		if ( showAnim )
		{
			GUILayout.Label ("ANIM",GUILayout.Width (animWidth));
			GUILayout.Label ("ANIMSTATE",GUILayout.Width (animStateWidth));
		}
		if ( showScript )
			GUILayout.Label ("INTERACTION",GUILayout.Width (scriptWidth));
		if ( showTarget )
		{
			GUILayout.Label ("AT NODE",GUILayout.Width (targetNodeWidth));
			GUILayout.Label ("TARGET",GUILayout.Width (targetNodeWidth));
		}
		if ( showNav )
			GUILayout.Label ("NAV STATUS",GUILayout.Width (navWidth));
		GUILayout.EndHorizontal();
		GUI.color = Color.white;

		foreach( MonitorInfo item in Info )
			item.Display();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button ("Pause") )
			Time.timeScale = 0.0f;
		if ( GUILayout.Button ("0.5x") )
			Time.timeScale = 0.5f;
		if ( GUILayout.Button ("1x") )
			Time.timeScale = 1.0f;
		if ( GUILayout.Button ("2x") )
			Time.timeScale = 2.0f;
		if ( GUILayout.Button ("5x") )
			Time.timeScale = 5.0f;
		GUILayout.EndHorizontal();

		GUILayout.Space (5);
		GUILayout.Label ("Error<" + error + ">");
	}
	
	// Update is called once per frame
	void Update () {
		CheckInit();
		CheckNoScripts();
	}
}

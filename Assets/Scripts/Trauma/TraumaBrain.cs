using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

public class TraumaStates : BrainStates
{
    BrainState _beginscenario;
    BrainState _endscenario;
	BrainState _assessment;

    public TraumaStates( TraumaBrain brain )
    {
        _beginscenario = new BeginScenario();
        _endscenario = new EndScenario();
		_assessment = new Assessment();
    }

    public BrainState BeginScenario
    {
        get { return _beginscenario; }
    }

    public BrainState EndScenario
    {
        get { return _endscenario; }
    }
	
	public BrainState Assessment
	{
		get { return _assessment; }
	}
}

public class TraumaState : BrainState
{
    public TraumaState()
        : base()
    {
        Hints = new List<string>();
        currHint = 0;
    }

    protected List<string> Hints;
    protected int currHint;
    protected string hintString;

    protected string GetHint()
    {
        string hint;

        if (Hints.Count == 0)
            return "No hints right now";

        hint = Hints[currHint];

        return hint;
    }

    protected void IncHint()
    {
        if (++currHint >= Hints.Count)
            currHint = 0;
    }

    protected string GetHintTitle()
    {
        string hint;

        if (Hints.Count == 0)
            return "Hint";

        hint = "Hint #" + (currHint + 1);
        return hint;
    }

    protected void Finish()
    {
        // tell LMS we're finished (put in proper syntax later)
        Application.ExternalCall("finishLMS");
        Application.LoadLevel("finish");
    }
}

public class EndScenario : TraumaState
{
    public EndScenario() { }

    float time = 0.0f;

    public override void Update(float elapsedTime)
    {
        base.Update(elapsedTime);
    }

    public override void Init()
    {
        base.Init();
	
		UnityEngine.Debug.LogError("TraumaState : EndScenario");
		
		// pause the game
//		Time.timeScale = 0.0f;  // let the scripts complete


		Patient patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		patient.EndScenario = true; // set a flag that can be read by various systems that the scenario is over
		// primarily used by the CT scripts to know the patient has died during a scan...

		// close dialog
		GUIManager.GetInstance().CloseDialogs();
		
		// load plan of care
		DialogMsg dmsg = new DialogMsg();
		dmsg.className = "GUIScreen";
		dmsg.xmlName = "traumaPlanOfCare"; //changed from PlanOfCare 4/28/15 PAA
		dmsg.modal = true;
//		GUIManager.GetInstance().LoadDialog(dmsg); // dont show plan of care
    }

    public override void OnGUI()
    {
    }

    public override void PutMessage(GameMsg msg)
    {
        UnityEngine.Debug.Log("EndScenario : PutMessage");
    }
}

public class Assessment : TraumaState
{
    public Assessment() { }

    float time = 0.0f;

    public override void Update(float elapsedTime)
    {
        base.Update(elapsedTime);
    }

    public override void Init()
    {
        base.Init();
	
		UnityEngine.Debug.LogError("TraumaState : Assessment");
		
		// pause the game
		Time.timeScale = 0.0f;

		// close dialog
		GUIManager.GetInstance().CloseDialogs();		
		
		// generate a report and save to the database
		TraumaReportMgr.GetInstance().CreateReport().SaveDatabase();
		
		// load plan of care
		DialogMsg dmsg = new DialogMsg();
		dmsg.className = "CaseOverview";
		dmsg.xmlName = "traumaCaseOverviews";
		dmsg.modal = true;
		GUIManager.GetInstance().LoadDialog(dmsg);
    }

    public override void OnGUI()
    {
    }

    public override void PutMessage(GameMsg msg)
    {
        UnityEngine.Debug.Log("Assessment : PutMessage");
    }
}

public class BeginScenario : TraumaState
{
    public BeginScenario() 
    {
    }

    public override void Update(float elapsedTime)
    {
        base.Update(elapsedTime);

        // only runs once everything is started
        DecisionMgr.GetInstance().Update();
    }

    public override void Init()
    {
        base.Init();

		// start key handler
		TraumaBrain.GetInstance().gameObject.AddComponent<TraumaKeyHandler>();
		// clear the logs
		LogMgr.GetInstance().ClearLogs();
		LogMgr.GetInstance().CreateLog("interactlog");
		LogMgr.GetInstance().SetCurrent("interactlog");
	}

    public override void PutMessage(GameMsg msg)
    {
        AssessmentScenarioMsg asmsg = msg as AssessmentScenarioMsg;
        if (asmsg != null)
        {
            QuickInfoMsg qimsg = new QuickInfoMsg();
            qimsg.text = asmsg.PrettyPrint();
            QuickInfoDialog.GetInstance().PutMessage(qimsg);
        }
        AssessmentListMsg almsg = msg as AssessmentListMsg;
        if (almsg != null)
        {
#if DEBUG_ASSESSMENT_LIST
            QuickInfoMsg qimsg = new QuickInfoMsg();
            qimsg.text = almsg.PrettyPrint();
            QuickInfoDialog.GetInstance().PutMessage(qimsg);
#endif
        }
        AssessmentItemMsg aimsg = msg as AssessmentItemMsg;
        if (aimsg != null)
        {
            if (aimsg.Item.Fatal == true)
            {
                QuickInfoMsg qimsg = new QuickInfoMsg();
                qimsg.title = "FATAL";
                qimsg.text = aimsg.PrettyPrint();
                QuickInfoDialog.GetInstance().PutMessage(qimsg);
            }
        }
    }
}

public class TraumaBrain : Brain
{
    Log logger;

    float worldTime = 0;


    public TraumaBrain()
        : base()
    {
        instance = this;
    }
	
	public void OnDestroy()
	{
		// init things on cleanup for next run
		ObjectManager.GetInstance().Init();
	}

    public void Awake()
    {
        UnityEngine.Debug.Log("TraumaBrain.Awake()");

		audioSource = Camera.mainCamera.audio;		
		characterAudioSources = new Dictionary<string,AudioSource>();
		foreach (UnityEngine.Object tc in FindObjectsOfType(typeof (TaskCharacter))){
			if (((TaskCharacter)tc).gameObject.audio != null)
				characterAudioSources[tc.name]=((TaskCharacter)tc).gameObject.audio;
		}

		// get all ambient sounds
		GetAmbientSounds();

        // init all brain states
        brainStates = new TraumaStates(this);

        // init vitals mgr
        VitalsMgr.GetInstance().Init();

		// init voice mgr
		VoiceMgr.GetInstance().Init ();

        // init decision engine
        DecisionMgr.GetInstance().Init();

        // load interactions
        InteractionMgr.GetInstance().LoadXML("XML/interactions/interactions");

        // load tasks
		TaskMaster.GetInstance().LoadXML("XML/Tasks");

        // open AssessmentMgr
		// loaded now in content linker
        //AssessmentMgr.GetInstance().LoadXML("XML/assessment");
		
        // set initial state message
        PutMessage(new ChangeStateMsg("BeginScenario"));
    }

	// this was the old DB Error checker
	void CheckDBError( bool status, string data, string error_msg, WWW download)
	{
		if ( status == false )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaErrorPopup";
			msg.className = "TraumaError";
			msg.modal = true;
			msg.arguments.Add("DB ERROR");
			msg.arguments.Add(error_msg);
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}
	
    public override void Start()
    {
        base.Start();
			
		// set GUIManager Fade off
		GUIManager.GetInstance().Fade = false;
		GUIManager.GetInstance().SetFadeCurtain(1.0f,0.0f,1.0f);
		
#if FORCE_START
		// instantiate objects
		ActivateGameObjects();
#endif

		UnityEngine.Debug.Log("TraumaBrain.Start() : Username <" + LoginMgr.GetInstance().Username + ">");
	}
	
	float ActivateObject( string name, object[] list )
	{
  		foreach (object o in list)
  		{
			GameObject go = o as GameObject;
			
	        Stopwatch stopWatch = new Stopwatch();
	        stopWatch.Start();
			
			if ( go && go.name == name && go.active == false )
			{
				go.active = true;
				go.SetActiveRecursively(true);
				stopWatch.Stop();
				UnityEngine.Debug.LogError("Activating inactive object <" + go.name + "> : time=" + stopWatch.Elapsed.Milliseconds);
				return stopWatch.Elapsed.Milliseconds;
			}
		}
		return 0.0f;				
	}
	
	void ActivateGameObjects()
	{
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
 		object[] list = Resources.FindObjectsOfTypeAll(typeof(GameObject));
		stopWatch.Stop();
		UnityEngine.Debug.Log("Find time = " + stopWatch.Elapsed.Milliseconds/1000.0f);
		
		float seconds = 0.0f;
		seconds += ActivateObject("_Cameras",list);
		seconds += ActivateObject("_Characters",list);
		seconds += ActivateObject("_Environment",list);
		seconds += ActivateObject("_Equipment",list);
		seconds += ActivateObject("_PathNodes",list);
		seconds += ActivateObject("AnimationScripts",list);
		seconds += ActivateObject("Dispatcher",list);
		seconds += ActivateObject("Pharmacy",list);
		seconds += ActivateObject("Trauma_05GameStateGroup",list);
		seconds += ActivateObject("Trauma_05Scripts",list);
		seconds += ActivateObject("TraumaLights",list);
		seconds += ActivateObject("VitalGraphers",list);
		seconds += ActivateObject("GUIManager",list);
		seconds += ActivateObject("CaseConfiguration",list);

		UnityEngine.Debug.Log("Loadtime=" + seconds/1000.0f);
	}

    public override void Update()
    {
        base.Update();

        worldTime += Time.deltaTime;

        if (UnityEngine.Debug.isDebugBuild)
        {
			
        }
    }

    public void InitTurn()
    {
        // set current turn
        
    }

	public void StartCase(string caseName){

		MenuLoader loader = FindObjectOfType<MenuLoader> ();
		if (loader == null)
			loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
		if ( loader != null ){
			if (caseName.ToLower() == "mainmenu")
				loader.GotoMain();
			else
			{
				if (caseName.ToLower() == "caseselection"){
					TraumaStartScreen.GetInstance().StartCase = "Tutorial Case"; // will this have any effect ? if so pass in special argument to set it
					loader.GotoCase();
				}
				else
					loader.StartCase (CaseConfiguratorMgr.GetInstance().Data.missionStartXml, caseName);  // first arg is GUI xml, second is case name
			}
		}
	}

	public override void QueueAudioGap( float time, string charName=null )
	{
		base.QueueAudioGap( time, charName );

		SAPISpeechManager.StopListeningFor( time ); //TODO this should only happen when the gap is played, not when it is queued...
	}

	public override void PlayAudio(string name) // TODO need to queue these if !VoiceManager.CanSpeak()
	{
		if (name == null) {
			UnityEngine.Debug.LogWarning("null string passed to Brain.PlayAudio");
			return;
		}
		AudioClip clip = SoundMgr.GetInstance().Get(name);
		if (clip != null) {
			SAPISpeechManager.StopListeningFor (clip.length);
		}
		base.PlayAudio (name);
	}

	public override void PlayAudio(AudioClip clip)// TODO need to queue these if !VoiceManager.CanSpeak()
	{
		if (clip != null)
		{
			SAPISpeechManager.StopListeningFor(clip.length);
		}
		base.PlayAudio (clip);
	}

	public override void PlayAudio(AudioInfo ai)// TODO need to queue these if !VoiceManager.CanSpeak()
	{
		if (ai != null && ai.clip != null)
		{
				SAPISpeechManager.StopListeningFor(ai.clip.length);
		}
		base.PlayAudio (ai);
	}

	public override void PlayVocals(string name)
	{
		if (name == null)
			return;
		
		AudioClip clip = SoundMgr.GetInstance().Get(name);
		if (clip != null )
		{
			SAPISpeechManager.StopListeningFor(clip.length);
		}
		base.PlayVocals (name);
	}

	public override bool PlayTTS(string text, string character){

#if !UNITY_STANDALONE_WIN
		return false;
#endif
		if (SAPISpeechManager.Instance == null)
						return false;

		int pitch = 0;
		int speed = 0;
		if (character == "ProcedureResident")
			pitch -=5;
		if (character == "PrimaryNurse") {
			pitch += 10;
			speed += 1; // -10 ~ +10
				}
		if (character == "RespiratoryTech") // need to handle gender ?
			pitch +=10;

		string pitchString = "<pitch middle = '"+pitch.ToString()+"'/>";
		string rateString = "<rate speed = '"+speed.ToString ()+"'/>";
		string volumeString = "<volume level = '100'/>";
		// stop listening // TODO
		// play the tts, no queuing at this point
		SAPISpeechManager.Speak (volumeString+pitchString+rateString+text); //<pitch middle = '-10'/>
		// could put stop listening in the thread that is speaking...
		// resume listening // TODO
		return true;
		}

	public override void ResumeAudioQueue(){
		
		CancelInvoke ("ResumeAudioQueue");
		AudioQueuePaused = false;
		if (Camera.mainCamera != null) {
			AudioSource cameraAudio = audioSource;
			if (CurrentAudioInfo != null && CurrentAudioInfo.source != null) cameraAudio = CurrentAudioInfo.source;
			if (cameraAudio != null) {
				cameraAudio.Play ();
				VoiceMgr.ResumeSpeaking();
				
				AudioQueuePaused = false;
				if (AudioQueuePauseTime != 0)
					AudioQueueTime += (Time.time - AudioQueuePauseTime);
				AudioQueuePauseTime = 0;
				// If there was something playing, remind the SAPI not to listen until that queue empties
				if (AudioQueueTime > Time.time)
					SAPISpeechManager.StopListeningFor(AudioQueueTime - Time.time);
			}
		}
	}

	AudioSource[] ambientSounds=null;
	public void GetAmbientSounds()
	{
		GameObject GO = GameObject.Find ("Trauma05_AmbientSounds");
		if ( GO != null )
			ambientSounds = GO.GetComponentsInChildren<AudioSource>();
	}

	public void StartAmbientSounds()
	{
		foreach( AudioSource sound in ambientSounds )
			sound.Play ();
	}

	public void StopAmbientSounds()
	{
		foreach( AudioSource sound in ambientSounds )
			sound.Stop ();
	}

    public void WriteLog(string filename)
    {
        logger.WriteXML(filename);
    }

    public bool DebugAssessment = false;

    public override void PutMessage(GameMsg msg)
    {
		if (GUIManager.GetInstance() != null)
			GUIManager.GetInstance().Fade = false; // this doesn't belong here at all. but it needs to happen.

		InteractStatusMsg ismsg = msg as InteractStatusMsg;
		if (ismsg != null)
		{
			if (ismsg.InteractName == "GO:TO:ASSESSMENT")
			{
				MenuLoader loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
				if ( loader != null )
					loader.GotoAssessment();
			}
		}

        base.PutMessage(msg);

		// let assessment manager chew on this msg
		AssessmentMgr.GetInstance().PutMessage(msg);
		
//		InteractionMgr.GetInstance().EvaluateInteractionSet(msg);
		
#if DEBUG_ASSESSMENT_ITEM
        AssessmentItemMsg aimsg = msg as AssessmentItemMsg;
        if (aimsg != null)
        {
            // do a quickinfo dialog for the 1st list item
            QuickInfoMsg qimsg = new QuickInfoMsg();
            qimsg.timeout = 3.0f;
            qimsg.title = "Assessment Item";
            qimsg.text = aimsg.PrettyPrint();
            QuickInfoDialog.GetInstance().PutMessage(qimsg);
        }
#endif
#if DEBUG_ASSESSMENT_LIST
        AssessmentListMsg almsg = msg as AssessmentListMsg;
        if (almsg != null)
        {
            // do a quickinfo dialog for the 1st list item
            QuickInfoMsg qimsg = new QuickInfoMsg();
            qimsg.timeout = 3.0f;
            qimsg.title = "Assessment List";
            qimsg.text = almsg.PrettyPrint();
            QuickInfoDialog.GetInstance().PutMessage(qimsg);
        }
#endif

        // send to MedLabMgr
        //MedLabMgr.GetInstance().PutMessage(msg);
    }

    public override void OnGUI() 
    {
		
    }

	
    public float WorldTime
    {
        get { return worldTime; }
        set { worldTime = value; }
    }
}
//#define DEBUG_BRAIN

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Reflection;

public abstract class BrainStates
{
    public BrainStates()
    {
    }
}

public class BrainState
{
    public string Name;
    public string[] Args;
    public float elapsedTime;

    public BrainState() 
    {
    }

    virtual public void Update(float elapsedTime) 
    { 
        this.elapsedTime += elapsedTime; 
    }

    virtual public void Init() 
    {
        Debug.Log("BrainState.Init, name=" + Name);
        elapsedTime = 0.0f; 
    }

    virtual public void Cleanup() 
    { 
        Debug.Log("BrainState.Cleanup"); 
    }

    virtual public void PutMessage(GameMsg msg) 
    { 
    }

    virtual public void OnGUI()
    {
    }
}

public class Brain : MonoBehaviour
{
    public float elapsedTime = 0.0f;

    // current
    protected BrainState current;
    public BrainState GetBrainState()
    {
        return current;
    }

    protected BrainStates brainStates;

    int fpsCnt = 0;
    float fpsAveTime = 0.0f;
    float fps = 0;
	public DateTime simulatedStartTime;

    static protected Brain instance;
    public static Brain GetInstance()
    {
        return instance;
    }
	
	protected AudioSource audioSource;
	protected Dictionary<string,AudioSource> characterAudioSources;

	virtual public void Awake() // this virtual definition differs from Unity's Awake Signature, so it isnt called.
	{
		if (instance == null)
			instance = this;
		// get audio source
		audioSource = Camera.mainCamera.audio;		
		characterAudioSources = new Dictionary<string,AudioSource>();
		foreach (UnityEngine.Object tc in FindObjectsOfType(typeof (TaskCharacter))){
			if (((TaskCharacter)tc).gameObject.audio != null)
				characterAudioSources[tc.name]=((TaskCharacter)tc).gameObject.audio;
		}
	}
	
    // Use this for initialization
    virtual public void Start()
    {
    }
	
    float ComputeFPS()
    {
        fpsAveTime += Time.deltaTime;
        fpsCnt++;
        if (fpsAveTime > 2.0f)
        {
            fps = 1.0f / (fpsAveTime / (float)fpsCnt);
            fpsAveTime = 0.0f;
            fpsCnt = 0;
        }

        return fps;
    }
	
	public void AdvanceStartTime(string timeString){ // pass in hh:mm
		string[] ss = timeString.Split (':')	;
		if (ss.Length == 2){
			int hours = 0;
			int mins = 0;
			int.TryParse(ss[0],out hours);
			int.TryParse(ss[1],out mins);
			TimeSpan ts = new TimeSpan(hours, mins, 0);
			simulatedStartTime = simulatedStartTime + ts;
			
			// use SetStartTime to get the clocks and all updated
			SetStartTime(simulatedStartTime.Hour.ToString()+":"+simulatedStartTime.Minute.ToString());

			// advance the scene timer
			Timer timer = FindObjectOfType(typeof(Timer)) as Timer;
			timer.AddTime(mins*60);
		}	
	}

	public void SetStartTime(string timeString){ // pass in hh:mm
		simulatedStartTime = DateTime.Now;
		string[] ss = timeString.Split (':')	;
		if (ss.Length == 2){
			int hours = 0;
			int mins = 0;
			int.TryParse(ss[0],out hours);
			int.TryParse(ss[1],out mins);
			TimeSpan ts = new TimeSpan(hours, mins, 0);
			simulatedStartTime = simulatedStartTime.Date + ts;
		}
		// set up any clocks in the level:
		AnalogClocks[] clocks = FindObjectsOfType(typeof(AnalogClocks)) as AnalogClocks[];
		foreach (AnalogClocks clock in clocks){
			clock.startTime = simulatedStartTime;	
		}
	}	
	
	public string GetSimulatedTimeText(){ // could pass in a format  hh:mm for now.
		
		DateTime simulatedTime = simulatedStartTime.AddSeconds(Time.time);;
		return simulatedTime.ToString("HH.mm"); //ToShortTimeString();	
	}
	

	virtual public void OnGUI()
    {
#if SHOW_FPS
		// display framerate
        int w = 80;
        int h = 20;
        int x = Screen.width - w;// Screen.width / 2 - w / 2;
        int y = 0;// Screen.height - h;

        string framerate = System.String.Format("{0:F0} FPS",ComputeFPS());
        GUI.Box(new Rect(x, y, w, h), framerate);

        if (current != null)
            current.OnGUI();
#endif
	}

    // Update is called once per frame
    virtual public void Update()
    {
        elapsedTime += Time.deltaTime;

        if (current != null)
            current.Update(Time.deltaTime);
		
		CheckAudioQueue();
    }

    virtual public void PutMessage(GameMsg msg)
    {
        // handlers
        HandleChangeState(msg);
        HandleInteractStatusMsg(msg);
		HandleCallbacks(msg);

        // pass messages to current state
        if (current != null)
        {
            // send to brain states
            current.PutMessage(msg);
        }
    }

	public delegate bool PutMessageCallback(GameMsg msg);
	private PutMessageCallback putMessageCallback;

	public void AddCallback(PutMessageCallback callback)
	{
		putMessageCallback += callback;
	}
	public void RemoveCallback(PutMessageCallback callback)
	{
		putMessageCallback -= callback;	
	}
	
	public void HandleCallbacks( GameMsg msg )
	{
		if ( putMessageCallback != null )
			putMessageCallback(msg);
	}
	
	public void HandleChangeState(GameMsg msg)
    {
        // swap states
        ChangeStateMsg statemsg = msg as ChangeStateMsg;
        if (statemsg != null)
        {
#if BRAIN_DEBUG
            Debug.Log("Got ChangeStateMsg = " + statemsg.state);
#endif
            // convert string to classname
            Type type = brainStates.GetType();
            if (type == null)
                return;

            PropertyInfo state = type.GetProperty(statemsg.state);
            if (state == null)
            {
                Debug.Log("Brain : HandleChangeState(" + statemsg.state + ") Can't find state!");
                return;
            }

            BrainState next = state.GetValue(brainStates, null) as BrainState;
            if (next != null)
            {
                // save name
                next.Name = statemsg.state;
                next.Args = statemsg.args;

                // cleanup old state
                if (current != null)
                    current.Cleanup();

                // set
                current = next;

                // init new state
                if (current != null)
                    current.Init();
            }
            else
            {
                Debug.LogWarning("ChangeStateMsg: unknown state - " + statemsg.state);
            }
        }
    }

    public void HandleInteractStatusMsg(GameMsg msg)
    {
        // first try making this a InteractStatusMsg
        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg == null)
        {
            // nope, try InteractMsg
            InteractMsg imsg = msg as InteractMsg;
            if (imsg == null)
            {
#if DEBUG_BRAIN
                UnityEngine.Debug.Log("Brain.HandleInteractStatusMsg() : not InteractMsg!!");
#endif
                // not the right msg, hangup
                return;
            }

            ismsg = new InteractStatusMsg(imsg);
        }
        if (ismsg != null)
        {
#if DEBUG_BRAIN
            UnityEngine.Debug.Log("Brain.HandleInteractStatusMsg(" + ismsg.InteractName + ")");
#endif
            // log it
            LogMgr.GetInstance().GetCurrent().Add(new InteractStatusItem(ismsg));

            // send interact status message to everyone
            ObjectManager.GetInstance().PutMessage(ismsg);
			// pass to the GUI manager
			if (GUIManager.GetInstance() != null)
				GUIManager.GetInstance().PutMessage(ismsg);
            // pass to decision mgr
            DecisionMgr.GetInstance().PutMessage(ismsg);
        }
    }
	
	public class AudioInfo
	{
		public string name;
		public AudioClip clip;
		public float time;
		public AudioSource source;
	}
	
	protected Queue<AudioInfo> AudioQueue;
	protected float AudioQueueTime = 0.0f;
	protected bool AudioQueuePaused = false; // allow pause with a failsafe timeout so player can speak over characters...
	protected float AudioQueuePauseTime = 0;

	public AudioInfo CurrentAudioInfo = null;

	public AudioSource GetAudioSource(string character){
		if (characterAudioSources.ContainsKey (character))
			return characterAudioSources[character]; 
		// add here if it's not in the dictionary.  The Patient is added after Awake, so will follow this pattern.
		foreach (TaskCharacter tc in FindObjectsOfType<TaskCharacter>()){
			if (tc.name == character && tc.gameObject.audio != null){
				characterAudioSources[tc.name]= tc.gameObject.audio;
				return characterAudioSources[character]; 
			}
		}


		return audioSource;
		// could check and use audioScource
	}
	
	public void QueueAudio(AudioClip clip, string charName=null)
	{
		if ( AudioQueue == null )
			AudioQueue = new Queue<AudioInfo>();
		// create new info
		AudioInfo ai = new AudioInfo();
		ai.clip = clip;
		ai.time = 0.0f;
		ai.name = charName;
		if ( charName != null )
			ai.source = GetAudioSource(charName);
		// add to queue
		AudioQueue.Enqueue(ai);
	}

	public virtual void QueueAudioGap( float time, string charName=null )
	{
		if ( AudioQueue == null )
			AudioQueue = new Queue<AudioInfo>();
		
		AudioInfo ai = new AudioInfo();
		ai.time = time;
		ai.name = charName;
		ai.source = audioSource;
		if (charName != null && characterAudioSources.ContainsKey (charName))
			ai.source = characterAudioSources[charName]; // could check and use audioScource
		AudioQueue.Enqueue(ai);
	}

	public virtual void PauseAudioQueue(float timeout=5){

		if (CurrentAudioInfo == null)
						return;

		if (AudioQueuePaused) {
			CancelInvoke ("ResumeAudioQueue"); // restart invoke with the new timeout
		}
		if (Camera.mainCamera != null) {
			AudioSource cameraAudio = audioSource;
			if (CurrentAudioInfo.source != null) cameraAudio = CurrentAudioInfo.source;
			if (cameraAudio != null) {
				cameraAudio.Pause ();
				VoiceMgr.PauseSpeaking();
				Invoke ("ResumeAudioQueue",timeout);
				AudioQueuePaused = true;
				if (AudioQueuePauseTime == 0)
					AudioQueuePauseTime = Time.time; // so we can update the Queue timer when we resume
			}
		}
	}

	public virtual void ResumeAudioQueue(){ // OVERRIDDEN WITHOUT BASE CALL in Trauma Brain
		if (CurrentAudioInfo == null)
						return;
		CancelInvoke ("ResumeAudioQueue");
		AudioQueuePaused = false;
		if (Camera.mainCamera != null) {
			AudioSource cameraAudio = Camera.mainCamera.audio;
			if (CurrentAudioInfo.source != null) cameraAudio = CurrentAudioInfo.source;
			if (cameraAudio != null) {
				cameraAudio.Play ();
				VoiceMgr.ResumeSpeaking();

				AudioQueuePaused = false;
				if (AudioQueuePauseTime != 0)
					AudioQueueTime += (Time.time - AudioQueuePauseTime);
				AudioQueuePauseTime = 0;
			}
		}
	}
			
	public void CheckAudioQueue() // need to make this queue per character.
	{
		if ( AudioQueue == null )
			return;
		if (!VoiceMgr.CanSpeak()) return; // don't process the queue while the player is speaking
		// check to see if we're allowed to play 
		if ( AudioQueueTime < Time.time )
		{
			if ( AudioQueue.Count > 0 )
			{
				// get next audio to play
				AudioInfo ai = AudioQueue.Dequeue(); 
				// set current
				CurrentAudioInfo = ai;
				if ( ai.clip != null )
				{
					// get the audio time
					AudioQueueTime = Time.time + ai.clip.length;
					// now play it
					PlayAudio(ai);
				}
				else
				{
					// this is just an audio gap
					AudioQueueTime = Time.time + ai.time;
					CurrentAudioInfo = null;
				}
			}
			else
				CurrentAudioInfo = null;
		}
	}

	// Zoll, scripts, and sound effects call this one
    public virtual void PlayAudio(string name) // TODO need to queue these if !VoiceManager.CanSpeak()
    {
        if (name == null || name == "")
        {
            //UnityEngine.Debug.Log("Brain.PlayAudio() : name null or empty");
            return;
        }

        if ( Camera.mainCamera != null )
        {
        	AudioSource cameraAudio = audioSource;
            if (cameraAudio != null)
            {
                AudioClip clip = SoundMgr.GetInstance().Get(name);
                if (clip != null && cameraAudio != null)
                {
#if DEBUG_AUDIO
                    UnityEngine.Debug.Log("Brain.PlayAudio() : play <" + name + ">");
#endif
                    // move audio to current camera location
                    cameraAudio.PlayOneShot(clip);
                }
                else
                    UnityEngine.Debug.Log("Brain.PlayAudio() : can't find sound clip <" + name + ">");
            }
            else
                UnityEngine.Debug.Log("Brain.PlayAudio() : cameraAudio=null");
        }
        else
            UnityEngine.Debug.Log("Brain.PlayAudio() : can't find MainCamera");
    }

	public virtual void PlayAudio(AudioClip clip)// TODO need to queue these if !VoiceManager.CanSpeak()
    {
    	if (clip != null)
        {
            UnityEngine.Debug.Log("Brain.PlayAudio() : play <" + clip.name + ">");
            // move audio to current camera location
            Camera.mainCamera.audio.PlayOneShot(clip);
        }
    }

	public virtual void PlayAudio(AudioInfo ai)// TODO need to queue these if !VoiceManager.CanSpeak()
	{
		if (ai.clip)
		{
#if DEBUG_BRAIN
			UnityEngine.Debug.Log("Brain.PlayAudio() : play <" + ai.clip.name + ">");
#endif
			if ( ai.source != null ){
				ai.source.clip = ai.clip;
				ai.source.Play();
			}
			else
				Camera.mainCamera.audio.PlayOneShot(ai.clip);
		}
	}

	public virtual bool PlayTTS(string text, string character){
		// base brain does nothing here, trauma brain uses SAPI to play TTS
		return false;
	}
	
	public void LoopAudio( string name )
	{
    	AudioClip clip = SoundMgr.GetInstance().Get(name);
		LoopAudio (clip);
	}
	
	public void LoopAudio( AudioClip clip )
	{		
    	if (clip != null)
        {
            UnityEngine.Debug.Log("Brain.PlayAudio() : play <" + clip.name + ">");
            // move audio to current camera location
			Camera.mainCamera.audio.loop = true;
            Camera.mainCamera.audio.PlayOneShot(clip);
        }
	}
	
	public void PlayAudioLoop( AudioClip clip )
	{
		
	}

    public virtual void PlayVocals(string name)
    {
        if (name == null)
            return;

        AudioClip clip = SoundMgr.GetInstance().Get(name);
        if (clip != null )
        {
            Camera.mainCamera.audio.clip = clip;
            Camera.mainCamera.audio.Play();
        }
    }

    public void StopVocals()
    {
        AudioSource cameraAudio = GameObject.Find("MainCamera").audio;
        if(cameraAudio != null)
        {
            cameraAudio.Stop();
            cameraAudio.clip = null;
        }
    }

    // this is handling calls from the JavaScript container
    public void SpeechToText(string command)
    {
        if (command == "")
            return;

        //SpeechProcessor.GetInstance().SpeechToText(command);
        NluPrompt.GetInstance().SpeechToText(command);
    }
}



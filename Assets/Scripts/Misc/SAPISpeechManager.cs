#define USE_INFO_DIALOG
//#define DEBUG_SAPI

using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[System.Serializable]
public class SpeechInputRecord {
	public string filename;
	public string nextCommand;
	public string status;
	[XmlIgnore]
	public AudioClip clip;
}

public class SAPISpeechManager : MonoBehaviour {

	public enum eInputMode{
		openMic,
		pushToTalk,
	}

	public bool useRecording = true;
	public bool playbackFailures = true;
	public bool logFailures = true;
	public AudioClip micOpenSound;
	public bool isActivated = false;  // Call Activate/Deactivate
	public bool listening = false;
	public bool expectingResult = false; // push to talk and a sound has started...
	public Microphone.eClipStatus micError = Microphone.eClipStatus.ok;
	public List<SpeechInputRecord> unrecognizedClips;
	float lastRecognizerUpdate = 0;
	int inputSequence=0;
	public bool showdebug = false;
	public bool hasShownWarning = false;
	public string debugString = "";
	public eInputMode inputMode = eInputMode.openMic;

	// Grammar XML file name
	public string GrammarFileName;
	
	// Grammar language (English by default)
	public int LanguageCode = 1033;
	
	// Required confidence
	public float RequiredConfidence = 0.0f;
	public int phraseConfidence = 0;
	
	// GUI Text to show messages.
	public GameObject debugText;
	
	public bool useVoiceCommands = false; // voice won't initialize api or do any processing unless this is set to true by someone


	public GUILabel micIcon;
	public GUIStyle iconStyle;
	public GUILabel micText;
	public Texture2D mutedTexture;
	public Texture2D listeningTexture;
	public Texture2D talkingTexture;
	public Texture2D busyTexture;
	Texture2D currentStateTexture;

	// Is currently listening to a sound
	private bool isListening;
	private float timeSoundStarted = 0;
	private int feedbackIndex = 0;
	
	// Current phrase recognized
	private bool isPhraseRecognized;
	private string phraseTagRecognized;
	float tagTime = 0;

	public string textSpoken = "";
	public string semanticPhrase = "";
	
	// Bool to keep track of whether Kinect and SAPI have been initialized
	private bool sapiInitialized = false;
//	private GameHUD gameHUD = null; // use to set chatbar text
//	private FilterCommandGUI filterCommandGUI = null;
	private string commandTag = "";
	// The single instance of SpeechManager
	private static SAPISpeechManager instance = null;
	
	
	// returns the single SpeechManager instance
	public static SAPISpeechManager Instance
	{
		get
		{
#if UNITY_STANDALONE_WIN
			return instance;
#else
			return null;
#endif
		}
	}

	// called when characters or other system sounds want to interrupt listening...
	// in theory, this should not be called while the push to talk bar is being held, the other systems should know
	// not to interrupt during that time...

	// caled from: Trauma Brain
	public static void StopListeningFor(float time){

		if (instance == null) return;
		if (!instance.sapiInitialized) return;

		if (instance.listening) { // this would mean a character has interrupted us, or we are in open mic mode
			instance.debugString = "StopListeningFor"+time;
			instance.StopListening();
			if (instance.inputMode == eInputMode.openMic)
				instance.Invoke("StartListening",time-0.05f); // restart before the next item is de-queued
		}
	}

	public static bool HandleSayButton( bool buttonState ){
		if (instance == null || !instance.isActivated ) // or not active for some other reason
			return false;

		if (instance.useRecording) {


			if (buttonState && !instance.listening) {
				//instance.listening = true;
				instance.StartListening (); // was just unmute
					// start MIC
				if (MicrophoneMgr.GetInstance ().Microphone != null){
					Camera.main.audio.clip = instance.micOpenSound;
					Camera.main.audio.pitch = 4;
					Camera.main.audio.volume = 0.25f;
					Camera.main.audio.Play();
					MicrophoneMgr.GetInstance ().Microphone.StartRecording ();
					SAPIWrapper.ResetFileProcessing(); // release the file if it is still open
					SAPIWrapper.UpdateSpeechRecognizer(); // needed to actually close the file - added when update made conditional
				}
			}
			if (!buttonState && instance.listening) {
				//instance.listening = false;
				instance.StopListening();
				Camera.main.audio.clip = null;
				Camera.main.audio.pitch = 1;
				Camera.main.audio.volume = 1;
				if (MicrophoneMgr.GetInstance ().Microphone != null){
					string sFilename = Application.dataPath+"/../spokenInput"+".wav"; //instance.inputSequence++.ToString ("D3")+
//					SAPIWrapper.ResetFileProcessing(); // release the file if it is still open - moved to start recording call
//					SAPIWrapper.UpdateSpeechRecognizer(); // needed to actually close the file
					Microphone mic = MicrophoneMgr.GetInstance ().Microphone;
					mic.SetFilename(sFilename);
					if(mic.StopRecordingFile ()){//Invoke("StopRecordingFile",0.1f);  // you cant delay this and then call process file, they have to happen in order!!!!!
						instance.micError = MicrophoneMgr.GetInstance ().Microphone.status;
						instance.expectingResult = true;
						instance.Invoke ("ExpectingResultTimeout",5);
						int hr = SAPIWrapper.ProcessFile(sFilename);
	//					SAPIWrapper.LoadSpeechGrammar(Application.dataPath+instance.GrammarFileName,(short)instance.LanguageCode); // try clling this here to get the grammar working?
						if (hr < 0)
							Debug.LogWarning (	SAPIWrapper.GetSystemErrorMessage(hr));
					}

				}

			}
			return true;
		}
		
		// in open mic mode, the say button interrupts the characters speech so the user can get commands in
		// in push to talk mode, if the button state has changed, mute or unmute the mic.
		instance.debugString = "HandleSayButton" + buttonState;
		if (buttonState && !instance.listening){
//			if (instance.inputMode == eInputMode.pushToTalk)
			//TODO we could set the state to active - purge any previous sounds - in the .dll change is needed to do this

		    	instance.StartListening();
			Brain.GetInstance().PauseAudioQueue();
		}
		if (!buttonState && instance.listening){
			if (instance.inputMode == eInputMode.pushToTalk){
//				instance.Invoke("StopListening",1.5f); // give the speech a chance to be recognized
				instance.StopListening();
				instance.Invoke ("ExpectingResultTimeout",3.0f);
//				instance.stopPending = true;
			}
				//instance.StopListening();
			Brain.GetInstance().ResumeAudioQueue();
		}

		return true; 
	}


	void Awake(){
#if UNITY_STANDALONE_WIN

		// make sure we remain a singleton
		if (instance != null && instance != this){
			DestroyImmediate(this.gameObject);
			return;
		}
		// otherwise, we are the first
		instance = this;
		DontDestroyOnLoad (gameObject);
		SAPIWrapper.ExtractGrammarFile (GrammarFileName, GrammarFileName.Replace ("/Resources/",""));
		unrecognizedClips = new List<SpeechInputRecord>();
#else
		instance = null;
		DestroyImmediate( this );
#endif
		}


	// Use this for initialization
	void Start () {
		if (showdebug)
						Activate ();
	}

	public void OnLevelWasLoaded(){
		if (Application.loadedLevelName == "Trauma_05" || Application.loadedLevelName == "Trauma_Tut")
						Invoke ("CallActivate", 2);
						//Activate(); //TODO using invoke to delay calling this doesnt seem to work.
				else
						Deactivate ();
	}

	public void CallActivate(){
		// Can't call the static function from  Invoke
		Activate ();
	}

	public static void Activate(){
		if (instance == null)
						return;
		if (instance.isActivated)
						return;

		GUIScreen hud = GUIManager.GetInstance().FindScreen("HUDScreen");
		if (hud != null) {
			instance.micIcon = hud.Find("micStatusIcon") as GUILabel;
			instance.micText = hud.Find("micStatusText") as GUILabel;
			instance.iconStyle = instance.micIcon.Style;
		}

		if(!instance.sapiInitialized)
		{
			instance.StartRecognizer();
			
			if(!instance.sapiInitialized)
			{
				//				Application.Quit();
				//				debugText.guiText.text = "Speech not properly initialized";
				return;
			}
		}
		if (instance.inputMode == eInputMode.openMic)
			instance.StartListening();
		else
			instance.StopListening();
		instance.isActivated = true;
	}

	public static void Deactivate(){
		// this SHOULD be called when ending a case
		if (instance == null)
			return;
		if (!instance.isActivated)
			return;
		if(instance.sapiInitialized)
		{
			instance.StopListening();
		}
		instance.isActivated = false;
	}

	public void StartListening(){
		// starting and stopping the recognizer is very expensive, so we use a 'listening' flag to tell us to disregard
		// unwanted input.  Setting the mic input volume works better, but some mic drivers seem to bypass this system setting and give us sound anyway.
		// the listening flag isnt perfect, as the system is actually still processing input
//		Debug.LogWarning ("SAPI StartListening " + Time.time +" "+Time.realtimeSinceStartup);
		debugString = "StartListening";
//		SAPIWrapper.Mute();
		currentStateTexture = listeningTexture;
		// if we're push to talk, tell the brain not to start any new VO's
		if (inputMode == eInputMode.pushToTalk) {
			VoiceMgr.PauseSpeaking ();
			Brain.GetInstance ().PauseAudioQueue (10);
		}

		SAPIWrapper.UnMute();
		//SAPIWrapper.Resume ();
		SAPISpeechManager.Instance.listening = true;
//		UpdateChatBar ();
		UpdateMicIcon ();
	}

	public void StopListening(){
		debugString = "StopListening";
//		Debug.LogWarning ("SAPI STOP Listening " + Time.time +" "+Time.realtimeSinceStartup);
		// if expecting result, we are being interrupted...
		if (expectingResult)
						return;

		currentStateTexture = talkingTexture;

		VoiceMgr.ResumeSpeaking ();
//		if (Brain.GetInstance () != null)
//			Brain.GetInstance ().ResumeAudioQueue (); // this causes a loop
		SAPIWrapper.Mute();
		//SAPIWrapper.Pause ();
		SAPISpeechManager.Instance.listening = false;
//		UpdateChatBar ();
		UpdateMicIcon ();
	}

	public static void Speak(string textToSpeak){
#if UNITY_STANDALONE_WIN
		if (Instance == null || !instance.sapiInitialized)
						return;
		Instance.currentStateTexture = Instance.talkingTexture;
		float phraseLength = 0.5f + (textToSpeak.Length/25f);
		if (Brain.GetInstance() != null)
			Brain.GetInstance().QueueAudioGap(phraseLength);
		SAPIWrapper.Speak ( textToSpeak ); // need to make this a co routine
		Instance.currentStateTexture = Instance.listeningTexture;
#endif
	}
	
	// Update is called once per frame
	
	void Update () 
	{
		if (!isActivated ) 
						return;
//		if (!useVoiceCommands) return;
		
		// start Kinect speech recognizer as needed
		if(!sapiInitialized)
		{
			StartRecognizer(); //TODO we can't be calling this every frame...
			
			if(!sapiInitialized)
			{
				//				Application.Quit();
//				debugText.guiText.text = "Speech not properly initialized";
				return;
			}
		}

/*		key check handled in TraumaKeyHandler
		// check on push to talk with the space bar - these only return true for the frame update when the state changed.
		if ( Input.GetKeyDown( KeyCode.Space)){
			
		}
		if (Input.GetKeyUp (KeyCode.Space)) {
			
		}
*/

		
		if(sapiInitialized && expectingResult)// && (Time.time - lastRecognizerUpdate > 0.5f)) // added expectingResult 7/28
		{
			lastRecognizerUpdate = Time.time; // try limiting update frequency ?
			// update the speech recognizer
			int rc = SAPIWrapper.UpdateSpeechRecognizer(); // the callback occurs during this call if recognized
			
			if(rc >= 0)
			{
				// estimate the listening state
				if(SAPIWrapper.IsSoundStarted())
				{
#if DEBUG_SAPI
Debug.Log ("SAPI SoundStarted "+isListening+listening+expectingResult+Time.time+"rc"+rc);
#endif
					if (!isListening && ( inputMode==eInputMode.openMic || listening) ){
						textSpoken = "";
						timeSoundStarted = Time.time;
						isListening = true;
						expectingResult = true;
						VoiceMgr.PauseSpeaking();
//						UpdateChatBar();
					}

					isListening = true;
				}
				else if(SAPIWrapper.IsSoundEnded()) // SoundEnded seems to remain true every update once it is true.
				{
					float soundLength = Time.time - timeSoundStarted; // ignore short, unparsable sounds // this is wrong for file based recognitions.
					if (useRecording) soundLength = MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip ().length;

					if (isListening){
#if DEBUG_SAPI
Debug.Log ("SAPI SoundEnded "+soundLength+isListening+listening+expectingResult+Time.time+"rc"+rc);
#endif
						VoiceMgr.ResumeSpeaking();
						if (!SAPIWrapper.IsPhraseRecognized() && soundLength > 0.5f && expectingResult){ // disregard unrecognized phrases if not listening
					
							if (!AnnounceSoundError()){

								VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:BAD:COMMAND:"+(feedbackIndex+2).ToString());
								// PlayAudio("BAD:COMMAND:"+ (feedbackIndex+2).ToString());
								// in a very trauma specific way, have the PR give these messages:
								feedbackIndex = ++feedbackIndex%3;
								if (playbackFailures)
									Brain.GetInstance().QueueAudio(	MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip (),null);

							}


							SpeechRejectedCallback();
	//						textSpoken = "--- not recognized ---";
	//						UpdateChatBar();
							expectingResult = false;
						}
						//TODO should we stop expecting result if it was a short sound ?
					    isListening = false;

					}
				}
				
				// check if a grammar phrase has been recognized
				if(SAPIWrapper.IsPhraseRecognized())
				{
					isPhraseRecognized = true;
					
					IntPtr pPhraseTag = SAPIWrapper.GetRecognizedTag();
					phraseTagRecognized = Marshal.PtrToStringUni(pPhraseTag);
					pPhraseTag = SAPIWrapper.GetSpokenText();
					textSpoken = Marshal.PtrToStringUni(pPhraseTag);
					pPhraseTag = SAPIWrapper.GetRecognizedTag();
					semanticPhrase = Marshal.PtrToStringUni(pPhraseTag);

// try triggering command from polled update rather than callback
					if (semanticPhrase.Contains (":"))
						SpeechRecognizedCallback("");
//					    StartCoroutine(ExecuteCommand (semanticPhrase));

					expectingResult = false;
					SAPIWrapper.ClearPhraseRecognized();
	//				UpdateChatBar();
					
					//Debug.Log(phraseTagRecognized);
				}
			}
			else{
				Debug.LogWarning("Update Recognizer returned error "+SAPIWrapper.GetSystemErrorMessage(rc));
			}
		}
		UpdateMicIcon ();
	}

//	public void LateUpdate(){
		// under some conditions, the push to talk is getting stuck.  to make the system inherently stable,
		// note the space bar is up but we are still listening.  this is an error condition and we should stop listening.
		//TODO
		// this should also be centralized in TraumaKey Handler if it works
//		if (!Input.GetKeyUp(KeyCode.Space) && !Input.GetKey (KeyCode.Space) && listening && !expectingResult) {
//			StopListening ();
//		}

//	}

	public static void UpdateMicIcon(){
		if (instance.listening){
			//get the mic level and pic a texture to match
			instance.iconStyle.normal.background = instance.listeningTexture;
			instance.micIcon.text = " (O) ";
			
		}else{
			if (instance.expectingResult){
				instance.iconStyle.normal.background = instance.talkingTexture;
				instance.micIcon.text = "o o o";
				if (instance.micError != Microphone.eClipStatus.ok)
					instance.micText.text = "processing ("+instance.micError.ToString()+")";
				else
					instance.micText.text = "processing";
			}else{
				// if we failed... show red during feedback
				if (instance.micError != Microphone.eClipStatus.ok){
					instance.iconStyle.normal.background = instance.busyTexture;
					instance.micIcon.text = "ERROR";
					instance.micText.text = instance.micError.ToString();
				}else{
					instance.iconStyle.normal.background = instance.mutedTexture;
					instance.micIcon.text = "MUTED";
					instance.micText.text = "Press SPACE bar to talk";
				}
			}
		}
		instance.micIcon.SetStyle(instance.iconStyle);
	}

	bool AnnounceSoundError(){
		// announce sppech quality problem if applicable.
		if (micError == Microphone.eClipStatus.levelLow 
		    || micError == Microphone.eClipStatus.noGaps
		    || micError == Microphone.eClipStatus.noSilence
		    || micError == Microphone.eClipStatus.levelHigh){
			if (micError == Microphone.eClipStatus.levelLow)
				VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:SPEECH:TOO:QUIET");
			else{
				if (micError == Microphone.eClipStatus.noGaps
				    || micError == Microphone.eClipStatus.noSilence)
					VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:SPEECH:TOO:FAST");
				else
					VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:SPEECH:TOO:LOUD");
			}
			return true;
		}else{
			return false;
		}
	}

	public void ClearMicError(){
		micError = Microphone.eClipStatus.ok;
	}

	public void ExpectingResultTimeout(){
		expectingResult = false;
		SAPISpeechManager.Instance.micError = MicrophoneMgr.GetInstance ().Microphone.status;
		SAPISpeechManager.Instance.Invoke ("ClearMicError", 3);
		// for debug, get any info you can from SAPI
		IntPtr pPhraseTag = SAPIWrapper.GetSpokenText();
		string spokenText = Marshal.PtrToStringUni(pPhraseTag);
		pPhraseTag = SAPIWrapper.GetRecognizedTag();
		string recognizedTag = Marshal.PtrToStringUni(pPhraseTag);

		if (logFailures) {
			// log the clip as unrecognized:
			SpeechInputRecord newRecord = new SpeechInputRecord();
			newRecord.clip = MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip ();
			string caseName = CaseConfigurator.GetInstance().data.casename.Replace(" ","");
			newRecord.filename = caseName+System.DateTime.Now.ToShortDateString () +"-"+System.DateTime.Now.ToLongTimeString();
			newRecord.filename = newRecord.filename.Replace("/","-");
			newRecord.filename = newRecord.filename.Replace(":","-");
			newRecord.filename = newRecord.filename.Replace(" ","-");
			newRecord.status = MicrophoneMgr.GetInstance ().Microphone.status.ToString ()+"-processingTimedOut";
			unrecognizedClips.Add (newRecord);

			if (!AnnounceSoundError()){

				VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:BAD:COMMAND:"+(feedbackIndex+2).ToString());
				// PlayAudio("BAD:COMMAND:"+ (feedbackIndex+2).ToString());
				// in a very trauma specific way, have the PR give these messages:
				feedbackIndex = ++feedbackIndex%3;
				if (playbackFailures)
					Brain.GetInstance().QueueAudio(	MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip (),null);
			}

		}
	}

	public void SpeechRecognizedCallback(string text){ // the parameter text isnt used

		VoiceMgr.ResumeSpeaking();
		Brain.GetInstance ().ResumeAudioQueue ();

		SAPISpeechManager.Instance.CancelInvoke ("ExpectingResultTimeout");
		SAPISpeechManager.Instance.micError = Microphone.eClipStatus.ok;
		// Debug.Log ("Speech Recognized " + text);
		if (!SAPISpeechManager.Instance.isActivated || !SAPISpeechManager.Instance.expectingResult)
						return;

		// here is where we send the semantic phrase to the dispatcher...
		IntPtr pPhraseTag = SAPIWrapper.GetRecognizedTag();
		string commandPhrase = Marshal.PtrToStringUni(pPhraseTag);

		SAPISpeechManager.Instance.phraseConfidence = (int)(SAPISpeechManager.Instance.RequiredConfidence*100f);
		pPhraseTag = SAPIWrapper.GetSpokenText();
		string spokenPhrase = Marshal.PtrToStringUni(pPhraseTag);
		SendPhraseToInfoDialog (spokenPhrase);
			
		if (commandPhrase.Contains ("%")){
			string[] parts = commandPhrase.Split('%');
			SAPISpeechManager.Instance.phraseConfidence = int.Parse(parts[0]);
			commandPhrase = parts[1];
		}
		if (commandPhrase.Contains ("=")) { //Fragments understood but not a complete command
			VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:BAD:COMMAND:1");
			//SAPISpeechManager.Instance.PlayAudio ("BAD:COMMAND:1");
			SAPISpeechManager.Instance.expectingResult = false;
			textSpoken = " --- please rephrase your command --- ";
			if (logFailures) {
				// log the clip as unrecognized:
				SpeechInputRecord newRecord = new SpeechInputRecord();
				newRecord.clip = MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip ();
				string caseName = CaseConfigurator.GetInstance().data.casename.Replace(" ","");
				newRecord.filename = caseName+System.DateTime.Now.ToShortDateString () +"-"+System.DateTime.Now.ToLongTimeString();
				newRecord.filename = newRecord.filename.Replace("/","-");
				newRecord.filename = newRecord.filename.Replace(":","-");
				newRecord.filename = newRecord.filename.Replace(" ","-");
				newRecord.status = MicrophoneMgr.GetInstance ().Microphone.status.ToString () + "["+commandPhrase+"]";
				unrecognizedClips.Add (newRecord);
			}
		}

		string[] commands = commandPhrase.Split ('+'); // send off multiple commands if separated by '+'

		foreach (string command in commands) { 
			if (command.Contains (":")){
				StartCommand (command);
				// if there were any misses, log them as having been attempts at this command
				if (logFailures && unrecognizedClips.Count > 0) {
					foreach (SpeechInputRecord record in unrecognizedClips){
						record.nextCommand = commandPhrase;
						record.filename = command.Replace (":","-")+"_"+record.status+"_"+record.filename;
						LogRecord(record);
					}
					unrecognizedClips.Clear();
				}
			}
		}
	}

	void LogRecord(SpeechInputRecord record){
		MemoryStream memStream = SaveWav.Save (record.clip);//SaveWav.TrimSilence (currentClip, 0.1f));	
		string path = Application.dataPath + "/../SAPIErrors";

		// get application path and create folder if doesn't exist
		if ( Directory.Exists(path) == false )
			Directory.CreateDirectory(path);
		path = path + "/";

		try{
			FileStream file = new FileStream(path+record.filename+".wav", FileMode.Create, FileAccess.Write); 
			memStream.WriteTo(file);
			file.Close();
		}		
		catch
		{
			Debug.LogError("Error opening speech error log .wav file");
		}
//		XmlSerializer serializer = new XmlSerializer(typeof(SpeechInputRecord));
//		FileStream stream = new FileStream( path+record.filename+".xml", FileMode.Create);
//		serializer.Serialize(stream, record);
//		stream.Close();	
	}

	public void SpeechRejectedCallback( ){

		// announce the rejection ?

		// add the unrecognized clip to a list to be logged on the next successful command

		if (logFailures) {
			// log the clip as unrecognized:
			SpeechInputRecord newRecord = new SpeechInputRecord();
			newRecord.clip = MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip ();
			string caseName = CaseConfigurator.GetInstance().data.casename.Replace(" ","");
			newRecord.filename = caseName+System.DateTime.Now.ToShortDateString () +"-"+System.DateTime.Now.ToLongTimeString();
			newRecord.filename = newRecord.filename.Replace("/","-");
			newRecord.filename = newRecord.filename.Replace(":","-");
			newRecord.filename = newRecord.filename.Replace(" ","-");
			newRecord.status = MicrophoneMgr.GetInstance ().Microphone.status.ToString ();
			unrecognizedClips.Add (newRecord);
		}
		
		VoiceMgr.ResumeSpeaking ();
		Brain.GetInstance ().ResumeAudioQueue ();
		SAPISpeechManager.Instance.expectingResult = false;
		SAPISpeechManager.Instance.CancelInvoke ("ExpectingResultTimeout");
//		VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:BAD:COMMAND:1");
		//SAPISpeechManager.Instance.PlayAudio ("BAD:COMMAND:1");
		textSpoken = " --- speech rejected callback received --- ";
		SAPISpeechManager.Instance.micError = MicrophoneMgr.GetInstance ().Microphone.status;
		SAPISpeechManager.Instance.Invoke ("ClearMicError", 3);

		return;
	}

	public static void StartCommand(string commandPhrase){
	//	instance.ExecuteCommand (commandPhrase);
		instance.StartCoroutine( instance.ExecuteCommand (commandPhrase));
	}

	public IEnumerator ExecuteCommand(string command){
		yield return null; // allows update to continue
		Dispatcher.GetInstance ().ExecuteCommand (command);
		commandTag = command;
/* These cause conflicts with core projects other than Trauma and are not needed...
		if (filterCommandGUI == null) {
			filterCommandGUI = (GUIManager.GetInstance().FindScreenByType<FilterCommandGUI>()) as FilterCommandGUI;
		}
		if (filterCommandGUI != null) {
			filterCommandGUI.Update(); // will call build buttons when it sees the new text
			filterCommandGUI.OnClose();
			Invoke ("ShowQuickCommand",1.5f);
			Invoke ("ShowQuickCommand",3.0f); // because sometimes quick command takes a while and fills the box with rubbish
			Invoke ("CloseQuickCommand",6.0f);
		}
*/
	}

	void SendPhraseToInfoDialog(string phrase){
		InfoDialogMsg infomsg1 = new InfoDialogMsg();
		infomsg1.command = DialogMsg.Cmd.open;
		infomsg1.title = "<Player>";
		infomsg1.text = phrase;
		InfoDialogLoader.GetInstance().PutMessage(infomsg1);
	}
	void SendCommandToInfoDialog(string phrase){
		InfoDialogMsg infomsg1 = new InfoDialogMsg();
		infomsg1.command = DialogMsg.Cmd.open;
		infomsg1.title = "<SEMANTICS>";
		infomsg1.text = phrase;
		InfoDialogLoader.GetInstance().PutMessage(infomsg1);
	}

	void OnGUI(){
//		GUI.Label (new Rect (300, 20, 150, 100), debugString);
		if (!isActivated || !showdebug)
						return;
		if (!showdebug)	GUI.depth = 5;
		Color oldColor = GUI.color;
		Rect iconRect = new Rect (155, 0, 35, 35);
		if (listening && inputMode == eInputMode.pushToTalk ) { //&& SAPIWrapper.IsSoundStarted ()
			float level = MicrophoneMgr.GetInstance().Microphone.Level ();
			float red = 0;
			float green = 0;
			if (level > 0.5f){
				red = 1;
			}
			else if (level > 0.25f){
				red = 0.5f+level*2;
				if (red > 1) red = 1;
				green = 1;
			}
			else{
				green = 0.5f+level*20f;
				if (green > 1) green = 1;
			}
			GUI.color = new Color( red,green,0);// Color.green; // dim if not hearing a sound
			GUI.Box (iconRect, talkingTexture);
		} else {
			if (!listening){
				if (inputMode == eInputMode.pushToTalk){
					GUI.color = Color.red;
					GUI.Box (iconRect, listeningTexture);
				}
				else
				{
					GUI.color = Color.yellow;
					GUI.Box (iconRect, currentStateTexture);
				}
			}
			else
				GUI.Box (iconRect, currentStateTexture);
		}
/*
		iconRect.x += 35;
		iconRect.width = 95;
		string mode = "Listening\nMic Open";
		if (inputMode == eInputMode.pushToTalk || !listening) {
			mode = "Push SAY\nTo Talk";
			GUI.color = new Color(.4f,.7f,1);
		} else {
			GUI.color = oldColor;
		}
		if (GUI.Button (iconRect,mode)){
			if (inputMode == eInputMode.openMic){
				inputMode = eInputMode.pushToTalk;
				StopListening();
			}
			else{
				inputMode = eInputMode.openMic;
				StartListening();
			}
		}
*/

		GUI.color = oldColor;
//		iconRect.y += 40;
//		GUI.Label (iconRect, "SPEECH\nINPUT");

		if (showdebug) {
						GUI.Box (new Rect (300, 00, 650, 120), "Local SAPI ACTIVE:");
						GUI.Label (new Rect (300, 40, 600, 40), "RECOGNIZED UTTERANCE: " + textSpoken + "[" + phraseConfidence + "%]");
						GUI.Label (new Rect (300, 60, 600, 40), "SEMANTIC INTERPRETATION: " + semanticPhrase);


						if (listening) {
								GUI.Label (new Rect (300, 20, 150, 100), "LISTENING");
//			if (GUI.Button(new Rect(0,40,150,40),"STOP")){
//				SAPIWrapper.StopListening();
//				listening = false;
//			}
						} else {
								GUI.Label (new Rect (300, 20, 150, 100), "NOT LISTENING");
//			if (GUI.Button(new Rect(0,40,150,40),"LISTEN")){
//				SAPIWrapper.StartListening();
//				listening = true;
//			}
						}
						GUI.Label (new Rect (400, 20, 250, 100), "Characters CanSpeak()=" + VoiceMgr.CanSpeak ());
						IntPtr pPhraseTag = SAPIWrapper.GetRecognizedTag ();
						string phraseRecognized = Marshal.PtrToStringUni (pPhraseTag);
						pPhraseTag = SAPIWrapper.GetDebugText ();
						string debugText = Marshal.PtrToStringUni (pPhraseTag);

						string status = "Started:" + SAPIWrapper.IsSoundStarted () + 
								" Ended:" + SAPIWrapper.IsSoundEnded () +
								" Phrase:" + SAPIWrapper.IsPhraseRecognized () +
								" Recognized:" + phraseRecognized +
								"\nDebug:" + debugText;
						GUI.Label (new Rect (300, 80, 600, 80), status);
				} else {
			// adding these calls did not prevent the crash we are seeing with no debug .
			/*
			IntPtr pPhraseTag = SAPIWrapper.GetRecognizedTag ();
			string phraseRecognized = Marshal.PtrToStringUni (pPhraseTag);
			pPhraseTag = SAPIWrapper.GetDebugText ();
			string debugText = Marshal.PtrToStringUni (pPhraseTag);
			
			string status = "Started:" + SAPIWrapper.IsSoundStarted () + 
				" Ended:" + SAPIWrapper.IsSoundEnded () +
					" Phrase:" + SAPIWrapper.IsPhraseRecognized () +
					" Recognized:" + phraseRecognized +
					"\nDebug:" + debugText;
					*/
				}
	}

	void StartRecognizer() 
	{
		// only do this on awake ?
		//SAPIWrapper.ExtractGrammarFile (GrammarFileName, GrammarFileName.Replace ("/Resources/",""));
		//SAPIWrapper.CopySystemDlls(); // just for testing, do this always...
		// reload the same level

		try 
		{
			// Initialize the speech wrapper
			int rc = 0;
			string sCriteria = String.Format("Language={0:X}", LanguageCode);
			rc = SAPIWrapper.InitSpeechRecognizer("", false, true);

			if (rc < 0)
			{
				Debug.LogWarning ("Error initializing SAPI: " + SAPIWrapper.GetSystemErrorMessage(rc));

				// put up a dialog message explaining spesh cound not be initialized.
				if (!hasShownWarning)
				{
					DialogMsg msg = new DialogMsg();
					msg.xmlName = "traumaErrorPopup";
					msg.className = "TraumaError";
					msg.modal = true;
					msg.arguments.Add("Speech Error");
					msg.arguments.Add("Speech could not be initialized.  Is your microphone plugged in?");
					GUIManager.GetInstance().LoadDialog(msg);
					hasShownWarning = true;
				}

				throw new Exception(String.Format("Error initializing SAPI: " + SAPIWrapper.GetSystemErrorMessage(rc)));
			}
			else
			{
				sapiInitialized = true;
			}

			instance = this;


			if (inputMode == eInputMode.pushToTalk){
				//SAPIWrapper.Mute();
				StopListening();
				Speak ("<volume level='50'> Push Space Bar To Talk");
			}
			else	
				Speak ("<volume level='50'> Voice is Listening");

			rc = SAPIWrapper.LoadSpeechGrammar(Application.dataPath+GrammarFileName,(short)LanguageCode);
			if (rc < 0)
			{
				sapiInitialized = false;
				Speak ("Error loading grammar");
				// put up a dialog message explaining spesh cound not be initialized.
				if (!hasShownWarning)
				{
					DialogMsg msg = new DialogMsg();
					msg.xmlName = "traumaErrorPopup";
					msg.className = "TraumaError";
					msg.modal = true;
					msg.arguments.Add("Speech Error");
					msg.arguments.Add("Speech could not be initialized.  Is your microphone plugged in?");
					GUIManager.GetInstance().LoadDialog(msg);
					hasShownWarning = true;
				}
				throw new Exception(String.Format("Error loading Grammar: " + SAPIWrapper.GetSystemErrorMessage(rc)));
			}

//			SAPIWrapper.SetRuleState("playercommand",0); // test setting this rule inactive

//!!TEST			SAPIWrapper.SetSpeechRecoCallback (SpeechRecognizedCallback);

//!!TEST			SAPIWrapper.SetSpeechRejectCallback (SpeechRejectedCallback);

			// we should delay this until the level is up and loaded...
//			SAPIWrapper.StartListening();
//			listening = true;
			
//			DontDestroyOnLoad(gameObject); // I think this has already been done in Awake()
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			// see if it's the dll not found error, if so copy the .dll's and init again...
			SAPIWrapper.CopySystemDlls();
			Application.Quit();
		//	Application.LoadLevel(Application.loadedLevel); // this fails for Trauma, so we just quit after copying the .dll's
			if(debugText != null)
				debugText.guiText.text = "Please check the SAPI installations.";
		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = ex.Message;
		}
	}

	private void PlayAudio(string name)
	{
		if (name == null || name == "")
		{
			//UnityEngine.Debug.Log("Brain.PlayAudio() : name null or empty");
			return;
		}

		if ( Camera.main != null )
		{
			AudioSource cameraAudio = Camera.main.audio;
			if (cameraAudio != null)
			{
				AudioClip clip = SoundMgr.GetInstance().Get(name);
				Brain.GetInstance ().QueueAudio ( clip );/*
				if (clip != null && cameraAudio != null)
				{
					bool wasListening = SAPISpeechManager.Instance.listening;
					if (wasListening) StopListening(); // Stop Listening for the length of the clip.
					cameraAudio.PlayOneShot(clip);
					if (wasListening) Invoke("StartListening",clip.length);
				}
				else
					UnityEngine.Debug.Log("PlayAudio() : can't find sound clip <" + name + ">");

*/
			}
			else
				UnityEngine.Debug.Log("PlayAudio() : cameraAudio=null");
		}
		else
			UnityEngine.Debug.Log("PlayAudio() : can't find MainCamera");

		if (useRecording) // should really queue this...
			Brain.GetInstance ().QueueAudio (MicrophoneMgr.GetInstance ().Microphone.GetCurrentClip ());
						
	}

	
	
	
}

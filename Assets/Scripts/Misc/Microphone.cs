#define DEBUG_KEYS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

//[RequireComponent (typeof (AudioSource))]
public class Microphone : MonoBehaviour
{
	public enum eClipStatus
	{
		ok,
		noSound,
		noSilence,
		noGaps,
		levelLow,
		levelHigh,
	}
	public string device;
	public int SampleRate=22050;
	public bool Loop=false;
	public bool UseLoopMode = true;
	bool isLoopRecording = false;
	AudioClip loopClip;
	int loopStartPosition = 0;
	int loopEndPosition = 0;
	int lastMicPosition = 0;
	float lastUpdateTime = 0;
	public int RecordDuration=10;
	public bool ShowGUI=false;
	public bool AutoSelectMicrophone=false;
	public string filename="";
	public eClipStatus status = eClipStatus.ok; // set this when recording stops to help with any recognition problems
	
	string url="";
	
	public Microphone ()
	{
	}

    IEnumerator RequestMicrophone() {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone)) {
			UnityEngine.Debug.Log("Microphone Authorized!");
        } else {
			UnityEngine.Debug.Log("Microphone Not Authorized!");
        }
	}

	AudioSource goAudioSource;
	
	public void Start()
	{
		StartCoroutine(RequestMicrophone());
		
		goAudioSource = this.GetComponent<AudioSource>();
		
		if ( AutoSelectMicrophone == false )
		{
			UnityEngine.Debug.Log("Microphone.Start() : Enum Devices...");
			foreach( string tmp in UnityEngine.Microphone.devices )
			{
				AudioClip clip = UnityEngine.Microphone.Start(tmp,Loop,RecordDuration,SampleRate);
				if ( clip != null )
				{
					UnityEngine.Debug.Log("Microphone Device = <" + tmp + "> is ok");
					if ( device == null || device == "" )
						device = tmp;
				} 
				else
					UnityEngine.Debug.Log("Microphone Device = <" + tmp + "> is not ok");			
			}
		}
		
		MicrophoneMgr.GetInstance().Microphone = this;
		url = NluMgr.GetInstance().GetURL();
		// send setup request
		NluMgr.GetInstance().ContextSetupRequest("1.0", "trauma", null);

		if ( device != null && device != "none" )
		{
			//			UnityEngine.Debug.Log("Microphone Device = <" + device + "> play "+Time.time +" "+Time.realtimeSinceStartup);			
			loopClip = UnityEngine.Microphone.Start(device,true,RecordDuration,SampleRate); // loop = true;
			if ( loopClip == null ){
				UnityEngine.Debug.Log("Microphone Device Problem!!");
				return;
			}
		}
	} 
	
	List<string> devices=null;
	
	void GetDevices()
	{
		if ( devices != null )
			return;
		
		devices = new List<string>();
		foreach( string tmp in UnityEngine.Microphone.devices )
		{
			AudioClip clip = UnityEngine.Microphone.Start(tmp,Loop,RecordDuration,SampleRate);
			if ( clip != null )
				devices.Add(tmp);
		}
	}
	
	Rect guiRect;
	Rect replyRect;
	public void OnGUI()
	{
		if ( ShowGUI == false )
			return;
		
		GetDevices();
		
		int width=Screen.width;
		int height=Screen.height/2;
		guiRect = new Rect(Screen.width/2-width/2,0,width,height);
		GUI.Window (0, guiRect, MicSetup, "Microphone Setup");
		
		replyRect = new Rect(Screen.width/2-width/2,height,width,height);
		GUI.Window (1,replyRect, ReplySetup, "NLU Reply");
	}
	
	Vector2 scrollview2;
	void ReplySetup( int id )
	{
		GUI.depth = 0;
		GUI.DragWindow (new Rect(replyRect.xMin,replyRect.yMin,replyRect.width,20));
		
		scrollview2 = GUILayout.BeginScrollView(scrollview2,false,false);
		GUILayout.Label(NluMgr.GetInstance().RawWWWString);
		GUILayout.EndScrollView();
	}	
	
	Vector2 scrollview;
	void MicSetup( int id )
	{	
		GUI.depth = 0;
		GUI.DragWindow (new Rect(guiRect.xMin,guiRect.yMin,guiRect.width,20));
		
		GUILayout.Label("current <" + device + ">");
		GUILayout.BeginScrollView(scrollview,false,false);
		foreach( string tmp in devices )
		{
			if ( GUILayout.Button(tmp) )
			{
				device = tmp;
#if CLOSE_ON_SELECT
				ShowGUI = false;
#endif
			}
		}
		if ( GUILayout.Button("none") )
		{
			device = "none";
#if CLOSE_ON_SELECT
			ShowGUI = false;
#endif
		}
		
		if ( device == null || device == "" )
			GUILayout.Label("SELECT DEVICE");
		else
			GUILayout.Label("Press M key to start recording, let go to stop (max " + RecordDuration + " sec).  As soon as key is lifted the audio will be sent to the NLU.  You can send the clip as many times as you want by using the buttons below.");
		
		GUILayout.EndScrollView();
		
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Label("URL",GUILayout.Width(50));
		url = GUILayout.TextField(url);
		if ( GUILayout.Button("SET") )
		{
			NluMgr.GetInstance().SetURL(url);
		}		
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		if ( currentClip != null )
		{
			if ( GUILayout.Button("Play Audio Samples=<" + currentClip.samples + ">") )
			{
				Camera.main.audio.PlayOneShot(currentClip);
				//Brain.GetInstance().PlayAudio(currentClip);
			}
			if ( GUILayout.Button("Send to NLU") )
			{
				NluMgr.GetInstance().UtteranceAudio(currentClip);
			}
		}
		
		if ( GUILayout.Button("Close Window") )
			ShowGUI = false;		
	}
	
	bool wasDown=false;
	AudioClip currentClip;
	float startTime;
	float clipTime;

	public void SetFilename(string sFilename){
		filename = sFilename;
	}

	public void StartRecordingLoop(){
		// let the microphone record continuously, and just grab the start and end samples
		if (!UnityEngine.Microphone.IsRecording(device)) {
			// start up a single clip that will be used in loop mode to avoid the startup delay
			// this should be happening in Start now.
			if ( device != null && device != "none" )
			{
				//			UnityEngine.Debug.Log("Microphone Device = <" + device + "> play "+Time.time +" "+Time.realtimeSinceStartup);			
				loopClip = UnityEngine.Microphone.Start(device,true,RecordDuration,SampleRate); // loop = true;
				if ( loopClip == null ){
					UnityEngine.Debug.Log("Microphone Device Problem!!");
					return;
				}
			}
		}
		startTime = Time.time;
		loopStartPosition = UnityEngine.Microphone.GetPosition(device);
		loopEndPosition = loopStartPosition;
		isLoopRecording = true;
	}
	
	public void StartRecording()
	{
		if (UseLoopMode) {
			StartRecordingLoop();
			return;
		}
		// It takes almost 1 sec realtime for the call to UnityEngine.Midrophone.Start, during which the unity engine stalls.
		// this is unacceptable UI for a response to a key press, so we need to start the recording earlier, mark the start and
		// end positions, and then unwind the circular buffer when we stop recording...

		// Trauma was using a device name of  "", so probably just the default device.
		if ( device != null && device != "none" )
		{
//			UnityEngine.Debug.Log("Microphone Device = <" + device + "> play "+Time.time +" "+Time.realtimeSinceStartup);			
			currentClip = UnityEngine.Microphone.Start(device,Loop,RecordDuration,SampleRate);
			startTime = Time.time;
			if ( currentClip == null )
				UnityEngine.Debug.Log("Microphone Device Problem!!");

//			UnityEngine.Debug.Log("Microphone Recording <" + UnityEngine.Microphone.IsRecording(device) + "> Position : " + UnityEngine.Microphone.GetPosition(device)+" "+Time.time +" "+Time.realtimeSinceStartup);
		}
		else
		{
			if ( device == null )
				ShowGUI = true;
		}
	}

	public float Level(){ // returns a value from 0 to 1 for the current recording level...
		if (device == null || device == "none" || !UnityEngine.Microphone.IsRecording (device))
			return 0;
		float[] test = new float[256];
		int position = UnityEngine.Microphone.GetPosition (device);
		if (position < test.Length+1)
						return 0;
		if (UseLoopMode) {
			loopClip.GetData (test, position - test.Length - 1);
		} else {
			currentClip.GetData (test, position - test.Length - 1);
		}
		float maxLevel = 0;
		foreach (float f in test)
			if (Mathf.Abs (f) > maxLevel)
				maxLevel = Mathf.Abs (f);
		return maxLevel;
	}
	
	public float StopRecording()
	{
		if ( device == null || device == "none" )
			return 0.0f;
		
		UnityEngine.Microphone.End(device);
//		UnityEngine.Debug.Log("Microphone Device End");

		// if length < 0.5 then don't do anything
		if ( (Time.time-startTime) < 0.5f )
			return 0.0f;
		
		if ( goAudioSource == null )
		{
			Camera.main.audio.PlayOneShot(currentClip);
			//Brain.GetInstance().PlayAudio(currentClip);
		}
		else
		{
			UnityEngine.Debug.Log("Microphone Play Clip from AudioSource");
			goAudioSource.clip = currentClip;
			goAudioSource.Play();
		}

		if ( currentClip != null )
		{
			// check data
			int goodData = 0;
	 		float[] samples = new float[currentClip.samples * currentClip.channels];
	        currentClip.GetData(samples, 0);				
			foreach( float floatVal in samples )
			{
				if ( floatVal > 0.01f )
					goodData++;
			}
			UnityEngine.Debug.Log("Microphone goodData = <"  + goodData + ">");
			// save the wav to a memstream
			NluMgr.GetInstance().UtteranceAudio(currentClip);			
		}
		
		// return time of recording
		return (clipTime=Time.time-startTime);
	}

	public MemoryStream StopRecordingStream()
	{
		if ( device == null || device == "none" )
			return null;
		
		UnityEngine.Microphone.End(device);
		UnityEngine.Debug.Log("Microphone Device End");
		
		// if length < 0.5 then don't do anything
		if ( (Time.time-startTime) < 0.5f )
			return null;
		
		if ( goAudioSource == null )
		{
			Camera.main.audio.PlayOneShot(currentClip);
			//Brain.GetInstance().PlayAudio(currentClip);
		}
		else
		{
			UnityEngine.Debug.Log("Microphone Play Clip from AudioSource");
			goAudioSource.clip = currentClip;
			goAudioSource.Play();
		}
		
		// check data
		int goodData = 0;
		float[] samples = new float[currentClip.samples * currentClip.channels];
		currentClip.GetData(samples, 0);				
		foreach( float floatVal in samples )
		{
			if ( floatVal > 0.01f )
				goodData++;
		}
		UnityEngine.Debug.Log("Microphone goodData = <"  + goodData + ">");
		
		return SaveWav.Save (SaveWav.TrimSilence (currentClip, 0.1f));
	}

	public bool StopRecordingFile()
	{
		if ( device == null || device == "none" )
			return false;


		if (UseLoopMode) {
			AudioClip copyClip = AudioClip.Create("copyClip", loopClip.samples, loopClip.channels, loopClip.frequency,false,false);
			float[] loopSamples = new float[loopClip.samples];
			loopClip.GetData(loopSamples,0);
			copyClip.SetData(loopSamples,0);
//			Brain.GetInstance().QueueAudio(	copyClip,null);
			// get the data from loopClip
			isLoopRecording = false;
			float duration = Time.time-startTime;
			if (duration < 0.5f) return false;

			int sampleCount = (int)(duration*loopClip.frequency);

			loopEndPosition = UnityEngine.Microphone.GetPosition(device);

			int expectedEnd = loopStartPosition + sampleCount;
			if (expectedEnd > loopClip.samples) expectedEnd -= loopClip.samples;

			int miscount = expectedEnd - loopEndPosition;
			if (Mathf.Abs(miscount) > 1000)
				Debug.LogWarning("Microphone samples off by "+miscount);

			float[] recordedSamples = new float[sampleCount];
			copyClip.GetData(recordedSamples,loopStartPosition);

			currentClip = AudioClip.Create(loopClip.name, sampleCount, loopClip.channels, loopClip.frequency,false,false);
			currentClip.SetData(recordedSamples,0);
		} 
		else 
		{
			UnityEngine.Microphone.End (device);
		}
//		UnityEngine.Debug.Log("Microphone Device End");
		
		// if length < 0.5 then don't do anything
		if ( (Time.time-startTime) < 0.5f )
			return false;
		
/*		don't play back the recording, unless the command is not recognized...
 * 		if ( goAudioSource == null )
		{
			Camera.main.audio.PlayOneShot(currentClip);
			//Brain.GetInstance().PlayAudio(currentClip);
		}
		else
		{
			UnityEngine.Debug.Log("Microphone Play Clip from AudioSource");
			goAudioSource.clip = currentClip;
			goAudioSource.Play();
		}
*/		
		// check data
		int goodData = 0;
		float[] samples = new float[currentClip.samples * currentClip.channels];
		currentClip.GetData(samples, 0);				
		foreach( float floatVal in samples )
		{
			if ( floatVal > 0.01f )
				goodData++;
		}
//		UnityEngine.Debug.Log("Microphone goodData = <"  + goodData + ">");

		// could just tack this last bit of code onto the memory stream returned by the StopRecordingStream method when called.
		// using a file because I couldnt find the way to marshall the stream into an object implementing IStream for the SAPI to use
		// could create a class that implements IStream and just pass the recording in memory...

		MemoryStream memStream = SaveWav.Save (currentClip);//SaveWav.TrimSilence (currentClip, 0.1f));		
// TODO need to try/catch this. can fail if SAPI still has open
		try{
		FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write); 
		memStream.WriteTo(file);
		file.Close();
		}		
		catch
		{
			Debug.LogError("Error opening spoken input file - sharing ?");
		}

		status = ClipStatus ();

		return true;
	}

	public AudioClip GetCurrentClip(){
		return currentClip;
	}

	public float PlaybackCurrentClip(){
		if (currentClip == null)
						return 0;

		if ( goAudioSource == null )
		{
			Camera.main.audio.PlayOneShot(currentClip);
			//Brain.GetInstance().PlayAudio(currentClip);
		}
		else
		{
			goAudioSource.clip = currentClip;
			goAudioSource.Play();
		}
		return currentClip.length;
	}

	public void Update()
	{
		// validate mic position is advancing
/*
		if (UseLoopMode && loopClip != null) {
			int pos = UnityEngine.Microphone.GetPosition(device);
			if (lastUpdateTime != 0){
				int expectedPos = lastMicPosition + (int)((Time.time - lastUpdateTime)*loopClip.frequency);
				if (expectedPos > loopClip.samples) expectedPos -= loopClip.samples;
				if (Mathf.Abs (expectedPos - pos) > 1500)
					Debug.LogWarning("Mic not tracking as expected "+pos+" "+expectedPos);
			}
			lastUpdateTime = Time.time;
			lastMicPosition = pos;
		}
*/

#if DEBUG_KEYS
		if ( Application.isEditor == false )
			return;
		if ( Input.GetKeyUp(KeyCode.M) && Input.GetKey(KeyCode.LeftControl) )
		{
			ShowGUI = true;
			return;
		}

		if ( ShowGUI == false )
			return;

		if ( Input.GetKey(KeyCode.M) == true )
		{
			if ( wasDown == false )
			{
				wasDown = true;
				StartRecording();
			}
		}
		else
		{
			if ( wasDown == true )
			{
				wasDown = false;
				StopRecording();
			}
		}
#endif
	}

	public eClipStatus ClipStatus(){
		// analyse the current clip (from start point to finish point) for volume, gaps, etc.

		// scan once and count some properties
		int numSilent = 0;
		int numTooQuiet = 0;
		int numTooLoud = 0;
		int headLength = 0;
		int tailLength = 0;
		int numGaps = 0;

		// now walk the clip and tally
		float silentLevel = 0.0005f;
		float tooQuietLevel = 0.01f;
		float tooLoudLevel = 0.15f;
		int gapLength = 2000; // a gap is this many frames at quiet level or below
		int gapCount = 0;

		float[] samples = new float[currentClip.samples * currentClip.channels];
		currentClip.GetData(samples, 0);				
		foreach( float floatVal in samples )
		{
			float testVal = Mathf.Abs(floatVal);

			if ( testVal <= silentLevel ){
				numSilent++;
				gapCount++;
			}
			else
			{
				if (testVal <= tooQuietLevel){
					numTooQuiet ++;
					gapCount++;
				}
				else
				{ 
					// not a gap.
					if (gapCount > gapLength){
						if (headLength == 0){
							headLength = gapCount;
						}else{
							numGaps++;
						}
					}
					gapCount = 0;
					if (testVal >= tooLoudLevel){
						numTooLoud++;
					}

				}
			}
		}
		// done with samples, was there a tail ?
		if (gapCount >= gapLength) 
		{
			tailLength = gapLength;
		}

		// now to choose a status to return

		if (numSilent >= samples.Length*0.95f)
		    return eClipStatus.noSound;
		if (numTooLoud >= samples.Length * 0.05f ) 
			return eClipStatus.levelHigh;
		if (numSilent+numTooQuiet >= samples.Length * 0.95f && numTooLoud < gapLength )
			return eClipStatus.levelLow;
		if ((headLength == 0 || tailLength == 0) && numGaps == 0)
			return eClipStatus.noSilence;
		if (numGaps == 0)
			return eClipStatus.noGaps;

		return eClipStatus.ok;
	}
}

public class MicrophoneMgr 
{
    static MicrophoneMgr instance;
    public static MicrophoneMgr GetInstance()
    {
        if (instance == null)
            instance = new MicrophoneMgr();
        return instance;
    }
	
	Microphone microphone;
	public Microphone Microphone
	{
		get { return microphone; }
		set { microphone = value; }
	}
}


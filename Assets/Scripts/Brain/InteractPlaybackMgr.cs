#define DEBUG_PLAYBACK
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

public class InteractPlaybackItem : PlaybackItem
{
	public InteractPlaybackItem() : base()
	{}

}

public class DialogButtonPlaybackItem : PlaybackItem
{
	public string Dialog;
	public string Button;

	public DialogButtonPlaybackItem() : base()
	{}
	
}

public class PlaybackItem
{
	public enum PlaybackType
	{
		button,
		interact
	}
	public PlaybackType Type;
	
	public string Say;
	public string InteractName;
	public string Character;
	public string Args;

	public string Dialog;
	public string Button;

	public float RealTime;
    public float Delay;
    public List<string> Prereq;
    public bool Executed;

    public PlaybackItem()
    {
        Executed = false;
		RealTime = -1;
		Delay = -1;
    }

	public virtual void Execute()
	{
		switch( Type )
		{
		case PlaybackType.interact:
			ExecuteInteract();
			break;
		case PlaybackType.button:
			ExecuteButton();
			break;
		}
	}

	public void ExecuteButton()
	{
		// find the dialog to press
		GUIScreen screen = GUIManager.GetInstance().FindScreen (Dialog) as GUIScreen;
		if ( screen != null )
		{
			// find button
			GUIButton button = screen.Find (Button) as GUIButton;
			if ( button != null )
			{
				button.SimulateButtonAction ();
				Executed = true;
			}
		}
	}

	public void ExecuteInteract()
	{
		// set executed
		Executed = true;
		
		// if we have a say tag just say it and return
		if (Say != null && Say != "")
		{
			NluPrompt.GetInstance().SpeechToText(Say);
			
			QuickInfoMsg qimsg = new QuickInfoMsg();
			qimsg.title = "You Say...";
			qimsg.text = Say;
			QuickInfoDialog.GetInstance().PutMessage(qimsg);
		}
		else
		{
			// put the interact message
			InteractionMap map = InteractionMgr.GetInstance().Get(InteractName);
			if (map != null)
			{
				if (Character == null)
				{
					#if DEBUG_PLAYBACK
					UnityEngine.Debug.Log("InteractPlaybackItem.Execute() : no character or character is player, send <" + InteractName + "> to Brain");
					#endif
					Dispatcher.GetInstance().ExecuteCommand(map.item);
					// send to brain
					//Brain.GetInstance().PutMessage(new InteractMsg(null, map,true));
					// log it
					//LogMgr.GetInstance().Add(new InteractLogItem(Time.time,"player",map.item,map.response));
				}
				else
				{
					ObjectInteraction objint = ObjectManager.GetInstance().GetBaseObject(Character.ToLower()) as ObjectInteraction;
					if (objint != null)
					{
						#if DEBUG_PLAYBACK
						UnityEngine.Debug.Log("InteractPlaybackItem.Execute() : Create interaction=<" + InteractName + "> for character=<" + Character + ">");
						#endif
						// do message
						Dispatcher.GetInstance().ExecuteCommand(map.item,Character.ToLower ());
						//objint.PutMessage(new InteractMsg(objint.gameObject, map));
					}
					else
					{
						#if DEBUG_PLAYBACK
						UnityEngine.Debug.Log("InteractPlaybackItem.Execute() : can't find character=<" + Character + ">, send <" + InteractName + "> to Brain");
						#endif
						Dispatcher.GetInstance().ExecuteCommand(map.item);
						// send to brain
						//Brain.GetInstance().PutMessage(new InteractMsg(null, map));
						// log it
						//LogMgr.GetInstance().Add(new InteractLogItem(Time.time, "player", map.item, map.response));
					}
				}
			}
			#if DEBUG_PLAYBACK
			else{
				UnityEngine.Debug.LogError("InteractPlaybackItem.Execute() : can't find interaction=<" + InteractName + "> for character=<" + Character + ">");
				map = InteractionMgr.GetInstance().Get(InteractName);

			}
			#endif
		}
	}

	public static string GetTimeString( float time )
	{
		if ( time < 0.0f )
			time = 0.0f;

		int hours, minutes, seconds;
		seconds = (int)(time);
		minutes = (int)(time / 60.0f);
		hours = (int)(minutes / 60.0f);
		minutes -= hours * 60;
		seconds -= (minutes * 60) + (hours * 3600);
		//return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
		return minutes.ToString("00") + ":" + seconds.ToString("00");
	}

	public string Debug()   
	{
		string debug = "";

		switch ( Type )
		{
		case PlaybackType.interact:
			debug = "Command<" + InteractName + "> Character<" + Character + "> Time<" + GetTimeString(RealTime) + "><" + GetTimeString(RealTime-InteractPlaybackMgr.GetInstance().elapsedTime) + ">";
			break;
		case PlaybackType.button:
			debug = "Dialog<" + Dialog + "> Button<" + Button + "> Time<" + GetTimeString(RealTime) + "><" + GetTimeString(RealTime-InteractPlaybackMgr.GetInstance().elapsedTime) + ">";
			break;
		}

#if DEBUG_PLAYBACK                    
        //UnityEngine.Debug.Log("InteractPlaybackItem : InteractName=" + InteractName + " : Character=" + Character + " : Delay=" + Delay);
#endif
		return debug;
    }

    public List<InteractStatusItem> FindInteractLogItems(string name, List<LogItem> log)
    {
#if DEBUG_PLAYBACK
        //UnityEngine.Debug.Log("InteractPlaybackItem.FindInteractLogItems(" + name + ")");
#endif

        List<InteractStatusItem> found = new List<InteractStatusItem>();
        if (log != null)
        {
            // search list for the assessment item
            foreach (LogItem logitem in log)
            {
                InteractStatusItem ilogitem = logitem as InteractStatusItem;
                if (ilogitem != null)
                {
#if DEBUG_PLAYBACK
                    //UnityEngine.Debug.Log("InteractPlaybackItem.FindInteractLogItems(" + name + ") : compare=" + ilogitem.InteractName);
#endif

                    if (name == ilogitem.Msg.InteractName)
                    {
#if DEBUG_PLAYBACK
                        UnityEngine.Debug.Log("InteractPlaybackItem.FindInteractLogItems(" + name + "," + ilogitem.Msg.InteractName + ") : true");
#endif
                        // add all items found to the list
                        found.Add(ilogitem);
                    }
                }
            }
        }

        return found;
    }

    float reportTime = 0.0f;

    public bool WaitForPrereq(List<LogItem> log, float elapsedTime)
    {
        bool report = false;
        if (elapsedTime > reportTime)
        {
            reportTime = elapsedTime + 2.0f;
            report = true;
        }

        if (Prereq != null && Prereq.Count > 0)
        {
            foreach( string prereq in Prereq )
            {
                if (FindInteractLogItems(prereq, log).Count == 0)
                {
#if DEBUG_PLAYBACK                    
                    if ( report == true )
                        UnityEngine.Debug.Log("InteractPlaybackItem.WaitForPrereq(" + prereq + ")");
#endif
                    return true;
                }
            }
        }
        return false;
    }

}

public class InteractPlaybackList
{
    public List<PlaybackItem> Items;
	public string Filename;

	public InteractPlaybackList()
	{
	}
	
    public void LoadXML(string filename)
    {
		string path=null;
		
		if ( filename == null )
		{
#if UNITY_EDITOR
			path = EditorUtility.OpenFilePanel("Select log file...","","xml");
#endif
			if ( path == "" || path == null )
				return;

			Filename = Path.GetFileName(path);
			
			// load from stream reader
	        Serializer<List<PlaybackItem>> serializer = new Serializer<List<PlaybackItem>>();
        	Items = serializer.Load(new StreamReader(path));
		}
		else
		{
			// load from text asset
	        Serializer<List<PlaybackItem>> serializer = new Serializer<List<PlaybackItem>>();
        	Items = serializer.Load(filename);
		}
		
        if (Items != null)
        {
            Debug();
        }
    }

	public void SaveXML(string filename)
	{
		string path=null;

		if ( filename == null )
		{
#if UNITY_EDITOR
			path = EditorUtility.SaveFilePanel("Enter save file...","","playback","xml");
#endif
			if ( path == "" || path == null )
				return;
		}
		else
			path = filename;
		
        Serializer<List<PlaybackItem>> serializer = new Serializer<List<PlaybackItem>>();
		if ( serializer != null && path != null)
		{
			serializer.Save(path,this.Items);
		}

		// also save Log file 
		string directory = Path.GetFullPath(path);
		string name = Path.GetFileName(path);
		directory = directory.Replace(name,"");

		LogMgr.GetInstance().Save (directory + "LOG_"+name);
	}
	
	public void Save( LogRecord log )
	{
		// create list of log items from the log record
		List<LogItem> LogItems = log.CreateLogItems();
		
		// iterate and build the playback list
		float lasttime = 0.0f;		
		Items = new List<PlaybackItem>();	
		foreach (LogItem item in LogItems )
		{
			InteractLogItem logitem = item as InteractLogItem;
			// only record the non-scripted actions (player actions)
			if ( logitem != null && logitem.scripted.ToLower() == "false" )
			{
				PlaybackItem playback = new PlaybackItem();
				playback.Type = PlaybackItem.PlaybackType.interact;
				playback.InteractName = logitem.InteractName;
				playback.Character = logitem.param;
				playback.RealTime = logitem.time;
				playback.Args = logitem.args;
				// init delay to this one if unitialized
				if ( lasttime == 0.0f )
					lasttime = logitem.time;
				// set delay to difference between this command and the last
				playback.Delay = logitem.time - lasttime;				
				lasttime = logitem.time;
				// add it
				Items.Add(playback);
			}

			// get dialog button items
			DialogButtonItem dbi = item as DialogButtonItem;
			if ( dbi != null )
			{
				PlaybackItem dbPlayback = new PlaybackItem();
				dbPlayback.Type = PlaybackItem.PlaybackType.button;
				dbPlayback.Button = dbi.button;
				dbPlayback.Dialog = dbi.dialog;
				dbPlayback.RealTime = dbi.time;
				// init delay to this one if unitialized
				if ( lasttime == 0.0f )
					lasttime = dbi.time;
				// set delay to difference between this command and the last
				dbPlayback.Delay = dbi.time - lasttime;				
				lasttime = dbi.time;
				// add it
				Items.Add(dbPlayback);
			}
		}
	}
	
    public void Debug()
    {
        foreach (PlaybackItem item in Items)
        {
            item.Debug();
        }
    }
}

public class InteractPlaybackFileMgr
{
	static InteractPlaybackFileMgr instance;
	public static InteractPlaybackFileMgr GetInstance()
	{
		if ( instance == null )
			instance = new InteractPlaybackFileMgr();
		return instance;
	}
	public InteractPlaybackList StartList;
	public string Filename;
	public float timeScale;
}

public class InteractPlaybackMgr : MonoBehaviour
{
	public bool ShowGUI=false;
	public bool EnableAutoLogging=true;
    public InteractPlaybackList List;
	public float StartTime=0;
	bool savingFile = false;

    public enum State
    {
        Init=0,
        Playing,
        Complete
    }
    State state;

    static InteractPlaybackMgr instance;
    public InteractPlaybackMgr()
    {
        instance = this;
    }

    public static InteractPlaybackMgr GetInstance()
    {
        return instance;
    }

	public void Start()
	{
		elapsedTime = StartTime;
		StartList(InteractPlaybackFileMgr.GetInstance().StartList);
	}

	public string SavePath="Playbacks";
	public string SaveBaseName="PB-";
	public string FullPath=null;
	
	// init gets called at beginning of scenario....make the filename
	// we then call Save repeatedly after every interaction during the game, saving data to the
	// same file for later playback.
	public string InitSaveFile()
	{
		string path = Application.dataPath + "/../" + SavePath;
		// get application path and create folder if doesn't exist
		if ( Directory.Exists(path) == false )
			Directory.CreateDirectory(path);
		// create unique name
		string unique=System.DateTime.Now.ToString ();
		unique = unique.Replace("/",".");
		unique = unique.Replace(":",".");
		unique = CaseConfigurator.GetInstance ().data.casename.Replace (" ", "") + unique;
		FullPath = path + "/" + SaveBaseName + unique + ".xml";
		// print path
		UnityEngine.Debug.Log ("InteractPlaybackMgr.InitSaveFile() : FullPath=" + FullPath);
		//
		return FullPath;
	}
	
	public void Save()
	{
		// don't save if not initialzed above
		if ( FullPath != null && FullPath != "")
			StartCoroutine(SaveBackground ());
	}

	public IEnumerator SaveBackground()
	{
/*  moved this block into thread
		// create a logrecord
		LogRecord record = new LogRecord();
		record.Add(LogMgr.GetInstance().GetCurrent());
		record.SetDateTime();
		// create a playback file
		InteractPlaybackList list = new InteractPlaybackList();
		list.Save(record);
		// now save this file
		list.SaveXML(FullPath);
*/
		yield return null;
		LogRecord record = new LogRecord();
		record.Add(LogMgr.GetInstance().GetCurrent());
		record.SetDateTime();

		while (savingFile) // to prevent other threads
		yield return new WaitForSeconds(0.5f);

		savingFile = true;

		ThreadedSavePlayback saveThread = new ThreadedSavePlayback();
		saveThread.filePath = FullPath;
		saveThread.record = record;
		Thread saveCallThread = new Thread(saveThread.StartSaveCall);
		saveCallThread.Start();
		
		while (!saveThread.ThreadComplete)
			yield return new WaitForSeconds(.1f);
		
//		while (saveThread.ThreadRunning)
//			yield return new WaitForSeconds(.5f);
		
		saveCallThread.Abort();
		saveCallThread.Join();

		savingFile = false;

		yield return null;

	}
	
	int currentIdx = -1;
    public void Load()
    {
        List = new InteractPlaybackList();
        List.LoadXML("XML/playback");
        currentIdx = 0;
        elapsedTime = StartTime;
        // init state
        state = State.Init;
    }
	
	// create a playback list from a log file
	public void Load(LogRecord log)
	{
		List = new InteractPlaybackList();
		List.Save(log);
		currentIdx = 0;
		elapsedTime = StartTime;
		// init state
		state = State.Init;
	}

    public float elapsedTime;
	public bool debug=false;

    public bool Done()
    {
        if (List.Items.Count == 0)
            return false;

        foreach (PlaybackItem item in List.Items)
        {
            if (item.Executed == false)
                return false;
        }

		Time.timeScale = 1.0f;

		// set time to restart
		if ( restartTime == 0.0f )
			restartTime = elapsedTime + RestartDelay;

        return true;
    }

    public void Update()
    {
        // quit if we're not playing or completed
        if (state != State.Playing)
            return;

		// turn up speed after things get settled
		if ( elapsedTime > 2.0f && elapsedTime < 3.0f )
			Time.timeScale = InteractPlaybackFileMgr.GetInstance().timeScale;

		// elapsed time
		elapsedTime += Time.deltaTime;

		// check playback
		if (currentIdx != -1 && List.Items.Count > 0)
        {
            // get item
            PlaybackItem item = List.Items[currentIdx];

 			// if the elapsed time is greater than item time then just execute it!
			if ( elapsedTime > item.RealTime )
			{
				item.Execute();

				if ( item.Executed == true )
				{
					// reset elapsedTime back to what it should be
					elapsedTime = item.RealTime;
					// increment
	                if (++currentIdx >= List.Items.Count)
	                {
	                   currentIdx = -1;
	                }
				}
			}
        }

		// check break condition, something hung up
		if ( currentIdx != -1 && currentIdx < List.Items.Count-1 )
		{
			PlaybackItem item = List.Items[currentIdx+1];
			if ( item != null )
			{
				if ( item.RealTime-InteractPlaybackMgr.GetInstance().elapsedTime <= 0.0f )
				{
					PlaybackItem current = List.Items[currentIdx];
					UnityEngine.Debug.LogError("InteractPlaybackMgr.Update() : Playback lockup on command <" + current.Debug () + "> at time=" + current.RealTime);
					UnityEngine.Debug.LogError("InteractPlaybackMgr.Update() : prev <" + List.Items[currentIdx-1].Debug () );
					if ( currentIdx+1 < List.Items.Count )
						UnityEngine.Debug.LogError("InteractPlaybackMgr.Update() : next <" + List.Items[currentIdx+1].Debug () );
					if (RestartOnDoneOrError){
						UnityEngine.Debug.LogError("Restarting!");
						RestartWithList(List);
					}
				}
			}

		}

        // check if we're done and then wait 2 seconds and display assessment dialog
        if (Done() == true && elapsedTime > restartTime)
        {
#if DISPLAY_ASSESSMENT_DIALOG
            // run the report here
            // for testing, evaluate on every interact msg...
            ScenarioReport report = new ScenarioReport();
            report.SetInfo("Trauma", "Rob", "123981328123");
            report.SetScenario("Scenario 1");
            report.Evaluate();
            report.SaveDatabase();

            AssessmentMgrDialogMsg amd = new AssessmentMgrDialogMsg();
            amd.command = DialogMsg.Cmd.open;
            amd.Report = report.Report;
            AssessmentMgrDialog.GetInstance().PutMessage(amd);
#endif
			if ( RestartOnDoneOrError == true )
			{
				UnityEngine.Debug.LogError("InteractPlaybackMgr.Update() : Sucessful...Restarting!");
				RestartWithList(List);
			}
        }
    }

	public void StartList( InteractPlaybackList list )
	{
		if ( list != null && list.Items != null && list.Items.Count > 0 )
		{
			// set new playback file
			List = list;
			// start playing, give some extra time
			elapsedTime = StartTime;
			currentIdx = 0;
			state = State.Playing;
			debug = true;
			restartTime = 0.0f;

			// calc index to start based on StartTime
			if ( StartTime != 0.0f )
			{
				foreach( PlaybackItem item in list.Items )
				{
					if ( item.RealTime >= StartTime )
						break;
					else
						currentIdx++;
				}
				UnityEngine.Debug.Log ("InteractPlaybackMgr.StartList() Advancing to index=" + currentIdx + " : item=" + list.Items[currentIdx].InteractName);
			}
		}
	}

	public bool RestartOnDoneOrError=true;
	public float RestartDelay=10.0f;
	float restartTime;

	public void RestartWithList( InteractPlaybackList list )
	{
		// reset executed flag in list
		foreach( PlaybackItem item in list.Items )
			item.Executed = false;

		InteractPlaybackFileMgr.GetInstance().StartList = list;
		InteractPlaybackFileMgr.GetInstance().Filename = list.Filename;
		InteractPlaybackFileMgr.GetInstance().timeScale = Time.timeScale;
		Application.LoadLevel ("Trauma_05");
	}

	void OnGUI()
	{
		if ( ShowGUI == false )
			return;

		GUILayout.BeginArea(new Rect(0,40,600,300));

		if ( state != State.Playing )
		{
			GUILayout.Box ("INTERACT PLAYBACK");
			if ( GUILayout.Button ("Save") )
			{
				// create a logrecord
				LogRecord record = new LogRecord();
				record.Add(LogMgr.GetInstance().GetCurrent());
				record.SetDateTime();
				// create a playback file
				InteractPlaybackList list = new InteractPlaybackList();
				list.Save(record);
				// now save this file
				list.SaveXML(null);
			}
			if ( GUILayout.Button ("Load") )
			{
				debug = true;
				if ( state != State.Playing )
				{
					// now create a playback list
					InteractPlaybackList list = new InteractPlaybackList();
					list.LoadXML(null);
					// save start 
					RestartWithList(list);
					//StartList(list);
				}
				else
				{
					UnityEngine.Debug.LogError("InteractPlaybackMgr.Update() : can't start new playback while one is in progress!!");
				}				
			}
		}
		else if ( state == State.Playing )
		{
			GUILayout.Box ("INTERACT PLAYBACK : " + InteractPlaybackFileMgr.GetInstance().Filename);
			if ( GUILayout.Button ("Stop") )
			{
				state = State.Complete;
				Time.timeScale = 1.0f;
			}
			GUILayout.BeginHorizontal();
			if ( GUILayout.Button ("1x"))
				Time.timeScale = 1.0f;
			if ( GUILayout.Button ("2x"))
				Time.timeScale = 2.0f;
			if ( GUILayout.Button ("5x"))
				Time.timeScale = 5.0f;
			GUILayout.EndHorizontal();
		}

		if ( state != State.Playing || currentIdx == -1 || debug == false )
		{
			if ( restartTime != 0.0f && RestartOnDoneOrError == true)
				GUILayout.Button("Playback Complete, Restarting in " + ((int)(restartTime-elapsedTime)).ToString () + " seconds...");

			GUILayout.EndArea();
			return;
		}

		GUILayout.Space(10);
		if ( (currentIdx-1) >= 0 )
			GUILayout.Label(" PB.curr=" + List.Items[currentIdx-1].Debug ());//InteractName + "," + List.Items[currentIdx].Character);
		GUILayout.Label(" PB.wait=" + List.Items[currentIdx].Debug ());//InteractName + "," + List.Items[currentIdx].Character);
		if ( (currentIdx+1) < List.Items.Count )
			GUILayout.Label(" PB.next=" + List.Items[currentIdx+1].Debug ()); //InteractName + "," + List.Items[currentIdx+1].Character);
		GUILayout.EndArea();
	}		
}

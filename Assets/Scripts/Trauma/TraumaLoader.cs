using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TraumaStartScreen
{
	static TraumaStartScreen instance;
	public static TraumaStartScreen GetInstance()
	{
		if ( instance == null )
			instance = new TraumaStartScreen();
		return instance;
	}
	
	public string StartXML="traumaLoginScreen";//"trauma05MissionStart";
	public string StartCase;//="Tutorial Case";
}

public class MenuLoader : MonoBehaviour
{
	static float fadeTime=0.5f;

	static MenuLoader instance;
	public static MenuLoader GetInstance()
	{
		return instance;
	}

	void Awake()
	{
		instance = this;
	}
	
	IEnumerator gotoCase()
	{
		yield return new WaitForSeconds(fadeTime+0.1f);
		// set start for traumaMainMenu
		TraumaStartScreen.GetInstance().StartXML = "traumaCaseSelection";
		// go to intro screen
		Application.LoadLevel("traumaMainMenu");
	}
	
	public void GotoCase()
	{
		// set fading down
		Time.timeScale = 1.0f;
		GUIManager.GetInstance().Fade = true;
		GUIManager.GetInstance().SetFadeCurtain(1.0f,fadeTime,0.0f);
		StartCoroutine(gotoCase ());
	}
	
	IEnumerator gotoMain()
	{
		yield return new WaitForSeconds(fadeTime+0.1f);
		// go to intro screen
		Application.LoadLevel("traumaMainMenu");
	}
	
	public void GotoMain()
	{
		// set start for traumaMainMenu
		TraumaStartScreen.GetInstance().StartXML = "traumaMainMenu";
		// set fading down
		Time.timeScale = 1.0f;
		GUIManager.GetInstance().Fade = true;
		GUIManager.GetInstance().SetFadeCurtain(1.0f,fadeTime,0.0f);
		StartCoroutine(gotoMain ());
	}

	// this adjusts the ISM times of any scans to when they are actually complete (CALL:CT:PHONE:COMPLETE)
	public void ChangeCTScanTimes()
	{
		List<InteractStatusItem> ctList=new List<InteractStatusItem>();
		
		List<InteractStatusItem> ism = LogMgr.GetInstance().FindLogItems<InteractStatusItem>();
		foreach( InteractStatusItem item in ism )
		{
			// start of command sequence
			if ( item.Msg.InteractName == "CALL:CT:PHONE" )
				ctList.Clear ();
			// schedule item
			if ( item.Msg.InteractName.Contains ("SCHEDULE:CT") )
				ctList.Add (item);
			// we're done with all the commands 
			if ( item.Msg.InteractName == "CALL:CT:PHONE:COMPLETE" )
			{
				foreach( InteractStatusItem ct in ctList )
				{
					// set time for "SCHEDULE:CT" commands to 
					// be the COMPLETE time
					ct.time = item.time;
				}
			}
		}
	}	
	
	IEnumerator gotoAssessment()
	{
		yield return new WaitForSeconds(fadeTime+0.1f);
		// set start for traumaMainMenu
		TraumaStartScreen.GetInstance().StartXML = "traumaDecisionBreakdown";
		// generate a report and save to the database
		TraumaReportMgr.GetInstance().CreateReport().SaveDatabase();	
		// wait for database to get request
		yield return new WaitForSeconds(0.6f);
#if WAIT_UNTIL_COMPLETE
		// make sure the requests finish
		float waitTime = Time.time + 10.0f;
		while ( DatabaseMgr.GetInstance().HasActiveRequests() == true && Time.time < waitTime )
			UnityEngine.Debug.LogError ("HasActiveRequests==true");
#endif
		// go to intro screen
		Application.LoadLevel("traumaMainMenu");
	}
	
	public void GotoAssessment()
	{
		// set fading down
		Time.timeScale = 1.0f;
		GUIManager.GetInstance().Fade = true;
		GUIManager.GetInstance().SetFadeCurtain(1.0f,fadeTime,0.0f);
		// adjust CT times
		ChangeCTScanTimes();
		// load scene
		StartCoroutine(gotoAssessment ());
	}
	
	IEnumerator startCase()
	{
		// wait for fade below
		yield return new WaitForSeconds(fadeTime+0.1f);
		// go to main menu
		Application.LoadLevel("traumaMainMenu");
	}

	public void StartCase( string xml, string casename )
	{
		//
		UnityEngine.Debug.Log ("TraumaLoader.StartCase(" + TraumaStartScreen.GetInstance().StartCase + ")");
		// set GUI
		TraumaStartScreen.GetInstance().StartXML = xml;	//"trauma05MissionStart";
		TraumaStartScreen.GetInstance().StartCase = casename;
		// set fading down
		Time.timeScale = 1.0f;
		GUIManager.GetInstance().Fade = true;
		GUIManager.GetInstance().SetFadeCurtain(1.0f,fadeTime,0.0f);
		// start after fade
		StartCoroutine(startCase ());
	}

	public void RetryCase()
	{
		if ( TraumaStartScreen.GetInstance().StartXML != null && TraumaStartScreen.GetInstance().StartCase != null )
			StartCase (TraumaStartScreen.GetInstance().StartXML,TraumaStartScreen.GetInstance().StartCase);
	}	
}

public class TraumaLoader : MonoBehaviour
{
	static float fadeTime=0.5f;

	void Awake()
	{
		instance = this;
	}

	static TraumaLoader instance;
	public static TraumaLoader GetInstance()
	{
		return instance;
	}

	void loadCallback( bool status, string data, string error, WWW www )
	{
		if ( status == false )
			UnityEngine.Debug.LogError ("TraumaLoader.loadCallback() : can't load StartCase=<" + TraumaStartScreen.GetInstance().StartCase + ">");                      
	}

	public void Start()
	{
		// load start case if exists
		if (TraumaStartScreen.GetInstance ().StartCase != null) {
			CaseConfiguratorMgr.GetInstance ().LoadCaseConfiguration (TraumaStartScreen.GetInstance ().StartCase, loadCallback);
			// the mission start xml now varies by case, so load the correct one here.
			TraumaStartScreen.GetInstance().StartXML = CaseConfiguratorMgr.GetInstance ().Data.missionStartXml;
		}
		// load GUI
		if (TraumaStartScreen.GetInstance ().StartXML != null && TraumaStartScreen.GetInstance ().StartXML != "") 
		{
			// set screen to proper res
			GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
			GUIManager.GetInstance().FitToScreen = true;
			GUIManager.GetInstance().Letterbox = true;
			// go to start XML
			GUIManager.GetInstance ().LoadFromFile (TraumaStartScreen.GetInstance ().StartXML);
		} 
		else 
		{
			// no startxml, so no video, so just load up the level and go.
			// with the config loaded, is this enough ?
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoadingScreen";
			msg.className = "TraumaLoadingScreen";
			GUIManager.GetInstance().LoadDialog(msg);
		}

	}
	
	public void Load()
	{
		UnityEngine.Debug.Log ("TraumaLoader.Load()");
		Application.LoadLevel("Trauma_05");
	}

	IEnumerator restart()
	{
		// start fade
		yield return new WaitForEndOfFrame();
		//float time=1.0f;
		//GUIManager.GetInstance().SetFadeCurtain(1.0f,time,0.0f);
		//yield return new WaitForSeconds(time+0.1f);
		Load ();
	}

	public void Restart()
	{
		UnityEngine.Debug.Log ("TraumaLoader.Restart()");
		// set to not load GUI
		TraumaStartScreen.GetInstance().StartXML = null;
		// set fading down
		Time.timeScale = 1.0f;
		GUIManager.GetInstance().Fade = true;
		GUIManager.GetInstance().RemoveAllScreens();
		// set screen to proper res
		GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
		GUIManager.GetInstance().FitToScreen = true;
		GUIManager.GetInstance().Letterbox = true;
		// restart
		StartCoroutine(restart ());
	}

	AsyncOperation op;
	
	IEnumerator LoadAsync()
	{
      	op = Application.LoadLevelAsync("Trauma_05");
   		while (!op.isDone)
    	{ 
			yield return new WaitForSeconds(0.1f); 
		}
		yield return 0;
	}
	
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
	
	public void OnGUI()
	{
//		if ( op != null )
//			GUI.Label(new Rect(0,0,300,50),"op.progress=" + op.progress);
	}
}


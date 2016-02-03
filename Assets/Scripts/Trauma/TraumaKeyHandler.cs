using UnityEngine;
using System.IO;

// try to start consolidating game keystrokes here
public class TraumaKeyHandler : MonoBehaviour
{
	void Update()
	{
		// don't do anything if case isn't valid
		if ( CaseConfiguratorMgr.GetInstance().CaseValid == false )
			return;

		if ( Input.GetKeyUp (KeyCode.Escape))
		{
			if ( GUIManager.GetInstance().FindScreen ("TraumaPauseMenu") == null )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaPauseMenu";
				msg.className = "TraumaPauseMenu";
				msg.modal = true;
				GUIManager.GetInstance().LoadDialog(msg);
			}
			else
			{
				GUIManager.GetInstance().Remove ("TraumaPauseMenu");
			}
		}

		// check on push to talk with the space bar 
		if ( Input.GetKeyDown( KeyCode.Space)){
//			Debug.LogWarning(" Detected space bar down, "+Time.time+" "+Time.realtimeSinceStartup);
			if (!SAPISpeechManager.HandleSayButton(true))
			{
				// start MIC
				UnityEngine.Debug.Log("GameHUD.HandleSayButton() : start MIC");
				if ( MicrophoneMgr.GetInstance().Microphone != null )
					MicrophoneMgr.GetInstance().Microphone.StartRecording();
			}
	
		}
		if (Input.GetKeyUp (KeyCode.Space)) {
			if (!SAPISpeechManager.HandleSayButton(false))
			{
				// stop MIC
				UnityEngine.Debug.Log("GameHUD.HandleSayButton() : stop MIC");
				if ( MicrophoneMgr.GetInstance().Microphone != null ){
					string sFilename = Application.dataPath+"/spokenInput.wav";
					MemoryStream stream = MicrophoneMgr.GetInstance().Microphone.StopRecordingStream();
					FileStream file = new FileStream(sFilename, FileMode.Create, FileAccess.Write); 
					stream.WriteTo(file);
					file.Close();
					SAPIWrapper.ProcessFile(sFilename);
				}
			}
		}
	
	if ( Application.isEditor && Input.GetKeyUp ( KeyCode.L ) )
		{
			GUIManager.GetInstance().FitToScreen = (GUIManager.GetInstance().FitToScreen)?false:true;
			GUIManager.GetInstance().Letterbox = true;
		}
	}
}
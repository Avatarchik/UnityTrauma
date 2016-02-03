using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LMSIntegrationTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public string username="rob.hafey@gmail.com";
	public string password="password1";
	public string contentID="Zoll Maintenance";
	public bool loopTest=false;
	string contentError="";

	bool loginError=false;
	string loginString="";

	IEnumerator DoLogin()
	{
		yield return new WaitForSeconds(0.01f);
		LMSIntegration.GetInstance().LMSLoginWithPing(username,password,LoginCallbackRepeated);
	}

	public void LoginCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
			loginString = "data=" + data + " : download.text=" + download.text;
		else
			loginString = "download.error=" + download.error;
		debugList.Add (loginString);
	}

	public void LoginCallbackRepeated(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			StartCoroutine(DoLogin ());
			loginString = "data=" + data + " : download.text=" + download.text;
			UnityEngine.Debug.LogError("OK:" + loginString);
			debugList.Add (loginString);
		}
		else
		{
			loginString = "download.error=" + download.error;
			UnityEngine.Debug.LogError("ERROR:" + loginString);
			debugList.Add (loginString);
		}
	}
	
	List<LMSIntegration.LMSCourseInfo> courseList;

	void CourseCallback( bool status, string result, string contentID, List<LMSIntegration.LMSCourseInfo> list, WWW download )
	{
		// special case, no id
		if ( download == null )
		{
			debugList.Add (LMSCmd + "status=" + status + ": result=" + result );
			UnityEngine.Debug.LogError("CourseCallback() : " + LMSCmd + "status=" + status + ": result=" + result );
			return;
		}

		contentError = download.error;

		if ( download.error != null )
		{
			UnityEngine.Debug.LogError("CourseCallback() : " + LMSCmd + download.error);
			debugList.Add (download.error);
			courseList = null;
		}
		else
		{
			contentError = "ok";
			courseList = list;
			if ( showDetail == true )	
				debugList.Add (download.text);
			if ( list == null || list.Count == 0)
			{
				debugList.Add (LMSCmd + "status=" + status + ": result=" + result + " : ContentID=[" + contentID + "] : No Courses");
				UnityEngine.Debug.LogError("CourseCallback() : " + LMSCmd + "status=" + status + ": result=" + result + " : ContentID=[" + contentID + "] : No Courses");
			}
			else
			{
				foreach( LMSIntegration.LMSCourseInfo item in courseList )
				{
					debugList.Add (LMSCmd + "ContentID=[" + contentID + "] : " + item.DebugString);
					UnityEngine.Debug.LogError("CourseCallback() : " + LMSCmd + "ContentID=[" + contentID + "] : " + item.DebugString);
				}
			}
		}
	}

	string LMSCmd="";

	void SimpleCallback( bool status, string contentID, string result, WWW download )
	{
		if ( download == null )
		{
			debugList.Add (LMSCmd + "contentID=" + contentID + " : status=" + status + ": result=" + result );
			UnityEngine.Debug.LogError("SimpleCallback() : " + LMSCmd + "status=" + status + ": result=" + result );
			return;
		}
		if ( download.error != null )
		{
			debugList.Add (LMSCmd + download.error);
			UnityEngine.Debug.LogError("SimpleCallback() : " + LMSCmd + download.error );
		}
		else
		{
			if ( showDetail == true )
				debugList.Add(LMSCmd + download.text);
			debugList.Add (LMSCmd + "contentID=" + contentID + " : status=" + status + " : result=" + result );
			UnityEngine.Debug.LogError("SimpleCallback() : " + LMSCmd + "status=" + status + " : result=" + result );
		}
	}
	
	void LoadDataCallback( List<string> data, WWW download )
	{
		if ( download.error != null )
			debugList.Add (download.error);
		else
		{
			if ( showDetail == true )
				debugList.Add (download.text);
			if ( data != null && data.Count > 0)
			{
				for (int i=0 ; i<data.Count ; i++)
				{
					debugList.Add ("idx[" + i + "] = [" + data[i] + "]");
				}
			}
			else
			{
				debugList.Add ("no data found");
			}
		}
	}
	
	void AppInfoCallback( bool status, string version, string update, string path, WWW download )
	{
		if ( download.error != null )
			debugList.Add (download.error);
		else
		{
			if ( showDetail == true )
				debugList.Add (download.text);
			debugList.Add ("status=" + status + " : version[" + version + "] : update[" + update + "] : path[" + path + "]");

		}
	}

	void StartContentCallback( bool status, string contentID, string result, WWW download )
	{
		if ( status == true )
		{
			string error = "StartContentCallback() : content launched ok!";
			debugList.Add (error);
			UnityEngine.Debug.LogError (error);

			if ( loopTest == true )
				LMSIntegration.GetInstance().LMSStartContent(contentID,StartContentCallback);
		}
		else
		{
			string error = "StartContentCallback() : error=" + result;
			debugList.Add (error);
			UnityEngine.Debug.LogError (error);
		}
	}

	void GetUserInfoCallback( bool status, string data )
	{
		debugList.Add (data);
	}	

	Vector2 scrollPos;
	List<string> debugList = new List<string>();
	public bool showDetail=false;
	public string key="zoll/defib/sc1";
	public string data="Just a short string!";

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(20,20,Screen.width-40,Screen.height-40));
		GUILayout.Label("LMSLogin : AuthKey=" + LMSIntegration.GetInstance().AuthKey);
		GUILayout.BeginHorizontal();
		GUILayout.Label("username:",GUILayout.Width(80));
		username = GUILayout.TextField(username);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("password:",GUILayout.Width(80));
		password = GUILayout.TextField(password);     
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("LoginWithPing") )
		{
			LMSIntegration.GetInstance().LMSLoginWithPing(username,password,LoginCallback);
		}
		if ( GUILayout.Button("LoginWithPing (Repeated)") )
		{
			LMSIntegration.GetInstance().LMSLoginWithPing(username,password,LoginCallbackRepeated);
		}
		if ( GUILayout.Button("Login") )
		{
			LMSIntegration.GetInstance().LMSLogin(username,password,LoginCallback);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Get UserInfo",GUILayout.Width(150)) )
		{
			LMSIntegration.GetInstance().LMSGetUserInfo(GetUserInfoCallback);
		}
		GUILayout.Label("UserInfo:" + LMSIntegration.UserInfo.PrettyPrint());
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("loginData=");
		GUILayout.Label(loginString);
		GUILayout.EndHorizontal();

		GUILayout.Space(20);
		GUILayout.BeginHorizontal();
		GUILayout.Label("contentID:",GUILayout.Width(80));
		contentID = GUILayout.TextField(contentID);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Enroll & Launch") )
		{
			LMSIntegration.GetInstance().LMSStartContent(contentID,StartContentCallback);
		}
		if ( GUILayout.Button("Enrollment Info") )
		{
			LMSCmd = "LMSGetUserEnrollment : ";
			LMSIntegration.GetInstance().LMSGetUserEnrollment(contentID,CourseCallback);
		}
		if ( GUILayout.Button("Course Info") )
		{
			LMSCmd = "LMSGetCoursesUsingContent : ";
			LMSIntegration.GetInstance().LMSGetCoursesUsingContent(contentID,CourseCallback);
		}
		if ( GUILayout.Button("Auto Enroll") )
		{
			LMSCmd = "LMSEnrollContent : ";
			LMSIntegration.GetInstance().LMSEnrollContent(contentID,SimpleCallback);
		}
		if ( GUILayout.Button("Launch Content") )
		{
			if ( courseList != null && courseList.Count > 0 )
			{
				LMSCmd = "LMSLaunchContent : ";
				LMSIntegration.GetInstance().LMSLaunchContent(contentID,courseList,SimpleCallback);
			}
			else
				debugList.Add ( "No Course List.  Either list wasn't received or list is empty!" );
		}
		if ( GUILayout.Button("Set Complete") )
		{
			if ( courseList != null && courseList.Count > 0 )
			{
				LMSCmd = "LMSSetContentComplete : ";
				LMSIntegration.GetInstance().LMSSetContentComplete(contentID,courseList,SimpleCallback);
			}
			else
				debugList.Add ( "No Course List.  Either list wasn't received or list is empty!" );
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(20);

		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Get App Info") )
			LMSIntegration.GetInstance().LMSAppInfo("zoll","ipad",AppInfoCallback);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("key:",GUILayout.Width(30));
		key = GUILayout.TextField(key);
		GUILayout.Space(10);
		GUILayout.Label("data:",GUILayout.Width(40));
		data = GUILayout.TextField(data);
		if ( GUILayout.Button("Save Data") )
			LMSIntegration.GetInstance().LMSSaveData(key,data,SimpleCallback);
		if ( GUILayout.Button("Load Data") )
			LMSIntegration.GetInstance().LMSLoadData(key,LoadDataCallback);
		if ( GUILayout.Button("Debug SESSIONID") )
			Application.ExternalCall("ShowSessionID");
		GUILayout.EndHorizontal();
		showDetail = GUILayout.Toggle(showDetail,"Show JSON Return");

		GUILayout.BeginVertical();
		scrollPos = GUILayout.BeginScrollView(scrollPos);
		for(int i=debugList.Count-1 ; i>=0 ; i--)
			GUILayout.Label(debugList[i]);
		GUILayout.EndScrollView();
		GUILayout.EndVertical();

		GUILayout.EndArea();
	}

}

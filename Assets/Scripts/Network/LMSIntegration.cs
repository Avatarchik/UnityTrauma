//#define UNITY_IPHONE
#define DEBUG_DATABASE

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;  
using SimpleJSON;
using System.Runtime.InteropServices;

public class LMSInfo
{
	public bool PRODUCTION;
	public string PROD_URL;
	public string PROD_APPINFO_URL;
	public string TEST_URL;
	public string TEST_APPINFO_URL;
}

public class LMSIntegration : MonoBehaviour 
{
	static LMSIntegration instance;
	public static LMSIntegration GetInstance()
	{
		return instance;
	}

	public bool debug=false;
	public bool webdebug=false;
	public string LMS_URL;
	public string MedstarNow_AppName;
	public string MedstarNow_AppURL;
	public string MedstarNow_ActivationCode;

	void LMSDebug( string msg )
	{
		if ( debug == true )
		{
			UnityEngine.Debug.Log (msg);
		}
		if ( webdebug == true )
		{
			Application.ExternalCall("UnityDebug",msg);
		}
	}

#if UNITY_IPHONE

	[DllImport ("__Internal")]
	private static extern System.IntPtr _GetBaseURL();

	[DllImport ("__Internal")]
	private static extern System.IntPtr _GetMedstarNowActivationCode();

	[DllImport ("__Internal")]
	private static extern System.IntPtr _GetMedstarNowAppURL( System.IntPtr product, System.IntPtr version );

	public string GetBaseURL()
	{
		string url="no dll";
#if !UNITY_EDITOR
		url = Marshal.PtrToStringAnsi(_GetBaseURL());
#endif
		return url;
	}
	
	public string GetMedstarNowActivationCode()
	{
		string code="no dll";
#if !UNITY_EDITOR
		code = Marshal.PtrToStringAnsi(_GetMedstarNowActivationCode());
#endif
		return code;
	}

	public string GetMedstarNowAppURL( string product, string version )
	{
		string code="no dll";
#if !UNITY_EDITOR
		code = Marshal.PtrToStringAnsi(_GetMedstarNowAppURL( Marshal.StringToHGlobalAuto(product), Marshal.StringToHGlobalAuto(version) ));
#endif
		return code;
	}

	public void LaunchMedstarNow( string product, string version )
	{
		MedstarNow_AppURL = GetMedstarNowAppURL( product, version );
		Application.OpenURL(MedstarNow_AppURL);
	}
	
#endif

	IEnumerator sendAwakeMessage()
	{
		yield return new WaitForSeconds(0.1f);
		Application.ExternalCall("UnityDebug","sendAwakeMessage()");
		Application.ExternalCall("LMSIntegrationAwake");
	}
	
	void Awake()
	{
		instance = this;
#if UNITY_IPHONE
		// only need to do these once
		LMS_URL = GetBaseURL();
		MedstarNow_ActivationCode = GetMedstarNowActivationCode();
		// set paths
		getAPI();
#endif
#if UNITY_WEBPLAYER
		// set user info to invalid before starting handshake
		UserInfo.Valid = false;
		// NOTE!! this call handshakes with the containing web page to get important info
		// back from the web page (user name and authkey for now).  This JS method must exist
		// in the containing page for the handshake to work.
		//
		SendAwakeMessage ();
#endif
		SetURLs();
	}

	public void SendAwakeMessage()
	{
		StartCoroutine(sendAwakeMessage());
	}

#if !UNITY_WEBPLAYER
	// this method is used for getting the URL and APPINFO_URL from the
	// ConfigLMS.xml file from the streaming assets folder.  The method only changes the 
	// URLs if the file exists.  To edit you must open the IPA file, go into the data/RAW folder 
	// and edit the XML.
	void getAPI()
	{
		Serializer<LMSInfo> serializer = new Serializer<LMSInfo>();
		try {
			StreamReader reader = new StreamReader(Application.streamingAssetsPath+"/ConfigLMS.xml");
			if ( reader != null )
			{
				LMSInfo info = serializer.Load (reader);
				if ( info != null )
				{
					if ( info.PRODUCTION == true )
					{
						if ( info.PROD_URL != null )
							URL = info.PROD_URL;
						if ( info.PROD_APPINFO_URL != null )
							APPINFO_URL = info.PROD_APPINFO_URL;
					}
					else
					{
						if ( info.TEST_URL != null )
							URL = info.TEST_URL;
						if ( info.TEST_APPINFO_URL != null )
							APPINFO_URL = info.TEST_APPINFO_URL;
					}
				}
			}
		} 
		catch(FileNotFoundException e)
		{
			//UnityEngine.Debug.LogError("<" + e.FileName + "> not found!");
		}
	}
#endif

	string username;

	public string Username
	{
		get { return username; }
	}

	public void SetAPI( string api )
	{
		URL = api;
		LMSDebug ("API=" + api);
	}

#if UNITY_WEBPLAYER
	// this method gets the authkey back from the containing webpage.  
	// NOTE!! The calling convention requires that there is a GameObject named "LMSIntegration" with the LMSIntegration
	// script sitting in the root of the running scene!
	//
	public void SendAuthKey( string authkey )
	{
		Application.ExternalCall("UnityDebug","got SendAuthKey=" + authkey);
		AuthKey = "\"" + authkey + "\"" ;
		// call to get LMSUserInfo
		LMSGetUserInfo();
	}

	public void SendUsername( string username )
	{
		this.username = username;
		Application.ExternalCall("UnityDebug","got SendUsername=" + username);
	}

#endif

	public bool Production=false;
	public string URL_Production="api.sitelms.org/corwin/r2d0/";
	public string APPINFO_URL_Production="http://api.medstarapps.org/corwin/stateless/get/";
	public string URL_Staging="api.newstaging.sitelms.org/corwin/r2d0/";
	public string APPINFO_URL_Staging="http://api.staging.medstarapps.org/corwin/stateless/get/";

	void SetURLs()
	{
		if ( Production == true )
		{
			URL = URL_Production;
			APPINFO_URL = APPINFO_URL_Production;
		}
		else
		{
			URL = URL_Staging;
			APPINFO_URL = APPINFO_URL_Staging;
		}
	}

	public string URL;
	public string APPINFO_URL;

	//
	// LOGIN  METHODS
	// passes username ,password, and callback
	//
	//
	public void LMSLogin( string username, string password, DatabaseMgr.Callback Callback )
	{
		StartCoroutine(lmsLogin(username,password,Callback));
	}
	
	IEnumerator lmsLogin( string username, string password, DatabaseMgr.Callback Callback )
	{
		string url = "http://www.sitelms.org/restapi/auth";
		//string bodyString = "{\"action\":\"User\",\"params\":{\"user_name\":\"" + username + "\",\"password\":\"" + password + "\"},\"authKey\":null}";
		
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "User");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("user_name",username);
		arr.AddField ("password", password );
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();
		
		// Create a download object
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString));
		yield return download;
		
		//Application.ExternalCall("Debug","LMSIntegration.lmsLogin() : URL=" + url + " : bodyString=" + bodyString );
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null)
		{
			DBResult = "";
			DBErrorString = download.error;
			// do callback
			if (Callback != null)
				Callback(false, DBResult, DBErrorString, download);
			// do global callback
			if (DatabaseMgr.GetInstance().ErrorCallback != null )
				DatabaseMgr.GetInstance().ErrorCallback(false,DBResult,DBErrorString,download);
		}
		else
		{
			if ( download.text == null )
			{
				DBResult = "invalid";
				UnityEngine.Debug.LogError ("LMSIntegration.LMSLogin() : download.text is null!");
			}
			else
			{
				JSONObject decoder = new JSONObject(download.text);
				if ( CheckReturn(decoder) == true )
				{
					JSONObject authkey = decoder.GetField ("authKey");
					if ( authkey != null )
					{
						if ( authkey.print () != "null" )
						{
							DBResult = "ok";
							UnityEngine.Debug.LogError ("LMSIntegration.LMSLogin() : authKey=" + authkey.print () + " : valid");
						}
						else
						{
							DBResult = "invalid";
							UnityEngine.Debug.LogError ("LMSIntegration.LMSLogin() : authKey=" + authkey.print () + " : not valid");
						}
					}
					else
					{
						DBResult = "invalid";
						UnityEngine.Debug.LogError ("LMSIntegration.LMSLogin() : username=<" + username + "> password=<" + password + "> result=" + download.text);
					}
				}
				else
				{
					DBResult = "invalid";
					UnityEngine.Debug.LogError ("LMSIntegration.LMSLogin() : username=<" + username + "> password=<" + password + "> result=" + download.text);
				}
			}
			// save the results
			DBErrorString = "";
			// do callback
			if (Callback != null)
				Callback(true, DBResult, DBErrorString, download);
		}
	}

	//
	// LMS Login with Ping
	// Keeps connection open and saves authKey
	// args: username, password, callback
	//
	// NOTE: Starts a LMSPing coroutine which keeps getting called at the ping rate below
	//

	public float PingRate=60.0f;

	public void LMSLoginWithPing( string username, string password, DatabaseMgr.Callback Callback )
	{
		UnityEngine.Debug.Log ("LMSLoginWithPing(" + username + ")");
		// start off assuming logon is invalid
		LMSLogout ();	
		// start login coroutine
		StartCoroutine(lmsLoginWithPing(username,password,Callback));
	}

	IEnumerator lmsLoginWithPing( string username, string password, DatabaseMgr.Callback Callback )
	{
		//string URL="http://192.168.12.148/api3/corwin/r2d0/statefull/auth/";
		//string bodyString = "{\"action\":\"NoOrg\",\"params\":{\"username\":\"" + username + "\",\"password\":\"" + password + "\"},\"authKey\":null}";
		
		string url = "http://" + URL + "statefull/auth";
		//string bodyString = "{\"action\":\"User\",\"params\":{\"user_name\":\"" + username + "\",\"password\":\"" + password + "\"},\"authKey\":null}";
		
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "NoOrg");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("username",username);
		arr.AddField ("password", password );
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);

		// get bodystring
		string bodyString = j.print();
		
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString));

		yield return download;
		
		//Application.ExternalCall("Debug","LMSIntegration.lmsLoginWithPing() : URL=" + url + " : bodyString=" + bodyString + " : text=" + download.text + " : error=" + download.error );
	
		string DBResult="";
		string DBErrorString;
		
		if (download.error != null)
		{
			LMSDebug ("LMSIntegration.lmsLoginWithPing(" + username + "," + password + ") url=<" + url + "> bodyString=<" + bodyString + "> error=<" + download.error + ">"); 
			// save the error
			DBResult = null;
			DBErrorString = download.error;
			// do callback
			if (Callback != null)
				Callback(false, DBResult, DBErrorString, download);
			// do global callback
			if (DatabaseMgr.GetInstance() != null && DatabaseMgr.GetInstance().ErrorCallback != null )
				DatabaseMgr.GetInstance().ErrorCallback(false,DBResult,DBErrorString,download);
		}
		else
		{
			LMSDebug ("LMSIntegration.lmsLoginWithPing(" + username + "," + password + ") url=<" + url + "> bodyString=<" + bodyString + "> text=<" + download.text + ">");			
			if ( download.text == null )
			{
				DBResult = "invalid";
				UnityEngine.Debug.LogError ("LMSIntegration.LMSLoginWithPing() : download.text is null!");
			}
			else
			{
				JSONObject decoder = new JSONObject(download.text);
				if ( CheckReturn(decoder) == true )
				{
					JSONObject authkey = decoder.GetField ("authKey");
					if ( authkey != null )
					{
#if !UNITY_WEBPLAYER
						// get response headers
						GetCookies(download);
#endif
						// get authkey
						pingAuthKey = authkey.print ();
						LMSPing (pingAuthKey);
						DBResult = "ok";
					}
					else
					{
						DBResult = "invalid";
						UnityEngine.Debug.LogError ("LMSIntegration.LMSLoginWithPing() : username=<" + username + "> password=<" + password + "> result=" + download.text);
					}
				}
				else
				{
					DBResult = "invalid";
					UnityEngine.Debug.LogError ("LMSIntegration.LMSLoginWithPing() : username=<" + username + "> password=<" + password + "> result=" + download.text);
				}
			}
			// save the results
			DBErrorString = "";
			// do callback
			if (Callback != null)
				Callback(true, DBResult, DBErrorString, download);
		}
	}
	
	// 
	// Get Cookies from header
	//
	static string cookieName="Cookie";
	Dictionary<string,string> pingHeaders=new Dictionary<string, string>();
	public Dictionary<string,string> PingHeaders
	{
		get { return pingHeaders; }
	}

#if !UNITY_WEBPLAYER
	void GetCookies( WWW download )
	{
		pingHeaders = new Dictionary<string,string>();
		Dictionary<string,string> dict = download.responseHeaders;
		if ( dict.ContainsKey("SET-COOKIE") )
		{
			pingHeaders.Add (cookieName,dict["SET-COOKIE"]);
			UnityEngine.Debug.LogError("LMSIntegration.GetCookies() : found cookie=" + dict["SET-COOKIE"] + " : new dict=" + pingHeaders[cookieName]);
		}
	}
#else
	public void SetCookies(string headers)
	{
		pingHeaders = new Dictionary<string,string>();
		pingHeaders.Add(cookieName,headers);
		LMSDebug("LMSIntegration.SetCookies(" + pingHeaders[cookieName] + ")");
	}
#endif

	void DebugPingHeaders()
	{
		if ( pingHeaders.ContainsKey(cookieName) )
			UnityEngine.Debug.LogError("LMSIntegration.DebugPingHeaders() : new dict=" + pingHeaders[cookieName]);
	}
	
	string pingAuthKey="none";

	public string AuthKey
	{
		get { return pingAuthKey; }
		set { pingAuthKey=value; }
	}

	public bool LMSIsValidLogin()
	{
		return (pingAuthKey != "none");
	}
	
	public void LMSLogout()
	{
		AuthKey = "none";
	}
	
	public void LMSPing( string authkey=null )
	{
		// set key
		if ( authkey != null )
			pingAuthKey = authkey;
		// only start pinging if we have a valid login
		if ( LMSIsValidLogin() == true )
			StartCoroutine(lmsPing(pingAuthKey));
	}

	IEnumerator lmsPing( string authkey )
	{
		//string bodyString = "{\"action\":\"Ping\",\"params\":null,\"authKey\":" + authkey + "}";
		
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "Ping");
		// make param area
		j.AddField("params", "null");
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();
		
		// Create a download object
		string url = "http://" + URL + "statefull/get";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;

		LMSDebug ("LMSIntegration.lmsPing(" + authkey + ") text=<" + download.text + "> error=<" + download.error + ">");
		DebugPingHeaders();
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
		}
		else
		{
			DBResult = "ok";
		}
		
		yield return new WaitForSeconds(PingRate);
		
		LMSPing ();
	}

	public static class UserInfo
	{
		public static string FirstName;
		public static string LastName;
		public static string Email;
		public static string Org;
		public static bool Valid;

		public static string PrettyPrint()
		{
			return "first=" + FirstName + " : last=" + LastName + " : Email=" + Email + " : Org=" + Org;
		}
	}

	public delegate void UserInfoCallback( bool status, string data );

	public void LMSGetUserInfo( UserInfoCallback callback=null )
	{
		StartCoroutine(lmsGetUserInfo(callback));
	}

	IEnumerator lmsGetUserInfo( UserInfoCallback callback )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "User_Info");
		// make param area
		j.AddField("params", "null");
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();

		LMSDebug("lmsGetUserInfo: bodyString=[" + bodyString +"]");
		
		// Create a download object
		string url = "http://" + URL + "statefull/get";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;
		
		if (download.error != null)
		{
			LMSDebug("lmsGetUserInfo: download.error=" + download.error);

			// save the error
			if ( callback != null )
				callback(false,download.error);
			UserInfo.Valid = false;
		}
		else
		{
			// decode
			// "{"action":"response","params":{"fname":"Itay","lname":"Moav","email":"itay.moav@email.sitel.org","current_org":"eonflux"},"authKey":"c1nsqupar527qcjhc0s5m93le5","dbgenv":"192.168.12.148"}"			
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);
				UserInfo.FirstName = N["params"]["fname"].ToString(); 
				UserInfo.LastName = N["params"]["lname"].ToString(); 
				UserInfo.Email = N["params"]["email"].ToString();
				UserInfo.Org = N["params"]["current_org"].ToString();
				UserInfo.Valid = true;
				LMSDebug("lmsGetUserInfo: UserInfo.Valid=" + LMSIntegration.UserInfo.Valid);
				if ( callback != null )
					callback(true,download.text);
			}
			else
			{
				LMSDebug("lmsGetUserInfo: download.text=" + download.text);
				if ( callback != null )
					callback(false,download.text);
			}
		}
	}

	//
	//
	// LMS Get Courses Using Content
	// args: contentID, callback
	//
	// Gets a List<LMSCourseInfo> of courses used by the content ID
	//

	public delegate void LMSCourseInfoCallback( bool status, string result, string contentID, List<LMSCourseInfo> LMSCoursesUsingContent, WWW download );

	// lmsGetCoursesUsingContent("s9e1sdo6dhvh1j8851sjh6igd6") text=<{"action":"response","params":{"adopting_courses":{"15176":[{"course_enrollment_id":"1232858","enrollment_date":"2014-07-11 15:58:40","course_status":"complete"}],"15211":[],"15333":[],"15220":[],"15221":[],"15256":[],"15258":[],"15257":[],"15336":[],"15345":[],"15306":[],"15373":[],"15383":[],"15387":[],"15402":[],"15403":[],"15409":[],"15420":[],"15423":[],"15636":[],"15637":[],"15638":[],"15956":[],"15980":[]}},"authKey":null}> error=<>
	// assumes we logged in and have valid authkey
	public void LMSGetCoursesUsingContent( string contentID, LMSCourseInfoCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSGetCoursesUsingContent(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,error,contentID,null,null);
		}
		else
			StartCoroutine(lmsGetCoursesUsingContent(contentID, callback));
	}

	public class LMSCourseInfo
	{
		public string CourseID;
		public int EnrollmentID;
		public string EnrollmentDate;
		public string CourseStatus;

		public LMSCourseInfo(string course, string id, string date, string status )
		{
			int result=0;

			CourseID = course;
			Int32.TryParse(id,out result);
			EnrollmentID = result;

			EnrollmentDate = date;
			CourseStatus = status;
		}

		string debugString;
		public string DebugString
		{
			get {
				debugString = "course<" + CourseID + "> enrollmentID<" + EnrollmentID + "> enrollmentDate<" + EnrollmentDate + "> status<" + CourseStatus + ">";
				return debugString;
			}
		}

		public void PrettyPrint()
		{
			UnityEngine.Debug.LogError(debugString);
		}
	}

	public List<LMSCourseInfo> LMSCoursesUsingContent;

	IEnumerator lmsGetCoursesUsingContent( string contentID, LMSCourseInfoCallback callback  )
	{
		//string bodyString = "{\"action\":\"Catalog_ContentAdoptingCourses\",\"params\":{\"content_id\":\"" + contentID + "\"},\"authKey\":" + pingAuthKey + "}";

		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "Catalog_ContentAdoptingCourses");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("content_id",contentID);
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();
		
		// Create a download object
		string url = "http://" + URL + "statefull/get";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;

		LMSDebug ("lmsGetCoursesUsingContent(" + contentID +")");
		LMSDebug ("lmsGetCoursesUsingContent(" + pingAuthKey + ") text=<" + download.text + "> error=<" + download.error + "> bodystring=<" + bodyString + ">");

		string DBResult;
		string DBErrorString;

		// create new list
		LMSCoursesUsingContent = new List<LMSCourseInfo>();
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			if ( callback != null )
				callback(false,"error",contentID,null,download);
		}
		else
		{
			DBResult = "ok";
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);
				// extract courses
				if ( N["params"] != null )
				{
					JSONClass tryme = N["params"]["adopting_courses"] as JSONClass;
					if ( tryme != null )
					{
						for( int i=0 ; i<tryme.Count ; i++)
						{
							JSONNode itemNode = N["params"]["adopting_courses"][i];
							if ( itemNode.Count > 0 )
							{
								LMSCoursesUsingContent.Add (new LMSCourseInfo(contentID/*tryme.Key(i)*/,itemNode[0]["course_enrollment_id"].ToString().Replace("\"",""),itemNode[0]["enrollment_date"].ToString().Replace("\"",""),itemNode[0]["course_status"].ToString().Replace("\"","")));
								#if DEBUG_LIST
								UnityEngine.Debug.LogError("key=" + tryme.Key(i) + " item=" + itemNode.ToString () + " count=" + itemNode.Count);
								UnityEngine.Debug.LogError("course_enrollment_id=" + itemNode[0]["course_enrollment_id"].ToString().Replace("\"",""));
								UnityEngine.Debug.LogError("enrollment_date=" + itemNode[0]["enrollment_date"].ToString().Replace("\"",""));
								UnityEngine.Debug.LogError("course_status=" + itemNode[0]["course_status"].ToString().Replace("\"",""));
								UnityEngine.Debug.LogError(">>>>");
								#endif
							}
						}			
					}
					else
						UnityEngine.Debug.LogError ("lmsGetCoursesUsingContent() : tryme==null");
				}
				else
					UnityEngine.Debug.LogError ("lmsGetCoursesUsingContent() : N[params]==null");
				// do callback
				if ( callback != null )
				{
					// if we have courses then send back list otherwise return null
					if ( LMSCoursesUsingContent.Count > 0 )
						callback(true,"enrolled",contentID,LMSCoursesUsingContent,download);
					else
						callback(true,"not_enrolled",contentID,null,download);
				}
			}
			else
			{
				if ( callback != null )
					callback(false,"error",contentID,null,download);
			}
		}
	}

	//
	// LMS Get User Enrollment
	// args: contentID and callback
	// NOTE: sets only last item in list complete
	//

	public void LMSGetUserEnrollment( string contentID, LMSCourseInfoCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSGetUserEnrollment(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,error,contentID,null,null);
		}
		else
			StartCoroutine(lmsGetUserEnrollment(contentID,callback));
	}
	
	IEnumerator lmsGetUserEnrollment( string contentID, LMSCourseInfoCallback callback  )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "User_Enrollments");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("content_id",contentID);
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();
		
		// Create a download object
		string url = "http://" + URL + "statefull/get";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;
		
		LMSDebug ("lmsGetUserEnrollment(" + contentID +")");
		LMSDebug ("lmsGetUserEnrollment(" + pingAuthKey + ") text=<" + download.text + "> error=<" + download.error + "> bodystring=<" + bodyString + ">");
		
		string DBResult;
		string DBErrorString;
		
		// create new list
		LMSCoursesUsingContent = new List<LMSCourseInfo>();
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			if ( callback != null )
				callback(false,"error",contentID,null,download);
		}
		else
		{
			DBResult = "ok";
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);				
				JSONClass tryme = N["params"] as JSONClass; 
				if ( tryme != null && tryme.Count > 0 )
				{
					//string status=tryme[0];
					//string enrollment_id=tryme.Key(0);
					// this is new method syntax
					string status=tryme["course_status"];
					string enrollment_id=tryme["enrollment_id"];
					LMSCoursesUsingContent.Add (new LMSCourseInfo(contentID,enrollment_id.Replace("\"",""),"",status.Replace("\"","")));
				}
				// do callback
				if ( callback != null )
				{
					// if we have courses then send back list otherwise return null
					if ( LMSCoursesUsingContent.Count > 0 )
						callback(true,"enrolled",contentID,LMSCoursesUsingContent,download);
					else
						callback(true,"not_enrolled",contentID,null,download);
				}
			}
			else
			{
				var N = JSONNode.Parse(download.text);				
				string code = N["params"]["code"].ToString ().Replace ("\"",""); 
				string msg = N["params"]["msg"].ToString ().Replace ("\"",""); 
				LMSDebug("lmsGetUserEnrollment(" + contentID +") : code=<" + code + "> : msg=<" + msg + ">");
				// no enrollments, return reason why
				if ( callback != null )
					callback(true,msg,contentID,null,download);
			}
		}
	}

	//
	// LMS Set Content Complete
	// args: contentID, List<LMSCourseInfo>, and callback
	// NOTE: sets only last item in list complete
	//

	public delegate void LMSSimpleCallback( bool status, string contentID, string result, WWW download );

	public void LMSSetContentComplete( string contentID, List<LMSCourseInfo> list, LMSSimpleCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSGetCoursesUsingContent(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,contentID,error,null);
		}
		else
		{
#if SET_COMPLETE_ON_ALL_RECORDS
			// this sends a content complete to all content registered
			foreach( LMSCourseInfo item in list )
			{
				LMSSetContentComplete(contentID,item.EnrollmentID,callback);
			}
#else
			// this only sends content complete to last record, because
			// that is the most up to date record
			if ( list != null && list.Count > 0 )
				LMSSetContentComplete(contentID,list[list.Count-1].EnrollmentID,callback);
#endif
		}
	}

	public void LMSSetContentComplete( string contentID, int enrollment_id, LMSSimpleCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSSetContentComplete(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,contentID,error,null);
		}
		else
			StartCoroutine(lmsSetContentComplete(contentID,enrollment_id,callback));
	}
	
	IEnumerator lmsSetContentComplete( string contentID, int enrollment_id, LMSSimpleCallback callback )
	{
		//string bodyString = "{\"action\":\"Curriculum_CompleteContent\",\"params\":{\"content_id\":\"" + contentID + "\"," + "\"enrollment_id\":" + enrollment_id + "},\"authKey\":" + pingAuthKey + "}";

		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "Curriculum_CompleteContent");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("content_id",contentID);
		arr.AddField ("enrollment_id", enrollment_id );
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();

		LMSDebug ("lmsSetContentComplete(" + contentID + "," + enrollment_id + ") : bodystring=" + bodyString);

		// Create a download object
		string url = "http://" + URL + "statefull/post";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;

		LMSDebug ("lmsSetContentComplete(" + pingAuthKey + ") text=<" + download.text + "> error=<" + download.error + ">");
		
		string DBResult;
		string DBErrorString;

		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			if ( callback != null )
				callback(false,contentID,"",download);
		}
		else
		{
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);
				string result = N["params"]["mark_start_flag"].ToString(); 
				string msg = "lmsSetContentComplete() : mark_start_flag=" + result;
				LMSDebug (msg);
			
				DBResult = "ok";
				if ( callback != null )
					callback(true,contentID,msg,download);
			}
			else
			{
				if ( callback != null )
					callback(false,contentID,download.text,download);
			}
		}
	}

	//
	// LMS Launch Content
	// args: contentID, List<LMSCourseInfo>, callback
	// NOTE: sets last contentID complete
	//
	//

	public void LMSLaunchContent( string contentID, List<LMSCourseInfo> list, LMSSimpleCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSLaunchContent(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,contentID,error,null);
		}
		else
		{
			if ( list != null )
			{
#if SET_LAUNCH_ON_ALL_RECORDS
				foreach( LMSCourseInfo item in list )
				{
					LMSLaunchContent(contentID,item.EnrollmentID,callback);
				}
#else
				if ( list.Count > 0 )
					LMSLaunchContent(contentID,list[list.Count-1].EnrollmentID,callback);
#endif
			}
		}
	}
	
	public void LMSLaunchContent( string contentID, int enrollment_id, LMSSimpleCallback callback )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSLaunchContent(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,contentID,error,null);
		}
		else
			StartCoroutine(lmsLaunchContent(contentID,enrollment_id,callback));
	}
	
	IEnumerator lmsLaunchContent( string contentID, int enrollment_id, LMSSimpleCallback callback )
	{
		//string bodyString = "{\"action\":\"Curriculum_LaunchContent\",\"params\":{\"content_id\":\"" + contentID + "\"," + "\"enrollment_id\":" + enrollment_id + "},\"authKey\":" + pingAuthKey + "}";
		
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "Curriculum_LaunchContent");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("content_id",contentID);
		arr.AddField ("enrollment_id", enrollment_id );
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();

		LMSDebug ("lmsLaunchContent(" + contentID + "," + enrollment_id + ") : bodystring=" + bodyString);
		
		// Create a download object
		string url = "http://" + URL + "statefull/post";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;

		LMSDebug ("lmsLaunchContent(" + pingAuthKey + ") text=<" + download.text + "> error=<" + download.error + ">");
		DebugPingHeaders();
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			
			if ( callback != null )
				callback(true,contentID,"",download);
		}
		else
		{
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);
				string result = N["params"]["mark_start_flag"].ToString(); 
				string msg = "lmsLaunchContent() : mark_start_flag=" + result;
				LMSDebug (msg);

				if ( callback != null )
					callback(true,contentID,msg,download);
			}
			else
			{
				if ( callback != null )
					callback(false,contentID,download.text,download);
			}
			
			DBResult = "ok";
		}
	}

	//
	// LMS Enroll Content
	// args: contentID, callback
	// NOTE: enrolls current user in contentID
	//


	//"{"action":"Curriculum_EnrollToContent","params":{"content_id":3645},"authKey":"qedr4rm7gddo5russ0hh0m9gd3"}"
	public void LMSEnrollContent( string contentID, LMSSimpleCallback callback=null )
	{
		if ( LMSIsValidLogin() == false )
		{
			string error = "LMSEnrollContent(" + contentID + ") authkey not set or not valid!";
			UnityEngine.Debug.LogError(error);
			if ( callback != null )
				callback(false,contentID,error,null);
		}
		else
			StartCoroutine(lmsEnrollContent(contentID,callback));
	}

	IEnumerator lmsEnrollContent( string contentID, LMSSimpleCallback callback )
	{
		//string URL="http://192.168.12.148/api3/corwin/r2d0/statefull/post/";
		//string bodyString = "{\"action\":\"Curriculum_EnrollToContent\",\"params\":{\"content_id\":\"" + contentID + "\"},\"authKey\":" + pingAuthKey + "}";

		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "Curriculum_EnrollToContent");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("content_id",contentID);
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get bodystring
		string bodyString = j.print();

		LMSDebug ("LMSIntegration.lmsEnrollContent(" + contentID + ") : bodystring=" + bodyString);
		
		// Create a download object
		string url = "http://" + URL + "statefull/post";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders);
		yield return download;

		LMSDebug ("LMSIntegration.lmsEnrollContent(" + pingAuthKey + ") text=<" + download.text + "> error=<" + download.error + ">");
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			if ( callback != null )
				callback(false,contentID,"",download);
		}
		else
		{
			DBResult = "ok";
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				var N = JSONNode.Parse(download.text);
				string id = N["params"]["enrollment_id"].ToString(); 
				string flag = N["params"]["enrollment_flag"].ToString();
				string msg = "lmsEnrollContent() : enrollment_id=" + id + " : enrollment_flag=" + flag;
				LMSDebug (msg);
			
				if ( callback != null )
					callback(true,contentID,msg,download);
			} 
			else
			{
				if ( callback != null )
					callback(false,contentID,download.text,download);
			}
		}
	}

	//
	// LMS AppInfo
	// args: appid(zoll), device(ipad), callback
	// NOTE: gets the current app version and update flag
	//
	//

	public delegate void LMSVersionUpdateCallback( bool status, string version, string update, string path, WWW download );

	//$obj->action='ApplicationVersion';
	//$obj->params=['app_id'=>'zoll','device'=>'ipad'];
	//$obj->authKey='';
	//"{"action":"Curriculum_EnrollToContent","params":{"content_id":3645},"authKey":"qedr4rm7gddo5russ0hh0m9gd3"}"
	public void LMSAppInfo( string appID, string deviceID, LMSVersionUpdateCallback callback )
	{
		StartCoroutine(lmsAppInfo(appID,deviceID,callback));
	}
	
	IEnumerator lmsAppInfo( string appID, string deviceID, LMSVersionUpdateCallback callback )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "ApplicationVersion");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("app_id",appID);
		arr.AddField ("device",deviceID);
		// add authkey
		j.AddField ("authKey",MedstarNow_ActivationCode);
		// get the string
		string bodyString = j.print();

		LMSDebug ("LMSIntegration.lmsAppInfo(" + appID + "," + deviceID + ") : bodystring=<" + bodyString + ">");
		
		// Create a download object
		WWW download = new WWW(APPINFO_URL,Encoding.ASCII.GetBytes(bodyString),pingHeaders); 
		yield return download;
		
		string DBResult;
		string DBErrorString;

		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			// decode return
			JSONObject decoder = new JSONObject(download.text);
			JSONObject msg = decoder.GetField ("msg");
			if ( msg != null )
				UnityEngine.Debug.LogError("LMSIntegration.lmsAppInfo() : error=" + download.error + " msg=" + msg.print());
			else
				UnityEngine.Debug.LogError("LMSIntegration.lmsAppInfo() : error=" + download.error);
			// bad return
			if ( callback != null )
				callback(false,"error","error","none",download);
		}
		else
		{
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				JSONObject p = decoder.GetField ("params");
				if ( p != null )
				{
					JSONObject version = p.GetField ("appver");
					JSONObject update = p.GetField ("force_update");
					string msg = "LMSIntegration.lmsAppInfo() : version=" + version.print () + " : update=" + update.print ();
					LMSDebug (msg);
					// ok return
					if ( callback != null )
					{
						callback(true,version.print(true).Replace ("\"",""),update.print(true).Replace ("\"",""),"",download);
					}
				}
			}
			else
			{
				// probably an error, show it
				JSONObject p = decoder.GetField ("params");
				JSONObject msg = p.GetField("msg");
				if ( msg != null )
				{
					UnityEngine.Debug.LogError("LMSIntegration.lmsAppInfo() : msg=" + msg.print ());
					// bad return
					if ( callback != null )
						callback(false,msg.print(),download.text,"none",download);
				}
			}


			DBResult = "ok";

		}
	}

	//
	// LMS Load Data
	// args: path, callback
	// NOTE: gets the data stored in the path defined as a normal path </path/filename>
	//

	public delegate void LMSLoadDataCallback( List<string> data, WWW download );

	public void LMSLoadData( string path, LMSLoadDataCallback callback )
	{
		StartCoroutine(lmsLoadData(path,callback));
	}
	
	IEnumerator lmsLoadData( string path, LMSLoadDataCallback callback )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "AppData_DataDump");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("key",path);
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get the string
		string bodyString = j.print();

		LMSDebug ("LMSIntegration.lmsLoadData(" + path + ") : bodystring=<" + bodyString + ">");
		
		// Create a download object
		string url = "http://" + URL + "statefull/get";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders); 
		yield return download;
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null) 
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			// decode return
			JSONObject decoder = new JSONObject(download.text);
			JSONObject msg = decoder.GetField ("msg");
			UnityEngine.Debug.LogError("LMSIntegration.lmsLoadData() : error, msg=" + msg.print());
			// bad return
			if ( callback != null )
				callback(null,download); 
		}
		else
		{
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				JSONObject p = decoder.GetField ("params");
				if ( p != null && p.list != null )
				{
					List<string> returnData = new List<string>();
					foreach( JSONObject o in p.list )
					{
						JSONObject k = o.GetField("key");
						JSONObject d = o.GetField("data");
						UnityEngine.Debug.LogError("key=" + k.print() + " : data=" + d.print());
						returnData.Add (d.print());
					}
					if ( callback != null )
						callback(returnData,download);
				}
			}
			else
			{
				// probably an error, show it
				JSONObject p = decoder.GetField ("params");
				JSONObject msg = p.GetField("msg");
				if ( msg != null )
				{
					UnityEngine.Debug.LogError("LMSIntegration.lmsLoadData() : msg=" + msg.print ());
					// bad return
					if ( callback != null )
						callback(null,download);
				}
			}
			
			DBResult = "ok";
			
		}
	}
	
	//
	// LMS Save Data
	// args: path, callback
	// NOTE: saves the data stored in the path defined as a normal path </path/filename>
	//
	
	public void LMSSaveData( string path, string data, LMSSimpleCallback callback )
	{
		StartCoroutine(lmsSaveData(path,data,callback));
	}
	
	IEnumerator lmsSaveData( string path, string data, LMSSimpleCallback callback )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		//number
		j.AddField("action", "AppData_SaveDump");
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add params
		arr.AddField ("key",path);
		arr.AddField ("data",data);
		// add authkey
		string keyNoQuotes = pingAuthKey.Replace ("\"","");
		j.AddField ("authKey",keyNoQuotes);
		// get the string
		string bodyString = j.print();

		LMSDebug ("LMSIntegration.lmsSaveData(" + path + "," + data + ") : bodystring=<" + bodyString + ">");
		
		// Create a download object
		string url = "http://" + URL + "statefull/post";
		WWW download = new WWW(url,Encoding.ASCII.GetBytes(bodyString),pingHeaders); 
		yield return download;
		
		string DBResult;
		string DBErrorString;
		
		if (download.error != null)
		{
			// save the error
			DBResult = "";
			DBErrorString = download.error;
			// decode return
			JSONObject decoder = new JSONObject(download.text);
			JSONObject msg = decoder.GetField ("msg");
			UnityEngine.Debug.LogError("LMSIntegration.lmsSaveData() : error, msg=" + msg.print());
			// bad return
			if ( callback != null )
				callback(false,path,"",download);
		}
		else
		{
			// decode
			JSONObject decoder = new JSONObject(download.text);
			if ( CheckReturn(decoder) == true )
			{
				// ok return
				if ( callback != null )
					callback(true,path,"transfer ok",download);
			}
			else
			{
				// probably an error, show it
				JSONObject p = decoder.GetField ("params");
				JSONObject msg = p.GetField("msg");
				if ( msg != null )
				{
					UnityEngine.Debug.LogError("LMSIntegration.lmsSaveData() : msg=" + msg.print ());
					// bad return
					if ( callback != null )
						callback(false,path,msg.print(),download);
				}
			}
					
			DBResult = "ok";
			
		}
	}
	
	// 
	// LMS ENROLL & LAUNCH
	// this is a wrapper to handle checking enrollment, enrolling if not enrolled, and then launching the content
	//
	void CourseListCallback( bool status, string result, string contentID, List<LMSIntegration.LMSCourseInfo> list, WWW download )
	{
		if ( status == true )
		{
			// check list count
			if ( list == null || list.Count == 0 )
			{
				// list count is 0 which means the user is not enrolled, kick off enrollment
				// user is enrolled at least once so we can just launch
				LMSDebug ("LMSIntegration.CourseListCallback() : no courses, call LMSEnrollContent()!");
				LMSIntegration.GetInstance().LMSEnrollContent(contentID,EnrollContentCallback);
			}
			else
			{
				// user is enrolled at least once so we can just launch
				LMSDebug ("LMSIntegration.CourseListCallback() : we have courses, call LMSLaunchContent()!");
				LMSIntegration.GetInstance().LMSLaunchContent(contentID,list,LaunchContentCallback);
			}
		}
		else
		{
			// we're done if something goes wrong
			if ( startContentCallback != null )
				startContentCallback(status,contentID,result,download);
			UnityEngine.Debug.LogError ("LMSIntegration.CourseListCallback() : error getting course list=" + result);
		}
	}	
	
	void EnrollContentCallback( bool status, string contentID, string result, WWW download )
	{
		if ( status == true )
		{
			// enrollment went ok, get course list again
			LMSDebug ("LMSIntegration.EnrollContentCallback() : enroll ok, call LMSGetCoursesUsingContent()!");
			LMSIntegration.GetInstance().LMSGetUserEnrollment(contentID,CourseListCallback);
			//LMSIntegration.GetInstance().LMSGetCoursesUsingContent(contentID,CourseListCallback);
		}
		else
		{
			// we're done if something goes wrong
			if ( startContentCallback != null )
				startContentCallback(status,contentID,result,download);
			UnityEngine.Debug.LogError ("LMSIntegration.EnrollContentCallback() : error enrolling=" + result);
		}
	}
	
	void LaunchContentCallback( bool status, string contentID, string result, WWW download )
	{
		if ( status == true )
		{
			LMSDebug ("LMSIntegration.LaunchContentCallback() : content launched ok!");
		}
		else
			UnityEngine.Debug.LogError ("LMSIntegration.LaunchContentCallback() : error=" + result);

		// we're done, returns status
		if ( startContentCallback != null )
			startContentCallback(status,contentID,result,download);
	}
	
	// store callback
	LMSSimpleCallback startContentCallback;

	// this method kicks off a chain of callbacks which...
	// 1. get course list
	// 2. IF course list is empty (no enrollments) THEN enroll user and start content ELSE just start the content
	public void LMSStartContent( string contentID, LMSSimpleCallback callback=null )
	{
		// set callback
		startContentCallback = callback;
		// auto enroll this user		
		LMSDebug ("LMSIntegration.LMSStartContent() : first see if user is enrolled, calling LMSGetCoursesUsingContent()");
		LMSIntegration.GetInstance().LMSGetUserEnrollment(contentID,CourseListCallback);
	}

	// store callback
	LMSSimpleCallback contentCompleteCallback;
	
	void SetCompleteCallback( bool status, string result, string contentID, List<LMSIntegration.LMSCourseInfo> list, WWW download )
	{
		if ( status == true )
		{
			// everything ok, now call real set complete
			LMSSetContentComplete(contentID,list,contentCompleteCallback);
		}
		else
		{
			// we're done if something goes wrong
			if ( contentCompleteCallback != null )
				contentCompleteCallback(status,contentID,result,download);
			UnityEngine.Debug.LogError ("LMSIntegration.CourseListCallback2() : error getting course list=" + result);
		}
	}	
	
	//
	// LMSSetComplete(contentID)
	//
	// this method first needs to make sure course list is valid and then call LMSSetContentComplete()
	//
	public void LMSSetComplete( string contentID, LMSSimpleCallback callback=null )
	{
		// set callback
		contentCompleteCallback = callback;
		// auto enroll this user		
		LMSDebug ("LMSIntegration.LMSStartContent() : first see if user is enrolled, calling LMSGetCoursesUsingContent()");
		LMSIntegration.GetInstance().LMSGetUserEnrollment(contentID,SetCompleteCallback);
	}


	// valid return

	bool CheckReturn( JSONObject decoder )
	{
		// check decoder object
		if ( decoder == null )
		{
			UnityEngine.Debug.LogError("CheckReturn Error: decoder string is NULL");
			return false;
		}
		// check that action == response
		JSONObject action = decoder.GetField("action");
		if ( action != null && action.print().Replace ("\"","") == "response" )
			return true;
		else
		{
			if ( action == null )
				UnityEngine.Debug.LogError("CheckReturn Error: can't find <action> in <" + decoder + ">");
			return false;
		}
	}

	public void LMSSendXApi( JSONObject j )
	{
	}

	public void LMSSendXApiList( JSONObject j )
	{
	}

	public void LMSSendXApiList( List<XApiLogItem> list )
	{
		// make JSON
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		// make param area
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("params", arr);
		// add all elements to params array
		foreach( XApiLogItem item in list )
		{
			arr.Add(item.ToJSON());
		}
		string result = j.print();
	}
}







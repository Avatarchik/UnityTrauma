#define DEBUG_DATABASE

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[RequireComponent (typeof (LMSIntegration))]
public class DatabaseMgr : MonoBehaviour
{
    private static DatabaseMgr instance;

    public DatabaseMgr() // don't use constructors for MonoBehavior derived classes, use Awake or Start to initialize
    {
        instance = this;
    }
	
	public void Awake(){
		instance=this;			
	}

    public static DatabaseMgr GetInstance()
    {
        if (instance == null)
        {
            //UnityEngine.Debug.LogError("DatabaseMgr.GetInstance() : Script not attached!");
            // instance = new DatabaseMgr();  can't use 'new' to create a component, use AddCOmponent
			instance = FindObjectOfType(typeof(DatabaseMgr)) as DatabaseMgr;
			if (instance == null){
				Brain brainComponent = FindObjectOfType(typeof(Brain)) as Brain;
				if (brainComponent != null){
					instance = brainComponent.gameObject.AddComponent<DatabaseMgr>();
				}
				else
				{
					UnityEngine.Debug.LogError("DatabaseMgr.GetInstance() : script not present!!");
				}
			}			
        }
        return instance;
    }

	public float DefaultTimeout=10.0f;
	
	List<DBCallInfo> DBRequests = new List<DBCallInfo>();
	List<DBCallInfo> DBRequestsActive = new List<DBCallInfo>();
	public DatabaseMgr.Callback ErrorCallback;

	public bool HasActiveRequests()
	{
		return ( DBRequestsActive.Count > 0 );
	}
	
	public class DBCallInfo
	{
		public string URL;
		public WWWForm Form;
		public DatabaseMgr.Callback Callback;
		public float elapsedTime;
		
		public DBCallInfo( string url, WWWForm form, DatabaseMgr.Callback callback )
		{
			URL = url;
			Form = form;
			Callback = callback;
		}		
		
		public IEnumerator Dispatch( List<DBCallInfo> activeList )
		{
#if DEBUG_DATABASE
			UnityEngine.Debug.Log("DBCallInfo.Dispatch() : URL=" + URL);
#endif
			// place this in the active list
			activeList.Add (this);
			//UnityEngine.Debug.LogError ("Dispatch add list");

		    // Create a download object
	        WWW download = new WWW(URL, Form);
	        elapsedTime = Time.time;
	
#if DEBUG_DATABASE
			Debug.Log("DBCallInfo.Dispatch() : Requesting WWW : " + URL); 
#endif
	
	        // Wait until the download is done
			bool timeout=false;

#if CONTROL_TIMEOUT
			while( download.isDone == false && timeout == false )
			{
				//UnityEngine.Debug.Log ("elapsedTime=" + (Time.time-elapsedTime).ToString ());
				if ( (Time.time-elapsedTime) > DatabaseMgr.GetInstance().DefaultTimeout )
				{
					timeout = true;
				}
				else
					yield return null;
			}
#else
			yield return download;
#endif

#if DEBUG_DATABASE
	        Debug.Log("DBCallInfo.Dispatch() : elapsedTime=" + (Time.time-elapsedTime).ToString());
#endif

			// remove from active list because we are finished
			activeList.Remove (this);
			//UnityEngine.Debug.LogError ("Dispatch remove list");

			string DBResult;
		    string DBErrorString;
			
	        if (download.error != null || timeout == true )
	        {
		        // save the error
	        	DBResult = "";
				DBErrorString = (timeout==false) ? download.error : "TIMEOUT ERROR!! : timeout=" + DatabaseMgr.GetInstance().DefaultTimeout;
#if DEBUG_DATABASE
				Debug.Log("DBResult (Error): " + DBErrorString);
#endif
				// do callback
	        	if (Callback != null)
		            Callback(false, DBResult, DBErrorString, download);
				// do global callback
				if (DatabaseMgr.GetInstance().ErrorCallback != null )
					DatabaseMgr.GetInstance().ErrorCallback(false,DBResult,DBErrorString,download);
	        }
	        else
	        {
		        // save the results
#if DEBUG_DATABASE

	        	Debug.Log("DBResult : " + download.text);
#endif
				// This STRING is also what is cahced in the offline asset object to mimic what the database returns...
				// so grab this and use it to initialize the offline versions.

	        	DBResult = download.text;
	        	DBErrorString = "";
	        	// do callback
	        	if (Callback != null)
		            Callback(true, DBResult, DBErrorString, download);
	        }
		}		
	}

	public void DBCall(string URL, WWWForm form, Callback callback)
	{
		DBCallInfo ci = new DBCallInfo (URL, form, callback);
		DBRequests.Add (ci);
	}
	
	public void HandleDBRequests()
	{
		if ( DBRequests.Count == 0 )
			return;
		
		foreach( DBCallInfo ci in DBRequests )
		{
			StartCoroutine(ci.Dispatch(DBRequestsActive));
		}
		DBRequests.Clear();
	}

    public void Update()
    {
		HandleDBRequests();		
    }

    public delegate void Callback(bool error, string data, string error_msg, WWW download);

}

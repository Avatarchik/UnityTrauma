using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public class CachedDBResult{
	public string command = "";
	public string param1 = "";
	public string param2 = "";
	public string data = ""; // the resulting string
}

[System.Serializable]
public class CachedMovieTexture{
	public string url = "";
#if !UNITY_IPHONE
	public MovieTexture movie = null;
#endif
}

public class TraumaOfflineAssetContainer : MonoBehaviour {

	// this script should live on a game object in a scene which is only included in standalone-capable builds, and should be
	// populated with all the assets needed for standalone.  Then, platform sensitive code should be run to load that scene additively

	// TODO  to initially populate these elements in the instance of this class (from a prefab) in the main menu level,
	// USE THE DATABASE, and grab the result, which is printed to the console window by the Database Manager on line 136 (or so)


	public bool bUseOfflineAssets = false;
	public List<CachedDBResult> OfflineDBResults;
	public List<CachedMovieTexture> CachedMovieTextures;

	// this container could contain all the video and texture assets needed to run while offline, as well as cached versions of the database results
	// that would be needed, in a format that could easily be accessed
	// it would be good if this could be used for both PC and Mobile standalone...

	static TraumaOfflineAssetContainer _instance= null;

	public static TraumaOfflineAssetContainer GetInstance(){ // if this returns null, then the offline asset scene has not been loaded, and we can't run offline
		if (_instance == null)
			_instance = FindObjectOfType<TraumaOfflineAssetContainer> ();
		return _instance;
	}

	void Awake(){

		DontDestroyOnLoad (this);
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// it looks like most of the classes using the DatabaseManager have callbacks that only use the string 'data' to process, so we could cache these strings
	// based on the parameter set passed in, like 'LoadCases','Owner', or 'LoadCase', 'caseName', and the string containing the data we want to use for offline session

	public void DBCallOffline(string URL, WWWForm form, DatabaseMgr.Callback callback){

		// if we can find a match in our list of cached responses, send it back
		// first, find the arguments
		string strFormData = Encoding.UTF8.GetString(form.data, 0, form.data.Length);  //"command=cmd&param=paramvalue"

		string[] fields = strFormData.Split ('&');
		string command = "";
		string param1 = "";
		string[] pair;
		if (fields [0].Contains ("=")) {
			pair = fields [0].Split ('=');
			command = pair [1];
			if (fields.Length > 1 && fields[1].Contains("=")){
				pair = fields [1].Split ('=');
				param1 = pair [1].Replace("+"," ");

			}
		}
		string data = "";
		foreach (CachedDBResult result in OfflineDBResults) {
			if (result.command == command && result.param1 == param1){
				data = result.data;
				break;
			}
		}

		// don't use coroutine here because the timescale might be 0 and
		// then we will never return...		
		//StartCoroutine(CallbackAfterDelay(callback,data)); 

		// do callback
		if ( callback != null )
			callback(true,data,"",null);
	}

	IEnumerator CallbackAfterDelay(DatabaseMgr.Callback callback, string data){

		yield return new WaitForSeconds(0.1f);
		callback(true, data, "", null); // could tell them we are offline...
	}

#if !UNITY_IPHONE
	public MovieTexture GetMovieTexture(string url){
		foreach (CachedMovieTexture check in CachedMovieTextures) {
			if (check.url == url) return check.movie;
		}
		return null;
	}
#endif
}

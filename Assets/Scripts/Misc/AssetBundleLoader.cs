using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// the reason for having a single instance handle the loading is to queue and coordinate the loading in stages,
// so the things that are needed first get loaded first.

// since different options might require the same bundle, the loader must also check for 'already requested' by bundle name

[System.Serializable]
public class AssetBundleInfo{		
	public string bundleName = "";
	public int stage;
/*
	private WWW myWww = null;
	public WWW www
    {
        get { return myWww; }
        set { myWww = value; }
    }	
*/
	public string assetName = ""; // used if only one asset is needed
}

public class AssetBundleLoader : MonoBehaviour {
	
	static AssetBundleLoader instance = null;
	public bool useLocal = true; // local files or url ?
	public bool useCache = true;
	public int currentStage = 0;
	public string localPath = "AssetBundles/";
	public string urlPath = "http://somewhereinthecloud.com/wheretheassetsare/";
	
	public List<AssetBundleInfo> loading;
	public List<AssetBundleInfo> waiting;
	
    public static AssetBundleLoader GetInstance()
    {
		if (instance == null){
			instance = FindObjectOfType(typeof(AssetBundleLoader)) as AssetBundleLoader;
			if (instance == null){ // no AssetBundleLoader in the level, add one
				GameObject dgo = new GameObject("AssetBundleLoader");
				instance = dgo.AddComponent<AssetBundleLoader>();
			}
		}
  	    return instance;
    }	

	// Use this for initialization
	void Start () {
		loading = new List<AssetBundleInfo>();
		waiting = new List<AssetBundleInfo>();
		localPath = "file://C:"+Application.dataPath+"/"+localPath;	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void Load(AssetBundleInfo info){
		// for now, just start loading this immediately
		// later we will queue later stage requests
		
		if (info.bundleName == "") return;
		
		loading.Add (info);
		
		if (useCache)
			StartCoroutine(DownloadAndCache(info));
		else
			StartCoroutine(DownloadNoCache(info));
		
		// once the bundle is instantiated, it needs to link itself up, load animations, assign scripts, etc...
		// best to let the bundle main asset contain the appropriate scripts to perform those operations
		
	}
	
	
	// sample unity recommended code:
	
	IEnumerator DownloadNoCache(AssetBundleInfo info) {
	   // Download the file from the URL. It will not be saved in the Cache
		string path;
		if (useLocal)
			path = localPath+info.bundleName+".unity3d";
		else
			path = urlPath+info.bundleName+".unity3d";
		
		
	   using (WWW www = new WWW(path)) {
		   yield return www;
		   if (www.error != null)
			   throw new Exception("WWW download had an error:" + www.error);
		   AssetBundle bundle = www.assetBundle;
		   if (info.assetName == "")
			   Instantiate(bundle.mainAsset);
		   else
			   Instantiate(bundle.Load(info.assetName));
                   // Unload the AssetBundles compressed contents to conserve memory
                   bundle.Unload(false);
	   }
   }
	
	IEnumerator DownloadAndCache (AssetBundleInfo info){
		// Wait for the Caching system to be ready
		while (!Caching.ready)
			yield return null;
		
		int version = 0;
		
		// Load the AssetBundle file from Cache if it exists with the same version or download and store it in the cache
		string path;
		if (useLocal)
			path = localPath+info.bundleName+".unity3d";
		else
			path = urlPath+info.bundleName+".unity3d";
		
		using(WWW www = WWW.LoadFromCacheOrDownload (path, version)){
			yield return www;
			if (www.error != null)
				throw new Exception("WWW download had an error:" + www.error);
			AssetBundle bundle = www.assetBundle;
			if (info.assetName == "")
				Instantiate(bundle.mainAsset);
			else
				Instantiate(bundle.Load(info.assetName));
                	// Unload the AssetBundles compressed contents to conserve memory
                	bundle.Unload(false);
		}
	}
	
	
	
}

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

public class AssetData
{
    static AssetData instance;
    public static AssetData GetInstance()
    {
        if (instance == null)
            instance = new AssetData();
        return instance;
    }

    public string mydata;
}

public class AssetLoader : MonoBehaviour
{
    WWW www;
    AsyncOperation op;
    Dictionary<string, WWW> bundles;

    AssetLoader()
    {
		instance = this;
    }

    static AssetLoader instance;
    public static AssetLoader GetInstance()
    {
        return instance;
    }

    string status;
	
	void Awake()
	{
        GameObject.DontDestroyOnLoad(transform.gameObject);
	}

    void Start()
    {
        bundles = new Dictionary<string, WWW>();
    }

    void OnApplicationQuit()
    {
        foreach (WWW www in bundles.Values)
        {
            www.assetBundle.Unload(false);
        }
    }

    public void LoadSceneURL(string url, string scene)
    {
        if (www == null || IsDone())
        {
            if (!bundles.ContainsKey(url))
            {
                displayDebug = true;
                www = new WWW(url);
                StartCoroutine(WaitForWWW(www, scene));
            }
            else
                Debug.Log(url + " already loaded.");
        }
        else
            Debug.LogError("AssetLoader already downloading scene.");
    }

    IEnumerator WaitForWWW(WWW www, string scene)
    {
        status = "Loading WWW...";
        yield return www;

        if (www.error != null)
        {
            UnityEngine.Debug.Log(www.error);
        }
        else
        {
            // Add to bundles array
            bundles.Add(www.url, www);

            status = "LoadAll()...";
            // load all the assets
            www.assetBundle.LoadAll();

            // add in the new scene
            status = "LoadLevelAdditiveAsync()...";
            op = Application.LoadLevelAsync(scene);
            yield return op;

            status = "Everything done...";
        }
    }

    public float GetProgress()
    {
        if (www != null && op != null)
        {
            return (www.progress * 0.75f) + (op.progress * 0.25f);
        }
        else if (www != null && op == null)
        {
            return (www.progress * 0.75f);
        }
        else
            return 0.0f;
    }

    public bool IsDone()
    {
        if (www != null)
            return www.isDone && op.isDone;
        else
            return false;
    }

    bool displayDebug = false;
    public void OnGUI()
    {
        if (Debug.isDebugBuild == false && displayDebug == false)
            return;

        GUILayout.BeginVertical();

        if (www != null)
        {
            GUILayout.Label("WWW status=" + status + " : percent=" + (int)(www.progress * 100.0f));
        }

        if (op != null)
        {
            GUILayout.Label("OP status=" + status + " : percent=" + (int)(op.progress * 100.0f) + " : isDone=" + op.isDone);
        }

        GUILayout.EndVertical();
    }

    public void Unload(string url)
    {
        if (bundles.ContainsKey(url))
        {
            bundles[url].assetBundle.Unload(true);
            bundles.Remove(url);
        }
    }

    void OnLevelWasLoaded(int level)
    {
        displayDebug = false;
    }
}


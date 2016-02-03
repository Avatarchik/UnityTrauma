using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SoundMgr : StringMgr
{
    Dictionary<string, AudioClip> SoundMap;
    public string soundmap = "XML/soundmap";

    static SoundMgr instance;
    public static SoundMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new SoundMgr();
            instance.Load();
        }
        return instance;
    }
	
    public void Load( string filename=null )
    {
        if ( stringmap == null )
            stringmap = new List<StringMap>();
		
		if ( filename != null )
			soundmap = filename;
		
        Debug.Log("SoundMgr:Load <" + soundmap + ">");

        LoadXML(soundmap);

        // make new SoundMap
        SoundMap = new Dictionary<string, AudioClip>();
        // load all resources
        for (int i = 0; i < stringmap.Count; i++)
        {
            if (SoundMap.ContainsKey(stringmap[i].key) == false)
            {
                AudioClip clip = (AudioClip)Resources.Load(stringmap[i].value);
                if (clip != null)
                {
                    SoundMap[stringmap[i].key] = clip;
#if DEBUG_AUDIO
                    Debug.Log(stringmap[i].key + "<" + stringmap[i].value + "> loaded OK");
#endif
                }
                else
                {
                    Debug.Log(stringmap[i].key + "<" + stringmap[i].value + "> not loaded, null");
                }
            }
        }
        return;
    }
	
	public AudioClip GetMapClip( string mapname )
	{
		if ( SoundMap.ContainsKey(mapname) )
			return SoundMap[mapname];
		return null;
	}

    public AudioClip GetClip(string filename)
    {
        AudioClip clip = (AudioClip)Resources.Load(filename);
        if (clip == null)
            UnityEngine.Debug.LogWarning("SoundMgr.GetClip(" + filename + ") : can't load clip");
        return clip;
    }

    public AudioClip Get(string name)
    {
        if (SoundMap.ContainsKey(name))
            return SoundMap[name];
        else
            return null;
    }
}

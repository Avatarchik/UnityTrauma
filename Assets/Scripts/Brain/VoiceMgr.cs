//#define DEBUG_VM
#define USE_INFO_DIALOG

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

[System.Serializable]
public class VoiceMap
{
    public string Tag;
    public string Audio;
    public string Text;
    public string LookAt;
	[XmlIgnore]
    public AudioClip Clip;
	public bool Dynamic;
	
	public VoiceMap()
	{
		Dynamic = false;
	}

    public void Debug()
    {
        UnityEngine.Debug.Log("VoiceMap <" + Tag + "> : <" + Audio + ">");
    }
}

[System.Serializable]
public class VoiceList
{
    public string Name;
    public List<VoiceMap> VoiceMaps;

    public void Debug()
    {
        UnityEngine.Debug.Log("VoiceList : Name=<" + Name + ">");
        foreach (VoiceMap map in VoiceMaps)
        {
            map.Debug();
        }
    }

	public string ToString()
	{
		string result = "VoiceList : Name=<" + Name + ">\r\n";
		foreach (VoiceMap map in VoiceMaps)
		{
			result +="   VoiceMap <" + map.Tag + "> : <" + map.Audio + "> '"+map.Text+"'\r\n";
		}
		return result;
	}
}

public class VoiceMgr
{
    public VoiceMgr()
    {
        VoiceLists = new List<VoiceList>();
    }

    static VoiceMgr instance;
    public static VoiceMgr GetInstance()
    {
        if (instance == null)
            instance = new VoiceMgr();
        return instance;
    }

	public void Init()
	{
		instance = null;
	}

    public List<VoiceList> VoiceLists;

	// These are used to coordinate characters with voice recognition, so they stay out of each other's way
	private bool speakingEnabled = true;
	public static bool CanSpeak(){ // brain and dynamic audio check this before playing the next queued sound
		if (instance == null) return true;
		else return instance.speakingEnabled;
	}
	public static void PauseSpeaking(){ // called when voice recognition detects speech starting
		if (instance != null)
			instance.speakingEnabled = false;
	}
	public static void ResumeSpeaking(){ // called when voice recognition detects speech input ended
		if (instance != null)
			instance.speakingEnabled = true;
	}


    public void LoadXML( string filename)
    {
        if (filename == null || filename == "")
            return;

#if DEBUG_VM
        UnityEngine.Debug.Log("VoiceList=<" + filename + ">");
#endif

        Serializer<VoiceList> serializer = new Serializer<VoiceList>();
        VoiceList list = serializer.Load(filename);
        if (list != null)
        {
            VoiceLists.Add(list);
#if DEBUG_VM
            list.Debug();
#endif
        }
    }
	
	public void AddVoiceList(VoiceList list){
		if (list == null) return;
		
		VoiceList oldList = FindList(list.Name);
		if (oldList != null){
			foreach (VoiceMap map in list.VoiceMaps){
				// replace any existing map by the same Name
				VoiceMap oldMap = null;
                for(int i = 0; i< oldList.VoiceMaps.Count; i++) // need to go thru by index so we can replace
                {
                    if (map.Tag == oldList.VoiceMaps[i].Tag){
                        oldMap = oldList.VoiceMaps[i];
						oldList.VoiceMaps[i] = map;
						break;
					}
                }				
				if (oldMap == null){
					oldList.VoiceMaps.Add(map);
				}
			}
		}
		else
			VoiceLists.Add(list);
	}
	
	public VoiceList FindList(string character)
	{
        foreach (VoiceList list in VoiceLists)
        {
#if DEBUG_VM
            UnityEngine.Debug.Log("VoiceMgr.Find(" + character + "," + tag + ") : Searching for character=<" + list.Name + ">");
#endif
            if ( list.Name == character )
            {
				return list;
			}
		}
		return null;
	}
			

    public VoiceMap Find(string character, string tag)
    {
		// this quick hackish version of wildcarding only handles trailing '*'
		// i.e. VOICE:ACKOWLEDGE:* MATCHES VOICE:ACKNOWLEDGE:1 or VOICE:ACKNOWLEDGE
		bool hasWildcard = tag.Contains("*");
		string wildcardTag = tag.Replace("*","");
		if (wildcardTag.Last() == ':'){
			wildcardTag = wildcardTag.Substring(0,wildcardTag.Length-1);	
		}
		
        foreach (VoiceList list in VoiceLists)
        {
#if DEBUG_VM
            UnityEngine.Debug.Log("VoiceMgr.Find(" + character + "," + tag + ") : Searching for character=<" + list.Name + ">");
#endif
            if ( list.Name == character )
            {
				// if wildcard, allow any match in that position (entire subfield only)
				
				if (hasWildcard){
					// make list of all matching maps
					List<VoiceMap> maps = new List<VoiceMap>();
	                foreach (VoiceMap map in list.VoiceMaps)
	                {
	                    if (map.Tag.Contains(wildcardTag))
	                       maps.Add(map);
	                }
					if (maps.Count > 0){
						int index = (int)(UnityEngine.Random.value*((float)maps.Count-.0001f));
						return maps.ElementAt(index);
					}
				}
				else
				{
	                foreach (VoiceMap map in list.VoiceMaps)
	                {
#if DEBUG_VM
	                    UnityEngine.Debug.Log("VoiceMgr.Find(" + character + "," + tag + ") : Tag=<" + map.Tag + "]");
#endif
	                    if (map.Tag == tag)
	                        return map;
	                }
				}
            }
        }
        return null;
    }

    public void Play(InteractMsg msg)
    {
        Play(msg.gameObject, msg.map.item);
    }

    public void Play(string character, string tag)
    {
        VoiceMap map = Find(character, tag);
        if (map != null)
        {
			if (map.Audio == "Audio/missingAudio")
				Debug.LogError("missing audio for tag "+tag+" "+character);
            // play the audio
			if (map.Dynamic == true )
			{
				// play dynamic text
				// get the dynamic audio player
				DynamicAudioMgr da = Brain.GetInstance().gameObject.GetComponent<DynamicAudioMgr>() as DynamicAudioMgr;
				if ( da != null )
				{
	                Character c = ObjectManager.GetInstance().GetBaseObject(character) as Character;
                	if (c != null)
                	{
						AudioSource source = Brain.GetInstance().GetAudioSource(character);
						c.TalkTime = da.Play(source,map.Audio,FindList(character).VoiceMaps);
                   		c.LookAt(map.LookAt, c.TalkTime);
						// queue gap for this duration
						// it looks like da.Play is going to play right now, but if there is other speech queued, it will speak over the top.
						// this needs to be interleaved with the non-dynamic audio
						// lets try playing the dynamic
						Brain.GetInstance().QueueAudioGap(c.TalkTime,character);
						// set the map text
						map.Text = da.GetPlayString();
					}
				}
			}
			else
			{
	            // get the clip
            	if (map.Clip == null)
	                map.Clip = SoundMgr.GetInstance().GetClip(map.Audio);
			}
			
            if (map.Clip != null)
            {
                // do LookAt
                Character c = ObjectManager.GetInstance().GetBaseObject(character) as Character;
                if (c != null)
                {
					// check for ignore
                    if (map.LookAt == null || map.LookAt == "")
                        map.LookAt = "camera";
					
					if (map.LookAt.ToLower() != "ignore")
					{
                    	// look
                    	c.LookAt(map.LookAt, Time.time + map.Clip.length);
						// set talk time
						c.TalkTime = Time.time + map.Clip.length;
					}
                }

				if ( map.Audio.Contains ("missing") ){
					UnityEngine.Debug.LogError("VoiceMgr.Play(" + character + "," + tag + ") audio missing for command<" + map.Tag + ">");
//				else
  //              	UnityEngine.Debug.Log("VoiceMgr.Play(" + character + "," + tag + ") ok");
					if (map.Text != ""){ // let us hear the text spoken
						Brain.GetInstance().PlayTTS(map.Text, character);
						float phraseLength = 2 + (map.Text.Length/15f);
						c.LookAt(map.LookAt, Time.time + phraseLength);
						c.TalkTime = Time.time + phraseLength; // rough amount of time to speak the text
						Brain.GetInstance().QueueAudioGap(phraseLength,character);
					}
				}else{
                Brain.GetInstance().QueueAudio(map.Clip,character);
					}
            }
            // put up dialog
            if (map.Text != null)
            {
#if USE_INFO_DIALOG
                InfoDialogMsg infomsg1 = new InfoDialogMsg();
                infomsg1.command = DialogMsg.Cmd.open;
                infomsg1.title = "<" + character + ">";
                infomsg1.text = Parse(map.Text);
                InfoDialogLoader.GetInstance().PutMessage(infomsg1);
#else
                QuickInfoMsg infomsg1 = new QuickInfoMsg();
                infomsg1.command = DialogMsg.Cmd.open;
                infomsg1.title = character;
                infomsg1.text = map.Text;
                infomsg1.timeout = 2.0f;
                QuickInfoDialog.GetInstance().PutMessage(infomsg1);

                UnityEngine.Debug.LogWarning("VoiceMgr.Play(" + character + "," + tag + ") can't play clip, text=[" + map.Text + "]");
#endif
        	}
        }
#if DEBUG_VM
        else
            UnityEngine.Debug.LogWarning("VoiceMgr.Play(" + character + "," + tag + ") can't find tag or character");
#endif
    }
	
	
	public string Parse( string input )
	{
		string output="";
		
		if ( input.Contains("%") == true )
		{
			string[] words = input.Split(' ');
			foreach( string word in words )
			{
				if ( word.Contains("%") )
				{
					// remove %
					string tmp = word.Replace("%","");
					// get DV
					DecisionVariable dv = new DecisionVariable(tmp,true);
					if ( dv != null )
					{
						int val = (int)(Math.Round(dv.GetFloat(),0));
						output += val.ToString() + " ";
					}
					else
						output += "(<" + word + "> not found)" + " ";
				}
				else
					output += word + " ";
			}
			return output;
		}
		
		return input;
	}	
}

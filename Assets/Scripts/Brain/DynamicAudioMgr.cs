
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DynamicAudioMgr : MonoBehaviour
{
	public DynamicAudioMgr ()
	{
	}
	
	public string TestNumber="125.4";
	
	public class SoundBite
	{
		public SoundBite()
		{
			ready = false;
		}
		
		public string name;
		public AudioClip clip;
		public float time;
		public bool ready;
		public AudioSource source;
		
		public void Play()
		{
			source.PlayOneShot(clip);
 //           Brain.GetInstance().PlayAudio(clip);
			ready = false;
		}
	}
	// this global struct means we can only process one character at a time, 
	// so go ahead and assign another global for the audioSource to play through :/
	List<SoundBite> soundBites = new List<SoundBite>();
	AudioSource characterSource;
	
	public StringMap Map;
	public void LoadXML( string name )
	{
		Serializer<StringMap> serializer = new Serializer<StringMap>();
		Map = serializer.Load(name);
	}
	
	// audio=AUDIO:AUDIO number=%Patient.HR audio=AUDIO.AUDIO etc...
	public float biteGap = 0.2f;
	public float numberGap = -0.1f;
	bool decimals=false;
	
	float endtime;
	string outputString;
	
	public string GetPlayString()
	{
		return outputString;
	}
	
	// specific voice to play
	List<VoiceMap> voiceList;
	
	public void Update()
	{
		if (!VoiceMgr.CanSpeak()) return; // don't process the queue while the player is speaking
		List<SoundBite> remove = new List<SoundBite>();
		
		// check list for something to play
		foreach( SoundBite sb in soundBites )
		{
			if ( sb.ready == true )
			{
				// check time
				if ( Time.time > sb.time )
				{
					// play it
					sb.Play();					
					// remove
					remove.Add(sb);
				}
			}
		}

		// remove bites already played
		foreach( SoundBite sb in remove )
		{
			soundBites.Remove(sb);
		}
	}
	
	public float Play(AudioSource source, string text, List<VoiceMap> list )
	{
		// save off current time for calculation below
		float currTime = Time.time;
		
		// send command to player, set voiceList
		voiceList = list;
		characterSource = source;
		Play(text);
		voiceList = null;

		// return duration
		return endtime-currTime;
	}
		
	public void Play( string text )
	{
		List<string> commands = GetKeyValuePairs(text);
	
		// set end time to start time
		endtime = Time.time;
		
		// init string to build
		outputString = "";
		
		foreach( string command in commands )
		{
			if ( command.Contains("audio") )
			{
				string audio;
				GetToken(command,"audio",out audio);
				// add audio
				AddAudioBite(audio);
				// build string
				outputString += (GetClipString(audio));
				// add gap
				endtime += biteGap;
			}
			if ( command.Contains("variable") )
			{
				// have to build a number here, from a decision variable
				string numString="";
				GetToken(command,"variable",out numString);
				// this is a decision variable
				DecisionVariable var = new DecisionVariable(numString.ToLower(), true);
				if ( var.Valid == true )
				{
					PlayNumber(var.Get());
					// convert number to string
					float number = Convert.ToSingle(var.Get());
					number = (float)Math.Round(number,(decimals==true)?1:0);
					outputString += (number.ToString());
				}
			}
			if ( command.Contains("number") )
			{
				// have to build a number here, from a decision variable
				string numString="";
				GetToken(command,"number",out numString);
				PlayNumber(numString);
				outputString += (numString);
			}
			if ( command.Contains("decimals") )
			{
				string decimals="";
				GetToken(command,"decimals", out decimals);
				this.decimals = Convert.ToBoolean(decimals);
			}
			if ( command.Contains("gap") )
			{
				string gap="";
				GetToken(command,"gap", out gap);
				endtime += Convert.ToSingle(gap);
				outputString += ".  ";
			}
			if ( command.Contains("text") )
			{
				string textStr="";
				GetToken(command,"text", out textStr);
				if ( textStr == "space" )
					outputString += " ";
				else
					outputString += textStr;
			}
		}
	}	
	
	public AudioClip GetClip( string name )
	{
		if ( voiceList != null )
		{
			foreach( VoiceMap map in voiceList )
			{
				if ( map.Tag == name )
				{
					return SoundMgr.GetInstance().GetClip(map.Audio);
				}
			}
			UnityEngine.Debug.Log("DynamicAudioMgr.GetClip() : can't find <" + name + ">");
			return null;
		}
		else
			return SoundMgr.GetInstance().GetClip(name);
	}
	
	public string GetClipString( string name )
	{
		if ( voiceList != null )
		{
			foreach( VoiceMap map in voiceList )
			{
				if ( map.Tag == name )
				{
					return map.Text;
				}
			}
			return null;
		}
		return null;
	}
		
	public void AddAudioBite( string audio )
	{
		// get the clip
		AudioClip clip = GetClip(audio);
		if ( clip != null )
		{
			string source = null;
			if ( characterSource!= null) source = characterSource.name;
			Brain.GetInstance().QueueAudio(clip, source);
			/*
			// make sound bite
			SoundBite bite = new SoundBite();
			bite.name = audio;
			bite.clip = clip;
			bite.time = endtime;
			endtime += bite.clip.length;
			bite.ready = true;
			bite.source = characterSource; // this member set in Play
			soundBites.Add(bite);	
			*/
		}
	}
	
	public void PlayNumber( string number )
	{
		float value = Convert.ToSingle(number);
		
		if ( value == 0 )
		{
			AddAudioBite("VOICE:NUMDIGIT:0");
			endtime += numberGap;
		}
		
		if ( value >= 100 )
		{
			int hundreds = (int)(value/100.0f);
			// create audio name for 100s 
			string audio100s = "VOICE:NUM100:" + hundreds;
			// say hundreds 
			AddAudioBite(audio100s);
			// add gap
			endtime += numberGap;
			// subtract value
			value -= (float)(hundreds*100);			
			value = (float)Math.Round(value,(decimals==true)?1:0);
		}

		if ( value >= 20 )
		{
			int tens = (int)(value/10.0f);
			if ( tens != 0 )
			{
				// create audio name for 100s 
				string audio10s = "VOICE:NUM10:" + tens;
				// say hundreds 
				AddAudioBite(audio10s);
				// add gap
				endtime += numberGap;
			}
			// subtract value
			value -= (float)(tens*10);			
			value = (float)Math.Round(value,(decimals==true)?1:0);
		}
		if ( value >= 10 )
		{
			// do teens
			int teens = (int)(value-10.0f);
			// create audio name for teens (0=10) 
			string audioTeens = "VOICE:NUMTEEN:" + teens;
			// say hundreds 
			AddAudioBite(audioTeens);
			// add gap
			endtime += numberGap;				
			// subtract value
			value -= (float)(teens+10);			
			value = (float)Math.Round(value,(decimals==true)?1:0);
		}
		if ( value != 0 )
		{			
			int ones = (int)(value);
			// create audio name for 100s 
			if ( ones != 0 )
			{
				string audioDigits = "VOICE:NUMDIGIT:" + ones;
				// say hundreds 
				AddAudioBite(audioDigits);
				// add gap
				endtime += numberGap;
			}
			// subtract value
			value -= (float)ones;
			value = (float)Math.Round(value,(decimals==true)?1:0);
		}
		if ( value != 0 && decimals == true)
		{
			float tmp = value * 10.0f;
			int fraction = (int)(tmp);
			if ( fraction != 0 )
			{
				// create audio name for 100s 
				string audioFrac = "VOICE:NUMFRAC:" + fraction;
				// say hundreds 
				AddAudioBite(audioFrac);
				// add gap
				endtime += numberGap;
			}
		}
	}

    public List<string> GetKeyValuePairs(string input)
    {
        List<string> pairs = new List<string>();

        string result = "";
        bool inQuote = false;
        // scan input until there is a space
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == ' ')
            {
                if (inQuote == true)
                    result += input[i];
                else
                {
                    // reached an end
                    pairs.Add(result);
                    result = "";
                }
            }
            else
            {
                if ( input[i] == '"' )
                    inQuote = (inQuote == true) ? false : true;
                result += input[i];
            }
        }

        pairs.Add(result);
        return pairs;
    }
	
    public bool GetToken(string arg, string key, out string value)
    {
        string[] args = arg.Split(' ');
        for (int i = 0; i < args.Length; i++)
        {
            string[] keyvalue = args[i].Split('=');
            if (keyvalue.Length == 2)
            {
                if (keyvalue[0] == key)
                {
                    value = keyvalue[1];
                    return true;
                }
            }
        }
        value = "";
        return false;
    }
	
	public void OnGUI()
	{
		return;
		
		GUILayout.Label("SAY : " + TestNumber);
		foreach( SoundBite bite in soundBites )
		{
			GUILayout.Label("BITE : " + bite.name);
		}
	}
}



#define DEBUG_ANIM_EVENTS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AnimEventList // for checking that all events are present and executing...used by the editor event exporter
{
	public string clipName; 
	public string owners;
	//	public AnimationClip clip; // not a lot of useful info except wrap mode.
	public List<AnimationEvent> events;
}

public class AnimationMessenger : MonoBehaviour
{
	public GameObject target;
	
	private AnimationManager am;
	private TaskCharacter tc;
	public static List<AnimEventList> animEventList = null;
	public AnimEventList expectedEvents;
	public string debugClipName = "";
//	List<AnimEventList> MatchingLists; // since multiple clips chare a name..
	public List<AnimationEvent> receivedEvents;
	public List<string> skippedEvents; // for debug gui
	string setClipName = "noneLoaded";
	string prevClipName = "notSet";
	bool initialized = false;
	int callCount = 0;

	public void Awake()
	{
		Init ();
	}

	void Init(){ // refactored this because Awake could be called after the first ExpectEvents call, wiping out the settings
		if (initialized) return;
		initialized = true;

		if(target == null && transform.parent != null )
			target = transform.parent.gameObject;
		if ( target != null )
		{
			am = target.GetComponent<AnimationManager>();
			tc = target.GetComponent<TaskCharacter>();
		}
		skippedEvents = new List<string>();
		

		expectedEvents = new AnimEventList();
		expectedEvents.events = new List<AnimationEvent>();
		receivedEvents = new List<AnimationEvent>();

//		MatchingLists = new List<AnimEventList>() ;
		
		if (animEventList == null){
			Serializer<List<AnimEventList>> serializer = new Serializer<List<AnimEventList>>();
			string pathname = "XML/AnimEventList";
			animEventList = serializer.Load(pathname);
//			CheckForDuplicateClips(); // there are a lot of duplicates, we are dealing with it.
		}

	}

	public void ExpectEventsFor(string clipname){
		Init ();
		prevClipName = setClipName;
		setClipName = clipname;

		if (expectedEvents == null){
			expectedEvents = new AnimEventList();
			expectedEvents.events = new List<AnimationEvent>();
		}
		// here's where we check for any unfired events, but not for looping mode clips
		if (animation[expectedEvents.clipName] != null && animation[expectedEvents.clipName].wrapMode != WrapMode.Loop) {
			foreach (AnimationEvent eve in expectedEvents.events) {
				if (eve.time < animation [prevClipName].time) { // the event's time in the clip has passed, should have fired
					Debug.LogError (Time.time + target.name + ": AnimationEvent " + eve.functionName + ":" + eve.time + " FAILED TO TRIGGER for " + prevClipName + ":" + animation [prevClipName].time);
					string message = Time.time + target.name + ": AnimationEvent " + ":" + eve.time + eve.functionName + " FAILED TO TRIGGER for " + prevClipName + ":" + animation [prevClipName].time;
					skippedEvents.Add (message);
				}
			}
		}
		expectedEvents.events.Clear();
		receivedEvents.Clear();
		bool found = false;
		string eventnames = "-";

		foreach (AnimEventList list in animEventList){
			// this next test assumes that the no event clips, without owners listed, are checked last, thus act as defaults if no
			if (list.clipName == clipname && (list.owners == null || list.owners.Contains(target.name))){
				found = true;
				expectedEvents.clipName = clipname;
				foreach (AnimationEvent e in list.events){
					expectedEvents.events.Add(e); // do we need to make a copy of 'e' ? i dont think so
					eventnames += e.functionName;
					// only set a failsafe for non-looping clips
					if (animation[expectedEvents.clipName].wrapMode != WrapMode.Loop)
						StartCoroutine(StartFailsafeFor(e)); // start a time based co routine to be sure this triggers.
				}
#if DEBUG_ANIM_EVENTS
//				if (list.owners == null)
//					Debug.LogWarning(target.name+" using default no-event clip for "+clipname);
#endif
				break;
			}
		}
//		Debug.LogError (Time.time+": "+target.name+" starting clip "+clipname+" has events "+expectedEvents.events.Count+eventnames);
		if (!found) Debug.LogError(target.name+" found no event list for clip "+clipname);
		//if (clipname != expectedEvents.clipName) Debug.LogError("HOW COULD THIS HAPPEN???"); // awake was getting called after ExpectEvent
		//Debug.LogError(target.name+" expectEventsFor ["+clipname+"] found ["+expectedEvents.clipName+"]");

	}

	bool AlreadyExpecting(AnimationEvent evt){ // not using this now that clips are tagged with their owners
		// see if there is already a matching method name and very close timestamp 
		bool result = false;
		foreach (AnimationEvent e in expectedEvents.events) {
			if (e.functionName == evt.functionName &&
			    Mathf.Abs(e.time - evt.time)< 0.07f){ // within 1/15 sec, two frames @ 30 hz
				result = true;
				break;
			}
		
		}
		return result;
	}

	public bool EventIsExpected(AnimationEvent evt){

		bool found = false;
		foreach (AnimationEvent eve in expectedEvents.events) {
			if (eve == evt) {
				found = true;
				break;
			}
		}
		// now be sure that the clip is stil playing, and that the time is later than the event time
		if (found && animation.IsPlaying (expectedEvents.clipName) && animation [expectedEvents.clipName].time > evt.time)
						return true;
				else
						return false;
	}

	public bool ReceivedEventFor(string eventMethod){
		callCount++;
#if DEBUG_ANIM_EVENTS
		if (setClipName == debugClipName){
			Debug.LogWarning(target.name+" received "+eventMethod+" at "+animation[setClipName].time+" in clip "+debugClipName);
		}
#endif
		float animTime = animation [expectedEvents.clipName].time;
		while (animTime > animation [expectedEvents.clipName].length)
			animTime -= animation [expectedEvents.clipName].length;

//#if DEBUG_ANIM_EVENTS
		bool found = false;
		foreach (AnimationEvent eve in expectedEvents.events){
			if (eve.functionName == eventMethod){
				if (Mathf.Abs(eve.time - animTime)<0.07f){ //right command and about the right time
					found = true;
	//				Debug.LogError(found.ToString()+Time.time+"#"+callCount+target.name+" Removing from expected events "+eventMethod+" while playing "+setClipName+":"+animation[setClipName].normalizedTime);
					receivedEvents.Add (eve);
					if (animation[expectedEvents.clipName].wrapMode != WrapMode.Loop)
						expectedEvents.events.Remove (eve);
					break;
				}else{
					bool missedEvent = false;
					foreach (AnimationEvent e in receivedEvents){
						if (e.functionName == eventMethod){
							if (Mathf.Abs(e.time - animTime)<0.07f){ //right command and about the right time
								missedEvent = true;
							}
						}
					}
					float missTime = animTime - eve.time; // less than zero means we are early, that's an error condition
					if (missTime < 0){
						missTime += animation[expectedEvents.clipName].length; // try allowing the event to occur after the clip is done.
#if DEBUG_ANIM_EVENTS
						Debug.LogWarning("adjusted event time to after clip length "+ expectedEvents.clipName+"."+eve.functionName);
#endif
					}
#if DEBUG_ANIM_EVENTS
					Debug.LogWarning (Time.time+target.name+expectedEvents.clipName+eve.functionName+"Method match but time off by "+missTime);
#endif
					// if there wasn't a better match in the received event list, then go ahead and fire this one...
					if ((missTime > 0 && !missedEvent) || animation[setClipName].wrapMode == WrapMode.Loop){
#if DEBUG_ANIM_EVENTS
						Debug.LogWarning ("Allowing "+missTime+" late animation event to run ("+animation[setClipName].wrapMode+" mode animation)");
#endif
						found = true;
						//				Debug.LogError(found.ToString()+Time.time+"#"+callCount+target.name+" Removing from expected events "+eventMethod+" while playing "+setClipName+":"+animation[setClipName].normalizedTime);
						receivedEvents.Add (eve);
						expectedEvents.events.Remove (eve);
						break;
					}
				}
			}
		}
#if DEBUG_ANIM_EVENTS
		if (!found) { // is this happening ? We need to understand why...
			bool foundOne = false;
			string parm = "";
			foreach (AnimationEvent eve in receivedEvents){
				if (eve.functionName == eventMethod){
					foundOne = true;
					parm = eve.stringParameter;
					break;
				}
			}
			if (foundOne)
				Debug.LogWarning (Time.time+"#"+callCount+target.name + " Received EXTRA event " + eventMethod+":"+parm + " playing clip [" + expectedEvents.clipName+":"+animation[setClipName].time+"]after["+prevClipName+"]");
			else
				Debug.LogWarning (found.ToString()+Time.time+"#"+callCount+target.name + " Received unexpected event " + eventMethod + " playing clip [" + expectedEvents.clipName+":"+animation[setClipName].time+"]after["+prevClipName+"]");
		}
#endif
		return (found);
//#endif		
}
	
	public void PlayNext(float blendTime)
	{
		if (!ReceivedEventFor("PlayNext")) return;
		if(am != null)
			am.PlayNext(blendTime);
	}
	
	public void SetAnimState(CharacterAnimState newState)
	{
		if (!ReceivedEventFor("SetAnimState")) return;
		am.AnimState = newState;
	}
	
	public void Attach()
	{
		if (!ReceivedEventFor("Attach")) return;
		if(am.AnimState == CharacterAnimState.Transitioning) // why can i only respond to an attach event if i am transitioning ?
			tc.Attach();
		if(am.AnimState == CharacterAnimState.Interacting) // added by phil to get the vials handoff to work...
			tc.Attach();
	}
	
	public void Detach()
	{
		if (!ReceivedEventFor("Detach")) return;
		tc.Detach();
	}
	
	public void InteractAttach(string attachName)
	{
		if (!ReceivedEventFor("InteractAttach")) return;
		if(am.AnimState == CharacterAnimState.Interacting)
			am.Attach(attachName);
	}
	
	public void InteractDetach(string attachName)
	{
		if (!ReceivedEventFor("InteractDetach")) return;
		if(am.AnimState == CharacterAnimState.Interacting)
			am.Detach(attachName);
	}
	
	public void PlayAudio(AudioClip clip)
	{
		if (!ReceivedEventFor("PlayAudio")) return;
		target.audio.PlayOneShot(clip);
	}
	
	public void PlaySpeech(string voiceTag)
	{
			if (!ReceivedEventFor("PlaySpeech")) return;
			VoiceMgr.GetInstance().Play(target.name, voiceTag);
	}
	
	public void PlaySubAnim(string subAnimName)
	{
		if (!ReceivedEventFor("PlaySubAnim")) return;
		if(am.AnimState == CharacterAnimState.Interacting)
			am.PlayInteractSubAnim(subAnimName);
		else
			am.PlayPostureSubAnim(subAnimName);
	}
	
	public void SwitchPosture(string newPosture)
	{
		if (!ReceivedEventFor("SwitchPosture")) return;
		am.NextPosture(newPosture, true);
		tc.ChangePostureInstant(newPosture);  // keep the tc in sync with this instant change.
	}
	
	// this method conflicts with the Unity SendMessage method, and should be renamed.  It is already in use
	// so all occurences in animations would need to be changed.
	// temp fix is to add a null SendMessage to the LookAtController component.
	public void SendMessage(string message)
	{
		if (!ReceivedEventFor("SendMessage")) return;
		if(Brain.GetInstance() != null)
		{
			string processedMsg = message.Replace("#MYSELF#", target.name.ToUpper());
			Brain.GetInstance().PutMessage(new InteractStatusMsg(processedMsg));
		}
	}
	
	public void ScriptEvent(string actionName){
		if (!ReceivedEventFor("ScriptEvent")) return;
		// there should be an interaction script defined or overridden to search for this action name
		if (am.EventScript != null){
			am.EventScript.ExecuteEvent(am.GetComponent<ScriptedObject>(),actionName);
		}
	}

#if DEBUG_ANIM_EVENTS
	public void OnGUI(){ 
		if (skippedEvents.Count > 0){
			foreach (string message in skippedEvents){
//				GUILayout.Label(message);
			}
		}
	}
 
	// we want to see any errors that occur so leave the GUI up
#endif

	IEnumerator StartFailsafeFor(AnimationEvent evt){

		// wait until it's time...
		yield return new WaitForSeconds (evt.time+0.05f); // let the anim event beat us if it fires

		if (EventIsExpected (evt)) {
			float animTime = animation[setClipName].time;
			while ( animTime > animation[setClipName].length) animTime -= animation[setClipName].length;
#if DEBUG_ANIM_EVENTS
			Debug.LogWarning (Time.time+" Failsafe animation event ["+evt.time+"] firing ["+animTime+"] for "+target.name+" clip "+setClipName+" event "+evt.functionName+":"+evt.stringParameter);
			string message = Time.time+" Failsafe animation event ["+evt.time+"] firing ["+animTime+"] for "+target.name+" clip "+setClipName+" event "+evt.functionName+":"+evt.stringParameter;
			skippedEvents.Add (message);
			#endif
				switch (evt.functionName) {
				case "PlayNext":
						{
								PlayNext (evt.floatParameter);
								break;
						}
				case "SetAnimState":
						{
								SetAnimState ((CharacterAnimState)evt.intParameter);
								break;
						}
				case "Attach":
						{
								Attach ();
								break;
						}
				case "Detach":
						{
								Detach ();
								break;
						}
				case "InteractAttach":
						{
								InteractAttach (evt.stringParameter);
								break;
						}
				case "InteractDetach":
						{
								InteractDetach (evt.stringParameter);
								break;
						}
				case "PlayAudio":
						{
								PlayAudio ((AudioClip)evt.objectReferenceParameter); // THIS WILL ALWAYS BE NULL WITH DESERIALIZED EVENTS
								break;
						}
				case "PlaySpeech":
						{
								PlaySpeech (evt.stringParameter);
								break;
						}
				case "PlaySubAnim":
						{
								PlaySubAnim (evt.stringParameter);
								break;
						}
				case "SwitchPosture":
						{
								SwitchPosture (evt.stringParameter);
								break;
						}
				case "SendMessage":
						{
								SendMessage (evt.stringParameter);
								break;
						}
				case "ScriptEvent":
						{
								ScriptEvent (evt.stringParameter);
								break;
						}
				}
		}

	}

	void CheckForDuplicateClips(){
		// see if the anim event list contains any duplicate names, this will lead to problems
		for (int i=0; i< animEventList.Count; i++)
						for (int j=i+1; j<animEventList.Count; j++)
								if (animEventList [i].clipName == animEventList [j].clipName) {
				// compare the event lists. if they are different, flag an error
				bool inconsistent = false;
				if (animEventList[i].events.Count != animEventList[j].events.Count){
					inconsistent = true;
					Debug.LogError ("Found DUPLICATE CLIP NAMES in Animation Event List for " + animEventList [i].clipName+", different # of events");
				}
				if (!inconsistent){
					// same number of events, check method names
					for (int k=0; k < animEventList[i].events.Count; k++){
						if ( animEventList[i].events[k].functionName != animEventList[j].events[k].functionName){
							inconsistent = true;
							Debug.LogError ("Found DUPLICATE CLIP NAMES in Animation Event List for " + animEventList [i].clipName+", different event types");
						}
					}
				}


				//Debug.LogError ("Found DUPLICATE CLIP NAMES in Animation Event List for " + animEventList [i].clipName);
			}

		}
}

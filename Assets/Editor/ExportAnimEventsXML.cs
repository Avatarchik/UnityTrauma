using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

// We're going to need a runtime accessible structure to deserialize these into, for runtime checking if
// these events are getting skipped...

/* how to use this tool:
 * In a scene with all the animated objects, like the Trauma Editing Scene, 
 * then use File/Export/AnimatinEvents
 * 
 * That will generate AnimEventList.xml in the project folder, above Assets.
 * 
 * You could use this list to check on proper execution of these events, or to verify or add them with an editor tool
 * when the animations have been worked on
 */


public class AnimEventList
{
	public string clipName; // put the clip here if it will serialize
	public string owners; // task character names using this clip for choosing between multiple identically named clips
	public AnimationClip clip; // not a lot of useful info except wrap mode.
	public List<AnimationEvent> events; // may have to store these as serializable infos
}
 

public class AnimEventExporter : ScriptableObject
{


	[MenuItem ("File/Export/AnimationEvents")]
	static void DoExportAnimEvents()
	{
		DoExport(true);
	}

	static void DoExport(bool makeSubmeshes)
	{
		List<AnimEventList> clipLists;
		List<AnimationClip> processedClips;


//		if (Selection.gameObjects.Length == 0)
//		{
//			Debug.Log("Didn't Export Anything!");
//			return;
//		}
		clipLists = new List<AnimEventList>();
		processedClips = new List<AnimationClip>();
		List<AnimationClip> zeroEventClips = new List<AnimationClip>();
		// collect all the animation messenger components...
		AnimationMessenger[] messengers = FindObjectsOfType<AnimationMessenger> (); // in the scene
		//foreach (GameObject obj in Selection.gameObjects){
		foreach (AnimationMessenger mess in messengers){
			GameObject obj = mess.gameObject;
			GameObject target = FindTarget(mess);
			if (obj.animation != null){
				// be sure we have a
				foreach (AnimationState state in obj.animation){
					if (!processedClips.Contains (state.clip)){

						AnimationEvent[] evs = AnimationUtility.GetAnimationEvents(state.clip);
						if (evs.Length > 0){
							processedClips.Add(state.clip);
							AnimEventList evList = new AnimEventList();
							evList.events = new List<AnimationEvent>();
							evList.clipName = state.clip.name;
							evList.clip = state.clip;
							evList.owners = target.name; // start with the first owner
							foreach (AnimationEvent ev in evs){
								evList.events.Add (ev);
							}
							Debug.Log (evs.Length+" events found on clip "+state.clip.name+" of "+target.name);
							clipLists.Add (evList);
						}
						else
						{
							if (!zeroEventClips.Contains (state.clip)){
								Debug.LogWarning(state.clip.name+" has NO animation Events");
								zeroEventClips.Add(state.clip);
							}
						}
					}
					else{
						//  add this character as an owner of the clip
						foreach (AnimEventList checkList in clipLists) // we might not be able to access this while in use...
							if (checkList.clip == state.clip){
								checkList.owners += " "+ target.name;
								break;
							}
					}

				}
			}
		}
		// add all the zero event clips at the end so we can find them in the xml
		foreach (AnimationClip clip in zeroEventClips){
			AnimEventList evList = new AnimEventList();
			evList.events = new List<AnimationEvent>();
			evList.clipName = clip.name;
			//evList.clip = clip;
			clipLists.Add (evList);
		}

		// we may have to null out the 'clip' elements for serialization
		foreach (AnimEventList list in clipLists)
						list.clip = null; // there's really no useful info here...

		// now lets try to serialize them
		XmlSerializer serializer = new XmlSerializer(typeof(List<AnimEventList>));
		FileStream stream = new FileStream("AnimEventList.xml", FileMode.Create);
		serializer.Serialize(stream, clipLists);
		stream.Close();	

		Debug.Log (clipLists.Count+" Animations With Events serialized");
	}

	static GameObject FindTarget(AnimationMessenger mess){
		if (mess.target != null)
						return mess.target;
		Transform xfm = mess.transform;
		while (xfm.parent != null) {
			if (xfm.parent.GetComponent<AnimationManager>() != null)
				return xfm.parent.gameObject;
			xfm = xfm.parent;
		}
		// if we got here, there was no ancestor with an animation manager!
		return null;
	
	}
}

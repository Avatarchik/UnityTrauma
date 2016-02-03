using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

public class AnimatedInteractionData
{
	public string name;
	public List<string> clips;
	public List<AttachmentData> attachments;
	public List<SubAnimData> subAnims;
	public string eventScript; // name of adefault event script for any scriptedEvents
}

public class AttachmentData
{
	public string name;
	public string targetObject;
	public string targetBone;
}

public class SubAnimData
{
	public string name;
	public string targetObject;
	public string animName;
}

public class AnimatedInteraction
{
	private string name;
	private List<AnimationClip> animClips = new List<AnimationClip>();
	private Dictionary<string, AttachableObject> attachableObjects = new Dictionary<string, AttachableObject>();
	private Dictionary<string, SubAnim> subAnims = new Dictionary<string, SubAnim>();
	private InteractionScript eventScript;
	
	public string Name
	{
		get {return name;}
		set {name = value;}
	}
	
	public List<AnimationClip> AnimClips
	{
		get {return animClips;}
		set {animClips = value;}
	}
	
	public Dictionary<string, AttachableObject> AttachableObjects
	{
		get {return attachableObjects;}
		set {attachableObjects = value;}
	}
	
	public Dictionary<string, SubAnim> SubAnims
	{
		get {return subAnims;}
		set {subAnims = SubAnims;}
	}
	
	public AnimatedInteraction(string newName)
	{
		name = newName;
		animClips = new List<AnimationClip>();
	}
	
	public AnimatedInteraction(string newName, List<AnimationClip> newClips)
	{
		name = newName;
		animClips = newClips;
	}
	
	public AnimatedInteraction(string newName, Animation sourceAnims)
	{
		name = newName;
		foreach(AnimationState animState in sourceAnims)
			if(animState.name.StartsWith(name))
				animClips.Add(animState.clip);
		if(animClips.Count > 1)
			animClips.Sort();
	}
	
	public AnimatedInteraction(string newName, List<string> animNames, Animation sourceAnims)
	{
		name = newName;
		foreach(string n in animNames)
			if(sourceAnims[n] != null)
				animClips.Add(sourceAnims[n].clip);
	}
	
	public InteractionScript EventScript
	{
		get {return eventScript;}
		set {eventScript = value;}
	}
	
	/*public void Debug() {
		UnityEngine.Debug.Log("AnimatedInteraction: Name: " + name); 
	}*/
}
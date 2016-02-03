using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostureData 
{
	public string name;
	public float idleRate;
	public float idleDev;
	public float walkSpeed;
	public List<string> keys;
	public List<string> characters;
	public bool hasAttachObject;
	public string targetObject;
	public string parentObject;
	public List<SubAnimData> subAnims;
}

public class Posture
{
	private AnimationClip baseClip;
	private AnimationClip walkClip;
	private List<AnimationClip> idleClips = new List<AnimationClip>();
	private Dictionary<Posture, AnimationClip> transitions = new Dictionary<Posture, AnimationClip>();
	private float idleRate;
	private float idleDeviation;
	private float walkSpeed = 1.5f;
	private string name;
	private bool hasAttachObject;
	private string targetObject;
	private string parentObject;
	private bool canIdle = true;
	private bool canWalk = true;
	private Dictionary<string, SubAnim> subAnims = new Dictionary<string, SubAnim>();
	
	public bool HasAttachObject
	{
		get {return hasAttachObject;}
		set {hasAttachObject = value;}
	}
	
	public string TargetObject
	{
		get {return targetObject;}
		set {targetObject = value;}
	}
	
	public string ParentObject
	{
		get {return parentObject;}
		set {parentObject = value;}
	}
	
	public List<string> keys;
	
	public string Name
	{
		get {return name;}
		set {name = value;}
	}
	
	public AnimationClip BaseClip
	{
		get {return baseClip;}
		set
		{
			baseClip = value;
		}
	}
	
	public AnimationClip WalkClip
	{
		get {return walkClip;}
		set {walkClip = value;}
	}
	
	public List<AnimationClip> IdleClips
	{
		get {return idleClips;}
		set {idleClips = value;}
	}
	
	public float IdleRate
	{
		get {return idleRate;}
		set
		{
			if(value < 0f)
				idleRate = value;
		}
	}
	
	public float IdleDeviation
	{
		get {return idleDeviation;}
		set
		{
			if(value > idleRate)
				idleDeviation = idleRate;
		}
	}
	
	public float WalkSpeed
	{
		get {return walkSpeed;}
		set {walkSpeed = value;}
	}
	
	public Dictionary<Posture, AnimationClip> Transitions
	{
		get {return transitions;}
		set {transitions = value;}
	}
	
	public bool CanIdle
	{
		get {return canIdle;}
		set {canIdle = value;}
	}
	
	public bool CanWalk
	{
		get {return canWalk;}
		set {canWalk = value;}
	}
	
	public Dictionary<string, SubAnim> SubAnims
	{
		get {return subAnims;}
		set {subAnims = SubAnims;}
	}
	
	public Posture() {}
	
	public Posture(AnimationClip newBase, AnimationClip newWalk, List<AnimationClip> newIdles, Dictionary<Posture, AnimationClip> newTransitions, float newIdleRate, float newIdleDev, float newWalkSpeed)
	{
		baseClip = newBase;
		walkClip = newWalk;
		idleClips = newIdles;
		transitions = newTransitions;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = newWalkSpeed;
		name = baseClip.name;
		
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		
		if(walkSpeed > 0f)
			canWalk = true;
		else
			canWalk = false;
	}
	
	public Posture(AnimationClip newBase, AnimationClip newWalk, List<AnimationClip> newIdles, float newIdleRate, float newIdleDev, float newWalkSpeed)
	{
		baseClip = newBase;
		walkClip = newWalk;
		idleClips = newIdles;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = newWalkSpeed;
		transitions = new Dictionary<Posture, AnimationClip>();
		name = baseClip.name;
		
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		
		if(walkSpeed > 0f)
			canWalk = true;
		else
			canWalk = false;
	}
	
	public Posture(string newName, float newIdleRate, float newIdleDev)
	{
		name = newName;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = 1.5f;
		
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		
		canWalk = true;
		
	}
	
	public Posture(string newName, float newIdleRate, float newIdleDev, float newWalkSpeed, Animation sourceAnims)
	{
		name = newName;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = newWalkSpeed;
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		
		if(walkSpeed > 0f)
			canWalk = true;
		else
			canWalk = false;
		
		FindBaseAnims(sourceAnims);
	}
	
	public Posture(string newName, float newIdleRate, float newIdleDev, float newWalkSpeed, Animation sourceAnims, 
				   bool hasAttachedObject, string targetObject, string parentObject)
	{
		name = newName;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = newWalkSpeed;
		this.hasAttachObject = hasAttachObject;
		this.targetObject = targetObject;
		this.parentObject = parentObject;
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		
		if(walkSpeed > 0f)
			canWalk = true;
		else
			canWalk = false;
		FindBaseAnims(sourceAnims);
	}
	
	public Posture(string newName, float newIdleRate, float newIdleDev, Animation sourceAnims)
	{
		name = newName;
		idleRate = newIdleRate;
		idleDeviation = newIdleDev;
		walkSpeed = 1.5f;
		if(idleRate > 0f)
			canIdle = true;
		else
			canIdle = false;
		canWalk = true;
		FindBaseAnims(sourceAnims);
	}
	
	public void FindBaseAnims(Animation sourceAnims)
	{
		if(sourceAnims[name])
		{
			baseClip = sourceAnims[name].clip;
			if(sourceAnims[name + "Walk"])
				walkClip = sourceAnims[name + "Walk"].clip;
			else
			{
				walkClip = null;
				canWalk = false;
			}
		
			foreach(AnimationState animState in sourceAnims)
				if(animState.clip.name.StartsWith(baseClip.name + "Idle"))
				   idleClips.Add(animState.clip);
			if(idleClips.Count > 0)
				canIdle = true;
			else
				canIdle = false;
		}
		else
			UnityEngine.Debug.LogWarning("Cannot find Base Clip for Posture " + name + " for "+sourceAnims.transform.parent.name);
	}
	
	public void BuildTransitionAnims(List<Posture> postureList, List<string> targetPostures, Animation sourceAnims)
	{
		if(baseClip != null)
		{
			foreach(string pName in targetPostures)
			{
				Posture foundPosture = null;
				string transitionName = baseClip.name + "_to_" + pName;
				foreach(Posture p in postureList)
				{
					if(p.Name == pName)
					{
						foundPosture = p;
						continue;
					}
				}
				if(sourceAnims[transitionName] != null && foundPosture != null)
					transitions.Add(foundPosture, sourceAnims[transitionName].clip);
			}
		}
		else
			transitions = null;
	}
	
	/*public void Debug() {
		UnityEngine.Debug.Log("Posture: Name: " + Name + " idleRate: " + idleRate + " idleDev: " + idleDeviation);
		UnityEngine.Debug.Log("Posture: Transition: " + transitions.ToString());
	}*/
				
}

class PostureEqualityComparer : IEqualityComparer<Posture>
{
	public bool Equals(Posture p1, Posture p2)
	{
		bool match = true;
		if(p1.IdleClips.Count == p2.IdleClips.Count)
		{
			for(int i = 0; i < p1.IdleClips.Count; i++)
			{
				if(p1.IdleClips[i] != p2.IdleClips[i])
				{
					match = false;
					continue;
				}
			}
		}
		else
			match = false;
		
		if(match && p1.BaseClip == p2.BaseClip)
			return true;
		else
			return false;
	}
	
	public int GetHashCode(Posture p)
	{
		int hCode = p.BaseClip.name.GetHashCode();
		foreach(AnimationClip c in p.IdleClips)
			hCode += c.name.GetHashCode();
		return hCode.GetHashCode();
	}
}
	
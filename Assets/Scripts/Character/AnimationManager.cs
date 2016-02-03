//#define DEBUG_ANIM
//#define DEBUG_ANIM_WALK
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CharacterAnimState
{
	Idle,
	Transitioning,
	Interacting,
	Moving
}

public class AnimationManager : MonoBehaviour
{
	public GameObject body;
	public AnimationClip blinkClip;
	public AnimationClip breatheClip;
	public float defaultWalkTransition = 1f;
	public float blinkRate = 6f;
	public float blinkDev = 2f;
	public int blinkMax = 2;
	public bool blinkEnabled = true;
	public Transform browRoot;
	public bool canLookAt = true;
	private InteractionScript eventScript = null;
	public float animSpeed = 1; // for overrideing defaults
	public float animWeight = 1;
	public float animTime = 0;
	private bool scriptNameOverride = false;
	public string[] nowPlaying; // for debug. what is playing now , by layer
	public string lastLayerZero = "";
	public static Dictionary<string, AnimationClip> maleClips = null;
	public static Dictionary<string, AnimationClip> femaleClips = null;
	public static ScriptedObject animationScriptObject = null;
	
	private List<Posture> postures = new List<Posture>();
	private List<AnimatedInteraction> interactions = new List<AnimatedInteraction>();
	private int currentPosture = -1;
	private AnimatedInteraction currentInteraction = null;
	private string currentAnim = "";
	private AnimationClip baseClip;
	private AnimationClip idleClip;
	private AnimationClip walkClip;
	private bool idleEnabled = false;
	private float idleTime;
	private float idleRate;
	private float idleDev;
	private float walkSpeed;
	private float blinkTime;
	private int blinkNum = 1;
	private int blinkCurrent = 0;
	private Queue<AnimationClip> animQueue = new Queue<AnimationClip>();
	private CharacterAnimState animState;
	private float currentWalkSpeed;
	private LookAtController lookAt;
	private bool animInit = false;

	public string CurrentAnim
	{
		get { return currentAnim; }
	}
	
	public CharacterAnimState AnimState
	{
		get {return animState;}
		set 
		{	
			if(animState != value && !(value == CharacterAnimState.Idle && animState == CharacterAnimState.Moving))
			{
#if DEBUG_ANIM
				Debug.Log ("AM.AnimState.Set() : " + name + " changing state from " + animState + " to " + value);
#endif
				if(animState == CharacterAnimState.Transitioning)
					gameObject.GetComponent<TaskCharacter>().EndPostureChange();
				else if(animState == CharacterAnimState.Interacting)
				{
					currentInteraction = null;
					gameObject.GetComponent<TaskCharacter>().EndAnimatedInteraction();
				}
				if(value == CharacterAnimState.Idle)
				{
					if(NextIdle())
						idleEnabled = true;
				}
				else
					idleEnabled = false;
				animState = value;
			}
		}
	}
	
	public List<Posture> Postures
	{
		get {return postures;}
		set
		{
			postures = value;
			if(!animInit)
				InitAnimations();
		}
	}
	
	public List<AnimatedInteraction> Interactions
	{
		get {return interactions;}
		set {interactions = value;}
	}
	
		
	public AnimatedInteraction GetInteractionByName(string name)
	{
		for(int c = 0; c < interactions.Count; c++)
		{
			if(name == interactions[c].Name)
			{
				return interactions[c];
			}
		}
		return null;
	}
	
	public float CurrentWalkSpeed
	{
		get {return currentWalkSpeed;}
		set
		{
			currentWalkSpeed = value;
			if(walkSpeed != 0f && postures[currentPosture].CanWalk && walkClip != null)
				body.animation[walkClip.name].speed = currentWalkSpeed / walkSpeed;
		}
	}
	
	public float LookWeight
	{
		get 
		{
			if(canLookAt)
				return lookAt.Weight;
			else
				return 0f;
		}
		set
		{
			if(canLookAt)
				lookAt.Weight = value;
		}
	}
	
	public InteractionScript EventScript // called from AnimationMessenger when event is triggered
	{
		get 
		{
			if (eventScript != null) return eventScript;
			// look for a default eventScript matching the animation name
			if (animationScriptObject != null)
			{
				foreach (InteractionScript IS in animationScriptObject.scripts){
					if (IS.name == currentAnim)
						eventScript = IS;
				}
			}
			return eventScript;
		}
		set {eventScript = value;}
	}

	void ErrorAbort(string msg, float blendTime)
	{
		// let's try out this simple fix for the exploding breathing over no-base-clip bug
		// got PlayNext, but nothing queued.  Be sure the posture's base clip is playing.
		Debug.LogError ("AM.ErrorAbort(" + this.name + ") : " + msg + " : currentAnim=" + currentAnim + " : currentPosture=" + currentPosture);
		currentAnim = postures[currentPosture].BaseClip.name;
		Debug.LogError ("AM.ErrorAbort(" + this.name + ") : " + msg + " : setting to baseClip=" + currentAnim);
		if (!body.animation.IsPlaying(currentAnim)){
			AnimState = CharacterAnimState.Idle;
			body.animation.Rewind(currentAnim);
			body.animation.CrossFade(currentAnim, blendTime);
		}

		// this will pause the sim
		ScriptedObjectMonitor monitor = ScriptedObjectMonitor.GetInstance();
		if ( monitor != null )
			monitor.FlagError(msg);
	}
	
	public void PlayNext(float blendTime)
	{
		// there's something wrong about this scriptNameOverride logic. TODO Fix this!
		if (!scriptNameOverride)
			eventScript = null; // skip this one time if name was specified in interaction argument
		scriptNameOverride = false;
		
		if(animQueue.Count > 0)
		{
			currentAnim = animQueue.Dequeue().name;
#if DEBUG_ANIM
			Debug.Log ("AM.PlayNext(" + this.name + ") : blendTime=" + blendTime + " : currentAnim=" + currentAnim + " : time=" + Time.time );
#endif
			if(currentAnim == baseClip.name)
				AnimState = CharacterAnimState.Idle;
			else
				idleEnabled = false;

			if (animTime == 0) // if a call has overridden start time, don't rewind
				body.animation.Rewind(currentAnim);
			animTime = 0; // only use once
//			if (animWeight == 1){
				body.animation.CrossFade(currentAnim, blendTime);
//			} else {
				//blend also seems to mess up some event related stuff
//				body.animation.Blend(currentAnim,animWeight,blendTime); // this will leave the idle playing at 1-weight, we should blend it out.
//			}
			body.GetComponent<AnimationMessenger>().ExpectEventsFor(currentAnim);
		}
		else
		{	
			ErrorAbort("PlayNext() animQueue == 0", blendTime);
		}
	}
	
	private bool NextIdle()
	{
		if(currentPosture >= 0 && currentPosture < postures.Count && postures[currentPosture].CanIdle)
		{
			int idleIndex = Random.Range(0, postures[currentPosture].IdleClips.Count - 1);
			idleClip = postures[currentPosture].IdleClips[idleIndex];
			idleTime = Time.time + Random.Range (idleRate - idleDev, idleRate + idleDev);
			// ensure that the idle clip has a playnext on it
		}
		else
		{
			idleTime = Mathf.Infinity;
			idleClip = null;
		}
		
		return idleClip != null && idleTime != Mathf.Infinity;
	}
	
	private void NextBlink()
	{
		if(blinkMax == 1 || blinkCurrent == 0 || blinkCurrent == blinkNum)
		{
			if(blinkMax > 1)
				blinkNum = Random.Range(1, blinkMax + 1);
			blinkTime = Time.time + Random.Range(blinkRate - blinkDev, blinkRate + blinkDev);
			blinkCurrent = 0;
		}
		else
			blinkTime = Time.time + blinkClip.length + 0.1f;
	}
	
	private void RemoveLastAnim()
	{
        Queue<AnimationClip> tempQueue = new Queue<AnimationClip>();
        while (animQueue.Count > 1)
            tempQueue.Enqueue(animQueue.Dequeue());
		animQueue = tempQueue;
    }
	
	private List<AnimationClip> BuildTransition(Posture sourcePosture, Posture targetPosture)
	{
		return BuildTransition(sourcePosture, targetPosture, new List<Posture>());
	}
	
	private List<AnimationClip> BuildTransition(Posture sourcePosture, Posture targetPosture, List<Posture> skip)
	{
		AnimationClip foundClip;
		List<AnimationClip> clipList = new List<AnimationClip>();
		sourcePosture.Transitions.TryGetValue(targetPosture, out foundClip);
		if(foundClip)
			clipList.Add(foundClip);
		else
		{
			skip.Add(sourcePosture);
			List<List<AnimationClip>> options = new List<List<AnimationClip>>();
			foreach(KeyValuePair<Posture, AnimationClip> kvp in sourcePosture.Transitions)
			{
				if(!skip.Contains(kvp.Key))
				{
					List<AnimationClip> tempList = BuildTransition(kvp.Key, targetPosture, skip);
					if(tempList.Count > 0)
					{
						tempList.Insert(0, kvp.Value);
						options.Add(tempList);
					}
				}
			}
			int listSize = System.Int32.MaxValue;
			foreach(List<AnimationClip> l in options)
			{
				if(l.Count < listSize)
				{
					clipList = l;
					listSize = l.Count;
				}
			}
		}
		return clipList;
	}
	
	public void InitAnimations(int newPosture)
	{
		if(newPosture < postures.Count && newPosture >= 0)
		{
			animInit = NextPosture(newPosture, true);
//			currentPosture = newPosture;
//			animQueue.Clear();
//			baseClip = postures[currentPosture].BaseClip;
//			walkClip = postures[currentPosture].WalkClip;
//			idleRate = postures[currentPosture].IdleRate;
//			idleDev = postures[currentPosture].IdleDeviation;
//			walkSpeed = postures[currentPosture].WalkSpeed;
			if(animInit)
			{
				body.animation.Rewind(baseClip.name);
				body.animation.Play(baseClip.name);
				if(breatheClip != null){
					body.animation[breatheClip.name].blendMode = AnimationBlendMode.Additive;
					body.animation[breatheClip.name].layer = 1;
					body.animation.CrossFade(breatheClip.name);
				}
				else
					Debug.LogWarning(gameObject.name + " has null breatheClip.");
				if(NextIdle())
					idleEnabled = true;
				else
					idleEnabled = false;
				if(blinkClip != null)
					NextBlink();
			}
			else
				Debug.LogError(gameObject.name + " unable to set posture " + newPosture);
		}
		else
			Debug.LogError(gameObject.name + " has invalid Posture Index " + newPosture + " out of " + postures.Count);
	}
		
	public void InitAnimations()
	{
		int p = GetPostureByName(body.animation.clip.name);
		if(p > -1)
			InitAnimations(p);
		else
			InitAnimations(0);
	}
	
	public void InitAnimations(string newPostureName)
	{
		int p = GetPostureByName(newPostureName);
		if(p > -1)
			InitAnimations(p);
		else
			Debug.LogWarning(gameObject.name + " trying to InitAnimations for invalid Posture " + newPostureName);
	}
	
	private int GetPostureByName(string name)
	{
		int found = -1;
		for(int c = 0; c < postures.Count; c++)
		{
			if(name == postures[c].Name)
			{
				found = c;
				continue;
			}
		}
		return found;
	}
	
	private bool NextPosture(int newPosture, bool instant)
	{
		if(newPosture < 0 || newPosture >= postures.Count)
			return false;

		if(newPosture != currentPosture)
		{
#if DEBUG_ANIM
			Debug.Log ("AM.NextPosture(" + this.name + ") : posture name=" + postures[newPosture].Name + " : time=" + Time.time );
#endif
			if(postures[newPosture].BaseClip == null)
			{
				Debug.LogError(gameObject.name + " loading null base clip for posture " + newPosture);
				return false;
			}
			
			if(animQueue.Count > 0)
				RemoveLastAnim();
			
			if(!instant && currentPosture >= 0)
			{
				List<AnimationClip> transitionList = BuildTransition(postures[currentPosture], postures[newPosture]);
				// if the transition list contains no entries, then we need to inform the task character that
				// the posture change is completed
				if (transitionList == null || transitionList.Count == 0)
					gameObject.GetComponent<TaskCharacter>().EndPostureChange();

				foreach(AnimationClip c in transitionList)
				{
					if(c != null)
						animQueue.Enqueue(c);
					else
						Debug.LogWarning(gameObject.name + " loading null transition from " + currentPosture + " to " + newPosture);
				}
			}
			
			currentPosture = newPosture;
			baseClip = postures[currentPosture].BaseClip;
			animQueue.Enqueue(baseClip);

			idleEnabled = false;
			
			if(animState == CharacterAnimState.Idle || animState == CharacterAnimState.Moving)
				PlayNext(0.5f);
			
			if(postures[currentPosture].CanWalk)
			{
				walkClip = postures[currentPosture].WalkClip;
				walkSpeed = postures[currentPosture].WalkSpeed;
			}
			else
			{
				walkClip = null;
				walkSpeed = 0f;
			}
			
			if(postures[currentPosture].CanIdle)
			{
				idleRate = postures[currentPosture].IdleRate;
				idleDev = postures[currentPosture].IdleDeviation;
				//idleEnabled = true;
			}
			else
			{
				idleRate = 0f;
				idleDev = 0f;
				//idleEnabled = false;
			}
			return true;
		}
		else
			return false;
	}
	
	public bool NextPosture(string newPostureName, bool instant)
	{
		int p = GetPostureByName(newPostureName);
		bool result = false;
		if(p > -1)
			result = NextPosture(p, instant);
		else
			Debug.LogWarning(gameObject.name + " trying to go to invalid posture " + newPostureName);
		return result;
	}
	
	public bool NextPosture(string newPostureName)
	{
		return NextPosture(newPostureName, false);
	}
	
	public void DoInteraction(string interactionName)
	{
        bool found = false;
		eventScript = null;
		string argString = "";
		
		if (interactionName.Contains("?")){
			// process any affixed parameters, including 'script=', which must appear first if included
			// arguments should be of the form ?name=value?name=value?name=value
			// and can be referenced as script arguments within the animationScript as #name.
			// the expression ?name=value may include ?name=#arg or ?name=%variable and substitution should occur
			// in the originating script 
//(not implemented in the first pass)
			string[] p = interactionName.Split ('?');
			interactionName = p[0];

			animSpeed = 1; // for overrideing defaults
			animWeight = 1;
			animTime = 0;

			int start=1;
			if (p[1].ToLower().Contains("script=")){
				start=2;
				string[] q = p[1].Split('=');
				foreach (InteractionScript IS in animationScriptObject.scripts){
					if (IS.name == q[1]){
						eventScript = IS;
						scriptNameOverride = true;
					}
				}	
			}
			// process speed=  weight= time= possibly mixing transform...
			while (start < p.Length){
				if (p.Length > start && p[start].ToLower().Contains("speed=")){
					string[] q = p[start].Split('=');
					float.TryParse(q[1],out animSpeed);
				}
				if (p.Length > start && p[start].ToLower().Contains("weight=")){
					string[] q = p[start].Split('=');
					float.TryParse(q[1],out animWeight);
				}
				if (p.Length > start && p[start].ToLower().Contains("time=")){
					string[] q = p[start].Split('=');
					float.TryParse(q[1],out animTime);
				}
				start++;
			}

			body.animation[p[0]].speed = animSpeed;
			body.animation[p[0]].weight = animWeight; 	// weight gets overridden by crossfade.
			if (animTime >= 0) // use -1 to keep animation where it is but change it's speed...
				body.animation[p[0]].time = animTime;		// setting this beyond the initial anim event breaks the anim system, we might need to fake the skipped anim event

			for (int i=start;i<p.Length; i++){
				argString += p[i];
				if (i<p.Length-1)
					argString+=" ";
			}

		}
		
		//Debug.Log ("!!!!!!" + name + " Doing Interaction " + interactionName);

		for(int c = 0; c < interactions.Count; c++)
		{
			if(interactionName == interactions[c].Name)
			{
                found = true;
				currentInteraction = interactions[c];
				if(animQueue.Count > 0)
						RemoveLastAnim();
				foreach(AnimationClip ac in interactions[c].AnimClips)
				{
					if(ac != null && body.animation[ac.name] != null) // we might have one to load from resources
						animQueue.Enqueue(ac);
					else
						Debug.LogWarning(gameObject.name + " loading null Interaction Animation for " + interactions[c].Name);
				}
				if(baseClip != null)
					animQueue.Enqueue(baseClip);
				idleEnabled = false;
				
// we now search for a script name==currentAnim when an event is triggered,, so this check may be un needed.
/*
				if (eventScript == null){ // look for a default eventScript matching the animation name
					foreach (InteractionScript IS in animationScriptObject.scripts){
						if (IS.name == interactionName)
							eventScript = IS;
					}
				}
*/
				if(animState == CharacterAnimState.Idle)
					PlayNext(0.5f); // careful, PlayNext clears eventScript...
				
				//return; //we've done one, we should leave now.
				continue;
			}
		}

        if (found == false)
        {
			// see if there's a clip of this name on our animator
			AnimationClip aClip = null;
			if (body.animation[interactionName] != null)
				aClip = body.animation[interactionName].clip;  // do we want to be saving animation states ?
			if (aClip == null){
				// or a clip we loaded from resources
				if (maleClips.ContainsKey (interactionName)){ // have to check based on m/f
					aClip = maleClips[interactionName];
					body.animation.AddClip(aClip,interactionName);
				}
				// if so, add it to our animations
				
			}
			if (aClip != null){
				// inefficient, but let's see if there's a matching script here for now.
					//we should look in the resources folder for these, too.
				if (eventScript == null){ // look for a default eventScript matching the animation name
					foreach (InteractionScript IS in animationScriptObject.scripts){
						if (IS.name == interactionName)
								eventScript = IS;
								scriptNameOverride = true; // adding this so playnext doesn't clear. seems wrong.
					}
				}
				if(animQueue.Count > 0)
						RemoveLastAnim();
				animQueue.Enqueue(aClip);

				if(baseClip != null)
					animQueue.Enqueue(baseClip);
				idleEnabled = false;
				if(animState == CharacterAnimState.Idle)
					PlayNext(0.5f); // careful, PlayNext clears eventScript...
				
			}
            currentInteraction = null; // do we need to make one of these, or is this what we should save?
        }
		if (eventScript != null){
			if (argString != ""){
				eventScript.startingArgs = argString; // this replaces and overrides any editor supplied values...	
			}
			// pass along arguments from the script that called this DoInteraction
			// there may be some confusion here about which script it was.
			TaskCharacter tc = GetComponent<TaskCharacter>();
			ScriptedAction sa = null;
			if (tc != null) sa=tc.executingScript;
			if (sa!=null && sa.executedBy!= null){
				eventScript.startingArgs += " "+sa.executedBy.GetArgs();
			}
		}
	}
	
	public void DoInteraction(AnimatedInteraction newAI) // supply one with the call
	{
		currentInteraction = newAI;
		foreach(AnimationClip ac in currentInteraction.AnimClips)
		{
			if(animQueue.Count > 0)
				RemoveLastAnim();
			if(ac != null && body.animation[ac.name] != null)
				animQueue.Enqueue(ac);
			else
				Debug.LogWarning(gameObject.name + " loading null Interaction Animation for " + currentInteraction.Name);
		}
		if(baseClip != null)
			animQueue.Enqueue(baseClip);
		idleEnabled = false;
		if(animState == CharacterAnimState.Idle)
			PlayNext(0.5f);
	}

#if DEBUG_ANIM_WALK
	bool debugWalk=false;
#endif

	public bool Walk(float speed, float transition)
	{
		if(AnimState != CharacterAnimState.Idle)
		{
#if DEBUG_ANIM_WALK
			if ( debugWalk == false )
			{
				debugWalk = true;
				UnityEngine.Debug.LogError("AM.Walk(" + this.name + ") : trying to walk but in middle of animation=" + currentAnim);
			}
#endif
			return false;
		}

#if DEBUG_ANIM_WALK
		if ( animQueue.Count > 0 )
		{
			UnityEngine.Debug.LogError ("AM.Walk(" + this.name + ") : currentAnim=" + currentAnim + " : animQueue.Count=" + animQueue.Count);
			PrintValues (animQueue);
		}
		else
			UnityEngine.Debug.Log ("AM.Walk(" + this.name + ") : currentAnim=" + currentAnim + " : animQueue.Count=" + animQueue.Count);

		debugWalk = false;
#endif

		if(postures[currentPosture].CanWalk && walkClip != null)
		{
			idleEnabled = false;
			if(animQueue.Count > 0)
				animQueue.Clear();
			currentWalkSpeed = speed;
			body.animation[walkClip.name].speed = speed / walkSpeed;
			AnimState = CharacterAnimState.Moving;
			body.animation.CrossFade(walkClip.name, transition);
			return true;
		}
		else
			return false;
	}
	
	public bool Walk(float speed)
	{
		return Walk(speed, defaultWalkTransition);
	}
	
		public bool Walk()
	{
		return Walk(walkSpeed, defaultWalkTransition);
	}
	
	public static void PrintValues( IEnumerable myCollection )  
	{
		string output = "";
		foreach ( Object obj in myCollection )
		{
			output += " : " + obj;
		}
		UnityEngine.Debug.LogError ("QUEUE=" + output);
	}

	public void StopWalk(float transition)
	{
		if (baseClip != null && AnimState == CharacterAnimState.Moving)
		{
#if DEBUG_ANIM_WALK
			UnityEngine.Debug.Log ("AM.StopWalk(" + this.name + ") : currentAnim=" + currentAnim + " : animQueue.Count=" + animQueue.Count + " : animState=" + animState);
#endif
			if (animQueue.Count == 0)
				animQueue.Enqueue (baseClip);

			if (animQueue.Count >= 1)
			{
				animState = CharacterAnimState.Idle;
				PlayNext (transition);
			}
		}
	}

	public bool CanWalk()
	{
		return (AnimState == CharacterAnimState.Idle);
	}
	
	public void StopWalk()
	{
		StopWalk(defaultWalkTransition);
	}
	
	public void LookStart(Transform newTarget, float newWeight, float time)
	{
		if(canLookAt)
		{
			lookAt.target = newTarget;
			lookAt.Weight = newWeight;
            lookAt.StopTime = time;
            lookAt.Active = true;
		}
	}
	
	public void LookStart(Transform newTarget, float stopTime)
	{
		LookStart(newTarget, 1f, stopTime);
	}
	
	public void LookStop()
	{
		if(canLookAt)
			lookAt.Active = false;
	}
	
	public Transform GetBone(string boneName)
	{
		return GetBone(gameObject.transform, boneName);
	}
	
	public Transform GetBone(Transform startBone, string boneName)
	{
		if(startBone.name == boneName)
			return startBone;
		else if(startBone.childCount > 0)
		{
			foreach(Transform b in startBone)
			{
				Transform match = GetBone(b, boneName);
				if(match)
					return match;
			}
			return null;
		}
		return null;
	}
	
	public void Awake()
	{
		if(canLookAt)
			lookAt = body.GetComponent<LookAtController>();
		if(lookAt == null)
			canLookAt = false;
		
		if(breatheClip != null)
		{
			body.animation[breatheClip.name].blendMode = AnimationBlendMode.Additive;
			body.animation[breatheClip.name].layer = 1;
		}
		else
			Debug.LogWarning(gameObject.name + " starting with no breatheClip!!");
		
		//if(postures.Count > 0 && !animInit)
			//InitAnimations();
		// whoever gets here first can build these:
		if (maleClips == null){
			maleClips = new Dictionary<string,AnimationClip>();
			Object[] mObjs = Resources.LoadAll("Animations/Male");
			// this seems pretty lame, but this is how we apparently have to instantiate these
			for (int i=0; i<mObjs.Length; i++){
				Instantiate(mObjs[i]);
				maleClips.Add (mObjs[i].name, mObjs[i] as AnimationClip);
			}	
		}
		if (femaleClips == null){
			femaleClips = new Dictionary<string,AnimationClip>();
			Object[] mObjs = Resources.LoadAll("Animations/Female");
			// this seems pretty lame, but this is how we apparently have to instantiate these
			for (int i=0; i<mObjs.Length; i++){
				Instantiate(mObjs[i]);
				femaleClips.Add (mObjs[i].name, mObjs[i] as AnimationClip);
			}
		}
		
		if (animationScriptObject == null){
			Object SOB = Resources.Load("Animations/AnimationScripts");
			GameObject go = Instantiate (SOB) as GameObject;
			if (go != null){
				animationScriptObject = go.GetComponent<ScriptedObject>();
			}
		}
	}
	
	public void Start()
	{
		if(blinkClip != null && body.animation[blinkClip.name] != null)
		{
			body.animation[blinkClip.name].AddMixingTransform(browRoot);
			body.animation[blinkClip.name].blendMode = AnimationBlendMode.Blend;
		}
		
		else
			blinkEnabled = false;
			
		//if(postures.Count > 0 && !animInit)
			//InitAnimations();
	}
	
	public void Update()
	{
		if(idleEnabled && Time.time >= idleTime)
		{
			if(AnimState == CharacterAnimState.Idle)
			{
				if(idleClip != null)
				{
#if DEBUG_ANIM
					Debug.Log ("AM.PlayNext(" + this.name + ") : idling : time=" + Time.time);
#endif
					animQueue.Enqueue(idleClip);
					animQueue.Enqueue(baseClip);
					PlayNext(0.5f);
					NextIdle();
				}
				else
					Debug.LogWarning("Trying to play null idle clip in Posture " + currentPosture);
			}
			else
				NextIdle ();
		}
		if(blinkEnabled && Time.time >= blinkTime)
		{
			if(blinkClip != null)
			{
				blinkCurrent++;
				body.animation.Rewind(blinkClip.name);
				body.animation.Play(blinkClip.name);
				NextBlink();
				
			}
			else
				Debug.LogWarning("Trying to play null Blink Clip on " + gameObject.name);
		}
		if (true || Application.isEditor){
			nowPlaying = new string[5+animQueue.Count];
			foreach (AnimationState ast in body.animation){
				if (body.animation.IsPlaying(ast.name)){
					nowPlaying[ast.layer] += ast.name + " "+body.animation[ast.name].normalizedTime;
				}
			}
			int i=4;
			foreach(AnimationClip clip in animQueue){
				nowPlaying[i++]="Q["+(i-5)+"]"+clip.name;
			}
			if (idleClip!=null)
				nowPlaying[i]="NI="+idleClip.name+(idleTime-Time.time);
			
			if (nowPlaying[0]!=null && nowPlaying[0]!="") lastLayerZero = nowPlaying[0];
			else if (lastLayerZero != "")
			{
				Debug.LogError(name+" AnimationLayer 0 EMPTY after playing "+lastLayerZero);
				if (animQueue.Count > 0)
				{
					string error = "AnimQueue was not empty when error was detected. Probabably skipped a PlayNext. Calling PlayNext";
					Debug.LogError(error);
					PlayNext(0.5f);
									
					// this will pause the sim
					ScriptedObjectMonitor monitor = ScriptedObjectMonitor.GetInstance();
					if ( monitor != null )
						monitor.FlagError(error);
				}
				else
				{
					string error = "AnimQueue was empty when error was detected. Trying to start base clip.";
					Debug.LogError(error);
					Debug.Break (); // we really want to catch this!
					string baseAnim = postures[currentPosture].BaseClip.name;
					if (!body.animation.IsPlaying(baseAnim)){
						body.animation.Rewind(baseAnim);
						body.animation.CrossFade(baseAnim, 0.5f);
					}
					
					// this will pause the sim
					ScriptedObjectMonitor monitor = ScriptedObjectMonitor.GetInstance();
					if ( monitor != null )
						monitor.FlagError(error);
				}
			}
		}
	}
	
	public void Detach(string attachName)
	{
		if(currentInteraction!= null && currentInteraction.AttachableObjects.Count > 0)
			currentInteraction.AttachableObjects[attachName].obj.transform.parent = currentInteraction.AttachableObjects[attachName].naturalParent;
	}
	
	public void Attach(string attachName)
	{
		if(currentInteraction!= null && currentInteraction.AttachableObjects.Count > 0)
		{
			Transform attachBone = GetBone(currentInteraction.AttachableObjects[attachName].targetBoneName);
			if(attachBone == null)
				attachBone = GameObject.Find(currentInteraction.AttachableObjects[attachName].targetBoneName).transform;
			if(attachBone != null)
				currentInteraction.AttachableObjects[attachName].obj.transform.parent = attachBone;
			else
				Debug.LogError(gameObject.name + " doing interaction " + currentInteraction.Name + " failed to find bone " + currentInteraction.AttachableObjects[attachName].targetBoneName);
		}
		else
			Debug.LogError(gameObject.name + " doing interaction " + currentInteraction.Name + " has no AttachableObjects.");
	}
	
	public void PlayInteractSubAnim(string subAnimName)
	{
		if(currentInteraction.SubAnims != null && currentInteraction.SubAnims.Count > 0)
		{
			SubAnim sa = currentInteraction.SubAnims[subAnimName];
			if(sa != null)
			{
				sa.obj.animation.Rewind(sa.animName);
				sa.obj.animation.Play(sa.animName);
			}
			else
				Debug.LogError(gameObject.name + " doing interaction " + currentInteraction.Name + " failed to play SubAnim " + sa.animName);
		}
		else
			Debug.LogError(gameObject.name + " doing interaction " + currentInteraction.Name + " has no SubAnims.");
	}
	
	public void PlayPostureSubAnim(string subAnimName)
	{
		//Debug.Log(gameObject.name + " trying to play sub anim " + subAnimName + " on frame " + body.animation["bvm1"].time);
		if(postures[currentPosture].SubAnims != null && postures[currentPosture].SubAnims.Count > 0)
		{
			SubAnim sa = postures[currentPosture].SubAnims[subAnimName];
			if(sa != null)
			{
				sa.obj.animation.Rewind(sa.animName);
				sa.obj.animation.Play(sa.animName);
			}
			else
				Debug.LogError("Sub Anim " + subAnimName + " failed to play.");
		}
	}
}
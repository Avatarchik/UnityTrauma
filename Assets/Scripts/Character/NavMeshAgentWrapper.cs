//#define DEBUG_NAVIGATION

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NavMeshAgentWrapper : MonoBehaviour {
	Vector3 destPos = Vector3.zero;
	bool navRequested = false; // should use a state mechanism here
	public bool isNavigating = false;
	public bool navCompleted = false;
	public bool isAvoiding = false;
	public bool holdPosition = false; // may be set by character tasks so we don't move from our requested position
	public bool lockPosition = false; // stays set longer than hold position
	public float heading = 0;
	public float speed = 1.0f;
	public Transform headingTransform = null;
	private List<GameObject> reattachedObjects = new List<GameObject>();
	public int priority = 0;
	NavMeshAgent m_navMeshAgent = null;
	public float agentRadius = 0.22f; // the radius of our agent, which might not be present while we are stopped.
	NavMeshObstacle m_navMeshObstacle = null;
	NavMeshAgentWrapper makingWayFor = null;
	float failsafeSlider = 0; // this will count down from 1.0 if navigation fails to guide interpolation.
	private Vector3 finalDestination = Vector3.zero;
	private Quaternion finalRotation = Quaternion.identity;
	private bool hasFinalRotation = false;
	Quaternion lerpFromRotation = Quaternion.identity;
	private Vector3 steeringTarget = Vector3.zero; // just for debug draw
	float lastAvoidCheckTime = 0;
	float moveStartTime =0;
	int avoidCount = 0;
	public float moveTimeout = 15.0f; // if you're not there by now, we are putting you there.
//	private float minAvoidTime = 1.0f; //once you start avoiding, continue for at least this long
	private float avoidTime = 0;
//	private float lastBump = 0;
	public float avoidCheckInterval = 0.1f; // how often to see if we might run into someone
//	private float bumpAmount = 0.035f; // how much to move us to the right when we need to avoid
	private float defaultSpeed = 1.5f;
	// debug variables
	public float distance = 0;
	public string TargetName = "";
	public int layermask = 0;
	public Vector3 avoidPosition = Vector3.zero;
	Vector3[] avoidHistory = new Vector3[22];
	Vector3[] avoidPoints;
	public Vector3 nextPointMarker = Vector3.zero; // just for nav debugging
	
	void Awake()
	{
		m_navMeshAgent = GetComponent<NavMeshAgent>();
		m_navMeshObstacle = GetComponent<NavMeshObstacle>();
		if (m_navMeshAgent != null){
			defaultSpeed = m_navMeshAgent.speed;
//			m_navMeshAgent.autoBraking = false; // this seems to hurt a lot more than it helps.
//			m_navMeshAgent.stoppingDistance = 0.1f;
		}
		lastAvoidCheckTime = Time.time;
		//Time.timeScale = 0.25f;
		if (headingTransform == null){
			LookAtController model = GetComponentInChildren(typeof(LookAtController)) as LookAtController;
			if (model != null) headingTransform = model.transform;			
		}
	}
	
	void Update ()
	{
		avoidTime -= Time.deltaTime;
		if (navRequested)
		{
			if (m_navMeshAgent == null){
				Debug.LogWarning (name+" lost agent while navRequested.");
				m_navMeshAgent = RecreateAgent();
				lastAvoidCheckTime = 0; // cause us to take the timeout exit path to our destination
			}
			
			if (m_navMeshAgent.hasPath)
			{
				navRequested = false;
				isNavigating = true;
//				if (!CheckForAvoidance()) // this will check every frame while needed
						lastAvoidCheckTime = Time.time;
			} else if (Time.time - lastAvoidCheckTime > avoidCheckInterval) {
				// failed to path in time, return failure
//Debug.LogWarning(name+" NavMeshAgent failed to find initial path fast enough. Warping!");
				SendMessageUpwards("OnNavigationRequestFailed",SendMessageOptions.DontRequireReceiver);
				navRequested = false;
				m_navMeshAgent.Stop();
//				GetComponent<AnimationManager>().StopWalk(0.5f);
				m_navMeshAgent.updateRotation = false; // apparently this is needed to keep the positino and ritation from being updated
				
				// new code here: 4/2/13 PAA
				// this works well, but we have to be careful that the destination is a good one.
				// people were sinking down to avoid positions...
				// it didn't solve the very short distance navigation problem, though.
				isNavigating = true;
				failsafeSlider = 1.0f; // lerp to the failed point...
				moveStartTime = Time.time-moveTimeout-1.0f; // ensure that the lerp starts now.
				// somehow, final destination can end up below the floor, so we are correcting for that here.
//				finalDestination.y = transform.position.y; // don't lerp vertically
				
			}
		}
		
		if (isNavigating)
		{
			if (failsafeSlider <= 0 && Time.time - moveStartTime > moveTimeout)
				failsafeSlider = 1.0f;
			
			if (failsafeSlider > 0){ // we have run into trouble so just lerp to the final destination
//				Debug.LogWarning(name+" navigation failed to complete, Warping!");
				isAvoiding = false;
				// we should turn off the collider at this point to avoid pushing anybody aside!
				if (collider != null)
					collider.isTrigger = true; // this didn't work, check on the nav mesh agent and capsule collider
				
				if (m_navMeshAgent != null){
					//m_navMeshAgent.SetDestination(finalDestination); // let the final lerp do the node alignment
					// There is an unfortunate characteristic of the Unity NavMeshAgent, that when you take over
					// moving the object like this, the agent retains it's last position, and the next time
					// you let the agent drive, it snaps back to that spot.  
					// The only way I found to get around this is to Destroy then Recreate the agent!
					m_navMeshAgent.Stop(true);
					m_navMeshAgent.ResetPath();
					Object.Destroy(m_navMeshAgent);
					m_navMeshAgent = null;
				}
//				m_navMeshAgent.Stop(true); // let the final lerp do the node alignment
//				m_navMeshAgent.updateRotation = false; 
//				m_navMeshAgent.updatePosition = false;
				transform.position = Vector3.Lerp(transform.position,finalDestination,1.0f-failsafeSlider);
				failsafeSlider -= Time.deltaTime/2.0f; // this gives us a 2 second lerp.
				if (failsafeSlider < 0.5f) GetComponent<AnimationManager>().StopWalk(0.25f); // start blending out the walk...
				if (failsafeSlider <= 0 && Time.time - moveStartTime > moveTimeout){
					transform.position = finalDestination;
					if (m_navMeshAgent == null) RecreateAgent ();
					isNavigating = false;
					failsafeSlider = 0;
					if (collider != null)
						collider.isTrigger = false;
					LockMe ();
					makingWayFor = null; // if i was getting out of someone's way, i have done that now.
					SendMessageUpwards("OnNavigationRequestSucceeded",SendMessageOptions.DontRequireReceiver); // should really wait for alignment to complete for this
					GetComponent<TaskCharacter>().Arrive();
					GetComponent<AnimationManager>().StopWalk(0.25f); // this would be better moved to TaskCharacter.Arrive()
				}
				return;
			}
			
			distance = m_navMeshAgent.remainingDistance;
			if (!isAvoiding){ // blend to normal speed if not avoiding
/*				
				if (m_navMeshAgent.speed < defaultSpeed)
					m_navMeshAgent.speed = m_navMeshAgent.speed*1.1f;
				else
					m_navMeshAgent.speed = defaultSpeed;
*/
				
				// lets start the alignment a bit earlier
				if ( hasFinalRotation && distance < 3.0f*m_navMeshAgent.radius){ 
					m_navMeshAgent.updateRotation = false;
						
					// if we were walking backwards, transfer the heading rotation up to the parent and let the lerp to
					// final rotation do the final alignment
					if (heading != 0 && headingTransform != null){
						// fool the nav mesh agent into walking backwards by setting a rotation and counter rotation
						// on the two top hierarchy nodes
						foreach (GameObject go in reattachedObjects){
							go.transform.parent = null;	
						}								
						transform.rotation *= Quaternion.AngleAxis(-heading,Vector3.up);
						headingTransform.localRotation = Quaternion.identity;
						heading = 0;
						foreach (GameObject go in reattachedObjects){
							go.transform.parent = transform;	
						}
						reattachedObjects.Clear();
					}
					if (lerpFromRotation == Quaternion.identity)
						lerpFromRotation = transform.rotation;
					
					float denominator = 3.0f*m_navMeshAgent.radius;
					float numerator = denominator - distance;
					
					
					float lrp = 0.75f*numerator/denominator;// (3.0f*m_navMeshAgent.radius - distance) / 3.0f*m_navMeshAgent.radius;
					// technically, this kind of lerp should retain the starting value instead of
					// using the current value, as this leads to a non-linear interpolation, but this is easier to code
					Quaternion desiredRotation = Quaternion.Lerp(lerpFromRotation,finalRotation,lrp);
					// if the angle is too great, limit rotation and hold 'distance' above threshold until rotation can complete to avoid snapping on short trips
					float remainingAngle;
					Vector3 rotAxis;
					Quaternion delta = Quaternion.Inverse(transform.rotation) * desiredRotation;
					delta.ToAngleAxis(out remainingAngle,out rotAxis);
					float maxSlew = 3.0f*(Time.timeScale+.01f);
					if (remainingAngle > maxSlew){
						delta = Quaternion.AngleAxis(maxSlew,rotAxis);
						transform.rotation = transform.rotation * delta;
						if (distance < 0.01f)
							distance = 0.011f; // so we don't trigger arrival yet...
					}
					else
					{
						transform.rotation = Quaternion.Lerp(lerpFromRotation,finalRotation,lrp);
					}
				}
				
				if (distance < 0.01f) // was 0.01f, but having occasional race conditions
				{	
					transform.position = m_navMeshAgent.destination; // snap to final
					lerpFromRotation = Quaternion.identity;
					
					m_navMeshAgent.Stop(); // let the final lerp do the node alignment
					m_navMeshAgent.updateRotation = false; // apparently this is needed to keep the position and rotation from being updated. = false; 
					isNavigating = false;
					makingWayFor = null; // if i was getting out of someone's way, i have done that now.
					if ((m_navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete))
					{
						SendMessageUpwards("OnNavigationRequestFailed",SendMessageOptions.DontRequireReceiver); // should really wait for slignment to complete for this
					} else {
						SendMessageUpwards("OnNavigationRequestSucceeded",SendMessageOptions.DontRequireReceiver); // should really wait for alignment to complete for this
					}
					LockMe (); // may destroy the agent
#if DEBUG_NAVIGATION
					UnityEngine.Debug.Log ("NavMeshAgent "+name+" Arrived at "+TargetName);
#endif
					GetComponent<TaskCharacter>().Arrive();
					GetComponent<AnimationManager>().StopWalk(0.5f); // this would be better moved to TaskCharacter.Arrive()
				}
				// see if anyone is in our path
				if (Time.time - lastAvoidCheckTime > avoidCheckInterval)
				{
					if (!CheckForAvoidance()) // this will check every frame while needed
						lastAvoidCheckTime = Time.time;
				}
			}
			else
			{ // is Avoiding, repath when one radius from goal to avoid slowdown ?
				if (distance < m_navMeshAgent.radius*1.0f)
				{
					m_navMeshAgent.SetDestination(finalDestination);
					isNavigating = false;
					isAvoiding = false;
					navRequested = true;
					return;
				}
				else
				{
					// see if anyone ELSE is in our path
					if (Time.time - lastAvoidCheckTime > avoidCheckInterval)
					{
						if (!CheckForAvoidance()) //
							lastAvoidCheckTime = Time.time;
					}
				}
			}
		} 
		else 
		{
			isAvoiding = false;
		}
	}
	
	NavMeshAgent RecreateAgent(){
		m_navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
		// initialize non-defaults
		m_navMeshAgent.radius = 0.22f;
		m_navMeshAgent.speed = defaultSpeed;
		m_navMeshAgent.angularSpeed = 220; // was 120
		m_navMeshAgent.height = 1.7f;
		m_navMeshAgent.acceleration = 3; // was 2
//		m_navMeshAgent.stoppingDistance = 0.1f;
//		m_navMeshAgent.autoBraking = false; // this seems to hurt a lot more than it helps.
		
		return m_navMeshAgent;
	}
	
	void setHeading(float heading){
		// we have to detach then re-attach any attached objects so they don't get spun
		reattachedObjects.Clear();
		TaskCharacter tc = GetComponent<TaskCharacter>();
		if (tc != null){
			foreach (GameObject go in tc.attachedObjects){
				// if this thing is really parented to us
				if (go.transform.parent = transform){
					reattachedObjects.Add(go);
					go.transform.parent = null;					
				}				
			}			
		}
		transform.rotation *= Quaternion.AngleAxis(heading,Vector3.up);
		headingTransform.localRotation = Quaternion.AngleAxis(-heading,Vector3.up);
		
		foreach (GameObject go in reattachedObjects){
			go.transform.parent = transform;	
		}
	}
	
	void OnDrawGizmos()
	{
		if (isNavigating && m_navMeshAgent!= null)
		{
			if (isAvoiding) {
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(avoidPosition, 0.25f);
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(nextPointMarker, 0.25f);
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireSphere(steeringTarget, 0.25f);
				Gizmos.color = Color.cyan;
				
				if (avoidPoints != null){
					Gizmos.color = Color.gray;	
					for (int i = 0; i< avoidPoints.Length; i++){
						Gizmos.DrawWireSphere(avoidPoints[i], 0.15f);
					}
				}
				if (avoidHistory != null){
					Gizmos.color = Color.black;	
					Gizmos.DrawWireSphere(avoidHistory[0], 0.66f);
					for (int i = 0; i< avoidHistory.Length; i++){
						Gizmos.DrawWireSphere(avoidHistory[i], 0.15f);
					}
				}
			} else {
				Gizmos.color = Color.blue;
			}
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			Vector3 checkDir = m_navMeshAgent.velocity;
			checkDir.y = 0;
			
			Gizmos.DrawWireSphere(transform.position+checkDir.normalized*1.0f,.25f);
		} else if (navRequested) {
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
		}
	}
	
	public bool MoveToPosition(Vector3 targetPosition, float replanInterval)
	{
		FreeMe ();
//		if (m_navMeshAgent == null) RecreateAgent ();
		if (m_navMeshAgent != null)
		{
			moveStartTime = Time.time;
			avoidCount = 0;
			failsafeSlider = 0;
			destPos = targetPosition;
			finalDestination = destPos;
			hasFinalRotation = false;
			avoidHistory = new Vector3[22];
			//destPos.y = transform.position.y;
//			m_navMeshAgent.speed = defaultSpeed*0.5f;
			m_navMeshAgent.SetDestination(destPos);
			m_navMeshAgent.updatePosition = true;
			m_navMeshAgent.updateRotation = true;
			navRequested = true;
			lastAvoidCheckTime = Time.time+2;  // was 1, ensure there is enough time for original pathfinding
			return true;
		}
		else
		{
			Debug.LogWarning(name+" MoveToPosition:FreeMe failed to create NavMeshAgent");
			return false;
		}
	}
	
	public bool MoveToGameObject(GameObject targetObject, float replanInterval)
	{
#if DEBUG_NAVIGATION
					UnityEngine.Debug.Log ("NavMeshAgent "+name+" Sent to "+targetObject.name);
#endif
		FreeMe ();
//			if (m_navMeshAgent == null) RecreateAgent ();
		if (m_navMeshAgent != null)
		{
			moveStartTime = Time.time;
			avoidCount = 0;
			failsafeSlider = 0;
			TargetName = targetObject.name;
			destPos = targetObject.transform.position;
			//destPos.y = transform.position.y;
			finalDestination = destPos;
			hasFinalRotation = true;
			finalRotation = targetObject.transform.rotation;
			avoidHistory = new Vector3[22];
			
			// if we are already within the distance threshold, don't even start the navMeshAgent, just lerp and be done
			if (Vector3.Distance(transform.position,destPos) < 0.01f){
				isNavigating = true;
				failsafeSlider = 1.0f; // start an interpolation, trigger normal completion code
				moveStartTime = 0;
				return true;
			}
//			m_navMeshAgent.speed = defaultSpeed*0.5f;
			m_navMeshAgent.SetDestination(destPos);
			m_navMeshAgent.updatePosition = true;
			m_navMeshAgent.updateRotation = true;
			navRequested = true;
			lastAvoidCheckTime = Time.time+1;  // ensure there is enough time for original pathfinding
			if (heading != 0 && headingTransform != null){
				setHeading(heading);
				// fool the nav mesh agent into walking backwards by setting a rotation and counter rotation
				// on the two top hierarchy nodes

			}
			return true;
		}
		else
		{
			Debug.LogWarning(name+" MoveToGameObject:FreeMe failed to create NavMeshAgent");
			return false;
		}
	}

	public void CancelActiveRequest()
	{
		m_navMeshAgent.Stop(); // let the final lerp do the node alignment
		m_navMeshAgent.updateRotation = false; // apparently this is needed to keep the position and rotation from being updated. = false; // apparently this is needed to keep the positino and ritation from being updated
		isNavigating = false;
		navRequested = false;
		//m_steeringAgent.StopSteering();
		//m_pathAgent.CancelActiveRequest();
		GetComponent<TaskCharacter>().Arrive();
	}
	
	bool CheckForAvoidance()
	{
		if (isAvoiding) return false; // this is for the new repathing
		if (m_navMeshAgent == null) return false;
		//return false;
		
		// Unity's NavMesh Agent does pretty well at avoidance for low numbers of npc's, but with larger numbers, sometimes
		// it needs a little help.  this bumps agents to the right if they seem to be about to collide.

		// see if anything is in our way, and if so, nudge slightly to the right, and return true
		// do a spherecast in the direction of our motion, out to 3 meters but not beyond out next path node,
		// and see if we hit any other capsules.
		//  can we move directly to the final position without passing through anything...
		RaycastHit[] hits;
		List<NavMeshAgentWrapper> ignoreAgents = new List<NavMeshAgentWrapper>();
		LayerMask mask = 1 << LayerMask.NameToLayer("NPC"); 
		//layermask = mask.value;
		float checkDistance = m_navMeshAgent.speed*0.5f; // was 1.0f
		if (checkDistance > m_navMeshAgent.remainingDistance)
			checkDistance = m_navMeshAgent.remainingDistance;
		Vector3 checkDir = m_navMeshAgent.desiredVelocity;
		checkDir.y = 0;
		
		steeringTarget = m_navMeshAgent.steeringTarget;
		
		// inform any npc's along our path, beyond distance radius, that we are coming their way...
		Vector3 steeringDir = m_navMeshAgent.steeringTarget-transform.position;
		
//new, because the desired velocity isn't really along our path, it may be correctional and very strong.	 4/2/13	
		checkDir = steeringDir;
		checkDir.y=0;
		checkDir = checkDir.normalized;
		
		if ( steeringDir.magnitude > m_navMeshAgent.radius*0.00001f){
			hits = Physics.SphereCastAll(transform.position,
					m_navMeshAgent.radius*1.0f, steeringDir.normalized,
					steeringDir.magnitude+m_navMeshAgent.radius*2.0f,
					mask.value);
			foreach (RaycastHit hit in hits)
			{
				NavMeshAgentWrapper w = hit.collider.gameObject.GetComponent<NavMeshAgentWrapper>();
				if (w && w!= this){
					if (w.WarnCollision(this, hit))
						ignoreAgents.Add(w); // don't repath around agents who said they will move out of your way
				}
			}
		}
		
		//spherecast twice the radius, from a point 0.1 behind the center but not outside the radius of our nav mesh agent
		hits = Physics.SphereCastAll(transform.position-checkDir*0.0f, m_navMeshAgent.radius*1.0f, checkDir*1.1f, checkDistance, mask.value);
		foreach (RaycastHit hit in hits)
		{
			// identify the hit collider and avoid if it's another npc
//			NavMeshAgent hitAgent = hit.collider.GetComponent<NavMeshAgent>();
			NavMeshAgentWrapper hitWrapper = hit.collider.GetComponent<NavMeshAgentWrapper>();
			if (hitWrapper != null)
			{
				if (!ignoreAgents.Contains (hitWrapper)){ // we should have warned them!
					if (hitWrapper.WarnCollision(this, hit))
						continue;
				}
				if ((priority > hitWrapper.priority || !hitWrapper.isNavigating) && !ignoreAgents.Contains (hitWrapper)){
#if DEBUG_NAVIGATION
					Debug.Log (name+" repathing to avoid "+hitWrapper.name+" ignoring "+ignoreAgents.Count);
#endif
					AvoidanceRepath(hitWrapper.gameObject,m_navMeshAgent.radius); // should check on priorities?
					return true;
				}
				else
					continue;
			}
			else // this might have been causing the premature failsafe...
			{	// we hit something layered NPC (or our future obstacle layer)
#if DEBUG_NAVIGATION
				Debug.Log (name+"detected hit with "+hit.collider.gameObject.name);
#endif
				AvoidanceRepath(hit.collider.gameObject,0.22f); // should check on priorities?
					return true;
			}
		}
		// there was no hit.  should we continue avoiding for a bit ?
//		isAvoiding = false;
//		if (m_navMeshAgent.speed < defaultSpeed)
//			m_navMeshAgent.speed = m_navMeshAgent.speed*1.1f;
//		else
//			m_navMeshAgent.speed = defaultSpeed;
		
		return false; // we didn't hit anything, so no need to keep rechecking
	}
	
	public bool AvoidanceRepath(GameObject avoidAgent,float radius){
		if (navRequested) return true; //we are already moving to avoid this guy
		
		if (avoidCount == 0){
			avoidHistory = new Vector3[22]; // new 4/2/13
			avoidHistory[0] = transform.position;
		}
		avoidCount++;
#if DEBUG_NAVIGATION
		Debug.Log (name+" avoiding "+avoidAgent.name+avoidCount+" "+Time.time);
#endif
		if (avoidCount > 20)
		{
			Debug.LogWarning(name+" had too many avoid failures. following too closely?");
			failsafeSlider = 1.0f; // start an interpolation
			moveStartTime = 0;
			//Debug.Break();
		}
			
		// we should generate a bunch of repath avoidance point options, then rank them by a cost metric,
		avoidPoints = new Vector3[18];
		if (!isAvoiding){ // only set this at the top level call to avoidance
			nextPointMarker = m_navMeshAgent.steeringTarget; // for debug display	
		}
		
		// based on how far they take us off path, both angularly, 
		//whether we collide from there, whether we can get there without hitting anything etc.
		
		
		// try to choose a point 3*radius to the right or left of the Agent, which is on the navmesh and can be reached. 
		
//		Vector3 delta = avoidAgent.transform.position - transform.position;
		Vector3 delta = m_navMeshAgent.steeringTarget - transform.position;
		delta.y=0;
		// we might want to back up a step towards us with this intermediate position...
		
		Vector3 offset = -Vector3.Cross(delta,transform.up).normalized * radius*3.0f;
		
		// test whether offset or -offset is a better first choice based on distance from our steering target,
		// swap if needed
		if ((nextPointMarker-avoidAgent.transform.position-offset).magnitude >
			(nextPointMarker-avoidAgent.transform.position+offset).magnitude)
			offset = -offset;
		
		// for our first two points, try either side of the detected agent, tangent to our goal direction
		
		avoidPoints[0] = avoidAgent.transform.position + 2.1f/3.0f*offset;
		avoidPoints[1] = avoidAgent.transform.position - 2.1f/3.0f*offset;
				
//		Vector3 newPoint;// the point we will check first
		Vector3 backOff = -delta.normalized*m_navMeshAgent.radius*1.0f; //one diameter back towards us from
		// build an array of choices for our avoid point. alternate left and right, increasing radius and backoff
		float sweep = 0.5f;
		float back=1.0f;
		for (int i=2; i<18; i+=2){
			avoidPoints[i] = avoidAgent.transform.position+sweep*offset+back*backOff;	
			avoidPoints[i].y = transform.position.y;
			avoidPoints[i+1] = avoidAgent.transform.position-sweep*offset+back*backOff;
			avoidPoints[i+1].y = transform.position.y;
			sweep += 0.3f; //1.7max
			back *= 1.3f; //2.14max
		}
		
		for (int i=0;i<18;i++){
			if (IsGoodAvoidPosition(avoidPoints[i])){
				m_navMeshAgent.SetDestination(avoidPoints[i]);
				avoidPosition = avoidPoints[i];
				avoidHistory[avoidCount] = avoidPosition;
				isAvoiding = true;
				navRequested = true;
				isNavigating = false;
				return true;
			}
		}
		isAvoiding = false;
		return false; // couldnt find a good point...
	}
	
	bool IsGoodAvoidPosition(Vector3 newPoint){
		NavMeshHit hit;
		if (NavMesh.SamplePosition(newPoint,out hit, 0.01f, -1)){// && should also Ray trace and sphere cast test this
			// raytrace for geometry TODO
			
			for (int h=0;h<avoidCount;h++){ // if this is too close to a point we alreadty tried, don't go back there.
				float r = m_navMeshAgent.radius;
				if (h==0) r*=2; // was r*=3
				if (Vector3.Distance(newPoint,avoidHistory[h])< r)
					return false;
			}
			
			// spherecast
			RaycastHit rhit;
//			LayerMask mask = 1 << LayerMask.NameToLayer("NPC"); 
			Vector3 checkDir = (newPoint - transform.position).normalized;
			float checkDistance = (newPoint - transform.position).magnitude;
			// capsule cast instead ?
			if (!Physics.SphereCast(transform.position-checkDir*0.1f, m_navMeshAgent.radius*1.0f, checkDir*1.1f, out rhit, checkDistance, -1)) //should use any layer ?
			{
				// and can we get from this avoid path to our steering target without hitting anything ?
				RaycastHit rhit2;
				Vector3 checkDir2 = (nextPointMarker - newPoint).normalized;
				float checkDistance2 = (nextPointMarker - newPoint).magnitude;
				// capsule cast instead ?
				if (!Physics.SphereCast(newPoint-checkDir2*0.1f, m_navMeshAgent.radius*1.0f, checkDir2*1.1f, out rhit2, checkDistance2, -1)) //should use any layer ?
				{
				
					return true;
				}
			}
		}
		return false;
	}
	bool WarnCollision(NavMeshAgentWrapper navigator, RaycastHit hit){

		// someone is coming	
//Debug.Log (name+" got warning from "+navigator.name);
		if (!isNavigating &&
			!holdPosition &&
			!lockPosition &&
			GetComponent<TaskCharacter>().executingScript == null){// && priority > navigator.priority){ //if we are less important than the mover
			// don't let a character move out of the way if they are still animating
			// moved this inside so we only test if the other checks said it was okay.
			if ( GetComponent<AnimationManager>().CanWalk() == false ) //this only tests CharacterAnimState==Idle
			{
				UnityEngine.Debug.LogError ("NavMeshAgentWrapper.WarnCollision(" + this.name + ") : can't move out of the way, still animating!");
				return false;
			}

			// what if we just move forward or back one radius ?
			Vector3 dir = hit.point - transform.position;
			dir.y=0;
			moveStartTime = Time.time;
			avoidCount = 0;
			FreeMe();
			destPos = transform.position - dir.normalized*m_navMeshAgent.radius*1.5f;
			finalDestination = destPos;
			hasFinalRotation = false;
			// can we give a final rotation facing the patient ?

			hasFinalRotation = true;
			Vector3 rotationVector = GameObject.Find("lookPatient").transform.position-destPos;
			rotationVector.y = 0;
			finalRotation = Quaternion.LookRotation(rotationVector);

			GetComponent<AnimationManager>().Walk(0.25f);
			GetComponent<TaskCharacter>().IsInPosition(""); // clears atNode so we have to nav back
			m_navMeshAgent.SetDestination(destPos);
			makingWayFor = navigator;
#if DEBUG_NAVIGATION
			Debug.Log (name+" Moving to avoid "+navigator.name+" hold="+holdPosition);
#endif
			// blending forward/back with hit direction.
//			if (Vector3.Dot(dir,transform.forward) > 0)
//				m_navMeshAgent.SetDestination(transform.position - transform.forward*m_navMeshAgent.radius);
//			else
//				m_navMeshAgent.SetDestination(transform.position + transform.forward*m_navMeshAgent.radius);
			
			navRequested = true;
			isNavigating = false;
			return true;
		}
		else{
			if (makingWayFor == navigator) 
				return true; // i am already stepping out of your way.
			else{
//Debug.Log (name+" won't move to make way for "+navigator.name);
				return false; // cant help you, i am busy.
			}
		}
	}
	
/*==================================== LOCKDOWN ========================================================
	 So what's all this, then ?
	 
	 Locking is used to anchor the characters in place while they are running synchronized animations,
	 so that alignment is not disturbed.
	 
	 The navMeshAgents can push each other around, regardless of any rigidbody settings, so we need to
	 remove them from stationary characters who are animating.  We no longer need the rigidbody component
	 when using navMeshAgents, so with those two gone, nothing remains which can move the character via physics.
	 
	 To keep active agents from pathing through inactive ones, we use both an avoidance layer, and as a further
	 deterrent, place a navMeshObstacle at the fixed location of the fixed character.  The navmesh obstacle,
	 as of Unity 4.1, does not actually cause the agent to path around it, rather jsut acts as a sort of
	 fixed collider which the agents kind of slip around. That's why we still use the avoidance code.  Without
	 a rigidbody, the capsule collider won't keep anything from passing thru the locked character, so the
	 navMeshObstacle is enabled while locked to do this, without any chance of the character getting bumped.
	 (which a kinematic rigidbody with constraints ought to do, but seems to fail at)
	 
	 Avoidance consists of two mechanisms, making way and repathing.  As agents navigate, any inactive agents
	 along the path are warned, and if they are not locked due to animating, they may step out of the way.
	 If an inactive agent does not yield, then the active agent will try to repath around it.  This warning
	 uses raycasting, which requires a non-trigger collider to find the agents, so stationary agents need
	 non trigger capsule colliders just for this (they serve no physics purpose).
	 
	 The Mechanics:  LockMe() and FreeMe() are called from within the navMeshAgent when navigation begins or ends,
	 and they swap the capsuleCollider and navMeshAgent. (destroying or creating the navMeshAgent, which
	 has its own hidden collider and rigidbody).
	 
	 HoldPosition is called from within a characterTask, to temporarily shelter a character from collision and
	 keep it from responding to MakeWay warnings while animating.
	 
	 LockPosition is called directly from scripts, to create a larger timespan in which the character is kept
	 from moving.
 
	 Lessons learned:
	 Adding a NavMeshObstacle at runtime has two problems.  One, adjusting the radius seems to be ineffective,
	 and the obstacle behaves as if it has a 0.5 radius regardless of the setting.  Two, if a nav mesh agent is
	 present, or created just ater destroyImmediate of the obstacle, the agent will pop away from the obstacle to
	 avoid collision.  So this requires the obstacle to be created and destroyed at a different time than the
	 agent, here it is done when the lockPosition and holdPosition methods are called.
	 Unless Unity navmesh is fixed to cause agents to PATH AROUND NavMeshObstacles, they are not really
	 very helpful here, and only serve as a last resort to avoid agents walking thru stationary agents.
	 
	 Epilog:  I ended up pulling out most of the nav mesh obstacles for these reasons:
	 1) Putting a nav mesh obstacle on something that's going to be pushed results in a back and forth wavering
	 motion as the agent tries to avoid it.
	 2) The agent's don't actually path around the obstacles, but apply a corrective force when they detect
	 one ahead, and it's usually too late or in the wrong direction.
	 3) the agents get stuck on the obstacles quite often, especially at close range.
	 4) dynamically enabling and disabling the obstacles seems to leave artifacts that agents later try to avoid
	 in the positions where the obstacles once were.
	 5) creating and setting the size of navMeshObstacles seems to result in obstacles of the default (0.5) size,
	 no matter what.
	 
======================================================================================================*/
	
	
	public void LockPosition(bool setting){
		if (setting == lockPosition) return;
		lockPosition = setting; // global, overriding lock, on and off as required under script control
		HoldPosition(setting); // local, transient version, scope this scripted action only
	}
	
	public void HoldPosition(bool setting){
		holdPosition = setting;
		
		// creating a navMeshObstacle should keep agents from trying to path thru us,
		// but care must be taken, we can just wear one as it interferes with our movement,
		// and deleting it at the same time we restore out agent results in a collision,
		// so it has to be done by a separate command, at least a physics frame before the agent
		// is resreated.
		
		if (setting == true){
			// we are not to be run over, so deploy a navMeshObstacle around us
//			if (m_navMeshObstacle == null){
//				m_navMeshObstacle = gameObject.AddComponent("NavMeshObstacle") as NavMeshObstacle;
//				m_navMeshObstacle.radius = 0.23f;
//			}
//			m_navMeshObstacle.enabled = true;
		}
		else
		{
			if (!lockPosition){
				// we can move now, so get rid of this obstacle thing
				if (m_navMeshObstacle!=null){
					m_navMeshObstacle.enabled = false;
				}
			}
		}
	}
	
	void LockMe(){
		// prevent physics from moving me.
		if (m_navMeshAgent!=null)
			Object.DestroyImmediate(m_navMeshAgent);
//		collider.isTrigger=false; // for spherecasts to detect this, they can't be triggers
	}
	void FreeMe(){
		if (lockPosition || holdPosition){
Debug.LogWarning (name+" asked to move while locked. Add unlock to a script ?");
			LockPosition (false);	
			
		}
//		collider.isTrigger=true; // to avoid bumping things, we could probably leave it as collider.
		if (m_navMeshAgent==null)
			m_navMeshAgent=RecreateAgent();
	}
}

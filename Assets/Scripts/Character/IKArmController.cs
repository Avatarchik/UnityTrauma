using UnityEngine;
using System.Collections;

public class IKArmController : MonoBehaviour {
	
	[System.Serializable]
	public class IKConstrainedJoint{
		public Transform master;
		public GameObject go;
		public float maxBend = 1.0f;
		public float minBend = 0;
	}
	
	public Transform spine;
	public Transform shoulder;
	public Transform hand;
	
	int iSpine = -1;
	int iShoulder = 0; // these depend on our rig and what bones we build on.
	int iElbow = 3; // these get reset during init chain based on names.
//	int iWrist = 5;
	int iHand = 6;	
	Vector3 spineAxis = new Vector3( -.5f, 0, 1.0f); //( backward , left side lean, twist clockwise )
	
	public Vector3 offset = Vector3.zero; // this allows the 'palm' to be offset from the target transform.
	public Quaternion orientation = Quaternion.identity;
	public IKConstrainedJoint[] chain;
	public Transform target;
	public float blendTime = 1.0f;
	public float releaseTime = 0;
	private float startBlend = 0;
	private float blending = 0;
	private float startRelease = 0;
	
	private Transform targetInternal = null; // we keep this so we can blend back after target is null
	public float blendWeight = 1;
	public float positionAccuracy = 0.001f;

	// Use this for initialization
	void Start () {
		if (spine != null && hand != null && (chain == null || chain.Length == 0)){
			chain = GetChain(spine, hand);	
		}
		else
			InitChain(chain);
	}
	
	// Update is called once per frame
	void Update () {
		// see if we got or lost a target..
		if (target != targetInternal && blending == 0){
			if (target != null){
				// if we had non-null targetInternal, we should handle blenging to the new one..TODO
				startBlend = Time.time;
				blending = 1;
				blendWeight = 0;
				targetInternal = target;
				if (releaseTime > 0)
					startRelease = Time.time + releaseTime - blendTime;
				else 
					startRelease = 0;
			}
			else{
				// we lost our target, blend out.
				startBlend = Time.time;
				blending = -1;
			}
		}
		// is it time to release our target ? 
		if (startRelease > 0){
			if (Time.time > startRelease){
				target = null; // will trigger blend out on next update
				startRelease = 0;
			}
		}
		// perform blending
		if (blending != 0){
			if (blending > 0){
				blendWeight = (Time.time - startBlend)/blendTime;
				if (blendWeight >= 1){
					blending = 0;
					blendWeight = 1;
				}
			}
			else{
				blendWeight = 1.0f - (Time.time - startBlend)/blendTime;
				if (blendWeight <= 0){
					blending = 0;
					blendWeight = 0;
					targetInternal = null;
				}
			}
		}
	}
	
	void LateUpdate(){
		if (chain != null && targetInternal != null){
//			blendWeight = 0.5f*(1 + Mathf.Sin(Time.realtimeSinceStartup));
			MirrorChain (chain);
			Solve(chain, targetInternal);
			BlendChain(chain, blendWeight);
		}
	}
	
	// Get the chain of transforms from one transform to a descendent one
	public IKConstrainedJoint[] GetChain(Transform upper, Transform lower) {
		Transform t = lower;
		int chainLength = 1;
		while (t != upper) {
			t = t.parent;
			chainLength++;
		}
		IKConstrainedJoint[] chain = new IKConstrainedJoint[chainLength];
		t = lower;
		for (int j=0; j<chainLength; j++) {
			chain[chainLength-1-j] = new IKConstrainedJoint();
			chain[chainLength-1-j].master = t;
			t = t.parent;
		}
		InitChain(chain);
		return chain;
	}
	
	public void InitChain(IKConstrainedJoint[] chain){
		for (int i = 0; i<chain.Length; i++){
			chain[i].go = new GameObject(chain[i].master.name+"_IK_Ghost");	
			if (i==0)
				chain[i].go.transform.parent = chain[i].master.parent;
			else
				chain[i].go.transform.parent = chain[i-1].go.transform;
			chain[i].go.transform.position = chain[i].master.position;
			chain[i].go.transform.rotation = chain[i].master.rotation;
			
			if (chain[i].master.name.Contains("Spine1"))
				iSpine = i;
			if (chain[i].master.name.Contains("Upperarm1"))
				iShoulder = i;
			if (chain[i].master.name.Contains("Forearm1"))
				iElbow = i;
//			if (chain[i].master.name.Contains("Forearm3"))
//				iWrist = i;
			if (chain[i].master.name.Contains("Palm"))
				iHand = i;
		}
		MirrorChain(chain);
	}
	
	public void MirrorChain(IKConstrainedJoint[] chain){
		for (int i = 0; i<chain.Length; i++){
			chain[i].go.transform.position = chain[i].master.position;
			chain[i].go.transform.rotation = chain[i].master.rotation;
		}
	}
	
	public void BlendChain(IKConstrainedJoint[] chain, float blendWeight){
		// test just jamming the values...
//		for (int i = 0; i<chain.Length; i++){
//			chain[i].master.position = chain[i].go.transform.position;
//			chain[i].master.rotation = chain[i].go.transform.rotation;
//		}		
//		return;
		
		
		for (int i = 0; i<chain.Length; i++){
//			chain[i].master.position = Vector3.Lerp(chain[i].master.position,chain[i].go.transform.position,blendWeight);
			chain[i].master.localRotation = Quaternion.Slerp(chain[i].master.localRotation,chain[i].go.transform.localRotation,blendWeight);
		}
	}
	
	public int maxIterations = 100;
	
	public void Solve(IKConstrainedJoint[] bones, Transform target) {
		
		/* This code works by cleverly looking at the rotations in the current pose and determining
		 * an axis of rotation for each joint from that.
		 * It then assumes that more bending shortens the overall length of the chain. and adjusts a single
		 * bending coefficient to achieve the desired overall chain length to the target, then adjusts
		 * the first bone, 'hip', as a ball/socket pivot to get the correct direction of the chain to the target
		 * point.  This works well for legs and not bad for arms.
		 * 
		 * Right now, we feed it all 7 bones from the collarbone to the palm, most of which are not useful
		 * in solving the problem.  We need to tell it which bones to adjust, and how much min/max, in what order,
		 * but not ignore all the intervening transforms in checking the placement.
		 * 
		 * We can improve by re-writing the algorithm to manipulate only the elbow, add in some spine rotation for 
		 * extending reach (a couple of spine bones and rotations to apply for greater reach could give us turn and
		 * bend, applied as needed in the order elbow, turn, bend. We need to provide maximun bends for each joint.
		 * transferring twist to the lower arm.  we could think about an up vector for elbow.
		 * A cone constraint on the final alignment rotation at the shoulder.
		*/
		
		Transform endEffector = bones[bones.Length-1].go.transform;
		//public Transform targetEffector = null; //the actual target end effector that we move around
		
		Vector3 targetPos = target.position + target.TransformDirection(offset);
		
		// Get the axis of rotation for each joint
		
		// rather than this, we should supply known min/max rotations for each adjustable joint.
		Vector3[] rotateAxes = new Vector3[bones.Length-2];
		float[] rotateAngles = new float[bones.Length-2];
		Quaternion[] rotations = new Quaternion[bones.Length-2];
		float spineBend = 0;
		for (int i=0; i<bones.Length-2; i++) {
			rotateAxes[i] = Vector3.Cross(
				bones[i+1].go.transform.position-bones[i].go.transform.position,
				bones[i+2].go.transform.position-bones[i+1].go.transform.position
			);
			rotateAxes[i] = Quaternion.Inverse(bones[i].go.transform.rotation) * rotateAxes[i];
			rotateAxes[i] = rotateAxes[i].normalized;
			rotateAngles[i] = Vector3.Angle(
				bones[i+1].go.transform.position-bones[i].go.transform.position,
				bones[i+1].go.transform.position-bones[i+2].go.transform.position
			);
			
			rotations[i] = bones[i+1].go.transform.localRotation;
		}
		
		// Get the length of each bone - this is only used to calculate a reasonable accuracy tolerance
		float[] boneLengths = new float[bones.Length-1];
		float legLength = 0;
		for (int i=0; i<bones.Length-1; i++) {
			boneLengths[i] = (bones[i+1].go.transform.position-bones[i].go.transform.position).magnitude;
			legLength += boneLengths[i];
		}
		positionAccuracy = legLength*0.001f;
		

		
		float currentDistance = (endEffector.position-bones[iShoulder].go.transform.position).magnitude;
		float targetDistance = (targetPos - bones[iShoulder].go.transform.position).magnitude;
		
		// Search for right joint bendings to get target distance between hip and foot
		float bendingLow, bendingHigh;
		bool minIsFound = false;
		bool bendMore = false;
		if (targetDistance > currentDistance) {
			minIsFound = true;
			bendingHigh = 1;
			bendingLow = 0;
		}
		else {
			bendMore = true;
			bendingHigh = 1;
			bendingLow = 0;
		}
		int tries = 0;
		while ( Mathf.Abs(currentDistance-targetDistance) > positionAccuracy && tries < maxIterations ) {
			tries++;
			float bendingNew;
			if (!minIsFound) bendingNew = bendingHigh;
			else bendingNew = (bendingLow+bendingHigh)/2;
//			for (int i=0; i<bones.Length-2; i++) {
			// bend the elbow for length 
			int elbow = iElbow-1;{
				float newAngle;
				if (!bendMore) newAngle = Mathf.Lerp(180, rotateAngles[elbow], bendingNew);
				else newAngle = rotateAngles[elbow]*(1-bendingNew) + (rotateAngles[elbow]-30)*bendingNew;
				float angleDiff = (rotateAngles[elbow]-newAngle);
				Quaternion addedRotation = Quaternion.AngleAxis(angleDiff,rotateAxes[elbow]);
				Quaternion newRotation = addedRotation * rotations[elbow];
				bones[elbow+1].go.transform.localRotation = newRotation;
			}
			currentDistance = (endEffector.position-bones[iShoulder].go.transform.position).magnitude;
			if (targetDistance > currentDistance) minIsFound = true;
			if (minIsFound) {
				if (targetDistance > currentDistance) bendingHigh = bendingNew;
				else bendingLow = bendingNew;
				
				// if we are below a certain amount of bend, and still can't reach our target,
				// reach by rotating the spine and bending forward, distributed along 3 spine bones,
				// the rotation to add is created during setup, for left or right side.
				// and remember to re-calculate the target distance after doing so.
				if (!bendMore && targetDistance > currentDistance) spineBend += (targetDistance - currentDistance)*25.0f;
				if (spineBend > 20) spineBend = 20;
				{
					for (int s=iSpine+1; s<iSpine+4; s++){
//						float newAngle;
//						newAngle = spineBend; //rotateAngles[s-1]*(1-bendingNew) + (rotateAngles[s-1]-30)*bendingNew;
//						float angleDiff = (rotateAngles[s-1]-newAngle);
						Quaternion addedRotation = Quaternion.AngleAxis(spineBend,spineAxis);
						Quaternion newRotation = addedRotation * rotations[s-1];
						bones[s].go.transform.localRotation = newRotation;						
					}
					targetDistance = (targetPos - bones[iShoulder].go.transform.position).magnitude;
				}
				
				if (bendingHigh < 0.01f) break;
			}
			else {
				spineBend = 0; //?
				bendingLow = bendingHigh;
				bendingHigh++;
			}
		}
		//Debug.Log("tries: "+tries);
		//Debug.Log("Result at "+bones[1].go.transform.localRotation.eulerAngles+" spineBend="+spineBend);		
		// Rotate iShoulder bone such that foot is at desired position
		bones[iShoulder].go.transform.rotation = (
			Quaternion.AngleAxis(
				Vector3.Angle(
					(endEffector.position-bones[iShoulder].go.transform.position),
					(targetPos - bones[iShoulder].go.transform.position)
				),
				Vector3.Cross(
					(endEffector.position-bones[iShoulder].go.transform.position),
					(targetPos - bones[iShoulder].go.transform.position)
				)
			) * bones[iShoulder].go.transform.rotation
		);
		// we need to try and blend in the target orientation as well...
		// separate out the twist and apply that at the wrist bone
		
//		bones[iWrist].go.transform.rotation = target.rotation * orientation;
		
		bones[iHand].go.transform.rotation = target.rotation * orientation;
	}	
	
	public void Setup(GameObject body, string side){
		spine = FindInChildren( body, "sitelHuman_Spine1");
		if (side.ToLower ().Contains("left")){
			shoulder = FindInChildren( body, "sitelHuman_LArmUpperarm1");
			hand = FindInChildren( shoulder.gameObject, "sitelHuman_LArmPalm");	
			spineAxis = new Vector3( -.5f, 0, 1.0f);
		}
		else
		{
			shoulder = FindInChildren( body, "sitelHuman_RArmUpperarm1");
			hand = FindInChildren( shoulder.gameObject, "sitelHuman_RArmPalm");	
			spineAxis = new Vector3( -.5f, 0, -1.0f);
		}
	}
	
	private Transform FindInChildren(GameObject go, string childName){
		foreach (Transform t in go.GetComponentsInChildren<Transform>()){
			if (t.name == childName) return t;
		}
		return null;
	}	
	
	
	void OnDrawGizmos()
	{
		if (target == null || chain == null) return;

		for (int i = 0; i<chain.Length; i++){
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(chain[i].go.transform.position, 0.05f);
			if (i>0)		
				Gizmos.DrawLine(chain[i].go.transform.position, chain[i-1].go.transform.position);
			Gizmos.color = Color.green;
			Gizmos.DrawLine(chain[i].go.transform.position, chain[i].go.transform.position+chain[i].go.transform.up*0.1f);
		}
	}
}

using UnityEngine;
using System.Collections;

// this is meant to be a class of object whose children are bones with a skinned mesh, with two animation
// clips, lateral and vertical.
// the animations should be linear movements along one axis which deform the bones, and should be played as a blend and an
// additive layers, to change the length and vertical displacement of the mesh while keeping it's general shape.
// the lateral clip should have rotation keys for the start point.

// a runtime, the frames of the animations being used are intended to be adjusted to fit the desired position of
// the endpoint.  the start point should remain fixed through all animation frames.


public class AnimatedTubing : MonoBehaviour {
	
	public AnimationClip lateralClip;
	public AnimationClip verticalClip;
	public Transform startpoint;
	public Transform endpoint; // should be a child, in the 'forward' direction from this object, vertical differences ok
	public Transform startTarget;
	public Transform endTarget; // points in the scene to stretch to.
	//for debug
	public float lTime;
	public float vTime;
	Vector3 minOffset;
	Vector3 maxOffset;
	float latMin;
	float latScale;
	float vertMin;
	float vertScale; // convert to 1 sec
	AnimationState lat;
	AnimationState vert;


	// Use this for initialization
	void Start () {
		lat = animation[lateralClip.name];
		vert = animation[verticalClip.name];
		lat.layer = 1;
		vert.layer = 2;
		lat.speed = 0;
		vert.speed = 0;
		lat.blendMode = AnimationBlendMode.Blend;
		vert.blendMode = AnimationBlendMode.Additive;
		lat.enabled = true;
		vert.enabled = true;
		animation.Play(lat.name);
		animation.Play(vert.name);
		
		// we have to sample the frames to determine the min and max extent in each direction
		lat.time = 0;
		vert.time = 0;
		animation.Sample();
		minOffset = endpoint.position;
		lat.normalizedTime = 1;
		vert.normalizedTime = 1;
		animation.Sample();
		maxOffset = endpoint.position;
		vertMin = minOffset.y;
		minOffset.y = startpoint.position.y;
		float maxY = maxOffset.y;
		maxOffset.y = startpoint.position.y;
		latMin = Vector3.Distance(startpoint.position, minOffset);
//		float maxRange = Vector3.Distance(startpoint.position, maxOffset);
		latScale = 1.0f/(Vector3.Distance(minOffset, maxOffset)); // so time = scale * (range-min);
		vertScale = 1.0f/(maxY - vertMin);
	}
	
	// Update is called once per frame
	void Update () {
		
		// first, move us so startpoint is at it's target.
		// (not currently doing that)
		
		// we may need to do these in the reference frame of the startpoint, as the y axis may not align
		
		
		// find the lateral and vertical distance from startTarget to End Target 
		Vector3 targetVector = endTarget.position - startpoint.position;
		float vertOffset = endTarget.position.y; //targetVector.y;
		targetVector.y = 0;
		float range = targetVector.magnitude;
		// convert those to frame time (1 sec animation = full range)	
		lTime = lat.normalizedTime = Mathf.Clamp(latScale * (range - latMin),0,1.0f);
		vTime = vert.normalizedTime = Mathf.Clamp(vertScale * (vertOffset - vertMin),0,1.0f);
		
		animation.Sample();
		// test movement...
//		float lt = 0.5f*(Mathf.Sin(Time.time)+1);
//		vert.time = 0.5f*(Mathf.Cos(Time.time*1.5f)+1);
	}
	
	void LateUpdate(){
		
			// look at. the rotation should occur around startpoint's y axis, not the scene's ?
		
		Vector3 lookAt = endTarget.position - startpoint.position;
		Vector3 currentAt = endpoint.position - startpoint.position;
		lookAt.y = 0;
		currentAt.y = 0;
		Quaternion q = startpoint.rotation;
//		Quaternion u = Quaternion.identity;
//		u.SetFromToRotation(startpoint.right,Vector3.up);
		Quaternion r = Quaternion.identity;
		r.SetFromToRotation(currentAt, lookAt);
		startpoint.rotation = r*q;

	}
	
	void OnGUI(){
		GUILayout.Label("lat="+lTime+" vert="+vTime);	
		
	}
}

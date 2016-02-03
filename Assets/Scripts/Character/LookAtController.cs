using UnityEngine;
using System.Collections;

public class LookAtController : MonoBehaviour
{
	public Transform rootBone;
	public Transform spineBaseBone;
	public Transform neckBaseBone;
	public Transform headBone;
	public Vector3 headForward;
	public Vector3 headUp;
	public float maxHeadAngleHorz = 70f;
	public float maxHeadAngleVert = 50f;
	public float maxSpineAngleHorz = 90f;
	public float maxSpineAngleVert = 90f;
	public float responsiveness = 5f;
	public Transform target;
	private new bool active = false; // hides unity object 'active'
	private float weight = 1f;
	private float targetWeight = 1f;
	private float lerpStartTime = 0; // for weight changes
	private Transform baseBone;
	private Transform topBone;
	private Quaternion parentRotation;
	private Quaternion invParentRotation;
	private Vector3 baseForward;
	private Vector3 baseUp;
	private float hAngle;
	private float vAngle;
	private Vector3 upDir;
	private int chainLength = 0;
	
	public bool Active
	{
		get {return active;}
		set
		{
            if (value)
            {
 //               hAngle = 0f;
 //               vAngle = 0f;
                active = value;
            }
            else
                Weight = 0f;
		}
	}
	
	public float Weight
	{
		get {return weight;}
		set 
		{
			targetWeight = Mathf.Clamp01(value);
			lerpStartTime = Time.time;
		}
	}

    float stopTime;
    public float StopTime
    {
        get { return stopTime; }
        set {
            stopTime = value; 
        }
    }
	
	private float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
	{
		dirA = dirA - Vector3.Project(dirA, axis);
		dirB = dirB - Vector3.Project(dirB, axis);
		float angle = Vector3.Angle(dirA, dirB);
		if((Vector3.Dot(axis, Vector3.Cross(dirA, dirB))) < 0)
			angle *= -1;
		return angle;
	}	
	
	private void Setup(Transform newBaseBone, Transform newTopBone)
	{
		baseBone = newBaseBone;
		topBone = newTopBone;
		parentRotation = baseBone.parent.rotation;
		invParentRotation = Quaternion.Inverse(parentRotation);
		baseForward = invParentRotation * rootBone.rotation * headForward.normalized;
		baseUp = invParentRotation * rootBone.rotation * headUp.normalized;
		hAngle = 0f;
		vAngle = 0f;
		upDir = baseUp;
		for(Transform bone = topBone; bone != baseBone.parent && bone != rootBone; bone = bone.parent)
			chainLength++;
	}
	
	public void Start()
	{	
		Setup(neckBaseBone, headBone);
	}

    public void Update()
    {
        // check when to stop looking
        if (Active == true && targetWeight > 0)
        {
            if (stopTime != 0 && Time.time > stopTime)
                Active = false;
        }
    }
	
	public void LateUpdate()
	{
		if(!active || target == null)
			return;
		if(Time.deltaTime == 0)
			return;
		if (weight != targetWeight)
			weight = Mathf.Lerp(weight, targetWeight, (Time.time - lerpStartTime));//* responsiveness); 1 sec for testing
		
		parentRotation = baseBone.parent.rotation;
		invParentRotation = Quaternion.Inverse(parentRotation);
		
		Vector3 lookDirWorld = target.position - topBone.position;
		Vector3 lookDirGoal = invParentRotation * lookDirWorld.normalized;
		
		float hAngleGoal = AngleAroundAxis(baseForward, lookDirGoal, baseUp);
		Vector3 targetRight = Vector3.Cross(baseUp, lookDirGoal);
		Vector3 horzPlaneGoal = lookDirGoal - Vector3.Project(lookDirGoal, baseUp);
		float vAngleGoal = AngleAroundAxis(horzPlaneGoal, lookDirGoal, targetRight);
		
		float maxAngleHorz = maxHeadAngleHorz;
		float maxAngleVert = maxHeadAngleVert;
		
		hAngleGoal = Mathf.Clamp(hAngleGoal, -maxAngleHorz, maxAngleHorz);
		vAngleGoal = Mathf.Clamp(vAngleGoal, -maxAngleVert, maxAngleVert);
		
		hAngle = Mathf.Lerp(hAngle, hAngleGoal, Time.deltaTime * responsiveness);
		vAngle = Mathf.Lerp(vAngle, vAngleGoal, Time.deltaTime * responsiveness);
		
		Vector3 baseRight = Vector3.Cross(baseUp, baseForward);
		lookDirGoal = Quaternion.AngleAxis(hAngle, baseUp) * Quaternion.AngleAxis(vAngle, baseRight) * baseForward;
		Vector3 lookUpGoal = baseUp;
		Vector3.OrthoNormalize(ref lookDirGoal, ref lookUpGoal);
		Vector3 lookDir = lookDirGoal;
		upDir = lookUpGoal;
		Vector3.OrthoNormalize(ref lookDir, ref upDir);
		
		Quaternion lookRotation = (parentRotation * Quaternion.LookRotation(lookDir, upDir)) * Quaternion.Inverse(parentRotation * Quaternion.LookRotation(baseForward, baseUp));
		Quaternion distributedRotation = Quaternion.Slerp(Quaternion.identity, lookRotation, 1f / chainLength);
		
		for(Transform bone = topBone; bone != baseBone.parent && bone != rootBone; bone = bone.parent)
			bone.rotation = Quaternion.Lerp(bone.rotation, distributedRotation * bone.rotation, weight);
		
		if(active && targetWeight == 0 && Mathf.Approximately(weight, 0f))
		{
			weight = 0f;
			active = false;
			target = null;
		}
	}
	
	public new void SendMessage(string message)
	{
		// this method is only here to avoid unity errors when the animation event 'SendMessage' is called on this component,
		// due to the conflict with the Unity SendMessage signature
	}
}

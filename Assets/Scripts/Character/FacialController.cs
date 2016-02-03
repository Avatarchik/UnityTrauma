using UnityEngine;
using System.Collections;

public class FacialController : MonoBehaviour
{
	public string animName;
	public int animLayer;
	public float blendTime;
	
	private AnimationState anim;
	private float faceCurrent;
	private float faceTarget;
	private float faceStart;
	private float endTime;
	private bool faceEnabled;
	private float faceWeight;
	private float blendMod;
	private float weightStart;
	private float weightMod;
	private float weightEnd;
	
	public void SetWeight(float newVal)
	{
		weightStart = anim.weight;
		weightMod = blendTime * Mathf.Abs(newVal - weightStart);
		weightEnd = Time.time + weightMod;
		faceWeight = newVal;
	}
	
	public bool IsEnabled()
	{
		return faceEnabled;
	}
	
	public void SetEnabled(bool newVal)
	{
		faceEnabled = newVal;
	}
	
	public void SetTarget(float newVal)
	{
		faceStart = faceCurrent;
		blendMod = blendTime * Mathf.Abs(newVal - faceStart);
		endTime = Time.time + blendMod;
		faceTarget = newVal;
	}
	
	void Awake()
	{
		faceEnabled = true;
		faceWeight = weightStart = 1f;
		anim = animation[animName];
		anim.wrapMode = WrapMode.ClampForever;
		anim.blendMode = AnimationBlendMode.Additive;
		anim.layer = animLayer;
		anim.enabled = faceEnabled;
		anim.weight = faceWeight;
		
		blendMod =  weightMod = blendTime;
		faceCurrent = faceTarget = faceStart = 0f;
		endTime = 0f;
	}
	
	void LateUpdate()
	{
        if (anim == null)
            return;

		if(faceEnabled)
		{
			//if(anim.name == "railDown") Debug.Log(anim.name + " normalizedTime = " + anim.normalizedTime);
			if(faceCurrent != faceTarget)
				faceCurrent = Mathf.Lerp(faceStart, faceTarget, (endTime - (Time.time + blendMod)) / (0f - blendMod));
			if(anim.weight != faceWeight)
				anim.weight = Mathf.Lerp(weightStart, faceWeight, (weightEnd - (Time.time + weightMod)) / (0f - weightMod));
			anim.normalizedTime = faceCurrent;
			if(anim.name == "railDown") Debug.Log(anim.name + " normalizedTime = " + anim.normalizedTime);
		}
	}
}
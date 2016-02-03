/*
Current Version: 0.1
BorfTargets: Morph Targets for Bones
User supplies an array of animation clips to poses.
These animations must be:
	1) Present in the Animation Controller's animation array
	2) Exactly Two Frames long, with the first frame being the neutral bind pose the model was built in, and the second frame being the desired pose

Once these are configured, the user may then trigger any combination of the available poses using the available public functions.
Supplied weights will be automatically normalized to 1 if they are not when given.

poses: an array of Animation Clips representing various poses
defaultBlend: the number of seconds over which to blend from one pose to another if no time is supplied
animLayer: the layer to place poses on
rootNode: the transform of the base node for Animation Mixing
recursive: if using Animation Mixing, whether to apply animations to children of the rootNode
animBlend: the blend type, either Blend or Additive

SetWeights(float[] weightList, float blendTime)
Sets all the pose weights according to the array weightList, blended over time blendTime in seconds
	index 0 of weightList represents the weight of the neutral base pose, and indices 1 through n correspond to poses supplied in the array "poses"
	length of weightList must equal the length of poses + 1
	
SetWeights(float[] weightList)
Same as above, but uses default blend time

SetPoseWeight(int weightIndex, float newWeight, float blendTime)
Sets specific pose weightIndex to specific weight newWeight over blendTime seconds.
	a weightIndex of 0 represents the netural pose, 1 through n represent the poses supplied in the array "poses"
	the maximum value of weightIndex is the length of poses + 1
	newWeight must be between 0 and 1
	
SetPoseWeight(int weightIndex, float newWeight)
Same as above, but uses default blend time

SetPoseWeight(string poseName, float newWeight, float blendTime)
Sets specific pose with a name matching poseName to specific weight newWeight over blendTime seconds.
	use the string "neutral" to set weights for the neutral pose
	
SetPoseWeight(string poseName, float newWeight)
Same as above but uses default blend time
*/

using UnityEngine;

public class BorfTargets : MonoBehaviour
{
	public AnimationClip[] poses;
	public float defaultBlend = 1f;
	public int animLayer;
	public Transform rootNode;
	public bool recursive = true;
	public AnimationBlendMode animBlend = AnimationBlendMode.Additive;
	
	AnimationState[] poseStates;
	float[] poseWeights;
	bool validated = true;
	
	void Awake()
	{
		if(poses.Length > 0)
		{
			poseStates = new AnimationState[poses.Length];
			poseWeights = new float[poses.Length + 1];
			//begins with neutral pose at a weight of 1
			poseWeights[0] = 1f;
			for(int i = 0; i < poses.Length; i++)
			{
				//find matching AnimationState in animation component for each supplied AnimationClip in poses
				foreach(AnimationState targetState in animation)
				{
					if(targetState.clip == poses[i])
					{
						//when found, initialize AnimationState and break out of loop
						poseStates[i] = targetState;
						poseStates[i].blendMode = animBlend;
						poseStates[i].layer = animLayer;
						if(rootNode) poseStates[i].AddMixingTransform(rootNode, recursive);
						poseStates[i].enabled = true;
						poseStates[i].normalizedTime = 1f;
						poseWeights[i+1] = poseStates[i].weight = 0f;
						break;
					}
				}
				//if no matching AnimationState is found, disable component
				if(!poseStates[i])
				{
					validated = false;
					break;
				}
			}
		}
		else validated = false;
	}
	
	float TotalWeights(int skip)
	{
		float total = 0f;
		for(int i = 0; i < poseWeights.Length; i++)
			if(i != skip) total += poseWeights[i];
		return total;
	}
	
	void NormalizeWeights(float targetTotal, int skip)
	{
		float total = TotalWeights(skip);
		//if the user zeroes out the weights
		//set the currently modified weight to 1
		if(total == 0f)
		{
			//if updating all weights at once, set to neutral
			if(skip == -1) skip = 0;
			poseWeights[skip] = 1f;
			return;
		}
		if(total != targetTotal)
		{
			float multiplier = targetTotal / total;
			for(int i = 0; i < poseWeights.Length; i++)
				if(i != skip) poseWeights[i] *= multiplier;
		}
	}
	
	void UpdateAndNormalizeWeights(float newVal, int valIndex)
	{
		if(newVal > 1f || newVal < 0f) return;
		if(valIndex > poseWeights.Length || valIndex < 0) return;
		poseWeights[valIndex] = newVal;
		NormalizeWeights(1f - newVal, valIndex);
	}
	
	public void SetWeights(float[] weightList, float blendTime)
	{
		if(!validated || weightList.Length != poseWeights.Length) return;
		poseWeights = weightList;
		NormalizeWeights(1,-1);
		for(int i = 0; i < poseStates.Length; i++)
			animation.Blend(poseStates[i].name, poseWeights[i + 1], blendTime);
	}
	
	public void SetWeights(float[] weightList)
	{
		SetWeights(weightList, defaultBlend);
	}
	
	public void SetPoseWeight(int weightIndex, float newWeight, float blendTime)
	{
		if(!validated || weightIndex < 0 || weightIndex > poseWeights.Length) return;
		UpdateAndNormalizeWeights(newWeight, weightIndex);
		for(int i = 0; i < poseStates.Length; i++)
			animation.Blend(poseStates[i].name, poseWeights[i + 1], blendTime);
	}
	
	public void SetPoseWeight(int weightIndex, float newWeight)
	{
		SetPoseWeight(weightIndex, newWeight, defaultBlend);
	}
	
	public void SetPoseWeight(string poseName, float newWeight, float blendTime)
	{
		int weightIndex = -1;
		if(poseName == "neutral") weightIndex = 0;
		else
			for(int i = 0; i < poseStates.Length; i++)
				if(poseStates[i].name == poseName) weightIndex = i + 1;
		if(weightIndex == -1) return;
		SetPoseWeight(weightIndex, newWeight, blendTime);
	}
	
	public void SetPoseWeight(string poseName, float newWeight)
	{
		SetPoseWeight(poseName, newWeight, defaultBlend);
	}
}
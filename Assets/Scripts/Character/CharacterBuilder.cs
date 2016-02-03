using UnityEngine;
using System;
using System.Collections;

public class CharacterBuilder : MonoBehaviour
{
	public GameObject body;
	public GameObject head;
	public Transform bodyNeckbone;
	public Transform bodyHeadbone;
	public Transform bodyRoot;
	public Transform headNeckbone;
	public Transform faceRoot;
	public Transform headRoot;
	public int faceLayer = 2;
	public SkinnedMeshRenderer bodyRenderer;
	public Material bodyMaterial;
	
	private Transform FindMatch(Transform targetBone, Transform sourceBone)
	{
		if(targetBone.name == sourceBone.name)
			return targetBone;
		else if(targetBone.childCount > 0)
		{
			foreach(Transform b in targetBone)
			{
				Transform match = FindMatch(b, sourceBone);
				if(match)
					return match;
			}
			return null;
		}
		return null;
	}
	
	public void Awake()
	{
		head.transform.position = bodyNeckbone.position;
		
		SkinnedMeshRenderer[] headSMRs = head.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		foreach(SkinnedMeshRenderer smr in headSMRs)
		{
			Transform[] newBones = new Transform[smr.bones.Length];
			int bIdx = 0;
			foreach(Transform b in smr.bones)
			{
				Transform match = FindMatch(bodyRoot, b);
				if(match != null)
					newBones[bIdx] = match;
				else
					newBones[bIdx] = b;
				bIdx++;
			}
			smr.bones = newBones;
		}
		
		faceRoot.parent = bodyHeadbone;
		
		Destroy(headRoot.gameObject);
		
		Animation bodyAnim = body.GetComponent<Animation>();
		Animation headAnim = head.GetComponent<Animation>();
		foreach(AnimationState a in headAnim)
		{
			bodyAnim.AddClip(a.clip, a.name);
			bodyAnim[a.name].AddMixingTransform(faceRoot);
			bodyAnim[a.name].layer = faceLayer;
		}
		Destroy(headAnim);
		
		if (bodyRenderer != null && bodyMaterial != null)
			bodyRenderer.material = bodyMaterial;
	}
}
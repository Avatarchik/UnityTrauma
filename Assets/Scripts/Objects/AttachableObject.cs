using UnityEngine;
using System.Collections;



public class AttachableObject
{
	public GameObject obj;
	public Transform naturalParent;
	public string targetBoneName;
	
	public AttachableObject(GameObject newObj)
	{
		obj = newObj;
		if(obj.transform.parent != null)
			naturalParent = obj.transform.parent;
	}
	
	public AttachableObject(string newObjName, string newTargetBoneName)
	{
		obj = GameObject.Find(newObjName);
		if(obj != null && obj.transform.parent != null)
			naturalParent = obj.transform.parent;
		targetBoneName = newTargetBoneName;
	}
}

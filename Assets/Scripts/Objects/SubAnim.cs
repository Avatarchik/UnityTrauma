using UnityEngine;
using System.Collections;

public class SubAnim
{
	public GameObject obj;
	public string animName;
	
	public SubAnim(GameObject newObj, string newAnimName)
	{
		obj = newObj;
		animName = newAnimName;
	}
	
	public SubAnim(string newObjName, string newAnimName)
	{
		obj = GameObject.Find(newObjName);
		if(obj == null)
			Debug.LogError("subAnim target object " + newObjName + " not found.");
		animName = newAnimName;
	}
}
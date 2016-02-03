using UnityEngine;
using System.Collections;

public class MultiMesh : MonoBehaviour
{
	public GameObject[] meshes;
	public bool blend = false;
	public float blendTime = 1f;
	
	private int activeMesh = 0;
	private int oldMesh = 0;
	private bool canBlend = true;
	private float doneBlend = Mathf.Infinity;
	
	public void Start()
	{
		if(meshes.Length > 0)
		{
			if(blend)
			{
				foreach(GameObject go in meshes)
				{
					if(go.GetComponent<ColorChanger>() == null)
					{
						canBlend = false;
						continue;
					}
				}
				blend = canBlend;
			}
			
			meshes[0].SetActive( true);
			if(blend)
				meshes[0].GetComponent<ColorChanger>().defaultBlendTime = blendTime;
			if(meshes.Length > 1)
			{
				for(int c = 1; c < meshes.Length; c++)
				{
					meshes[c].SetActive( false);
					if(blend)
						meshes[c].GetComponent<ColorChanger>().defaultBlendTime = blendTime;
				}
			}
		}
	}
	
	public void SwitchMesh(int newMesh)
	{
		if(newMesh <= meshes.Length && newMesh != activeMesh)
		{
			if(blend)
			{
				meshes[newMesh].SetActive( true);
				Color oldColor = meshes[activeMesh].GetComponent<ColorChanger>().TargetColor;
				oldColor.a = 0f;
				meshes[activeMesh].GetComponent<ColorChanger>().TargetColor = oldColor;
				oldColor = meshes[newMesh].GetComponent<ColorChanger>().TargetColor;
				oldColor.a = 1f;
				meshes[newMesh].GetComponent<ColorChanger>().TargetColor = oldColor;
				doneBlend = Time.time + blendTime;
			}
			else
			{
				meshes[activeMesh].SetActive( false);
				meshes[newMesh].SetActive( true);
			}
			oldMesh = activeMesh;
			activeMesh = newMesh;
		}
	}
	
	public void SwitchMesh()
	{
		SwitchMesh(0);
	}
	
	public void Update()
	{
		if(blend && Time.time >= doneBlend)
		{
			meshes[oldMesh].SetActive(false);
			doneBlend = Mathf.Infinity;
		}
	}
	
	/*public void OnGUI()
	{
		if(GUILayout.Button("0"))
			SwitchMesh(0);
		if(GUILayout.Button("1"))
			SwitchMesh(1);
	}*/
}

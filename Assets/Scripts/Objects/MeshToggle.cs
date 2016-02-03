using UnityEngine;
using System.Collections;

public class MeshToggle : MonoBehaviour
{
	// This script would be improved if it did the following:
	// rather than setting the game object this script is on to active/inactive, just affect the renderer.
	// Even better, affect the renderer currently assigned to the color changer.
	// Actually toggling a mesh would be a nice feature to add.
	public string trigger = "UNINITIALIZED";
	public int renderQueue = 3050;
	public bool startState = true;
	private bool canFade = true;
	private bool state = true;
	private ColorChanger cc;
	private float doneTime = Mathf.Infinity;
	public bool toggleRenderer = false;
	
	public void HandleTrigger(string triggerString){
		if (triggerString.Contains(trigger)){
			Toggle(triggerString.Contains("ON"));
		}
	}
	
	public void Toggle(bool newState, float newTime)
	{
		state = newState;
		if(canFade)
		{
			Color newColor = cc.TargetColor;
			if(state)
			{
				if(toggleRenderer)
					cc.targetMesh.enabled = true;
				else
					gameObject.SetActive(true);
				newColor.a = 1f;
			}
			else
				newColor.a = 0f;
			doneTime = Time.time + newTime;
			cc.ChangeColor(newColor, newTime);
		}
		else
		{
			if(toggleRenderer){
				if (cc != null)
					cc.targetMesh.enabled = state;
				else 
					renderer.enabled = state;
			}
			else
				gameObject.SetActive(state);
		}
	}
	
	public void Toggle(bool newState)
	{
		if(canFade)
			Toggle(newState, cc.defaultBlendTime);
		else
			Toggle(newState, 0f);
	}
	
	public void ToggleRenderer(string sNewState) // send "true" or "false"
	{
		bool bNewState;
		toggleRenderer = true;
		if (!bool.TryParse(sNewState, out bNewState))
			bNewState = !state;
		
		Toggle(bNewState);
			
//		state = bNewState;
//		if(canFade)
//		{
//			Color newColor = cc.TargetColor;
//			if(state)
//			{
//				cc.targetMesh.enabled = true;
//				newColor.a = 1f;  
//			}
//			else
//				newColor.a = 0f;
//			doneTime = Time.time + cc.defaultBlendTime;
//			cc.ChangeColor(newColor, cc.defaultBlendTime);
//		}
//		else
//			cc.targetMesh.enabled = state;
	}
	
	public void Awake()
	{
		cc = gameObject.GetComponent<ColorChanger>();
		if(cc == null)
			canFade = false;
		if(!startState)
			Toggle(false, 0f);

	}
	
	public void Start(){
		if (renderer != null)
			renderer.material.renderQueue = renderQueue;
	}
	
	public void Update()
	{
		if(Time.time >= doneTime)
		{
			doneTime = Mathf.Infinity;
			if(!state){
				if (toggleRenderer)
					cc.targetMesh.enabled = false;
				else
					gameObject.SetActive (false);
			}
		}
	}	
}

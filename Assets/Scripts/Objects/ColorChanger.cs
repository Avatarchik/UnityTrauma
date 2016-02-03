using UnityEngine;
using System.Collections;

public class ColorChanger : MonoBehaviour
{
	public Renderer targetMesh;
	public string targetProperty = "_Color";
	public float defaultBlendTime = 0.5f;
	
	private float blendTime;
	private Color targetColor;
	private Color startColor;
	private float endTime = 0f;
	private bool done = true;
	
	public Color TargetColor
	{
		get {return targetColor;}
		set 
		{
			ChangeColor(value);
		}
	}
	
	public void ChangeColor(Color newColor, float newTime)
	{
		startColor = targetMesh.material.GetColor(targetProperty);
		targetColor = newColor;
		blendTime = newTime;
		endTime = Time.time + blendTime;
		if (blendTime == 0){
			targetMesh.material.SetColor(targetProperty, targetColor);
			done=true;
		}
		else
		{
			done = false;
		}
	}
	
	public void ChangeColor(Color newColor)
	{
		ChangeColor(newColor, defaultBlendTime);
	}
	
	private bool MatchColor(Color c1, Color c2)
	{
		return Mathf.Approximately(c1.r, c2.r) && Mathf.Approximately(c1.g, c2.g) && Mathf.Approximately(c1.b, c2.b) && Mathf.Approximately(c1.a, c2.a);
	}
	
	public void Awake()
	{
	if (targetMesh == null){
			Debug.Log ("assigning Color Changer mesh on "+name);
			targetMesh = gameObject.renderer;
		}
		else // if there was no mesh assigned, we shouldn't take the color
		{
			targetColor = startColor = targetMesh.material.GetColor(targetProperty);
		}
	}
	
	public void Update()
	{
		if(!done)
		{
			float t = (endTime - (Time.time + blendTime)) / (0f - blendTime); //eschew obfuscation ?
			targetMesh.material.SetColor(targetProperty, Color.Lerp(startColor, targetColor, t));
			if(Time.time >= endTime)
			{
				targetMesh.material.SetColor(targetProperty, targetColor);
				done = true;
			}
		}
	}
}
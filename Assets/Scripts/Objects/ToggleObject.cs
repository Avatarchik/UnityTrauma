using UnityEngine;
using System.Collections;

public class ToggleObject : MonoBehaviour
{
	public float blendTime = 1f;			//time to blend in seconds
	
	bool currentState = true;
	float doneBlend = -1.0f;
	Color currentColor = Color.white;
	Color targetColor = Color.white;
	Renderer[] objRenderers;
	
	public void Toggle(bool visState)
	{
        if (visState)
            foreach (Renderer r in objRenderers)
                r.renderer.enabled = true;
		//if(visState != currentState)
		{
			currentState = visState;
			if(visState)
			{
                targetColor = new Color(0.72f, 0.72f, 0.72f, 1f);//RenderSettings.ambientLight;
			}
			else targetColor = new Color(0.72f, 0.72f, 0.72f, 0f);

            if(currentColor != targetColor)
			    doneBlend = Time.time + blendTime;
		}
	}
	
	public void ToggleNow(bool visState)
	{
        foreach (Renderer r in objRenderers)
            r.renderer.enabled = visState;

        Color newColor = new Color(0.72f, 0.72f, 0.72f, 1f);//RenderSettings.ambientLight;
        currentState = visState;
        targetColor = visState ? newColor : new Color(0.72f, 0.72f, 0.72f, 1f);
        currentColor = targetColor;
	}

    public bool GetState()
    {
        return currentState;
    }
	
	void Awake()
	{
		objRenderers = GetComponentsInChildren<Renderer>();
	}
	
	void Update()
	{
		if(Time.time <= doneBlend && objRenderers.Length > 0)
			foreach(Renderer r in objRenderers)
				r.material.color = Color.Lerp(currentColor, targetColor, (doneBlend - (Time.time + blendTime)) / -blendTime);
        else if (currentColor != targetColor)
        {
            currentColor = targetColor;
            foreach (Renderer r in objRenderers)
                r.renderer.enabled = currentState;
        }
	}
}
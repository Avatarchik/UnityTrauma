using UnityEngine;
using System.Collections;

public class NameTag : MonoBehaviour 
{
    float blendTime;
    float timer;
    bool fadeIn;
    public Camera targetCamera;

	// Use this for initialization
	void Start ()
	{
	}
	
	// Update is called once per frame
	void Update () 
	{
        if (targetCamera == null)
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        else
            transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward, targetCamera.transform.rotation * Vector3.up);

        if (blendTime > 0)
        {
            timer += Time.deltaTime;
            float ratio = timer / blendTime;
            if (ratio > 1f)
                ratio = 1f;

            Color temp = renderer.material.color;
            if (fadeIn && temp.a != 1f)
                temp.a = ratio;
            else if (!fadeIn && temp.a != 0)
                temp.a = 1 - ratio;
            else
            {
                blendTime = 0;
                timer = 0;
            }

            renderer.material.color = temp;
        }
	}

    public void SetVisible(bool visible)
    {
        if (renderer != null)
            renderer.enabled = visible;
    }

    public void SetVisible(bool visible, float blend)
    {
        if (blend == 0)
            SetVisible(visible);
        else
        {
            timer = 0;
            blendTime = blend;
            fadeIn = visible;
        }
    }
}
using UnityEngine;
using System.Collections;

public class IVBagTextureOvertakeScript : MonoBehaviour 
{
    public float overtake = 0;
    float oldOvertake = 0;
	// Use this for initialization
	void Start () 
    {
        //Material mat = gameObject.renderer.material;
        //mat.SetFloat("_OvertakePercent", 0.5f);
	}
	
	public void SetPercentage( float percentage )
	{
		overtake = percentage;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (overtake != oldOvertake)
        {
            if (overtake < 0)
                overtake = 0;
            else if (overtake > 1f)
                overtake = 1f;
            oldOvertake = overtake;
            // 0.79
            gameObject.renderer.material.SetFloat("_OvertakePercent", overtake * 0.78f);
        }
	}
}
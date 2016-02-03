using UnityEngine;
using System.Collections;

public class TwitchController : MonoBehaviour 
{	
	public string animName;
	public int animLayer;
	public float space;
	public float rate;
	public float rateDev;
	public float timeDev;
	public int max;
	
	private AnimationState anim;
	private bool twitchEnabled;
	private float next;
	private int num;
	private float speedMod;
	
	public void CalcNext()
	{
        if (anim != null)
        {
            next = Time.time + rate + Random.Range(0f - rateDev, rateDev);
            if (max > 1) num = Mathf.RoundToInt(Random.Range(1f, max));
            else num = 1;
            if (timeDev != 1f) anim.speed = Random.Range(1f - timeDev, 1f + timeDev);
            anim.speed *= speedMod;
        }
	}
	
	public void SetEnabled(bool newVal)
	{
		twitchEnabled = newVal;
	}
	
	public bool IsEnabled()
	{
		return twitchEnabled;
	}
	
	public void SetSpeedMod(float newVal)
	{
		speedMod = newVal;
		anim.speed *= speedMod;
	}
	
	public IEnumerator Play()
	{
        if (anim != null)
        {
            CalcNext();
            anim.weight = 1f;
            for (int c = 0; c < num; c++)
            {
                anim.normalizedTime = 0f;
                yield return new WaitForSeconds(space + anim.length);
            }
            anim.weight = 0f;
        }
	}
	
	void Awake()
	{
		anim = animation[animName];
		anim.wrapMode = WrapMode.ClampForever;
		anim.blendMode = AnimationBlendMode.Additive;
		anim.layer = animLayer;
		anim.enabled = twitchEnabled = true;
		anim.weight = 0f;
		speedMod = 1f;
	}
	
	void Start()
	{
		CalcNext();
	}
	
	void Update()
	{
		if(twitchEnabled && Time.time >= next)  StartCoroutine(Play());
	}
}
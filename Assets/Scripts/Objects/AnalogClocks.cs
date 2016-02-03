using System;
using UnityEngine;

public class AnalogClocks : MonoBehaviour {
	public GameObject[] clocks;
	
	private Transform[] hourHandTransforms;
	private Transform[] minuteHandTransforms;
	private Transform[] secondHandTransforms;
	
	private Vector3 hv;
	private Vector3 mv;
	private Vector3 sv;

	private float next;
	public DateTime startTime;
	
	void Start ()
	{
		if (clocks == null || clocks.Length == 0){
			clocks = new GameObject[1];
			clocks[0] = gameObject;
		}
		
		hourHandTransforms = new Transform[clocks.Length];
		minuteHandTransforms = new Transform[clocks.Length];
		secondHandTransforms = new Transform[clocks.Length];
		
		for (int i = 0; i< clocks.Length; i++){
			foreach (Transform child in clocks[i].transform){
				if (child.name.Contains("hour")) hourHandTransforms[i] = child;
				if (child.name.Contains("min")) minuteHandTransforms[i] = child;
				if (child.name.Contains("sec")) secondHandTransforms[i] = child;
			}
		}
		 
		startTime = DateTime.Now;
		
		hv = new Vector3(270,-90,90); // these are funky due to 3DSMax/Unity coordinate swap.
		mv = new Vector3(270,-90,90);
		sv = new Vector3(270,-90,90);
		next = 0.0f;
	}
	
	void Update ()
	{
		if (next > 0.0f) { next -= Time.deltaTime; return; }
		next = 0.5f;
		
		// allow a reference start time to be set other than real time
		
		DateTime d  = startTime.AddSeconds(Time.time);//DateTime.Now;
		
		sv.x = 270 + d.Second * 6f;
		mv.x = 270 + d.Minute * 6f; 
		hv.x = 270 + (d.Hour * 30f) + ((d.Minute / 60.0f) * 30f);
		
		foreach ( Transform	t in hourHandTransforms) t.localEulerAngles = hv;
		foreach ( Transform	t in minuteHandTransforms) t.localEulerAngles = mv;
		foreach ( Transform	t in secondHandTransforms) t.localEulerAngles = sv;
	}

}
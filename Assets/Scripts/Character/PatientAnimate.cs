//ChangeBreatheMode(PatientBreathe)
//changes the patient's breathing animation

//ChangePosture(PatientPosture)
//changes the patient's posture

//SetWeakLevel(float)
//SetPainLevel(float)
//SetFearLevel(float)
//sets the patient's condition to a value from 0 - 1

//StartSeizure() to start a seizure
//EndSeizure() to end it
//if you change position during a seizure it will end the seizure animation
//if you change Fear/Pain/Fear Level during a seizure her facial expression will change

//RailUp() to raise bed rails
//RailDown() to lower them

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PatientAnimate : MonoBehaviour
{
	public float idleRate;
	public float idleRateDev;
	protected float idleNext = 0f;
	protected bool idleEnabled = true;
	protected Queue<string> animQueue;

    void ToggleIdle(bool newVal) {
		if(idleEnabled != newVal)
		{
			idleEnabled = newVal;
			if(idleEnabled) CalcIdleNext();
		}
    }
	
	protected void IdleEnable()
	{
		idleEnabled = true;
		CalcIdleNext();
	}

    protected void IdleDisable() {
		idleEnabled = false;
    }

    protected void CalcIdleNext() {
		idleNext = Time.time + Random.Range(idleRate - idleRateDev, idleRate + idleRateDev);
    }

    protected void SkipLastAnim() {
        PrintAnimQueue("before skipLastAnim");
        Queue<string> tempQueue = new Queue<string>();
        while (animQueue.Count > 1)
        {
            tempQueue.Enqueue(animQueue.Dequeue());
        }
		animQueue = tempQueue;
        PrintAnimQueue("after skipLastAnim");
    }

    public void PrintAnimQueue( string header ) {
        string accum = "";
        foreach (string str in animQueue)
        {
            accum += "[" + str + "]";
        }
        Debug.Log("PrintAnimQueue:" + header + " : " + accum);
    }

    protected void PlayNext(float blendTime) {
        if (animQueue.Count > 0)
        {
            Debug.Log("PlayNext: [" + animQueue.Peek() + "] : blendTime=" + blendTime);
            animation.CrossFade(animQueue.Dequeue(), blendTime);
        }
    }

    virtual public void Awake() {
        Init();
    }

    virtual public void Init() {
        animQueue = new Queue<string>();
        Start();
    }

    virtual public void Start() {
		CalcIdleNext();

        // stop animations playing
        animation.Stop();
    }

    virtual public void Update() {

    }

    public void BeginAnim(string origin)
    {
        AnimationSequenceController.GetInstance().NextAnim(origin);
    }

    public void EndAnim(string origin)
    {
        AnimationSequenceController.GetInstance().NextAnim(origin);
    }
}
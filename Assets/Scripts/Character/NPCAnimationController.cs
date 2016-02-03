using UnityEngine;
using System.Collections;

//Use SetAnimState(newState) to change the animation from one state to another
//When in the Stand state, will play an Idle animation every (idleRate - idleDev) to (idleRate + idleDev) seconds.
//Speed of the walk animation can be adjusted by calling SetWalkSpeed(newSpeed) with newSpeed being the desired speed in meters per second.

public class NPCAnimationController : MonoBehaviour
{	
	public float idleRate;				//number of seconds between idle animations
	public float idleDev;				//maximum deviation for time between idle animations
	protected float nextIdle;
	
	virtual public void Start()
	{
		CalcNextIdle();
	}
	
	virtual public void Update()
	{
        
    }
	
	protected void CalcNextIdle()
	{
		nextIdle = Time.time + Random.Range(idleRate - idleDev, idleRate + idleDev);
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
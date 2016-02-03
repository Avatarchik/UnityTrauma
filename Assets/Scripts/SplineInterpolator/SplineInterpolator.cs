using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum eEndPointsMode { AUTO, AUTOCLOSED, EXPLICIT }
public enum eWrapMode { ONCE, LOOP }
public delegate void OnEndCallback();

public enum eAnimateState { STOP, MOVING, WAITING, AVOIDING }

public class SplineInterpolator : MonoBehaviour
{
	eEndPointsMode mEndPointsMode = eEndPointsMode.AUTO;

	protected class SplineNode
	{
		internal Vector3 Point;
		internal Quaternion Rot;
		internal float Time;
		internal Vector2 EaseIO;

		internal SplineNode(Vector3 p, Quaternion q, float t, Vector2 io) { Point = p; Rot = q; Time = t; EaseIO = io; }
		internal SplineNode(SplineNode o) { Point = o.Point; Rot = o.Rot; Time = o.Time; EaseIO = o.EaseIO; }
	}

	protected List<SplineNode> mNodes = new List<SplineNode>();

	protected string mState = "";
    public bool isMoving = false;
	protected bool mRotations;
    public float tension = 0.5f;

	protected OnEndCallback mOnEndCallback;
	
	public bool isVerbose = false;
    protected eAnimateState animateState = eAnimateState.STOP;
    public bool callFadeDown = false;
    protected bool startedFade = false;
	protected float rotationLerp = 0;
	
	public eAnimateState State
	{
		get { return animateState; }
	}
	
	public OnEndCallback OnEndCallback
	{
		set { mOnEndCallback = value; }
	}

	public float CurrentTime
	{
		get { return mCurrentTime; }
	}
	
	public void Once()
	{
		mState = "Once";
		rotationLerp = 0;
	}
	
	public void Stop()
	{
		mState = "Stopped";
	}

	void Awake()
	{
		Reset();
	}

    public virtual void AtStart()
    {
		animateState = eAnimateState.MOVING;
		if(isVerbose)
		{
			Brain brain = Brain.GetInstance();
			AnimateMsg msg = new AnimateMsg(gameObject.name, animateState);
			brain.PutMessage(msg);
		}
    }

    public virtual void AtEnd()
    {
		animateState = eAnimateState.STOP;
		if(isVerbose)
		{
			Brain brain = Brain.GetInstance();
			AnimateMsg msg = new AnimateMsg(gameObject.name, animateState);
			brain.PutMessage(msg);
		}		
    }

    public virtual void AtLoopEnd()
    {
		if(isVerbose)
		{
			Brain brain = Brain.GetInstance();
			AnimateMsg msg = new AnimateMsg(gameObject.name, eAnimateState.STOP);
			brain.PutMessage(msg);
			
			msg = new AnimateMsg(gameObject.name, animateState);
			brain.PutMessage(msg);			
		}		
    }

    public virtual void AtWait()
    {
        animateState = eAnimateState.WAITING;
        if(isVerbose)
        {
			Brain brain = Brain.GetInstance();
			AnimateMsg msg = new AnimateMsg(gameObject.name, animateState);
			brain.PutMessage(msg);
        }
    }

    public void StartInterpolation(OnEndCallback endCallback, bool bRotations, eWrapMode mode)
	{
		if (mState != "Reset")
			throw new System.Exception("First reset, add points and then call here");

		mState = mode == eWrapMode.ONCE ? "Once" : "Loop";
        isMoving = true;
		mRotations = bRotations;
		mOnEndCallback = endCallback;

		SetInput();

        AtStart();
	}

	public void Reset()
	{
		mNodes.Clear();
		mState = "Reset";
		mCurrentIdx = 1;
		mCurrentTime = 0;
		mRotations = false;
		mEndPointsMode = eEndPointsMode.AUTO;
	}

    public void Restart()
    {
        mCurrentIdx = 1;
        mCurrentTime = 0;
    }

	public void AddPoint(Vector3 pos, Quaternion quat, float timeInSeconds, Vector2 easeInOut)
	{
		if (mState != "Reset")
			throw new System.Exception("Cannot add points after start");

		mNodes.Add(new SplineNode(pos, quat, timeInSeconds, easeInOut));
	}


	void SetInput()
	{
		if (mNodes.Count < 2)
			throw new System.Exception("Invalid number of points");

		if (mRotations)
		{
			for (int c = 1; c < mNodes.Count; c++)
			{
				SplineNode node = mNodes[c];
				SplineNode prevNode = mNodes[c - 1];

				// Always interpolate using the shortest path -> Selective negation
				if (Quaternion.Dot(node.Rot, prevNode.Rot) < 0)
				{
					node.Rot.x = -node.Rot.x;
					node.Rot.y = -node.Rot.y;
					node.Rot.z = -node.Rot.z;
					node.Rot.w = -node.Rot.w;
				}
			}
		}

		if (mEndPointsMode == eEndPointsMode.AUTO)
		{
			mNodes.Insert(0, mNodes[0]);
			mNodes.Add(mNodes[mNodes.Count - 1]);
		}
		else if (mEndPointsMode == eEndPointsMode.EXPLICIT && (mNodes.Count < 4))
			throw new System.Exception("Invalid number of points");
	}

	void SetExplicitMode()
	{
		if (mState != "Reset")
			throw new System.Exception("Cannot change mode after start");

		mEndPointsMode = eEndPointsMode.EXPLICIT;
	}

	public void SetAutoCloseMode(float joiningPointTime)
	{
		if (mState != "Reset")
			throw new System.Exception("Cannot change mode after start");

		mEndPointsMode = eEndPointsMode.AUTOCLOSED;

		mNodes.Add(new SplineNode(mNodes[0] as SplineNode));
		mNodes[mNodes.Count - 1].Time = joiningPointTime;

		Vector3 vInitDir = (mNodes[1].Point - mNodes[0].Point).normalized;
		Vector3 vEndDir = (mNodes[mNodes.Count - 2].Point - mNodes[mNodes.Count - 1].Point).normalized;
		float firstLength = (mNodes[1].Point - mNodes[0].Point).magnitude;
		float lastLength = (mNodes[mNodes.Count - 2].Point - mNodes[mNodes.Count - 1].Point).magnitude;

		SplineNode firstNode = new SplineNode(mNodes[0] as SplineNode);
		firstNode.Point = mNodes[0].Point + vEndDir * firstLength;

		SplineNode lastNode = new SplineNode(mNodes[mNodes.Count - 1] as SplineNode);
		lastNode.Point = mNodes[0].Point + vInitDir * lastLength;

		mNodes.Insert(0, firstNode);
		mNodes.Add(lastNode);
	}

	public float mCurrentTime;
	protected int mCurrentIdx = 1;
	
	protected virtual void Update()
	{
		if (mState == "Reset" || mState == "Stopped" || mNodes.Count < 4)
			return;

		mCurrentTime += Time.deltaTime;

		// We advance to next point in the path
		if (mCurrentTime >= mNodes[mCurrentIdx + 1].Time)
		{
            if (callFadeDown && !startedFade && mCurrentIdx > 3)
            {
                startedFade = true;
                GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Fader>().fadeSpeed = .15f;
                GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Fader>().Out();
            }
			if (mCurrentIdx < mNodes.Count - 3)
			{
				mCurrentIdx++;
			}
            else
            {
                if (mState != "Loop")
                {
                    AtEnd();

                    mState = "Stopped";
                    isMoving = false;

                    // We stop right in the end point
                    transform.position = mNodes[mNodes.Count - 2].Point;

                    if (mRotations)
                        transform.rotation = mNodes[mNodes.Count - 2].Rot;

                    // We call back to inform that we are ended
                    if (mOnEndCallback != null)
                        mOnEndCallback();
                }
                else
                {
                    AtLoopEnd();

                    mCurrentIdx = 1;
                    mCurrentTime = 0;
                }
            }
		}

		if (mState != "Stopped")
		{
			// Calculates the t param between 0 and 1
			float param = (mCurrentTime - mNodes[mCurrentIdx].Time) / (mNodes[mCurrentIdx + 1].Time - mNodes[mCurrentIdx].Time);

			// Smooth the param
			param = MathUtils.Ease(param, mNodes[mCurrentIdx].EaseIO.x, mNodes[mCurrentIdx].EaseIO.y);

			transform.position = GetHermiteInternal(mCurrentIdx, param);

			if (mRotations)
			{	
				Quaternion desiredRotation = GetSquad(mCurrentIdx, param);
				rotationLerp += Time.deltaTime*0.3f; // 3 sec to align
				transform.rotation = Quaternion.Slerp(transform.rotation,desiredRotation,rotationLerp);
			}
		}
	}

	protected Quaternion GetSquad(int idxFirstPoint, float t)
	{
		Quaternion Q0 = mNodes[idxFirstPoint - 1].Rot;
		Quaternion Q1 = mNodes[idxFirstPoint].Rot;
		Quaternion Q2 = mNodes[idxFirstPoint + 1].Rot;
		Quaternion Q3 = mNodes[idxFirstPoint + 2].Rot;

		Quaternion T1 = MathUtils.GetSquadIntermediate(Q0, Q1, Q2);
		Quaternion T2 = MathUtils.GetSquadIntermediate(Q1, Q2, Q3);

		return MathUtils.GetQuatSquad(t, Q1, Q2, T1, T2);
	}



	public Vector3 GetHermiteInternal(int idxFirstPoint, float t)
	{
		float t2 = t * t;
		float t3 = t2 * t;

		Vector3 P0 = mNodes[idxFirstPoint - 1].Point;
		Vector3 P1 = mNodes[idxFirstPoint].Point;
		Vector3 P2 = mNodes[idxFirstPoint + 1].Point;
		Vector3 P3 = mNodes[idxFirstPoint + 2].Point;

		//float tension = 0.5f;	// 0.5 equivale a catmull-rom

		Vector3 T1 = tension * (P2 - P0);
		Vector3 T2 = tension * (P3 - P1);

		float Blend1 = 2 * t3 - 3 * t2 + 1;
		float Blend2 = -2 * t3 + 3 * t2;
		float Blend3 = t3 - 2 * t2 + t;
		float Blend4 = t3 - t2;

		return Blend1 * P1 + Blend2 * P2 + Blend3 * T1 + Blend4 * T2;
	}


	public Vector3 GetHermiteAtTime(float timeParam)
	{
        if(mNodes == null)
            mNodes = new List<SplineNode>();
		
		if (timeParam >= mNodes[mNodes.Count - 2].Time)
			return mNodes[mNodes.Count - 2].Point;

		int c;
		for (c = 1; c < mNodes.Count - 2; c++)
		{
			if (mNodes[c].Time > timeParam)
				break;
		}

		int idx = c - 1;
		float param = (timeParam - mNodes[idx].Time) / (mNodes[idx + 1].Time - mNodes[idx].Time);
		param = MathUtils.Ease(param, mNodes[idx].EaseIO.x, mNodes[idx].EaseIO.y);

		return GetHermiteInternal(idx, param);
	}
}
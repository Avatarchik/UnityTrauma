using UnityEngine;
using System.Collections;

public class CameraRailObserver : SplineInterpolator
{
    public int timeMod = 0;	
	float lastStartTime=0.0f;	

	float _start = 0.0f;
	public float StartTime
	{
		get { return _start; }
		set { _start = value; }
	}
	// just for display of current time
	public float CurrentTime;	
	
	// calc starting index with a fractional percent of total spline
	// start is fraction of total spline (0-1)
	void CalcTimeIndex()
	{
		// keep start time between 0 and 1
		float frac = StartTime = Mathf.Clamp(StartTime,0.0f,1.0f);
		
		if ( frac == lastStartTime )
			return;
		
		// find range for time
		float startTime = mNodes[0].Time;
		float endTime = mNodes[mNodes.Count-1].Time;	
			
		// get start based on StartTime fraction
		float start = (endTime-startTime)*frac + startTime;
		
		int index=0;
		foreach( SplineNode sn in mNodes )
		{
			// increment until greater than time in node
			if ( start >= sn.Time )
			{
				index++;
			}
		}
		
		mCurrentIdx = index-1;
		mCurrentTime = start;
		
		lastStartTime = frac;
	}

    // Use this for initialization
    void Start()
    {
		CalcTimeIndex();
    }

	public void Restart()
	{
		mCurrentIdx = 1;
		mCurrentTime = 0;

		 lastStartTime=0.0f;	
		
		 _start = 0.0f;
	}

    // Update is called once per frame
    protected override void Update()
    {
        if (mState == "Reset" || mState == "Stopped" || mNodes.Count < 4)
            return;

		CalcTimeIndex();
		
        mCurrentTime += Time.deltaTime * timeMod;
		CurrentTime = mCurrentTime;

        // We advance to next point in the path
        if (mCurrentTime >= mNodes[mCurrentIdx + 1].Time)
        {
            if (mCurrentIdx < mNodes.Count - 3)
            {
                mCurrentIdx++;
            }
            else
            {
                if (mState != "Loop")
                {
                    AtEnd();

                    //mState = "Stopped";
                    //isMoving = false;

                    // We stop right in the end point
                    transform.position = mNodes[mNodes.Count - 2].Point;

                    if (mRotations)
                        transform.rotation = mNodes[mNodes.Count - 2].Rot;

                    // We call back to inform that we are ended
                    if (mOnEndCallback != null)
                        mOnEndCallback();

                    mCurrentIdx = mNodes.Count - 3;
                    mCurrentTime = mNodes[mCurrentIdx + 1].Time;
                }
                else
                {
                    AtLoopEnd();

                    mCurrentIdx = 1;
                    mCurrentTime = 0;
                }
            }
        }
        else if (mCurrentTime < mNodes[mCurrentIdx].Time)
        {
            if (mCurrentIdx > 1)
            {
                mCurrentIdx--;
            }
            else
            {
                if (mState != "Loop")
                {
                    AtEnd();

                    //mState = "Stopped";
                    //isMoving = false;

                    // We stop right in the end point
                    transform.position = mNodes[0].Point;

                    if (mRotations)
                        transform.rotation = mNodes[0].Rot;

                    // We call back to inform that we are ended
                    if (mOnEndCallback != null)
                        mOnEndCallback();

                    mCurrentIdx = 1;
                    mCurrentTime = 0;
                }
                else
                {
                    AtLoopEnd();
                    mCurrentIdx = mNodes.Count - 3;
                    mCurrentTime = mNodes[mCurrentIdx + 1].Time;
                }
            }
        }

        if (mState != "Stopped")
        {
            // Calculates the t param between 0 and 1
            float param = (mCurrentTime - mNodes[mCurrentIdx].Time) / (mNodes[mCurrentIdx + 1].Time - mNodes[mCurrentIdx].Time);
            if (param == 1)
                param = 0.999f;
            // Smooth the param
            param = MathUtils.Ease(param, mNodes[mCurrentIdx].EaseIO.x, mNodes[mCurrentIdx].EaseIO.y);

            transform.position = GetHermiteInternal(mCurrentIdx, param);

            if (mRotations)
            {
                transform.rotation = GetSquad(mCurrentIdx, param);
            }
        }
    }
}
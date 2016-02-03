using UnityEngine;
using System.Collections;

public class CameraRailCoordinator : MonoBehaviour
{
    public CameraRailObserver outerRail;
    public CameraRailObserver innerRail;
    SplineController outerController;
    SplineController innerController;

    public GameObject lookAtObj;
    Vector3 pointA, pointB;

    public float duration = 10f;
    bool outer = true;
    public bool lookAtFollow = true;

    public bool keyboardInput = false;
    int guiInput = 0;
	public bool bAllowMovement = true;

    //public GameObject lookAtTarget;
    //void OnDrawGizmos()
    //{
    //    if (lookAtFollow)
    //    {
    //        CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
    //        if (cl != null)
    //        {
    //            Vector3 result = LookingAtSearch(pointA, pointB, cl.transform.position);

    //            Gizmos.color = Color.red;
    //            Gizmos.DrawLine(Camera.main.transform.position, result);
    //        }
    //    }
    //}

	public float StartPercentage=0.0f;
	float lastStartTime;

	// Use this for initialization
    void Awake()
    {
		lastStartTime = StartPercentage;
		
        if (outerRail != null)
        {
            outerController = outerRail.gameObject.GetComponent<SplineController>();
            if (outerController == null)
            {
                enabled = false;
                return;
            }
        }
        if (innerRail != null)
        {
            innerController = innerRail.gameObject.GetComponent<SplineController>();
            if (innerController == null)
            {
                enabled = false;
                return;
            }
        }

        innerController.Duration = duration;
        outerController.Duration = duration;

        innerController.OrientationMode = eOrientationMode.NODE;
        outerController.OrientationMode = eOrientationMode.NODE;
		
        //innerController.WrapMode = eWrapMode.LOOP;
        //outerController.WrapMode = eWrapMode.LOOP;
        if (lookAtObj != null)
        {
            Transform[] temp = lookAtObj.GetComponentsInChildren<Transform>();
            if (temp.Length >= 3)
            {
                pointA = temp[1].position;
                pointB = temp[2].position;
            }
        }
        else
            lookAtFollow = false;
    }

    void Start()
    {
        CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        if (cl != null)
            cl.MoveTo(outerRail.transform, 0, true, false);
    }

    // Update is called once per frame
    void Update()
    {
		// set last start time here
		StartPercentage = Mathf.Clamp(StartPercentage,0.0f,100.0f);		
		innerRail.StartTime = StartPercentage/100.0f;
		outerRail.StartTime = StartPercentage/100.0f;	
		if ( StartPercentage != lastStartTime )
		{
			lastStartTime = StartPercentage;
			LookAt();
		}

		if (bAllowMovement) {
				float value = ((keyboardInput && GUIUtility.keyboardControl == 0) ? Input.GetAxis ("Horizontal") : 0) + guiInput;
				if (value < 0)
						Forward ();
				else if (value > 0)
						Reverse ();
				else
						Stop ();
//		if (value != 0)
//						Debug.LogError ("Cam move value = " + value + " at " + Time.time);

				/* the input mapping for JUMP was switching rails */
				if ((Input.GetAxis ("Vertical") > 0 && outer) || (Input.GetAxis ("Vertical") < 0 && !outer))
// if (Input.GetButtonDown("Jump"))
						Switch ();

				//Forward();
		}
    }

    public void GUIInput(int input)
    {
        guiInput = input;
    }

    public void Forward()
    {
        outerRail.timeMod = 1;
        innerRail.timeMod = 1;

        LookingAt();
    }

    public void Reverse()
    {
        outerRail.timeMod = -1;
        innerRail.timeMod = -1;

        LookingAt();
    }

    public void LookingAt()
    {
        if (lookAtFollow)
        {
            CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
            if (cl != null)
            {
                // Try a binary search to find the nearly point on the line segment
                cl.RotateTowards(LookingAtSearch(pointA, pointB, cl.transform.position));
            }
        }
    }
	
    public void LookAt()
    {
        if (lookAtFollow)
        {
            CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
            if (cl != null)
            {
                // Try a binary search to find the nearly point on the line segment
                cl.LookAt(LookingAtSearch(pointA, pointB, cl.transform.position));
            }
        }
    }

	public void MovingLookAt(Vector3 endpoint)
	{
		if (lookAtFollow)
		{
			CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
			if (cl != null)
			{
				// Try a binary search to find the nearly point on the line segment
				Vector3 finalLookAt = LookingAtSearch(pointA, pointB, cl.transform.position);
				Vector3 finalLookVector = finalLookAt - endpoint; // how the camera should face at the end of the move
				Vector3 currentLookAt = cl.transform.position+finalLookVector;
				cl.RotateTowards(currentLookAt);
//				cl.LookAt(LookingAtSearch(pointA, pointB, cl.transform.position));
			}
		}
	}

    Vector3 LookingAtSearch(Vector3 lineA, Vector3 lineB, Vector3 pointA)
    {
        if((lineA - lineB).magnitude < 0.001)
            return lineA + (lineB - lineA) * 0.5f;

        float test1 = (lineA - pointA).magnitude;
        float test2 = (lineB - pointA).magnitude;
        Vector3 halfPoint = lineA + (lineB - lineA) * 0.5f;

        if (test1 < test2)
        {
            return LookingAtSearch(lineA, halfPoint, pointA);
        }
        else if (test2 < test1)
        {
            return LookingAtSearch(lineB, halfPoint, pointA);
        }
        else
            return halfPoint;
        
    }

    public void Stop()
    {
        outerRail.timeMod = 0;
        innerRail.timeMod = 0;
    }

    public void Switch()
    {
        CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        if (cl != null)
        {
            if (outer)
            {
                if(Brain.GetInstance() != null)
                    Brain.GetInstance().PutMessage(new InteractStatusMsg("RAIL:SWITCH:INNER"));
                cl.MoveTo(innerRail.transform, 1, true, false);
            }
            else
            {
                if (Brain.GetInstance() != null)
                    Brain.GetInstance().PutMessage(new InteractStatusMsg("RAIL:SWITCH:OUTER"));
                cl.MoveTo(outerRail.transform, 1, true, false);
            }
            outer = !outer;
        }
    }

    public void Reset()
    {
        innerRail.Restart();
        outerRail.Restart();
        CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        if (cl != null)
        {
            cl.LookAt(LookingAtSearch(pointA, pointB, cl.transform.position));
        }
    }

    //void OnGUI()
    //{
        //GUILayout.Space(20);
        //if (GUILayout.Button("Stop"))
        //{
        //    Stop();
        //}
        //if (GUILayout.Button("Forward"))
        //{
        //    Forward();
        //}
        //if (GUILayout.Button("Reverse"))
        //{
        //    Reverse();
        //}
        //if (GUILayout.Button("GetCam"))
        //{
        //    CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        //    if (cl != null)
        //        cl.MoveTo(outerRail.transform, 0, true, false);
        //}
        //if (GUILayout.Button("Switch"))
        //{
        //    CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        //    if (cl != null)
        //    {
        //        if (outer)
        //            cl.MoveTo(innerRail.transform, 1, true, false);
        //        else
        //            cl.MoveTo(outerRail.transform, 1, true, false);
        //        outer = !outer;
        //    }
        //}
        //if (GUILayout.Button("Detach"))
        //{
        //    CameraLERP cl = Camera.main.GetComponent<CameraLERP>();
        //    if (cl != null)
        //    {
        //        cl.Return();
        //    }
        //}
    //}
}

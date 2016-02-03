using UnityEngine; 
using System.Collections;

public class CameraZoomPinch : MonoBehaviour { 
	public bool doZoom = true; // these are only public to allow initial setting in the editor.
	public bool doPan = true;
	public bool doTwist = false;
	public bool doLook = false;
	public int speed = 3; 
	public Camera selectedCamera; 
	public float MINSCALE = 2.0F; 
	public float MAXSCALE = 5.0F; 
	private Vector3 cameraStartPos;
	private float cameraStartFOV = 0;
	private Vector3 cameraRight,cameraUp;
	private Vector2 panLimit = new Vector2(0.15f,0.1f);
	private float panSpeed = 0.00033f;
	private Vector2 currentPan = Vector2.zero;
	private GestureManager.RegisteredListener rlPan,rlZoom,rlTwist,rlLook;
	
	public bool DoZoom
	    {
        get { return doZoom; }
        set { 
			if (doZoom != value){
				if (value){
					GestureManager.Register(rlZoom);
				}
				else
				{
					GestureManager.Unregister(rlZoom);
				}
				doZoom = value;
			}
		}
    }
	public bool DoPan
	    {
        get { return doPan; }
        set { 
			if (doPan != value){
				if (value){
					GestureManager.Register(rlPan);
				}
				else
				{
					GestureManager.Unregister(rlPan);
				}
				doPan = value;
			}
		}
    }
	public bool DoTwist
	    {
        get { return doTwist; }
        set { 
			if (doTwist != value){
				if (value){
					GestureManager.Register(rlTwist);
				}
				else
				{
					GestureManager.Unregister(rlTwist);
				}
				doTwist = value;
			}
		}
    }
	public bool DoLook
	    {
        get { return doLook; }
        set { 
			if (doLook != value){
				if (value){
					GestureManager.Register(rlLook);
				}
				else
				{
					GestureManager.Unregister(rlLook);
				}
				doLook = value;
			}
		}
    }

    // Use this for initialization
    void Awake ()
    {		
		rlPan = new GestureManager.RegisteredListener();
		rlPan.callback = HandleSwipe;
		rlPan.screenRect = new Rect(0,0,0,0);
		rlPan.type = GestureManager.gestureType.swipe;
		
		rlZoom = new GestureManager.RegisteredListener();
		rlZoom.callback = HandlePinch;
		rlZoom.screenRect = new Rect(0,0,0,0);
		rlZoom.type = GestureManager.gestureType.pinch;
		
		rlTwist = new GestureManager.RegisteredListener();
		rlTwist.callback = HandleTwist;
		rlTwist.screenRect = new Rect(0,0,0,0);
		rlTwist.type = GestureManager.gestureType.twist;
		
		rlLook = new GestureManager.RegisteredListener();
		rlLook.callback = HandleLook;
		rlLook.screenRect = new Rect(0,0,0,0);
		rlLook.type = GestureManager.gestureType.swipe;
		
		cameraStartFOV = selectedCamera.fieldOfView;
		SetHomePoint ();
		
		if (doPan){
			GestureManager.Register(rlPan);
		}
		//This struct is passed by reference, so make a new one for each call
		// or you'll be overwrited the one in the manager's list.
		if (doZoom){
			GestureManager.Register(rlZoom);
		}
		if (doTwist){
			GestureManager.Register(rlTwist);
		}
		if (doLook){
			GestureManager.Register(rlLook);
		}
    }
	
	public void SetHomePoint(){
		cameraStartPos = selectedCamera.transform.position;
		cameraRight = selectedCamera.transform.right;
		cameraUp = selectedCamera.transform.up;
		currentPan = Vector2.zero;
	}
	
	public void HandleSwipe(string message){
		float zoomPct = selectedCamera.fieldOfView/cameraStartFOV;
		Vector2 delta;
		string[] parms = message.Split(' ');
		delta.x = float.Parse(parms[0]);
		delta.y = float.Parse(parms[1]);
		currentPan += delta*panSpeed*zoomPct; 
		currentPan.x = Mathf.Clamp(currentPan.x,-panLimit.x,panLimit.x);
		currentPan.y = Mathf.Clamp(currentPan.y,-panLimit.y,panLimit.y);
		Vector3 newPos = cameraStartPos - cameraRight*currentPan.x - cameraUp*currentPan.y;
		selectedCamera.transform.position = newPos;
	}
	
	public void HandlePinch(string message){
		float touchDelta = float.Parse(message);
		float sign = Mathf.Sign(touchDelta);
		selectedCamera.fieldOfView = Mathf.Clamp(selectedCamera.fieldOfView - (sign * speed),15,90);
	}
	
	public void HandleTwist(string message){
		float angleDelta = float.Parse(message);
		Quaternion twist = Quaternion.AngleAxis(angleDelta, selectedCamera.transform.forward);
		selectedCamera.transform.rotation = twist * selectedCamera.transform.rotation;
	}
	
	public void HandleLook(string message){
		float zoomPct = selectedCamera.fieldOfView/cameraStartFOV;
		Vector2 delta;
		string[] parms = message.Split(' ');
		delta.x = -.25f*float.Parse(parms[0])*zoomPct;
		delta.y = 0.3f*float.Parse(parms[1])*zoomPct;
		
		Quaternion az = Quaternion.AngleAxis(delta.x, new Vector3(0,1,0));
		Quaternion alt = Quaternion.AngleAxis(delta.y, selectedCamera.transform.right);
		// apply the rotations
		selectedCamera.transform.rotation = alt * az * selectedCamera.transform.rotation;
		// switch to Euler space to clamp and level camera
		Vector3 camEuler = selectedCamera.transform.rotation.eulerAngles;
		// keep the axis in the vertical/forward plane
		camEuler.z=0;
		// clamp look up/down to 60 deg.
		if (camEuler.x > 180) camEuler.x -=360.0f;
		camEuler.x = Mathf.Clamp(camEuler.x,-60.0f,60.0f);
		selectedCamera.transform.rotation = Quaternion.Euler(camEuler);
	}
	
	
	
	
/*     
    // Update is called once per frame
    void Update ()
    {
#if false// UNITY_IPHONE
		// detect one touch drag
	    if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved )
	    {
			float zoomPct = selectedCamera.fieldOfView/cameraStartFOV;
			Vector2 delta = Input.GetTouch(0).deltaPosition;
			currentPan += delta*panSpeed*zoomPct; 
			currentPan.x = Mathf.Clamp(currentPan.x,-panLimit.x,panLimit.x);
			currentPan.y = Mathf.Clamp(currentPan.y,-panLimit.y,panLimit.y);
			Vector3 newPos = cameraStartPos - cameraRight*currentPan.x - cameraUp*currentPan.y;
			selectedCamera.transform.position = newPos;
		}
		
	    if (Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
	    {
	     
	    	curDist = Input.GetTouch(0).position - Input.GetTouch(1).position; //current distance between finger touches
	    	prevDist = ((Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition) - (Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition)); //difference in previous locations using delta positions
	    	touchDelta = curDist.magnitude - prevDist.magnitude;
	    	speedTouch0 = Input.GetTouch(0).deltaPosition.magnitude / Input.GetTouch(0).deltaTime;
	    	speedTouch1 = Input.GetTouch(1).deltaPosition.magnitude / Input.GetTouch(1).deltaTime;
	     
	     
	    	if ((touchDelta + varianceInDistances <= 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))
	    	{
	    		selectedCamera.fieldOfView = Mathf.Clamp(selectedCamera.fieldOfView + (1 * speed),15,90);
	   	 	}
	     
	    	if ((touchDelta +varianceInDistances > 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))
	    	{
	     		selectedCamera.fieldOfView = Mathf.Clamp(selectedCamera.fieldOfView - (1 * speed),15,90);
	    	}
		}
#endif
    }
 */
}



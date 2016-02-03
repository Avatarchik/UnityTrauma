using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GestureManager : MonoBehaviour {
	
	/* the Gesture Manager accepts registration from objects interested in touch events.
	 * Registering objects provide a callback to handle the event, and an optional screen rectangle to bound the gestures
	 * Controls should un-register when they become inactive.
	 */
	public static GestureManager _Instance = null;
	
	public enum gestureType{
		click,
		swipe,
		pinch,
		twist,
	}
	
	public delegate void GestureCallback(string message);
	
	public class RegisteredListener{
		public GestureCallback callback;
		public Rect screenRect;
		public gestureType type;
	}
	
	public float minPinchSpeed = 5.0F; 
	public float varianceInDistances = 5.0F;
	
	private List<RegisteredListener> listeners;
	private int[] numListeners = new int[10]; // bigger than number of gesture types

	// Use this for initialization
	void Start () {
		if (listeners == null)
			listeners = new List<RegisteredListener>();
	}
	
	void Awake(){
		// find or make instance of singleton
		_Instance=this;
		if (listeners == null)
			listeners = new List<RegisteredListener>();
	}
	static void CreateInstance(){
		_Instance = GameObject.FindObjectOfType(typeof(GestureManager)) as GestureManager;
		if (_Instance == null){
			GameObject CGO = Camera.main.gameObject;
			_Instance = CGO.AddComponent<GestureManager>();
		}
		_Instance.listeners = new List<RegisteredListener>();
	}

#if UNITY_IPHONE
	// Update is called once per frame
	void Update () {
		if (listeners.Count == 0) return;

		// detect one touch drag
	    if (numListeners[(int)gestureType.swipe] > 0 &&
			Input.touchCount == 1 && 
			Input.GetTouch(0).phase == TouchPhase.Moved )
	    {
			// got a swipe - drag motion
			foreach (RegisteredListener listener in listeners){
				if (listener.type == gestureType.swipe)
				//	&& (listener.screenRect == new Rect(0,0,0,0) || listener.screenRect.Contains(Input.GetTouch(0).position)))
				{
					string message = Input.GetTouch(0).deltaPosition.x + " "+Input.GetTouch(0).deltaPosition.y;
					GestureCallback cb = listener.callback;
					cb(message);
				}
			}
		}
		
	    if (numListeners[(int)gestureType.pinch] > 0 &&
			Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Moved && 
			Input.GetTouch(1).phase == TouchPhase.Moved)
	    {
			// see if this is a pinch or maybe a twist
			
			Vector2 curDist = Input.GetTouch(0).position - Input.GetTouch(1).position; //current distance between finger touches
	    	Vector2 prevDist = ((Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition) - (Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition)); //difference in previous locations using delta positions
	    	float touchDelta = curDist.magnitude - prevDist.magnitude;
	    	float speedTouch0 = Input.GetTouch(0).deltaPosition.magnitude / Input.GetTouch(0).deltaTime;
	    	float speedTouch1 = Input.GetTouch(1).deltaPosition.magnitude / Input.GetTouch(1).deltaTime;
			// amount. Angle only returns positive angles, so compare both to a fixed vector,
			// but this code sometimes gets the sign backwards, so TODO: fix this!
			float a1 = Vector2.Angle(new Vector2(1,0),prevDist);
			float angleDelta = Vector2.Angle(new Vector2(1,0),curDist);
			angleDelta -= a1;

//	    	if ((touchDelta + varianceInDistances <= 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))
//	    	if ((touchDelta +varianceInDistances > 1) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed))

			if (Mathf.Abs(angleDelta) > 2){ // isTwist
				foreach (RegisteredListener listener in listeners){
					if (listener.type == gestureType.twist)
						//&& (listener.screenRect == new Rect(0,0,0,0) || listener.screenRect.Contains(Input.GetTouch(0).position)))
					{
						string message = angleDelta.ToString();
						GestureCallback cb = listener.callback;
						cb(message);
					}
				}
			}

			// isPinch?
			if ((Mathf.Abs(touchDelta) >= varianceInDistances) && (speedTouch0 > minPinchSpeed) && (speedTouch1 > minPinchSpeed)){
				foreach (RegisteredListener listener in listeners){
					if (listener.type == gestureType.pinch)
						//&& (listener.screenRect == new Rect(0,0,0,0) || listener.screenRect.Contains(Input.GetTouch(0).position)))
					{
						string message = touchDelta.ToString();
						GestureCallback cb = listener.callback;
						cb(message);
					}
				}
			}
		}
	}
#else   // not iPhone, so watch mouse events
		// process left mouse drag for swipe and mouse wheel for zoom, right mouse drag for twist
	public void OnGUI(){
		if (listeners != null || listeners.Count == 0) return;
		
		if (Event.current.isMouse || Event.current.type == EventType.ScrollWheel){
			if (Event.current.type == EventType.MouseDrag){
				// left button for pan,
				if (Input.GetMouseButton(1) && numListeners[(int)gestureType.swipe] > 0){
					// got a swipe - drag motion
					foreach (RegisteredListener listener in listeners){
						if (listener.type == gestureType.swipe)
						//	&& (listener.screenRect == new Rect(0,0,0,0) || listener.screenRect.Contains(Input.GetTouch(0).position)))
						{
							string message = Event.current.delta.x.ToString() + " "+(-Event.current.delta.y).ToString();
							GestureCallback cb = listener.callback;
							cb(message);
						}
					}
				}
				// right button X axis for twist
				if (Input.GetMouseButton(2) && numListeners[(int)gestureType.twist] > 0){
					foreach (RegisteredListener listener in listeners){
						if (listener.type == gestureType.twist)
						//	&& (listener.screenRect == new Rect(0,0,0,0) || listener.screenRect.Contains(Input.GetTouch(0).position)))
						{
							string message = Event.current.delta.x.ToString();
							GestureCallback cb = listener.callback;
							cb(message);
						}
					}					
				}
			}
		
			if (Event.current.type == EventType.ScrollWheel && numListeners[(int)gestureType.pinch] > 0){
				// process this like a pinch
				foreach (RegisteredListener listener in listeners){
					if (listener.type == gestureType.pinch){
						string message = Event.current.delta.y.ToString();
						GestureCallback cb = listener.callback;
						cb(message);
					}
				}
			}
		}
	}	
#endif	
	
	public static void Register(RegisteredListener listener){
		if (_Instance == null) CreateInstance();
		if (!_Instance.listeners.Contains(listener)){
			_Instance.listeners.Add(listener);
			_Instance.numListeners[(int)listener.type]++;
		}
	}
	public static void Unregister(RegisteredListener listener){
		if (_Instance.listeners.Contains(listener)){
			_Instance.listeners.Remove(listener);
			_Instance.numListeners[(int)listener.type]--;
		}		
	}
}

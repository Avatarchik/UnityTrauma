using UnityEngine;
using System.Collections;


/*  This is designed to smoothly couple the main camera to a 1st person controller to give a better
 *  physical feel to the movement.
 * 
 * This has been specifically tuned for the museum level.  drag this onto the main camera and turn off or
 * delete the 'Graphics' capsule renderer.
 * 
 * Recommend setting mouseLook SensitivityX to -5
 * 
 */

public class SmoothCoupler : MonoBehaviour {
	
	public Transform master = null;
	public bool softenMouseLook = false; // should we?
	public float maxDistance = 5.0f;
	public float moveRate = 2.0f;
	public float maxAngle = 60.0f;
	public float turnRate = 60.0f;
	Vector3 prevMasterPos = Vector3.zero;
	Vector3 prevMasterRot = Vector3.zero;
	float yOffset = 0; // camera height vs the controller height.
//	Vector3 prevPos = Vector3.zero;
//	Vector3 prevRot = Vector3.zero;
//	float skewy = 0;
	float currentTurnRate = 0;
	float masterTurnRate = 0;
//	float torque = 0;
//	float currentSpeed = 0;
	bool aligning = false;
	bool homing = false;
	float timeRemainingToAlign = 0;
	float timeRemainingToHome = 0;
	float alignTime = 1.5f;

	// Use this for initialization
	void Start () {
		if (master != null){
			prevMasterPos = master.position;
			prevMasterRot = master.rotation.eulerAngles;
			yOffset = transform.position.y - master.position.y;
//			transform.position = prevMasterPos;
			transform.rotation = Quaternion.Euler (0,prevMasterRot.y,0);
			transform.parent = null;
		}
	
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 aim = master.position - transform.position;
		aim.y += yOffset;
		bool dragging=false;
		
		if (aim.magnitude < 0.0001f) {
			alignTime = 0.0f; // more responsive while just looking around.
			dragging = true; // we might have arrived already, so be careful here...
			if (!softenMouseLook){
				transform.rotation = master.rotation;
				return;
			}
		}
		else {
			alignTime = 1.5f; // longer alignment when navigating
		}
		
		
		Vector3 skew = master.rotation.eulerAngles - transform.eulerAngles;
		if (aim == Vector3.zero && skew.y == 0) return; // we are at the master position
		
		Vector3 masterDelta = master.position - prevMasterPos;

		if (masterDelta.magnitude == 0){
			if ( homing == false){
				homing = true;
				timeRemainingToHome = 2.0f+Time.deltaTime;
			}
			timeRemainingToHome -= Time.deltaTime;
		}
		else
			homing = false;
		
		Vector3 move;
		if (homing){
			if (timeRemainingToHome <0){
				homing = false;
				timeRemainingToHome = 0;
				move = aim;
			}
			else
			{
				move = 5.0f*Time.deltaTime*aim/timeRemainingToHome;
			}
		}
		else
		{
			move = moveRate*Time.deltaTime*aim*(aim.magnitude/maxDistance);
		}
		
		masterTurnRate = Mathf.Abs(master.rotation.eulerAngles.y - prevMasterRot.y);
		
		prevMasterPos = master.position;
		prevMasterRot = master.rotation.eulerAngles;
		// mod 360
		if (skew.y < -180) skew.y += 360;
		if (skew.y > 180) skew.y -= 360;
		if (masterTurnRate < -180) masterTurnRate += 360;
		if (masterTurnRate > 180) masterTurnRate -= 360;
		
		
		float desiredTurnRate = 0;
		if (Mathf.Abs(skew.y) > 0.1f){
			if (masterTurnRate == 0){
				if (aligning == false){
					// we are just starting an alignment
					aligning = true;
					timeRemainingToAlign = alignTime+Time.deltaTime;
				}
				timeRemainingToAlign -= Time.deltaTime;
				if (timeRemainingToAlign <= 0){
					aligning=false;
					transform.rotation = master.rotation;
					currentTurnRate = 0;
					return;
				}
				// we have a current turn rate, how long would that take us to get there?
				float timeAtCurrentRate = Mathf.Abs(skew.y)/currentTurnRate;
				float prop = timeAtCurrentRate/timeRemainingToAlign;
				if (prop > 1){
					// increase our turn rate as much as we are allowed.
					desiredTurnRate = 1000;
				}
				else
				{
					// we're going to make it, we can relax. Start a smooth deceleration
					if (prop < 0.25f)
					{	// slow as rapidly as we are allowed
						desiredTurnRate = 0;
					}
					else
					{	// we are approaching nicely, slow proportionately
						// pick the rate that would get us there in half the remaining time
						desiredTurnRate = 3.0f*Mathf.Abs(skew.y)/timeRemainingToAlign;
					}
				}
				
				
//				desiredTurnRate = turnRate *Mathf.Abs (skew.y)* Mathf.Abs (skew.y)/maxAngle;
			}
			else
			{
				aligning = false;
				if (dragging)
					desiredTurnRate = Mathf.Abs (skew.y)/5.0f/Time.deltaTime;
				//desiredTurnRate = masterTurnRate * Mathf.Abs (skew.y)/maxAngle/Time.deltaTime;
				else
					desiredTurnRate = Mathf.Abs (skew.y)/maxAngle/Time.deltaTime;
			}
		}
		else{
			aligning = false;
			transform.rotation = master.rotation;
			currentTurnRate = 0;
		}
		
		

		if (desiredTurnRate > currentTurnRate){
			if (currentTurnRate == 0) currentTurnRate = 0.25f;
			else{
				if (dragging)
					currentTurnRate*=1.5f;
				else
					currentTurnRate*=1.1f;
			}
			if (currentTurnRate > desiredTurnRate) currentTurnRate = desiredTurnRate;
		}
		if (desiredTurnRate < currentTurnRate){
			currentTurnRate*=0.75f;
			if (currentTurnRate < desiredTurnRate) currentTurnRate = desiredTurnRate;
		}
		float turn = currentTurnRate*Time.deltaTime*Mathf.Sign(skew.y);
//		skewy = skew.y;
		
		transform.position = transform.position+move;
		Vector3 eul = transform.rotation.eulerAngles;
		transform.rotation = Quaternion.Euler(eul.x,eul.y+turn,eul.z);
	}
	
//	public void OnGUI(){
//		GUILayout.Label("                        skew="+skewy+" masterTurn="+masterTurnRate+" currentTurnRate="+currentTurnRate+" aligning:"+aligning+timeRemainingToAlign);	
//	}
}

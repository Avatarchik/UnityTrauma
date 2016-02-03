using UnityEngine;
using System.Collections;


// Require a character controller to be attached to the same game object
[RequireComponent (typeof(CharacterMotor))]
[AddComponentMenu ("Character/FPS Input Controller")]

public class FPSInputController : MonoBehaviour
{
    private CharacterMotor motor;
	
	public bool MouseButton0Move=false;
	public bool MouseButton1Move=false;
	public float MouseButtonMoveScale=100.0f;
	
    // Use this for initialization
    void Awake () {
	    motor = GetComponent<CharacterMotor>();
    }

	bool mouseDownL=false;
	bool mouseDownR=false;
	bool doJump=false;
	
    // Update is called once per frame
    void Update () {
	    // Get the input vector from kayboard or analog stick
	    var directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		
		if ( Input.GetMouseButtonDown(0) == true )
			mouseDownL = true;
		if ( Input.GetMouseButtonUp(0) == true )
			mouseDownL = false;
		if ( Input.GetMouseButtonDown(1) == true )
			mouseDownR = true;
		if ( Input.GetMouseButtonUp(1) == true )
			mouseDownR = false;
		
		// check this when there is no input
		if (directionVector == Vector3.zero && mouseDownL == true && MouseButton0Move) {
			directionVector = new Vector3(0,0,Time.deltaTime*MouseButtonMoveScale);
		}
		if (directionVector == Vector3.zero && mouseDownR == true && MouseButton1Move) {
			directionVector = new Vector3(0,0,-Time.deltaTime*MouseButtonMoveScale);
		}
    	
	    if (directionVector != Vector3.zero) {
		    // Get the length of the directon vector and then normalize it
		    // Dividing by the length is cheaper than normalizing when we already have the length anyway
		    var directionLength = directionVector.magnitude;
		    directionVector = directionVector / directionLength;
    		
		    // Make sure the length is no bigger than 1
		    directionLength = Mathf.Min(1, directionLength);
    		
		    // Make the input vector more sensitive towards the extremes and less sensitive in the middle
		    // This makes it easier to control slow speeds when using analog sticks
		    directionLength = directionLength * directionLength;
    		
		    // Multiply the normalized direction vector by the modified length
		    directionVector = directionVector * directionLength;
	    }
    	
	    // Apply the direction to the CharacterMotor
	    motor.inputMoveDirection = transform.rotation * directionVector;
		if ( doJump == true )
	    	motor.inputJump = Input.GetButton("Jump");
    }
}
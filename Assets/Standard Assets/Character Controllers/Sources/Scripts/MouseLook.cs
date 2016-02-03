using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	float rotationY = 0F;
	
	public bool requireKey=false;
	bool keydown=false;
	
	public bool requireMouse=false;
	bool mousedown=false;
	
	public bool mouseButton0=true;
	public bool mouseButton1=false;
	public bool mouseButton2=false;
	
	bool active=true;
	public void SetActive( bool active )
	{
		if ( active == true && this.active == false )
			Input.ResetInputAxes();
		this.active = active;
		this.mousedown = false;
	}

	void Update ()
	{
		if ( active == false )
			return;
		
		// require space to move
		if ( Input.GetKeyDown(KeyCode.Space) == true )
			keydown = true;
		if ( Input.GetKeyUp(KeyCode.Space) == true )
			keydown = false;
		if ( requireKey == true && keydown == false )
			return;                                        	
		
		// require move down to move
		if ( mouseButton0 == true && Input.GetMouseButtonDown(0) == true )
			mousedown = true;
		if ( mouseButton0 == true && Input.GetMouseButtonUp(0) == true )
			mousedown = false;
		if ( mouseButton1 == true && Input.GetMouseButtonDown(1) == true )
			mousedown = true;
		if ( mouseButton1 == true && Input.GetMouseButtonUp(1) == true )
			mousedown = false;
		if ( mouseButton2 == true && Input.GetMouseButtonDown(2) == true )
			mousedown = true;
		if ( mouseButton2 == true && Input.GetMouseButtonUp(2) == true )
			mousedown = false;
		
		if ( requireMouse == true && mousedown == false )
			return;
		
		if (axes == RotationAxes.MouseXAndY)
		{
			float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
			
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
		}
		else if (axes == RotationAxes.MouseX)
		{
			transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
			transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
		}
	}
	
	void Start ()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
			rigidbody.freezeRotation = true;
	}
}
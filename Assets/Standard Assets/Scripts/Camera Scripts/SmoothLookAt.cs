using UnityEngine;

[AddComponentMenu("Camera-Control/Smooth Look At")]
public class SmoothLookAt : MonoBehaviour
{
    public Transform target;
    public int speed = 10;
    public bool smooth = true;
    public bool moving = false;
    public float lastAngle;

    void LateUpdate ()
    {
	    if (target) {
		    if (smooth)
		    {
		        // Look at and dampen the rotation
		        if(!moving) {
	                moving = true;
                    lastAngle = transform.rotation.y;
                }		    
		        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * speed);
    			    
		    }
	    }
    }

    void Update()
    {
        if(moving){
            if(Mathf.Abs(lastAngle - transform.rotation.y) < 0.0003f) {
                target = null;
                moving = false;
            }
            else 
                lastAngle = transform.rotation.y;
        }   
    }


    void Start()
    {
	    // Make the rigid body not change rotation	
   	    if (rigidbody)
		    rigidbody.freezeRotation = true;
    		
        //oldRot = Quaternion.identity;
    }
}
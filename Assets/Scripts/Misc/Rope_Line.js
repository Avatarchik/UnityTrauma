//============================
//==	Physics Based 3D Rope ==
//==	File: Rope_Tube.js	==
//==	By: Jacob Fletcher	==
//==	Use and alter Freely	==
//============================
//To see other things I have created, visit me at www.reverieinterative.com
//How To Use:
// ( BASIC )
// 1. Simply add this script to the object you want a rope teathered to
// 3. Assign the other end of the rope as the "Target" object in this script
// 4. Play and enjoy!
// (About Character Joints)
// Sometimes your rope needs to be very limp and by that I mean NO SPRINGY EFFECT.
// In order to do this, you must loosen it up using the swingAxis and twist limits.
// For example, On my joints in my drawing app, I set the swingAxis to (0,0,1) sense
// the only axis I want to swing is the Z axis (facing the camera) and the other settings to around -100 or 100.
var target : Transform;
var targetPosition : Transform; // darn i wish i had named this better, cause it's not the target that gets put here...
var targetParent : Transform = null; // if set, reparent the Target to this transform
var targetLerp : float = 0;
var material : Material;
var ropeWidth = 0.5;
var resolution = 0.5;
var ropeDrag = 0.1;
var ropeAngularDrag = 0.1;
var ropeMass = 0.5;
var radialSegments = 6;
var startRestrained = true;
var endRestrained = false; // for our tubing, we restrain both ends
var useMeshCollision = false; // since the tube renderer has a mesh collider, set this true.
var hideAfterBuilding = false; // turn off the tubing renderer
// Private Variables (Only change if you know what your doing)
private var segmentPos : Vector3[];
private var joints : GameObject[];
private var tubeRenderer : GameObject;
private var line : TubeRenderer;
private var segments = 4;
private var rope = false;
private var id : String = "";
//Joint Settings
var swingAxis = Vector3(0,1,0);
var lowTwistLimit = 0.0;
var highTwistLimit = 0.0;
var swing1Limit	= 20.0;
// Require a Rigidbody
@script RequireComponent(Rigidbody)
 
function OnDrawGizmos() {
	if(target) {
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine (transform.position, target.position);
		Gizmos.DrawWireSphere ((transform.position+target.position)/2,ropeWidth);
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere (transform.position, ropeWidth);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere (target.position, ropeWidth);
	} else {
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere (transform.position, ropeWidth);	
	}

	if (segmentPos == null || segmentPos.Length<(segments-1)) return;
	Gizmos.color = Color.blue;
	for(var s:int=0;s < segments;s++)
	{
		Gizmos.DrawWireSphere (segmentPos[s], ropeWidth);
	}

}
 
function Awake()
{
	id = transform.parent.name.Replace("IVLine","");
	name = name+id; // add our suffix
	if(target) {
		target.transform.parent= targetParent; // packaged as our child for the prefab template
		target.name = target.name+id; // add our suffix
		 BuildRope();
	} else {
		Debug.LogError("You must have a gameobject attached to target: " + this.name,this);	
	}
}
 
function LateUpdate()
{
	if(target) {
		// Does rope exist? If so, update its position
		if(rope) {
//		joints[segments-1].transform.position = target.transform.position;
		 line.SetPoints(segmentPos, ropeWidth, Color.white);
		 line.enabled = true;
			segmentPos[0] = transform.position;
			for(var s:int=1;s<segments;s++)
			{
			 segmentPos[s] = joints[s].transform.position;
			}
			segmentPos[segments-1] = target.transform.position;
		}
		if (targetPosition != null){
			transform.position = Vector3.Lerp(transform.position,targetPosition.position,targetLerp);
			transform.rotation = Quaternion.Lerp(transform.rotation,targetPosition.rotation,targetLerp);
			targetLerp += 1.0f/100;
			if (targetLerp > 1){
				transform.parent = targetPosition.parent;
			 	targetPosition = null;
			 	tubeRenderer.renderer.useLightProbes = true;
			 	if (hideAfterBuilding) tubeRenderer.renderer.enabled = false;
			}
		}
		/// for debugging length change
/*
		for (var n:int = 0; n<segments; n++){
			var c :ConfigurableJoint = joints[n].GetComponent("ConfigurableJoint") as ConfigurableJoint;
			var nd:float = Vector3.Distance(c.transform.position,c.connectedBody.transform.position);
			c.highAngularXLimit.damper  = nd/ c.lowAngularXLimit.damper; 
		}
*/
	}
}
 
function BuildRope()
{
	if (rope) return; // already built
	tubeRenderer = new GameObject("TubeRenderer_" + gameObject.name);
	line = tubeRenderer.AddComponent(TubeRenderer);
	line.useMeshCollision = useMeshCollision;
	
	// Find the amount of segments based on the distance and resolution
	// Example: [resolution of 1.0 = 1 joint per unit of distance]
	segments = Vector3.Distance(transform.position,target.position)*resolution;
	if(material) {
		 material.SetTextureScale("_MainTex",Vector2(1,segments+2));
		 if(material.GetTexture("_BumpMap"))
			material.SetTextureScale("_BumpMap",Vector2(1,segments+2));
	}
	line.vertices = new TubeVertex[segments];
	line.crossSegments = radialSegments;
	line.material = material;
	segmentPos = new Vector3[segments];
	joints = new GameObject[segments];
//	segmentPos[0] = transform.position;
//	segmentPos[segments-1] = target.position;
	// Find the distance between each segment
	var segs = segments;//-1  this will make the rope a bit longer than the distance
	
	var seperation = ((target.position - transform.position)/segs);
	for(var s:int=0;s < segments;s++)
	{
		// Find the each segments position using the slope from above
		var vector : Vector3 = (seperation*(s+1)) + transform.position;	
		segmentPos[s] = vector;
		//Add Physics to the segments
		AddConfigurableJointPhysics(s);
	}
	// Attach the joints to the target object and parent it to this object
	var end : ConfigurableJoint = target.gameObject.AddComponent("ConfigurableJoint");
	end.connectedBody = joints[joints.length-1].transform.rigidbody;
/*
	end.swingAxis = swingAxis;
	end.lowTwistLimit.limit = lowTwistLimit;
	end.highTwistLimit.limit = highTwistLimit;
	end.swing1Limit.limit	= swing1Limit;
*/
	var k:float = 1000000000000;
end.anchor = Vector3.zero;
end.lowAngularXLimit.damper = k*10;
end.highAngularXLimit.damper = k*10;
end.angularYLimit.damper = k*10;
end.angularZLimit.damper = k*10;

	end.axis = new Vector3(0,1,0);// was transform.up //added
	end.secondaryAxis = new Vector3(1,0,0);//Vector3.left; //swingAxis;
	end.xMotion = ConfigurableJointMotion.Locked;
	end.yMotion = ConfigurableJointMotion.Limited;//
	end.zMotion = ConfigurableJointMotion.Locked;
	end.linearLimit.spring = 0;//100000;
	end.linearLimit.damper = 1000;
	end.angularXMotion = ConfigurableJointMotion.Limited;
	end.lowAngularXLimit.limit = -1;
	end.lowAngularXLimit.spring = k;
	end.highAngularXLimit.limit = 1;
	end.highAngularXLimit.spring = k;
	end.angularYMotion = ConfigurableJointMotion.Limited;
	end.angularZMotion = ConfigurableJointMotion.Limited;
	end.angularYLimit.limit = 1;
	end.angularYLimit.spring = k;
	end.angularZLimit.limit = 1;
	end.angularZLimit.spring = k;

	end.xDrive.mode=JointDriveMode.Position;
	end.xDrive.positionSpring=10000;
	end.yDrive.mode=JointDriveMode.Position;
	end.yDrive.positionSpring=10000;
	end.zDrive.mode=JointDriveMode.Position;
	end.zDrive.positionSpring=10000;







//	target.parent = transform;
	if(endRestrained)
	{
		end.rigidbody.isKinematic = true;
	}
	if(startRestrained)
	{
		transform.rigidbody.isKinematic = true;
	}
	// Rope = true, The rope now exists in the scene!
	rope = true;
}
 
function AddConfigurableJointPhysics(n : int)
{
	var k:float = 0.7; //0.6; //1;//2; // at 0.1,0.3 the tubing kinks where it should bend sharply
	if (n<10) k+=(10-n);
	if (n>segments-10) k+= 10.0*Mathf.Pow(1.75,n-segments+11);
	if (n<=1) k=10000000000000000;


	joints[n] = new GameObject("Joint_" + n);
	joints[n].transform.parent = transform;
	joints[n].transform.rotation = transform.rotation;//added
//joints[n].transform.position = segmentPos[n];
//return;
	var rigid : Rigidbody = joints[n].AddComponent("Rigidbody");
	if(!useMeshCollision) {
		 var col : SphereCollider = joints[n].AddComponent("SphereCollider");
		 col.radius = ropeWidth;
	}

	var ph : ConfigurableJoint = joints[n].AddComponent("ConfigurableJoint");
	ph.axis = new Vector3(0,1,0);// was transform.up //added
	ph.secondaryAxis = new Vector3(1,0,0);//Vector3.left; //swingAxis;
	ph.xMotion = ConfigurableJointMotion.Locked;
	ph.yMotion = ConfigurableJointMotion.Limited;//
	ph.zMotion = ConfigurableJointMotion.Locked;
	ph.linearLimit.spring = 0;//100000;
	ph.linearLimit.damper = 1000;
	ph.angularXMotion = ConfigurableJointMotion.Limited;
	ph.lowAngularXLimit.limit = -1; //20
	ph.lowAngularXLimit.spring = k;
	ph.highAngularXLimit.limit = 1;
	ph.highAngularXLimit.spring = k;
	ph.angularYMotion = ConfigurableJointMotion.Limited;
	ph.angularZMotion = ConfigurableJointMotion.Limited;
	ph.angularYLimit.limit = 1;
	ph.angularYLimit.spring = k;
	ph.angularZLimit.limit = 1;
	ph.angularZLimit.spring = k;
	if (k>1){
		ph.lowAngularXLimit.damper = k*10;
		ph.highAngularXLimit.damper = k*10;
		ph.angularYLimit.damper = k*10;
		ph.angularZLimit.damper = k*10;
	}

	ph.xDrive.mode=JointDriveMode.Position;
	ph.xDrive.positionSpring=10000;
	ph.yDrive.mode=JointDriveMode.Position;
	ph.yDrive.positionSpring=10000;
	ph.zDrive.mode=JointDriveMode.Position;
	ph.zDrive.positionSpring=10000;
/*
	ph.angularXDrive.mode=JointDriveMode.Position;
	ph.angularXDrive.positionSpring=100;
	ph.angularYZDrive.mode=JointDriveMode.Position;
	ph.angularYZDrive.positionSpring=100;
*/


	//ph.breakForce = ropeBreakForce; <--------------- TODO
	joints[n].transform.position = segmentPos[n];
	rigid.drag = ropeDrag;
	rigid.mass = ropeMass;
	rigid.angularDrag = ropeDrag;
//rigid.useGravity = false;
//rigid.isKinematic=true;
	//rigid.freezeRotation = 
	
	if (n%2==0 ||((n<segments*.25)||(n>segments*.75))) rigid.useGravity = false;//only weight half of the center of the line
	if (n==(segments-1)){
//		rigid.isKinematic=true; //should we freeze stuff, too ?
		
	}

	if(n==0){			
		ph.connectedBody = transform.rigidbody;
		rigid.isKinematic = true;
			
	} else
	{
		ph.connectedBody = joints[n-1].rigidbody;	
	}
	// this was just to save the length to debug length change
//	ph.lowAngularXLimit.damper = Vector3.Distance(ph.transform.position,ph.connectedBody.transform.position);

//	Debug.Break();
}
function AddCharacterJointPhysics(n : int)
{
	joints[n] = new GameObject("Joint_" + n);
	joints[n].transform.parent = transform;
	joints[n].transform.rotation = transform.rotation;//added
//joints[n].transform.position = segmentPos[n];
//return;
	var rigid : Rigidbody = joints[n].AddComponent("Rigidbody");
	if(!useMeshCollision) {
		 var col : SphereCollider = joints[n].AddComponent("SphereCollider");
		 col.radius = ropeWidth;
	}

	var ph : CharacterJoint = joints[n].AddComponent("CharacterJoint");
	ph.axis = transform.up; //added
	ph.swingAxis = Vector3.left; //swingAxis;
	ph.lowTwistLimit.limit = lowTwistLimit;
	ph.highTwistLimit.limit = highTwistLimit;
	ph.swing1Limit.limit	= swing1Limit;
	ph.swing2Limit.limit	= swing1Limit; //added

	//ph.breakForce = ropeBreakForce; <--------------- TODO
	joints[n].transform.position = segmentPos[n];
	rigid.drag = ropeDrag;
	rigid.mass = ropeMass;
	rigid.angularDrag = ropeAngularDrag;
//rigid.isKinematic=true;
	//rigid.freezeRotation = 

	if(n==0){			ph.connectedBody = transform.rigidbody;
	} else
	{
		ph.connectedBody = joints[n-1].rigidbody;	
	}

//	Debug.Break();
}
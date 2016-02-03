var speed : float = 40;
var cubeThing : Transform;
var maxRotateSpeed : float = 1.0;
var yDirection : boolean = false;

private var lastMousePosition : Vector3;
private var clickMousePosition : Vector3;
private var direction : float = 1.0;
private var rotateAmt : float;

function Update ()
{

var spinAmount = Time.deltaTime * speed / 20;
transform.Rotate(0,spinAmount,0);

}

/**
 * Aiming rotation update.
 */
// function FixedUpdate () {
	// var horiz : float;
	
	// if (yDirection) {
		// horiz = Input.GetAxis("Vertical");
		// if (horiz > 0) {
			// direction = -1.0;
		// } else if (horiz < 0) {
			// direction = 1.0;
		// }
		// if (cubeThing.rotation.eulerAngles.z > 44  && cubeThing.rotation.eulerAngles.z < 300.0 && direction == 1.0) {
			// return;
		// } else if (cubeThing.rotation.eulerAngles.z > 300.0 && direction == -1.0) {
			// return;
		// }
	// }
	
	// if (yDirection) {
		// if (Input.GetMouseButton(0)) {
			// Rotation via mouse
			// cubeThing.Rotate((Vector3.forward *  rotateAmt));
		// } else {
			// Rotation via arrow keys
			// horiz = Input.GetAxis("Vertical");
			// if (horiz != 0) {
				// cubeThing.Rotate(Vector3(0, 0, -horiz * maxRotateSpeed));
			// }
		// }
	// } else {
		// if (Input.GetMouseButton(0)) {
			// Rotation via mouse
			// cubeThing.Rotate((Vector3.up *  rotateAmt));
		// } else {
			// Rotation via arrow keys
			// horiz = Input.GetAxis("Horizontal");
			// if (horiz != 0) {
				// cubeThing.Rotate(Vector3(0, horiz * maxRotateSpeed, 0));
			// }
		// }
	// }
// }

// /**
 // * Aiming via mouse.
 // */
// function OnMouseDrag() {
	// var curDirection : float = 1.0;
	// var halfScreen : float;
	
	// TODO: cleanup copy pasted code
	// if (yDirection) {
		// if (Input.GetMouseButton(0)) {
			// if (!Input.GetMouseButtonDown(0)) {
				// if (lastMousePosition.y != Input.mousePosition.y) {
					// if (lastMousePosition.y - Input.mousePosition.y < 0.0) {
						// curDirection = -1.0;
					// }

					// if (curDirection != direction) {			
						// if ((lastMousePosition.y - Input.mousePosition.y) < 0.0) {
							// direction = -1.0;
						// } else {
							// direction = 1.0;
						// }
						// clickMousePosition = Input.mousePosition;
					// }
				// }

				// halfScreen = Screen.height / 2.0;
				// rotateAmt = Mathf.Clamp((clickMousePosition.y - Input.mousePosition.y), -halfScreen, halfScreen) / halfScreen;				
				// rotateAmt *= maxRotateSpeed;
			// }
			
			// lastMousePosition = Input.mousePosition;
		// }	
	// } else {
		// if (Input.GetMouseButton(0)) {
			// if (!Input.GetMouseButtonDown(0)) {
				// if (lastMousePosition.x != Input.mousePosition.x) {
					// if (lastMousePosition.x - Input.mousePosition.x < 0.0) {
						// curDirection = -1.0;
					// }

					// if (curDirection != direction) {			
						// if ((lastMousePosition.x - Input.mousePosition.x) < 0.0) {
							// direction = -1.0;
						// } else {
							// direction = 1.0;
						// }
						// clickMousePosition = Input.mousePosition;
					// }
				// }

				// halfScreen = Screen.width / 2.0;
				// rotateAmt = Mathf.Clamp((clickMousePosition.x - Input.mousePosition.x), -halfScreen, halfScreen) / halfScreen;
				// rotateAmt *= maxRotateSpeed;
			// }
			
			// lastMousePosition = Input.mousePosition;
		// }
	// }
// }

// function OnMouseDown() {
	// clickMousePosition = Input.mousePosition;
// }

// function OnMouseUp() {
	// rotateAmt = 0.0;
// }
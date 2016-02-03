#pragma strict

var offColor : Color = Color(0.71, 0.71, 0.71, 0.63);
var onColor : Color = Color(0.0, 0.79, 1.0, 0.63);
var clickColor : Color = Color(1.0, 0.34, 0.34, 1.0);

var clickSpeed : float = 8.0;
var offSpeed : float = 4.0;
var onSpeed : float = 2.0;

var maxScaleSize : float = 1.2;

private var shouldUpdate : boolean;
private var counter : float;
private var mouseDown : boolean = false;
private var counterLimit : float;
private var speed : float;

/**
 * Scales the attached game object to maxScaleSize on mouseOver at speed onSpeed and changes color to clickColor.
 * When the mouse is removed, it scales it back to 1.0 at offspeed and changes color to offColor.  
 * When it is clicked, it scales it to 1.0 at clickspeed and changes color to clickColor.
 */
function Start() {
	renderer.material.color = offColor;
	Reset();
}

function Update() {
	if (shouldUpdate) {
		if (counter < counterLimit) {
			counter += Time.deltaTime * speed;
			if (counter > 3.14) {
				Reset();
			}
		}
		var amt : float = 1.0 + Mathf.Sin(counter) * (maxScaleSize - 1.0);
		transform.localScale = Vector3(amt, amt, amt);
	}
}

function Reset() {
	counter = 0.0;
	speed = onSpeed;
	counterLimit = 1.57;
	shouldUpdate = false;
	transform.localScale = Vector3(1, 1, 1);
}

function OnMouseEnter() {
	if (!mouseDown) {
		shouldUpdate = true;
	}
}

function OnMouseOver() {
	if (!mouseDown) {
		renderer.material.color = onColor;
		shouldUpdate = true;
	} else {
		renderer.material.color = clickColor;
	}
}

function OnMouseDown() {
	renderer.material.color = clickColor;
	mouseDown = true;
	counterLimit = 3.14;
	speed = clickSpeed;
}

function OnMouseUp() {
	renderer.material.color = offColor;
	mouseDown = false;
	Reset();
}

function OnMouseExit() {
	counterLimit = 3.14;
	speed = offSpeed;
	if (!mouseDown) {
		renderer.material.color = offColor;
	}
}
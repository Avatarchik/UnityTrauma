var customSkin : GUISkin;

function OnGUI () {

GUI.skin = customSkin;
	
	GUI.Box (Rect (200,8,472,84), "Nurse Jones says: you are an idiot", "box_light");
	
	GUI.Button (Rect (Screen.width-80,10,24,32), "", "button_chart");
	GUI.Button (Rect (Screen.width-80,42,24,32), "", "button_lab");
	GUI.Button (Rect (Screen.width-52,4,48,96), "", "button_viewToggle");
	
	GUI.Button (Rect (64,32,32,32), "","button_navPadCamHome");
	GUI.Button (Rect (64,0,32,32), "","button_navPadCamUp");
	GUI.Button (Rect (64,64,32,32), "","button_navPadCamDown");
	GUI.Button (Rect (32,32,32,32), "","button_navPadCamLeft");
	GUI.Button (Rect (96,32,32,32), "","button_navPadCamRight");
	
	GUI.Button (Rect (0,0,32,96), "","button_navPadCamLeft");
	GUI.Button (Rect (128,0,32,96), "","button_navPadCamRight");
	
	GUI.TextField (Rect (8,Screen.height-92,Screen.width-105,84), "This is text in the text box");
	GUI.Button (Rect (Screen.width-82,Screen.height-82,64,64), "SUBMIT","box_light");

}


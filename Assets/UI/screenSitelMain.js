var customSkin : GUISkin;

var levelSlot01: String;
var levelButtonName01: String;
var levelSlot02: String;
var levelButtonName02: String;
var levelSlot03: String;
var levelButtonName03: String;
var levelSlot04: String;
var levelButtonName04: String;
var levelSlot05: String;
var levelButtonName05: String;
var levelSlot06: String;
var levelButtonName06: String;
var levelSlot07: String;
var levelButtonName07: String;
var levelSlot08: String;
var levelButtonName08: String;


function OnGUI ()
{

GUI.skin = customSkin;

GUI.Box (Rect(0,0,300,Screen.height),"","box_light");
GUI.Box (Rect(0,0,300,88),"","graphic_logoSitel");


GUILayout.BeginArea (Rect(30,120,270,500));
GUILayout.BeginVertical();

//	if (GUILayout.Button (levelButtonName01, "button_start"))

//	{
	//Application.LoadLevel(levelSlot01);
	//}
	
	if (GUILayout.Button (levelButtonName01, "button_menu"))

	{
	Application.LoadLevel(levelSlot01);
	}
	
	if (GUILayout.Button(levelButtonName02, "button_menu"))
	{
	Application.LoadLevel(levelSlot02);
	}
	
	if (GUILayout.Button(levelButtonName03, "button_menu"))
	{
	Application.LoadLevel(levelSlot03);
	}
	
	if (GUILayout.Button(levelButtonName04, "button_menu"))
	{
	Application.LoadLevel(levelSlot04);
	}

	if (GUILayout.Button(levelButtonName05, "button_menu"))
	{
	Application.LoadLevel(levelSlot05);
	}
	
	if (GUILayout.Button(levelButtonName06, "button_menu"))
	{
	Application.LoadLevel(levelSlot06);
	}
	
	if (GUILayout.Button(levelButtonName07, "button_menu"))
	{
	Application.LoadLevel(levelSlot07);
	}
	
	if (GUILayout.Button(levelButtonName08, "button_menu"))
	{
	Application.LoadLevel(levelSlot08);
	}
	
GUILayout.EndVertical();
GUILayout.EndArea();

}


var customSkin : GUISkin;
var mainTitle : Texture2D;

var levelSlot01: String;
var levelButtonName01: String;
var levelSlot02: String;
var levelButtonName02: String;
var levelSlot03: String;
var levelButtonName03: String;


function OnGUI ()
{

GUI.skin = customSkin;

GUI.Box (Rect(0,0,260,Screen.height),"","box_light");
GUI.Box (Rect(10,Screen.height-73,260,63),"","graphic_logoMedstar");


GUILayout.BeginArea (Rect(10,10,250,500));

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

GUILayout.EndVertical();
GUILayout.EndArea();

//GUI.Box (Rect(0,Screen.height-88,260,88),"",titleImage);
//GUI.Box(Rect(260,0,Screen.width-260,160), mainTitle, "titleMain");

GUI.Box(Rect(260,0,Screen.width-260,160), mainTitle, "titleMain");

}

//GUI.Box (Rect(0,0,300,Screen.height),"","box_light");
using UnityEngine;
using System.Collections;

public class BodyPartTest : MonoBehaviour 
{
	public string GetPart(Color32 partColor)
	{
		string result;
		
		if(CompareColor32(partColor, new Color32(255, 0, 0, 255)))
			result = "Head";
		else if(CompareColor32(partColor, new Color32(255, 0, 255, 255)))
			result = "Torso";
		else if(CompareColor32(partColor, new Color32(170, 0, 0, 255)))
			result = "Right Arm";
		else if(CompareColor32(partColor, new Color32(255, 159, 85, 255)))
			result = "Left Arm";
		else if(CompareColor32(partColor, new Color32(0, 0, 255, 255)))
			result = "Waist";
		else if(CompareColor32(partColor, new Color32(0, 255, 0, 255)))
			result = "Right Leg";
		else if(CompareColor32(partColor, new Color32(255, 255, 0, 255)))
			result = "Left Leg";
		else
			result = "None";
		
		return result;
	}
	
	public bool CompareColor32(Color32 c1, Color32 c2)
	{
		return (c1.r == c2.r && c1.g == c2.g && c1.b == c2.b && c1.a == c2.a);
	}
}
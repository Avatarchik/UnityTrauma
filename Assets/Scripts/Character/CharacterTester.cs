using UnityEngine;
using System.Collections;

public class CharacterTester : MonoBehaviour
{
	public Camera useCamera;
	
	private TextureClick tc;
	private BodyPartTest bpt;
	private Color32 pickedColor;
	private string bodyPart;
	bool clicked = false;
	
	public void Awake()
	{
		tc = gameObject.GetComponent<TextureClick>();
		bpt = gameObject.GetComponent<BodyPartTest>();
	}
	
	public void Update()
	{
		if(Input.GetMouseButtonUp(0))
		{
			pickedColor = tc.GetHitMouse(useCamera);
			bodyPart = bpt.GetPart(pickedColor);
			clicked = true;
		}
	}
	
	public void OnGUI()
	{
		if(clicked && bodyPart != "None") {
			InfoDialogMsg msg = new InfoDialogMsg();
			msg.text = ("You clicked " + bodyPart);
			msg.command = DialogMsg.Cmd.open;
			InfoDialogLoader.GetInstance().PutMessage(msg);
			clicked = false;
		}
		//GUILayout.Label("Color = " + pickedColor);   
		//GUILayout.Label("Part = " + bodyPart);
	}
}

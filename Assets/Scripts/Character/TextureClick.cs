using UnityEngine;
using System.Collections;
using System.IO;

public class TextureClick : MonoBehaviour
{
	public Shader altShader;
	public string tagName = "HitRender";
	
	public Color32 GetHitXY(Camera targetCamera, int x, int y)
	{
		RenderTexture rTex = new RenderTexture(Screen.width, Screen.height, 24);
		Texture2D screen = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		
		RenderTexture oldTex = targetCamera.targetTexture;
		RenderTexture oldActive = RenderTexture.active;
		Color oldBack = targetCamera.backgroundColor;
		
		targetCamera.targetTexture = rTex;
		targetCamera.backgroundColor = Color.black;
		RenderTexture.active = rTex;
		targetCamera.RenderWithShader(altShader, tagName);
		screen.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		
		targetCamera.backgroundColor = oldBack;
		targetCamera.targetTexture = oldTex;
		RenderTexture.active = oldActive;

		Destroy(rTex);
		
		Color32 result = screen.GetPixel(x, y);
		return result;
	}
	
	public Color32 GetHitMouse(Camera targetCamera)
	{
		return GetHitXY(targetCamera, (int)Input.mousePosition.x, (int)Input.mousePosition.y);
	}
}

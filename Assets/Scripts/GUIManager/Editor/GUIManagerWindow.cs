using UnityEditor;
using UnityEngine;

public class GUIManagerWindow : EditorWindow
{
	static GUIManager guiManager;

	static Vector2 lastNative;
	
	// Add menu item named "My Window" to the Window menu
	[MenuItem("Window/GUIManager Window")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		if ( GUIManager.GUIEditWindowSize.x != 0.0f && GUIManager.GUIEditWindowSize.y != 0.0f )
		{
			GetWindow<GUIManagerWindow>().minSize = GUIManager.GUIEditWindowSize;
			GetWindow<GUIManagerWindow>().maxSize = GUIManager.GUIEditWindowSize;
			EditorWindow.GetWindowWithRect(typeof(GUIManagerWindow),new Rect(0,0,GUIManager.GUIEditWindowSize.x,GUIManager.GUIEditWindowSize.y));
		}
		else
			EditorWindow.GetWindow(typeof(GUIManagerWindow));

		// get GUIManager
		if ( guiManager == null )
		{
			GameObject go = GameObject.Find ("GUIManager");
			if ( go != null )
			{
				guiManager = go.GetComponent<GUIManager>() as GUIManager;
				if ( guiManager != null )
				{
					guiManager.Fade = false;
				}
			}
		}
	}

	GUIEditObject guiEditObj;

	void Update()
	{
		Repaint ();
	}

	void OnGUI()
	{
		if ( guiManager != null )
		{
			GUIManager.GetInstance().HandleRegisteredAreas();			
			guiManager.DrawGUI();
		}
	}
}
		

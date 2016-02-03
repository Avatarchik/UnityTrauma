using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public static class PathSaver
{
	static string path=Application.dataPath;

	public static string Path
	{
		get { 
			//UnityEngine.Debug.LogError ("get path=" + path);	
			return path; 
		}
		set { 
			path = System.IO.Path.GetDirectoryName(value);
			//UnityEngine.Debug.LogError ("set path=" + path);	
		}
	}
}

[CustomEditor(typeof(GUIManager))]

public class GUIManagerInspector : Editor
{
	protected bool onSelected = false;
	protected bool showDefaultInspector = false;
	GUIManager myObject = null;
	
	protected string guiName="";
	protected GUIEditObject.GUITypes guiType;
	
	virtual public void OnSelected() // look at a particular instance object		
	{
		myObject = target as GUIManager;
	}

	bool addStatus=true;
	string addName="";

	public virtual void AddScreen()
	{
		if ( GUILayout.Button("Add Screen") )
		{
			if ( guiName != null && guiName != "" )
			{
				AddBlankScreenInfo(guiName);
				guiName = "";
			}
		}
		guiName = EditorGUILayout.TextField("screen name (without .xml)",guiName);
	}
	
	public override void OnInspectorGUI()
	{
		if (!onSelected) 
		{
			onSelected = true;
			OnSelected();  
		}

#if TEST
		if ( Application.isPlaying == false )
		{
			DrawDefaultInspector();
			return;
		}
		else
#endif
		{
			showDefaultInspector = EditorGUILayout.Toggle ("Show Default Inspector",showDefaultInspector);
			if ( showDefaultInspector == true )
				DrawDefaultInspector();
		}
		
		// LOADING EXISTING XML
		
		if ( GUILayout.Button("Load XML") )
		{
			string loadName = EditorUtility.OpenFilePanel("Select file from GUIScripts!!...",PathSaver.Path,"xml");
			if ( loadName != "" && loadName != null )
			{
				// save path for later
				PathSaver.Path = loadName;
				// load the screen, force the type to GUIDialog so we don't try to run the real code
				ScreenInfo si = GUIManager.GetInstance().LoadFromDisk(loadName, "GUIDialog");
				BuildScreenHeirarchy(loadName,si);
			}
		}
		
		// ADD NEW
		
		AddScreen();
		
		// CLEAR ALL SCREENS
		
		if ( GUILayout.Button("Clear GUIManager Screens") )
		{
			if ( GUIManager.GetInstance() )
			{
				GUIManager.GetInstance().RemoveAllScreens();
			}
		}
			
		// INSPECTORS
		
		if ( GUILayout.Button ("<<<< REFRESH ACTIVE SCREEN LIST >>>>"))
			myObject = target as GUIManager;
		
		foreach( ScreenInfo si in myObject.Screens )
		{
			GUILayout.BeginHorizontal();
			if ( GUILayout.Button ("INSPECT <" + si.Screen.name + ">"))
			{
				BuildScreenHeirarchy(si.Screen.name,si,true);			
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.Label (">>>> Set these for fixed screen size editing");
		GUILayout.BeginHorizontal();
		GUILayout.Label ("Edit Window X");
		GUIManager.GUIEditWindowSize.x = EditorGUILayout.FloatField(GUIManager.GUIEditWindowSize.x);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Edit Window Y");
		GUIManager.GUIEditWindowSize.y = EditorGUILayout.FloatField(GUIManager.GUIEditWindowSize.y);
		GUILayout.EndHorizontal();

		if ( GUILayout.Button ("Refresh Window") )
		{
			GUIManagerWindow.GetWindow<GUIManagerWindow>().Close ();
			GUIManagerWindow.ShowWindow();
		}

		//if ( GUILayout.Button ("Build Screen Inspectors...") )
			//BuildScreens ();
	}
	
	public void BuildScreens()
	{
		foreach( ScreenInfo si in myObject.Screens )
		{
			BuildScreenHeirarchy(si.Screen.name,si);
		}
	}
	
	public bool BuildScreenHeirarchy( string pathname, ScreenInfo si, bool makeNewSI=false )
	{
		makeNewSI=false;
		
		if ( si == null || si.Screens.Count == 0 )
			return false;
		
		string name = Path.GetFileNameWithoutExtension(pathname);
		
		GUIEditScreenInfo esi=null;
		
		// make name
		string loadedXML = name + ".xml";
		
		// add game object for this heirarchy
		GameObject newobj = new GameObject(name + ".xml");
		if ( newobj != null )
		{
			// either make a new SI if inspecting, or use the original if loading
			esi = newobj.AddComponent(typeof(GUIEditScreenInfo)) as GUIEditScreenInfo;
			if ( makeNewSI == true )
			{
				ScreenInfo newSI = new ScreenInfo();
				newSI.isModal = false;
				esi.ScreenInfo = newSI;
			} 
			else
			{				
				esi.ScreenInfo = si;
			}
			esi.saveName = loadedXML;
			esi.loadName = pathname;
			// make this a child of the GUIManager
			newobj.transform.parent = myObject.gameObject.transform;
			newobj.transform.localPosition = Vector3.zero;
			// make this selected object
			Selection.activeGameObject = newobj;
		}
		
		// build only first screen (for now)
		if ( si.Screens.Count > 0 )
		{
			foreach( GUIScreen s in si.Screens )
			{
				GUIScreen tmp = s;
				// make sure that this is a base screen if we're making new
				if ( makeNewSI == true )
				{
					GUIScreen newScreen = new GUIScreen();
					newScreen.type = "GUIScreen";
					newScreen.CopyFrom(s);
					newScreen.name = s.name;
					esi.ScreenInfo.AddScreen(newScreen);
					tmp = newScreen;
				}
				// this will recursively build the whole structure
				GameObject go = BuildScreen(tmp,esi);	
				// handle parenting
				go.transform.parent = newobj.transform;
				go.transform.localPosition = Vector3.zero;	
			}
			
			// add new one
			//GUIManager.GetInstance().Add (esi.ScreenInfo);
			
			// we're ok
			return true;
		}
		
		return false;
	}
	
	public virtual GameObject BuildScreen( GUIScreen screen, GUIEditScreenInfo esi )
	{
		GameObject newobj = new GameObject(name);
		if ( newobj != null )
		{
			GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
			eo.guiScreen = screen;
			eo.name = screen.name;			
			eo.LoadedXML = esi.loadName;
			eo.editSI = esi;
			// add all the objects below this
			foreach( GUIObject guiObj in screen.Elements )
				BuildObject(newobj, guiObj);	
			// initialize (sets up all skins/styles)
			//screen.Initialize(esi.ScreenInfo);
		}
		
		return newobj;
	}
	
	public virtual void BuildObject( GameObject parent, GUIObject guiObj )
	{
		GameObject newobj = new GameObject(name);
		if ( newobj != null )
		{
			GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
			// handle parenting
			newobj.transform.parent = parent.transform;
			newobj.transform.localPosition = Vector3.zero;
			// add everything else
			eo.guiObject = guiObj;
			eo.name = guiObj.name;
			eo.Parent = parent.GetComponent<GUIEditObject>();
			eo.Parent.AddChild(newobj);
			eo.ShowElements();
			// order the elements
			eo.BaseName = guiObj.name;
			eo.OrderElements();
			// if this object is a container then add elements
			GUIContainer container = guiObj as GUIContainer;
			if ( container != null )
			{
				foreach( GUIObject go in container.Elements )
					BuildObject(newobj,go);
			}
		}		
	}

	public void AddBlankScreenInfo( string name )
	{		
		// create new SI structure with new screen inside
		ScreenInfo si = new ScreenInfo();
		GUIManager.GetInstance().Add(si);
		GUIScreen newscreen = new GUIScreen();
		newscreen.name = name;
		si.AddScreen(newscreen);
		// build the heirarchy
		BuildScreenHeirarchy(name,si);
	}
	
}


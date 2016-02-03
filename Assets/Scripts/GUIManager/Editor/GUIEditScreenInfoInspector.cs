using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(GUIEditScreenInfo))]

public class GUIEditScreenInfoInspector : Editor
{
	GUIEditScreenInfo myObject;
	
	public GUIEditScreenInfoInspector ()
	{
	}

	virtual public void OnSelected() // look at a particular instance object		
	{
		myObject = target as GUIEditScreenInfo;		
		UnityEngine.Debug.Log("GUIEditScreenInfoInspector.OnSelected() : myObject.saveName=<" + myObject.saveName + ">");
		GUIManager.GetInstance().Screens.Remove(myObject.ScreenInfo);
		GUIManager.GetInstance().Screens.Add(myObject.ScreenInfo);
	}
	
	bool showElementErrors = false;
	
	public override void OnInspectorGUI()
	{
		if ( myObject == null )
			OnSelected();		
		
		if ( myObject.pathName != null && myObject.pathName.Length != 0 && GUILayout.Button("Save <" + myObject.saveName + ">") )
		{
			SaveScreenInfo();
		}
		
		if ( GUILayout.Button("Save As") )
		{
			SaveScreenInfoAs();
		}
		
		if ( GUILayout.Button("Remove from Editor") )
		{
			RemoveEditor();
		}
				
		if ( GUILayout.Button("Add Blank Screen" ) )
		{
			AddBlankScreen();
		}
		
		if ( GUILayout.Button("GUIManager Add") )
		{
			GUIManager.GetInstance().AddImmediate(myObject.ScreenInfo);
		}

		if ( GUILayout.Button("GUIManager Remove") )
		{
			GUIManager.GetInstance().RemoveImmediate(myObject.ScreenInfo);
		}
		
		GUILayout.Space(10);
		
		if ( GUILayout.Button("Fixup Textures/Fonts" ) )
			FixupTexturesFonts();
		
		if ( GUILayout.Button("Analyze") )
			Analyze();
		
		if ( Errors != null && Errors.Count > 0 )
		{
			showElementErrors = EditorGUILayout.Foldout(showElementErrors,"Erros");
			if ( showElementErrors == true )
			{
				GUILayout.BeginVertical();
				foreach( string item in Errors )
					GUILayout.Label (item);
				GUILayout.EndVertical();
			}
		}
		
		GUILayout.Space(10);
		
		if ( GUILayout.Button("Save GUIStyle Info"))
			SaveGUIStyleInfo();
		
		if ( GUILayout.Button("Load GUIStyle Info"))
			LoadGUIStyleInfo();
		
		if ( guiMagic = GUILayout.Toggle(guiMagic,"Do GUI Magic") )
		{
			GUIMagic();
			
#if DEBUG
			foreach( GUIEditScreenInfo.StyleInfo info in myObject.customStyles )
			{
				info.Debug();
			}
#endif
		}
	}
	
	bool guiMagic = false;
	
	public void RemoveEditor()
	{
		GUIManager.GetInstance().RemoveImmediate(myObject.ScreenInfo);
		DestroyImmediate(myObject.gameObject);
	}
	
	public void AddBlankScreen()
	{
		GameObject newobj = new GameObject("blank");
		if ( newobj != null )
		{
			// create new EditObject and name it
			GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
			eo.guiScreen = new GUIScreen();
			eo.guiScreen.name = newobj.name;
			eo.editSI = myObject;
			eo.name = newobj.name;
			// add a component to the ScreenInfo
			newobj.AddComponent<GUIEditObject>();
			myObject.ScreenInfo.AddScreen(eo.guiScreen);
			// parent new obj
			newobj.transform.parent = myObject.gameObject.transform;
			newobj.transform.localPosition = Vector3.zero;
		}
	}

	public void SaveScreenInfo()
	{
		// convert to base GUIScreen
		myObject.ScreenInfo.ConvertToGUIScreen();
		// create info
		myObject.ScreenInfo.Screen.CreateStyleInfo();
		// create new XML
		Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
		serializer.Save(myObject.loadName,myObject.ScreenInfo);				
	}

	string savePath=null;

	public void SaveScreenInfoAs()
	{
		string path = EditorUtility.SaveFilePanel("Enter Save File Name...",PathSaver.Path,myObject.saveName,"xml");
		if ( path == null || path == "" )
			return;
		PathSaver.Path = path;
		// save path
		myObject.loadName = path;
		myObject.pathName = path;
		SaveScreenInfo();
	}

	GUISkin transfer;
	bool saved=false;
	
	public void GUIMagic()
	{
		transfer = (GUISkin)EditorGUILayout.ObjectField("Transfer Skin",transfer,typeof(GUISkin));
		if ( transfer != null && GUILayout.Button("Transfer Skins/Styles") )
		{
			// create new list
			List<GUIEditScreenInfo.StyleInfo> customStyles=new List<GUIEditScreenInfo.StyleInfo>();			
			// add styles from existing skin
			myObject.GUIAddStyles(transfer,customStyles);
			// get new styles
			myObject.GUIGetStyles(customStyles);
			// copy styles to new skin
			int cnt=0;
			transfer.customStyles = new GUIStyle[customStyles.Count];
			foreach( GUIEditScreenInfo.StyleInfo style in customStyles )
			{
				transfer.customStyles[cnt++] = style.style;
			}
			
			// now change all the guiobjects to use the new skin
			myObject.GUIChangeSkin(transfer);
			
			// save the xml
			SaveScreenInfo();
			
			saved = true;
		}
		if ( saved == true )
		{			
			GUILayout.Label("ScreenInfo saved....don't forget to save the skin!");
		}
	}
	
	public void SaveGUIStyleInfo()
	{
		GUISaveGUIStyleInfo( myObject.ScreenInfo.Screen.Skin );
	}
	
	public void GUISaveGUIStyleInfo( GUISkin skin )
	{
		List<GUIStyleInfo> Info = new List<GUIStyleInfo>();
		
		foreach( GUIStyle style in skin.customStyles )
		{
			GUIStyleInfo info = new GUIStyleInfo();
			info.Copy (style);
			if ( info.HasInfo == true )
				Info.Add (info);
		}
		
		// save it
		string path = EditorUtility.SaveFilePanel("Enter GUIStyleInfo Save Name...","./Assets/Resources/GUIScripts/",skin.name+"-StyleInfo","xml");
		if ( path == null || path == "" )
			return;
		
		// create new XML
		Serializer<List<GUIStyleInfo>> serializer = new Serializer<List<GUIStyleInfo>>();
		serializer.Save(path,Info);			
	}
	
	public void LoadGUIStyleInfo()
	{
		GUILoadGUIStyleInfo( myObject.ScreenInfo.Screen.Skin );		
	}
	
	List<string> Errors;
	
	public void FixupTexturesFonts()
	{
		GUILoadGUIStyleInfo( myObject.ScreenInfo.Screen.Skin );				
	}
	
	public void GUILoadGUIStyleInfo( GUISkin skin )
	{
		List<GUIStyleInfo> Info = myObject.ScreenInfo.Screen.styleInfo;
			
		if ( Info == null )
		{
			string loadName = EditorUtility.OpenFilePanel("Select GUIStyleInfo Name...",PathSaver.Path,"xml");
			if ( loadName != "" && loadName != null )
			{
				PathSaver.Path = loadName;
				Serializer<List<GUIStyleInfo>> serializer = new Serializer<List<GUIStyleInfo>>();
				Info = serializer.Load(new StreamReader(loadName));	
			}
		}
		
		if ( Info != null )
		{		
			string elementsPath = EditorUtility.OpenFolderPanel("Select GUI Elements Directory...","./Assets","");
			if ( elementsPath != "" && elementsPath != null )
			{
				int idx = elementsPath.IndexOf("Assets");
				elementsPath = elementsPath.Substring(idx) + "/";
				
				if ( Info != null )
				{
					Errors = new List<string>();
					
					foreach( GUIStyleInfo info in Info )
					{
						info.FixupTextures(skin,elementsPath);
						info.FixupFonts (skin,elementsPath);
						info.Missing(skin,Errors);
					}
				}
			}
		}
		else
			UnityEngine.Debug.LogError("GUILoadGUIStyleInfo() no style info for screen<" + myObject.ScreenInfo.Screen.name + ">, skin<" + skin + ">");
	}
	
	public void Analyze()
	{
		List<GUIStyleInfo> Info = myObject.ScreenInfo.Screen.styleInfo;
		GUISkin skin = myObject.ScreenInfo.Screen.Skin;
		if ( Info == null || skin == null )
			return;
		
		Errors = new List<string>();		
		
		foreach( GUIStyleInfo info in Info )
		{
			info.Missing(skin,Errors);
		}
		
		UnityEngine.Debug.Log ("Analyze Errors: " + Errors.Count);
		showElementErrors = true;
	}
}

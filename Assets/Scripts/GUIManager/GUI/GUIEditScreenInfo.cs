using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

#if UNITY_EDITOR

public class GUIEditScreenInfo : MonoBehaviour
{
	public GUIEditScreenInfo()
	{		
	}

	public string pathName;
	public string saveName;
	public string loadName;
	public ScreenInfo ScreenInfo;
	
	public void ShowInspectorGUI()
	{}
	
	public class StyleInfo
	{
		public StyleInfo( string name, GUISkin skin, GUIStyle style )
		{
			this.name = name;
			this.skin = skin;
			this.style = style;
			this.count = 0;
		}

		public string name;
		public GUISkin skin;
		public GUIStyle style;
		public int count;
		
		public void Debug()
		{
			GUILayout.BeginHorizontal();
			//GUILayout.Button(skin.name);
			GUILayout.Button(style.name);
			//GUILayout.Button(name);
			GUILayout.EndHorizontal();
		}
	}
	
	public List<StyleInfo> GUIAddStyles(GUISkin skin, List<StyleInfo> customStyles )
	{
		foreach (GUIStyle style in skin.customStyles)
		{
			StyleInfo info = new StyleInfo(skin.name,skin,style);
			if ( style != null && HasStyle(customStyles,style.name) == false )
				customStyles.Add(info);
		}
		return customStyles;
	}
	
	public List<StyleInfo> GUIGetStyles(List<StyleInfo> customStyles)
	{
		foreach( GUIScreen screen in ScreenInfo.Screens )
		{
			StyleInfo info = new StyleInfo(screen.name,screen.Skin,screen.Style);
			if ( screen.Style != null && HasStyle(customStyles,screen.Style.name) == false )
				customStyles.Add(info);
		
			foreach( GUIObject element in screen.Elements )
			{
				FindStyles(element,customStyles);
			}
		}
		
		return customStyles;
	}
	
	public bool HasStyle( List<StyleInfo> customStyles, string name )
	{
		foreach( StyleInfo info in customStyles )
		{
			if ( info.style.name == name )
				return true;
		}
		return false;
	}
	
	public void FindStyles(GUIObject guiObject,List<StyleInfo> customStyles)
	{
		if ( guiObject == null )
			return;
		
		GUIContainer container = guiObject as GUIContainer;
		if ( container != null )
		{
			// add style of container
			StyleInfo info = new StyleInfo(guiObject.name,guiObject.Skin,guiObject.Style);
			if ( guiObject.Style != null && HasStyle(customStyles,guiObject.Style.name) == false )
				customStyles.Add(info);
			
			foreach( GUIObject element in container.Elements )
			{
				FindStyles(element,customStyles);
			}
		}
		else
		{
			StyleInfo info = new StyleInfo(guiObject.name,guiObject.Skin,guiObject.Style);
			if ( guiObject.Style != null && HasStyle(customStyles,guiObject.Style.name) == false )
				customStyles.Add(info);
		}
	}

	public void GUIChangeSkin( GUISkin skin )
	{
		foreach( GUIScreen screen in ScreenInfo.Screens )
		{
			screen.SetSkin(skin);
			screen.skin = skin.name;
			
			foreach( GUIObject element in screen.Elements )
			{
				ChangeSkin(element,skin);
			}
		}
	}
	
	public void ChangeSkin(GUIObject guiObject, GUISkin skin)
	{
		if ( guiObject == null )
			return;
		
		GUIContainer container = guiObject as GUIContainer;
		if ( container != null )
		{
			// change skin
			guiObject.SetSkin(skin);
			// change skin name
			guiObject.skin = skin.name;
			// do container
			foreach( GUIObject element in container.Elements )
			{
				ChangeSkin(element,skin);
			}
		}
		else
		{
			// change skin
			guiObject.SetSkin(skin);
			// change skin name
			guiObject.skin = skin.name;
		}
	}

}

#endif

//#define WRITE_POSITION

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;

public class GUIStyleInfo
{
	public string name;
	public string texNormal;
	public string texHover;
	public string texActive;
	public string texOnNormal;
	public string texOnHover;
	public string texOnActive;
	public string font;
	
	bool hasInfo;
	public bool HasInfo
	{
		get { return hasInfo; }
	}
	
#if WRITE_POSITION
	public Vector2 contentOffset;
	public float fixedWidth;
	public float fixedHeight;
#endif
	
	public GUIStyleInfo ()
	{
		hasInfo = false;
	}
	
	public void Copy( GUIStyle style )
	{
		name = style.name;
		
		// copy style texture names
		if (style.normal.background != null)
			texNormal = style.normal.background.name;
		if (style.hover.background != null)
			texHover = style.hover.background.name;
		if (style.active.background != null)
			texActive = style.active.background.name;
		if (style.onNormal.background != null)
			texOnNormal = style.onNormal.background.name;
		if (style.onHover.background != null)
			texOnHover = style.onHover.background.name;
		if (style.onActive.background != null)
			texOnActive = style.onActive.background.name;
		
		// copy fonts
		if (style.font != null)
			font = style.font.name;
		
		if ( texNormal != null || texHover != null || texActive != null ||
			 texOnNormal != null || texOnHover != null || texOnActive != null ||
			 font != null)
			hasInfo = true;
		else 
			hasInfo = false;
		
#if WRITE_POSITION
		contentOffset.x = style.contentOffset.x;
		contentOffset.y = style.contentOffset.y;
		fixedWidth = style.fixedWidth;
		fixedHeight = style.fixedHeight;
		HasInfo = true;
#endif
	}

#if UNITY_EDITOR	
	string pathname="Assets/Scenes/Zoll/Elements/";
	string ext=".png";
	
	Texture2D LoadTexture( string debug, string name )
	{
		if ( name == null )
			return null;
		Texture2D tex = AssetDatabase.LoadAssetAtPath(pathname + name + ext, typeof(Texture2D)) as Texture2D;
		if ( tex == null )
			UnityEngine.Debug.Log("GUIStyleInfo.FixupTextures() : resource not found : " + debug + "<" + pathname + name + ">");
		else		
			UnityEngine.Debug.Log("GUIStyleInfo.FixupTextures() : replacing : " + debug + "<" + pathname + name + ">");
		return tex;
	}
	
	string CheckTexture( string name, Texture2D texture, List<string> errors )
	{
		string outstr=null;
		
		if ( name == null )
			return outstr;
		if ( texture != null && texture.name == name )
			return "ok";
		
		if ( texture == null )
			outstr = "missing<" + name + ">";
		if ( texture != null && texture.name != name )
			outstr = "mismatch<" + name + "> got<" + texture.name + ">"; 
		if ( errors != null )
			errors.Add("style<" + this.name + "> " + outstr);
		return outstr;
	}
	
	string CheckFont( string name, Font font, List<string> errors )
	{
		string outstr=null;
		
		if ( name == null )
			return outstr;
		if ( font != null && font.name == name )
			return "ok";
		
		if ( font == null )
			outstr = "missing<" + name + ">";
		if ( font != null && font.name != name )
			outstr = "mismatch<" + name + "> got<" + font.name + ">"; 
		if ( errors != null )
			errors.Add("style<" + this.name + "> " + outstr);
		return outstr;
	}
	
	public List<string> Missing( GUISkin skin, List<string> errors )
	{
		foreach( GUIStyle style in skin.customStyles )
		{
			if ( style.name == this.name )
			{
				CheckTexture(texNormal,style.normal.background,errors);
				CheckTexture(texHover,style.hover.background,errors);
				CheckTexture(texActive,style.active.background,errors);
				CheckTexture(texOnNormal,style.onNormal.background,errors);
				CheckTexture(texOnActive,style.onActive.background,errors);
				CheckTexture(texOnHover,style.onHover.background,errors);
				CheckFont(font,style.font,errors);
			}
		}
		return errors;
	}
	
	public void FixupTextures(GUISkin skin, string pathname )
	{
		this.pathname = pathname;
		
		foreach( GUIStyle style in skin.customStyles )
		{
			if ( style.name == this.name )
			{
				// check textures
				if ( style.normal.background == null )
					style.normal.background = LoadTexture("normal", texNormal);
				if ( style.hover.background == null )
					style.hover.background = LoadTexture("hover", texHover);
				if ( style.active.background == null )
					style.active.background = LoadTexture("active", texActive);
				
				if ( style.onNormal.background == null )
					style.onNormal.background = LoadTexture("onNormal", texOnNormal);
				if ( style.onHover.background == null )
					style.onHover.background = LoadTexture("onHover", texOnHover);
				if ( style.onActive.background == null )
					style.onActive.background = LoadTexture("onActive", texOnActive);
			}
		}
	}
		
	Font LoadFont( string debug, string name )
	{
		if ( name == null )
			return null;
		Font tex = AssetDatabase.LoadAssetAtPath(pathname + name + ext, typeof(Font)) as Font;
		if ( tex == null )
			UnityEngine.Debug.Log("GUIStyleInfo.FixupTextures() : resource not found : " + debug + "<" + pathname + name + ">");
		else		
			UnityEngine.Debug.Log("GUIStyleInfo.FixupTextures() : replacing : " + debug + "<" + pathname + name + ">");
		return tex;
	}
	
	public void FixupFonts( GUISkin skin, string pathname )
	{
		this.pathname = pathname;
		
		foreach( GUIStyle style in skin.customStyles )
		{
			if ( style.name == this.name )
			{
				if ( style.font == null )
				{
					style.font = LoadFont ("font", font);
				}
			}
		}		
	}
#endif
}


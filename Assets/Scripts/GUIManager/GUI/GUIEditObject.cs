using UnityEngine;
using System;
using System.Collections.Generic;

public class GUIEditObject : MonoBehaviour
{
	public enum GUITypes 
	{
		Area,
		Label,
		Button,
		RepeatButton,
		Toggle,
		Space,
		Horizontal,
		Vertical,
		Scrollview,
		Screen,
		EditBox,
		Box,
		VerticalSlider,
		HorizontalSlider,
		Movie,
	}	

	public GUIStyle Style;	
	
	List<GameObject> children = new List<GameObject>();	
	public GameObject[] Elements;	
	public string LoadedXML="";
	public int OrderIndex=0;

#if UNITY_EDITOR
	public GUIEditScreenInfo editSI;
#endif	

	public DialogMsg.Cmd Command;
	public string Anchor;
	public int Xoffset;
	public int Yoffset;
	public int Width;
	public int Height;
	
	GUIEditObject parent;
	public GUIEditObject Parent
	{
		get { return parent; }
		set { parent = value; }
	}
	
	public GUIEditObject ()
	{
		_guiObject = null;
		_guiScreen = null;
	}
	
	GUIObject _guiObject;
	public GUIObject guiObject
	{
		set { _guiObject = value; }
		get { return _guiObject; }
	}
	
	GUIScreen _guiScreen;
	public GUIScreen guiScreen
	{
		set { _guiScreen = value; }
		get { return _guiScreen; }
	}
	
	public void SetType( string guiName, GUIEditObject.GUITypes guiType )
	{				
		// create based on type
		if ( guiType == GUITypes.Screen )
		{
			guiScreen = new GUIScreen();
			guiScreen.name = guiName;
		}
		if ( guiType == GUITypes.Area )
		{
			guiObject = new GUIArea();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Label )
		{
			guiObject = new GUILabel();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Button )
		{
			guiObject = new GUIButton();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Horizontal )
		{
			guiObject = new GUIHorizontalCommand();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Vertical )
		{
			guiObject = new GUIVerticalCommand();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Scrollview )
		{
			guiObject = new GUIScrollView();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Toggle )
		{
			guiObject = new GUIToggle();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Space )
		{
			guiObject = new GUISpace();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.EditBox )
		{
			guiObject = new GUIEditbox();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Box )
		{
			guiObject = new GUIBox();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.HorizontalSlider )
		{
			guiObject = new GUIHorizontalSlider();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.VerticalSlider )
		{
			guiObject = new GUIVerticalSlider();
			guiObject.name = guiName;
		}
		if ( guiType == GUITypes.Movie )
		{
			guiObject = new GUIMovie();
			guiObject.name = guiName;
		}
	}
	
	public void Copy( GUIEditObject obj )
	{
		// copy entire tree
		if ( obj.guiObject != null )
		{
			guiObject = obj.guiObject.Clone();
		}
	}
	
	public void DuplicateStyle( string name )
	{
		guiObject.DuplicateStyle(name);
	}

	public void AddChild( GameObject go )
	{
		// add regular childrent
		children.Add(go);	
	}
	
	public void DelChild( GameObject go )
	{
		children.Remove(go);
	}
	
	public void MoveToFront()
	{
		if ( Parent == null )
			return;
		
		// order GAMEOBJECTS
		
		// order GUI stuff
		if ( Parent._guiScreen != null )
		{
			// has a screen for parent
			Parent._guiScreen.Elements.Remove(this.guiObject);
			//this.OrderIndex = Parent._guiScreen.Elements.Count;
			Parent._guiScreen.Elements.Add(this.guiObject);
		}
		if ( Parent._guiObject != null )
		{
			// has an object for parent, make sure it is a container
			GUIContainer parent = Parent._guiObject as GUIContainer;
			if ( parent != null )
			{
				// remove it
				parent.Elements.Remove(this.guiObject);
				// add to end
				//this.OrderIndex = parent.Elements.Count;
				parent.Elements.Add(this.guiObject);
			}			
		}
		OrderElements();
	}
	
	public void MoveToBack()
	{
		if ( Parent == null )
			return;
		
		if ( Parent._guiScreen != null )
		{
			// has a screen for parent
			Parent._guiScreen.Elements.Remove(this.guiObject);
			Parent._guiScreen.Elements.Insert(0,this.guiObject);
			//this.OrderIndex = 0;
		}
		if ( Parent._guiObject != null )
		{
			// has an object for parent, make sure it is a container
			GUIContainer parent = Parent._guiObject as GUIContainer;
			if ( parent != null )
			{
				parent.Elements.Remove(this.guiObject);
				parent.Elements.Insert(0,this.guiObject);
				//this.OrderIndex = 0;
			}			
		}
		OrderElements();
	}

	public string BaseName;
	public void MakeName()
	{
		gameObject.name = "(" + OrderIndex + ")" + "<" + guiObject.ToString().Replace("GUI","") + ">" + BaseName;
	}
	
	public void OrderElements()
	{		
		if ( Parent == null )
			return;
		
		// go through every gameobject child and try to find it's member in the child list
		foreach( GameObject go in Parent.children )
		{
			GUIEditObject eo = go.GetComponent<GUIEditObject>();
			if ( eo != null )
			{
				if ( Parent._guiScreen != null )
				{
					// i am a gui screen
					for (int i=0 ; i<Parent._guiScreen.Elements.Count ; i++)
					{
						if ( Parent._guiScreen.Elements[i] == eo._guiObject )
						{
							eo.OrderIndex = i;
							eo.MakeName ();
						}
					}
				}
				if ( Parent._guiObject != null )
				{
					// i am a gui object, i must be a container
					GUIContainer container = Parent._guiObject as GUIContainer;
					if ( container != null )
					{
						for (int i=0 ; i<container.Elements.Count ; i++)
						{
							if ( container.Elements[i] == eo._guiObject )
							{
								eo.OrderIndex = i;
								eo.MakeName ();
							}
						}
					}
				}
			}
		}
	}
	
	public void AddGuiElements( GameObject go )
	{
		// get guiObject to add
		GUIEditObject eo = go.GetComponent<GUIEditObject>() as GUIEditObject;
		if ( eo != null )
		{
			if ( eo._guiObject != null )
			{
				if ( _guiScreen != null )
				{
					// set order index
					eo.OrderIndex = _guiScreen.Elements.Count;
					// add it
					_guiScreen.Elements.Add(eo._guiObject);
				}
				if ( _guiObject != null )
				{
					GUIContainer container = _guiObject as GUIContainer;
					if ( container != null )
					{
						// set order index
						eo.OrderIndex = container.Elements.Count;
						// add it
						container.Elements.Add(eo._guiObject);
					}
				}
			}
			// now go up the parent chain and reinit the screen
			GUIEditObject current = eo;
			while ( current.Parent != null )
				current = current.Parent;
			// we should be at the top, init the screen here
			if ( current._guiScreen != null )
			{
				current._guiScreen.Initialize(current._guiScreen.Parent);
			}
		}
	}
	
	public void ShowElements()
	{	
		Elements = children.ToArray();
	}	
	
	public void ShowStyle()
	{
		if ( _guiScreen != null )
		{
			Style = _guiScreen.Style;
		}
		if ( _guiObject != null )
		{
			Style = _guiObject.Style;
		}
	}
	
	public ScreenInfo LoadScreenInGUIManager()
	{
		if ( _guiScreen != null )
		{
			ScreenInfo si = new ScreenInfo();
			si.AddScreen(_guiScreen);
			// load screen if GUI manager is running
			if ( GUIManager.GetInstance() != null )
			{
				GUIManager.GetInstance().Add(si);
			}
			
			return si;
		}
		return null;
	}
	
	public void RemoveFromGUIManager()
	{
		if ( _guiScreen != null )
		{
			if ( GUIManager.GetInstance() != null )
				GUIManager.GetInstance().RemoveScreen(_guiScreen);
		}
	}
	
	public GUIScreen BuildScreenXML()
	{
		GUIScreen builtScreen=null;
	
		if ( _guiScreen != null )
		{
			builtScreen = _guiScreen;
			if ( Elements != null )
			{
				foreach( GameObject go in Elements )
				{
					// get component
					GUIEditObject eo = go.GetComponent<GUIEditObject>() as GUIEditObject;
					if ( eo != null )
					{
						GUIObject guiObj = eo.BuildXML();
						if ( builtScreen.Elements == null )
							builtScreen.Elements = new List<GUIObject>();
						builtScreen.Elements.Add(guiObj);
					}
				}
			}
			else
				UnityEngine.Debug.Log("GUIEditObject.BuildXML() : Screen has no Elements!");
		}
		
		return builtScreen;
	}
	
	public GUIObject BuildXML()
	{
		if ( _guiObject != null )
		{
			GUIContainer container = _guiObject as GUIContainer;
			if ( container != null )
			{
				// this is a container
				foreach( GameObject go in Elements )
				{
					// get component
					GUIEditObject eo = go.GetComponent<GUIEditObject>() as GUIEditObject;
					if ( eo != null )
					{
						GUIObject guiObj = eo.BuildXML();
						if ( container.Elements == null )
							container.Elements = new List<GUIObject>();
						container.Elements.Add(guiObj);
					}
					// we're done, return the container
					return container;
				}
			}
			else
			{
				// normal object
				return _guiObject;
			}
		}
		// should't get here
		UnityEngine.Debug.Log("GUIEditObject.BuildXML() : Something went wrong!!");
		return null;
	}
}


#define USE_UNITY_PASSWORD
//#define GUIMANAGER_STANDALONE
//#define SHOW_WARNINGS

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;

[System.Serializable]
public abstract class GUIObject
{
    protected GUISkin _skin;
    protected GUIStyle _style;
	protected string _text="";
	protected GUIContent _content;
	protected float scaleX=1.0f,scaleY=1.0f;

    // Loaded data
    public string name = "";
    public string skin;
    public string style;

    public string text 
	{
		set { 
			// don't do anything if text hasn't really changed
			if ( value == _text )
				return;
			_text = value; 
			UpdateContent(); 
		}
		get { return _text; }
	}
	public bool visible;
	
	protected bool GUIDebug;

    protected GUIScreen parentScreen;
    protected GUIContainer container;

    public GUIObject()
    {
		visible = true;
    }

    public GUIObject(GUIObject obj)
    {
        name = obj.name;
        skin = obj.skin;
		_skin = obj._skin;
        style = obj.style;
		_style = obj._style;
        text = obj.text;
		_content = obj._content;
		visible = obj.visible;
    }

    public GUIObject(string name, string skin, string style, string text)
    {
        this.name = name;
        this.skin = skin;
        this.style = style;
        this.text = text;
		this.visible = true;
    }

    public void CopyFrom(GUIObject obj)
    {
        name = obj.name;
        skin = obj.skin;
        _skin = obj._skin;
        style = obj.style;
        _style = obj._style;
        text = obj.text;
        _content = obj._content;
		visible = obj.visible;
		scaleX = obj.scaleX;
		scaleY = obj.scaleY;

        parentScreen = obj.parentScreen;
        container = obj.container;
    }
	
	public virtual GUIObject Clone()
	{
		GUIObject obj = Activator.CreateInstance(this.GetType()) as GUIObject;		
		obj.CopyFrom(this);
		return obj;
	}

	public virtual void OnClose()
	{
	}
	
	public virtual void SetScale( float x, float y )
	{
		scaleX = x;
		scaleY = y;
	}
	
	protected List<string> styles = new List<string>();
	
	protected void GetStyleList()
	{
		// get list of custom styles
		styles.Clear();
		styles.Add("none");
		foreach( GUIStyle s in this._skin.customStyles)
			styles.Add(s.name);
	}
	
	protected GUIStyle FindStyle( string name )
	{
		if ( _skin == null )
			return null;

		// first use skin find for style.  This is really for legacy
		// use when we used a default label for style.
		GUIStyle baseStyle = _skin.FindStyle(name);
		if ( baseStyle != null )
			return baseStyle;

		// couldn't find it, look through custom styles
		foreach( GUIStyle style in _skin.customStyles )
		{
			if ( style.name == name )
				return style;
		}
		return null;
	}
	
	protected int FindStyleIndex( string name )
	{
		GetStyleList();
		for( int i=0 ; i<styles.Count ; i++ )
		{
			if ( styles[i] == name )
				return i;
		}
		return 0;
	}
	
	public virtual void DuplicateStyle( string newName=null )
	{
		// don't do anything if this object doesn't have a style
		if ( _style == null )
			return;
		// make a duplicate of the style
		_style = new GUIStyle(_style);		
		// make name, this is a duplicate name so find the first available
		bool found=false;
		for (int i=1 ; found==false ; i++)
		{
			string dupname = _style.name + "." + i.ToString();
			if ( newName != null )
				dupname = newName;
			if ( FindStyle(dupname) == null )
			{
				_style.name = dupname;
				style = _style.name;
				found = true;
			}
		}		
		// add one to the array and insert at the end
		GUIStyle[] array = new GUIStyle[_skin.customStyles.Length + 1];
		_skin.customStyles.CopyTo(array, 0);
		_skin.customStyles = array;
		_skin.customStyles[_skin.customStyles.Length-1] = _style;		
		// get the style list so the new style shows up!
		GetStyleList();
	}

	public void AddStyle( string name, out GUIStyle newstyle, out int newindex )
	{
		// increase array size
		GUIStyle[] array = new GUIStyle[_skin.customStyles.Length + 1];
        _skin.customStyles.CopyTo(array, 0);
		_skin.customStyles = array;
		
		// create and name new style
		newstyle = new GUIStyle();
		newstyle.name = name;
		
		// add to array
		_skin.customStyles[_skin.customStyles.Length-1] = newstyle;
		
		// return index
		newindex = _skin.customStyles.Length;				
	}
	
	public virtual void Copy(GUIObject obj)
	{
		this.CopyFrom(obj);
	}
	
	public virtual void UpdateContent()
	{
		_content = new GUIContent(_text);
	}
	
    public GUISkin Skin
    {
        get { return _skin; }
    }

    public GUIStyle Style
    {
        get { return _style; }
        //set { _style = value; }
    }

    public void SetSkin(GUISkin skin)
    {
        _skin = skin;
    }

    public void SetStyle(GUIStyle style)
    {
        _style = style;
    }

    public GUIContent Content
    {
        get { return _content; }
    }
	
	public Rect GetRect()
	{
		Rect r = new Rect();
		if ( Style != null )			
		{
			r.x = Style.contentOffset.x;
			r.y = Style.contentOffset.y;
			r.width = Style.fixedWidth;
			r.height = Style.fixedHeight;
		}
		return r;
	}

    public virtual void Process(GUIScreen parentScreen, GUIArea container)
    {
        this.parentScreen = parentScreen;
        this.container = container;

        // Find GUISkin
        if (skin != null && skin.Length > 0)
        {
            GUIManager manager = GUIManager.GetInstance();
            _skin = manager.FindSkin(skin);
            if (_skin == null)
            {
				// try to load the skin
				_skin = manager.LoadSkin(skin);
				// check error
				if ( _skin == null )	
                	Debug.LogError("Could not find Skin - " + skin + " - for GUIScreen " + name + ". Defaulting to default GUI skin.");
            }
            if (_skin != null && style != null)
            {
                // Set style, if exists
                _style = FindStyle(style);

                // Error check
                if (_style == null)
                {
#if DEFAULT_TO_BOX
                    Debug.LogError("Could not find Style - " + style + " - in Skin - " + skin + " for GUIScreen " + name + ". Defaulting to box.");
                    _style = _skin.box;
#endif			
                }
				//Debug.LogError ("GUIObject.Process() : name=" + name + " : skin=" + skin + " : style=" + style + " : _style=" + _style);

#if SHOW_WARNINGS
                if (_style.fixedHeight == 0 && _style.fixedWidth == 0 && !_style.stretchHeight && !_style.stretchWidth)
                    Debug.LogWarning("GUI Button -" + name + "- style has no width or height.");
#endif
            }
#if SHOW_WARNINGS
            else
                Debug.LogWarning("No Style set for " + name + " even though Skin was.");
#endif
        }

        // Set GUIContent text and image
        _content = new GUIContent(text);
    }
	
	protected bool editorShowSkin=true;
	protected bool editorShowStyle=true;
	protected bool editorShowText=true;
	
#if UNITY_EDITOR
	string customStyle="";
	GUISkin lastSkin=null;
	int styleIndex=0;
	bool addStyle=false,lastAddStyle=false;
	string addStyleName="";
	
	public virtual void ShowEditor()
	{
		//GUILayout.Label("GUIObject *********");
		this.name = EditorGUILayout.TextField("name",this.name);
		this.GUIDebug = EditorGUILayout.Toggle("debug",this.GUIDebug);
		this.visible = EditorGUILayout.Toggle ("visible", this.visible);
		
		if ( editorShowSkin == true )
		{
			this._skin = (GUISkin)EditorGUILayout.ObjectField("skin",this._skin,typeof(GUISkin));
			if ( lastSkin != this._skin && this._skin != null)
			{
				if ( this._skin != null )
					this.skin = this._skin.name;	
				lastSkin = this._skin;
				// get list of custom styles
				GetStyleList();
				// set the custom index and style name
				styleIndex = 0;
				if ( this.style == null || this.style == "" )
					// style not set, set to "none"
					customStyle = styles[0];
				else
				{
					// we have a style name, get the index
					customStyle = this.style;
					for (int i=0 ; i<styles.Count ; i++)
					{
						if ( styles[i].ToLower() == customStyle.ToLower() )
							styleIndex = i;
					}				
				}
			}
		}

		if ( editorShowStyle == true )
		{
			if ( this._skin != null && this._skin.customStyles.Length > 0 )
			{
				GetStyleList();
				// display style popup
				styleIndex = EditorGUILayout.Popup("style",styleIndex,styles.ToArray());
				// check index...could happen if user deletes styles
				if ( styleIndex < styles.Count )
					customStyle = styles[styleIndex];
				else
					customStyle = "Index of of Range";
				//UnityEngine.Debug.Log("Selected style=<" + customStyle + "> : index=" + styleIndex);
				this._style = FindStyle(this.customStyle);
				if ( this._style == null )
				{				
					UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find custom style <" + this.customStyle + ">");
					this._style = this._skin.FindStyle(this.customStyle);
					if ( this._style == null )
					{
						UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + this.customStyle + ">");
						styleIndex = 0;
					}
				}
				
				// create add style function
				addStyle = EditorGUILayout.Toggle("Add Style", addStyle);
				if ( addStyle == true )
				{
					if ( lastAddStyle == false )
					{
						// default style name here
						addStyleName = this.name;
					}
					addStyleName = EditorGUILayout.TextField("Style Name",addStyleName);
					if ( addStyleName != null && addStyleName != "" )
					{
						if ( GUILayout.Button("Add Style") )
						{
							// make style
							AddStyle(addStyleName, out _style, out styleIndex);
							
							// test
							GUIStyle test = FindStyle(addStyleName);
							if ( test == null )
								UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style after add <" + addStyleName + ">");
							else
								UnityEngine.Debug.Log("GUIObject.ShowEditor() : found style after add <" + addStyleName + ">");
								
							addStyle = false;
							// get style list
							GetStyleList();
						}
					}
				}
				lastAddStyle = addStyle;
			}
		}
		
		if ( editorShowText == true )
		{
			this.text = EditorGUILayout.TextField("text",this.text);
			if ( this._style != null )
				this.style = this._style.name;
			else
				this.style = "none";
		}
		
		//GUILayout.Label("GUIObject *********");		
	}
#endif
	
	// SCALING
	
	Rect saveStyle;	
	int saveFont;
	bool scaled = false;
	public virtual void Scale()
	{
		// never scale if we don't have to
		if ( scaleX == 1.0f && scaleY == 1.0f )
			return;
		
		// check to see if we're already scaled
		if ( scaled == true )
			return;
		scaled = true;
		
		if ( _style != null )
		{
			// scale the style
			saveStyle.x = _style.contentOffset.x;
			saveStyle.y = _style.contentOffset.y;
			saveStyle.width = _style.fixedWidth;
			saveStyle.height = _style.fixedHeight;
			
			Vector2 save;
			save.x = _style.contentOffset.x * scaleX;
			save.y = _style.contentOffset.y * scaleY;
			_style.contentOffset = save;
			
			_style.fixedWidth *= scaleX;
			_style.fixedHeight *= scaleY;
			
			saveFont = _style.fontSize;
			_style.fontSize = (int)(saveFont * scaleY);
		}
	}
	
	public virtual void Unscale()
	{	
		if ( scaled == false )
			return;
		scaled = false;
		
		if ( _style != null )
		{
			Vector2 save;
			
			save.x = saveStyle.x;
			save.y = saveStyle.y;
			_style.contentOffset = save;
			_style.fixedWidth = saveStyle.width;
			_style.fixedHeight = saveStyle.height;
			
			_style.fontSize = saveFont;
		}
	}
	
	// FLASHING
	bool flash;
	bool flashState=false;
	float flashTime=0.0f;
	public float FlashRate=0.5f;
	Texture2D normal=null;
	Texture2D active=null;
	Texture2D current=null;
	public bool Flash
	{
		set { 
			if ( _style == null )
				return;			
			flash = value; 
			if ( normal != _style.normal.background || active != _style.hover.background )
			{
				normal = _style.normal.background;
				active = _style.hover.background;
			}
			if ( flash == false )
				_style.normal.background = normal;
		}		
		get { return flash; }
	}
	
	protected void HandleFlash()
	{
		if ( _style == null )
			return;		
		if ( flash == false )
			return;
		if ( normal != _style.normal.background || active != _style.hover.background )
		{
			normal = _style.normal.background;
			active = _style.hover.background;
		}
		if ( flashTime < Time.time )
		{
			flashState = (flashState)?false:true;
			if ( flashState == true )
				current = active;
			else
				current = normal;
			flashTime = Time.time + FlashRate;
		}
		if ( current != null )
			_style.normal.background = current;
	}
	
	protected void UnFlash()
	{
		if ( _style == null )
			return;		
		if ( flash == true && normal != null )
			_style.normal.background = normal;
	}
	
	public virtual void RightArrow() {}
	public virtual void LeftArrow() {}
	public virtual void UpArrow() {}
	public virtual void DownArrow() {}

	// FADER STUFF

	GUIFader fader;

	public virtual void Fade( float alpha, float time, GUIFader.Callback callback=null, float forceAlpha=-1 )
	{
		if ( fader == null )
			fader = new GUIFader();
		if ( fader != null )
			fader.Fade(alpha,time,callback,forceAlpha);
	}

	public virtual void FadeIn()
	{
		if ( fader == null )
			return;
		fader.FadeIn();
	}

	public virtual void FadeOut()
	{
		if ( fader == null )
			return;
		fader.FadeOut();
	}
	
	public abstract void Execute();
}

public abstract class GUIContainer : GUIObject
{
    [XmlArray]
    [XmlArrayItem(ElementName = "Area", Type = typeof(GUIArea))]
    [XmlArrayItem(ElementName = "Horizontal", Type = typeof(GUIHorizontalCommand))]
    [XmlArrayItem(ElementName = "Vertical", Type = typeof(GUIVerticalCommand))]
    [XmlArrayItem(ElementName = "Scrollview", Type = typeof(GUIScrollView))]
    [XmlArrayItem(ElementName = "Label", Type = typeof(GUILabel))]
    [XmlArrayItem(ElementName = "Editbox", Type = typeof(GUIEditbox))]
    [XmlArrayItem(ElementName = "Button", Type = typeof(GUIButton))]
    [XmlArrayItem(ElementName = "RepeatButton", Type = typeof(GUIRepeatButton))]
    [XmlArrayItem(ElementName = "Toggle", Type = typeof(GUIToggle))]
    [XmlArrayItem(ElementName = "LoadingBar", Type = typeof(GUILoadingBar))]
    [XmlArrayItem(ElementName = "Movie", Type = typeof(GUIMovie))]
    [XmlArrayItem(ElementName = "Menu", Type = typeof(GUIMenu))]
    [XmlArrayItem(ElementName = "ScrollMenu", Type = typeof(GUIScrollMenu))]
    [XmlArrayItem(ElementName = "Space", Type = typeof(GUISpace))]
	[XmlArrayItem(ElementName = "Space", Type = typeof(GUISpace))]
    [XmlArrayItem(ElementName = "Box", Type = typeof(GUIBox))]
    [XmlArrayItem(ElementName = "HorizontalSlider", Type = typeof(GUIHorizontalSlider))]
    [XmlArrayItem(ElementName = "VerticalSlider", Type = typeof(GUIVerticalSlider))]
#if !GUIMANAGER_STANDALONE
	[XmlArrayItem(ElementName = "Heartbeat", Type = typeof(GUIHeartbeatGraph))]
	[XmlArrayItem(ElementName = "Diastolic", Type = typeof(GUIDiastolicGraph))]
	[XmlArrayItem(ElementName = "Systolic", Type = typeof(GUISystolicGraph))]
	[XmlArrayItem(ElementName = "O2", Type = typeof(GUIO2Graph))]
#endif
#if OLD
	[XmlArrayItem(ElementName = "InteractionButton", Type = typeof(GUIInteractButton))]
	[XmlArrayItem(ElementName = "DrugList", Type = typeof(GUIDrugList))]
	[XmlArrayItem(ElementName = "OrderCart", Type = typeof(GUIOrderCart))]
#endif
	public List<GUIObject> Elements;
	
	public override void RightArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
            if ( Input.GetKey(KeyCode.LeftControl) )
				_style.fixedWidth += increment;
			else	
				_style.contentOffset = new Vector2(_style.contentOffset.x+(int)increment,_style.contentOffset.y);
		}
	}
	public override void LeftArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.fixedWidth -= increment;
			else	
				_style.contentOffset = new Vector2(_style.contentOffset.x-(int)increment,_style.contentOffset.y);
		}		
	}
	public override void UpArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.fixedHeight -= increment;
			else	
				_style.contentOffset = new Vector2(_style.contentOffset.x,_style.contentOffset.y-(int)increment);
		}		
	}
	public override void DownArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.fixedHeight += increment;
			else	
				_style.contentOffset = new Vector2(_style.contentOffset.x,_style.contentOffset.y+(int)increment);
		}				
	}
	
	public GUIContainer() : base()
	{
		Elements = new List<GUIObject>();
	}

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);
        foreach (GUIObject obj in Elements)
        {
            obj.Process(parentScreen, container);
        }
    }

    public GUIObject Find(string name)
    {
        foreach (GUIObject obj in Elements)
        {
       		if (obj.name != null && obj.name.ToLower() == name.ToLower())
                return obj;
       		if (obj.GetType() == typeof(GUIContainer) || obj.GetType().BaseType == typeof(GUIContainer))
       		{
                GUIObject test = (obj as GUIContainer).Find(name);
           		if (test != null)
                   	return test;
			} 
        }
        return null;
    }

    public List<GUIObject> FindObjectsOfType(System.Type type)
    {
        List<GUIObject> objs = new List<GUIObject>();
        foreach (GUIObject obj in Elements)
        {
            System.Type objType = obj.GetType();
            if (objType == type)
                objs.Add(obj);
            if (objType == typeof(GUIContainer) || objType.BaseType == typeof(GUIContainer))
            {
                List<GUIObject> moreObjs = (obj as GUIContainer).FindObjectsOfType(type);
                foreach (GUIObject moreObj in moreObjs)
                    objs.Add(moreObj);
            }
        }
        return objs;
    }
	
	public GUIObject FindObjectOfType(System.Type type)
	{
		List<GUIObject> objs = new List<GUIObject>();
		foreach (GUIObject obj in Elements)
		{
			System.Type objType = obj.GetType();
			if (objType == type)
				return obj;
			if (objType == typeof(GUIContainer) || objType.BaseType == typeof(GUIContainer))
			{
				GUIObject containerObj = (obj as GUIContainer).FindObjectOfType(type);
				if ( containerObj != null )
					return containerObj;
			}
		}
		return null;
	}
	
	public override GUIObject Clone()
	{
		GUIContainer container = base.Clone() as GUIContainer;	
		// copy elements
		container.Elements = new List<GUIObject>();	
		// iterate
		foreach( GUIObject obj in Elements )
		{
			container.Elements.Add(obj.Clone());
		}
		return container;
	}	

	public override void OnClose()
	{
		foreach( GUIObject item in Elements )
			item.OnClose ();
	}
	
	public override void SetScale( float x, float y )
	{
		base.SetScale(x,y);
		foreach( GUIObject obj in Elements )
			obj.SetScale (x,y);
	}
}

public class GUIArea : GUIContainer
{
    protected float x;
    protected float y;
    protected float width;
    protected float height;
	
	public bool fillarea = false;
    public string relative;

	public float rotation;
	public float rotationOffsetX;
	public float rotationOffsetY;
	public bool rotateAroundCenter;
	public bool ignoreMouseOverCheck=false;

	public bool hotspot = false;
	public bool isHotspot = false;
	
	// anchors
	public enum Anchor
	{
		none=-1,
		normal,
		relative,
		centered,
		upper_left,
		lower_left,
		upper_right,
		lower_right,
		upper_middle,
		lower_middle,
		left_middle,
		right_middle,
	}
	public Anchor anchor=Anchor.relative;
	protected float anchorX = 0;
	protected float anchorY = 0;

    protected bool _relative = false;
    protected Rect area;
	protected Rect screenArea;
	
	public void SetArea( Rect area )
	{
		this.area = area;
	}
	
	public Rect GetArea()
	{
		return area;
	}

	public Vector2 GetXY()
	{
		Vector2 pos = new Vector2(0,0);
		// first see if we have container parents
		if ( parentScreen != null )
			pos = parentScreen.GetXY();
		else if ( container != null )
		{
			GUIArea tmp = container as GUIArea;
			pos = tmp.GetXY ();
		}
		
		//if ( Style != null )
		//	pos += Style.contentOffset;
		
		pos.x += area.x;
		pos.y += area.y;
		
		return pos;
	}
	
	public Vector2 GetScreenAreaXY()
	{
		Rect tmp = GetScreenArea();
		return new Vector2(tmp.x,tmp.y);
	}
	
	public Rect GetScreenArea()
	{
		return area;
	}

	public virtual bool MouseOverGUI( Vector2 position, Rect parentArea )
	{
		// get area and offset by parent
		Rect screenArea = GetScreenArea();
		screenArea.x += parentArea.x;
		screenArea.y += parentArea.y;

		if ( ignoreMouseOverCheck == false && screenArea.Contains(position) )
		{
			//UnityEngine.Debug.LogError ("GUIArea.MouseOverGUI() : position=" + position + " : screenArea=" + screenArea);
			return true;
		}

		foreach( GUIObject item in Elements )
		{
			GUIArea area = item as GUIArea;
			if ( area != null )
			{
				if ( area.MouseOverGUI (position,screenArea) == true )
					return true;
			}
		}
		return false;
	}
	
#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIArea --");
		
		base.ShowEditor();
		
		anchor = (Anchor)EditorGUILayout.EnumPopup("anchor",(System.Enum)anchor);

		GUIArea area = this as GUIArea;
		if ( area != null )
		{
			if ( this._style == null )
			{
				area.x = EditorGUILayout.FloatField("x",area.x);
				area.y = EditorGUILayout.FloatField("y",area.y);
				area.width = EditorGUILayout.FloatField("w",area.width);
				area.height = EditorGUILayout.FloatField("h",area.height);			
				area.SetArea(new Rect(area.x,area.y,area.width,area.height));
			}
			else
			{
				GUIStyle style = this._style;
				Vector2 offset = new Vector2(style.contentOffset.x,style.contentOffset.y);
				offset.x = EditorGUILayout.FloatField ("x", offset.x);
				offset.y = EditorGUILayout.FloatField ("y", offset.y);
				style.contentOffset = offset;
				style.fixedWidth = EditorGUILayout.FloatField("w", style.fixedWidth);
				style.fixedHeight = EditorGUILayout.FloatField("h", style.fixedHeight);				
			}
		}

		GUILayout.Space(5);
		rotation = EditorGUILayout.FloatField("rotation",rotation);
		rotationOffsetX = EditorGUILayout.FloatField("rotationOffsetX",rotationOffsetX);
		rotationOffsetY = EditorGUILayout.FloatField("rotationOffsetY",rotationOffsetY);
		rotateAroundCenter = EditorGUILayout.Toggle ("rotate around center", rotateAroundCenter);

		GUILayout.Space (5);
		hotspot = EditorGUILayout.Toggle ("hotspot", hotspot);
		GUILayout.Space (5);

		GUILayout.Label("-- GUIArea --");
	}
#endif
	
	public override GUIObject Clone()
	{
		GUIArea area = base.Clone() as GUIArea;
		area.anchor = this.anchor;
		area.anchorX = this.anchorX;
		area.anchorY = this.anchorY;
		area.area = this.area;
		area.x = this.x;
		area.y = this.y;
		area.width = this.width;
		area.height = this.height;
		area.fillarea = this.fillarea;	// not used
		area.rotation = this.rotation;
		area.rotationOffsetX = this.rotationOffsetX;
		area.rotationOffsetY = this.rotationOffsetY;
		area.rotateAroundCenter = this.rotateAroundCenter;
		area.hotspot = this.hotspot;
		_relative = this._relative;
		return area;
	}
	
	Vector2 lastScreenRes;
	bool screenHasChanged()
	{
		bool changed=false;
		
		if ( lastScreenRes.x != GUIManager.GetInstance().Width )
		{
			changed = true;
			lastScreenRes.x = GUIManager.GetInstance().Width;
		}
		if ( lastScreenRes.y != GUIManager.GetInstance().Height )
		{
			changed = true;
			lastScreenRes.y = GUIManager.GetInstance().Height;
		}
		
		return changed;
	}

	
	public void ResetPosition(GUIScreen parentScreen, GUIArea container)
	{
		if (anchor != Anchor.none )
		{
			// check to see if the area has a style, if so use it otherwise
			// just use the XYWH already defined
			if ( _style != null )
			{
				// XY is from contentOffset, width & height is from fixedWidth/fixedHeight
				x = anchorX = _style.contentOffset.x;
				y = anchorY = _style.contentOffset.y;
				width = _style.fixedWidth;
				height = _style.fixedHeight;
			}
			else
			{
				// no style, use X,Y to set anchor
				anchorX = x;
				anchorY = y;
			}
			
			//if ( screenHasChanged() == false )
			//	return;
			
			// do anchor types
			switch( anchor )
			{
			case Anchor.relative:
				{
				// get parent area
	            Rect tempArea = (container != null) ? container.GetArea() : parentScreen.GetArea();
				if ( width == 0 )
					width = tempArea.width;
				if ( height == 0 )
					height = tempArea.height;
				
				// relative means we are relative to our parent, ignore xy and just use parents, width, height
				area = new Rect(anchorX,anchorY,width,height);
				}
				break;
			case Anchor.normal:
				{
				area = new Rect(anchorX,anchorY,width,height);
				}
				break;
			case Anchor.centered:
				{
				area = new Rect(GUIManager.GetInstance().Width/2-width/2+anchorX,GUIManager.GetInstance().Height/2-height/2+anchorY,width,height);
				}
				break;
			case Anchor.upper_left:
				{
	            area = new Rect(anchorX,anchorY,width,height);						
				}
				break;
			case Anchor.lower_left:
				{
	            area = new Rect(anchorX,GUIManager.GetInstance().Height-height+anchorY,width,height);						
				}
				break;
			case Anchor.upper_right:
				{
	            area = new Rect(GUIManager.GetInstance().Width-width+anchorX,anchorY,width,height);						
				}
				break;
			case Anchor.lower_right:
				{
	            area = new Rect(GUIManager.GetInstance().Width-width+anchorX,GUIManager.GetInstance().Height-height+anchorY,width,height);						
				}
				break;
			case Anchor.upper_middle:
				{
	            area = new Rect(GUIManager.GetInstance().Width/2-width/2+anchorX,anchorY,width,height);						
				}
				break;
			case Anchor.lower_middle:
				{
	            area = new Rect(GUIManager.GetInstance().Width/2-width/2+anchorX,GUIManager.GetInstance().Height-height+anchorY,width,height);						
				}
				break;
			case Anchor.left_middle:
				{
	            area = new Rect(anchorX,GUIManager.GetInstance().Height/2-height/2+anchorY,width,height);						
				}
				break;
			case Anchor.right_middle:
				{
	            area = new Rect(GUIManager.GetInstance().Width-width+anchorX,GUIManager.GetInstance().Height/2-height/2+anchorY,width,height);						
				}
				break;
			}
		}
		else
		{
	        if (_relative)
	        {
	            int trueX, trueY, trueWidth, trueHeight;
				// get parent area
	            Rect tempArea = (container != null) ? container.GetArea() : parentScreen.GetArea();
				
	            trueX = (int)(tempArea.width * x);
	            trueY = (int)(tempArea.height * y);
	            trueWidth = (int)(tempArea.width * width);
	            trueHeight = (int)(tempArea.height * height);
				
	            area = new Rect(trueX, trueY, trueWidth, trueHeight);
	        }
	        else
	            area = new Rect(x, y, width, height);
		}
		//UnityEngine.Debug.LogError("GUIArea.ResetPosition(" + this.name + ") : area=<" + area.x + "," + area.y + "," + area.width + "," + area.height + "> : name=" + name);	
	}
	
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        _relative = relative != null;

		ResetPosition(parentScreen,container as GUIArea);
        
        base.Process(parentScreen, this);
        this.container = container;
    }
	
	// THIS IS FOR AREA EDITING
	
	int controlSize = 15;	
	public virtual void HandleDebug()
	{
		// do generic box if debug is enabled
		if ( GUIDebug == true )
		{
			GUIManager.GetInstance().RegisterArea(this);			
			GUI.skin = null;
			Texture2D image = new Texture2D(1,1,TextureFormat.RGBA32,false);
			Color save = GUI.color;
			int size=controlSize;
			GUI.color = new Color(1,1,1,.5f);
			GUI.DrawTexture(new Rect (area.x,area.y,area.width,area.height),image);
			GUI.color = Color.green;
			GUI.DrawTexture(new Rect (area.x,area.y,size,size),image);
			GUI.DrawTexture(new Rect (area.x+area.width-size,area.y+area.height-size,size,size),image);
			GUI.color = Color.red;
			//GUI.DrawTexture(new Rect (area.x+area.width-size,area.y,size,size),image);
			//GUI.DrawTexture(new Rect (area.x,area.y+area.height-size,size,size),image);
			GUI.color = save;
		}	
		else
			GUIManager.GetInstance().UnRegisterArea(this);
	}
	
	bool dragging=false;
	bool dragXY,dragWH;
	
	Vector2 firstMouse;
	Vector2 deltaMouse;
	Vector2 firstXY;

	public void HandleEditorEdit()
	{
		if ( Event.current == null || Event.current.isMouse == false )
			return;

		Vector2 mousePos = new Vector2(Event.current.mousePosition.x,Event.current.mousePosition.y);	
		
		if ( Event.current.button == 0 && Event.current.type == EventType.MouseDown )
		{
			if ( dragging == false )
			{
				// check to see if we're in XY Area
				Vector2 pos = GetXY ();
				if ( new Rect(pos.x,pos.y,controlSize,controlSize).Contains(mousePos) )
				{
					deltaMouse = firstXY = Style.contentOffset;
					dragXY = true;
				}
				else
					dragXY = false;
				
				// check to see if we're in WH Area
				if ( new Rect(pos.x+area.width-controlSize,pos.y+area.height-controlSize,controlSize,controlSize).Contains(mousePos) )
				{
					firstXY.x = Style.fixedWidth;
					firstXY.y = Style.fixedHeight;
					deltaMouse = firstXY;
					dragWH = true;
				}
				else
					dragWH = false;
				
				// set dragging if true
				if ( dragXY == true || dragWH == true )
				{					
					dragging = true;
					firstMouse = mousePos;		
				}
			}
		}
		if ( Event.current.type == EventType.MouseDrag )
		{
			deltaMouse = firstXY + (mousePos-firstMouse);
			deltaMouse.x = (float)((int)deltaMouse.x);
			deltaMouse.y = (float)((int)deltaMouse.y);
			//if ( deltaMouse.x < 0 ) deltaMouse.x = 0;
			//if ( deltaMouse.y < 0 ) deltaMouse.y = 0;
		}
		
		if ( Event.current.type == EventType.MouseUp )
		{
			dragging = false;
			dragXY = false;
			dragWH = false;
		}
		
		if ( dragXY == true )
		{
			Style.contentOffset = deltaMouse;
		}
		
		if ( dragWH == true )
		{
			Style.fixedWidth = deltaMouse.x;
			Style.fixedHeight = deltaMouse.y;
		}
	}
	
	public void HandleEdit()
	{
		if ( Input.GetMouseButton(0) )
		{
			Vector2 mousePos = new Vector2(Input.mousePosition.x,GUIManager.GetInstance().Height-Input.mousePosition.y);	
			
			if ( dragging == false )
			{
				// check to see if we're in XY Area
				Vector2 pos = GetScreenAreaXY ();
				if ( new Rect(pos.x,pos.y,controlSize,controlSize).Contains(mousePos) )
				{
					deltaMouse = firstXY = Style.contentOffset;
					dragXY = true;
				}
				else
					dragXY = false;
				
				// check to see if we're in WH Area
				if ( new Rect(pos.x+area.width-controlSize,pos.y+area.height-controlSize,controlSize,controlSize).Contains(mousePos) )
				{
					firstXY.x = Style.fixedWidth;
					firstXY.y = Style.fixedHeight;
					deltaMouse = firstXY;
					dragWH = true;
				}
				else
					dragWH = false;
				
				// set dragging if true
				if ( dragXY == true || dragWH == true )
				{					
					dragging = true;
					firstMouse = mousePos;		
				}
			}
			else
			{
				deltaMouse = firstXY + (mousePos-firstMouse);
			}
		}
		else
		{
			dragging = false;
			dragXY = false;
			dragWH = false;
		}
		
		if ( dragXY == true )
		{
			Style.contentOffset = deltaMouse;
		}
		
		if ( dragWH == true )
		{
			Style.fixedWidth = deltaMouse.x;
			Style.fixedHeight = deltaMouse.y;
		}
	}

	float rotCoordX=0.0f;
	float rotCoordY=0.0f;

	void Rotate()
	{
		// check to see if we have rotation
		if ( _style != null && rotation != 0.0f )
		{
			// this gets real screen coord
			Vector2 screenPos = new Vector2(area.x,area.y);
			// rotate about center case
			if ( rotateAroundCenter == true )
			{
				rotationOffsetX = area.width/2.0f;
				rotationOffsetY = area.height/2.0f;
			}
			// make coordinate
			Vector2 tmp = GUIUtility.GUIToScreenPoint(new Vector2(screenPos.x + rotationOffsetX,screenPos.y + rotationOffsetY));
			rotCoordX = tmp.x;
			rotCoordY = tmp.y;
			// rotate
			GUIUtility.RotateAroundPivot (rotation,new Vector2(rotCoordX,rotCoordY));
		}        
	}

	void UnRotate()
	{
		// debug
		if ( rotation != 0.0f && GUIDebug == true )
		{
			// draw blue debug box
			GUI.skin = null;
			Texture2D image = new Texture2D(1,1,TextureFormat.RGBA32,false);
			Color save = GUI.color;
			int size=10;
			GUI.color = new Color(0,0,1,1);
			GUI.DrawTexture(new Rect(rotCoordX-5,rotCoordY-5,10,10),image);
			GUI.color = save;
		}

		// set back rotation pivot if we're using it
		if ( rotation != 0.0f )
			GUIUtility.RotateAroundPivot(-rotation,new Vector2(rotCoordX,rotCoordY));
		
	}
	
	// NORMAL EXECUTE
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;

		FadeIn ();
		Scale ();
		ResetPosition (parentScreen,container as GUIArea);
		Rotate ();

		// check/set tooltip
		GUIManager.GetInstance().CheckHotspot(this);

		// make sure default skin is set
		GUI.skin = _skin;

		if (_skin != null && _style != null)
           	GUILayout.BeginArea(area, _content, _style);
       	else
            GUILayout.BeginArea(area, _content);

		foreach (GUIObject obj in Elements)
		{
            obj.Execute();
		}

		GUILayout.EndArea();

		HandleDebug();
		UnRotate ();
		Unscale();
		FadeOut ();
    }
	
	public virtual GUIButton CheckButtons( Rect parentArea, Vector2 position )
	{
		foreach( GUIObject obj in Elements )
		{
			GUIButton button = obj as GUIButton;
			if ( button != null )
			{
				// get this area coord
				Rect clientArea = GetArea();
				clientArea.x += parentArea.x;
				clientArea.y += parentArea.y;
				// add the button margin
				clientArea.x += button.Style.margin.left;
				clientArea.y += button.Style.margin.top;
				// set width/height
				clientArea.width = button.Style.fixedWidth;
				clientArea.height = button.Style.fixedHeight;
		
				if ( clientArea.Contains(position) )
				{
					UnityEngine.Debug.Log ("GUIArea.CheckButtons() : Area=<" + clientArea.x + "," + clientArea.y + "," + clientArea.width + "," + clientArea.height + ">");
					return button;
				}
			}
			
			GUIArea area = obj as GUIArea;
			if ( area != null )
			{
				GUIButton temp = area.CheckButtons(parentArea,position);
				if ( temp != null )
					return temp;
			}
		}
		return null;
	}
}


public class GUIHorizontalCommand : GUIContainer
{
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;

		FadeIn ();
		Scale ();
		
        if (_skin != null && _style != null)
		{
			GUI.skin = _skin;			
            GUILayout.BeginHorizontal(_content, _style);
		} else
            GUILayout.BeginHorizontal();
        foreach (GUIObject obj in Elements)
            obj.Execute();
        GUILayout.EndHorizontal();
		
		Unscale();
		FadeOut ();
    }	
}

public class GUIVerticalCommand : GUIContainer
{
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		FadeIn ();
		Scale ();
        if (_skin != null && _style != null)
		{
			GUI.skin = _skin;			
            GUILayout.BeginVertical(_content, _style);
		} else
            GUILayout.BeginVertical();
        foreach (GUIObject obj in Elements)
            obj.Execute();
        GUILayout.EndVertical();
		Unscale();
		FadeOut ();
    }
}

public class GUIScrollView : GUIContainer
{
    public bool displayHorizontal = false;
    public bool displayVertical = false;
    public string hStyle;
    public string vStyle;
    public Vector2 scroll = new Vector2(0, 0);
	public bool touchScrollX = false;
	public bool touchScrollY = false;
	public float touchScaleX = 1.0f; 
	public float touchScaleY = 1.0f;

    protected GUIStyle _hStyle;
    protected GUIStyle _vStyle;

    public GUIStyle HStyle
    {
        get { return _hStyle; }
    }
    public GUIStyle VStyle
    {
        get { return _vStyle; }
    }

	GUISkin lastSkin=null;
	
#if UNITY_EDITOR
	int HstyleIndex=0,VstyleIndex=0;
	
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIScrollView --");
		
		base.ShowEditor();

		displayHorizontal = (bool)EditorGUILayout.Toggle("displayHorizontal",displayHorizontal);
		displayVertical = (bool)EditorGUILayout.Toggle("displayVertical",displayVertical);
		
		if ( this._skin != null )
		{
			// first time get the style index
			if ( lastSkin != this._skin )
			{
				HstyleIndex = FindStyleIndex(hStyle);
				VstyleIndex = FindStyleIndex(vStyle);
				lastSkin = this._skin;
			}	
			
			HstyleIndex = EditorGUILayout.Popup("Hstyle",HstyleIndex,styles.ToArray());
			hStyle = styles[HstyleIndex];
			if ( hStyle == "none" )
			{
			}
			UnityEngine.Debug.Log("Selected style=<" + HStyle + "> : index=" + HstyleIndex);
			this._hStyle = FindStyle(hStyle);
			if ( this._hStyle == null )
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + HStyle + ">");

			VstyleIndex = EditorGUILayout.Popup("Vstyle",VstyleIndex,styles.ToArray());
			vStyle = styles[VstyleIndex];
			UnityEngine.Debug.Log("Selected style=<" + VStyle + "> : index=" + VstyleIndex);
			this._vStyle = FindStyle(vStyle);
			if ( this._vStyle == null )
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + VStyle + ">");
		}
			
		GUILayout.Label("-- GUIScrollView --");
	}
#endif
	
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        if (_skin != null)
        {
            if (hStyle != null)
            {
                _hStyle = _skin.FindStyle(hStyle);

#if SHOW_WARNINGS
				if (_hStyle == null)
                    Debug.LogError("Could not find hStyle - " + hStyle + " - in Skin - " + skin + " for GUI object " + name + ". Defaulting.");
#endif
			}

            if (vStyle != null)
            {
                _vStyle = _skin.FindStyle(vStyle);

#if SHOW_WARNINGS
                if (_hStyle == null)
                    Debug.LogError("Could not find vStyle - " + vStyle + " - in Skin - " + skin + " for GUI object " + name + ". Defaulting.");
#endif			
            }
        }
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;

		FadeIn ();
		Scale ();

		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) 
		{
			Vector2 delta = new Vector2();
			if ( touchScrollX == true )
				delta.x += Input.GetTouch(0).deltaPosition.x * touchScaleX;
			if ( touchScrollY == true )
				delta.y += Input.GetTouch(0).deltaPosition.y * touchScaleY;
			scroll += delta;
		}

        if (_skin != null)
        {
			GUI.skin = _skin;
				
            if (_vStyle != null && _hStyle != null)
            {
                if (_style != null)
                    scroll = GUILayout.BeginScrollView(scroll, displayHorizontal, displayVertical, _hStyle, _vStyle, _style);
                else
                    scroll = GUILayout.BeginScrollView(scroll, displayHorizontal, displayVertical, _hStyle, _vStyle);

                foreach (GUIObject obj in Elements)
                    obj.Execute();
                GUILayout.EndScrollView();
            }
			else
			{
                scroll = GUILayout.BeginScrollView(scroll, displayHorizontal, displayVertical);

				foreach (GUIObject obj in Elements)
                    obj.Execute();
                GUILayout.EndScrollView();
			}
        }
		
		Unscale ();
		FadeOut ();
    }
}

public class GUISpace : GUIObject
{
    public int pixels;
	
	public GUISpace() : base()
	{}
	
	public GUISpace(int space) : base()
	{
		pixels = space;
	}

	public override GUIObject Clone()
	{
		GUISpace space = base.Clone() as GUISpace;
		space.pixels = this.pixels;
		return space;
	}
	
#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUISpace --");
		
		base.ShowEditor();

		pixels = (int)EditorGUILayout.IntField("pixels",pixels);
			
		GUILayout.Label("-- GUISpace --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        if (pixels >= 0)
            GUILayout.Space(pixels);
        else
            GUILayout.FlexibleSpace();
    }	
}

public class GUIBox : GUIObject
{
	public override GUIObject Clone()
	{
		GUIBox box = base.Clone() as GUIBox;
		return box;
	}
	
#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIBox --");
		
		base.ShowEditor();

		GUILayout.Label("-- GUIBox --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		Scale ();
        if (_skin != null && _style != null)
            GUILayout.Box(_content, _style);
        else
            GUILayout.Box(_content);
		Unscale ();
    }	
}

public class GUIHorizontalSlider : GUIObject
{
	public float value;
	public float leftValue;
	public float rightValue;
	public string slider;
	public string thumb;
	protected GUIStyle _slider;
	protected GUIStyle _thumb;
	
	public GUIHorizontalSlider() : base()
	{
		value = 0;
		leftValue = 0;
		rightValue = 100;
		editorShowStyle = false;
		editorShowText = false;
	}
	
	public override GUIObject Clone()
	{
		GUIHorizontalSlider slider = base.Clone() as GUIHorizontalSlider;
		slider.value = this.value;
		slider.leftValue = this.leftValue;
		slider.rightValue = this.rightValue;
		slider.slider = this.slider;
		slider.thumb = this.thumb;
		slider._slider = this._slider;
		slider._thumb = this._thumb;
		return slider;
	}
	
#if UNITY_EDITOR
	
	int thumbIndex=0;
	int sliderIndex=0;
	
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIHorizontalSlider --");
		
		base.ShowEditor();
		
		value = EditorGUILayout.FloatField("value",value);
		leftValue = EditorGUILayout.FloatField("minValue",leftValue);
		rightValue = EditorGUILayout.FloatField("maxValue",rightValue);

		// slider
		if ( this._skin != null && this._skin.customStyles.Length > 0 )
		{
			// display style popup
			sliderIndex = EditorGUILayout.Popup("slider style",sliderIndex,styles.ToArray());
			slider = styles[sliderIndex];
			UnityEngine.Debug.Log("Selected style=<" + slider + "> : index=" + sliderIndex);
			this._slider = FindStyle(this.slider);
			if ( this._slider == null )
			{				
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find custom style <" + this.slider + ">");
				this._slider = this._skin.FindStyle(this.slider);
				if ( this._style == null )
				{
					UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + this.slider + ">");
					sliderIndex = 0;
				}
			}
		}	
		// slider
		if ( this._skin != null && this._skin.customStyles.Length > 0 )
		{
			// display style popup
			thumbIndex = EditorGUILayout.Popup("thumb style",thumbIndex,styles.ToArray());
			thumb = styles[thumbIndex];
			UnityEngine.Debug.Log("Selected style=<" + thumb + "> : index=" + thumbIndex);
			this._thumb = FindStyle(this.thumb);
			if ( this._thumb == null )
			{				
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find custom style <" + this.thumb + ">");
				this._thumb = this._skin.FindStyle(this.thumb);
				if ( this._style == null )
				{
					UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + this.thumb + ">");
					thumbIndex = 0;
				}
			}
		}	
		
		if ( _slider == null && _thumb == null && GUILayout.Button("Add Slider & Thumb Styles") )
		{
			AddStyle(this.name + ".slider",out _slider,out sliderIndex);
			AddStyle(this.name + ".thumb", out _thumb, out thumbIndex);
			GetStyleList();
		}
		
		GUILayout.Label("-- GUIHorizontalSlider --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
      	value = GUILayout.HorizontalSlider(value,leftValue,rightValue,_slider,_thumb);
    }	
}

public class GUIVerticalSlider : GUIObject
{
	public float value;
	public float leftValue;
	public float rightValue;
	public string slider;
	public string thumb;
	protected GUIStyle _slider;
	protected GUIStyle _thumb;
	
	public GUIVerticalSlider() : base()
	{
		value = 0;
		leftValue = 0;
		rightValue = 100;
		editorShowStyle = false;
		editorShowText = false;
	}
	
	public override GUIObject Clone()
	{
		GUIVerticalSlider slider = base.Clone() as GUIVerticalSlider;
		slider.value = this.value;
		slider.leftValue = this.leftValue;
		slider.rightValue = this.rightValue;
		slider.slider = this.slider;
		slider.thumb = this.thumb;
		slider._slider = this._slider;
		slider._thumb = this._thumb;
		return slider;
	}
	
#if UNITY_EDITOR
	
	int thumbIndex=0;
	int sliderIndex=0;
	
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIVerticalSlider --");
		
		base.ShowEditor();
		
		value = EditorGUILayout.FloatField("value",value);
		leftValue = EditorGUILayout.FloatField("minValue",leftValue);
		rightValue = EditorGUILayout.FloatField("maxValue",rightValue);

		// slider
		if ( this._skin != null && this._skin.customStyles.Length > 0 )
		{
			// display style popup
			sliderIndex = EditorGUILayout.Popup("slider style",sliderIndex,styles.ToArray());
			slider = styles[sliderIndex];
			UnityEngine.Debug.Log("Selected style=<" + slider + "> : index=" + sliderIndex);
			this._slider = FindStyle(this.slider);
			if ( this._slider == null )
			{				
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find custom style <" + this.slider + ">");
				this._slider = this._skin.FindStyle(this.slider);
				if ( this._style == null )
				{
					UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + this.slider + ">");
					sliderIndex = 0;
				}
			}
		}	
		// slider
		if ( this._skin != null && this._skin.customStyles.Length > 0 )
		{
			// display style popup
			thumbIndex = EditorGUILayout.Popup("thumb style",thumbIndex,styles.ToArray());
			thumb = styles[thumbIndex];
			UnityEngine.Debug.Log("Selected style=<" + thumb + "> : index=" + thumbIndex);
			this._thumb = FindStyle(this.thumb);
			if ( this._thumb == null )
			{				
				UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find custom style <" + this.thumb + ">");
				this._thumb = this._skin.FindStyle(this.thumb);
				if ( this._style == null )
				{
					UnityEngine.Debug.Log("GUIObject.ShowEditor() : can't find style <" + this.thumb + ">");
					thumbIndex = 0;
				}
			}
		}	
		
		if ( _slider == null && _thumb == null && GUILayout.Button("Add Slider & Thumb Styles") )
		{
			AddStyle(this.name + ".slider",out _slider,out sliderIndex);
			AddStyle(this.name + ".thumb", out _thumb, out thumbIndex);
			GetStyleList();
		}
		
		GUILayout.Label("-- GUIVerticalSlider --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
      	value = GUILayout.VerticalSlider(value,leftValue,rightValue,_slider,_thumb);
    }	
}


public class GUILabel : GUIObject
{
	bool editMode = false;

    public override void Execute()
    {
		Scale ();
		
		// handle visibility
		if ( visible == false )
			return;
		
		if ( _content == null )
			_content = new GUIContent(text);
		
        _content.text = text;

		if ( editMode == false )
		{
	        if (_skin != null && _style != null)
	            GUILayout.Label(_content, _style);
	        else
	            GUILayout.Label(_content);
		}
		else
		{
			if (_skin != null && _style != null)
				text = GUILayout.TextField(text,_style);
			else
				text = GUILayout.TextField(text);
		}
		
		Unscale ();
    }

	public override void RightArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
            if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right += (int)increment;
			else	
				_style.margin.left += (int)increment;
		}
	}
	public override void LeftArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right -= (int)increment;
			else	
				_style.margin.left -= (int)increment;
		}		
	}
	public override void UpArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right -= (int)increment;
			else	
				_style.margin.top -= (int)increment;
		}		
	}
	public override void DownArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right += (int)increment;
			else	
				_style.margin.top += (int)increment;
		}				
	}
	
#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUILabel --");
		
		base.ShowEditor();
		
		GUILayout.Label("-- GUILabel --");
	}
#endif
	
}

public class GUIEditbox : GUIObject
{
	public bool Password=false;
	public int PasswordPadding=0;

	string hiddenPassword="";
	public string GetPassword()
	{
		return hiddenPassword;
	}

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
		Scale ();		
        GUI.SetNextControlName(name);
#if USE_UNITY_PASSWORD
		if ( Password == true )
		{
	        if (_skin != null && _style != null)
			{
				// I put this extra space in here because Unity
				// has a problem with style margin
				GUILayout.BeginHorizontal();
				GUILayout.Space (PasswordPadding);
				text = GUILayout.PasswordField(text, '*', _style);
				GUILayout.EndHorizontal();
			}
        	else
	            text = GUILayout.PasswordField(text, '*');
		} 
		else
		{
	        if (_skin != null && _style != null)
            	text = GUILayout.TextField(text, _style);
        	else
	            text = GUILayout.TextField(text);
		}
#else
		// NOTE!! THIS DOESN'T WORK YET BECAUSE IT DOESN'T HANDLE
		// BACKSPACE AND SELECT DELETE!
		if ( Password == true )
		{
			// get text field
			if (_skin != null && _style != null)
				text = GUILayout.TextField(text, _style);
			else
				text = GUILayout.TextField(text);
			// check each character in text to see if we have a new character
			for( int i=0 ; i<text.Length ;i++)
			{
				if ( text[i] != '*' )
				{
					hiddenPassword += text[i];
					StringBuilder sb = new StringBuilder(text);
					sb[i] = '*';
					text = sb.ToString();		
					UnityEngine.Debug.LogError ("hiddenPassword=" + hiddenPassword);
				}
			}
		}
		else
		{
			if (_skin != null && _style != null)
				text = GUILayout.TextField(text, _style);
			else
				text = GUILayout.TextField(text);
		}
#endif

		Unscale();
        //GUI.FocusControl("GUIEditBox");
	}

#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIEditBox --");
		
		base.ShowEditor();

		// toggle password
		Password = GUILayout.Toggle (Password,"Password");
		if ( Password == true )
			PasswordPadding = EditorGUILayout.IntField("Password Margin (to fix Unity bug)",PasswordPadding);
		
		GUILayout.Label("-- GUIEditBox --");
	}
#endif
}

public class GUIButton : GUIObject
{
    public string message;
    public string screenTarget;
    public string guiTarget;
    public string sceneTarget;
    public string parameter;
    public string modal;
	public string interactMsg;
	public string interactStatusMsg;
	public string trigger;  // this ISM will cause button to depress while dialog is up
	public bool closeWindow;
	public bool log;
	public string tooltip;

    public GUIButton() : base()
    {
		log = false;
    }

    public GUIButton(GUIButton obj)
        : base(obj)
    {
    }

#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIButton --");
		
		base.ShowEditor();
		
		screenTarget = EditorGUILayout.TextField("screenTarget",screenTarget);
		sceneTarget = EditorGUILayout.TextField("sceneTarget",sceneTarget);
		guiTarget = EditorGUILayout.TextField("guiTarget",guiTarget);
		parameter = EditorGUILayout.TextField("parameter",parameter);
		modal = EditorGUILayout.TextField("modal",modal);
		closeWindow = EditorGUILayout.Toggle("closeWindow",closeWindow);
		interactMsg = EditorGUILayout.TextField("interactMsg",interactMsg);
		interactStatusMsg = EditorGUILayout.TextField("interactStatusMsg",interactStatusMsg);
		trigger = EditorGUILayout.TextField("trigger",trigger);

		GUILayout.Label("-- GUIButton --");
	}
#endif
	
	public override GUIObject Clone()
	{
		GUIButton button = base.Clone() as GUIButton;
		button.message = this.message;
		button.sceneTarget = this.sceneTarget;
		button.screenTarget = this.screenTarget;
		button.guiTarget = this.guiTarget;
		button.parameter = this.parameter;
		button.modal = this.modal;
		button.closeWindow = this.closeWindow;
		button.interactMsg = this.interactMsg;
		button.interactStatusMsg = this.interactStatusMsg;
		button.trigger = this.trigger;
		button.log = this.log;
		return button;
	}
	
	public virtual void Copy( GUIButton button )
	{
		base.Copy(button);
		this.message = button.message;
		this.sceneTarget = button.sceneTarget;
		this.screenTarget = button.screenTarget;
		this.guiTarget = button.guiTarget;
		this.sceneTarget = button.sceneTarget;
		this.sceneTarget = button.sceneTarget;
		this.parameter = button.parameter;
		this.modal = button.modal;
		this.closeWindow = button.closeWindow;
		this.interactMsg = button.interactMsg;
		this.interactStatusMsg = button.interactStatusMsg;
		this.trigger = button.trigger;
		this.log = button.log;
	}
	
	public override void Process(GUIScreen parentScreen, GUIArea container)
    {
		base.Process(parentScreen,container);

#if !GUIMANAGER_STANDALONE
		// if this button has a trigger ineractStatusMessage, add our Callback to the GUIManager
		// it's indexed by our GUIScreen parent so the callback can be removed when the screen is closed.	
		if (trigger != null && trigger != "")
			GUIManager.GetInstance().AddCallback(myInteractCallback);
#endif
	}

#if !GUIMANAGER_STANDALONE

	public override void OnClose()
	{
		if (trigger != null && trigger != "")
			GUIManager.GetInstance().RemoveCallback(myInteractCallback);
	}

	public bool myInteractCallback(GameMsg msg)
	{
		InteractStatusMsg ism = msg as InteractStatusMsg;
		if (ism != null){
			if (ism.InteractName == trigger){
				// act like we got pressed.
				ButtonAction();
				return true;
			}
		}
		return false;
	}

#endif

    public delegate void ButtonCallback(GUIScreen screen, GUIButton button, string keyvaluepairs);
    protected ButtonCallback Callback;

    public void AddCallback(ButtonCallback callback)
    {
        Callback += callback;
    }
	
	public void SetCallback(ButtonCallback callback)
	{
		Callback = callback;
	}

    protected List<string> Messages;
    public void AddMessage(string message)
    {
        if (Messages == null)
            Messages = new List<string>();
        Messages.Add(message);
    }

    public void SendMessages()
    {
		GUIDialog dialog = this.parentScreen as GUIDialog;
        if (Messages != null)
		{
	        foreach (string msg in Messages)
	        {
				// call parent dialog callback with this msg...
				if ( dialog != null )
				{
					// add pressed=name
					if ( dialog.DialogCallback != null )
						dialog.DialogCallback(msg + " pressed=" + this.name);
				}
	        }
		}
    }
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
		Scale ();
		
		HandleFlash();
		
		if ( _content == null )
			_content = new GUIContent(text);
		
        _content.text = text;
		_content.tooltip = tooltip;

        if (_style != null)
        {
            if (GUILayout.Button(_content, _style))
            {
                ButtonAction();
            }
        }
        else
        {
            if (GUILayout.Button(_content))
            {
                ButtonAction();
            }
        }
		
		UnFlash();
		Unscale();
    }

	public virtual void SimulateButtonAction()
	{
		ButtonAction ();
	}

    protected virtual void ButtonAction()
    {
#if !GUIMANAGER_STANDALONE
		// log this button event if enabled
		if ( log == true )
			LogMgr.GetInstance().Add (new DialogButtonItem(parentScreen.name,this.name));
#endif

        // call delegate (this is old functionality, may not be needed)
        if (Callback != null)
            Callback(this.parentScreen, this, "button=" + this.name.ToLower());

        // Send button messages before default callbacks
        parentScreen.ButtonMessages(this);

        // Activate button functionality
        parentScreen.ButtonCallback(this);
		
		// send the 'close' message last, if we are auto closing
		if (closeWindow == true)
		{
			GUIDialog dialog = this.parentScreen as GUIDialog;
			if (dialog != null && dialog.DialogCallback !=null)
        		dialog.DialogCallback ("onbutton=close action=close");
		}

        // Activate auto-Goto command to GUIScreenInfo
        if (guiTarget != null && guiTarget.Length > 0)
        {
            GUIManager manager = GUIManager.GetInstance();
            ScreenInfo info = manager.LoadFromFile(guiTarget);
            if (modal != null)
                info.SetModal();
        }

        // Activate auto-GoTo command to GUIScreen
        if (screenTarget != null && screenTarget.Length > 0)
        {
            parentScreen.Parent.SetScreenTo(screenTarget);
        }

        // Activate auto-Load of Unity scene
        if (sceneTarget != null && sceneTarget.Length > 0)
        {
            Application.LoadLevel(sceneTarget);
        }	

#if !GUIMANAGER_STANDALONE
		// do interactMsg if specified
		if ( interactMsg != null && interactMsg.Length > 0)
		{
			InteractMsg msg = new InteractMsg(null,interactMsg,true);
			Brain.GetInstance().PutMessage(msg);
		}
		// do interactStatusMsg if specified
		if ( interactStatusMsg != null && interactStatusMsg.Length > 0)
		{
			InteractStatusMsg msg = new InteractStatusMsg(interactStatusMsg);
			Brain.GetInstance().PutMessage(msg);
		}
#endif
    }

	public override void RightArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
            if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right += (int)increment;
			else	
				_style.margin.left += (int)increment;
		}
	}
	public override void LeftArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right -= (int)increment;
			else	
				_style.margin.left -= (int)increment;
		}		
	}
	public override void UpArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right -= (int)increment;
			else	
				_style.margin.top -= (int)increment;
		}		
	}
	public override void DownArrow()
	{
		if ( _style != null )
		{
			float increment = 1.0f;
			if ( Input.GetKey(KeyCode.LeftAlt) )
				increment = 10.0f;
				
			if ( Input.GetKey(KeyCode.LeftControl) )
				_style.margin.right += (int)increment;
			else	
				_style.margin.top += (int)increment;
		}				
	}
	
}

public class GUIRepeatButton : GUIButton
{
    public bool pushed = false;

    public GUIRepeatButton()
        : base()
    {
    }

    public GUIRepeatButton(GUIRepeatButton obj)
        : base(obj)
    {
    }

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);
    }

#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIRepeatButton --");
		
		base.ShowEditor();

		pushed = (bool)EditorGUILayout.Toggle("pushed",pushed);
			
		GUILayout.Label("-- GUIRepeatButton --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
		Scale ();
		
        bool test = false;
        if (_style != null)
        {
            if (test = GUILayout.RepeatButton(_content, _style))
            {
                pushed = true;
                ButtonAction();
            }
        }
        else
        {
            if (test = GUILayout.RepeatButton(_content))
            {
                pushed = true;
                ButtonAction();
            }
        }

        // Do final callback to let module cancel action
        if (!test && pushed)
        {
			pushed = false;
            parentScreen.ButtonCallback(this);
        }
		
		Unscale ();
    }
}

public class GUIToggle : GUIButton
{
    public bool toggle = false;

    public GUIToggle()
        : base()
    {
    }

    public GUIToggle(GUIToggle obj)
        : base(obj)
    {
    }
	
	public override GUIObject Clone()
	{
		GUIToggle toggle = base.Clone() as GUIToggle;
		toggle.toggle = this.toggle;
		return toggle;
	}

#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIToggle --");
		
		base.ShowEditor();

		toggle = (bool)EditorGUILayout.Toggle("toggle",toggle);
			
		GUILayout.Label("-- GUIToggle --");
	}
#endif
	
    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
		Scale ();
		
        bool test = toggle;
        if(_style != null)
        {
            test = GUILayout.Toggle(toggle, _content, _style);

        }
        else
        {
            test = GUILayout.Toggle(toggle, _content);
        }
        
        if (toggle != test)
        {
			toggle = test; // new 11/19/20913 PAA
            ButtonAction();
          //  toggle = test; // this was previous to 11/19/2013, but button action should see the new state.
        }
		
		Unscale();
    }

	public override void SimulateButtonAction()
	{
		// toggle!
		toggle = !toggle;

		ButtonAction ();
	}	
}

#if OLD
public class GUIInteractButton : GUIButton
{
    public string xml;
    ObjectInteractionInfo info;

	public override GUIObject Clone()
	{
		GUIInteractButton button = base.Clone() as GUIInteractButton;
		button.xml = this.xml;
		button.info = this.info;
		return button;
	}

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        if (xml != null && xml.Length > 0)
        {
            Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
            info = serializer.Load(xml);

            if (info == null)
                Debug.LogError("Button -" + name + "- failed to load file: " + xml);
        }

    }

    protected override void ButtonAction()
    {
        base.Execute();

        // interact menu
        InteractDialogMsg msg = new InteractDialogMsg();
        msg.command = DialogMsg.Cmd.open;
        msg.baseobj = null;
        msg.title = info.Name;
        msg.x = 0;
        msg.y = 0;
        msg.items = info.ItemResponse;
        msg.modal = true;
        //print(InteractDialog.GetInstance().title);
        InteractDialogLoader.GetInstance().PutMessage(msg);
    }
}
#endif

public class GUILoadingBar : GUIObject
{
    public string barStyle;
    public int x;
    public int y;
    public int width;
    public int height;
    public string relative;

    protected float percentage = 0f;
    protected bool _relative = false;
    protected GUIStyle _barStyle;
    protected int maxWidth;
    protected Rect area;

    public float Percentage
    {
        get { return percentage; }
        set { percentage = value; }
    }

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        _relative = relative != null;

        if (_relative)
        {
            int trueX, trueY, trueWidth, trueHeight;
            Rect tempArea;
            if (container != null)
                tempArea = container.GetArea();
            else
                tempArea = parentScreen.GetArea();

            trueX = (int)(tempArea.width * x);
            trueY = (int)(tempArea.height * y);
            trueWidth = (int)(tempArea.width * width);
            trueHeight = (int)(tempArea.height * height);
            area = new Rect(trueX, trueY, trueWidth, trueHeight);
        }
        else
            area = new Rect(x, y, width, height);

        if (_skin != null)
        {
            if (barStyle != null)
            {
                _barStyle = _skin.FindStyle(barStyle);

                if (_barStyle == null)
                    Debug.LogError("Could not find barStyle - " + barStyle + " - in Skin - " + skin + " for GUI object " + name + ". Defaulting.");
            }
            else
                Debug.LogWarning("No barStyle set for " + name + " even though Skin was.");
        }
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        if (_style != null)
            GUILayout.BeginArea(area, _style);
        else
            GUILayout.BeginArea(area);
        DrawBar();
        GUILayout.EndArea();
    }

    void DrawBar()
    {
        if (_barStyle != null)
            GUILayout.Label("", _barStyle, GUILayout.Width(width * percentage));
        else
            GUILayout.Label("", GUI.skin.box, GUILayout.Width(width * percentage));
    }
}

#if UNITY_IPHONE
public class iosMovieTexture : Texture
{
	public iosMovieTexture(string url){
		filename = url;
	}
	
	public AudioClip audioClip=null;
	public float duration = 0;
	private bool _isPlaying = false;
	public bool isPlaying{
		get {
			bool wasPlaying=_isPlaying;
			_isPlaying = false; // only return true once for each set=true
			return wasPlaying;}
		set {_isPlaying = value;}
	}
	public bool isReadyToPlay = true;
	public bool loop = false;
	public string filename = "Cscvid.mp4";
	
	public void Pause(){}
	public void Play(){
		iPhoneUtils.PlayMovie(filename, Color.clear,iPhoneMovieControlMode.Full,iPhoneMovieScalingMode.AspectFit);
		isPlaying=true;
	}
	public void Stop(){}		
}
#endif

public class GUIMovie : GUIObject
{
    public string url;
	public string filename;
	public string background = "";
    public string bStyle;
	public bool loop;
	public bool autostart;

    protected GUIStyle _bStyle;
#if UNITY_IPHONE
	private Texture2D backgroundTex;
	private Texture2D playButtonTex;
    protected iosMovieTexture movie;
	
	public iosMovieTexture GetMovieTexture(){ return null;} // will need help on IOS
	public void SetMovieTexture(iosMovieTexture texture){}
#else
	protected MovieTexture movie;
	protected AudioSource audio;
	public MovieTexture GetMovieTexture(){ return movie;}
	public void SetMovieTexture(MovieTexture texture)
	{ 	
		movie = texture;
	}
#endif
    WWW wwwMovie;
    bool autostarted = false;
	
	public GUIMovie() : base()
	{
		editorShowStyle = false;
		editorShowText = false;
		autostart = true;
	}

	public override GUIObject Clone()
	{
		GUIMovie movie = base.Clone() as GUIMovie;
		movie.url = this.url;
		movie.bStyle = this.bStyle;
		movie._bStyle = this._bStyle;
		movie.loop = this.loop;
		return movie;
	}

#if UNITY_EDITOR
	public override void ShowEditor()
	{
		GUILayout.Label("-- GUIMovie --");
		
		base.ShowEditor();
		
		url = EditorGUILayout.TextField("url",url);
		loop = EditorGUILayout.Toggle("loop",loop);		
		
		if ( GUILayout.Button("Load Movie") )
		{
			
#if UNITY_IPHONE
            movie = new iosMovieTexture(filename); 
#else
			wwwMovie = new WWW(url);
			movie = new MovieTexture();
			movie = wwwMovie.movie;
#endif
            
            
		}
		if ( movie != null && movie.isReadyToPlay && movie.isPlaying == false )
		{
			if ( GUILayout.Button("Play") )
				movie.Play();				
		}
		if ( movie != null && movie.isReadyToPlay && movie.isPlaying == true )
		{
			if ( GUILayout.Button("Stop") )
				movie.Stop();				
		}
						
		GUILayout.Label("-- GUIMovie --");
	}
#endif

	public void Loop( bool yesno ) 
	{
		loop = yesno;
		if ( movie != null )
			movie.loop = loop;
	}

	// this methods don't suppose IOS yet
	//
	public void SetURL( string url )
	{
#if !UNITY_IPHONE
		if ( url != null && url.Length != 0 )
		{
			// make sure to destroy the texture before the load
			if ( movie != null ) 
			{
				MovieTexture.DestroyImmediate(movie);
				// request garbage collection, movies can be huge
				System.GC.Collect();
			}
			// load
			this.url = url;
			wwwMovie = new WWW(url);
			movie = wwwMovie.movie;
			audio = GUIManager.GetInstance().audio;
			audio.clip = movie.audioClip;
		}
#endif
	}

	public void SetFilename( string filename )
	{
#if !UNITY_IPHONE
		if ( filename != null && filename.Length != 0 )
		{
			this.filename = filename;
			// make sure to destroy the texture before the load
			if ( movie != null )
			{
				MovieTexture.DestroyImmediate(movie);
				// request garbage collection, movies can be huge
				System.GC.Collect();
			}
			// load
			// iphone path
			//wwwMovie = new WWW("file://" + Application.dataPath + "/Raw/" + filename);
			// load new file
			wwwMovie = new WWW("file://" + Application.dataPath + "/StreamingAssets/" + filename);
			movie = wwwMovie.movie;
			audio = GUIManager.GetInstance().audio;
			audio.clip = movie.audioClip;
		}
#endif
	}
	
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

		SetURL (url);
		SetFilename (filename);

		/*
        if (url != null && url.Length > 0)
        {
            
#if UNITY_IPHONE
            movie = new iosMovieTexture(filename); 
			if (background != "")
				backgroundTex = Resources.Load(background) as Texture2D;
			playButtonTex = Resources.Load ("PlayButton") as Texture2D;
#else
			if (TraumaOfflineAssetContainer.GetInstance() != null &&
			    TraumaOfflineAssetContainer.GetInstance().bUseOfflineAssets &&
			    TraumaOfflineAssetContainer.GetInstance().GetMovieTexture(url) != null){
				movie = TraumaOfflineAssetContainer.GetInstance().GetMovieTexture(url); // load cached version if offline
			}else{
				wwwMovie = new WWW(url);
				movie = new MovieTexture();
				movie = wwwMovie.movie;
			}
#endif
        }
		else if ( filename != null && filename.Length > 0 )
		{
			if ( movie == null )
			{
#if !UNITY_IPHONE
				movie = Resources.Load<MovieTexture>(filename);
				UnityEngine.Debug.LogError ("MOVIE <" + filename + ">=" + movie);
#endif
			}
		}
		*/

        if (bStyle != null && bStyle.Length > 0 && _skin != null)
        {
            _bStyle = _skin.FindStyle(bStyle);
        }

		if ( movie != null )
      	  movie.loop = loop;
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
			
#if UNITY_IPHONE
		Rect texRect = (container as GUIArea).GetArea();
		texRect.x=0;
		texRect.y=0;
		
		if (backgroundTex != null){	
			GUI.DrawTexture(texRect,backgroundTex);
			if (!autostarted){
				// put up a texture button so clicking will start the video
				Rect labelRect = new Rect(texRect.width/2-50,texRect.height/2-20,playButtonTex.width,playButtonTex.height);
				if (GUI.Button(labelRect,playButtonTex)){
					autostarted = true;
					Play ();
				}
			}
		}
#else		
		Scale ();
        // Display movie texture
        if (_style != null){
			if (_style.fixedWidth == 0 && _style.fixedHeight == 0){
				// just draw the movie at it's native size
            	GUILayout.Label(movie, _style);
			}
			else
			{
				GUI.DrawTexture(new Rect(_style.contentOffset.x,_style.contentOffset.y,_style.fixedWidth, _style.fixedHeight),
								movie,ScaleMode.StretchToFill);
			}
		}
        else
            GUILayout.Label(movie);

        if (_bStyle != null)
        {
            if (!movie.isReadyToPlay)
            {
                GUILayout.Button("Loading", _bStyle);
            }
            if (movie.isPlaying)
            {
                if (GUILayout.Button("Pause", _bStyle))
                    Pause();
            }
            else if (!movie.isPlaying)
            {
                if (GUILayout.Button("Play", _bStyle))
                    Play();
            }
        }
		
		Unscale ();

        if (autostart == true && movie != null && !movie.isPlaying && movie.isReadyToPlay && !autostarted)
        {
            audio.clip = movie.audioClip;
			movie.Stop ();  // this rewinds it
            movie.Play();
            audio.Play();
            autostarted = true;
        }
#endif
    }

    public void Play() 
    {
		if ( movie != null  )
			movie.Play();
#if !UNITY_IPHONE
        audio.Play();
#endif
    }

    public void Pause()
    {
#if !UNITY_IPHONE
        GUIManager manager = GUIManager.GetInstance();
		if ( movie != null )
        	movie.Pause();
        audio.Pause();
#endif
    }

    public void Stop()
    {
#if !UNITY_IPHONE
        GUIManager manager = GUIManager.GetInstance();
		if ( movie != null )
    	    movie.Stop();
        audio.Stop();
#endif
    }

    public bool IsPlaying()
    {
		if ( movie == null )
			return false;
		else
        	return movie.isPlaying;
    }
}


public class GUIMenu : GUIObject
{
    public class MenuButton
    {
        public bool on = false;
        public bool active = false;
        public bool draw = true;
        public GUIButton button;
    };

    public GUIButton buttonTemplate;
    bool togglable = false;

    protected List<MenuButton> buttons = new List<MenuButton>();

    public List<MenuButton> GetButtons()
    {
        return buttons;
    }
	
	public MenuButton GetOnButton()
	{
        for(int i = 0; i < buttons.Count; i++)
        {
			if ( buttons[i].on == true )
				return buttons[i];
		}
		return null;
	}

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);
        buttonTemplate.Process(parentScreen, container);
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        GUILayout.BeginVertical();
        for(int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].draw)
            {
                bool test = GUILayout.Toggle(buttons[i].active, buttons[i].button.text, buttons[i].button.Style);
                if (buttons[i].active != test)
                {
                    buttons[i].on = test;
                }
            }
        }
        if (buttons.Count == 0)
            GUILayout.Toggle(false, "", buttonTemplate.Style);
        GUILayout.EndVertical();
    }

    public virtual void Setup(List<string> entries)
    {
        if (entries == null)
            return;
		buttons.Clear();
        foreach (string entry in entries)
        {
            MenuButton button = new MenuButton();
            button.button = new GUIButton(buttonTemplate);
            button.button.text = entry;
            buttons.Add(button);
        }
    }

    public void Clear()
    {
        buttons.Clear();
    }

    public void TurnActive(int turnActive)
    {
        for(int i = 0; i < buttons.Count; i++)
        {
            if (i == turnActive)
                buttons[i].active = true;
            else
                buttons[i].active = false;
        }
    }
}

public class GUIScrollMenu : GUIMenu
{
    public GUIScrollView scrollviewTemplate;
    protected Vector2 scroll = new Vector2(0, 0);

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);
		if(scrollviewTemplate != null)
        	scrollviewTemplate.Process(parentScreen, container);
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        GUILayout.BeginVertical();
        if (scrollviewTemplate != null && scrollviewTemplate.VStyle != null && scrollviewTemplate.HStyle != null)
        {
            if (_style != null)
                scroll = GUILayout.BeginScrollView(scroll, scrollviewTemplate.displayHorizontal, scrollviewTemplate.displayVertical, scrollviewTemplate.HStyle, scrollviewTemplate.VStyle, scrollviewTemplate.Style);
            else
                scroll = GUILayout.BeginScrollView(scroll, scrollviewTemplate.displayHorizontal, scrollviewTemplate.displayVertical, scrollviewTemplate.HStyle, scrollviewTemplate.VStyle);

            base.Execute();

            GUILayout.EndScrollView();
        }
        else
        {
            base.Execute();
        }
        GUILayout.EndVertical();
    }
}

#if !GUIMANAGER_STANDALONE
public abstract class GUIVitalGraph : GUIObject
{
    protected VitalsGraph graph;
	
	public VitalsGraph GetVitalsGrapher()
	{
		return graph;
	}
	
	public void SetVitalsGrapher( VitalsGraph graph )
	{
		this.graph = graph;
	}

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        // Get graphic
        //graph = VitalsGraph.GetInstance();
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        if (graph != null)
        {
            if (_style != null)
                GUILayout.Label(graph.GetTexture(), _style);
            else
                GUILayout.Label(graph.GetTexture());
        }
        else
        {
            if (_style != null)
                GUILayout.Label("VitalsGraph not found for GUI Object -" + name + "-", _style);
            else
                GUILayout.Label("VitalsGraph not found for GUI Object -" + name + "-");
        }
    }
}

public class GUIHeartbeatGraph : GUIVitalGraph
{
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        graph = Component.FindObjectOfType(typeof(HeartbeatGraph)) as HeartbeatGraph;
    }
}

public class GUIDiastolicGraph : GUIVitalGraph
{
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        graph = Component.FindObjectOfType(typeof(DiastolicGraph)) as DiastolicGraph;
    }
}

public class GUISystolicGraph : GUIVitalGraph
{
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        graph = Component.FindObjectOfType(typeof(SystolicGraph)) as SystolicGraph;
    }
}

public class GUIO2Graph : GUIVitalGraph
{
    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        graph = Component.FindObjectOfType(typeof(O2Graph)) as O2Graph;
    }
}

#endif

#if OLD
public class GUIDrugList : GUIObject
{
    public GUIScrollMenu scrollMenu;
    public GUILabel divider;

    GUIScrollMenu[] menus;

    public GUIScrollMenu[] GetMenus()
    {
        return menus;
    }

    public override void Process(GUIScreen parentScreen, GUIArea container)
    {
        base.Process(parentScreen, container);

        if (scrollMenu != null)
        {
            scrollMenu.Process(parentScreen, container);
            menus = new GUIScrollMenu[4];

            menus[0] = new GUIScrollMenu();
            menus[1] = new GUIScrollMenu();
            menus[2] = new GUIScrollMenu();
            menus[3] = new GUIScrollMenu();

            menus[0].CopyFrom(scrollMenu);
            menus[1].CopyFrom(scrollMenu);
            menus[2].CopyFrom(scrollMenu);
            menus[3].CopyFrom(scrollMenu);

            menus[0].buttonTemplate = scrollMenu.buttonTemplate;
            menus[0].scrollviewTemplate = scrollMenu.scrollviewTemplate;
            menus[0].Process(parentScreen, container);

            menus[1].buttonTemplate = scrollMenu.buttonTemplate;
            menus[1].scrollviewTemplate = scrollMenu.scrollviewTemplate;
            menus[1].Process(parentScreen, container);

            menus[2].buttonTemplate = scrollMenu.buttonTemplate;
            menus[2].scrollviewTemplate = scrollMenu.scrollviewTemplate;
            menus[2].Process(parentScreen, container);

            menus[3].buttonTemplate = scrollMenu.buttonTemplate;
            menus[3].scrollviewTemplate = scrollMenu.scrollviewTemplate;
            menus[3].Process(parentScreen, container);
        }

        if (divider != null)
            divider.Process(parentScreen, container);
    }

    public override void Execute()
    {
		// handle visibility
		if ( visible == false )
			return;
		
        if (scrollMenu != null)
        {
            GUILayout.BeginHorizontal();
            menus[0].Execute();
            if (divider != null)
                divider.Execute();
            menus[1].Execute();
            if (divider != null)
                divider.Execute();
            menus[2].Execute();
            if (divider != null)
                divider.Execute();
            menus[3].Execute();
            GUILayout.EndHorizontal();
        }
    }
}

public class GUIOrderCart : GUILabel
{

}

#endif
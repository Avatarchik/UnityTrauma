//#define GUIMANAGER_STANDALONE
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

public class GUIScreenMsg : GameMsg
{
	public GUIScreenMsg( string screenName=null ) : base() 
	{ 
		arguments = new List<string>();
		ScreenName = screenName; 
	}

	public string ScreenName;
    public List<string> arguments;
}

[System.Serializable]
public class GUIScreen
{
    static int counter = 1;

    public string type = "GUIScreen";
    public string name = "";
    public float x = 0;
    public float y = 0;
    public float width = 0;
    public float height = 0;
    public string skin = "";
    public string style = "";
    public string relative;
    public string centered;
	public bool fillarea = false;
	public bool fade = false;
	public float fadespeed = -1;
    public string blank; 
	
	public bool visible=true;
	
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
#if !GUIMANAGER_STANDALONE
	[XmlArrayItem(ElementName = "Heartbeat", Type = typeof(GUIHeartbeatGraph))]
    [XmlArrayItem(ElementName = "Diastolic", Type = typeof(GUIDiastolicGraph))]
    [XmlArrayItem(ElementName = "Systolic", Type = typeof(GUISystolicGraph))]
    [XmlArrayItem(ElementName = "O2", Type = typeof(GUIO2Graph))]
#endif
#if OLD
	[XmlArrayItem(ElementName = "InteractionButton", Type = typeof(GUIInteractButton))]
	[XmlArrayItem(ElementName = "OrderCart", Type = typeof(GUIOrderCart))]
	[XmlArrayItem(ElementName = "DrugList", Type = typeof(GUIDrugList))]
#endif
    public List<GUIObject> Elements = new List<GUIObject>();
	
	// listing of all style images, fonts, etc.
	public List<GUIStyleInfo> styleInfo;
	
	// create a style info for this screen
	public List<GUIStyleInfo> CreateStyleInfo()
	{
		if ( Skin == null )
			return null;
		
		List<GUIStyleInfo> Info = new List<GUIStyleInfo>();
		
		foreach( GUIStyle style in Skin.customStyles )
		{
			GUIStyleInfo info = new GUIStyleInfo();
			info.Copy (style);
			if ( info.HasInfo == true )
				Info.Add (info);
		}		
		// set to screen
		styleInfo = Info;
		// also return it
		return Info;
	}
                            
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
		right_middle
	}
	public Anchor anchor=Anchor.none;
	protected float anchorX;
	protected float anchorY;

	[System.NonSerialized]
    protected ScreenInfo parent;
    public bool windowed;
	public bool draggable = false;
    protected bool _relative = false;
    protected bool _centered = false;
    protected bool _blank = false;
    protected int windowID;
    protected GUISkin _skin;
    protected GUIStyle _style;
    protected Rect area;
	
	protected float scaleX=1.0f,scaleY=1.0f;

	public void SetAnchor( string anchorString )
	{
		if ( anchorString == "none" )
			return;

		// map name to enum
		switch( anchorString )
		{
		case "normal":
			anchor = Anchor.normal;
			break;
		case "relative":
			anchor = Anchor.relative;
			break;
		case "centered":
			anchor = Anchor.centered;
			break;
		case "upper_left":
			anchor = Anchor.upper_left;
			break;
		case "upper_right":
			anchor = Anchor.upper_right;
			break;
		case "lower_left":
			anchor = Anchor.lower_left;
			break;
		case "lower_right":
			anchor = Anchor.lower_right;
			break;
		case "left_middle":
			anchor = Anchor.left_middle;
			break;
		case "right_middle":
			anchor = Anchor.right_middle;
			break;
		}
	}

	public void SetPosition( float x, float y )
	{
		if ( _style != null && x != -1 && y != -1 )
		{
			anchorX = x;
			anchorY = y;
		}
	}
	
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
		Vector2 tmp = new Vector2(x,y);
		if ( Style != null )	
			tmp = new Vector2(area.x,area.y);
		return tmp;
	}

	protected Vector2 drag;

    public GUIScreen()
    {
    }
	
	public virtual void PutMessage( GameMsg msg )
	{
		DialogMsg dmsg = msg as DialogMsg;
		if ( dmsg != null )
		{
			switch ( dmsg.command )
			{
			case DialogMsg.Cmd.open:
				SetAnchor (dmsg.anchor);
				SetPosition(dmsg.x,dmsg.y);
				break;
			case DialogMsg.Cmd.close:
				GUIManager.GetInstance().RemoveNoFade(this.Parent);
				break;
			case DialogMsg.Cmd.position:
				SetPosition(dmsg.x,dmsg.y);
				break;
			case DialogMsg.Cmd.anchor:
				SetAnchor(dmsg.anchor);
				break;
			}
		}
	}
	
#if UNITY_EDITOR
	string customStyle="";
	List<string> styles = new List<string>();
	GUISkin lastSkin=null;
	int styleIndex=0;

	public void ShowEditor()
	{
		GUILayout.Label("-- GUIScreen --");
		//GUILayout.Label("GUIObject *********");
		this.name = EditorGUILayout.TextField("name",this.name);
		this.type = EditorGUILayout.TextField("type",this.type);
		this.draggable = EditorGUILayout.Toggle("draggable",this.draggable);
		anchor = (Anchor)EditorGUILayout.EnumPopup("anchor",(System.Enum)anchor);
		fillarea = EditorGUILayout.Toggle("fill",this.fillarea);
		
		if ( this._style == null )
		{
			x = EditorGUILayout.FloatField("x",x);
			y = EditorGUILayout.FloatField("y",y);
			width = EditorGUILayout.FloatField("w",width);
			height = EditorGUILayout.FloatField("h",height);			
		}
		
		this._skin = (GUISkin)EditorGUILayout.ObjectField("skin",this._skin,typeof(GUISkin));
		if ( lastSkin != this._skin && this._skin != null)
		{
			if (this._skin != null )
				this.skin = this._skin.name;			
			lastSkin = this._skin;
			
			// get list of custom styles
			styles.Clear();
			styles.Add("none");
			foreach( GUIStyle s in this._skin.customStyles)
				styles.Add(s.name);
			
			styleIndex = 0;
			
			// set to first one
			if ( this.style == null || this.style == "" )
				// optional, set style to NONE
				customStyle = "none"; //styles[0];
			else
			{
				customStyle = this.style;
				// set style index to this style
				for (int i=0 ; i<styles.Count ; i++)
				{
					if ( styles[i] == customStyle )
						styleIndex = i;
				}				
			}
		}
		if ( this._skin != null && this._skin.customStyles.Length > 0 )
		{
			// display popup
			styleIndex = EditorGUILayout.Popup("style",styleIndex,styles.ToArray());
			customStyle = styles[styleIndex];
			// display selected style
			//this.customStyle = EditorGUILayout.TextField("style",this.customStyle);
			// set the style
			if ( this._skin != null )
				this._style = this._skin.FindStyle(this.customStyle);
			else
				this._style = null;			
			
			if ( this._style != null )
				this.style = this._style.name;
			else
				this.style = "none";
		}		
		GUILayout.Label("-- GUIScreen --");
	}
	
#endif

    public void CopyFrom(GUIScreen scr)
    {
        type = scr.type;
        name = scr.name;
        x = scr.x;
        y = scr.y;
        width = scr.width;
        height = scr.height;
        skin = scr.skin;
        style = scr.style;
        relative = scr.relative;
        centered = scr.centered;
        blank = scr.blank;
        _relative = scr._relative;
        _centered = scr._centered;
        _blank = scr._blank;
        Elements = scr.Elements;
        area = scr.area;
        windowID = scr.windowID;
        windowed = scr.windowed;
		anchor = scr.anchor;
		anchorX = scr.anchorX;
		anchorY = scr.anchorY;
		draggable = scr.draggable;
		_skin = scr._skin;
		_style = scr._style;
		fillarea = scr.fillarea;
		fade = scr.fade;
		fadespeed = scr.fadespeed;
		scaleX = scr.scaleX;
		scaleY = scr.scaleY;
		styleInfo = scr.styleInfo;
    }

    public ScreenInfo Parent
    {
        get { return parent; }
    }

    public bool Windowed
    {
        get { return windowed; }
        set { windowed = value; }
    }

    public Rect Area
    {
        get { return area; }
    }

    public GUISkin Skin
    {
        get { return _skin; }
        //set { _skin = value; }
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

    public void Add(GUIObject obj)
    {
        Elements.Add(obj);
    }

    public void Remove(GUIObject obj)
    {
        Elements.Remove(obj);
    }

    public GUIObject Find(string name)
    {
        foreach (GUIObject obj in Elements)
        {
            if (obj.name != null && obj.name == name)
                return obj;
            if(obj.GetType() == typeof(GUIContainer) || obj.GetType().BaseType == typeof(GUIContainer))
            {
                GUIObject test = (obj as GUIContainer).Find(name);
                if(test != null)
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

    // helpers
    public virtual void SetXY(float x, float y)
    {
        this.x = x;
        this.y = y;
        ResetPosition();
    }

    public virtual void CenterX()
    {
        if (_relative)
        {
            this.x = 0.5f - (width * 0.5f);
        }
        else
        {
            this.x = GUIManager.GetInstance().Width / 2 - width / 2;
        }
        ResetPosition();
    }

    public virtual void CenterY()
    {
        if (_relative)
        {
            this.y = 0.5f - (height * 0.5f);
        }
        else
        {
            this.y = GUIManager.GetInstance().Height / 2 - height / 2;
        }
        ResetPosition();
    }

    public virtual void SetCentered(bool yesno)
    {
        _centered = yesno;
        ResetPosition();
    }

    public virtual void SetRelative(bool yesno)
    {
        _relative = yesno;
        ResetPosition();
    }
	
	public virtual void SetAnchor( Anchor anchor, float x, float y )
	{
		this.anchor = anchor;
		this.anchorX = x;
		this.anchorY = y;
		ResetPosition();
	}
	
	public virtual void SetScale( float x, float y )
	{
		scaleX = x;
		scaleY = y;
       	foreach (GUIObject obj in Elements)
           obj.SetScale(x,y);		
	}

    public virtual void SetSkin(string guiobjName, string skinName)
    {
        GUIObject guiobj = Find(guiobjName);
        if (guiobj != null)
        {
            GUISkin skin = GUIManager.GetInstance().FindSkin(skinName);
            if (skin != null)
            {
                guiobj.SetStyle(style);
            }
            else
                UnityEngine.Debug.LogWarning("GUIScreen.SetSkin(" + guiobjName + "," + skinName + ") : can't find Skin");
        }
        else
            UnityEngine.Debug.LogWarning("GUIScreen.SetSkin(" + guiobjName + "," + skinName + ") : can't find GUIObject");
    }

    public virtual void SetStyle(string guiobjName, string skinName, string styleName)
    {
        GUIObject guiobj = Find(guiobjName);
        if (guiobj != null)
        {
            GUISkin skin = GUIManager.GetInstance().FindSkin(skinName);
            if (skin != null)
            {
                GUIStyle style = skin.FindStyle(styleName);
                if (style != null)
                {
                    guiobj.SetSkin(skin);
                    guiobj.SetStyle(style);
                }
                else
                    UnityEngine.Debug.LogWarning("GUIScreen.SetStyle(" + guiobjName + "," + skinName + "," + styleName + ") : can't find Style");
            }
            else
                UnityEngine.Debug.LogWarning("GUIScreen.SetStyle(" + guiobjName + "," + skinName + "," + styleName + ") : can't find Skin");
        }
        else
            UnityEngine.Debug.LogWarning("GUIScreen.SetStyle(" + guiobjName + "," + skinName + "," + styleName + ") : can't find GUIObject");
    }

    public virtual void SetLabelText(string labelName, string text)
    {
        GUIObject guiobj = Find(labelName);
        if (guiobj != null)
        {
            GUILabel label = guiobj as GUILabel;
            if (label != null)
            {
                label.text = text;
                label.Content.text = text;
            }
            else
                UnityEngine.Debug.LogWarning("GUIScreen.SetLabelText(" + labelName + ") : tag not a GUILabel");
        }
        else
            UnityEngine.Debug.LogWarning("GUIScreen.SetLabelText(" + labelName + ") : can't find GUILabel");
    }

    // label=titleBar.titleBarText text=\"Confirm\" 
    public bool ParseAreaElement( string item, out string area, out string element )
    {
        area = "";
        element = "";
		
		if ( item.Contains(".") )
		{
			// 1st case is area.elemenet
        	string[] args = item.Split('.');
        	// get args
        	if (args.Length != 2)
	            return false;
	
        	area = args[0];
        	element = args[1];
		}
		else
		{
			// 2nd case is just element name (no conatiner)
			element = item;
		}
	
        return true;
    }

    public bool ParseArg( string item, out string arg )
    {
        arg = "";
		
		if ( item.Contains("=") )
		{
			// proper form, split
        	string[] args = item.Split('=');
        	if ( args.Length != 2)
	            return false;	
        	arg = args[1];
	        return true;
    	}
		else
			// no =, no go
			return false;
	}

    // onbutton=yesContainer.buttonConfirm interact=PREP:INTUBATION
    public virtual void ParseOnButton(string button)
    {
        string[] args = GetKeyValuePairs(button).ToArray();

        string area;
        string element;

        // remove onbutton=
        args[0] = args[0].Replace("onbutton=","");
		
		// first check to see if we can find this as an Arg
	    GUIButton GUIbutton = Find(args[0]) as GUIButton;
        if (GUIbutton != null)
        {
	    	// now add back args for value
            GUIbutton.AddMessage(button);
			// we're done
			return;
        }

        if (ParseAreaElement(args[0], out area, out element) == true)
        {
			if ( area != "" )
			{
				// we have an area, find the button inside it
            	GUIArea GUIarea = Find(area) as GUIArea;
            	if (GUIarea != null)
            	{
	                GUIbutton = GUIarea.Find(element) as GUIButton;
                	if (GUIbutton != null)
                	{
	                    // now add back args for value
                    	GUIbutton.AddMessage(button);
                	}
				}
				else
				{
					// can't find the area, try to find the element
					// no area container, just try to find the button
	        		GUIbutton = Find(element) as GUIButton;
                	if (GUIbutton != null)
                	{
		            	// now add back args for value
                    	GUIbutton.AddMessage(button);
                	}
				}
            }
        }
    }

    public List<string> GetKeyValuePairs(string input)
    {
        List<string> pairs = new List<string>();

        string result = "";
        bool inQuote = false;
        // scan input until there is a space
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == ' ')
            {
                if (inQuote == true)
                    result += input[i];
                else
                {
                    // reached an end
                    pairs.Add(result);
                    result = "";
                }
            }
            else
            {
                if ( input[i] == '"' )
                    inQuote = (inQuote == true) ? false : true;
                result += input[i];
            }
        }

        pairs.Add(result);
        return pairs;
    }

	public bool GetToken(List<string> args, string key, out string value)
	{
		value = "null";
		foreach( string arg in args )
		{
			if ( GetToken(arg,key,out value) == true )
				return true;
		}
		return false;
	}
	
    public bool GetToken(string arg, string key, out string value)
    {
		List<string> args = GetKeyValuePairs(arg);
		foreach( string item in args )
		{
            string[] keyvalue = item.Split('=');
            if (keyvalue.Length == 2)
            {
				if ( keyvalue[0] == key )
               	{
                    value = keyvalue[1];
                    return true;
                }
            }
        }
        value = "";
        return false;
	}				
			
    // label=area.label text="are you sure"
    public virtual void ParseLabel(string labelstring)
    {
        string[] args = GetKeyValuePairs(labelstring).ToArray();

        string area;
        string element;

        // remove onbutton=
        args[0] = args[0].Replace("label=","");

		// just an element
	    GUILabel GUIlabel = Find(args[0]) as GUILabel;
        if (GUIlabel != null)
        {
	        if (args[1].ToLower().Contains("text="))
        	{
	            args[1] = args[1].Replace("text=","");
            	args[1] = args[1].Replace("\"", "");
 				// replace token for stringmgr
				if ( args[1].Contains("%%") )
				{
					args[1] = args[1].Replace("%%","");
					args[1] = StringMgr.GetInstance().Get (args[1]);
				}
           		GUIlabel.text = args[1];
				// we're done!
				return;
        	}
        }
		
        if (ParseAreaElement(args[0], out area, out element) == true)
        {
			if ( area != "" )
			{
				// we have an area
            	GUIArea GUIarea = Find(area) as GUIArea;
            	if (GUIarea != null)
            	{
	                GUIlabel = GUIarea.Find(element) as GUILabel;
                	if (GUIlabel != null)
                	{
	                    if (args[1].ToLower().Contains("text="))
                    	{
	                        args[1] = args[1].Replace("text=","");
                        	args[1] = args[1].Replace("\"", "");
							// replace token for stringmgr
							if ( args[1].Contains("%%") )
							{
								args[1] = args[1].Replace("%%","");
								args[1] = StringMgr.GetInstance().Get (args[1]);
							}
                        	GUIlabel.text = args[1];
                    	}
                	}
            	}
				else
				{
					// can't find area, just find the element
		            GUIlabel = Find(element) as GUILabel;
                	if (GUIlabel != null)
                	{
		                if (args[1].ToLower().Contains("text="))
                		{
		                    args[1] = args[1].Replace("text=","");
                    		args[1] = args[1].Replace("\"", "");
 							// replace token for stringmgr
							if ( args[1].Contains("%%") )
							{
								args[1] = args[1].Replace("%%","");
								args[1] = StringMgr.GetInstance().Get (args[1]);
							}
                   			GUIlabel.text = args[1];
                		}
                	}
				}
			}
        }
    }
	
     // label=area.label text="are you sure"
    public virtual void ParseElement(string elementstring)
    {
        string[] args = GetKeyValuePairs(elementstring).ToArray();

        string area;
        string element;

		// just an element
	    GUIObject go = Find(args[0]) as GUIObject;
        if (go != null)
        {
	        if (args[1].ToLower().Contains("text="))
        	{
	            args[1] = args[1].Replace("text=","");
            	args[1] = args[1].Replace("\"", "");
 				// replace token for stringmgr
				if ( args[1].Contains("%%") )
				{
					args[1] = args[1].Replace("%%","");
					args[1] = StringMgr.GetInstance().Get (args[1]);
				}
           		go.text = args[1];
				go.UpdateContent();
				// we're done
				return;
        	}
			// to see visibility of an element
			if ( args[1].ToLower().Contains ("visible="))
			{
				args[1] = args[1].Replace("visible=","");
				args[1] = args[1].Replace("\"", "");
				if ( args[1] == "true" )
					go.visible = true;
				if ( args[1] == "false" )
					go.visible = false;
			}
		}
		
        if (ParseAreaElement(args[0], out area, out element) == true)
        {
			if ( area != "" )
			{
				// we have an area
            	GUIArea GUIarea = Find(area) as GUIArea;
            	if (GUIarea != null)
            	{
	                go = GUIarea.Find(element) as GUIObject;
                	if (go != null)
                	{
	                    if (args[1].ToLower().Contains("text="))
                    	{
	                        args[1] = args[1].Replace("text=","");
                        	args[1] = args[1].Replace("\"", "");
 							// replace token for stringmgr
							if ( args[1].Contains("%%") )
							{
								args[1] = args[1].Replace("%%","");
								args[1] = StringMgr.GetInstance().Get (args[1]);
							}
                       		go.text = args[1];
							go.UpdateContent();
                    	}
                	}
            	}
				else
				{
					// can't find area, just find the element
		            go = Find(element) as GUIObject;
                	if (go != null)
                	{
		                if (args[1].ToLower().Contains("text="))
                		{
		                    args[1] = args[1].Replace("text=","");
                    		args[1] = args[1].Replace("\"", "");
							// replace token for stringmgr
							if ( args[1].Contains("%%") )
							{
								args[1] = args[1].Replace("%%","");
								args[1] = StringMgr.GetInstance().Get (args[1]);
							}
                    		go.text = args[1];
							go.UpdateContent();
                		}
                	}
				}
			}
        }
    }
	
   // skin=area skin=name
    public virtual void ParseArea(string skinstring)
    {
		string area;
		if (GetToken(skinstring, "area", out area) == true)
		{
			// we have an area
           	GUIArea GUIarea = Find(area) as GUIArea;
           	if (GUIarea != null)
           	{
				// see if we have a skin
				string skin;
				if (GetToken(skinstring, "skin", out skin) == true)
				{
					// see if we have a style
					string style;
					if (GetToken(skinstring, "style", out style) == true)
					{
#if NEW_STUFF
						// check if style has an object tag
						if ( style.Contains("%%"))
						{
							// remove the %%, move to lower case
							style = style.Replace("%%","").ToLower();
							// syntax needs to be object.tag
							DecisionVariable var = new DecisionVariable(style, true);
							if ( var.Valid == true )
							{
								// get the new style
								style = var.Get();
							}
						}
#endif
						// set skin & style
						SetStyle(area,skin,style);
					}
					else
					{
						// no style, just set the skin
						SetSkin(area,skin);					
					}
				}
			}
		}
    }
	
	public virtual void ParseFlash( string elementstring )
	{
        string[] args = GetKeyValuePairs(elementstring).ToArray();

        string area;
        string element;

		// just an element
	    GUIObject go = Find(args[0]) as GUIObject;
        if (go != null)
        {
			for (int i=1 ; i<args.Length ; i++)
			{						
		        if (args[i].ToLower().Contains("value="))
	        	{
		        	args[i] = args[i].Replace("value=","");
	           		args[i] = args[i].Replace("\"", "");							
	            	go.Flash = (args[i]=="true")?true:false;
	        	}
		        if (args[i].ToLower().Contains("rate="))
	        	{
		        	args[i] = args[i].Replace("rate=","");
	           		args[i] = args[i].Replace("\"", "");							
	            	go.FlashRate = Convert.ToSingle(args[i]);
	        	}
			}
			// we're done
			return;
        }
		
        if (ParseAreaElement(args[0], out area, out element) == true)
        {
			if ( area != "" )
			{
				// we have an area
            	GUIArea GUIarea = Find(area) as GUIArea;
            	if (GUIarea != null)
            	{
	                go = GUIarea.Find(element) as GUIObject;
                	if (go != null)
                	{
						for (int i=1 ; i<args.Length ; i++)
						{						
		                    if (args[i].ToLower().Contains("value="))
	                    	{
		                    	args[i] = args[i].Replace("value=","");
	                       		args[i] = args[i].Replace("\"", "");							
	                        	go.Flash = (args[i]=="true")?true:false;
	                    	}
		                    if (args[i].ToLower().Contains("rate="))
	                    	{
		                    	args[i] = args[i].Replace("rate=","");
	                       		args[i] = args[i].Replace("\"", "");							
	                        	go.FlashRate = Convert.ToSingle(args[i]);
	                    	}
						}
                	}
            	}
				else
				{
					// can't find area, just find the element
		            go = Find(element) as GUIObject;
                	if (go != null)
                	{
						for (int i=1 ; i<args.Length ; i++)
						{						
		                    if (args[i].ToLower().Contains("value="))
	                    	{
		                    	args[i] = args[i].Replace("value=","");
	                       		args[i] = args[i].Replace("\"", "");							
	                        	go.Flash = (args[i]=="true")?true:false;
	                    	}
		                    if (args[i].ToLower().Contains("rate="))
	                    	{
		                    	args[i] = args[i].Replace("rate=","");
	                       		args[i] = args[i].Replace("\"", "");							
	                        	go.FlashRate = Convert.ToSingle(args[i]);
	                    	}
						}
                	}
				}
			}
        }		
	}
	
    public virtual void ParseParamString( List<string> arguments )
    {
        if ( arguments == null || arguments.Count == 0 )
            return;

#if DEBUG_PARAM_STRING
        UnityEngine.Debug.Log("GUIScreen.ProcessParamString() : string=" + arguments);
#endif

        // parse
        foreach (string element in arguments)
        {
#if DEBUG_PARAM_STRING
            UnityEngine.Debug.Log("GUIScreen.ProcessParamstring() : element: " + element);
#endif
            if (element.Contains("label="))
            {
				string tmp = element;
        		// remove element=
        		tmp = tmp.Replace("label=","");
				ParseElement(tmp);
                //ParseLabel(element);
            }
            if (element.Contains("onbutton="))
            {
                ParseOnButton(element);
            }
			if (element.Contains("area="))
			{
				ParseArea(element);
			}
			if (element.Contains("element="))
			{
				string tmp = element;
        		// remove element=
        		tmp = tmp.Replace("element=","");
				ParseElement(tmp);
			}
			if (element.Contains("flash="))
			{
				string tmp = element;
        		// remove element=
        		tmp = tmp.Replace("flash=","");
				ParseFlash(tmp);				
			}
        }
    }

    public virtual void Initialize(ScreenInfo parent)
    {
        this.parent = parent;
        //this.windowed = false;

        windowID = counter++;

        _relative = relative != null;
        _centered = centered != null;
        _blank = blank != null;
		
		//UnityEngine.Debug.LogError("GUIScreen.Initialize(" + this.name + ") : XYWH<" + x + "," + y + "," + width + "," + height + "> relative=" + _relative + " : anchor=" + anchor);
		
		// reset drag offsets
		if ( draggable == true )
		{
			drag = Vector2.zero;
		}
				
		// reset position
        ResetPosition();
        
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
	                Debug.LogError("Could not find Skin - " + skin + " - for GUI object " + name + ". Defaulting to default GUI skin.");
            }
            if (_skin != null && style != null)
            {
                // Set style, if exists
                _style = _skin.FindStyle(style);

#if DEFAULT_TO_BOX
                // Error check
                if (_style == null)
                {
                    Debug.LogError("Could not find Style - " + style + " - in Skin - " + skin + " for GUI object " + name + ". Defaulting to box.");
                    _style = _skin.box;
                }
#endif
            }
            else
                Debug.LogWarning("No Style set for " + name + " even though Skin was.");
        }

        foreach (GUIObject obj in Elements)
        {
            obj.Process(this, null);
        }
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
	
	protected virtual void ResetRelativePosition()
	{
        if (_centered)
       	{
            float trueWidth = (int)(GUIManager.GetInstance().Width * width);
           	float trueHeight = (int)(GUIManager.GetInstance().Height * height);
           	float halfW = trueWidth/2;
           	float halfH = trueHeight/2;
           	float screenWidthHalf = GUIManager.GetInstance().Width;
           	float screenHeightHalf = GUIManager.GetInstance().Height;
	
           	area = new Rect(screenWidthHalf - halfW, screenHeightHalf - halfH, screenWidthHalf + halfW, screenHeightHalf + halfH);

       	}
       	else
       	{
            int trueX = (int)(GUIManager.GetInstance().Width * x);
           	int trueY = (int)(GUIManager.GetInstance().Height * y);
           	int trueWidth = (int)(GUIManager.GetInstance().Width * width);
           	int trueHeight = (int)(GUIManager.GetInstance().Height * height);
           	area = new Rect(trueX, trueY, trueWidth, trueHeight);
		}
	}
			
	protected virtual void ResetAbsolutePosition( bool force )
	{
		// default the area to the screen size so children always show up
		area = new Rect(0,0,GUIManager.GetInstance().Width,GUIManager.GetInstance().Height);
		
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
				// no style means we are relying on xywh coords
				if ( anchor == Anchor.relative )
				{
					// relative means fraction of screen w/h
					anchorX = x * GUIManager.GetInstance().Width;
					anchorY = y * GUIManager.GetInstance().Height;
				}
				else
				{
					// default case
					anchorX = x;
					anchorY = y;
				}
			}
			
			// do anchor types
			switch( anchor )
			{
			case Anchor.relative:
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
	        if (_centered)
	        {
				area = new Rect(GUIManager.GetInstance().Width/2-width/2,GUIManager.GetInstance().Height/2-height/2,width,height);
	        }
			else
			{
				// not sure why i have to do this!
				// TODO!! figure out why!
				if ( force == true )
           		    area = new Rect(x, y, width, height);
			}
		}

		// add drag offset
		if ( draggable == true )
		{
			Vector2 invDrag = GUIManager.GetInstance().GUIMatrix.inverse.MultiplyVector(drag);
			area.x += invDrag.x;
			area.y += invDrag.y;
		}
	}

    protected virtual void ResetPosition(bool force=true)
    {
        if (_relative)
			ResetRelativePosition();
		else
			ResetAbsolutePosition(force);

		//UnityEngine.Debug.LogError("***** GUIScreen.ResetPosition(" + this.name + ") : area=<" + area.x + "," + area.y + "," + area.width + "," + area.height + "> : name=" + name);
    }
	
	Rect saveStyle;	
	bool scaled=false;
	Rect originalRect = new Rect(-1,-1,-1,-1);
	
	public virtual void Scale()
	{
		// never scale if we don't have to
		if ( scaleX == 1.0f && scaleY == 1.0f )
			return;
		
		if ( scaled == true )
			return;
		scaled = true;
		
		if ( _style != null )
		{
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
		}
		else
		{
			saveStyle.x = x;
			saveStyle.y = y;
			x *= scaleX;
			y *= scaleY;
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
		}
		else
		{
			x = saveStyle.x;
			y = saveStyle.y;
		}
	}

	
	public virtual bool MouseOverGUI( Vector2 position )
	{
		// check all elements looking for areas
		foreach( GUIObject obj in Elements )
		{
			GUIArea childArea = obj as GUIArea;
			if ( childArea != null )
			{
				// offset is parent area
				if ( childArea.MouseOverGUI(position,area) == true )
					return true;
			}
		}
		return false;
	}
	
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

	public virtual void FadeOpen( float time )
	{
		Fade (1.0f,time,null,0.0f);
	}

	public virtual void FadeClose( float time )
	{
		Fade (0.0f,time,FadeCloseCallback);
	}

	public virtual void FadeCloseCallback()
	{
		Close();
	}

	public virtual bool FadeDone()
	{
		if ( fader == null )
			return true;
		return fader.FadeDone ();
	}

	public virtual void Execute()
    {
		if ( visible == false )
			return;

		FadeIn ();

        // Create an Area or a Window based on options set
        if(windowed)
        {
        	// Update 
			Scale();		
			ResetPosition(false);	

			// this calls embedded DoWindow handler for GUI
            if (_style != null)
			{
                GUI.Window(windowID, area, DoWindow, "", _style);
			}
            else
                GUI.Window(windowID, area, DoWindow, "", GUI.skin.label);

			Unscale();
		}
        else
        {
        	// Update 
			Scale();		
			ResetPosition(false);	
		
			if ( fillarea == true && _style != null )
			{
				GUI.skin = _skin;
				GUI.Box(area,new GUIContent(),_style);
			}
			
			// normal area loop
            if (!_blank)
            {
                if (_style != null)
                    GUILayout.BeginArea(area, _style);
                else
                    GUILayout.BeginArea(area);
            }

        	foreach (GUIObject obj in Elements)
            	obj.Execute();
			
            if(!_blank)
                GUILayout.EndArea();

			Unscale ();
        }	

		FadeOut ();
    }
	
	public virtual GUIButton CheckButtons( Vector2 position )
	{
		foreach( GUIObject obj in Elements)
		{
			GUIArea area = obj as GUIArea;
			if ( area != null )
			{
				GUIButton button = area.CheckButtons(area.GetArea(),position);
				if ( button != null )
					return button;
			}
		}
		return null;
	}

	// allow screens to update
	public virtual void Update()
	{
		DoDragging();
	}
	
	public virtual void LateUpdate()
	{
	}

	Vector2 lastDrag;
	float lastDragX=0;
	float lastDragY=0;
	bool dragging = false;

	void DoDragging()
	{
		if ( draggable == false )
			return;

		if ( Input.GetMouseButtonDown(0) == true )
		{
			if ( dragging == false )
			{
				// flip y....weird that GUI origin is flipped
				Vector2 position;
				position.x = Input.mousePosition.x-GUIManager.GetInstance().FracX;
				position.y = Screen.height-Input.mousePosition.y-GUIManager.GetInstance().FracY;
				
				// save area
				Rect saveArea = this.area;
				
				// multiply position by GUI matrix (for scaling)
				this.area = GUIManager.GetInstance().GUITransformRect(area);
				
				// check for drag area...if nothing then allow the whole window
				GUIArea dragArea = Find("dragArea") as GUIArea;
				if ( dragArea != null )
				{
					// check to see if the mouse is in this area
					Rect screenRect = new Rect(Area.x+dragArea.Style.contentOffset.x,Area.y+dragArea.Style.contentOffset.y,dragArea.Style.fixedWidth,dragArea.Style.fixedHeight);
					if ( screenRect.Contains(position) == false )
						return;
				}
				else
				{
					// check window area
					if ( Area.Contains(position) == false )
						return;
				}
				
				// restore Area
				this.area = saveArea;
				
				//lastDrag = GUIManager.GetInstance().GUIMatrix.inverse.MultiplyVector(Input.mousePosition);
				//lastDrag = Input.mousePosition;
				lastDrag = Input.mousePosition;
				//lastDragX = Input.mousePosition.x;
				//lastDragY = Input.mousePosition.y;
				dragging = true;
			}
		}
		if ( Input.GetMouseButtonUp(0) == true )
		{
			dragging = false;
		}
		if ( dragging == true )
		{
			Rect screenRect = new Rect(0,0,GUIManager.GetInstance().Width,GUIManager.GetInstance().Height);
			if ( screenRect.Contains(Input.mousePosition) )
			{
				drag.x += (Input.mousePosition.x - lastDrag.x);
				drag.y += (lastDrag.y - Input.mousePosition.y);
				ResetPosition(true);			
				lastDrag = Input.mousePosition;
				//lastDragX = Input.mousePosition.x;
				//lastDragY = Input.mousePosition.y;
			}
		}
	}
	
	void DoWindow(int id)
	{	 
        // first get a control id. every subsequent call to GUI control function will get a larger id 
        int min = GUIUtility.GetControlID(FocusType.Native);
        // we can use the id to check if current control is inside our window
        if (GUIUtility.hotControl < min)
            SetHotControl(0); //if it's not - set hot control to 0, to allow window controls to become hot

        GUI.FocusWindow(id);
        GUI.BringWindowToFront(id);		

		GUI.skin = _skin;

		if ( fillarea == true && _style != null )
		{
			GUI.skin = _skin;
			GUI.Box(area,new GUIContent(),_style);
		}
		
        foreach (GUIObject obj in Elements)
            obj.Execute();

		Unscale ();
		
#if FIXED
		// NOTE!! this will break rollover state buttons if left in...
				
        //once again check current hot control
        //if it's outside our window - set it to -1 - it prevent's clicks!
        //we can't block clicks inside our window, so we have to check max!=-1
        //max equals -1 if a control inside our window has taken focus in this frame, because
        //we can't get a valid id in this frame any more

        int max = GUIUtility.GetControlID(FocusType.Native);
        if (GUIUtility.hotControl < min || (GUIUtility.hotControl > max && max != -1))
          	SetHotControl(-1);
#endif

		GUI.DragWindow (new Rect (0,0,10000,10000));
		
		// force cursor draw over window
		GUIManager.GetInstance().DrawCursor();
		GUIManager.GetInstance().DrawCurtain();
		GUIManager.GetInstance().DrawDebugInfo();
		
	}

    protected void SetHotControl(int id)
    {
        if (new Rect(0, 0, GUIManager.GetInstance().Width, GUIManager.GetInstance().Height).Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)))
            GUIUtility.hotControl = id;
    }

    public virtual void ButtonCallback(GUIButton button)
    {
		if ( button.closeWindow == true )
        	GUIManager.GetInstance().Remove(parent);
			
        switch (button.name.ToLower())
        {
            case "close":
                {
                    GUIManager.GetInstance().Remove(parent);
                }
                break;
        }
    }

    public virtual void ButtonMessages(GUIButton button)
    {
        button.SendMessages();
    }
	
	public virtual void Close()
	{
		GUIManager.GetInstance().Remove(this.Parent);
	}

    public virtual void OnClose() 
	{
		// call OnClose for all elements
		foreach( GUIObject item in Elements )
			item.OnClose();
	}
}

[System.Serializable]
public class ScreenInfo
{
    public List<GUIScreen> Screens;

    protected bool windowed = false;
    protected int activeScreen = 0;
    public bool isModal = false;
	
	public virtual void Update()
	{
		if ( Screen != null )
			Screen.Update();
	}

    public GUIScreen Screen
    {
        get
        {
			if (Screens == null)
				return null;
            if (activeScreen < Screens.Count)
                return Screens[activeScreen];
            return null;
        }
    }

    public bool Windowed
    {
        get { return windowed; }
        set { windowed = value; }
    }

    public void LastScreen()
    {
        if (activeScreen != 0)
            activeScreen--;
    }

    public void NextScreen()
    {
        if (activeScreen < Screens.Count - 1)
            activeScreen++;
    }

    public void SetScreenTo(int index)
    {
        if (index >= 0 && index < Screens.Count)
            activeScreen = index;
    }

    public void SetScreenTo(string name)
    {
        for (int i = 0; i < Screens.Count; i++)
        {
            if (Screens[i].name.ToLower() == name.ToLower())
            {
                SetScreenTo(i);
                break;
            }
        }
    }
	
	public void ConvertToGUIScreen()
	{
		for (int i=0 ; i<Screens.Count ; i++)
		{
			GUIScreen screen = new GUIScreen();
			screen.CopyFrom(Screens[i]);
			Screens[i] = screen;
		}
	}

    public void Initialize(string forceType=null)
    {
        List<GUIScreen> morph = new List<GUIScreen>();
        foreach (GUIScreen screen in Screens)
        {
			string type = screen.type;			
			// check to see if we're forcing a screen type
			if (forceType != null)
				type = forceType;

			// create it
            if (type != null && type.Length > 0)
            {
                System.Type screenType = System.Type.GetType(type);
                if (screenType != null)
                {
                    GUIScreen newScreen = System.Activator.CreateInstance(screenType) as GUIScreen;
                    newScreen.CopyFrom(screen);
                    morph.Add(newScreen);
                }
                else
                {
                    Debug.LogWarning("GUIScreen -" + screen.name + "- Type not found. Defaulting to GUIScreen type.");
                    morph.Add(screen);
                }
            }
            else
                morph.Add(screen);
        }

        Screens = morph;

        foreach (GUIScreen screen in Screens)
            screen.Initialize(this);
    }

    public void Execute()
    {
		if ( Screen != null )
		{
	        Screen.Windowed = windowed;
        	Screen.Execute();
		}
    }

    public static ScreenInfo Load(string fileName)
    {
        Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
        ScreenInfo screen = serializer.Load(fileName);

        return screen;
    }

    public static ScreenInfo LoadFromDisk(string fileName)
    {
        Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
		StreamReader reader = new StreamReader(fileName);
        ScreenInfo screen = serializer.Load(reader);

        return screen;
    }

    public void SetModal()
    {
        GUIManager manager = GUIManager.GetInstance();
        if (manager != null)
            manager.SetModal(this);
    }

    public GUIScreen FindScreen(string name)
    {
        foreach (GUIScreen screen in Screens)
        {
            if (screen.name == name)
                return screen;
        }
        return null;
    }

    public GUIScreen FindScreenByType<T>()
    {
        foreach (GUIScreen screen in Screens)
        {
            if (screen.GetType() == typeof(T))
                return screen;
        }
        return null;
    }

    public GUIScreen FindScreenByType(System.Type type)
    {
        foreach (GUIScreen screen in Screens)
        {
            if (screen.GetType() == type)
                return screen;
        }
        return null;
    }

    public void OnClose()
    {
        foreach (GUIScreen screen in Screens)
        {
            screen.OnClose();
        }
    }

    public void AddScreen(GUIScreen screen)
    {
		if ( Screens == null )
			Screens = new List<GUIScreen>();
		
        Screens.Add(screen);
    }

    public void RemoveScreen(GUIScreen screen)
    {
        Screens.Remove(screen);
    }
}

public class GUIDialog : GUIScreen
{
    private bool visible = false;
	
	public delegate void GUIDialogCallback(string status);
    public GUIDialogCallback DialogCallback;

    public bool Visible
    {
        get { return visible; }
        set
        {
            visible = value;
            if (visible == false && parent.isModal)
                GUIManager.GetInstance().ClearModal();
        }
    }

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
    }

    public override void ButtonCallback(GUIButton button)
    {
		// send default event to dialog callback
        if ( DialogCallback != null )
			DialogCallback("button=" + button.name);
		
		// allow default handler to run
        base.ButtonCallback(button);
    }

    public virtual void Load(DialogMsg msg)
	{
        // parse paramters
		ParseParamString (msg.arguments);
        // Set Dialog Callback
        DialogCallback = msg.callback;
		// reset position first time
		ResetPosition(true);
	}
}

public class GUIMoviePlayer : GUIScreen
{
    protected class MPControl
    {
        public GUIMovie movie;
        public GUIToggle toggleButton;
    }

    MPControl[] movies;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

        List<GUIObject> objs = FindObjectsOfType(typeof(GUIMovie));
        movies = new MPControl[objs.Count];
        int i = 0;
        foreach (GUIObject obj in objs)
        {
			movies[i] = new MPControl();
            movies[i].movie = obj as GUIMovie;
            i++;
        }

        objs = FindObjectsOfType(typeof(GUIToggle));
        foreach(GUIObject obj in objs)
        {
            GUIToggle toggle = obj as GUIToggle;
            if(obj.name.ToLower() == "play")
            {
				int player = 0;
                if (toggle.parameter != null && toggle.parameter.Length > 0)
                {
                    player = System.Convert.ToInt32(toggle.parameter);
                }
				
				if(player >= 0 && player < movies.Length)
                {
                        movies[player].toggleButton = toggle;
				}
            }
        }
    }

    public override void Execute()
    {
 	    base.Execute();

        foreach(MPControl movie in movies)
        {
            if(movie.toggleButton != null)
            {
                movie.toggleButton.toggle = movie.movie.IsPlaying();
            }
        }
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name.ToLower())
        {
            case "play":
                {
                    int player = 0;
                    
                    if (button.parameter != null && button.parameter.Length > 0)
                        player = System.Convert.ToInt32(button.parameter);

                    GUIToggle toggle = button as GUIToggle;
                    if(toggle != null)
                    {
                        if(movies[player].movie.IsPlaying())
                            Pause(player);
                        else
                            Play(player);
                    }
                    else
                    {
                        Play(player);
                    }
                }
                break;

            //case "pause":
            //    {
            //        if (button.parameter != null && button.parameter.Length > 0)
            //        {
            //            Pause(System.Convert.ToInt32(button.parameter));
            //        }
            //        else
            //            Pause(0);
            //    }
            //    break;

            case "stop":
                {
                    int player = 0;
                    
                    if (button.parameter != null && button.parameter.Length > 0)
                    {
                        player = System.Convert.ToInt32(button.parameter);
                    }

                    Stop(player);
                }
                break;
        };
    }

    public override void OnClose()
    {
        base.OnClose();

        foreach (MPControl movie in movies)
            movie.movie.Stop();
    }

    void Play(int index)
    {
        if (index >= 0 && index < movies.Length)
            movies[index].movie.Play();
    }

    void Pause(int index)
    {
        if (index >= 0 && index < movies.Length)
            movies[index].movie.Pause();
    }

    void Stop(int index)
    {
        if (index >= 0 && index < movies.Length)
            movies[index].movie.Stop();
    }
}
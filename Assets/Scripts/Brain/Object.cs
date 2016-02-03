//#define DEBUG_OBJECT
//#define DEBUG_OBJECT_MSGS

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BaseObject : MonoBehaviour
{
    public string Name = "BaseObject";
    public bool Enabled = true;
    public float elapsedTime = 0;
	public string[] startingAttributes = new string[0];		// visible in the editor

    public Dictionary<string,string> attributes = new Dictionary<string, string>();	// attribute value pairs for keeping state
    Dictionary<string,DecisionVariable> decisionVariables = new Dictionary<string,DecisionVariable>();  // decision variable attribute pairs
	
/* http://answers.unity3d.com/questions/32413/using-constructors-in-unity-c.html
    public BaseObject()
    {
		Name = name;
        Enabled = true;
        elapsedTime = 0.0f;
    }
*/

    virtual public void Awake(){
//Debug.Log ("Object.Awake for Name="+Name+" name="+name);
		
		// I've added registration to ALL objects because I needed to find an object, but this may be too many
		// to manage.  If so, we cal comment out and force the needed object to register?  
		// perhaps each instance needs a 'register' flag, like the scripted objects have.
		
        ObjectManager.GetInstance().RegisterObject(this); // this was commented out, is it too heavy this way ?
        elapsedTime = 0.0f;
		foreach (string s in startingAttributes)
		{
			if (s.Contains("=")){
				string[] p = s.Split ('=');
				SetAttribute(p[0],p[1]);
				
			}
			else
			{
				SetAttribute (s);
			}
		}
    }

    virtual public void Start()
    {
//Debug.Log ("Object.Start for Name="+Name+" name="+name);
    }

    virtual public void Update()
    {
        elapsedTime += Time.deltaTime;
    }

    virtual public void PutMessage(GameMsg msg)
    {
    }

	public DecisionVariable GetDecisionVariable( string name )
	{
		if ( decisionVariables.ContainsKey(name) == true )
			return decisionVariables[name];
		else
		{
			if ( AddDecisionVariable(name) == true )
				return decisionVariables[name];
		}
		return null;
	}
	
	public bool HasAttribute(string testString){
		// this can be either a single word, or an =, < or >
		char[] delims = new char[]{'=','<','>'}; // <= would give us trouble here...
		string[] tokens = testString.Split(delims);

		// if the string contains a % then the string is referring to a decision variable
		if (testString.Contains("%"))
            return(decisionVariables.ContainsKey(tokens[0]));
        else
			return(attributes.ContainsKey(tokens[0])); 
	}

    bool AddDecisionVariable(string key)
    {
        // dictionary doesn't contain this key so try to make one
        // form name, decision variable needs the object name
        DecisionVariable v = new DecisionVariable(this.Name + "." + key.Substring(1), true);
        if ( v != null && v.Valid == true )
        {
            decisionVariables.Add(key,v);
            return true;
        }
        else
        {
            UnityEngine.Debug.LogError("Object.GetAttribute(" + key + ") : can't create decision variable");
            return false;
        }
    }

    public string GetAttribute(string key)
    {
        if ( key.Contains("%"))
        {
            // this is a DecisionVariable
            if ( decisionVariables.ContainsKey(key) == false )
            {
                // try to add variable
                if ( AddDecisionVariable(key) == false )
                    return "";
            }

            // get the variable value
            if ( decisionVariables.ContainsKey(key) )
            {
                // get the variable
                DecisionVariable v = decisionVariables[key];
                if ( v != null )
				{
                    //UnityEngine.Debug.LogError("Object.GetAttribute(" + key + ") : v=" + v.Get());
                    return v.Get();//v.GetTypeCharacter() + v.Get();
				}
				else
                {
                    UnityEngine.Debug.LogError("Object.GetAttribute(" + key + ") : can't find reflected variable");
                    return "";
                }
            }
            else
                return "";
        }
        else
        {
			string val = "";
			if (attributes.ContainsKey(key))
				val = attributes[key];
			// to be symmetric here, since SetAttribute added the prefix character, should we remove it ?
			// I realize this makes the prefix character pretty useless at this time...PAA
			if (val != null && val.Length > 1) val = val.Substring(1); // strip off the type signifier character
            return val;
        }
    }

    public string MakeAttributeValue( string input )
    {
        // a valid float, int, or boolean constant? - return "f1.5" "i1" "btrue"
        float fVal;
        int iVal;
        bool bVal;
		if (int.TryParse(input,out iVal)){
            return "i"+iVal.ToString();
        }
        if (float.TryParse(input,out fVal)){
            return "f"+fVal.ToString();
        }
        if (bool.TryParse(input,out bVal)){
            return "b"+bVal.ToString();
        }
        // none of the above, just return "s" + string
        return "s" + input.Replace ("\"","").ToLower ();
    }

	public void SetAttribute(string key, string newValue="")
    {
        if ( key.Contains("%") )
        {
            // this is a DecisionVariable
            if ( decisionVariables.ContainsKey(key) == false )
            {
                // try to add variable
                if ( AddDecisionVariable(key) == false )
                    return;
            }

            // get the variable value
            if ( decisionVariables.ContainsKey(key) )
            {
                // get the variable
                DecisionVariable v = decisionVariables[key];
                if ( v != null )
                    v.Set(newValue);
                else
                {
                    UnityEngine.Debug.LogError("Object.GetAttribute(" + key + ") : can't find reflected variable");
                    return;
                }
            }
            else
                return;
        }
        else 
        {
            // normal attribute (not decision variable)
		    attributes[key] = MakeAttributeValue(newValue); // accessing this way will add or replace k,v
        }
	}

	public void RemAttribute(string key){
		attributes.Remove(key);
	}
	public void SetAttributes(string attributeExpression){
		// adds, removes or sets multiple attributes from a space delimited string
		// +prefix to add -prefix to remove or key=value is required in each opeation
		string[] tokens = attributeExpression.Split (' ');
		foreach (string s in tokens){
			if (s.Length > 0){	
				if (s[0] == '+'){
					SetAttribute(s.Substring(1),"");
				}
				else if (s[0] == '-'){
					RemAttribute(s.Substring(1));
				}
				else if (s.Contains("=")){
					string[]p = s.Split ('=');
					SetAttribute(p[0],p[1]);
				}
			}
		}
	}
	
#if UNITY_EDITOR
	public void ShowDecisionVariables(){
		Color restore = GUI.color;
		GUI.color = Color.yellow;
		if (decisionVariables.Count > 0){
			GUILayout.Label("Decision Variables:");
			foreach (KeyValuePair<string,DecisionVariable> kvp in decisionVariables){
				GUILayout.BeginHorizontal();
				GUILayout.Label(kvp.Key);	
				GUILayout.Label((kvp.Value as DecisionVariable).Get());
				GUILayout.EndHorizontal();
			}
		}	
		else{
			GUI.color = Color.yellow;
			GUILayout.Label("No Decision Variables:");
		}
		GUI.color = restore;
	}
#endif	
	
}

public class ColorControl
{
    protected Color currColor;
    protected Color seekColor;
    protected Color startColor;

    protected float startTime;
    protected float blendTime;
    protected float endTime;
    protected float currTime;

    public bool Dirty = false;

    public ColorControl()
    {
        startColor = currColor = seekColor = RenderSettings.ambientLight;
    }

    public ColorControl(Color color)
    {
        startColor = currColor = seekColor = color;
    }

    public virtual void Update()
    {
        if (currColor != seekColor)
        {
            currTime += Time.deltaTime * 3.0f;
            if (currTime > blendTime)
                currTime = blendTime;

            currColor = seekColor + (startColor - seekColor) * (blendTime - currTime);
            Dirty = true;
        }
    }

    public Color Color
    {
        get { return currColor; }
        set
        {
            SetColor(value, 1.0f);
        }
    }

    public virtual void SetColor(Color color, float time)
    {
        seekColor = color;

        if (time == 0.0f)
        {
            currColor = color;
        }
        else
        {
            startColor = currColor;
            blendTime = time;
            currTime = 0.0f;
        }

        Dirty = true;
    }

    public virtual void SetAlpha(float alpha, float time)
    {
        Color newcolor = currColor;
        newcolor.a = alpha;
        SetColor(newcolor, time);
    }
}

public class Object3D : BaseObject
{
    public ColorControl ColorControl;
    public Color selectedColor = new Color(0.7f, 0.7f, 1f, 1.0f);
    public Color deselectedColor = new Color(.75f, .75f, .75f, 1.0f);
    protected bool highlight;
    public float ActivateDistance = 0.0f;
/*
    public Object3D() : base()
    {
//	        Name = "Object3D";
    }
*/
    public override void Start()
    {
        base.Start();

        if (ColorControl == null)
            ColorControl = new ColorControl();
    }

    public virtual void SetColor(Color color, float time)
    {
		if ( ColorControl != null )
        	ColorControl.SetColor(color, time);    
    }

    public virtual void SetAlpha(float alpha, float time)
    {
		if ( ColorControl != null )
	        ColorControl.SetAlpha(alpha, time);
    }

    protected virtual void UpdateColor()
    {
		if ( ColorControl == null )
			return;
		
        // update color
        ColorControl.Update();

        // set object color
        if (ColorControl.Dirty == true)
        {
            // reset flag
            ColorControl.Dirty = false;
            // set object color
            ObjectManager.GetInstance().SetColor(gameObject, ColorControl.Color);
        }
    }
	
    public virtual void HighlightObject(bool yesno)
    {
        NameTag nameTag = gameObject.GetComponentInChildren<NameTag>();
        if (nameTag != null)
        {
            nameTag.gameObject.renderer.enabled = yesno;
        }

		IconTag iconTag = gameObject.GetComponentInChildren<IconTag>();
		if ( iconTag != null )
		{
			iconTag.gameObject.renderer.enabled = yesno;
		}

        if (yesno == true)
        {
            if (highlight == true)
                return;

            if (IsActive() == true && Enabled == true)
            {
				if ( ColorControl != null )
                	ColorControl.SetColor(selectedColor, 0.5f);
                //Brain.GetInstance().PlayAudio("OBJECT:INTERACT:HOVER");
                highlight = true;
            }
        }
        else
        {
            if (highlight == false)
                return;

            if (IsActive() == true && Enabled == true)
            {
				if ( ColorControl != null )
	                ColorControl.SetColor(deselectedColor, 0.5f);
                highlight = false;
            }
        }
    }

    public bool IsSelected()
    {
        return highlight;
    }

    virtual public bool IsActive()
    {
        if(DialogMgr.GetInstance() != null)
            return !DialogMgr.GetInstance().IsModal();
        return false;
    }

    protected bool WithinRange()
    {
        // check distance to main camera
        GameObject myCamera = GameObject.FindGameObjectWithTag("MainCamera");
		if ( myCamera == null )
		{
			// try current camera
			if ( Camera.main != null )
				myCamera = Camera.main.gameObject;
			
			if ( myCamera == null )
			{
				// try first camere
				if ( Camera.allCameras.Count() > 0 )
					myCamera = Camera.allCameras[0].gameObject;
			}
		}
		
        if (myCamera != null && gameObject != null)
        {
            // check distance
            float distance = Vector3.Distance(myCamera.transform.position, gameObject.transform.position);
            if (distance != 0.0f && distance > ActivateDistance)
            {
                //Debug.Log("Too Far:" + distance);
                return false;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

	bool recheckHighlight;

	public override void Update()
	{
		if ( highlight == true && GUIManager.GetInstance().MouseOverGUI(Input.mousePosition) == true )
		{
			// going into a GUI element, turn off object
			OnMouseExit ();
			recheckHighlight = true;
		}
		if ( recheckHighlight == true && highlight == false && GUIManager.GetInstance().MouseOverGUI(Input.mousePosition) == false )
		{
			// recheck on way out
			RaycastHit hitInfo;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if ( Physics.Raycast(ray,out hitInfo) )
			{
				hitInfo.collider.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
				recheckHighlight = false;
			}
		}
		
		base.Update();
		UpdateColor();
	}

#if !UNITY_ANDROID
    virtual public void OnMouseEnter()
    {
        if (ObjectInteractionMgr.GetInstance().Highlight == false)
            return;

        if (WithinRange() == false)
            return;

        HighlightObject(true);
    }

    virtual public void OnMouseExit()
    {
        HighlightObject(false);
	}
#endif
}

public class ObjectManager
{
    static ObjectManager instance;
    static public ObjectManager GetInstance()
    {
        if (instance == null)
            instance = new ObjectManager();

        return instance;
    }

    public void Init()
    {
        objects = null;
        objectmap = null;
    }

    List<BaseObject> objects;
    Dictionary<string, BaseObject> objectmap;

    public List<BaseObject> GetObjectList()
    {
        if (objectmap != null)
            return objectmap.Values.ToList();
        return null;
    }

    public void RegisterObject(BaseObject obj)
    {
        if (obj == null)
        {
            UnityEngine.Debug.Log("ObjectManager.RegisterObject() : obj is NULL");
            return;
        }

        if (obj.Name == null)
        {
            UnityEngine.Debug.Log("ObjectManager.RegisterObject() : obj Name is NULL. using name "+obj.name);
			obj.Name = obj.name;
//            return;
        }
		
        // add to list
        if (objects == null)
            objects = new List<BaseObject>();
		
		if (objects.Contains (obj)){
//			UnityEngine.Debug.LogWarning("object tried to register twice "+obj.name);
			return;
		}
		
		// don't register objects if their Scripted Object component says not to.
		// this is fallout from having ALL base objects register.
		if (obj.GetComponent<ScriptedObject>() != null &&
			obj.GetComponent<ScriptedObject>().register == false)
			return;
		
        objects.Add(obj);

        // add to dictionary
        if (objectmap == null)
            objectmap = new Dictionary<string, BaseObject>();
        objectmap[obj.Name.ToLower()] = obj;
#if DEBUG_OBJECT
        UnityEngine.Debug.Log("ObjectManager.RegisterObject(" + obj.Name+" "+obj.name + ")"+objectmap.Count);
#endif
		Dispatcher.GetInstance().RegisterObject(obj);
    }

    public BaseObject GetBaseObject(string name)
    {
        if (objectmap == null)
            return null;
        if (name == null)
            return null;

        if (objectmap.ContainsKey(name.ToLower()))
            return objectmap[name.ToLower()];
        else
            return null;
    }
	
	public BaseObject GetBaseObject(GameObject obj)
	{
		foreach( BaseObject baseObj in objectmap.Values )
		{
			if ( baseObj.gameObject == obj )
				return baseObj;
		}
		return null;
	}

    public GameObject GetGameObject(string name)
    {
        BaseObject obj = GetBaseObject(name);
        if (obj)
        {
            return obj.gameObject;
        }
        else
            return null;
    }

    // global enable
    public void EnableObjects(bool yesno)
    {
        foreach (BaseObject obj in objects)
        {
            obj.Enabled = yesno;
        }
    }

    // broadcast
    public void PutMessage(GameMsg msg)
    {
        if (objects == null)
            return;

        foreach (BaseObject obj in objects)
        {
#if DEBUG_OBJECT_MSGS
			InteractMsg imsg = msg as InteractMsg;
            if (imsg != null)
                Debug.Log("ObjectManager.PutMessage(" + imsg.map.item + ") : object=" + obj.ToString());
            else
                Debug.Log("ObjectManager.PutMessage(" + msg.GetType().ToString() + ") : object=" + obj.ToString());
#endif
            //UnityEngine.Debug.Log("ObjectManager.PutMessage(" + msg.GetType() + ") : obj=" + obj.Name);

            if (obj.enabled == true && obj.gameObject.active == true)
            {
#if DEBUG_OBJECT
                UnityEngine.Debug.Log("ObjectManager.PutMessage(" + msg.GetType() + ") : obj=" + obj.Name); 
#endif
                obj.PutMessage(msg);
            }
        }
    }

    // enable object in manager
    public void EnableObject(string name, bool yesno)
    {
        BaseObject obj = objectmap[name];
        if (obj != null)
        {
            EnableObject(obj.gameObject, yesno);
        }
        else
            Debug.LogError("ObjectManager.EnableObject(" + name + "): can't find object");
    }

    // enable any object
    public void EnableObject(GameObject obj, bool enabled)
    {
        if (obj != null)
        {
            if (enabled)
                obj.SetActiveRecursively(true);

            Renderer[] renderers = obj.GetComponents<Renderer>();
            if (renderers != null)
                foreach (Renderer render in renderers)
                    render.enabled = enabled;
            renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers != null)
                foreach (Renderer render in renderers)
                    render.enabled = enabled;


            ToggleObject[] tObjects = obj.GetComponentsInChildren<ToggleObject>();
            if (tObjects != null)
            {
                foreach (ToggleObject tObject in tObjects)
                {
                    Transform[] ts = tObject.GetComponentsInChildren<Transform>();
                    int len = ts.Length;

                    for (int i = 0; i < len; i++)
                    {
                        MeshRenderer render = ts[i].gameObject.GetComponent<MeshRenderer>();
                        if (render != null)
                            render.enabled = tObject.GetState();
                    }
                }
            }

            if (!enabled)
                obj.SetActiveRecursively(false);
        }
        else
            Debug.LogError("Brain.SetObject: Cannot find object:");
    }

    public void SetColor(GameObject obj, Color color)
    {
        // change color
        if (obj.renderer != null)
        {
            // set color
            obj.renderer.material.color = color;
        }

        // get children
        Renderer[] children = obj.GetComponentsInChildren<Renderer>();
        // set children
        if (children != null)
        {
            foreach (Renderer child in children)
            {
				float alpha;
				alpha = child.material.color.a;
				color.a = alpha;
                child.material.color = color;
            }
        }
    }
}

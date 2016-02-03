using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

[CustomEditor(typeof(GUIEdit))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class GUIEditInspector : Editor 
{
	protected bool onSelected = false;
	protected bool showDefaultInspector = false;
	protected string dialogName;
	GUIEdit myObject = null;
	GameObject screen;

	/*
    [XmlArrayItem(ElementName = "Area", Type = typeof(GUIArea))]
    [XmlArrayItem(ElementName = "Horizontal", Type = typeof(GUIHorizontalCommand))]
    [XmlArrayItem(ElementName = "Vertical", Type = typeof(GUIVerticalCommand))]
    [XmlArrayItem(ElementName = "Scrollview", Type = typeof(GUIScrollView))]
    [XmlArrayItem(ElementName = "Label", Type = typeof(GUILabel))]
    [XmlArrayItem(ElementName = "Editbox", Type = typeof(GUIEditbox))]
    [XmlArrayItem(ElementName = "Button", Type = typeof(GUIButton))]
    [XmlArrayItem(ElementName = "RepeatButton", Type = typeof(GUIRepeatButton))]
    [XmlArrayItem(ElementName = "Toggle", Type = typeof(GUIToggle))]
    */
	
	protected string guiName="";
	protected GUIEditObject.GUITypes guiType;
	
	public void Awake(){
	}
	
	virtual public void OnSelected() // look at a particular instance object		
	{
		myObject = target as GUIEdit;
		saveName = myObject.saveName;
		//base.OnSelected();
	}
	
	virtual public void OnDisable()
	{
		// i am trying to delete the loaded scene here on edit <del> but doesn't seem to work
		// get component in get GUIEditObject in screen
		//GUIManager.GetInstance().RemoveAllScreens();
	}	
		
	public override void OnInspectorGUI()
	{
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		
		Color oldColor = GUI.color;

		/*
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show Default Inspector");
		GUILayout.FlexibleSpace();
		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
		GUILayout.EndHorizontal();
		if (showDefaultInspector)
		{
			DrawDefaultInspector();
			return;
		}
		*/
		
		if ( GUIManager.GetInstance() == null )
		{		
			GUILayout.Label(">>>>>>>>>>>>>>>>>>");
			GUILayout.Label("GUIManager not found!!  Either the GUIManager is missing or you are trying to edit in RUN mode!!");
			GUILayout.Label(">>>>>>>>>>>>>>>>>>");
		}
		else
		{
			AddScreen();
			LoadObject();
			
			if ( GUILayout.Button("Clear GUIManager Screens") )
			{
				if ( GUIManager.GetInstance() )
				{
					GUIManager.GetInstance().RemoveAllScreens();
				}
			}
			
#if TEST_MYCLASS
			if ( GUILayout.Button("Add MyClass") )
			{
				GameObject newobj = new GameObject("myclass");
				if ( newobj != null )
				{
					newobj.AddComponent(typeof(Myclass));
				}
			}
#endif
			
			myObject.ShowInspectorGUI();
		}
		
		GUI.color = oldColor;		

		return;
	}
	
	bool toggleAdd=false;	
	bool addStatus=true;
	string addName="";

	public virtual void AddScreen()
	{
		//GUILayout.Label("--------------------------");
		
		// check button toggle
		toggleAdd = EditorGUILayout.Toggle("New Screen",toggleAdd);
		if ( toggleAdd == false )
		{
			//GUILayout.Label("--------------------------");
			return;
		}
		
		toggleLoad = false;
		
		guiName = EditorGUILayout.TextField("name",guiName);
		if ( guiName != null && guiName != "" )
		{
			GUILayout.BeginHorizontal();		
			if ( GUILayout.Button("Add Screen") )
			{
				AddBlankScreenInfo(guiName);
			}
			GUILayout.EndHorizontal();
		}
		
		//GUILayout.Label("--------------------------");
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
	
	public virtual GameObject BuildScreen( GUIScreen screen, GUIEditScreenInfo esi )
	{
		GameObject newobj = new GameObject(name);
		if ( newobj != null )
		{
			GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
			eo.guiScreen = screen;
			eo.name = screen.name;			
			eo.LoadedXML = loadedXML;
			eo.editSI = esi;
			// add all the objects below this
			foreach( GUIObject guiObj in screen.Elements )
				BuildObject(newobj, guiObj);	
			// set game object
			this.screen = newobj;
			// initialize (sets up all skins/styles)
			screen.Initialize(esi.ScreenInfo);
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
			eo.OrderElements();
			// set name of object
			newobj.name = "<" + eo.guiObject.ToString().Replace("GUI","") + ">" + newobj.name;
			// if this object is a container then add elements
			GUIContainer container = guiObj as GUIContainer;
			if ( container != null )
			{
				foreach( GUIObject go in container.Elements )
					BuildObject(newobj,go);
			}
		}		
	}

	bool toggleLoad=false;	
	bool loadStatus=true;
	string loadName="";
	string saveName="";
	protected string loadedXML="";
	
	public void LoadObject()
	{
		//GUILayout.Label("--------------------------");
		
		// check button toggle
		toggleLoad = EditorGUILayout.Toggle("Load Screen",toggleLoad);
		if ( toggleLoad == false )
		{
			//GUILayout.Label("--------------------------");
			return;
		}
		
		toggleAdd = false;

#if SHOW_LOAD_NAME
		loadName = EditorGUILayout.TextField("name",loadName);
#endif
		if ( GUILayout.Button("Load XML") )
		{
			if ( loadName == "" || loadName == null )
			{
				string path = EditorUtility.OpenFilePanel("Select file from GUIScripts!!...",PathSaver.Path,"xml");
				PathSaver.Path = path;
				loadName = path;
			}

			if ( loadName != "" && loadName != null )
			{
				// load the screen, force the type to GUIDialog so we don't try to run the real code
				ScreenInfo si = GUIManager.GetInstance().LoadFromDisk(loadName, "GUIDialog");
				UnityEngine.Debug.Log("GUIEditInspector.LoadObject() : loadName=<" + loadName + ">");
				loadedXML = loadName;
				saveName = Path.GetFileNameWithoutExtension(loadName);
				loadStatus = BuildScreenHeirarchy(loadName,si);
				// if loaded ok, then close the add area
				if ( loadStatus == true )
				{
					toggleLoad = false;
					myObject.saveName = saveName;
					// clear load name
					loadName = "";
				}
			}
			else
				loadName = "";
		}
		
		if ( loadName != "" && loadStatus == false  )
		{
			GUILayout.Label("Load Error <" + loadName + ">");
		}
		
		//GUILayout.Label("--------------------------");
	}
	
	public bool BuildScreenHeirarchy( string pathname, ScreenInfo si )
	{
		if ( si == null || si.Screens.Count == 0 )
			return false;
		
		string name = Path.GetFileNameWithoutExtension(pathname);
		
		GUIEditScreenInfo esi=null;
		
		// add game object for this heirarchy
		GameObject newobj = new GameObject(name + ".xml");
		if ( newobj != null )
		{
			esi = newobj.AddComponent(typeof(GUIEditScreenInfo)) as GUIEditScreenInfo;
			esi.ScreenInfo = si;
			esi.saveName = newobj.gameObject.name;
			esi.loadName = pathname;
		}
		
		// build only first screen (for now)
		if ( si.Screens.Count > 0 )
		{
			foreach( GUIScreen s in si.Screens )
			{
				// this will recursively build the whole structure
				GameObject go = BuildScreen(s,esi);	
				// handle parenting
				go.transform.parent = newobj.transform;
				go.transform.localPosition = Vector3.zero;				
			}
			// we're ok
			return true;
		}
		
		return false;
	}
}


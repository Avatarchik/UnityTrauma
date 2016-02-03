using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(GUIEditObject))]

public class GUIEditObjectInspector : GUIEditInspector
{
	GUIEditObject myObject;
	static GUIEditObject myObjectCopy;
	
	public GUIEditObjectInspector ()
	{
	}

	GUIEditWidget widget;

	virtual public void OnSelected() // look at a particular instance object		
	{
		// get target
		myObject = target as GUIEditObject;
		// show elements
		myObject.ShowElements();
		// show
		if ( GUIManager.GetInstance() )
		{
			// if guiScreen then bring to front
			if ( myObject.guiScreen != null )
			{
				if ( myObject.guiScreen.Parent == null )
					UnityEngine.Debug.LogError("GUIEditObjectInspector.OnSelected() : guiScreen.Parent == null");
				else
					myObject.guiScreen.Parent.SetScreenTo(myObject.guiScreen.name);
			}
			else
				GUIManager.GetInstance().SelectedGUIObject = myObject.guiObject;
			
			// attach this object to the GUIEditWigdet if available
			widget = GUIManager.GetInstance().gameObject.GetComponent<GUIEditWidget>();
			if ( widget != null )
			{
				widget.SetGUIObject(myObject.guiObject);
			}
		}
	}

	void Update()
	{
		if ( widget != null )
			widget.Update();
	}


	virtual public void OnDestroy()
	{
	}
	
	virtual public void OnDisable()
	{
		// I was hoping this would detect when a game object was being deleted
		// but i guess NOT!!
		return;

		/*
		// this object is being deleted
		if ( myObject != null && myObject.Parent != null )
		{
			// if the object has a parent we need to delete it from the parent's elements
			if ( myObject.Parent.guiScreen != null )
			{
				// this is a screen
				myObject.Parent.guiScreen.Elements.Remove(myObject.guiObject);
			}
			if ( myObject.Parent.guiObject != null )
			{
				// this is an object
				// must be a container
				GUIContainer container = myObject.Parent.guiObject as GUIContainer;
				if ( container != null )
					container.Elements.Remove(myObject.guiObject);
			}
		}
		*/
	}
	
	bool toggleAdd=false;	
	public void AddObject()
	{
		//GUILayout.Label("--------------------------");
		
		// check button toggle
		toggleAdd = EditorGUILayout.Toggle("Add Object",toggleAdd);
		if ( toggleAdd == false )
		{
			//GUILayout.Label("--------------------------");
			return;
		}

		toggleSave = false;
		
		guiName = EditorGUILayout.TextField("name",guiName);
		guiType = (GUIEditObject.GUITypes)EditorGUILayout.EnumPopup("type",(System.Enum)guiType);
			
		if ( guiName != null && guiName != "" )
		{
			GUILayout.BeginHorizontal();		
			if ( GUILayout.Button("Add Child GUI Object") )
			{
				// add new object
				GameObject newobj = new GameObject(guiName);
				if ( newobj != null )
				{
					// add edit object type
					GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
					// set type
					eo.SetType(guiName,guiType);
					// set parent
					eo.Parent = myObject;
					// grab parents skin
					if ( eo.Parent != null && eo.Parent.guiObject != null )
						eo.guiObject.SetSkin(eo.Parent.guiObject.Skin);
					if ( eo.Parent != null && eo.Parent.guiScreen != null )
						eo.guiObject.SetSkin(eo.Parent.guiScreen.Skin);
					// set as child of this object
					eo.BaseName = guiName;
					newobj.transform.parent = myObject.gameObject.transform;
					newobj.transform.localPosition = Vector3.zero;
					// add children
					myObject.AddChild(newobj);
					myObject.AddGuiElements(newobj);
					myObject.ShowElements();
					// reorder
					eo.OrderElements();
					// 
					toggleAdd = false;
				}
			}
			GUILayout.EndHorizontal();
		}
		
		//GUILayout.Label("--------------------------");
	}
	
	bool deleteOk=false;
	
	public void DeleteObject()
	{
		if ( GUILayout.Button("Delete Me (NO UNDO!!)") )
		{
			deleteOk=true;
		}
		if ( deleteOk == true )
		{
			GUILayout.BeginHorizontal();
			Color save = GUI.color;
			GUI.color = new Color(1.0f,0.6f,0.6f);
			
			if ( GUILayout.Button("Yes, really delete") )
			{
				deleteOk = false;
				
				// first delete this object from the parent
				GUIEditObject parent = myObject.Parent;
				
				if ( parent != null )
				{
					if ( parent.guiScreen != null )
					{
						// is a screen
						// remove my object from the screen
						parent.guiScreen.Elements.Remove(myObject.guiObject);
						// delete from list
						parent.DelChild(myObject.gameObject);
						// show elements
						parent.ShowElements();
					}
					if ( parent.guiObject != null )
					{
						// is a guiobject
						// remove my object from the screen
						GUIContainer area = parent.guiObject as GUIContainer;
						if ( area != null )
							area.Elements.Remove(myObject.guiObject);
						// delete from list
						parent.DelChild(myObject.gameObject);
						// show elements
						parent.ShowElements();
						// reorder the parent
						myObject.OrderElements ();
						// set guiObject to NULL
						myObject.guiObject = null;
						// now remove this game object			
						DestroyImmediate(myObject.gameObject);
						return;
					}
				}
				else
				{
					// this must be the top level....delete from ScreenInfo
					ScreenInfo si = myObject.editSI.ScreenInfo;
					si.RemoveScreen(myObject.guiScreen);
				}
				// now remove this game object			
				DestroyImmediate(myObject.gameObject);
			}
			GUI.color = save;
			if ( GUILayout.Button("No, don't delete!") )
			{
				deleteOk = false;
			}				
			GUILayout.EndHorizontal();
		}
	}
	
	public void OrderObject()
	{
		if ( GUILayout.Button("Bring to Front") )
		{
			myObject.MoveToFront();
			ReorderContainer(true);
		}
		if ( GUILayout.Button("Send to Back") )
		{
			myObject.MoveToBack();			
			ReorderContainer(true);
		}
	}
	
	public override void OnInspectorGUI()
	{
		if ( myObject == null )
			OnSelected();

		// check name of attached guiObject to make sure the basename matches
		if ( myObject.guiObject != null ) 
		{
			if ( myObject.guiObject.name != myObject.BaseName )
			{
				myObject.BaseName = myObject.guiObject.name;
				myObject.MakeName();
			}
		}

		// force dirty flag for all skins when editing
		if ( myObject.guiObject != null && myObject.guiObject.Skin != null )
			EditorUtility.SetDirty(myObject.guiObject.Skin);	
		
		// objects
		if (myObject.guiObject != null )
		{
			myObject.guiObject.ShowEditor();
			// only display defaults and add if object is a container
			GUIContainer container = myObject.guiObject as GUIContainer;
			if ( container != null )
			{
				AddObject();
				CopyObject();
				DuplicateStyle();
				PasteObject();
				DeleteObject();
				OrderObject();
				GUILayout.Label("-- Default Inspector --");
				DrawDefaultInspector();
			}
			else
			{
				GUILayout.Label("-- Default Inspector --");
				CopyObject();
				DuplicateStyle();
				DeleteObject();
				OrderObject();
				if ( myObject != null )
					DrawDefaultInspector();
			}
		}
		
		// screens
		if (myObject.guiScreen != null )
		{
			myObject.guiScreen.ShowEditor();
			AddObject();
			PasteObject();
			DeleteObject();
#if SAVE_SCREENS_HERE
			SaveObject();
#endif
			LoadObject();
			
#if SAVE_SCREENS_HERE
			if ( myObject.LoadedXML != null && myObject.LoadedXML != "" )
			{
				if ( GUILayout.Button("Save <" + myObject.LoadedXML + ".xml> (WARNING! only saves this screen)") )					
					EditorSaveXML("./Assets/Resources/GUIScripts/" + myObject.LoadedXML);
			}
#endif
			
			if ( GUILayout.Button("Add to GUIManager") )
			{
				myObject.LoadScreenInGUIManager();
			}
			if ( GUILayout.Button("Remove from GUIManager") )
			{
				myObject.RemoveFromGUIManager();
			}

			// default inspector at the end
			GUILayout.Label("-- Default Inspector --");
			DrawDefaultInspector();			
		}		
		
		myObject.ShowStyle();
	}
	
	public void CopyObject()
	{
		if ( GUILayout.Button("Copy Object") )
		{
			GUIManager.GetInstance().SetPasteObject(myObject);
		}
	}
	
	bool showDuplicateOptions=false;
	string duplicateName="";
	
	public void DuplicateStyle()
	{
		if ( GUILayout.Button("Duplicate Style") )
		{
			showDuplicateOptions = true;
			duplicateName = "autogen";
			//myObject.DuplicateStyle();
		}
		ShowDuplicateOptions();
	}
	
	void ShowDuplicateOptions()
	{
		if ( showDuplicateOptions == true )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label ("Duplicate Style Name ");
			duplicateName = EditorGUILayout.TextField(duplicateName);
			if ( GUILayout.Button("Ok") )
			{
				if ( duplicateName == "autogen" )
					duplicateName = null;
				myObject.DuplicateStyle(duplicateName);
				showDuplicateOptions = false;
			}
			if ( GUILayout.Button ("Cancel"))
				showDuplicateOptions = false;
			GUILayout.EndHorizontal();
		}
	}

	public void PasteObject()
	{
		// can't paste if this object isn't a container
		if ( myObject.guiObject != null)
			if ( myObject.guiObject as GUIContainer == null )
				return;

		GUIEditObject copyObj = null;
		copyObj = GUIManager.GetInstance().GetPasteObject();
		if ( copyObj == null )
			return;
		
		if ( GUILayout.Button("Paste Object") )
		{
			showPasteOptions = true;
		}
		ShowPasteOptions (copyObj);
	}

	void DoPaste( GUIEditObject copyObj, bool duplicate )
	{
		// first copy
		GUIEditObject eo = CopyObject(myObject,copyObj.guiObject,duplicate);
		// set name
		eo.BaseName = copyObj.BaseName;
		// add this to this object's elements
		if ( myObject.guiScreen != null )
			// parent is a screen
			myObject.guiScreen.Elements.Add(eo.guiObject);
		else
		{				
			// parent is a normal container
			GUIContainer container = myObject.guiObject as GUIContainer;
			if ( container != null )
				container.Elements.Add(eo.guiObject);
		}
		// let parent know new order
		eo.OrderElements();
		// reorder
		ReorderContainer();
	}

	bool showPasteOptions=false;
	void ShowPasteOptions(GUIEditObject copyObj)
	{
		if ( showPasteOptions == true )
		{
			Color save = GUI.color;
			GUI.color = Color.green;
			GUILayout.BeginHorizontal();
			if ( GUILayout.Button ("Paste With Same Styles"))
			{
				DoPaste (copyObj,false);
				showPasteOptions = false;
			}
			if ( GUILayout.Button ("Paste With Unique Styles"))
			{
				DoPaste (copyObj,true);
				showPasteOptions = false;
			}
			GUILayout.EndHorizontal();
			GUI.color = save;
		}
	}

	public virtual GUIEditObject CopyObject( GUIEditObject parentEO, GUIObject guiObj, bool duplicate, bool clone=true )
	{
		GameObject parent = parentEO.gameObject;		
		GameObject newobj = new GameObject(guiObj.name);
		if ( newobj != null )
		{
			GUIEditObject eo = newobj.AddComponent(typeof(GUIEditObject)) as GUIEditObject;
			// handle parenting
			newobj.transform.parent = parent.transform;
			newobj.transform.localPosition = Vector3.zero;
			// clone GUIObject at highest level, after that we have a duplicate of the whole guiobject
			// so we don't need to close on recursion
			if ( clone == true )
				eo.guiObject = guiObj.Clone();
			else
				eo.guiObject = guiObj;
			// duplicate the style (if any)
			if ( duplicate == true )
				eo.guiObject.DuplicateStyle();
			// do the rest
			eo.BaseName = guiObj.name;
			eo.name = guiObj.name;
			eo.Parent = parentEO;//parent.GetComponent<GUIEditObject>();
			eo.Parent.AddChild(newobj);
			eo.ShowElements();
			// add info to name
			newobj.name = "<" + eo.guiObject.ToString().Replace("GUI","") + ">" + newobj.name + " (copy)";
			// if this object is a container then add elements
			GUIContainer container = eo.guiObject as GUIContainer;
			if ( container != null )
			{
				foreach( GUIObject go in container.Elements )
					CopyObject(eo,go,duplicate,false);
			}
			eo.OrderElements();
			return eo;
		}		
		return null;
	}

	public virtual void ReorderContainer( bool useParent=false )
	{
		GUIEditObject target = myObject;
		if ( useParent == true )
			target = myObject.Parent;

		GUIContainer container = target.guiObject as GUIContainer;
		if ( container != null )
		{
			List<GUIEditObject> eoList = new List<GUIEditObject>();
			GUIEditObject[] children = target.GetComponentsInChildren<GUIEditObject>();

			// copy gameObjects
			for (int i=0 ; i<target.transform.childCount ; i++)
			{
				GUIEditObject item = target.transform.GetChild(i).GetComponent<GUIEditObject>();
				// save it
				eoList.Add (item);
			}
			// delete them all
			for (int i=0 ; i<target.transform.childCount ; i++)
			{
				//GameObject.DestroyImmediate(myObject.transform.GetChild(i).gameObject);
			}
			// now add them back in the right order
			int count = eoList.Count;
			for ( int i=0 ; i<count ; i++ )
			{
				foreach( GUIEditObject item in eoList )
				{
					if ( item.OrderIndex == i )
					{
						item.transform.parent = target.transform;
					}
				}
			}
			// we're done!
		}
	}

	bool toggleSave=false;	
	string saveName="";
	
	public void SaveObject()
	{
		//GUILayout.Label("--------------------------");
		
		// check button toggle
		toggleSave = EditorGUILayout.Toggle("Save Screen",toggleSave);
		if ( toggleSave == false )
		{
			//GUILayout.Label("--------------------------");
			return;
		}
		
		toggleAdd = false;
		
		saveName = EditorGUILayout.TextField("name",saveName);
		if ( GUILayout.Button("Save XML (WARNING! only saves this screen)") )
		{
			myObject.LoadedXML = saveName;
			EditorSaveXML("./Assets/Resources/GUIScripts/" + saveName);
			toggleSave = false;
		}
		
		//GUILayout.Label("--------------------------");
	}
	
	public void EditorSaveXML( string name )
	{
		// build the screen
		//GUIScreen screen = myObject.BuildScreenXML();
		GUIScreen screen = myObject.guiScreen;
		
		// create new screen info
		ScreenInfo si = new ScreenInfo();
		si.AddScreen(screen as GUIScreen);
		
		// convert to save
		si.ConvertToGUIScreen();
		
		// create new XML
		Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
		serializer.Save(name + ".xml",si);				
	}
}


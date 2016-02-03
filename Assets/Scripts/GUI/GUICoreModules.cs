using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InteractMenu2 : GUIDialog
{
    protected class Listings
    {
        public string category;
        public List<Listings> subCategories = new List<Listings>();
        public List<InteractionMap> items = new List<InteractionMap>();
        public Listings parent;

        public bool CheckForSubcatagory(string catagory)
        {
            foreach (Listings sub in subCategories)
            {
                if (sub.category == catagory)
                    return true;
            }
            return false;
        }

        public Listings FindSubCatagory(string category)
        {
            foreach (Listings sub in subCategories)
            {
                if (sub.category == category)
                    return sub;
            }
            return null;
        }
    }
	
    protected List<InteractionMap> items;
    protected ObjectInteraction interactObject;
    protected ObjectInteraction target;
    protected ObjectInteraction patient;
    protected List<ObjectInteraction> roomObjects = new List<ObjectInteraction>();
    protected string xmlData;
    protected Listings listings;
    protected Listings current;
	
	Dictionary<string,string> nameMap;
	
	public bool CloseOnTask = true;
	
	// internal gui elements
	GUILabel label;
	GUIScrollView view;
	GUIToggle categoryToggleTemplate;
	GUIButton taskButtonTemplate;		

    public override void Initialize(ScreenInfo parent)
    {
        patient = Patient.FindObjectOfType(typeof(Patient)) as Patient;

        Object[] temp = ObjectInteraction.FindObjectsOfType(typeof(ObjectInteraction));
        foreach (Object obj in temp)
        {
            if (obj == patient)
                continue;
            roomObjects.Add(obj as ObjectInteraction);
        }
		
		base.Initialize(parent);
    }

	protected void TopPanelInit(DialogMsg dmsg)
	{
		label = Find("npcTitle") as GUILabel;
		if ( label != null )
		{
			if ( interactObject != null )
				label.text = interactObject.Name;
		}
	}
	
	GUIHorizontalCommand bcArea;
	GUILabel bcLeft,bcRight,bcItem;
	
	public void BreadcrumbInit()
	{
		if ( bcArea == null )
		{
			bcArea = Find("breadcrumbsHorizontal") as GUIHorizontalCommand;
			if ( bcArea != null )
			{
				bcLeft = Find("bracketLeft") as GUILabel;
				bcRight = Find("bracketRight") as GUILabel;
				bcItem = Find("breadcrumbs.item") as GUILabel;
			}
		}
		if ( bcArea != null )
		{
			bcArea.Elements.Clear();
			
			bcArea.Elements.Add(bcLeft);

			// loop and get category and current level
			List<string> items = new List<string>();			
			Listings temp = current;
			while ( temp != null )
			{
				if ( temp.category != null )
					items.Add(temp.category);
				temp = temp.parent;					
			}
			// add our name
			items.Add(interactObject.name);

			// now go backwards and add strings
			for (int i=items.Count - 1 ; i>=0 ; i--)
			{
				GUILabel bc;
				bc = bcItem.Clone() as GUILabel;
				if ( i != 0 )
					bc.text = items[i] + " >> ";
				else
					bc.text = items[i];
				bcArea.Elements.Add(bc);
			}
			
			bcArea.Elements.Add(bcRight);
		}
	}
	
	public override void Update ()
	{
		if ( updateObj != null )
		{
			GotoObj(updateObj);
			updateObj = null;
		}
		if ( updateListing != null )
		{
			RightPanelInit(updateListing);
			updateListing = null;
		}
		base.Update ();
	}
	
	Listings updateListing = null;	
	protected void RightPanelUpdate( Listings listing )
	{
		updateListing = listing;
	}
	
	class TaskButton : GUIButton
	{
		public TaskButton( GUIButton button )
		{			
			Copy(button);
		}				
		public InteractionMap map;		
	}
	
	protected void RightPanelInit( Listings listing )
	{
		if ( view == null )		
			view = Find("secondaryMenuScrollArea") as GUIScrollView;
		
		if ( view != null )
		{
			// get category button template
			if ( categoryToggleTemplate == null )
			{				
				categoryToggleTemplate = view.Find("button.category") as GUIToggle;
			}
			// get task button template
			if ( taskButtonTemplate == null )
			{
				taskButtonTemplate = view.Find("button.task") as GUIButton;
			}
			
			// clear all elements
			view.Elements.Clear();
			
			// set current listing
			current = listing;
			
			// put category title first (if there is one)
			if ( current.parent != null )
			{
				// add element
				GUIToggle copy = categoryToggleTemplate.Clone() as GUIToggle;
				copy.name = "BACK";
				copy.text = current.category;
				copy.UpdateContent();
				copy.toggle = true;
				view.Elements.Add(copy);
			}
			
			// 
			// do categories first
		    foreach (Listings cat in listing.subCategories)
		    {
				// add element
				GUIToggle copy = categoryToggleTemplate.Clone() as GUIToggle;
				copy.name = "CATEGORY";
				copy.text = cat.category;
				copy.UpdateContent();
				view.Elements.Add(copy);
			}
			
			// init dictionary
			if ( nameMap == null )
				nameMap = new Dictionary<string, string>();
			nameMap.Clear();			
			
			// now do items
			foreach( InteractionMap map in listing.items )
			{
				// add element
				GUIButton temp = taskButtonTemplate.Clone() as GUIButton;
				TaskButton task = new TaskButton(temp);
				task.map = map;
				task.name = "TASK";
				task.text = StringMgr.GetInstance().Get(map.item);
				task.UpdateContent();
				view.Elements.Add(task);
				// add to name map
				nameMap[task.text] = map.item;
			}
		}
		
		BreadcrumbInit();
	}
	
	protected void LeftPanelInit()
	{
		GUIArea area;
		GUIToggle template;
		
		area = Find("primaryMenuButtonArea") as GUIArea;
		if ( area != null )
		{
			template = area.Elements[0] as GUIToggle;
			if ( template != null)
			{
				// remove the element
				//area.Elements.Remove(template);
				area.Elements.Clear();
				
				// now clone it
				GUIButton copy;
			
				copy = template.Clone() as GUIToggle;
				copy.name = "PATIENT";
				copy.text = "PATIENT";
				copy.UpdateContent();
				area.Elements.Add(copy);
				
				copy = template.Clone() as GUIToggle;
				copy.name = "TEAM";
				copy.text = "TEAM";
				copy.UpdateContent();
				area.Elements.Add(copy);				

				copy = template.Clone() as GUIToggle;
				copy.name = "ROOM";
				copy.text = "ROOM";
				copy.UpdateContent();
				area.Elements.Add(copy);				
			}
		}
	}
	
	ObjectInteraction updateObj;
	protected void ObjUpdate( ObjectInteraction obj )
	{
		updateObj = obj;
	}
	
	protected void GotoObj( ObjectInteraction objint )
	{
		if ( objint == null )
			return;
		
        interactObject = objint;
				
		// change title
		if ( label != null )
			label.text = interactObject.prettyname;
	
        InteractDialogMsg idmsg = new InteractDialogMsg();
        xmlData = interactObject.originXML;
        ScriptedObject so = interactObject.GetComponent<ScriptedObject>();
        if (so != null)
            idmsg.items = so.QualifiedInteractions();
        else
            idmsg.items = interactObject.ItemResponse;
        //idmsg.LoadXML(xmlData);
    	idmsg.baseXML = xmlData;
        idmsg.baseobj = interactObject;
	
        Setup(idmsg);
	}
	
	public ObjectInteraction GetRoomObject( string name )
	{
		foreach( ObjectInteraction objint in roomObjects )
		{
			if ( objint.prettyname == name )
				return objint;
		}
		return null;
	}
	
	public override void ButtonCallback (GUIButton button)
	{
		// task
		if ( button.name == "TASK" )
		{
			TaskButton task = button as TaskButton;
			if ( task != null )
			{
				// get interaction name
				//string name = nameMap[button.text];
				//InteractionMap im = InteractionMgr.GetInstance().Get(name);
        		// Activate interaction!
				// add breadcrumbs to param list
				InteractionMap tmpMap = new InteractionMap();  // need a copy operator?
				tmpMap = task.map;
				tmpMap.objectName = interactObject.Name;
				tmpMap.param.Add(current.category+"=True");
				Listings parent=current.parent;
				while (parent!=null && parent.category != null)
				{
					tmpMap.param.Add(parent.category+"=True");
					parent=parent.parent;
				}
				//InteractionMgr.GetInstance().EvaluateInteractionSet(task.map.item);	
				// go through the dispatcher.
				Dispatcher.GetInstance().ExecuteCommand(tmpMap);
				//Dispatcher.GetInstance().ExecuteCommand(task.map.item, interactObject.Name );
            	// interactObject.PutMessage(new InteractMsg(interactObject.gameObject, tmpMap, task.map.log));
            	current=listings; // reset the menu for next time PAA
				// close this bitch (optional)
				if ( CloseOnTask == true )
					Close();
			}
		}
		
		// patient
		if ( button.name == "PATIENT" )
		{
			GUIToggle toggle = Find("PATIENT") as GUIToggle;
			if ( toggle.toggle == true )
				// go to previous target object
				ObjUpdate(target);
			else
			{
				// save target for return
				target = interactObject;
				// go to patient
				ObjUpdate(patient);
			}
			
			// clear toggles
			toggle = Find("ROOM") as GUIToggle;
			toggle.toggle = false;
			toggle = Find("TEAM") as GUIToggle;
			toggle.toggle = false;
		}
		
		// room and items
		if ( button.name == "ROOM" )
		{
			GUIToggle toggle = Find("ROOM") as GUIToggle;
			if ( toggle.toggle == true )
				// go back to original object
				ObjUpdate(interactObject);
			else
				// go to room
				SetupRoom();
		}
		if ( button.name == "ROOM_ITEM" )
		{
			target = GetRoomObject(button.text); 
			ObjUpdate(target);
			// reset TEAM toggle
			GUIToggle toggle = Find("ROOM") as GUIToggle;
			toggle.toggle = false;
		}
		
		// team and items
		if ( button.name == "TEAM" )
		{
			GUIToggle toggle = Find("TEAM") as GUIToggle;
			if ( toggle.toggle == true )
				// go back to original object
				ObjUpdate(interactObject);
			else
				// go to team
				SetupTeam();
		}
		if ( button.name == "TEAM_ITEM" )
		{
			target = GetRoomObject(button.text); 
			ObjUpdate(target);
			// reset TEAM toggle
			GUIToggle toggle = Find("TEAM") as GUIToggle;
			toggle.toggle = false;
		}
		
		// category (right panel)
		if ( button.name == "CATEGORY" )
		{
			// go to this category
            current = current.FindSubCatagory(button.text);
			if ( current != null )
            	RightPanelUpdate(current);			
		}
		
		// back button (right panel category back)
		if ( button.name == "BACK" )
		{
			// go back
			current = current.parent;
			if ( current != null )
				RightPanelUpdate(current);
		}
		base.ButtonCallback (button);
	}

	public override void Load(DialogMsg msg)
    {
        InteractDialogMsg dmsg = msg as InteractDialogMsg;
        if (dmsg != null)
        {
			// first do setup
			Setup(dmsg);
			// left panel always the same
			LeftPanelInit();		
			// handle left panel init
			TopPanelInit(dmsg);
        }
		base.Load(msg);
    }
	
	void SetupTeam()
	{
		// clear view
		view.Elements.Clear();
		
	    // Setup buttons
        List<string> entries = new List<string>();
        foreach (ObjectInteraction obj in roomObjects)
       	{
			if (obj.onTeamMenu)
			{
				GUIToggle copy = categoryToggleTemplate.Clone() as GUIToggle;
				copy.name = "TEAM_ITEM";
				copy.text = obj.prettyname;
				copy.UpdateContent();
				view.Elements.Add(copy);
			}
        }
		
		// clear toggles
		GUIToggle toggle;
		toggle = Find("PATIENT") as GUIToggle;
		toggle.toggle = false;
		toggle = Find("ROOM") as GUIToggle;
		toggle.toggle = false;
		toggle = Find("TEAM") as GUIToggle;
		toggle.toggle = true;
	}

	void SetupRoom()
	{
		// clear view
		view.Elements.Clear();
		
	    // Setup buttons
        List<string> entries = new List<string>();
        foreach (ObjectInteraction obj in roomObjects)
       	{
			if (obj.onTeamMenu == false )
			{
				GUIToggle copy = categoryToggleTemplate.Clone() as GUIToggle;
				copy.name = "ROOM_ITEM";
				copy.text = obj.prettyname;
				copy.UpdateContent();
				view.Elements.Add(copy);
			}
        }
		
		// clear toggles
		GUIToggle toggle;
		toggle = Find("PATIENT") as GUIToggle;
		toggle.toggle = false;
		toggle = Find("TEAM") as GUIToggle;
		toggle.toggle = false;
		toggle = Find("ROOM") as GUIToggle;
		toggle.toggle = true;
	}

    void Setup(InteractDialogMsg msg)
    {
        // set visible
        Visible = true;
        if (msg.items != null)
        {
            // copy new items
            items = msg.items;

            // set game object
            interactObject = msg.baseobj;
			
            xmlData = msg.baseXML;

            // set position
            //area.x = x;
            //area.y = y;

            // Setup buttons
            List<string> entries = new List<string>();
			
			listings = new Listings();
            listings.subCategories.Clear();
            listings.items.Clear();
            // Build catagories
            foreach (InteractionMap item in items)
            {
                if (item.category != null)
                {
                    foreach (string category in item.category)
                    {
                        ParseCategory(listings, category);
                    }
                }
            }

            // Fill entries
            foreach (InteractionMap item in items)
            {
                if (item.category != null && item.category.Count > 0)
                {
                    foreach (string category in item.category)
                    {
                        AddtoCategory(item, listings, category);
                    }
                }
                else
                {
                    listings.items.Add(item);
                }
            }

            //rmenu.Setup(entries);
            RightPanelInit(listings);
			current = listings;
        }
        else
        {
            interactObject = null;
            xmlData = "";
        }
    }
	
    void ParseCategory(Listings parent, string category)
    {
        string front;
        int start = 0;

        // check if subcatagories listed in string
        if ((start = category.IndexOf("/")) >= 0)
            front = category.Substring(0, start);
        else
            front = category;

        Listings subCategory = parent.FindSubCatagory(front);
        if (subCategory == null)
        {
            // Add subcategory to parent
            subCategory = new Listings();
            subCategory.category = front;
            subCategory.parent = parent;
            SortIntoCategory(parent.subCategories, subCategory, front);
            //parent.subCategories.Add(subCategory);
        }

        // Put further sub-subcategories into this subcategory
        if (start >= 0)
        {
			string temp = category.Substring(start+1, category.Length - (start  + 1));
            ParseCategory(subCategory, temp);
        }
    }

    void SortIntoCategory(List<Listings> list, Listings entry, string name)
    {
        int count = list.Count, insertPoint = 0;
        for(int i = 0; i < count; i++)
        {
            if (SortTest(0, list[i].category.ToLower(), name.ToLower()))
			{
                insertPoint = i;
				break;
			}
			
			insertPoint = i + 1;
        }

        list.Insert(insertPoint, entry);
    }

    void AddtoCategory(InteractionMap item, Listings parent, string category)
    {
        string front;
        int start;
        // check if subcatagories listed in string
        if ((start = category.IndexOf("/")) >= 0)
        {
            front = category.Substring(0, start);
            AddtoCategory(item, parent.FindSubCatagory(front), category.Substring(start + 1, category.Length - (start+1)));
        }
        else
        {
            Listings subcat = parent.FindSubCatagory(category);
            subcat.items.Add(item);
        }
    }

    // False for first, true for second
    bool SortTest(int index, string first, string second)
    {
        if (first.Length < index)
            return false;
        if (second.Length < index)
            return true;

        if (first[index] == second[index])
            return SortTest(++index, first, second);
		
		bool test = first[index] > second[index];
        return test;
    }

}

public class InteractMenu : GUIDialog
{
    protected class Listings
    {
        public string category;
        public List<Listings> subCategories = new List<Listings>();
        public List<InteractionMap> items = new List<InteractionMap>();
        public Listings parent;

        public bool CheckForSubcatagory(string catagory)
        {
            foreach (Listings sub in subCategories)
            {
                if (sub.category == catagory)
                    return true;
            }
            return false;
        }

        public Listings FindSubCatagory(string category)
        {
            foreach (Listings sub in subCategories)
            {
                if (sub.category == category)
                    return sub;
            }
            return null;
        }
    }

    protected List<InteractionMap> items;
    protected ObjectInteraction interactObject;
    protected ObjectInteraction target;
    protected ObjectInteraction patient;
    protected List<ObjectInteraction> roomObjects = new List<ObjectInteraction>();
    protected string xmlData;
    protected Listings listings;
    protected Listings current;

    protected bool rooming = false;

    //protected List<GUIInteract> interactMenus = new List<GUIInteract>();
    //protected GUIInteract interactMenu;

    protected List<string> pastXMLs = new List<string>();

    protected GUIMenu lmenu;
    protected GUIScrollMenu rmenu;
	protected GUILabel title;
	
	static string sTeam = "Team";
	static string sHome = "Home";
	static string sBack = "Back";
	static string sPatient = "Patient";

    public override void Initialize(ScreenInfo parent)
    {
        // Create the space to make the left and right side menus
        // and add the generated GUIMenu objects into it.
        List<GUIObject> find = FindObjectsOfType(typeof(GUIMenu));
        if (find.Count > 0)
            lmenu = find[0] as GUIMenu;
        find = FindObjectsOfType(typeof(GUIScrollMenu));
        if (find.Count > 0)
            rmenu = find[0] as GUIScrollMenu;
		
		find = FindObjectsOfType(typeof(GUILabel));
		if (find.Count > 0)
		{
			title = find[0] as GUILabel;
        	if (interactObject != null)
        	    title.text = interactObject.prettyname;
		}
		
        find.Clear();
        Elements.Clear();
		
		GUIVerticalCommand vert = new GUIVerticalCommand();
		vert.Elements = new List<GUIObject>();
		Elements.Add(vert);
		
		if (title != null )
			vert.Elements.Add(title);
		
		GUISpace space = new GUISpace();
		space.pixels = 5;
		vert.Elements.Add(space);

        if (lmenu != null && rmenu != null)
        {
            GUIHorizontalCommand hc = new GUIHorizontalCommand();
            hc.Elements = new List<GUIObject>();
            hc.Elements.Add(lmenu);
            hc.Elements.Add(rmenu);
            vert.Elements.Add(hc);
        }
        base.Initialize(parent);

        patient = Patient.FindObjectOfType(typeof(Patient)) as Patient;

        Object[] temp = ObjectInteraction.FindObjectsOfType(typeof(ObjectInteraction));
        foreach (Object obj in temp)
        {
            if (obj == patient)
                continue;
            roomObjects.Add(obj as ObjectInteraction);
        }

        listings = new Listings();
        listings.category = "root";

        current = listings;
    }

    public override void Execute()
    {
        if (!Visible)
            return;

		base.Execute();

        int i;
        List<GUIMenu.MenuButton> buttons = lmenu.GetButtons();
		GUIMenu.MenuButton mbutton = lmenu.GetOnButton();
		
		if ( mbutton != null )
		{
	        // Check Target button, if active
            if ( mbutton.button.text == sHome ) // Patient button
	        {
                interactObject = target;
	
				// change title
				if ( title != null )
					title.text = interactObject.prettyname;
	
                InteractDialogMsg idmsg = new InteractDialogMsg();
                xmlData = interactObject.originXML;
                ScriptedObject so = interactObject.GetComponent<ScriptedObject>();
                if (so != null)
                    idmsg.items = so.QualifiedInteractions();
                else
                    idmsg.items = interactObject.ItemResponse;
                //idmsg.LoadXML(xmlData);
                idmsg.baseXML = xmlData;
                idmsg.baseobj = interactObject;
	
                Setup(idmsg);
	
                pastXMLs.Clear();
                rooming = false;
            }
            if ( mbutton.button.text == sPatient ) // Patient button
	        {
	            interactObject = patient;
				
				// change title
				if ( title != null )
					title.text = interactObject.prettyname;
	
	            InteractDialogMsg idmsg = new InteractDialogMsg();
	            xmlData = interactObject.originXML;
	            ScriptedObject so = interactObject.GetComponent<ScriptedObject>();
	            if (so != null)
	                idmsg.items = so.QualifiedInteractions();
	            else
	                idmsg.items = interactObject.ItemResponse;
	            //idmsg.LoadXML(xmlData);
	            idmsg.baseXML = xmlData;
	            idmsg.baseobj = interactObject;
	
	            Setup(idmsg);
	
	            pastXMLs.Clear();
	            rooming = false;
	        }
	        else if ( mbutton.button.text == sTeam ) // Team button
	        {
				// change button[0] name to this object
				
	            // Setup buttons
	            List<string> entries = new List<string>();
	            foreach (ObjectInteraction obj in roomObjects)
	            {
					if (obj.onTeamMenu)
						entries.Add(obj.prettyname);
	            }
	            rmenu.Setup(entries);
	            rooming = true;
	        }
	        else if ( mbutton.button.text == sBack ) // GoBack button
	        {
	            if (current != listings)
	            {
	                //InteractDialogMsg idmsg = new InteractDialogMsg();
	                //xmlData = pastXMLs[pastXMLs.Count - 1];
	                //idmsg.LoadXML(xmlData);
	                //idmsg.baseXML = xmlData;
	                //idmsg.baseobj = interactObject;
	
	                //Setup(idmsg);
	
	                //pastXMLs.RemoveAt(pastXMLs.Count - 1);
	                //rooming = false;
	
	                current = current.parent;
	                rooming = false;
	                SetupRight(current);
	            }
	            else
	            {
	                Visible = false;
	            }
	        }
			// clear buttons
	        for (i = 0; i < buttons.Count; i++)
	        {
	            buttons[i].on = false;
	        }
		}

        buttons = rmenu.GetButtons();
        for (i = 0; i < buttons.Count; i++)
        {
            if (rooming)
            {
                if (buttons[i].on)
                {
                    interactObject = null;
                    // Find matching Object by name (allows for onTeamMenu to work during runtime)
                    foreach (ObjectInteraction oi in roomObjects)
                    {
                        if (buttons[i].button.text == oi.prettyname)
                        {
                            interactObject = oi;
                            break;
                        }
                    }
                    if (interactObject == null) break; // Safety measure

                    InteractDialogMsg idmsg = new InteractDialogMsg();
                    xmlData = interactObject.originXML;
                    ScriptedObject so = interactObject.GetComponent<ScriptedObject>();
                    if (so != null)
                        idmsg.items = so.QualifiedInteractions();
                    else
                        idmsg.items = interactObject.ItemResponse;
					
                    idmsg.baseXML = xmlData;
                    idmsg.baseobj = interactObject;

					// change the title bar (or first element of left column for now) to be
					// this button name
					title.text = buttons[i].button.text;

					Setup(idmsg);					

                    pastXMLs.Clear();
                    rooming = false;
                    break;
                }
            }
            else
            {
                if (buttons[i].on)
                {
                    if (i < current.subCategories.Count)
                    {
                        current = current.subCategories[i];
                        SetupRight(current);
                        break;
                    }
                    else
                    {
                        InteractionMap im = current.items[i - current.subCategories.Count];
                        
                        // Activate interaction!
                        interactObject.PutMessage(new InteractMsg(interactObject.gameObject, im, im.log));
                        current=listings; // reset the menu for next time PAA
                        Visible = false;
                        break;
                    }
                }
            }
            buttons[i].on = false;
        }
    }

    public override void Load(DialogMsg msg)
    {
        InteractDialogMsg dialogMsg = msg as InteractDialogMsg;
        if (dialogMsg != null)
        {
            pastXMLs.Clear();
            Setup(dialogMsg);
            SetupLeft();
        }
    }

    void ParseCategory(Listings parent, string category)
    {
        string front;
        int start = 0;

        // check if subcatagories listed in string
        if ((start = category.IndexOf("/")) >= 0)
            front = category.Substring(0, start);
        else
            front = category;

        Listings subCategory = parent.FindSubCatagory(front);
        if (subCategory == null)
        {
            // Add subcategory to parent
            subCategory = new Listings();
            subCategory.category = front;
            subCategory.parent = parent;
            SortIntoCategory(parent.subCategories, subCategory, front);
            //parent.subCategories.Add(subCategory);
        }

        // Put further sub-subcategories into this subcategory
        if (start >= 0)
        {
			string temp = category.Substring(start+1, category.Length - (start  + 1));
            ParseCategory(subCategory, temp);
        }
    }

    void SortIntoCategory(List<Listings> list, Listings entry, string name)
    {
        int count = list.Count, insertPoint = 0;
        for(int i = 0; i < count; i++)
        {
            if (SortTest(0, list[i].category.ToLower(), name.ToLower()))
			{
                insertPoint = i;
				break;
			}
			
			insertPoint = i + 1;
        }

        list.Insert(insertPoint, entry);
    }

    // False for first, true for second
    bool SortTest(int index, string first, string second)
    {
        if (first.Length < index)
            return false;
        if (second.Length < index)
            return true;

        if (first[index] == second[index])
            return SortTest(++index, first, second);
		
		bool test = first[index] > second[index];
        return test;
    }

    void AddtoCategory(InteractionMap item, Listings parent, string category)
    {
        string front;
        int start;
        // check if subcatagories listed in string
        if ((start = category.IndexOf("/")) >= 0)
        {
            front = category.Substring(0, start);
            AddtoCategory(item, parent.FindSubCatagory(front), category.Substring(start + 1, category.Length - (start+1)));
        }
        else
        {
            Listings subcat = parent.FindSubCatagory(category);
            subcat.items.Add(item);
        }
    }

    void Setup(InteractDialogMsg msg)
    {
        // set visible
        Visible = true;
        if (/*dialogmsg.command == DialogMsg.Cmd.open && */msg.items != null)
        {
            // copy new items
            items = msg.items;

            // set game object
            interactObject = msg.baseobj;

            xmlData = msg.baseXML;

            // set position
            //area.x = x;
            //area.y = y;

            // Setup buttons
            List<string> entries = new List<string>();

            listings.subCategories.Clear();
            listings.items.Clear();
            // Build catagories
            foreach (InteractionMap item in items)
            {
                if (item.category != null)
                {
                    foreach (string category in item.category)
                    {
                        ParseCategory(listings, category);
                    }
                }
            }

            // Fill entries
            foreach (InteractionMap item in items)
            {
                if (item.category != null && item.category.Count > 0)
                {
                    foreach (string category in item.category)
                    {
                        AddtoCategory(item, listings, category);
                    }
                }
                else
                {
                    listings.items.Add(item);
                }
            }

            //rmenu.Setup(entries);
            SetupRight(listings);
            current = listings;
            if (interactObject == null)
                rmenu.GetButtons()[0].draw = false;
        }
        else
        {
            interactObject = null;
            xmlData = "";
        }
    }

    void SetupLeft()
    {
        // Set up left side
        target = interactObject;

        List<string> entries2 = new List<string>();
        //if (interactObject != null)
        //    entries2.Add(interactObject.prettyname);
		if ( title != null )
			title.text = interactObject.prettyname;
		entries2.Add(sHome);
        entries2.Add(sPatient);
        entries2.Add(sTeam);
        //entries2.Add("Phone");
        entries2.Add(sBack);
        lmenu.Setup(entries2);
    }

    void SetupRight(Listings category)
    {
        List<string> entries = new List<string>();
        StringMgr sMgr = StringMgr.GetInstance();

        // Add subcategories on top
        foreach (Listings subcat in category.subCategories)
        {
            entries.Add("|> " + subcat.category);
        }

        // Add entries in this category
        foreach (InteractionMap item in category.items)
        {
            entries.Add(sMgr.Get(item.item));
        }
		
		rmenu.Setup(entries);
    }
}

public class InfoDialogGUI : GUIDialog
{
	public bool AutoClose = true;
	
	public int MinMessages = 5;
	public int MaxMessages = 5;
	int maxcount = 20;
	
    List<string> entries = new List<string>();
    bool expanded = false;
    float backupY, backupHeight;
    int newMessages = 0;
	float lastMsgTime = 0.0f;
	float timeExpanded = 5.0f;
	float textHeight = 0;
	bool scroll=true;
	bool reverse=true;
	
	GUILabel contentText;
	GUIScrollView contentArea;
	
	GUIStyle parentStyle;
	GUIStyle textAreaStyle;
	GUIStyle textAreaContentStyle;
	GUIStyle buttonStyle;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

		// get important areas		
		GUIArea textArea = Find("textArea") as GUIArea;
		contentArea = textArea.Find("contentArea") as GUIScrollView;
		contentText = contentArea.Find("contentText") as GUILabel;
		contentArea.Elements.Remove(contentText);
		
		// save styles
		parentStyle = _style;
		textAreaStyle = textArea.Style;
		textAreaContentStyle = contentArea.Style;		
		
		GUIArea buttonArea = Find("buttonArea") as GUIArea;
		buttonStyle = buttonArea.Style;
		
        GUIObject notice = Find("notice");
        if (notice != null)
        {
            notice.text = newMessages.ToString() + " new messages";
        }		
		maxcount = MaxMessages;
    }

	public override void Update()
	{
		base.Update();
		if ( AutoClose == true )
		{
			if ( expanded == true && (lastMsgTime+timeExpanded) < Time.time )
			{
				ToggleButton();
				ToggleArea();
			}
		}
	}
	
	public void ToggleButton()
	{
		// get the button and turn off toggle
		GUIToggle toggle = this.Find("expand") as GUIToggle;
		if ( toggle != null )
			toggle.toggle = false;
	}
	
	public void SetAreaSize()
	{
        if (expanded == false)
        {
			// just give enough height to display the button
			parentStyle.fixedHeight = buttonStyle.fixedHeight;			
        }
        else
        {			
			// expand height to include the text area plus the button...textHeight is computed in CalcHeight
			textAreaStyle.fixedHeight = textHeight;
			textAreaContentStyle.fixedHeight = textAreaStyle.fixedHeight;						
			parentStyle.fixedHeight = textAreaStyle.fixedHeight + buttonStyle.fixedHeight;			
		}		
	}
	
	public void CalcAreaSize()
	{
		// get sizeo of area to render
	    GUIObject text = contentText;
		if ( entries.Count == 0 )	
			textHeight = 0;
		else
		{
			textHeight = 0;//text.Style.margin.bottom;			
			int idx=0;
			foreach( string str in entries )
			{
		        float sizeY = text.Style.CalcHeight(new GUIContent(str),text.Style.fixedWidth);
				textHeight += sizeY;
				if ( ++idx >= maxcount )
					break;
			}
		}
		// set size
		SetAreaSize();
	}
	
	public void ToggleArea()
	{
        expanded = !expanded;
		
		CalcAreaSize();
		
        newMessages = 0;
        GUIObject notice = Find("notice");
        if (notice != null)
        {
        	notice.text = newMessages.ToString() + " new messages";
        }
		lastMsgTime = Time.time;
	}
	
    public override void Load(DialogMsg msg)
    {
		UnityEngine.Debug.Log("InfoDialog.Load(" + msg.text + ")");
		
		InfoDialogMsg idMsg = msg as InfoDialogMsg;
		if ( idMsg != null )
		{
			AutoClose = idMsg.AutoClose;
			timeExpanded = idMsg.CloseTime;
			maxcount = MaxMessages = idMsg.MaxLines;
			reverse = idMsg.Reverse;
			scroll = idMsg.Scroll;
		}

		if (msg != null && msg.command == DialogMsg.Cmd.open)
        {
			// add new entry
            string newText = msg.title + " " + msg.text;
            entries.Add(newText);

			if ( scroll == false )
			{
				// handle max count
	            if (entries.Count > maxcount)
				{
					// remove first one
	                entries.RemoveRange(0, entries.Count - maxcount);
					// shift elements
					for (int i=0 ; i<entries.Count ; i++)
					{
						contentArea.Elements[i].text = entries[i];
					}
				}
			}
			if ( reverse == false )
			{
				// just add it
				GUILabel newlabel = contentText.Clone() as GUILabel;
				if ( newlabel != null )
				{
					newlabel.text = newText;
					contentArea.Elements.Add(newlabel);
				}
			} 
			else
			{
				contentArea.Elements.Clear();
				for( int i=entries.Count-1 ; i>=0 ; i--)
				{
					GUILabel newlabel = contentText.Clone() as GUILabel;
					if ( newlabel != null )
					{
						newlabel.text = entries[i];
						contentArea.Elements.Add(newlabel);
					}
				}
			}

			// calculate new size
			CalcAreaSize();

			Visible = true;

            // If hidden and a system notice, just post that a new message is available to be viewed
            if (msg.title.Contains("system") && !expanded)
            {
                newMessages++;
                GUIObject notice = Find("notice");
                if (notice != null)
                {
                    notice.text = newMessages.ToString() + " new messages";
                }
            }
            else // Force the dialog expanded.
            {
                GUIToggle expandToggle = Find("expand") as GUIToggle;
                if (expandToggle != null)
                {
                    expandToggle.toggle = true;
                    if (!expanded)
                        ButtonCallback(expandToggle);
                }
				lastMsgTime = Time.time;
            }
        }
    }
	
    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            case "expand":
                {
					ToggleArea();
                }
                break;
        }
    }
}

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

public class GUIDrugOrder : GUIDialog
{
    internal class GDOrder
    {
        public int number;
        public int dose;
        public int delivery;
        public int interval;

        public GDOrder() { number = -1; dose = -1; delivery = -1; interval = -1; }
    }

    PharmacyManager pManager;
    GUIDrugList list;
    GUIOrderCart cart;

    GDOrder order;
    List<GDOrder> cartedOrders = new List<GDOrder>();

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

        pManager = PharmacyManager.GetInstance();

        List<GUIObject> temp = FindObjectsOfType(typeof(GUIDrugList));
        if (temp != null || temp.Count > 0)
            list = temp[0] as GUIDrugList;

        temp = FindObjectsOfType(typeof(GUIOrderCart));
        if (temp != null || temp.Count > 0)
            cart = temp[0] as GUIOrderCart;

        SetupLeft();
        // Test
        //cart.text = "TestText";

        order = new GDOrder();
        cartedOrders.Clear();
    }

    public override void Execute()
    {
        GUIScrollMenu[] menus = list.GetMenus();
        if (menus == null || menus.Length == 0)
            return;

        base.Execute();
                
        // Drug list interactions
        List<GUIMenu.MenuButton> buttons = menus[0].GetButtons();
        int i;
        for(i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].on)
            {
                SetupRight(pManager.GetDrug(buttons[i].button.text));
                menus[0].TurnActive(i);
                if (order.number != i)
                {
                    order.number = i;
                    order.dose = -1;
                    order.delivery = -1;
                    order.interval = -1;
                }
                
            }
            buttons[i].on = false;
        }

        // Doses list
        buttons = menus[1].GetButtons();
        for (i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].on)
            {
                menus[1].TurnActive(i);
                order.dose = i;

            }
            buttons[i].on = false;
        }

        // Delivery list
        buttons = menus[2].GetButtons();
        for (i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].on)
            {
                menus[2].TurnActive(i);
                order.delivery = i;

            }
            buttons[i].on = false;
        }

        // Interval list
        buttons = menus[3].GetButtons();
        for (i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].on)
            {
                menus[3].TurnActive(i);
                order.interval = i;

            }
            buttons[i].on = false;
        }
    }

    public override void Load(DialogMsg msg)
    {
		base.Load(msg);
		
        if (msg != null)
        {
            SetupLeft();
        }
    }

    void SetupLeft()
    {
        GUIScrollMenu[] menus = list.GetMenus();

        List<string> entries = pManager.GetDrugList();
        menus[0].Setup(entries);
    }

    void SetupRight(PharmacyManager.DrugMerge order)
    {
        GUIScrollMenu[] menus = list.GetMenus();
        List<string> entries = new List<string>();
        List<PharmacyManager.DrugMerge.DoseOrder> doses = order.doses;

        // Doses
        foreach (PharmacyManager.DrugMerge.DoseOrder dose in doses)
        {
            entries.Add(dose.amount.ToString() + dose.type.ToString());
        }
        menus[1].Setup(entries);

        entries.Clear();
        // Deliveries
        foreach (PharmacyManager.DrugMerge.DeliveryOrder delivery in order.delivery)
        {
            entries.Add(delivery.type.ToString());
        }
        menus[2].Setup(entries);

        entries.Clear();
        // Intervals
        foreach(PharmacyManager.DrugMerge.IntervalOrder interval in order.intervals)
        {
            entries.Add(interval.amount.ToString() + " " + interval.type.ToString());
        }
        menus[3].Setup(entries);
    }

    void Clear()
    {
        GUIScrollMenu[] menus = list.GetMenus();
        menus[0].TurnActive(-1);
        menus[1].TurnActive(-1);
        menus[2].TurnActive(-1);
        menus[3].TurnActive(-1);
        order = new GDOrder();
        menus[1].Clear();
        menus[2].Clear();
        menus[3].Clear();
    }

    void UpdateCart()
    {
        string text = "";
        foreach (GDOrder item in cartedOrders)
        {
            PharmacyManager.DrugMerge drug = pManager.GetDrug(item.number);
            text += drug.drugName + " " + drug.doses[item.dose].amount.ToString() + drug.doses[item.dose].type.ToString() +
                " " + drug.delivery[item.delivery].type.ToString() + " " + drug.intervals[item.interval].amount.ToString() +
                drug.intervals[item.interval].type.ToString() + "\n";
        }

        cart.text = text;
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name.ToLower())
        {
            case "order":
                {
                    if (order.number >= 0 && order.dose >= 0 && order.delivery >= 0 && order.interval >= 0)
                    {
                        PharmacyManager.DrugMerge drug = pManager.GetDrug(order.number);
                        if (drug != null)
                        {
                            cartedOrders.Add(order);
                            order = new GDOrder();

                            UpdateCart();

                            Clear();
                        }
                    }
                }
                break;
            case "ok":
                {
                    List<PharmacyManager.DrugMerge> sendOrders = new List<PharmacyManager.DrugMerge>();
                    foreach(GDOrder item in cartedOrders)
                    {
                        PharmacyManager.DrugMerge drug = pManager.GetDrug(item.number);
                        PharmacyManager.DrugMerge orderedDrug = new PharmacyManager.DrugMerge();
                        orderedDrug.drugName = drug.drugName;
                        //orderedDrug.drugID = drug.drugID;
                        orderedDrug.doses.Add(drug.doses[item.dose]);
                        orderedDrug.delivery.Add(drug.delivery[item.delivery]);
                        orderedDrug.intervals.Add(drug.intervals[item.interval]);

                        sendOrders.Add(orderedDrug);
                    }



                    // Check the order
                    List<PharmacyManager.DrugEntry> queuing = new List<PharmacyManager.DrugEntry>();
                    foreach (PharmacyManager.DrugMerge dm in sendOrders)
                    {
                        PharmacyManager.DrugEntry entry = pManager.CheckItem(dm.drugName, dm.doses[0], dm.delivery[0], dm.intervals[0]);
                        if (entry != null)
                        {
                            queuing.Add(entry);
                        }
                        //{
                        //    VitalsBehaviorManager vbMgr = VitalsBehaviorManager.GetInstance();
                        //    if (vbMgr != null && entry.behavior != null)
                        //    {
                        //        vbMgr.AddBehavior(entry.behavior);
                        //    }
                        //}
                    }
                    if (queuing.Count > 0)
                        pManager.QueueDrugs(queuing);

                    cartedOrders.Clear();
                    UpdateCart();
                    Clear();
                    GUIManager.GetInstance().Remove(parent);
                }
                break;
        }
    }

    public override void OnClose()
    {
        base.OnClose();

        pManager.DialogClosed();
    }
}

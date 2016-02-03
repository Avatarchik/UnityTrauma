using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

[CustomEditor(typeof(ObjectInteraction))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class ObjectInteractionInspector : Editor 
{
	protected MenuTreeNode menuTree;
	protected bool onSelected = false;
	protected bool showDefaultInspector = true;
	
	public void Awake(){
		// load up the interactions and tasks this should be done elsewhere, and only once
		InteractionMgr.GetInstance().LoadXML("XML/interactions/interactions");
		TaskMaster.GetInstance().LoadXML("XML/Tasks");
		StringMgr.GetInstance().Load ();
	}
	
	virtual public void OnSelected() // look at a particular instance object
	{
	}
	
	public override void OnInspectorGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Show Default Inspector");
		GUILayout.FlexibleSpace();
		Color oldColor = GUI.color;
		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
		GUILayout.EndHorizontal();
		
		if (showDefaultInspector)
			DrawDefaultInspector();
		
		if (!onSelected) // this must be Pauls name.
		{
			onSelected = true;
			OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
		}
		GUI.color = Color.cyan;
//		ShowScriptSerialization(); // moved to the scripted object inspector
		ShowAttributeValues();
		ShowDecisionVariables();
		ShowQueuedScripts();
		GUI.color = Color.white;
		ShowStackedScripts();
		
		if (menuTree != null)
			menuTree.ShowInspectorGUI(0);
		else{
			GUILayout.Button ("No Menu exists for XMLName" );	
		}
		GUI.color = oldColor;
		return;
	}
	
	// this is no longer called here, it's on the ScriptedObject now.
	void ShowScriptSerialization(){
		// don't put these buttons up while running...
		if (EditorApplication.isPlaying || EditorApplication.isPaused) return;
		ScriptedObject so = (target as ObjectInteraction).GetComponent<ScriptedObject>();
		if (so != null){
			GUILayout.BeginHorizontal();
			GUILayout.Label("SCRIPTS FROM ");
			if (so.XMLName == null) so.XMLName = "";
			so.XMLName = GUILayout.TextField(so.XMLName);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				if (GUILayout.Button("SAVE")){
					ScriptedObject.ScriptedObjectInfo info = so.ToInfo (so);
					XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObject.ScriptedObjectInfo));
		 			FileStream stream = new FileStream(so.XMLName, FileMode.Create);
		 			serializer.Serialize(stream, info);
		 			stream.Close();				
				}
				if (GUILayout.Button("LOAD")){
					 XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObject.ScriptedObjectInfo));
					 FileStream stream = new FileStream(so.XMLName, FileMode.Open);
					 ScriptedObject.ScriptedObjectInfo info = serializer.Deserialize(stream) as ScriptedObject.ScriptedObjectInfo;
					 stream.Close();
					// really nead to clear out the SO first!
					foreach (InteractionScript script in so.scripts){
						DestroyImmediate(script.gameObject);
					}
					so.InitFrom(info);
				}
			GUILayout.EndHorizontal();			
		}
	}
	
	
	void ShowAttributeValues(){
		BaseObject bob = target as BaseObject;
		if (bob != null && bob.attributes.Count > 0){
			GUILayout.Label("Current Attributes");
			foreach (KeyValuePair<string,string> kvp in bob.attributes){
				GUILayout.BeginHorizontal();
				GUILayout.Label(kvp.Key);	
				GUILayout.Label(kvp.Value);
				GUILayout.EndHorizontal();
			}
		}
	}
	
	void ShowDecisionVariables(){
		BaseObject bob = target as BaseObject;
		if (bob != null){
			bob.ShowDecisionVariables();
		}
	}	
	
	void ShowQueuedScripts(){
		BaseObject bob = target as BaseObject;
		if (bob != null && bob.GetComponent<ScriptedObject>() != null){
			ScriptedObject so = bob.GetComponent<ScriptedObject>();
			if (so.scriptArray.Count > 0){
				GUILayout.Label("QueuedScripts:");
				foreach (object o in so.scriptArray){
				ScriptedObject.QueuedScript q = o as ScriptedObject.QueuedScript;
				//q.script.isReadyToRun (bob,false); // this didn't do anything and is costly
				setScriptColor(q.script.readyState);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("?",GUILayout.ExpandWidth(false)))
					ScriptViewWindow.Init(q.script);
				GUILayout.Label(q.priority.ToString(),GUILayout.ExpandWidth(false)); // what happened to q.priority?
				GUILayout.Label(q.script.name,GUILayout.ExpandWidth(false));	//,GUILayout.ExpandWidth(false)
				GUILayout.Label(q.args,GUILayout.ExpandWidth(false));
				GUILayout.Label(q.obj.name,GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();					
				}
			}
		}
	}
	
	void setScriptColor(InteractionScript.readiness readyState){
		// since if there are queued scripts, we are probably running one, we are not usually available,
		// we we don't see scripts in the 'readyToRun' state. seems like ever.
		GUI.color = Color.cyan;
		if (readyState == InteractionScript.readiness.executing) GUI.color = Color.green;
		if (readyState == InteractionScript.readiness.readyToRun) GUI.color = Color.yellow;
		if (readyState == InteractionScript.readiness.stale) GUI.color = Color.red;
		if (readyState == InteractionScript.readiness.unavailable) GUI.color = Color.grey;
	}
	
	void ShowStackedScripts(){
		BaseObject bob = target as BaseObject;
		if (bob != null && bob.GetComponent<ScriptedObject>() != null){
			ScriptedObject so = bob.GetComponent<ScriptedObject>();
			if (so.scriptStack.Count > 0){
				GUILayout.Label("StackedScripts:");
				foreach (ScriptedObject.QueuedScript q in so.scriptStack){
				GUILayout.BeginHorizontal();
				GUILayout.Label(q.script.name);	
				GUILayout.Label(q.args);
				GUILayout.Label(q.obj.name);
				GUILayout.EndHorizontal();					
				}
			}
		}
	}
	
	public void Update()
	{
	}
}



// This class is used to build a hierarchical representation of the menu dialog from the XML for editing.

public class MenuTreeNode
{
	public InteractionMap map;  // if this node is an interaction, not a submenu, this will be populated
	// otherwise, the node is a submenu described by an XML file representing an ObjectInteractionInfo
	public string filePath = null; // so we can serialize this node's objectInteractionInfo out after editing
	public ObjectInteractionInfo info; // the info that generated this node. reach into here to get the name, etc.
	// I want to preserve the intermix and ordering of the menu items
	public List<MenuTreeNode> children; // child nodes built from any XML: or InteractionMapNames in this node
	public MenuTreeNode parent = null;
	bool expanded = false; // is the menu level or interaction shown in detail?
	bool expandList = false; // for interactions, is the interactionlist at this level shown in detail
	
	MenuTreeNode newNode; // used when building up new menus
	bool valid = true; // when adding a new node, set to true when all required fields have values.
	
	// these local varaibles are for the editor GUI, letting you open one task,list or stringmap per node for editing.
	
	string editingTaskKey = ""; // the task at this node open for edit
	
	string editingStrmapKey = "";
	StringMap editStrmap = new StringMap(); // this persistent value lets us edit a stringmap
	
	public static MenuTreeNode BuildMenu( string fileName ) // pass in the relative path
    {
		MenuTreeNode returnNode = new MenuTreeNode();
		returnNode.filePath = fileName;
		
		Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
//        ObjectInteractionInfo info;
		returnNode.info = serializer.Load(fileName); // if this load fails, then we should probably create a default empty file
		returnNode.children = new List<MenuTreeNode>();
		// read in the ObjectInteractionInfo at this level. 
				
		// If a DialogItem begins with XML, 
		if (returnNode.info == null) return null;

	        foreach (InteractionMap map in returnNode.info.ItemResponse)
	        {
	            if (map.item.Contains("XML:"))
	            {
	                // remove XML:
	                string newXML = map.item.Remove(0, 4);
	                // load this new one
//	                info = serializer.Load("XML/Interactions/" + newXML);
	                // parse new one and add it as a child
					MenuTreeNode newNode = BuildMenu("XML/Interactions/" + newXML);
					if (newNode != null){
						newNode.parent = returnNode;
	                	returnNode.children.Add(newNode);
					}
	            }
	            else
	            {
	                // it's a terminal node, an interaction
					MenuTreeNode newNode = new MenuTreeNode();
	                newNode.map = map;
					newNode.parent = returnNode;
					returnNode.children.Add(newNode);
	            }
	        }

		return returnNode;
    }	
	
	public void RemoveChild(MenuTreeNode child){
		// find a child node, which might be a whole submenu, or just a single interaction,
		// and remove it from our info's DialogItems, and from our list of children
		
		// this child node is a submenu or an interactionMap
		if (child.map == null){
			// remove submenu XML: thinkg from our dialog items
			
		}
		else
		{	// remove the interaction item from our dialog items
			info.DialogItems.Remove (child.map.item);
			
			// and save our objectInteractionInfo out to it's filepath.
			Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
			serializer.Save("Assets/Resources/"+filePath+".xml",info); // updates this level of menu with the new child
			// then remove the child from our children list
			children.Remove(child);
		}	
		
	}
	
	
	// this method recursively displays the menu tree for the Editor inspector, and can call methods to add and save
	public void ShowInspectorGUI(int level){
		EditorGUILayout.Space ();

		if (level == -1)
			GUILayout.Label("============ CREATING NEW ITEM =============");
		else if (level == 0){
GUILayout.Label ("VVV >> FOR TESTING ONLY, STILL WORK IN PROGRESS << VVV");
			GUILayout.Label("============ INTERACTION MENUS =============");
		}
		string indent = "";
		string xp = expanded?"V ":"> ";
		for (int i=0;i<level;i++)
			indent += "__"; // just a string to add space on the left
		if (map == null){ // this is a submenu
			GUILayout.BeginHorizontal ();
			GUILayout.Label (indent+info.Name+" DialogTitle:",GUILayout.Width(125));
			info.DialogTitle = GUILayout.TextField (info.DialogTitle,GUILayout.Width(175));
			bool clicked = GUILayout.Button (xp+info.DialogTitle,GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			if (clicked) expanded = !expanded;
			if (expanded){
				Color topColor = GUI.color;
				Color nextColor = topColor; nextColor.r = 0.75f; nextColor.g = 0.9f;
				GUI.color = nextColor;
				GUILayout.BeginHorizontal();
				GUILayout.Label ("Loads From:");
				filePath = GUILayout.TextField(filePath);
				GUILayout.EndHorizontal();
				foreach (MenuTreeNode n in children)
					n.ShowInspectorGUI(level+1);

				GUILayout.BeginHorizontal();
				GUILayout.Label (info.DialogTitle+":");
				if (newNode == null){
					if (GUILayout.Button("Add Submenu",GUILayout.ExpandWidth(false)))
					{ //lets add a submenu!
						newNode = new MenuTreeNode();
						newNode.expanded = true; // makes more sense for editing
						newNode.info = new ObjectInteractionInfo();
						newNode.info.DialogTitle = "ENTER Submenu Title";
						newNode.filePath = "ENTER Path relative to XML/Interactions/";
						newNode.children = new List<MenuTreeNode>();
						//newNode.valid = false;
					}
					if (GUILayout.Button ("Add Interaction",GUILayout.ExpandWidth(false)))
					{ // lets add an interaction!
						newNode = new MenuTreeNode();
						newNode.expanded = true; // makes more sense for editing
						newNode.map = new InteractionMap(); // this tells the dialog we are adding an interaction
						newNode.map.item = "TASK:NEW:THING";
						//newNode.valid = false;
					}
				}
				if (newNode == null && CheckIsValid(this)) // we don't have work in progress
					if (GUILayout.Button ("Save Changes!",GUILayout.ExpandWidth(false)))
					{
						// save the xml stuff, and reload ourselves
						// if we are a submenu thing, serialize our 
					
					}
				GUILayout.EndHorizontal();
				if (newNode != null)
					ShowAddNodeGUI();
				GUI.color = topColor;
			}
			
		}
		else
		{  // this is a terminal node, an interaction, so show it for editing
			Color prevColor = GUI.color;
			GUI.color = new Color(.75f,1,1,1);
			GUILayout.BeginHorizontal();
			bool clicked = GUILayout.Button("Interaction"+indent+xp+StringMgr.GetInstance().Get(map.item));
			if (clicked) expanded = !expanded;
			if (DeleteButton()){// delete this interaction! that means the whole menu node at this level!
				parent.RemoveChild(this);
			}
			GUILayout.EndHorizontal();
			if (expanded)
			{

					GUILayout.BeginHorizontal();
					GUILayout.Label (indent+"item:",GUILayout.Width(125));
					map.item = GUILayout.TextField(map.item);
					GUILayout.EndHorizontal ();
					map.item = EditMappedString(indent+"item:", map.item, map);
					map.response = EditMappedString(indent+"response:", map.response, map);
					map.response_title = EditMappedString(indent+"response_title:", map.response_title, map);
					map.tooltip = EditMappedString(indent+"tooltip:", map.tooltip, map);
					map.note = EditMappedString(indent+"note:", map.note, map);
				
					if (map.sound != null){
						GUILayout.Label (indent+"sound:"+map.sound);
//						AudioClip sound = SoundMgr.GetInstance().Get(map.sound);
//						EditorGUILayout.PropertyField(sound);
					}
				
					GUILayout.BeginHorizontal();
					GUILayout.Label(indent+"task:",GUILayout.Width(125));
					if (map.task == null){
						if (GUILayout.Button ("No Task, CLICK TO ADD")) map.task = "TASK:";	
					}
					else
						map.task = GUILayout.TextField(map.task); // textfiled doesn't like a null argument
					GUILayout.EndHorizontal ();
					if (map.task != null){
						if (TaskMaster.GetInstance().GetTask(map.task) != null){
							Color taskColor = GUI.color;
							GUI.color = new Color(.95f,1,1,1);
							GUILayout.Label(indent+"-----------------Taskdata: ---------------");
							foreach (CharacterTask ct in TaskMaster.GetInstance().GetTask(map.task).data.characterTasks){
								GUILayout.Label (indent+"CT      characterName:"+ct.characterName);
								GUILayout.Label (indent+"CT      nodeName:"+ct.nodeName);
								if (ct.posture != null)
									GUILayout.Label (indent+"CT      posture:"+ct.posture);
								if (ct.lookAt != null)
									GUILayout.Label (indent+"CT      lookAt:"+ct.lookAt);
								if (ct.animatedInteraction != null)
									GUILayout.Label (indent+"CT      animatedInteraction:"+ct.animatedInteraction);
								GUILayout.Label (indent+"CT      delay:"+ct.delay);
								GUILayout.Label ("---------------------------------------------");
							}
							GUI.color = taskColor;
						}else{
							GUILayout.Label("*WARNING* NO TASK FOUND for ["+map.task+"] !!!!");
						}
					}
					string xl = expandList?"V ":"> ";
					GUILayout.BeginHorizontal ();
					GUILayout.Label (indent+"list:",GUILayout.ExpandWidth(false));
					if (GUILayout.Button(xl+map.list,GUILayout.ExpandWidth(false))) expandList = ! expandList;
					GUILayout.EndHorizontal();
				
						// some assumptions made about the way things are being used here, we should generalize.
						// we have a list of interactions, which point back to maps, which could have further lists or single tasks
						// here, we have assumed a single task, and that lets us open it for editing.
						// we could add a data field to the task, map, or interaction, that lets us flag it for editing, then
						// make these methods self contained and part of the each class.
						
						// for now, lets just assume lists of interactions point to maps with single tasks, and move forward...
				
					if (expandList && map.list != null){
						foreach (Interaction intr in InteractionMgr.GetInstance ().GetList(map.list).Interactions){
							EditorGUILayout.Space ();
							GUILayout.Label (indent+"    InteractionName:"+intr.Name); 
					//		GUILayout.Label ("    Map:"+intr.Map.item); // these 3 are the same as the intr name
					//		GUILayout.Label ("    Task:"+intr.Map.task);
					//		GUILayout.Label ("    TaskData:"+TaskMaster.GetInstance().GetTask(intr.Map.task).data.name);
						
							// if no task is open for edit, then allow opening this one
							if (intr.Map.task == null){ // there's no task for this, either add one or bail
								GUILayout.Label (indent+"    InteractionName:"+intr.Name+" has no task set yet. Bail for now.");
							}
							else
							{	// there is a map with a taskname.
								// is this the one opened task for this menuitem ?
								if (editingTaskKey == ""){  // then we can open this one
									if (GUILayout.Button ("Edit "+intr.Map.task)) editingTaskKey = intr.Map.task;
								}
								if (editingTaskKey == intr.Map.task){
									Color listColor = GUI.color;
									GUI.color = new Color(1,.7f,.7f,1);
									GUILayout.BeginHorizontal();
									intr.Map.task = EditorGUILayout.TextField ("task",intr.Map.task);
									if (GUILayout.Button ("SAVE",GUILayout.ExpandWidth(false))){
										//We have to save the task and task data, and maybe the interaction and its map as well!
									
									}
									if (GUILayout.Button ("CANCEL",GUILayout.ExpandWidth(false))){editingTaskKey = "";}
									GUILayout.EndHorizontal();
									intr.Name = EditorGUILayout.TextField ("Name",intr.Name);
									intr.Character = EditorGUILayout.TextField ("Character",intr.Character);
									EditorGUILayout.LabelField ("WaitTime: "+intr.WaitTime);
									EditorGUILayout.LabelField ("WaitTask: "+intr.WaitTask);
								
									if (TaskMaster.GetInstance().GetTask(intr.Map.task) != null){
										foreach (CharacterTask ct in TaskMaster.GetInstance().GetTask(intr.Map.task).data.characterTasks){
											GUILayout.Label ("-------Character Task-----------------------");
											ct.characterName = EditorGUILayout.TextField("characterName",ct.characterName);
											ct.nodeName = EditorGUILayout.TextField("nodeName",ct.nodeName);
											ct.posture = EditorGUILayout.TextField("posture",ct.posture);
											ct.lookAt = EditorGUILayout.TextField("lookAt",ct.lookAt);
											ct.animatedInteraction = EditorGUILayout.TextField("animatedInteraction",ct.animatedInteraction);
											GUILayout.Label (indent+"CT      delay:"+ct.delay);
											
										}
									}else{
										GUILayout.Label("*WARNING* NO TASK FOUND for ["+intr.Map.task+"] !!!!");
									}
									GUI.color = listColor;
								}
								
							}
						}
					}
					GUILayout.Label (indent+"time:"+map.time);
					GUILayout.Label (indent+"log:"+map.log);
					GUILayout.Label (indent+"max:"+map.max);
					GUILayout.Label (indent+"prereq:");
					if (map.prereq != null){
						foreach (string tag in map.prereq){
							GUILayout.Label (indent+"prereq: "+tag);
						}
					}
					
				}
			GUI.color = prevColor;
		}
	}
	
	public void ShowAddNodeGUI(){
		EditorGUILayout.Space();
		GUILayout.BeginHorizontal();
		if (newNode.map == null)
		{
			GUILayout.Label ("Add Submenu BELOW to "+info.DialogTitle+" Menu");
			// Not much needed here, 
		}
		else
		{
			GUILayout.Label ("Add Interaction BELOW to "+info.DialogTitle+" Menu");
			// gotta have an interaction map with a ...

		}
		
		if (GUILayout.Button ("CANCEL"))
		{
			newNode = null; // relies on garbage collection...
		}
		if (CheckIsValid(newNode))
			if (GUILayout.Button ("SAVE")){
				if (newNode.map == null){ // we've made a new submenu for our current
					//if its a submenu, add the XML our dialog items
					info.DialogItems.Add ("XML:"+newNode.filePath); // this is not exactly right. at all.
					Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
					serializer.Save("Assets/Resources/"+filePath+".xml",info); // updates this level of menu with the new child
					serializer.Save("Assets/Resources/XML/Interactions/"+newNode.filePath+".xml",newNode.info);
	
					// at some point, we need to create the new XML file at the specified path by serilaizing the ObjectInteractionInfo of the new node.
					// that should probably be done when we hit the SAVE CHANGES button, but we might do it here
				}
				else
				{	//we've added an interaction to an our current menu level, so update that with the item name
					info.DialogItems.Add (newNode.map.item); 
					Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
					serializer.Save("Assets/Resources/"+filePath+".xml",info);
					// we need to add the newNode.map to the interaction Manager and save out Interactions.xml
					// unless it's one that was already in there.
					// check to see if it exists
				
				    if (InteractionMgr.GetInstance().Get(newNode.map.item)== null){ // TODO make this a routine cause we need to do it elsewhere
						// and if not, add it, then serialize the interactions!
					
						InteractionMgr.GetInstance().Add(newNode.map);
						InteractionMgr.GetInstance().SaveXML("Assets/Resources/XML/Interactions/Interactions.xml");
					}
				}
			
				newNode.expanded = false; //looks better in the editor if itar closes when added
				children.Add(newNode);
				newNode = null;
			}
		GUILayout.EndHorizontal();
		EditorGUILayout.Space();
		if (newNode != null)
			newNode.ShowInspectorGUI (-1); // assuming this is sufficient for filling out the fields
	}
	
	public void EditTask(string label, string key, InteractionMap map){
		GUILayout.BeginHorizontal();
		
		
		
		
	}
	
	
	
	public string EditMappedString(string label,string key,InteractionMap map){
		GUILayout.BeginHorizontal ();
		GUILayout.Label (label,GUILayout.Width(125));
		// use this group of controls to display/edit a mapped string
		
		if (key == null){
			key = ""; // avoids errors with textField
		}
		
		// we'll only allow one stringmap to be edited at a time, and name of the key will flag
		// which one we are editing.
		
		// click the EDIT button to make us the one being edited.
		if (editingStrmapKey != "" && editingStrmapKey == key){
			// we are open for edit
			Color old = GUI.backgroundColor;
			GUI.backgroundColor = Color.red;
			GUILayout.Label ("Editing map key, value below");
			if (GUILayout.Button ("SAVE")){
				StringMgr.GetInstance().UpdateOrAdd(editStrmap.key,editStrmap.value);				
				editingStrmapKey = "";
				//editStrmap.key = ""; // this is pointing into the node's map so don't clear it!
				//editStrmap.value = "";
				// if the original key has changed, then we have to update the task or whatever this strmap was used in
				// kind of a hack, but...
				if (map != null){
					InteractionMgr.GetInstance().UpdateOrAdd(map);
					InteractionMgr.GetInstance().SaveXML("Assets/Resources/XML/Interactions/Interactions.xml");
				}
			}
			if (GUILayout.Button ("CANCEL")){
				editingStrmapKey = "";
				//editStrmap.key = "";
				//editStrmap.value = "";
			}
			GUILayout.EndHorizontal();
			editStrmap.key = EditorGUILayout.TextField("key = ",editStrmap.key); // how do we make the label narrower ?
			// if we are editing, and the key has been changed, we should look up the mapped value again...
			if (key != editStrmap.key){
				editStrmap.value = StringMgr.GetInstance().Get(editStrmap.key);
				if (editStrmap.value == editStrmap.key) editStrmap.value = "NOT MAPPED YET";
				key = editStrmap.key; // pass back the edited value
				editingStrmapKey = key;
			}
			editStrmap.value = GUILayout.TextField(editStrmap.value);
			GUI.backgroundColor = old;
			GUILayout.BeginHorizontal();
		}
		else
		{
			// we are just displaying
			string mappedValue = StringMgr.GetInstance().Get(key);
			if (mappedValue == key) mappedValue = "NOT MAPPED YET";
			GUILayout.Label("["+key+"]:"+mappedValue,GUILayout.Width(175));
			if (editingStrmapKey == ""){
				if (GUILayout.Button ("Edit Stringmap",GUILayout.Width(100))){
					if (key == "") key = map.item; // we can't lock to an empty key, so try this initial value
					editingStrmapKey = key;
					editStrmap.key = key;			// initialize the persistent temp strmap
					editStrmap.value = mappedValue;
				}
			}
			else
				GUILayout.Button ("-edit is busy-");
		}
		GUILayout.EndHorizontal();
		return key;
	}
	
	public bool CheckIsValid(MenuTreeNode node){
		if (node == null) return false;
		if (node.filePath != null && node.filePath.Contains ("ENTER")) return false;
		if (node.info != null && node.info.DialogTitle != null && node.info.DialogTitle.Contains ("ENTER")) return false;
		// here, depending on the node type, see if there is enough info to use this node
		return node.valid;	
	}
	
	bool DeleteButton(){
		Color old = GUI.color;
		GUI.color = Color.red;
		bool clicked = GUILayout.Button ("X",GUILayout.ExpandWidth(false));
		GUI.color = old;
		return clicked;
	}
		
}
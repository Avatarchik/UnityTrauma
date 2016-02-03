using UnityEngine;
using UnityEditor;
using System.Collections;

public class RoleEditorWindow : EditorWindow {
	public ScriptedObject myLeftRole = null;
	public ScriptedObject myRightRole = null;
//	ScriptedObject requestedRole = null;
	public static RoleEditorWindow instance = null;
	Vector2 leftScrollPos = Vector2.zero;
	Vector2 rightScrollPos = Vector2.zero;
	bool leftHasChanged = false;
	bool rightHasChanged = false;
//	bool confirmChange = false;

	SerializedObject serializedObject;
	int leftConfirmDelete = -1;
	int rightConfirmDelete = -1;
	int headerHeight = 40;
	
	// to be able to drag and drop scripted interactions, we need to wrap each up in an object derived from 
	// unity's GUIDraggableObject.  There is no such class, so just build it in here...
	Color dropColor = Color.white;
	Rect dropTarget; // rectangle the mouse is over to drop a script
	int dropTargetIndex = -1; // where we would insert into scripts array on drop
	ScriptedObject dropTargetRole = null;
	Vector2 dragStart; // set during the MouseDrag event pass when nothing is being dragged
	GameObject dragObject = null;
	
	// temporary debug variables
//	string eventtype = "";
//	string mouseloc = "";

	public static void Init(ScriptedObject role){
		instance = (RoleEditorWindow)EditorWindow.GetWindow(typeof(RoleEditorWindow));
		// be sure we don't blow away and edits when switching scripts...
//		instance.requestedScript = script;
		// what if we start running before saving ? we should probably warn for that too.
//		if (!(EditorApplication.isPlaying || EditorApplication.isPaused))
			// don't load up yet, put up a confirmation dialog first.
//			instance.confirmChange=true;
//			return;
//		}
		instance.myLeftRole = role;
		instance.title = "Role Editor";

		instance.leftHasChanged = false;
		instance.rightHasChanged = false;
//		EditorApplication.playmodeStateChanged += instance.PlaymodeCallback;
	}
	
//	void Awake(){
//		EditorApplication.playmodeStateChanged += PlaymodeCallback;
//	}
	
	void OnGUI(){
//		wantsMouseMove = true;
//		mouseloc = Event.current.mousePosition.ToString() + dropTarget.ToString();
		
		// handle drag/drop mouse events --------------------------------------------------------
		if (Event.current.type == EventType.mouseDrag){
			if (dragObject == null){
				// mark where the drag started so the Repaint can check
				dragStart = Event.current.mousePosition;
				dragStart.y -= headerHeight+20; // there's some strange offset required here...
			}
			else
			{   // dragging
				if ( dropTarget.Contains(Event.current.mousePosition)){
					// handle highlight of drop target  
					dropColor = Color.green;
				}
				else
					dropColor = Color.white;
			}
		}
		
		if (Event.current.type == EventType.mouseUp && dragObject != null){
			dropColor = Color.red;
			if ( dropTarget.Contains(Event.current.mousePosition)){
				// Perform Drop
				if (dropTargetRole == myLeftRole)
					leftHasChanged = true;
				else
					rightHasChanged = true;
				
				// see if we are rearranging within a role, or copying to a new role
				if (dragObject.transform.parent.gameObject == dropTargetRole.gameObject){
					// rearranging.  Need to know if we are moving up or down in the list,
					// because the dropTargetIndex changes if current index is lower.
					InteractionScript movingScript = dragObject.GetComponent<InteractionScript>();
					int currentIndex = dropTargetRole.IndexOf(movingScript);
					if (currentIndex < 0){
						// we should only be seeing array elements here... use Adopt drop target to reparent.
						return;  
					}
					else
					{
						if (dropTargetIndex > 0 && currentIndex <= dropTargetIndex)
							dropTargetIndex --; // drop target index is never < 1
						dropTargetRole.RemoveScript(movingScript);
						dropTargetRole.InsertScriptAt(movingScript,dropTargetIndex);
					}
				}
				else
				{ // duplicate entire InteractionScript,gameObject, actions, etc...and insert into drop Role
					GameObject newScriptObject = Instantiate(dragObject) as GameObject;
					newScriptObject.name = dragObject.name;
					// parent to the target role
					newScriptObject.transform.parent = dropTargetRole.transform;
					// convert role references in scripted actions of the new script if possible...TODO!
			
					InteractionScript newScript = newScriptObject.GetComponent<InteractionScript>();
					dropTargetRole.InsertScriptAt(newScript,dropTargetIndex);
					dragObject = null;
					dragStart = Vector2.zero;
					return; // does this avoid the GUI Element count error ?
				}
				dropColor = Color.yellow; // for debugging only
			}
			// un drag, no matter
			dragObject = null;
			dragStart = Vector2.zero;
		}
		// END of mouse even processing for drag and drop ------------------------------------------------------
		
		GUILayout.Label ("DRAG and DROP Interactions between Roles.  Drag roles (ScriptedObjects) to edit in from Hierarchy");
/*		if (GUILayout.Button (Event.current.type.ToString()+" "+mouseloc)){
			eventtype="";
			mouseloc="";
			dropColor = Color.white;
		}
*/
		
		// draw the left role
		GUILayout.BeginArea (new Rect(0,headerHeight ,position.width*.5f,position.height));
		GUILayout.BeginHorizontal ();
		myLeftRole = (ScriptedObject)EditorGUILayout.ObjectField("ROLE:",myLeftRole,typeof(ScriptedObject),true,GUILayout.Width(position.width*.5f-100));
		if (myLeftRole != null){
			if (leftHasChanged){
				// SAVE BUTTON!
				GUI.color = Color.green;
				if ( GUILayout.Button("SAVE")){
					SaveRole(myLeftRole);
					leftHasChanged = false;
				}
				GUI.color = Color.red;
				if ( GUILayout.Button("UNDO")){
					RevertRole(myLeftRole);
					leftHasChanged = false;
				}
				GUI.color = Color.white;
			}
			else{
				if ( GUILayout.Button("CLEAR"))
					myLeftRole = null;	
			}
		}
		GUILayout.EndHorizontal();
		if (myLeftRole != null){
			DrawRole(myLeftRole,ref leftScrollPos,ref leftConfirmDelete,ref leftHasChanged,0,headerHeight+20);
		}
		GUILayout.EndArea();
		
		// draw the right role 
		GUILayout.BeginArea (new Rect(position.width*.5f,headerHeight ,position.width*.5f,position.height));
		GUILayout.BeginHorizontal ();
		myRightRole = (ScriptedObject)EditorGUILayout.ObjectField("ROLE:",myRightRole,typeof(ScriptedObject),true,GUILayout.Width(position.width*.5f-100));
		if (myRightRole != null){
			if (rightHasChanged){
				// SAVE BUTTON!
				GUI.color = Color.green;
				if ( GUILayout.Button("SAVE")){
					SaveRole(myRightRole);
					rightHasChanged = false;
				}
				GUI.color = Color.red;
				if ( GUILayout.Button("UNDO")){
					RevertRole(myRightRole);
					rightHasChanged = false;
				}
				GUI.color = Color.white;
			}
			else{
				if ( GUILayout.Button("CLEAR"))
					myRightRole = null;	
			}
		}
		GUILayout.EndHorizontal();
		if (myRightRole != null){
			DrawRole(myRightRole,ref rightScrollPos,ref rightConfirmDelete,ref rightHasChanged,position.width*.5f,headerHeight+20);
		}
		GUILayout.EndArea();
		
		// show dragObject last
		if (dragObject == null)
			GUI.Button(new Rect(0,0,0,0),""); // dummy button to keep GUI element count even
		else{
			GUI.color = Color.green;
			if (GUI.Button(new Rect(Event.current.mousePosition.x-20,Event.current.mousePosition.y+10,250,20),dragObject.name)){
				dragObject = null;
				dragStart = Vector2.zero;
			}
			GUI.color = Color.green;
		}

	}
	
	void DrawRole(ScriptedObject role,ref Vector2 scrollPos,ref int confirmDelete,ref bool hasChanged, float areaOffsetX,float areaOffsetY){
		Rect lastRect = new Rect(0,0,0,0);
		if (role == null || role.scripts == null){
			GUILayout.Label("I lost my Role somewhere... sorry");
			return;
		}
		GUI.color = Color.white;

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		for (int i=0; i< role.scripts.Length; i++){
			if (confirmDelete == i){
				GUILayout.Label("CONFIRM Delete? (Script will remain as orphan)");
				if (GUILayout.Button("YES, ORPHAN the script.")){
					role.RemoveScript(role.scripts[i]);
					hasChanged = true;
					confirmDelete = -1;
					return; // does this avoid the GUI count error ?
				}
				if (GUILayout.Button("Oops, no, Keep the script")){
					confirmDelete = -1;
				}
				
			}
			bool expand = false;
			GUILayout.BeginHorizontal();
			GUI.backgroundColor = Color.grey;
			if (GUILayout.Button (new GUIContent("X",null,"delete line"),GUILayout.ExpandWidth(false))){
				confirmDelete = i;
				break; // we've affected the array, don't draw any more gui this frame.
			}

			if (role.scripts[i]!=null && Selection.activeObject != role.scripts[i].gameObject){
				GUI.backgroundColor = Color.yellow;
				if (GUILayout.Button (new GUIContent(">",null,"EDIT line"),GUILayout.ExpandWidth(false))){
					Selection.activeGameObject = role.scripts[i].gameObject;
					ScriptViewWindow.Init(role.scripts[i]);
				}
			} else {
				GUI.backgroundColor = Color.red;
				expand = true;
				if (GUILayout.Button (new GUIContent("V",null,"CLOSE line"),GUILayout.ExpandWidth(false)))
					Selection.activeGameObject = role.gameObject;
			}
//			string prefix = i<10?"  ":"";
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			GUILayout.Button(role.scripts[i].name+" | "+role.scripts[i].prettyname);
			// DRAG SOURCE from the above button
			// place this right after you draw the GUI element to drag from:--------------------------------
			// we pick the drag source during the Repaint pass, using the drag start that was set during the
			// EventMouseDrag handler when there was no Drag Object set ...
			if(Event.current.type == EventType.Repaint){
				lastRect = GUILayoutUtility.GetLastRect();
				Rect dragCheckRect = lastRect; // need to check against window coords, not area coords
				dragCheckRect.x += areaOffsetX;
				dragCheckRect.y -= scrollPos.y;
				if(dragObject == null && dragCheckRect.Contains(dragStart))
					dragObject = role.scripts[i].gameObject;
			}
			//-----------------------------------------End Drag Source handler -------------------------------			
			
			GUI.color = Color.white;
//			if (GUILayout.Button (new GUIContent("?",null,"show help"),GUILayout.ExpandWidth(false))) helpTarget = myScript.scriptLines[i];
			GUILayout.EndHorizontal();			
			
			// Set up a possible drop target button if we are dragging something over the item we just showed
			// we use GUI.Button instead of GUILayout.Button for better control of the drop target display
			Rect nextRect = lastRect;
			nextRect.y += nextRect.height/2; // move the drop target down a little to represent "insert after"
			if (dragObject != null && nextRect.Contains(Event.current.mousePosition)){
				GUILayoutUtility.GetRect (nextRect.width,nextRect.width,nextRect.height,nextRect.height); // reserve space for the drop target
				GUI.color = dropColor; // set in the MouseDrag pass to debug, but we don't really need that since it's working
				string verb = "COPY ";
				if (dragObject.transform.parent.gameObject == role.gameObject)
					verb = "MOVE ";
				GUI.Button(nextRect,verb+dragObject.name+" HERE ["+dropTargetIndex+"]");
				if(Event.current.type == EventType.Repaint){
					dropTargetIndex = i+1;  // where we would insert in the list...
					dropTargetRole = role;
					dropTarget = nextRect;
					dropTarget.x += areaOffsetX;
					dropTarget.y += areaOffsetY;  // relative to the window origin, not the GUI area...
				}
				GUI.color = Color.white;	
			}
			else {
				// dummy button to keep the count even
				GUILayoutUtility.GetRect (0,0,0,0);
				GUI.Button(new Rect(0,0,0,0),"");
			}
		}
		
		InteractionScript adoptScript = null;
		adoptScript = (InteractionScript)EditorGUILayout.ObjectField("Re-Adopt Child Script:",adoptScript,typeof(InteractionScript),true,GUILayout.Width(position.width*.5f-20));
		if (adoptScript != null){
			// be sure this is our child...
			if (adoptScript.transform.parent.gameObject == role.gameObject){
				role.InsertScriptAt(adoptScript, role.scripts.Length);
				hasChanged = true;
				return; 
			}
			
		}
		
		GUI.backgroundColor = Color.white;
		if (GUILayout.Button ("",GUILayout.ExpandWidth(false))){ //extra line so we can scroll to the bottom

		}
		EditorGUILayout.EndScrollView();
//		hasChanged |= GUI.changed; // only when WE say it has.
		if (GUI.changed) EditorUtility.SetDirty(role); // this helps keep changes		
		
	}
	
	void SaveRole(ScriptedObject role){
		role.SaveToXML(role.XMLName);
		// update the prefab too!
		GameObject vup = PrefabUtility.FindValidUploadPrefabInstanceRoot(role.gameObject);
//		Debug.Log (po.ToString()+pp.ToString()+pr.ToString()+rgo.ToString()+vup.ToString());
		
		if ( vup != null){
			PrefabUtility.ReplacePrefab (vup,
									PrefabUtility.GetPrefabParent(vup),
									ReplacePrefabOptions.ConnectToPrefab); // GetPrefabObject crashed unity editor...
		}
		
		
//		PrefabUtility.ReplacePrefab (role.gameObject,
//									PrefabUtility.GetPrefabParent(role.gameObject),
//									ReplacePrefabOptions.ConnectToPrefab); // GetPrefabObject crashed unity editor...
	}
	
	void RevertRole(ScriptedObject role){
//		role.LoadFromXML(role.XMLName); // reverting from the prefab is preferred.
		// because LOAD disconnects...
//		PrefabUtility.ResetToPrefabState(role.gameObject); // this seems to work strangely like an 'undo'
//		PrefabUtility.ReconnectToLastPrefab(role.gameObject);
		PrefabUtility.RevertPrefabInstance(role.gameObject); 
	}
	
	
/*  Lessons learned:
 * 
 * OnGUI is called multiple times with different event types different, for mouse stuff and repainting. 
 * Some things have to be done during the repainting pass, some during the mouse event pass, so info has to
 * be set up and kept between those passes.  the rectangles used by GUILayout are only known during the repaint pass.
 * 
 * There are also some surprises when using Areas, as the element rectangles are Area relative, but the mouse coords are not.
 * To do it all over, maybe just forget GUILayout and calculate your own placement.  GUI gets very upset if the number of controls
 * varies between passes, so adding a button dynamicaly will usually result in an error.
 */
	
	
}

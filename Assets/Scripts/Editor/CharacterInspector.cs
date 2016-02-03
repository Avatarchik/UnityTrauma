using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CustomEditor(typeof(Character))]

// have to add at this class level because this is where the XML Name comes in...

public class CharacterInspector : ObjectInteractionInspector 
{
	Character myObject;

	override public void OnSelected() // look at a particular instance object
	{
		myObject = target as Character;
//		myObject.Awake ();

		menuTree = MenuTreeNode.BuildMenu(myObject.XMLName); // create an editable representation of the object's interactions
		base.OnSelected();
	}
	
	override public void OnInspectorGUI(){
		base.OnInspectorGUI();
		if (myObject != null && myObject.attachedObjects.Count>0){
			GUILayout.Label("AttachedObjects");
			foreach (GameObject go in myObject.attachedObjects){
				GUILayout.Label(go.name);
			}
		}
		if (myObject != null && myObject.lastDetachedObject != null)
			GUILayout.Label ("LastDetached="+myObject.lastDetachedObject.name);
	}
}



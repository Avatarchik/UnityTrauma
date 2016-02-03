using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CustomEditor(typeof(TraumaObject))]

// this file should parallel Character Inspector
// have to add at this class level because this is where the XML Name comes in...

public class TraumaObjectInspector : ObjectInteractionInspector 
{
	TraumaObject myObject;

	override public void OnSelected() // look at a particular instance object
	{
		myObject = target as TraumaObject;
		myObject.Awake ();

		menuTree = MenuTreeNode.BuildMenu(myObject.XMLName); // create an editable representation of the object's interactions
		base.OnSelected();
	}
}



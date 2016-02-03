using UnityEditor;
using UnityEngine;
using System;


/* CODE THAT ADDS MyClass in the 
 * 
	if ( GUILayout.Button("Add MyClass") )
	{
		GameObject newobj = new GameObject("myclass");
		if ( newobj != null )
		{
			newobj.AddComponent(typeof(Myclass));
		}
	}				
*/


[CustomEditor(typeof(Myclass))]

public class MyclassInspector : Editor 
{
	Myclass myObject;
	bool showDefaultInspector=true;
	
	public MyclassInspector ()
	{
	}

	public override void OnInspectorGUI()
	{
		if ( myObject == null )
			myObject = target as Myclass;
		
		GUILayout.Label(">>>>>>>>>>>>>>>>>>");
		GUILayout.Label("Myclass Inspector");
		GUILayout.Label(">>>>>>>>>>>>>>>>>>");
		
		if ( GUILayout.Button("Change Name") )
		{
			myObject.SetName("New Name");
		}
		
		// toggle display of inspector GUI
		showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "Show Default Inspector");
		if (showDefaultInspector)
			DrawDefaultInspector();
		else
			myObject.OnInspectorGUI();			
	}
}

/*

// Myclass wouldn't normally be sitting with the Inspector.  I just have it here for clarity

public class Myclass : MonoBehaviour
{
	public string TestString = "Test";
	public float floatVar;
	public bool boolVar;
	
	// set class name
	private string name="MyClass";	
	public void SetName( string newname )
	{
		name = newname;
	}
	private GameObject go;
	
#if UNITY_EDITOR
	public void OnInspectorGUI()
	{
		GUILayout.Label("******** Myclass.OnInspectorGUI");
		// text field
		this.name = EditorGUILayout.TextField("name",this.name);
		// object field.... can be any Unity Object
		this.go = (GameObject)EditorGUILayout.ObjectField("object",this.go,typeof(GameObject));
		//
		GUILayout.Label("********");
	}
#endif
	
	public void OnGUI()
	{
		GUILayout.Label("MyClass, name=<" + this.name + ">");
	}
}

*/

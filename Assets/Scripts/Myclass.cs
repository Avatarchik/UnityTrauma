using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Myclass wouldn't normally be sitting with the Inspector.  I just have it hear for clarity

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
		// float
		this.floatVar = EditorGUILayout.FloatField("floatVar",this.floatVar);
		// bool
		this.boolVar = EditorGUILayout.Toggle("boolVar",this.boolVar);
		// object field.... can be any Unity Object
		this.go = (GameObject)EditorGUILayout.ObjectField("object",this.go,typeof(GameObject),true);
		// do something special
		if ( GUILayout.Button("My custom button that changes the world") )
		{}
		//
		GUILayout.Label("********");
	}
#endif
	
	public void OnGUI()
	{
		GUILayout.Label("MyClass, name=<" + this.name + ">");
	}
}
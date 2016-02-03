using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CustomEditor(typeof(FilterInteractions))]

public class FilterInteractionsInspector : Editor 
{
	FilterInteractions myObject;

	public override void OnInspectorGUI()
	{
		if (myObject == null)
			myObject = target as FilterInteractions;
		if (myObject != null)
		{
			if ( GUILayout.Button("Perform NLU Test") )
				myObject.TestCommandVariations();
		}
		base.DrawDefaultInspector();		
	}
}



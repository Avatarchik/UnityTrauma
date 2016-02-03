using UnityEngine;
using UnityEditor;
using System.Collections;

public class TransformSnapper : ScriptableObject
{
	private static ArrayList transformSaves = new ArrayList();
	private static string snaptoname = "NEED TO SET THIS FIRST";
	
	[MenuItem ("Custom/Transform Snapper/Record SnapTo Transform")]
	static void DoRecord()
	{
		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
		if (selection.Length != 1){
			EditorUtility.DisplayDialog("Transform Snapper", "Must select only one transform to snap to.", "OK", "");
			return;
		}
		transformSaves = new ArrayList(selection.Length);
		
		foreach (Transform selected in selection)
		{
			//TransformSave transformSave = new TransformSave( selected.localPosition, selected.localRotation, selected.localScale);
			transformSaves.Add(selected);
			snaptoname = selected.name;
		}
		
		EditorUtility.DisplayDialog("Transform Snapper Set", "Will now snap to "+snaptoname, "OK", "");
	}

	[MenuItem ("Custom/Transform Snapper/Snap Selected to Saved Snap transform")]
	static void DoApply()
	{
		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
		if (selection.Length != 1){
			EditorUtility.DisplayDialog("Transform Snapper", "Must select only one transform to snap.", "OK", "");
			return;
		}
		int numberApplied = 0;
		
		Transform snapTo = (Transform) transformSaves[0];
		
		selection[0].position = snapTo.position;
		selection[0].rotation = snapTo.rotation;
//		selection[0].localScale = snapTo.localScale;
		numberApplied++;
		
		EditorUtility.DisplayDialog("Transform Snapper", selection[0].name+" snapped to "+snaptoname, "OK", "");
	}
}

using UnityEngine;
using System.Collections;
using UnityEditor;
 
public class MySelectionScripts : MonoBehaviour {
 
    [MenuItem ("Custom/Selection/Deactivate objects %#d")]
    static void DoDeactivate()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            go.active = false;
			go.SetActiveRecursively(false);
        }
    }
 
    [MenuItem("Custom/Selection/Activate objects %#a")]
    static void DoActivate()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            go.active = true;
			go.SetActiveRecursively(true);
        }
    }
 
}

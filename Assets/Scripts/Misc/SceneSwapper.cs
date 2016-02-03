using UnityEngine;
using System.Collections;

public class SceneSwapper : MonoBehaviour {
	
	// created to switch from the editing scene to the starting scene,
	// simply loads the named scene.
	
	public string sceneToLoad = "";

	// Use this for initialization
	void Awake () {
		if (sceneToLoad != "")
			Application.LoadLevel (sceneToLoad);
	
	}
}

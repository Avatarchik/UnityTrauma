using UnityEngine;
using System.Collections;

public class GoToNextScene : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI() {
		if(GUI.Button(new Rect(0, 0, 300, 20), "Current: " + Application.loadedLevelName + " Go to Next"))
			Application.LoadLevel(Application.loadedLevel+1);
	}
}
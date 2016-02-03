using UnityEngine;
using System.Collections;

public class AudioFadeIn : MonoBehaviour {

	public float fadeLength = 5;
	float targetVolume;
	bool fadeComplete = false;

	void Awake(){
		targetVolume = audio.volume;
		audio.volume = 0;

	}

	// Use this for initialization
	void Start () {
		audio.Play ();
	
	}
	
	// Update is called once per frame
	void Update () {
		if (!fadeComplete){
			audio.volume = targetVolume*Time.timeSinceLevelLoad/fadeLength;
			if (Time.timeSinceLevelLoad > fadeLength){
				audio.volume = targetVolume;
				fadeComplete = true;
				this.enabled = false;
			}
		}
	
	}
}

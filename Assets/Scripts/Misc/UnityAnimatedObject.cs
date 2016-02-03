using UnityEngine;
using System.Collections;

public class UnityAnimatedObject : MonoBehaviour {

	public Camera myCamera = null;
	Transform originalParent = null;
	public bool reparent = true;

	// Use this for initialization
	void Start () {
		originalParent = transform.parent;
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void PlayAnimation(string clipName){
		if (clipName != null && clipName != "" && animation [clipName] != null)
			animation.clip = animation [clipName].clip;
		if (reparent) {
			transform.parent = null;
			Invoke ("Reparent", animation.clip.length);
		}
		animation.Play ();

		Debug.Log (name + " got PlayAnimation message");
	}

	public void LockMainCamera(){
		CameraLERP cl = Camera.main.GetComponent<CameraLERP> ();
		cl.MoveTo (transform, 0, true, true);
	}

	public void UnlockMainCamera(){
		CameraLERP cl = Camera.main.GetComponent<CameraLERP> ();
		cl.MoveTo (transform, 0, true, true);
	}

	void Reparent(){
		transform.parent = originalParent;
	}

	public void EnableCamera(){
		if (myCamera != null)
			myCamera.enabled = true;
	}

	public void DisableCamera(){
		if (myCamera != null)
			myCamera.enabled = false;
	}
}

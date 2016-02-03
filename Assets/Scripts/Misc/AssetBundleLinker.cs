using UnityEngine;
using System.Collections;

public class AssetBundleLinker : MonoBehaviour {
	
//	public AssetBundleInfo assetBundleInfo = new AssetBundleInfo();
	
	// Use this for initialization
	void Start () {
		// this script on a newly instantiated game object from an asset bundle, needs to connect the dots.
		Link ();
	
	}
	
	public void Link(){
		// The scripted Object class knows how to transfer scripts,
		foreach (Animation anim in GetComponentsInChildren<Animation>()){
			// find all characters with the matching named role
			string role = anim.name.Replace("Role_","");
			
			// use the dispatcher to find characters with this role
			Dispatcher.GetInstance().AssignAnimations(anim,role);
			
		}
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

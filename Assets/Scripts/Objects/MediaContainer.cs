using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MediaContainer : MonoBehaviour
{
    public string XMLName;
	public string XMLDirectory = "Assets/Resources/XML/";
	
//TODO Add to Info!	
	[System.Serializable]
	public class TaggedBundle{
		public string tag = "";
		public AssetBundleInfo assetBundleInfo = null;
		public VoiceList voiceList = null;
		public List<VoiceList> voiceLists;
		public List<VitalsBehavior> vitalsBehaviors = null;
		public List<ScanRecord> scanRecords; // for overloading with case specific media
	}
	bool prefabNeedsUpdate = false;

	public List<TaggedBundle> bundles;
	public bool PrefabNeedsUpdate(){ return prefabNeedsUpdate;}

	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	// lets implement this as step 2, once we see that we've met the requirements

	public class MediaContainerInfo
	{
		public MediaContainerInfo(){
		}
		
		public string unityObjectName;

	    public string XMLName;
	    public string XMLDirectory;
		public AssetBundleInfo assetBundleInfo;
		public VoiceList voiceList;
		public List<VoiceList> voiceLists;
		public List<VitalsBehavior> vitalsBehaviors;	
		public List<ScanRecord> scanRecords; // for overloading with case specific media
	}
	/*		
	// because the scripted objects thenselves are generally not created from serialized data when the game is run,
	// but rather instantiated from prefabs or placed in the level or asset bundle, these serialize routines
	// may not really matter.  The individual interaction scripts/actions, however, really do have to be right!!
	public MediaContainerInfo ToInfo(MediaContainer so){ // saves values to an info for serialization (to XML)
		MediaContainerInfo info = new MediaContainerInfo();
		
		info.unityObjectName = so.name;
		info.XMLName = so.XMLName;
		info.XMLDirectory = so.XMLDirectory;
		
		info.assetBundleInfo = so.assetBundleInfo;
		info.voiceList = so.voiceList;
		info.voiceLists = so.voiceLists;
		info.vitalsBehaviors = so.vitalsBehaviors;
		info.scanRecords = so.scanRecords;
	
		return info;
	}
	
	public void InitFrom(MediaContainerInfo info){
		// we should probably destroy any existing hierarchy here, calling OnDestroy() on our children;
		
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName;

		XMLName = info.XMLName;
		XMLDirectory = info.XMLDirectory;
		assetBundleInfo = info.assetBundleInfo;
		voiceList = info.voiceList;
		voiceLists = info.voiceLists;
		vitalsBehaviors = info.vitalsBehaviors;
		scanRecords = info.scanRecords;
	}
		
	public void LinkFrom(MediaContainerInfo info){
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName;

		XMLName = info.XMLName;
		XMLDirectory = info.XMLDirectory;
		assetBundleInfo = info.assetBundleInfo;
		voiceList = info.voiceList;
		voiceLists = info.voiceLists;
		vitalsBehaviors = info.vitalsBehaviors;
		scanRecords = info.scanRecords;
		
	}
	public void AppendFrom(MediaContainerInfo info){

	}	
	
	public void SaveToXML(string pathname){
		XMLName = pathname;
		MediaContainerInfo info = ToInfo (this);
		XmlSerializer serializer = new XmlSerializer(typeof(MediaContainerInfo));
		FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Create);
		serializer.Serialize(stream, info);
		stream.Close();	
#if UNITY_EDITOR
		// call to update the database to make sure this asset
		// will be reimported
		UnityEditor.AssetDatabase.Refresh ();
#endif
		prefabNeedsUpdate = true;
	}
	
	public void SaveToPrefab(){

#if UNITY_EDITOR
		// also update our prefab!
//		UnityEngine.Object po = PrefabUtility.GetPrefabObject(gameObject);
//		UnityEngine.Object pp = PrefabUtility.GetPrefabParent(gameObject);
//		GameObject pr = PrefabUtility.FindPrefabRoot(gameObject);
//		GameObject rgo = PrefabUtility.FindRootGameObjectWithSameParentPrefab(gameObject);
		GameObject vup = PrefabUtility.FindValidUploadPrefabInstanceRoot(gameObject);
//		Debug.Log (po.ToString()+pp.ToString()+pr.ToString()+rgo.ToString()+vup.ToString());
		
		if ( vup != null){
			PrefabUtility.ReplacePrefab (vup,
									PrefabUtility.GetPrefabParent(vup),
									ReplacePrefabOptions.ConnectToPrefab); // GetPrefabObject crashed unity editor...
			EditorUtility.DisplayDialog("Reminder","You must also save the scene for prefab to be updated","OK");
			prefabNeedsUpdate = false;
		}
#endif
	}
	
	public void LoadFromXML(string pathname){
		XmlSerializer serializer = new XmlSerializer(typeof(MediaContainerInfo));
		FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
		MediaContainerInfo info = serializer.Deserialize(stream) as MediaContainerInfo;
		stream.Close();

		InitFrom(info);			
	}
	*/
	public void LoadLinkablesFromXML(string pathname){ // everything except the scripts
		if (pathname == null || pathname == "") return;
		MediaContainerInfo info=null;
/*		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObjectInfo));
			FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
			info = serializer.Deserialize(stream) as ScriptedObjectInfo;
			stream.Close();
		}
		else
*/
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<MediaContainerInfo> serializer = new Serializer<MediaContainerInfo>();
			pathname = "XML/"+pathname.Replace (".xml","");
			info = serializer.Load(pathname);
			if ( info == null )
			{
				UnityEngine.Debug.LogError("LoadFromXML(" + pathname + ") : error serializing!");
				return;
			}
		}
//		LinkFrom(info);			
	}
	
	public void AppendFromXML(string pathname){
		MediaContainerInfo info=null;
		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(MediaContainerInfo));
			FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
			info = serializer.Deserialize(stream) as MediaContainerInfo;
			stream.Close();
		}
		else
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<MediaContainerInfo> serializer = new Serializer<MediaContainerInfo>();
			pathname = "XML/"+pathname.Replace (".xml","");
			info = serializer.Load(pathname);
		}
//		AppendFrom(info);			
	}
	/* ----------------------------  SERIALIZATION ----------------------------------------- */		
	
	
    public void Awake()
    {
		// this is a kind of a hack, the base trauma prefab exists just to cause the scripts to get loaded
		// for each of its scripted objects, which have no scripts for size reasons, but they can be out of sync
		// with any linkable content added in the editing versions of those scripted objects, so this syncs them.
		// this keeps the placeholder prefabs in the BasePrefab matching the data saved to the xml.

		 //?? do we do this ?LoadLinkablesFromXML(XMLName);	// this might only work in the editor ?
    }

	public void ActivateAllMedia(){ // this is what a scripted object does with it's media, but we don't do that automatically
		foreach (TaggedBundle bundle in bundles)
			ActivateBundle (bundle);
	}

	public void ActivateBundle( TaggedBundle bundle){
		// is there an asset bundle to be loaded with this ?
		if (bundle.assetBundleInfo != null){
			AssetBundleLoader.GetInstance().Load(bundle.assetBundleInfo);	
		}
		
		// Unlike scripted object, we DO NOT automatically add our stuff on awake...
		// add any Voicemaps or Vitals behaviors we are carrying
		
		if (bundle.voiceList != null && bundle.voiceList.Name != ""){
			VoiceMgr.GetInstance().AddVoiceList(bundle.voiceList);
		}
		
		if (bundle.voiceLists != null){
			foreach (VoiceList vl in bundle.voiceLists){
				VoiceMgr.GetInstance().AddVoiceList(vl);
			}
		}
		
		if (bundle.vitalsBehaviors != null && bundle.vitalsBehaviors.Count > 0){
			foreach (VitalsBehavior vb in bundle.vitalsBehaviors){
				VitalsBehaviorManager.GetInstance().AddToLibrary(vb);
			}
		}
		
		// is this late enough to cause override of any content in the patient records ?
		if (bundle.scanRecords != null && bundle.scanRecords.Count > 0){
			Patient patient = FindObjectOfType<Patient>();
			if (patient != null){
				foreach (ScanRecord record in bundle.scanRecords){
					patient.LoadScanRecord(record);
				}
			}
		}

	}


	//THIS METHOD IS BEING CALLED BY a UnityMessage on the LinkableContent object in Trauma,
	// for some reason I can't recall, that ended up being the best solution at the time.
	// I envisioned that put message would have been called, but perhaps there were problems with that scheme
	// put message would have tried to ActivateTaggedBundle with the supplied tag...


	public void ActivateTaggedBundle(string tag){
	
		// see if we have a bundle with this tag
		foreach (TaggedBundle bundle in bundles) {
			if (bundle.tag == tag)
				ActivateBundle (bundle);
		}
	
	
	}


    public void Start()
    {
    }
	
	public void Update(){

	}

    public void PutMessage( GameMsg msg ) 
    {

    }

}

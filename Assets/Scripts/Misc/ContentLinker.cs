using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

public class ContentLinker : MonoBehaviour {
	
	public AssetBundleInfo assetBundleInfo = null;
	public List<VoiceList> voiceLists = null;
	public List<VitalsBehavior> vitalsBehaviors = null;
	public List<string> assessmentListXMLs = null;
	
	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	// include save/load to xml just for human readable backups...
	public class LinkableContentInfo
	{
		public LinkableContentInfo(){
		}
		
		public AssetBundleInfo assetBundleInfo;
		public List<VoiceList> voiceLists;
		public List<VitalsBehavior> vitalsBehaviors;	
		public List<string> assessmentListXMLs;
	}
	
	public LinkableContentInfo ToInfo(ContentLinker item){
		LinkableContentInfo info = new LinkableContentInfo();
		info.assetBundleInfo = item.assetBundleInfo;
		info.voiceLists = item.voiceLists;
		info.vitalsBehaviors = item.vitalsBehaviors;
		info.assessmentListXMLs = item.assessmentListXMLs;
		return info;
	}
	public void InitFrom(LinkableContentInfo info){
		assetBundleInfo = info.assetBundleInfo;
		voiceLists = info.voiceLists;
		vitalsBehaviors = info.vitalsBehaviors;
		assessmentListXMLs = info.assessmentListXMLs;
	}
	void SaveToXML(string filepath){		
		LinkableContentInfo info = ToInfo (this);
		XmlSerializer serializer = new XmlSerializer(typeof(LinkableContentInfo));
		FileStream stream = new FileStream( filepath, FileMode.Create);
		serializer.Serialize(stream, info);
		stream.Close();	
	}	
	void LoadFromXML(string filepath){
			Serializer<LinkableContentInfo> serializer = new Serializer<LinkableContentInfo>();
			filepath = "XML/"+filepath.Replace (".xml","");
			LinkableContentInfo info = new LinkableContentInfo();
			info = serializer.Load(filepath);
			InitFrom( info );
	}
		
    public void Awake()
    {
		// is there an asset bundle to be loaded with this ?
		if (assetBundleInfo != null){
			AssetBundleLoader.GetInstance().Load(assetBundleInfo);	
		}
		// add any Voicemaps or Vitals behaviors we are carrying
		
		if (voiceLists != null){
			foreach (VoiceList list in voiceLists){
				if (list.Name != "")
					VoiceMgr.GetInstance().AddVoiceList(list);
			}
		}
		
		if (vitalsBehaviors != null && vitalsBehaviors.Count > 0){
			foreach (VitalsBehavior vb in vitalsBehaviors){
				VitalsBehaviorManager.GetInstance().AddToLibrary(vb);
			}
		}
	
		// add in additional assessment lists
		
		if (assessmentListXMLs != null)
		{
			foreach(string xml in assessmentListXMLs)
			{
				AssessmentMgr.GetInstance().LoadAssessmentListXML(xml);
			}
		}
	}	
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	

}

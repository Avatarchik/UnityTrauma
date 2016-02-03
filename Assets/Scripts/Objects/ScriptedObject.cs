using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScriptedObject : MonoBehaviour
{
	public bool moveToParentOnDrop = false; // set this in each prefab, but false so constructed ones dont
	// this is a development version of an object which doesn't rely on XML for it's information,
	// rather everything is exposed as properties on the objects script.
	// the interaction menu is very simple
	
	// an array of InteractionScripts that this object can trigger
	public string dropTargetName = "";
	public InteractionScript[] scripts;
	public InteractionScript startupScript = null;
	public bool register = false; // instead, derive this from any contained scripts who listen to status messages
	public string prettyname; // this was on the OI class, we shouldnt need it here
    public string XMLName;
	public string XMLDirectory = "Assets/Resources/XML/";
	
//TODO Add to Info!	
	public AssetBundleInfo assetBundleInfo = null;
	public VoiceList voiceList = null;
	public List<VoiceList> voiceLists;
	public List<VitalsBehavior> vitalsBehaviors = null;
	public List<ScanRecord> scanRecords; // for overloading with case specific media

	ObjectInteraction myOI = null;
	public ObjectInteraction ObjectInteraction
	{
		get{ return myOI; }
	}
	public class QueuedScript
	{
		public QueuedScript()
		{
			TimeQueued = Time.time;
		}
		public float TimeQueued;
		public InteractionScript script;
		public string args;
		public GameObject obj;
		public int priority = 0; // have to keep this here so we can queue same script at different priorities
		public bool executing = false; // so we can remove the right instance from the queue
	}
	public Stack<QueuedScript> scriptStack = new Stack<QueuedScript>(); // actions can pend while they call subrouties
//	public Queue<QueuedScript> scriptQueue = new Queue<QueuedScript>(); // we queue the currently executing script as well..
	public ArrayList scriptArray = new ArrayList(); // <QueuedScript> for prioritizing.
	public QueuedScript currentScript = null;
	bool checkForScriptReady = false;
	public int executePriorityLock = -1; // don't start any scripts less than this priority
	bool prefabNeedsUpdate = false;
	public InteractionScript lastScriptExecuted;
	public float lastScriptExecutedTime;
	public bool PrefabNeedsUpdate(){ return prefabNeedsUpdate;}

	/* ----------------------------  SERIALIZATION ----------------------------------------- */
	public class ScriptedObjectInfo
	{
		public ScriptedObjectInfo(){
		}
		
		public string unityObjectName;
		
		public bool moveToParentOnDrop; // set this in each prefab, but false so constructed ones dont
		public string dropTargetName = "";
			
		public InteractionScript.InteractionScriptInfo[] scripts;
		public string startupScriptName = "";
		public bool register = false; // instead, derive this from any contained scripts who listen to status messages
		public string prettyname; // this was on the OI class, we shouldnt need it here
	    public string XMLName;
	    public string XMLDirectory;
			
		public AssetBundleInfo assetBundleInfo;
		public VoiceList voiceList;
		public List<VoiceList> voiceLists;
		public List<VitalsBehavior> vitalsBehaviors;	
		public List<ScanRecord> scanRecords; // for overloading with case specific media
	}
		
	// because the scripted objects thenselves are generally not created from serialized data when the game is run,
	// but rather instantiated from prefabs or placed in the level or asset bundle, these serialize routines
	// may not really matter.  The individual interaction scripts/actions, however, really do have to be right!!
	public ScriptedObjectInfo ToInfo(ScriptedObject so){ // saves values to an info for serialization (to XML)
		ScriptedObjectInfo info = new ScriptedObjectInfo();
		
		info.unityObjectName = so.name;

		info.moveToParentOnDrop = so.moveToParentOnDrop; // set this in each prefab, but false so constructed ones dont
		info.dropTargetName = so.dropTargetName;
		info.scripts = new InteractionScript.InteractionScriptInfo[so.scripts.Length];
		for (int i = 0; i<so.scripts.Length; i++){
			if (so.scripts[i] != null){
				info.scripts[i] = so.scripts[i].ToInfo(so.scripts[i]);
			}
			else 
			{
				Debug.Log("Scripted Boject "+so.name+"contains null script at index "+i);
#if UNITY_EDITOR
				EditorUtility.DisplayDialog("Save Failed","Scripted Boject "+so.name+"contains null script at index "+i,"OK");
#endif
			}			
		}
		if (so.startupScript != null)
			info.startupScriptName = so.startupScript.name;
		info.register = so.register; // instead, derive this from any contained scripts who listen to status messages
		info.prettyname = so.prettyname; // this was on the OI class, we shouldnt need it here
		info.XMLName = so.XMLName;
		info.XMLDirectory = so.XMLDirectory;
		
		info.assetBundleInfo = so.assetBundleInfo;
		info.voiceList = so.voiceList;
		info.voiceLists = so.voiceLists;
		info.vitalsBehaviors = so.vitalsBehaviors;
		info.scanRecords = so.scanRecords;
	
		return info;
	}
	
	public void InitFrom(ScriptedObjectInfo info){
		// we should probably destroy any existing hierarchy here, calling OnDestroy() on our children;
		
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName;
		
		moveToParentOnDrop = info.moveToParentOnDrop; // set this in each prefab, but false so constructed ones dont
		dropTargetName = info.dropTargetName;
		// make children from all the scriptInfos, and init each one from it's info, which will build the Actions
		scripts = new InteractionScript[info.scripts.Length];
		for (int i = 0; i<info.scripts.Length; i++){
			GameObject go = new GameObject(info.scripts[i].unityObjectName);
			go.transform.parent = this.transform;
			scripts[i] = go.AddComponent("InteractionScript") as InteractionScript;
			scripts[i].InitFrom(info.scripts[i]);	
		}
		if (info.startupScriptName != null && info.startupScriptName != ""){
			for (int i = 0; i<scripts.Length; i++){
				if (scripts[i].name == info.startupScriptName){
					startupScript = scripts[i];
					break;
				}
			}
		}
		
		register = info.register; // instead, derive this from any contained scripts who listen to status messages
		prettyname = info.prettyname; // this was on the OI class, we shouldnt need it here
		XMLName = info.XMLName;
		XMLDirectory = info.XMLDirectory;
		assetBundleInfo = info.assetBundleInfo;
		voiceList = info.voiceList;
		voiceLists = info.voiceLists;
		vitalsBehaviors = info.vitalsBehaviors;
		scanRecords = info.scanRecords;
	}
		
	public void LinkFrom(ScriptedObjectInfo info){
		// 	initialize members from deserialized info
		gameObject.name = info.unityObjectName;
		moveToParentOnDrop = info.moveToParentOnDrop; // set this in each prefab, but false so constructed ones dont
		dropTargetName = info.dropTargetName;

		// don't process the scripts in this method
		
		register = info.register; // instead, derive this from any contained scripts who listen to status messages
		prettyname = info.prettyname; // this was on the OI class, we shouldnt need it here
		XMLName = info.XMLName;
		XMLDirectory = info.XMLDirectory;
		assetBundleInfo = info.assetBundleInfo;
		voiceList = info.voiceList;
		voiceLists = info.voiceLists;
		vitalsBehaviors = info.vitalsBehaviors;
		scanRecords = info.scanRecords;
		
	}
	public void AppendFrom(ScriptedObjectInfo info){
		// make children from all the scriptInfos, and init each one from it's info, which will build the Actions
		int hadScripts = scripts.Length;
		// append my scripts to the parent's ScriptedObject
		InteractionScript[] tmp = new InteractionScript[hadScripts];
		for (int i = 0; i< hadScripts; i++)
			tmp[i] = scripts[i];
		
		// since you can't extend an array, copy the old one here, create a new one, and copy back 
		scripts = new InteractionScript[hadScripts + info.scripts.Length];
		for (int i = 0; i< hadScripts; i++)
			scripts[i] = tmp[i];
		for (int i = 0; i< info.scripts.Length; i++){
			GameObject go = new GameObject(info.scripts[i].unityObjectName);
			go.transform.parent = this.transform;
			scripts[i+hadScripts] = go.AddComponent("InteractionScript") as InteractionScript;
			scripts[i+hadScripts].InitFrom(info.scripts[i]);	
		}
		// see if there's a startup script to link
		if (info.startupScriptName != null && info.startupScriptName != ""){
			for (int i = 0; i<scripts.Length; i++){
				if (scripts[i].name == info.startupScriptName){
					startupScript = scripts[i];
					break;
				}
			}
		}
	}	
	
	public void SaveToXML(string pathname){
		XMLName = pathname;
		ScriptedObjectInfo info = ToInfo (this);
		XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObjectInfo));
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
		XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObjectInfo));
		FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
		ScriptedObjectInfo info = serializer.Deserialize(stream) as ScriptedObjectInfo;
		stream.Close();
		// need to clear out the old SO first!
		foreach (InteractionScript script in scripts){
			DestroyImmediate(script.gameObject);
		}
		InitFrom(info);			
	}
	
	public void LoadLinkablesFromXML(string pathname){ // everything except the scripts
		if (pathname == null || pathname == "") return;
		ScriptedObjectInfo info=null;
/*		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObjectInfo));
			FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
			info = serializer.Deserialize(stream) as ScriptedObjectInfo;
			stream.Close();
		}
		else
*/
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<ScriptedObjectInfo> serializer = new Serializer<ScriptedObjectInfo>();
			pathname = "XML/"+pathname.Replace (".xml","");
			info = serializer.Load(pathname);
			if ( info == null )
			{
				UnityEngine.Debug.LogError("LoadFromXML(" + pathname + ") : error serializing!");
				return;
			}
		}
		LinkFrom(info);			
	}
	
	public void AppendFromXML(string pathname){
		ScriptedObjectInfo info=null;
		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(ScriptedObjectInfo));
			FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
			info = serializer.Deserialize(stream) as ScriptedObjectInfo;
			stream.Close();
		}
		else
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<ScriptedObjectInfo> serializer = new Serializer<ScriptedObjectInfo>();
			pathname = "XML/"+pathname.Replace (".xml","");
			info = serializer.Load(pathname);
		}
		AppendFrom(info);			
	}
	/* ----------------------------  SERIALIZATION ----------------------------------------- */		
	
	
    public void Awake()
    {
		// this is a kind of a hack, the base trauma prefab exists just to cause the scripts to get loaded
		// for each of its scripted objects, which have no scripts for size reasons, but they can be out of sync
		// with any linkable content added in the editing versions of those scripted objects, so this syncs them.
		// this keeps the placeholder prefabs in the BasePrefab matching the data saved to the xml.

		LoadLinkablesFromXML(XMLName);	// this might only work in the editor ?
			
		// is there an asset bundle to be loaded with this ?
		if (assetBundleInfo != null){
			AssetBundleLoader.GetInstance().Load(assetBundleInfo);	
		}
		// add any Voicemaps or Vitals behaviors we are carrying
		
		if (voiceList != null && voiceList.Name != ""){
			VoiceMgr.GetInstance().AddVoiceList(voiceList);
		}
		
		if (voiceLists != null){
			foreach (VoiceList vl in voiceLists){
				VoiceMgr.GetInstance().AddVoiceList(vl);
			}
		}
		
		if (vitalsBehaviors != null && vitalsBehaviors.Count > 0){
			foreach (VitalsBehavior vb in vitalsBehaviors){
				VitalsBehaviorManager.GetInstance().AddToLibrary(vb);
			}
		}

		// is this late enough to cause override of any content in the patient records ?
		if (scanRecords != null && scanRecords.Count > 0){
			Patient patient = FindObjectOfType<Patient>();
			if (patient != null){
				foreach (ScanRecord record in scanRecords){
					patient.LoadScanRecord(record);
				}
			}
		}
		
		// handle reparent to drop target...
		// later, we want to do this in the editor instead of here, but this is a start.
		if (moveToParentOnDrop){
			
			Link ();

			// disable me.
			this.enabled = false;
			
			// disable or remove all my scripts
			for (int i=0; i<scripts.Length;i++){
				DestroyImmediate(scripts[i].gameObject);	
			}
			
			return; // dont do anything else.
		}
//        base.Awake();
    }
	
	public void Link(){
		// connect the payload scripts to the character(s) with the designated role
			string pathname = XMLName;
//			if (Application.isEditor)
//				pathname = XMLDirectory+pathname;
			
			Dispatcher.GetInstance().AssignScripts (pathname);
		
			
		
/* Old way of doing it, not duplicated in the dispatcher, so preserved here for reference.
			// if the parent already has no scripted Object, add the component
			GameObject pop=null;
			if (transform.parent != null) pop = transform.parent.gameObject;
			
			string[] targetNames;
			if (dropTargetName != ""){
				targetNames = dropTargetName.Split (',');	// comma delimited list of targets
			}
			else
			{
				if (pop == null) return; // no parent or drop target name, gotta bail
				targetNames = new string[1];
				targetNames[0] = pop.name;
			}
			
			foreach (string target in targetNames){
				if (target != "") // target name overrides hierarchical parent
					pop = GameObject.Find(target);
				
				// generate a new scripted Object from our XML file, and reparent all of it's scripts
				// and settings to the drop target
				
				if (pop==null){
					Debug.LogWarning(name+" couldnt find drop target "+target);
					continue; // couldnt find that parent
				}
			
				ScriptedObject newSO = pop.GetComponent<ScriptedObject>();
				
				if (newSO == null){
					newSO = pop.AddComponent<ScriptedObject>() as ScriptedObject; // could we use Clone here ?
					newSO.scripts = new InteractionScript[0];
					newSO.prettyname = prettyname;
				}
				
				newSO.AppendFromXML(XMLName);
	
				newSO.moveToParentOnDrop = false;
				
				// there might be other fields we want to copy here...
	//			newSO.ActivateDistance = ActivateDistance;
				newSO.register |= register;
	//			newSO.iconTexture = iconTexture;
	
	//			newSO.prettyname = prettyname;
				
				newSO.Awake();
			}
*/		
		
	}

    public void Start()
    {
//        base.Start();
     //   base.LoadXML(XMLName);

		
		myOI = GetComponent<ObjectInteraction>();
		if (myOI == null){
			myOI = gameObject.AddComponent<ObjectInteraction>();
			if (prettyname == null || prettyname == "") prettyname = name;
			myOI.prettyname = prettyname;
			myOI.ActivateDistance = 10;
		}
		
					// register this object
		if (register)
        	ObjectManager.GetInstance().RegisterObject(myOI); // this might result in the duplicate messages ?
		
		StartCoroutine(AddItems()); // see if delaying this has any effect
    }
	
	public void Update(){
		// control execution of scripts to single thread, one current script at a time.	
		if (scriptStack.Count > 0){
			if ( scriptStack.Peek ().script.SanityCheck() == true )
				scriptStack.Peek().script.UpdateScript();
			else
			{
				// bail for whatever reason
				UnityEngine.Debug.LogError ("ScriptedObject.Update() " + myOI.name + " : script<" + scriptStack.Peek ().script.name + "> failed sanity check, aborting!");
				OnScriptComplete(scriptStack.Peek().script,null);
			}
			myOI.IdleTime = 0;
		}
		else
		{
			if (myOI.actingInScript == null && myOI.reservedForScript == null)
				myOI.IdleTime += Time.deltaTime;
			else
				myOI.IdleTime = 0;
		}
		
		if (checkForScriptReady){ // could put a timer on this for performance
			if (scriptArray.Count <1){
				Debug.Log("ScriptQueue was empty, but check set for "+name);
				checkForScriptReady = false;
			}
			QueuedScript qs = FindBestScript();
			if (qs != null){
				qs.executing = true;
				ExecuteScript(qs.script,qs.args,qs.obj);
				checkForScriptReady = false;
			}
		}
		QueueAutoExecutes();
	}
	
	IEnumerator AddItems(){
		yield return new WaitForSeconds(2); // this delay was to be sure the xml got parsed first
		
		// Add a simple menu based on our data
		if (scripts != null){
	    	foreach (InteractionScript s in scripts){
				InteractionMap map = new InteractionMap( s.item,  s.prettyname,  s.response_title,  s.note,  s.tooltip,  s.sound,  s.task,  s.log); // using prettyname for response PAA 7/3/15
				// copy prereqs, categories, param
				if (s.prereq != null){
					map.prereq = new System.Collections.Generic.List<string>();
					for (int i = 0; i<s.prereq.Count; i++)
						map.prereq.Add (s.prereq[i]);
				}
				if (s.category != null){
					map.category = new System.Collections.Generic.List<string>();
					for (int i = 0; i<s.category.Count; i++)
						map.category.Add (s.category[i]);
				}
				if (s.param != null){
					map.param = new System.Collections.Generic.List<string>();
					for (int i = 0; i<s.param.Count; i++)
						map.param.Add (s.param[i]);
				}
				if (s.AddToMenu ) //&& s.isReadyFor(GetComponent<BaseObject>())
			    	myOI.AddItem(map);
				InteractionMgr.GetInstance().Add(map);
				if (s.prettyname != null && s.prettyname != "")
				{  // update the string map from the script menu
					StringMgr.GetInstance ().Load(s.item,s.prettyname);
				}
				else
				{  // see if you can get a nice menu name from the string map, or show just the ITEM:
					s.prettyname = StringMgr.GetInstance(). Get (s.item);
				}
			}
		}
		
		if (startupScript != null)
			QueueScript(startupScript,"trigger=startup",gameObject,4);
		
	}
	
    public void PutMessage( GameMsg msg ) 
    {
		InteractMsg imsg = msg as InteractMsg;
		
		if (imsg != null) // handle special messages to abort or flush script queue
		{
			if (imsg.map != null){
				if (imsg.map.item == "SCRIPT:ABORT"){
					if (scriptStack.Count > 0){
						scriptStack.Peek().script.Abort();
					}
					return;
				}
				if (imsg.map.item == "SCRIPT:QUEUE:FLUSH"){
					if (scriptStack.Count > 0){
						scriptStack.Peek().script.Abort();
					}
					//The OnComplete from this abort is likely to start up any stacked or queued scripts.
					// this really isn't very clean, as there might be stacked scripts which will not get a clean
					// shutdown opportunity, we should probably abort at each level of the stack before clearing all.
					
					// for now, just clear everything after aborting the current script. That should get Zoll going.
					scriptStack.Clear();
					scriptArray.Clear ();
					currentScript = null;
					if ( myOI != null )
						myOI.actingInScript = null;
					return;
				}
			}
			
//UnityEngine.Debug.Log("Scripted Object "+name+" received interact message "+imsg.map.item);
			// see if this message should trigger any of our scripts...
			InteractionScript s = TriggeredScript(imsg.map.item,myOI as BaseObject);
			if (s != null){
				string args = "trigger="+imsg.map.item;
				int priority=s.startPriority;
				if (imsg.map.param != null){
					for (int i=0;i<imsg.map.param.Count; i++){
						if (imsg.map.param[i].Contains("priority")){
							string[] kv = imsg.map.param[i].Split('=');
							int.TryParse(kv[1],out priority);									
						}
						else
						{
							args += " "+imsg.map.param[i];	
						}
					}
				}	
				QueueScript( s,args, gameObject,priority);  	
			}
			return;
		}
		
		InteractStatusMsg ismsg = msg as InteractStatusMsg;
		if (ismsg != null)
		{
			// see if this message should trigger any of our scripts...
			InteractionScript s = TriggeredScript(ismsg.InteractName,null);
			if (s != null){
				string args = "trigger="+ismsg.InteractName;
				int priority=4;
				if (ismsg.Params != null){ 
					foreach (string p in ismsg.Params){
						if (p.Contains("priority")){
							string[] kv = p.Split('=');
							int.TryParse(kv[1],out priority);									
						}
						else
						{
							args += " "+p;	
						}	
					}
				}
				QueueScript( s,args, gameObject,priority); 	
			}
//UnityEngine.Debug.Log("Scripted Object "+name+" received interact status message "+ismsg.InteractName);
		}
    }
	
	public InteractionScript TriggeredScript(string key,BaseObject obj){
		// return the lowest cost script which would trigger for this string/object pair
		// null BaseObject will check triggerOnStatus scripts
		InteractionScript lowestCostScript = null;
		float lowestCost = 99999999999f;
		float cost = 0;
//		InteractionScript found = null;
		
		foreach (InteractionScript s in scripts){
			
			if (obj != null && s.isReadyToQueue(obj)){ // we pretty much always want to queue these.
				foreach (string t in s.triggerStrings){
					if (t.Contains ("*")){ // wild card match
						string template =t.Replace("*","");
						if (key.Contains(template)){
							cost = s.GetCost(obj as ObjectInteraction);
							// moved these into GetCost so they affect dispatching too.
//							if (!s.isReadyFor(obj as ObjectInteraction)) cost+= 10;
//							if (!s.isReadyToRun(obj as ObjectInteraction,false)) cost+= 10;
							if (cost < lowestCost){
								lowestCostScript = s;
								lowestCost = s.GetCost(obj as ObjectInteraction);
							}
						}
					}
					else if (key.ToLower() == t.ToLower()){
						cost = s.GetCost(obj as ObjectInteraction);
						//if (!s.isReadyFor(obj as ObjectInteraction)) cost+= 10;
						//if (!s.isReadyToRun(obj as ObjectInteraction,false)) cost+= 10;
						if (cost < lowestCost){
							lowestCostScript = s;
							lowestCost = s.GetCost(obj as ObjectInteraction);
						}
					}
				}
			}
			if ( obj == null && s.triggerOnStatus){
				foreach (string t in s.triggerStrings){
					if (t.Contains ("*")){ // wild card match
						string template =t.Replace("*","");
						if (key.Contains(template)){
							cost = s.GetCost(obj as ObjectInteraction);
							//if (!s.isReadyFor(obj as ObjectInteraction)) cost+= 10;
							//if (!s.isReadyToRun(obj as ObjectInteraction,false)) cost+= 10;
							if (cost < lowestCost){
								lowestCostScript = s;
								lowestCost = s.GetCost(obj as ObjectInteraction);
							}
						}
					}
					else if (key.ToLower() == t.ToLower()){
						cost = s.GetCost(obj as ObjectInteraction);
						//if (!s.isReadyFor(obj as ObjectInteraction)) cost+= 10;
						//if (!s.isReadyToRun(obj as ObjectInteraction,false)) cost+= 10;
						if (cost < lowestCost){
							lowestCostScript = s;
							lowestCost = s.GetCost(obj as ObjectInteraction);
						}
					}
				}
			}
		}
		return lowestCostScript;
	}
	
	public void QueueScript(InteractionScript script, string args, GameObject obj, int priority=0){
		
		if (script == null){
			Debug.LogError("Tried to queue null script - ignored");
			return;
		}		
		QueuedScript qs = new QueuedScript();
		qs.script = script;
		qs.args = args;
		qs.obj = obj;
		qs.priority = priority;
//Q		scriptQueue.Enqueue(qs);
		scriptArray.Add (qs);
		qs.script.queueCount++;
//Debug.Log (name+" queued "+qs.script.name+" after queuing, count is "+scriptArray.Count);
//		if (scriptArray.Count == 1){
		if (myOI.actingInScript == null && scriptArray.Count == 1){
			if (script.isReadyToRun(obj.GetComponent<BaseObject>(),true)){
				qs.executing=true;
				ExecuteScript(script,args,obj,priority);
			}
			else
			{
				// verbalize an acknowledgement that the order has been recieved 

				checkForScriptReady=true;
			}
		}
		else
		{
			// verbalize an acknowledgement that the order has been recieved 
			
			
			// if the currently running script is cancellable, and its priority is lower than ours,
			// send it a cancel
			if (scriptStack.Count > 0){
				QueuedScript qScr = scriptStack.Peek();
				if (qScr != null && 
					qScr.script.cancellable &&
					qScr.priority < priority){
					qScr.script.Cancel();
				}
			}
		}
	}
	
	public void ExecuteScript( InteractionScript script, string args, GameObject obj, int priority=0){ // this can be called from a scriptedInteraction
		if (script == null){
			Debug.LogError("Tried to execute null script - ignored");
			return;
		}
		QueuedScript qs = new QueuedScript();
		qs.script = script;
		qs.args = args;
		qs.obj = obj;
		qs.priority = priority;
		scriptStack.Push(qs);
		currentScript = qs;

		if (scriptStack.Count > 1)
			Debug.Log (name+" stacked "+qs.script.name+" after stacking, count is "+scriptStack.Count);
		
		script.Execute(this,args,obj); // we always start a script when this is called
	}

	public void AbortAllScripts(){

		if (myOI as TaskCharacter != null) { // only do this for task characters
			myOI.actingInScript = null;
			myOI.reservedForScript = null;
			if ((myOI as TaskCharacter).executingScript != null)
				(myOI as TaskCharacter).executingScript.Cancel();
			(myOI as TaskCharacter).executingScript = null;
		}
		// FlushScriptQueue ();
		scriptArray.Clear ();
		while (scriptStack.Count > 0)
			scriptStack.Pop ();
		if (currentScript != null) // there is an abort method on the script but it doesnt do very much...
			currentScript.script.OnScriptComplete ("abort");
		currentScript = null;

	}
	
	public void FlushScriptQueue(){
		for(int i=scriptArray.Count-1; i>=0;i--){
			object obj = scriptArray[i];
			if (!(obj as QueuedScript).executing){
				(obj as QueuedScript).script.queueCount--;
				scriptArray.Remove(obj);
			}
		}	
	}
	
	
	public void OnScriptComplete(InteractionScript script,string error){
		lastScriptExecuted = script;
		lastScriptExecutedTime = Time.time;
//Debug.Log (name+" before dequeuing, count is "+scriptQueue.Count);
		
		// error might tell us this was aborted, how should we handle it ? abort the whole stack ? possibly.
		
//		QueuedScript completedScript = 

		if (scriptStack.Count > 0) scriptStack.Pop();
		// if there is anything still on the stack, updates will start giong to it,
		if (scriptStack.Count > 0) return;
		// if the stack is empty, see if there is a queued script waiting to be executed...
//Q		QueuedScript dequeuedScript = scriptQueue.Dequeue();
		
		foreach (object obj in scriptArray){
			if ((obj as QueuedScript).executing){
				currentScript = null;
				script.queueCount--;
				scriptArray.Remove(obj);
				break;
			}
		}
		
		
//		scriptArray.Remove(currentScript); // we could remove this when we start it running instead of here...
		
		if (scriptArray.Count > 0){
			// here is where we get intelligent about what to do next...for all the queued scripts, find the highest priority one which is ready to run right now.
			
			
			// if there isn't one, schedule update to keep checking until there is.
			
			
			//QueuedScript qs = scriptQueue.Peek();
			QueuedScript qs = FindBestScript(); //scriptArray[0] as QueuedScript; // pick the highest priority ready to run script in the queue...
			if (qs != null){
				qs.executing = true;
				ExecuteScript(qs.script,qs.args,qs.obj);
			}
			else {
				checkForScriptReady=true;
			}
		}
	}


	public void ReleasedBy(InteractionScript script){
		// if we were running in someone elses script, we might need restart our own script queue

		// may need some conditional checking here.
		if (scriptArray.Count > 0){ // and nothing stacked ?
			checkForScriptReady = true;
		}
	}
	
	// If a queued script is ready, and the highest priority thing, but the other characters are not ready,
	// then inform them that they are being waited for...  then checking if they are free also means checking it
	// the checker is the script they are reserved for.  watch out for lockups here...race conditions can easily occur.
	
	
	QueuedScript FindBestScript(){
		if (scriptArray.Count <1){
			Debug.Log("looking for script in empty list!");
			return null;
		}
		if (myOI.actingInScript != null || ( myOI as TaskCharacter != null && (myOI as TaskCharacter).currentTask != null)) return null; // wait till roles are done
		int highestPriority = executePriorityLock;
		QueuedScript bestScript = null;
		foreach (QueuedScript qs in scriptArray){
			if (qs.script.readyState != InteractionScript.readiness.executing &&
				qs.script.isReadyToRun(qs.obj.GetComponent<BaseObject>(),false) && 
				qs.priority > highestPriority){
				bestScript = qs;
				highestPriority = qs.priority;
			}
		}
		if (bestScript != null) // reserve the actors for the highest priority script
			bestScript.script.isReadyToRun(bestScript.obj.GetComponent<BaseObject>(),true);
		return bestScript;
	}
	
	void QueueAutoExecutes(){
		// see if any scripts that want to auto execute should be queued
		if (myOI.actingInScript != null || myOI.reservedForScript != null) return;
		// should really check the above for all roles...
		foreach (InteractionScript script in scripts){
			if (script.autoExecuteProbability == 0) continue;
			script.autoExecuteTimer +=Time.deltaTime;
			if (script.queueCount == 0 &&
			    script.startPriority > executePriorityLock &&
				script.readyState != InteractionScript.readiness.executing &&
				script.autoExecuteTimer>= Mathf.Abs(script.autoExecuteInterval) &&
				script.isReadyToRun(GetComponent<BaseObject>(),false)){
					script.autoExecuteTimer = 0;
					
					// now let's roll the die and see if we should run this puppy...
					if (UnityEngine.Random.Range(0,100) <= script.autoExecuteProbability*100.0f){
						if (script.autoExecuteInterval <= 0) script.autoExecuteProbability = 0;{// only run once if interval<=0
							QueueScript (script,"",gameObject,script.startPriority);
						}
					}
				// since we reset the timer, 
			}
		}
	}
	
	// list interactions available to build menus
	
	public List<InteractionMap> AllInteractions(){
		// return a list of interaction scripts whose prerequisites are met for the owning entity
		BaseObject bob = gameObject.GetComponent<BaseObject>();
		if (bob == null) return null;
		List<InteractionMap> iList = new List<InteractionMap>();
		foreach (InteractionScript s in scripts){
			if (s != null)
				iList.Add (s.ToMap (bob.name));
			else
				Debug.LogWarning(name+" found null script in list.");
		}
		return iList;
	}	
	
	public List<InteractionMap> QualifiedInteractions(){
		// return a list of interaction scripts whose prerequisites are met for the owning entity
		BaseObject bob = gameObject.GetComponent<BaseObject>();
		if (bob == null) return null;
		List<InteractionMap> iList = new List<InteractionMap>();
		foreach (InteractionScript s in scripts){
			// put everything on the menu that isn't unavailable.  queued, executing, whatever.
			if (s.AddToMenu && s.ReadyState(bob,false) != InteractionScript.readiness.unavailable){
				iList.Add (s.ToMap (bob.name)); // this will pass along the determined readiness to the menu
			}
		}
		return iList;
	}
	
	public List<InteractionMap> QualifiedInteractionsFor(GameObject GO){
		// return a list of interaction scripts whose prerequisites are met for the owning entity
		BaseObject bob = GO.GetComponent<BaseObject>();
		if (bob == null) return null;
		List<InteractionMap> iList = new List<InteractionMap>();
		foreach (InteractionScript s in scripts){
			if (s.ReadyState(bob,false) != InteractionScript.readiness.unavailable){
				iList.Add (s.ToMap (bob.name)); // this will pass along the determined readiness to the menu
			}
		}
		return iList;
	}
	
	// some utilities to help editor tools manage the scripts list
	public int IndexOf(InteractionScript script){
		int index=-1;
		for (int i=0;i<scripts.Length;i++){
			if (scripts[i] == script){
				index = i;
				break;
			}
		}
		return index;
	}
	
	public void InsertScriptSorted(InteractionScript script){
		int desiredIndex = script.menuOrder;
		//Find the slot where this menuOrder should be placed and insert there
		InsertScriptAt( script, desiredIndex);
	}
	
	public void InsertScriptAt(InteractionScript script, int desiredIndex){
		// if index greater than length, append
		if (desiredIndex > scripts.Length) desiredIndex = scripts.Length;

		InteractionScript[] newScripts = new InteractionScript[scripts.Length+1];
		for (int i=0; i<desiredIndex; i++){
			newScripts[i] = scripts[i];	
		}
		newScripts[desiredIndex] = script;
		for (int i=desiredIndex; i<scripts.Length; i++){
			newScripts[i+1] = scripts[i];	
		}
		scripts = newScripts;
		script.transform.parent = this.transform;
	}
	
	public void RemoveScript(InteractionScript script){
		// remove from array if found, but don't destroy or un-parent the script	
		int index= IndexOf(script);
		if (index < 0)
			return;	

		InteractionScript[] newScripts = new InteractionScript[scripts.Length-1];
		for (int i=0; i<index; i++){
			newScripts[i] = scripts[i];	
		}
		for (int i=index+1; i<scripts.Length; i++){
			newScripts[i-1] = scripts[i];	
		}
		scripts = newScripts;
	}

	public QueuedScript GetCurrentScript()
	{
		if ( scriptStack.Count <= 0 )
			return null;
		return scriptStack.Peek ();
	}
	
}

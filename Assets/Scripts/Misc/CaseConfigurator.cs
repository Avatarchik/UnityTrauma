#define SHOW_TEAM_SELECT
#define SHOW_DB_CASES
#define SHOW_VITALS_SELECT
#define LOAD_MENU

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/* =================================== How to =======================================
 * To make something in the game configurable, make a watchdog script to perform the task,
 * and place it in a prefab in the Resources/CaseConfiguration folder. 
 * Name the prefab in one of the Data.Options array in the CaseConfiguration object.
 * If the task is complex, it should be possible to include new equipment and scripts for characters to use with
 * the equipment as part of the prefab.  Any new animations needed should be incorporated in the prefab as well,
 * but the method for doing that is not yet defined at the time this comment was written.
 * 
 * the saved XML version of the case configuration data will be loaded when the case begins, and those selected options
 * will have their prefabs loaded.  If more complex startup is required, a new component class can be created and
 * placed on the top level object of the prefab, then called after instantiation to perform the complex startup functions.
 */

public class CaseConfigurator : MonoBehaviour 
{	
	public bool ShowGUI = false;
	public bool loadAndRun = false; // triggered by the start button
	bool loaded = false;

	static CaseConfigurator instance;
	public static CaseConfigurator GetInstance()
	{
		return instance;
	}

	void Awake()
	{
		instance = this;
	}

	// Use this for initialization
	void Start () {

		// reset clock because the TraumaBrain EndScenario and Assessment force the clock off
		Time.timeScale = 1; 

#if LOAD_MENU
		// do a CoRoutine here because we want to pause for a moment
		StartCoroutine( LoadMainMenu() );
#endif
	}
	
	IEnumerator LoadMainMenu()
	{		
		UnityEngine.Debug.Log("CaseConigurator.LoadMainMenu() : CaseValid=" + CaseConfiguratorMgr.GetInstance().CaseValid);

		yield return new WaitForSeconds(0.25f);
		
		// if we're coming from the traumaMainMenu scene this case
		// will always be valid. the case is only invalid if starting
		// from the Trauma5 scene.  In that case we want to start
		// with the menus.  The player would never start this way.
		if ( CaseConfiguratorMgr.GetInstance().CaseValid == true )
		{
			// if the case is valid, go ahead and start it
			CaseConfiguratorMgr.GetInstance().StartCase();
		}
		else
		{
			// not ready to start case, pause the game
			Time.timeScale = 0; 
			// case not valid, go to menus
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoginScreen";
			msg.className = "TraumaLogin";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		
		yield return 0;
	}

	// Update is called once per frame
	void Update () 
	{
#if !LOAD_MENU
		LoadAndRun();	
#endif
	}
	
	// create seperate method so we can call this outside
	public IEnumerator LoadConfig( CaseOptionData data )
	{
		if ( data == null )
		{
			UnityEngine.Debug.LogError("CaseConfigurator.LoadConfig() : data is NULL (probably bad load)...");
			yield return null;
		}

		UnityEngine.Debug.Log("CaseConfigurator.LoadConfig(" + data.casename + ")");
		
		// set assessment name
		AssessmentMgr.GetInstance().Scenario.Name = data.casename;
		
		// first, load up the base configuration
		
		if (data.baseConfig != null){
			if (data.baseConfig.Enabled){ 
				string prefabName = "CaseOptions/"+data.baseConfig.Prefab;
				UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() load prefab <" + prefabName + ">");
				UnityEngine.Object ro = Resources.Load(prefabName);
				if (ro != null){
					GameObject newGO = Instantiate(ro) as GameObject;
					if (newGO != null){
						newGO.name = data.baseConfig.Name;
						newGO.transform.parent = gameObject.transform;
				
						// eventually, we may need to call Link() here.
						// yield a few frames for all of that to get linked
						yield return null;
						yield return null;
						yield return null;
					}
				}
				else
					Debug.LogWarning("Could not load base Configuration "+prefabName);
			}			
		}
		else
			UnityEngine.Debug.LogWarning("No base configuration found in Config "+ data.casename);
		
		// load injuries here
		if ( data.injury != null )
		{
			// load primary injury prefab first
			if ( data.injury.Prefab != null && data.injury.Prefab != "" )
			{
				string prefabName = "CaseOptions/"+data.injury.Prefab;
				UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() load prefab <" + prefabName + ">");
				UnityEngine.Object ro = Resources.Load(prefabName);
				if (ro != null){
					GameObject newGO = Instantiate(ro) as GameObject;
					if (newGO != null){
						newGO.name = data.injury.Name;
						newGO.transform.parent = gameObject.transform;
				
						// eventually, we may need to call Link() here.
						// yield a few frames for all of that to get linked
						yield return null;
						yield return null;
						yield return null;
					}
				}
				else
					UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() : can't load prefab <" + prefabName + ">");
			}
			// now load any secondary injuries
			foreach( CaseConfigOption secondary in data.injury.SecondaryOptions )
			{
				string prefabName;
				// get name of prefab
				if ( secondary.Enabled == true )
					prefabName = secondary.Prefab;
				else
					prefabName = secondary.PrefabWhenDisabled;				
					
				if ( prefabName != null && prefabName != "" )
				{
					prefabName = "CaseOptions/"+prefabName;				
					UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() load prefab <" + prefabName + ">");
					UnityEngine.Object ro = Resources.Load(prefabName);
					if (ro != null){
						GameObject newGO = Instantiate(ro) as GameObject;
						if (newGO != null){
							newGO.name = secondary.Name;
							newGO.transform.parent = gameObject.transform;
					
							// eventually, we may need to call Link() here.
							// yield a few frames for all of that to get linked
							yield return null;
							yield return null;
							yield return null;
						}
					}
					else
						UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() : can't load prefab <" + prefabName + ">");
				}
			}
		}		
		
		// look for each configured option as our child, instantiate from resources prefabs or asset bundles if not found,
		// make them our children, then call the necessary startup methods.
		if (data.options != null)
		{
			foreach (CaseConfigOption opt in data.options)
			{
				string prefabName1;
				// get name of prefab
				if ( opt.Enabled == true )
					prefabName1 = opt.Prefab;
				else
					prefabName1 = opt.PrefabWhenDisabled;				
					
				if ( prefabName1 != null && prefabName1 != "" )
				{
					prefabName1 = "CaseOptions/"+prefabName1;				
					UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() load prefab <" + prefabName1 + ">");
					UnityEngine.Object ro = Resources.Load(prefabName1);
					if (ro != null){
						GameObject newGO = Instantiate(ro) as GameObject;
						if (newGO != null){
							newGO.name = opt.Name;
							newGO.transform.parent = gameObject.transform;
					
							// eventually, we may need to call Link() here.
							// yield a few frames for all of that to get linked
							yield return null;
							yield return null;
							yield return null;
						}
					}
					else
						UnityEngine.Debug.Log("CaseConfigurator.LoadConfig() : can't load prefab <" + prefabName1 + ">");
				}
			}
		}
		
		
		// grab/set the initial vitals state
		Patient patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;
		int HR = Convert.ToInt32(data.start.HR);
		if ( HR != -1 )
			patient.HR = HR;
		int BPSYS = Convert.ToInt32(data.start.BPSYS);
		if ( BPSYS != -1 )
			patient.BP_SYS = BPSYS;
		int BPDIA = Convert.ToInt32(data.start.BPDIA);
		if ( BPDIA != -1 )
			patient.BP_DIA = BPDIA;
		int SP = Convert.ToInt32(data.start.SP);
		if ( SP != -1 )
			patient.SP = SP;
		float TEMP = Convert.ToSingle(data.start.TEMP);
		if ( TEMP != -1 )
			patient.TEMP = TEMP;
		float RESP = Convert.ToSingle(data.start.RESP);
		if ( RESP != -1 )
			patient.RR = RESP;
	}
	
	public void LoadAndRun()
	{
		if ( CaseConfiguratorMgr.GetInstance().CaseValid == true )
		{
			// save data
			data = CaseConfiguratorMgr.GetInstance().Data;
			if ( data == null )
			{
				UnityEngine.Debug.LogError ("CaseConfigurator.LoadAndRun() : Data=null");
				return;
			}
			// load the case special stuff
			StartCoroutine(LoadConfig(data));		
		}
		Time.timeScale = 1.0f; // un pause the action // should we wait for loading to complete for this ?
	}
	
	public Rect guiRect = new Rect(100,100,800,450);
	public Texture2D backgroundTex = null;
	Vector2 scroller = Vector2.zero;
	Vector2 dbScrollPosition;
	Vector2 teamScrollPosition;
	Vector2 caseScrollPosition;
	Vector2 outerScrollPosition;
	
	public CaseOptionData Data;
	public CaseOptionData data
	{
		set 
		{ 
			CaseConfiguratorMgr.GetInstance().Data = value; 
			Data = value;
		}
		get 
		{ 
			Data = CaseConfiguratorMgr.GetInstance().Data;
			return Data; 
		}
	}
	
	void OnGUI(){

#if LOAD_MENU
		return;
#endif
		
		if (loadAndRun || ShowGUI == false) return; // we are starting up the configured options already.		

		Color oc = GUI.color;
		Color bc = GUI.backgroundColor;
//		GUI.color = new Color(1,1,1,1);
		GUI.backgroundColor = new Color(0.25f,0.4f,0.5f,1);		
		
		GUI.DrawTexture(new Rect(guiRect.x,guiRect.y,guiRect.width,guiRect.height),backgroundTex);
		guiRect = GUI.Window (0, guiRect, DrawConfigurator, "CASE CONFIGURATION: "+data.description);
		
		GUI.color = oc;
		GUI.backgroundColor = bc;
	}
	
	void DrawConfigurator(int id){
		GUI.depth=0;
		GUI.DrawTexture(new Rect(0,20,guiRect.width,guiRect.height-20),backgroundTex);
		GUI.DragWindow (new Rect(0,0,guiRect.width,20));
		GUILayout.BeginVertical();
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Name",GUILayout.Width(35));
		data.name = GUILayout.TextField(data.name,GUILayout.Width(300));
		GUILayout.Label("Age",GUILayout.Width(25));
		data.age = GUILayout.TextField(data.age,GUILayout.Width(40));
		GUILayout.EndHorizontal();
		caseScrollPosition = GUILayout.BeginScrollView(caseScrollPosition,GUILayout.Height(60));
		data.caseDescription = GUILayout.TextArea(data.caseDescription,GUILayout.Height(120));
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		scroller = GUILayout.BeginScrollView(scroller);
		if (data != null && data.options != null && data.options.Length > 0){
			GUILayout.BeginArea (new Rect(0,0,guiRect.width*.7f,guiRect.height));
			GUILayout.Label("CASE OPTIONS");
			foreach (CaseConfigOption opt in data.options){
				GUILayout.BeginHorizontal();
				opt.Enabled = GUILayout.Toggle(opt.Enabled,opt.shortDescription);
				GUILayout.EndHorizontal();
				if (opt.param.Length > 0){
					foreach (string str in opt.param){
						GUILayout.BeginHorizontal();
						//GUILayout.FlexibleSpace();
						GUILayout.Label("\t\t"+str);
						GUILayout.EndHorizontal();
					}
				}
			}
			GUILayout.EndArea();
			
#if SHOW_TEAM_SELECT			
			GUILayout.BeginArea (new Rect(guiRect.width*.6f,0,guiRect.width*.3f,guiRect.height*0.28f));
			GUILayout.Label("TEAM MEMBERS (planned feature)");
			teamScrollPosition = GUILayout.BeginScrollView(teamScrollPosition,false,true);
			foreach (CaseConfigOption opt in data.team){
				GUILayout.BeginHorizontal();
				GUILayout.Toggle(opt.Enabled,opt.shortDescription);
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
#endif
#if SHOW_VITALS_SELECT
			GUILayout.BeginArea (new Rect(guiRect.width*.6f,guiRect.height*0.30f,guiRect.width*.3f,guiRect.height*0.28f));
			GUILayout.Label("VITALS (START/END)");
			GUILayout.BeginHorizontal();
			GUILayout.Label("HR");
			float w=30;
			data.start.HR = GUILayout.TextArea(data.start.HR,GUILayout.Width(w));
			GUILayout.Label("SYS");
			data.start.BPSYS = GUILayout.TextArea(data.start.BPSYS,GUILayout.Width(w));
			GUILayout.Label("DIA");
			data.start.BPDIA = GUILayout.TextArea(data.start.BPDIA,GUILayout.Width(w));
			GUILayout.Label("SP");
			data.start.SP = GUILayout.TextArea(data.start.SP,GUILayout.Width(w));
			GUILayout.Label("TEMP");
			data.start.TEMP = GUILayout.TextArea(data.start.TEMP,GUILayout.Width(w));
			GUILayout.Label("RESP");
			data.start.RESP = GUILayout.TextArea(data.start.RESP,GUILayout.Width(w));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("HR");
			data.end.HR = GUILayout.TextArea(data.end.HR,GUILayout.Width(w));
			GUILayout.Label("SYS");
			data.end.BPSYS = GUILayout.TextArea(data.end.BPSYS,GUILayout.Width(w));
			GUILayout.Label("DIA");
			data.end.BPDIA = GUILayout.TextArea(data.end.BPDIA,GUILayout.Width(w));
			GUILayout.Label("SP");
			data.end.SP = GUILayout.TextArea(data.end.SP,GUILayout.Width(w));
			GUILayout.Label("TEMP");
			data.end.TEMP = GUILayout.TextArea(data.end.TEMP,GUILayout.Width(w));
			GUILayout.Label("RESP");
			data.end.RESP = GUILayout.TextArea(data.end.RESP,GUILayout.Width(w));
			GUILayout.EndHorizontal();
			GUILayout.EndArea();			
#endif
#if SHOW_DB_CASES
			GUILayout.BeginArea (new Rect(guiRect.width*.6f,guiRect.height*0.50f,guiRect.width*.3f,guiRect.height*0.20f));
			GUILayout.BeginHorizontal();
			GUILayout.Label("DB Saved Options");
			if ( GUILayout.Button("Refresh List") )
				LoadCaseConfigurations("rob");
			GUILayout.EndHorizontal();
			dbScrollPosition = GUILayout.BeginScrollView(dbScrollPosition,false,true);
			foreach( CaseInfo ci in CaseList )
			{
				if ( GUILayout.Button(ci.name) )
					LoadCaseConfiguration(ci);
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();			
#endif
		}
		GUILayout.EndScrollView();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("START"))
		{
			CaseConfiguratorMgr.GetInstance().Data = data;
			loadAndRun = true;
		}
		if (GUILayout.Button("SAVE as:"))
		{
			Serializer<CaseOptionData> serializer = new Serializer<CaseOptionData>();
			serializer.Save ("Assets/Resources/XML/Cases/"+data.casename+".xml",data);
			// save config
			SaveCaseConfiguration(data.casename,data.description,"rob");
		}
		data.casename = GUILayout.TextField(data.casename);
		data.description = GUILayout.TextField(data.description);
		GUILayout.EndHorizontal();
	}
	
	public void SaveCase()
	{
		// saves current case
		CaseConfiguratorMgr.GetInstance().SaveCaseConfiguration(null);
	}
	
	public void SaveCaseConfiguration( string name, string description, string owner )
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "saveCase");
        form.AddField("name", name);
        form.AddField("description", description);
        form.AddField("owner", owner);
        form.AddField("data", data.ToString());
		DBCall(GameMgr.GetInstance().DatabaseURL,form,null);
		
		// test...
		LoadCaseConfigurations(owner);
	}	
	
	public class CaseInfo
	{
		public string name;
		public string description;
		public string owner;
	}
	List<CaseInfo> CaseList = new List<CaseInfo>();
	
	void loadCases(bool status, string data, string error_msg, WWW download)
	{
		CaseList.Clear();
		
		string[] split = data.Split('#');
		foreach( string item in split )
		{
			string[] fields = item.Split('&');
			if ( fields.Length == 3 )
			{
				CaseInfo ci = new CaseInfo();
				ci.owner = fields[0];
				ci.name = fields[1];
				ci.description = fields[2];
				CaseList.Add(ci);
			}
		}
		
		LoadCaseConfiguration(CaseList[0]);
	}
	
	public void LoadCaseConfigurations( string owner )
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "loadCases");
        form.AddField("owner", owner);
		DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCases);
	}

	void loadCase(bool status, string data, string error_msg, WWW download)
	{
		// convert from string data to serialized
		Serializer<CaseOptionData> serializer = new Serializer<CaseOptionData>();
		this.data = serializer.FromString(data);
	}
	
	public void LoadCaseConfiguration( CaseInfo ci )
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "loadCase");
        form.AddField("name", ci.name);
		DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCase);
	}	

	public void DBCall(string URL, WWWForm form, DatabaseMgr.Callback callback)
	{
		// if we are offline then do trauma offline container
		if (TraumaOfflineAssetContainer.GetInstance () != null &&
		    TraumaOfflineAssetContainer.GetInstance ().bUseOfflineAssets ) {
			TraumaOfflineAssetContainer.GetInstance ().DBCallOffline (GameMgr.GetInstance().DatabaseURL, form, callback);
		} else {
			DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,callback);
		}
	}
}

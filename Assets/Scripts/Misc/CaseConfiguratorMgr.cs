using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CaseConfigOption
{
	public string Name = "";
	public string Prefab = "";
	public bool Enabled = true;
	public string PrefabWhenDisabled = "";
	public string shortDescription = "";
	public string longDescription = "";
	public string[] param;
}

[System.Serializable]
public class InjuryInfo
{
	public string Name;
	public string Prefab;
	public List<CaseConfigOption> SecondaryOptions;
	
	public static bool operator ==( InjuryInfo a, InjuryInfo b)
	{
		// check for null check
		if ( System.Object.ReferenceEquals(b, null) == true )
			return System.Object.ReferenceEquals(a, null);
		
		if ( a.Name != b.Name )
			return false;
		if ( a.SecondaryOptions.Count != b.SecondaryOptions.Count )
			return false;
		for (int i=0 ; i<a.SecondaryOptions.Count ; i++)
		{
			if ( a.SecondaryOptions[i].Name != b.SecondaryOptions[i].Name )
				return false;
			if ( a.SecondaryOptions[i].Enabled != b.SecondaryOptions[i].Enabled )
				return false;			
		}		
		return true;
	}
	
	public static bool operator !=( InjuryInfo a, InjuryInfo b)
	{
		return !(a == b);
	}
}

public class VitalsOption
{
	public string HR;
	public string BPSYS;
	public string BPDIA;
	public string SP;
	public string TEMP;
	public string RESP;
	public VitalsOption()
	{
		HR = "-1";
		BPSYS = "-1";
		BPDIA = "-1";
		SP = "-1";
		TEMP = "-1";
		RESP = "-1";
	}
}

[System.Serializable]
public class CaseOptionData
{
	public string owner;
	// patient info block
	public string name;
	public string age;
	public string gender;
	public string bloodtype;
	//
	public string caseDescription;
	public string description="";
	public string casename="";
	public string loadedCase="";
	public string shortName="";
	public CaseConfigOption baseConfig;
	public CaseConfigOption[] options;
	public CaseConfigOption[] team;
	public VitalsOption start;
	public VitalsOption end;
	public InjuryInfo injury;
	public string timeOfDeath;
	public string caseThumbnail;
	public string missionStartXml;
	public bool changed;
	public bool template;
	public List<string> users;
	
	public CaseOptionData()
	{
		start = new VitalsOption();
		end = new VitalsOption();
		name = "";
		description = "";
		caseDescription = "";
		baseConfig = new CaseConfigOption();
		baseConfig.Name = "Trauma_BasePrefab";
		baseConfig.Prefab = "Trauma_BasePrefab";
		baseConfig.Enabled = true;
		gender = "male";
		bloodtype = "n/a";
		timeOfDeath = "10";
		injury = null;
		changed = false;
		caseThumbnail = "thumbnail01";
		missionStartXml = "trauma05MissionStart"; // specify per case in CaseInfo.xml
		users = new List<string>();
	}
	
	public string ToString()
	{
		// serialize and return
		Serializer<CaseOptionData> serializer = new Serializer<CaseOptionData>();
		return serializer.ToString(this);
	}	
}

public class CaseConfiguratorMgr
{
	public CaseConfiguratorMgr()
	{
		data = null;
		caseValid = false;
	}
	
	CaseConfigurator _ccScript=null;
	CaseConfigurator ccScript
	{
		get 
		{
			if ( _ccScript == null )
			{
				GameObject cc = GameObject.Find ("CaseConfiguration");
				if ( cc != null )
				{
					_ccScript = cc.GetComponent<CaseConfigurator>() as CaseConfigurator;
				}
			}
			return _ccScript;
		}
	}
	
	// startup data
	CaseOptionData data;
	public CaseOptionData Data
	{
		get 
		{ 	
			return data; 
		}
		
		set
		{ 
			if ( CaseConfigurator.GetInstance() != null )
				CaseConfigurator.GetInstance().Data = value;
			//if ( ccScript != null )
			//	ccScript.Data = value;			
			data = value;
		}
	}
	
	bool caseValid;
	public bool CaseValid
	{
		get { return caseValid; }
		set { caseValid = value; }
	}
	
	// global instance
	private static CaseConfiguratorMgr instance;
	public static CaseConfiguratorMgr GetInstance()
	{
		if (instance == null)
		{
			instance = new CaseConfiguratorMgr();
			instance.Init();
		}
		
		return instance;
	}
	
	public List<InjuryInfo> InjuryConfig;	
	public List<CaseConfigOption> OptionConfig;
	
	public CaseConfigOption GetOptionConfig( string name )
	{
		foreach( CaseConfigOption option in OptionConfig )
		{
			if ( option.Name == name )
				return option;
		}
		return null;
	}
	
	public void Init()
	{
		Serializer<List<InjuryInfo>> serializer1 = new Serializer<List<InjuryInfo>>();
		InjuryConfig = serializer1.Load("XML/CaseConfigInjuries");		
		
		Serializer<List<CaseConfigOption>> serializer2 = new Serializer<List<CaseConfigOption>>();
		OptionConfig = serializer2.Load("XML/CaseConfigOptions");		
	}
	
	public void CreateCase()
	{
		// create new case
		Data = new CaseOptionData();
		// set to first injury
		Data.injury = CaseConfiguratorMgr.GetInstance().InjuryConfig[0];
		// set owner
		Data.owner = LoginMgr.GetInstance().Username;
	}
	
	public void DeleteCase( string caseName )
	{
		WWWForm form = new WWWForm();
		form.AddField("command", "deleteCase");
		form.AddField("case", caseName);
		DBCall(GameMgr.GetInstance().DatabaseURL,form,null);
	}
	
	public InjuryInfo FindInjury( string name )
	{
		foreach( InjuryInfo info in InjuryConfig )
		{
			if ( info.Name == name )
				return info;
		}
		return null;
	}
	
	public bool CaseAlreadyExists( string casename )
	{
		if ( CaseList != null )
		{
			foreach( CaseInfo info in CaseList )
			{
				if ( info.name == casename )
					return true;
			}
		}
		return false;
	}
	
	public class CaseInfo
	{
		public string name;
		public string descriptionShort;
		public string description;
		public string owner;
		public string template;
		public string thumbnail;
		public string datetime;
		// 
		public CaseOptionData CaseOptionData;
		
		public void SetOptionData( string data )
		{
			if ( data != null )
			{
				Serializer<CaseOptionData> serializer = new Serializer<CaseOptionData>();
				CaseOptionData = serializer.FromString(data);
			}
		}
	}
	
	public List<CaseInfo> CaseList = new List<CaseInfo>();	
	
	public void SaveXML( string filename )
	{
		Serializer<List<CaseInfo>> serializer = new Serializer<List<CaseInfo>>();
		serializer.Save (filename,CaseList);
	}
	
	public bool UsingLocalData=true;
	public List<CaseInfo> LoadXML( string filename )
	{
		if ( UsingLocalData == false )
			return null;
		Serializer<List<CaseInfo>> serializer = new Serializer<List<CaseInfo>>();
		CaseList = serializer.Load(filename);
		if ( CaseList == null )
			UsingLocalData = false;
		return CaseList;
	}
	
	public CaseInfo LoadCaseInfo( string name )
	{
		if ( CaseList == null )
			return null;
		
		foreach( CaseInfo item in CaseList )
		{
			if ( item.name == name )
				return item;
		}
		UnityEngine.Debug.LogError ("CaseConfigurator.LoadCaseInfo(" + name + ") can't find case!");
		return null;
	}
	
	public List<string> CaseOrder;
	
	public bool UsingCaseOrder=false;
	public List<string> LoadCaseOrder( string filename=null )
	{
		if ( UsingCaseOrder == false )
		{
			CaseOrder = new List<string>();
			foreach( CaseInfo item in CaseList )
			{
				CaseOrder.Add (item.name);
			}
		}
		else
		{
			if ( filename == null )
				filename = "XML/CaseOrder";
			Serializer<List<string>> serializer = new Serializer<List<string>>();
			CaseOrder = serializer.Load(filename);
			if ( CaseOrder == null )
			{
				// we had an error, just get case order from CaseList
				CaseOrder = new List<string>();
				foreach( CaseInfo item in CaseList )
					CaseOrder.Add (item.name);
			}
		}
		return CaseOrder;
	}
	
	DatabaseMgr.Callback loadCasesCallback;
	DatabaseMgr.Callback loadCaseCallback;
	DatabaseMgr.Callback loadCaseUsersCallback;
	
	public void loadCases(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			CaseList.Clear();
			
			string[] split = data.Split('#');
			foreach( string item in split )
			{
				string[] fields = item.Split('&');
				if ( fields.Length >= 7 )
				{
					CaseInfo ci = new CaseInfo();
					ci.owner = fields[0];
					ci.name = fields[1];
					ci.descriptionShort = fields[2];
					ci.description = fields[3];
					ci.template = fields[4];
					ci.thumbnail = fields[5];
					ci.datetime = fields[6];
					CaseList.Add(ci);
					// add data if field length is greater than 7
					if ( fields.Length >= 8 )
					{
						ci.SetOptionData(fields[7]);
					}
				}
			}
		}
		if ( loadCasesCallback != null )
			loadCasesCallback(status,data,error_msg,download);
	}
	
	public void LoadCaseConfigurations( DatabaseMgr.Callback callback )
	{
		if ( UsingLocalData == true )
		{
			// first try to load local CaseInfo.xml....if available then just use that
			if ( LoadXML("XML/CaseInfo") != null )
			{
				// we have a local file, we're ok
				callback(true,null,null,null);
				return;
			}
		}
		else
		{
			loadCasesCallback = callback;
			
			WWWForm form = new WWWForm();
			form.AddField("command", "loadCases");
			form.AddField("owner", LoginMgr.GetInstance().Username);
			// if we are offline then do trauma offline container
			if (TraumaOfflineAssetContainer.GetInstance () != null &&
			    TraumaOfflineAssetContainer.GetInstance ().bUseOfflineAssets ) {
				TraumaOfflineAssetContainer.GetInstance ().DBCallOffline (GameMgr.GetInstance().DatabaseURL, form, loadCases);
			} else {
				DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCases);
			}
		}
	}
	
	public void LoadUserAssignedCases( DatabaseMgr.Callback callback )
	{
		// first try to load local CaseInfo.xml....if available then just use that
		if ( LoadXML("XML/CaseInfo") != null )
		{
			// we have a local file, we're ok
			if ( callback != null )
				callback(true,null,null,null);
			return;
		}
		
		loadCasesCallback = callback;
		
		WWWForm form = new WWWForm();
		form.AddField("command", "loadUserAssignedCasesWithData");	// command without data is loadUserAssignedCases
		form.AddField("username", LoginMgr.GetInstance().Username);
		
		// if we are offline then do trauma offline container
		if (TraumaOfflineAssetContainer.GetInstance () != null &&
		    TraumaOfflineAssetContainer.GetInstance ().bUseOfflineAssets ) {
			TraumaOfflineAssetContainer.GetInstance ().DBCallOffline (GameMgr.GetInstance().DatabaseURL, form, loadCases);
		} else {
			DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCases);
		}
	}
	
	void PrepareCaseForStart()
	{
		if ( Data == null )
			return;
		CaseValid = true;			
		Data.changed = false;
		Data.loadedCase = this.data.casename;		
		// fixup data
		if ( Data.baseConfig.Prefab == "" )
			Data.baseConfig.Prefab = Data.baseConfig.Name;
	}
	
	void loadCase(bool status, string data, string error_msg, WWW download)
	{	
		// if status ok then set case valid
		if ( status == true )
		{
			// convert from string data to serialized
			Serializer<CaseOptionData> serializer = new Serializer<CaseOptionData>();
			this.Data = serializer.FromString(data);
			if ( this.Data != null )
			{
				UnityEngine.Debug.Log ("CaseConfigurator.loadCase() : casename=<" + this.data.casename + ">, loadTime=" + (Time.time-loadTime).ToString ());
				PrepareCaseForStart();
			}
			else
			{				
				// error msg
				error_msg = "DBError: de-serialize error!";
				// dialog
				// do callback with error
				if ( loadCaseCallback != null )
					loadCaseCallback(false,data,error_msg,download);
				return;
			}
		}
		// callback
		if ( loadCaseCallback != null )
			loadCaseCallback(status,data,error_msg,download);		
	}
	
	public void LoadCaseConfiguration( CaseInfo ci, DatabaseMgr.Callback callback )
	{
		if ( ci != null )
			LoadCaseConfiguration(ci.name,callback);
	}
	
	float loadTime;
	
	public void LoadCaseConfiguration( string name, DatabaseMgr.Callback callback )
	{
		if ( UsingLocalData == true )
		{
			// load local case if available
			CaseInfo localCase = LoadCaseInfo(name);
			if ( localCase != null )
			{
				// set local data
				Data = localCase.CaseOptionData;
				// prepare
				PrepareCaseForStart();
				// do callback
				if ( callback != null )
					callback(true,null,null,null);
			}
		}
		else
		{
			// check to see if we have local data, if so just copy the data
			loadCaseCallback = callback;	
			
			WWWForm form = new WWWForm();
			form.AddField("command", "loadCase");
			form.AddField("name", name);
			loadTime = Time.time;
			// if we are offline then do trauma offline container
			if (TraumaOfflineAssetContainer.GetInstance () != null &&
			    TraumaOfflineAssetContainer.GetInstance ().bUseOfflineAssets ) {
				TraumaOfflineAssetContainer.GetInstance ().DBCallOffline (GameMgr.GetInstance().DatabaseURL, form, loadCase);
			} else {
				DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCase);
			}
		}
	}

	public void SetCurrentCase( CaseInfo item )
	{
		if ( item != null )
		{
			Data = item.CaseOptionData;
			PrepareCaseForStart();
		}
	}
	
	public void SaveCaseConfiguration( string name )
	{
		if ( data == null )
			return;
		
		// only if specifying a name
		if ( name != null )
			data.casename = name;
		
		// default owner to currently logged in person
		if ( data.owner == null || data.owner == "" )
			data.owner = LoginMgr.GetInstance().Username;
		
		//		data.overrideConfig = new CaseOption();
		//		data.overrideConfig.name = "Trauma05_OverridePrefab";
		
		WWWForm form = new WWWForm();
		form.AddField("command", "saveCase");
		form.AddField("name", data.casename);
		form.AddField("descriptionShort", data.description);
		form.AddField("description", data.caseDescription);
		form.AddField("thumbnail", data.caseThumbnail);
		form.AddField("template", data.template.ToString());
		form.AddField("owner", data.owner);
		form.AddField("data", data.ToString());
		DBCall(GameMgr.GetInstance().DatabaseURL,form,null);
		
		data.changed = false;
	}		
	
	public void StartCase()
	{
		// load HUD
		GUIManager.GetInstance().RemoveAllScreens();
		GUIManager.GetInstance().LoadFromFile("hud3");
		// no loader, we are starting from inside the game
		CaseConfigurator cc = GameObject.Find("CaseConfiguration").GetComponent<CaseConfigurator>();
		if ( cc != null )
			cc.LoadAndRun();
		
		// clear the logs
		LogMgr.GetInstance().ClearLogs();
		LogMgr.GetInstance().CreateLog("interactlog");
		LogMgr.GetInstance().SetCurrent("interactlog");

		// init the InteractPlaybackMgr
		if ( InteractPlaybackMgr.GetInstance() != null && InteractPlaybackMgr.GetInstance().EnableAutoLogging)
			InteractPlaybackMgr.GetInstance().InitSaveFile();
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


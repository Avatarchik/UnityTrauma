using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml.Serialization;

public enum eScanType{
	XRay,
	FAST,
	CT,
}

[System.Serializable]
public class ScanRecord
{
	public string Name; 
	public eScanType Type;
	public string Filename; // resource path or url for CT, an xml in resources of string[] image stack paths
	public string Thumbnail; // still frame for index/viewer
	public string TimeTaken;
	public string Region;
	public string Assessment;
	#if !UNITY_IPHONE
	[XmlIgnore]
	public MovieTexture movieTexture; // cache the downloaded movie
	#endif
	public List<FilterInteractions.CommandVariation> assessmentCommands; // need to map these to buttons somehow
}

public class PatientRecord
{
    public PatientRecord() {}

    public string Name;
    public PatientInfo Info;
    public List<ScanRecord> XRayRecords;
	public List<ScanRecord> FastRecords;
	public List<ScanRecord> CTRecords;

    public string GetXRay(string name) // Deprecated, currently not used  GetXRayRecord instead
    {
		foreach (ScanRecord record in XRayRecords)
        {
            if (record.Name.ToLower() == name.ToLower ())
                return record.Filename;
        }
        return null;
    }
	public string GetFast(string name) // Deprecated, currently not used  GetFastRecord instead
	{
		foreach (ScanRecord record in FastRecords)
        {
            if (record.Name.ToLower() == name.ToLower())
                return record.Filename;
        }
        return null;
    }
	public string GetCT(string name) // Deprecated, currently not used  GetCTRecord instead
	{
		foreach (ScanRecord record in CTRecords)
        {
            if (record.Name.ToLower() == name.ToLower())
                return record.Filename;
        }
        return null;
    }
	public ScanRecord GetXRayRecord(string name)
    {
		// when an xray is taken, and the popup dialog is loaded, this is called to return the xray to display.
		// this should be a record who's time taken is null.  then, a copy of that record should be added with a timestamp
		// on it, so if another is taken, the whole sequence can be kept.



		foreach (ScanRecord record in XRayRecords)
        {
			if (record.Name.ToLower() == name.ToLower () && (record.TimeTaken == null || record.TimeTaken == "" )){
				ScanRecord newRecord = new ScanRecord();
				newRecord.Name = record.Name;
				newRecord.Type = record.Type ;
				newRecord.Filename = record.Filename; // resource path or url for CT, an xml in resources of string[] image stack paths
				newRecord.Thumbnail = record.Thumbnail; // still frame for index/viewer
				newRecord.Region = record.Region;
				XRayRecords.Add (newRecord);
				// the dialog will timestamp this new one and future xrays will clone the null timestamp one...
                return newRecord;
			}
        }
        return null;
    }
	public ScanRecord GetFastRecord(string name)
    {
		foreach (ScanRecord record in FastRecords)
        {
            if (record.Name.ToLower() == name.ToLower())
                return record;
        }
        return null;
    }
	public ScanRecord GetCTRecord(string name)
    {
		foreach (ScanRecord record in CTRecords)
        {
            if (record.Name.ToLower() == name.ToLower())
                return record;
        }
        return null;
    }

}

public class PatientInfo
{
    public PatientInfo() { }
	
    public string name;
    public string dob;
    public string bloodtype;
    public string allergies;
    public string contact;

    public string initialVitalState;
    public string initialVitalsBehavior;
	public DateTime startingDateTime; // created from the startingTime in the xml
	public string startingTime = ""; // "hh.mm" 24 hour format

	// deprecate these once we have full sets of XML records for everything
	public string chestXRAY;
	public string pelvicXRAY;
	public List<string> fast;
	public List<string> fastThumbnail;
	public List<string> CT;
	public List<string> CTThumbnail;
	// just the scanRecords should contain ALL the Xray, FAST and CT resource pathnnames.
	public List<string> scanRecords;

    public List<string> decisions;

	public int gcsEyes = 0;
	public int gcsVerbal = 0;
	public int gcsMotor = 0;
	public int gcsTotal = 0;

    public void Debug()
    {
        UnityEngine.Debug.Log("PatientInfo : name=" + name + " : dob=" + dob + " : bloodtype=" + bloodtype + " : allergies=" + allergies + " : contact=" + contact);
    }
}

[System.Serializable]
public class BreathSound{
	public float minResp;
	public float maxResp;
	public AudioClip sound;
	public float breathsPerClip;
}

public abstract class Patient : Character
{
	public string patientInfoXML = "XML/Patient/PatientInfo";
    public PatientInfo Info;
    public PatientVitals Vitals;
    public List<PatientRecord> PatientRecords;  // records based on case or scenario
    PatientRecord Record;

    public PatientRecord SetPatientRecord( string scenario ) // these are not going to work yet, each patient only has one
    {
        Record = GetPatientRecord(scenario);
        return Record;
    }

    public PatientRecord GetPatientRecord()
    {
        return Record;
    }

    public PatientRecord GetPatientRecord(string scenario)
    {
        foreach (PatientRecord record in PatientRecords)
        {
            if (record.Name == scenario)
                return record;
        }
        return null;
    }

	public BreathSound[] breathSounds;  // set this in the editor for now.  add load from xml by condition later ?

    public bool Intubated = false;
	public bool EndScenario = false;
    // variable interface

    float seek=0.0f;
    public float SEEK_RATE
    {
        get { return seek; }
        set 
        {
            //UnityEngine.Debug.LogWarning("Patient.SEEK_RATE.Set(" + value + ")");
            seek = value; 
        }
    }

	public int UnitsBlood = 0;
	public int UnitsPRBC = 0;
	public int UnitsPlatelets = 0;
	public int UnitsPlasma = 0;
	public int UnitsSaline = 0;
	public int UnitsCrystal = 0;
	public int UnitsRingers = 0;
	public int UnitsHypertonic = 0;
	
	// total blood bags running
    int totalBlood;
    public int TotalBloodBags
    {
        get { return totalBlood; }
        set { totalBlood = value; }
    }
	
	// max number of blood bags which have an effect
    int maxBlood = 6;
    public int MaxBloodBags
    {
        get { return maxBlood; }
        set { maxBlood = value; }
    }
	
	// total count of saline bags
    int totalSaline;
    public int TotalSalineBags
    {
        get { return totalSaline; }
        set { totalSaline = value; }
    }
	
	// max number of saline bags that can create an effect
    int maxSaline = 2;
    public int MaxSalineBags
    {
        get { return maxSaline; }
        set { maxSaline = value; }
    }

	public int TotalFluids // read only
	{
		get { return totalBlood + totalSaline; }
		set { }
	}

	// blood IV normal bag
    int bloodbagsIV;
    int lastBloodIV;
    public int BloodbagsIV
    {
        get { return bloodbagsIV; }
        set { bloodbagsIV = value; }
    }

	// blood pressure bag
    int bloodbagsPR;
    int lastBloodPR;
    public int BloodbagsPR
    {
        get { return bloodbagsPR; }
        set { bloodbagsPR = value; }
    }
	
	// blood rapid infuser bag
    int bloodbagsRI;
    int lastBloodRI;
    public int BloodbagsRI
    {
        get { return bloodbagsRI; }
        set { bloodbagsRI = value; }
    }
	
	// saline IV bag normal
    int salinebagsIV;
    int lastSalineIV;
    public int SalinebagsIV
    {
        get { return salinebagsIV; }
        set { salinebagsIV = value; }
    }
	
	// saline rapid infuser bag
    int salinebagsRI;
    int lastSalineRI;
    public int SalinebagsRI
    {
        get { return salinebagsRI; }
        set { salinebagsRI = value; }
    }
	
	// saline pressure bag
    int salinebagsPR;
    int lastSalinePR;
    public int SalinebagsPR
    {
        get { return salinebagsPR; }
        set { salinebagsPR = value; }
    }
	
	public string TimeAdvance{ // used to alter simulated time through setting an atttribute
		get { return ""; }
		set {
			if (value.Contains("+")){
				string timeMod = value.Replace("+","");
				// for now, lets just handle adding some minutes.
							// look for TraumaBrain and set the starting time there
				Brain tb = FindObjectOfType(typeof(Brain)) as Brain;
				if (tb != null){
					tb.AdvanceStartTime("00:"+timeMod.ToString());	
				}
				// now for the tricky part, to advance all the vitals behaviors
				VitalsBehaviorManager vbm = FindObjectOfType<VitalsBehaviorManager>();
				if (vbm != null){
					float duration = 0;
					float.TryParse(timeMod, out duration);
					duration *= 60; // value in munutes, we need seconds...
					vbm.ProcessTimeLapsed(duration);
				}
			}
		}
	}
	
	
/*	now, we just look at the timestamp for each record.
	bool fastPerformed = false; // this should be set to true when the xray is taken
	public bool FASTPerformed
	{
		get { return fastPerformed;  }
		set { fastPerformed = value; }
	}
*/
	
	// current chestXRAY style, gets set when PatientInfo gets
	// loaded for this case
	string chestXRAY="none";
	public string ChestXRAY
	{
		get { if (chestXrayTaken) return chestXRAY;
			  else return "none"; }
		set { chestXRAY = value; }
	}
// we should deprecate these and use the timestamp for detecting xray taken.
	bool chestXrayTaken = false; // this should be set to true when the xray is taken
	public bool ChestXrayTaken
	{
		get { return chestXrayTaken;  }
		set { chestXrayTaken = value; }
	}
	
	// current pelvicXRAY style, gets set when PatientInfo gets
	// loaded for this case
	string pelvicXRAY="none";
	public string PelvicXRAY
	{
		get { if (pelvicXrayTaken) return pelvicXRAY;
			  else return "none"; }
		set { pelvicXRAY = value; }
	}
	bool pelvicXrayTaken = false; // this should be set to true when the xray is taken
	public bool PelvicXrayTaken
	{
		get { return pelvicXrayTaken; }
		set { pelvicXrayTaken = value; }
	}

	string armrightXRAY="none";
	public string ArmRightXRAY
	{
		get { if (armrightXrayTaken) return armrightXRAY;
			else return "none"; }
		set { armrightXRAY = value; }
	}
	// we should deprecate these and use the timestamp for detecting xray taken.
	bool armrightXrayTaken = false; // this should be set to true when the xray is taken
	public bool ArmRightXrayTaken
	{
		get { return armrightXrayTaken;  }
		set { armrightXrayTaken = value; }
	}

	// these should be the only things we need now, the above could be reduced to just the public bool.
	// These are set as Decision Variables when the xray is performed in the scene.  There should be media for each of the regions in the Records list.
	public bool SkullXrayTaken = false;
	public bool CSpineXrayTaken = false;
	public bool AbdomenXrayTaken = false;
	public bool ElbowRightXrayTaken = false;
	public bool ForearmRightXrayTaken = false;
	public bool HandRightXrayTaken = false;
	public bool ArmLeftXrayTaken = false;
	public bool ElbowLeftXrayTaken = false;
	public bool ForearmLeftXrayTaken = false;
	public bool HandLeftXrayTaken = false;
	public bool ThighRightXrayTaken = false;
	public bool LegRightXrayTaken = false;
	public bool ThighLEftXrayTaken = false;
	public bool LEgLEftXrayTaken = false;

	// GameObject reference to the name of the IV bad currently
	// sitting on the left hanger
	string ivHangerLeft;
	public string IVHangerLeft
	{
		get { return ivHangerLeft; }
		set { ivHangerLeft = value; }
	}

	// GameObject reference to the name of the IV bad currently
	// sitting on the right hanger
	string ivHangerRight;
	public string IVHangerRight
	{
		get { return ivHangerRight; }
		set { ivHangerRight = value; }
	}
	
	// heart rate
    public float HR
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("HR").GetVal();  
		}
        set 
        {
            //UnityEngine.Debug.LogWarning("HR.set(" + value + ")");
			if ( Vitals != null )
			{
	            Vitals.Get("HR").Set(value, seek);
	            Vitals.Get("HR").ForceUpdate();
			}
        }
    }
	
	// respiration rate - should we rename this RESP for consistency ?
    public float RR
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("RESP").GetVal();  
		}
		set
        {
			if ( Vitals != null )
			{
	            //UnityEngine.Debug.LogWarning("HR.set(" + value + ")");
	            Vitals.Get("RESP").Set(value, seek);
	            Vitals.Get("RESP").ForceUpdate();
			}
        }
    }
	public float RESP
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("RESP").GetVal();  
		}
		set
        {
			if ( Vitals != null )
			{
	            //UnityEngine.Debug.LogWarning("HR.set(" + value + ")");
	            Vitals.Get("RESP").Set(value, seek);
	            Vitals.Get("RESP").ForceUpdate();
			}
        }
    }
	
	// current vital state
    public string VITAL_STATE
    {
        get { return Vitals.State.Name; }
        set 
        {
            //UnityEngine.Debug.LogWarning("VITAL_STATE.set(" + value + ") seek=" + seek);
            Vitals.Set(value, seek); 
        }
    }
	
	// current systolic blood pressure
    public float BP_SYS
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("BP_SYS").GetVal(); 
		}
        set 
        { 
			if ( Vitals != null )
			{
	            Vitals.Get("BP_SYS").Set(value, seek);
	            Vitals.Get("BP_SYS").ForceUpdate();
			}
        }
    }

	// current diastolic blood pressure
    public float BP_DIA
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("BP_DIA").GetVal(); 
		}
        set { 
			if ( Vitals != null )
			{
	            Vitals.Get("BP_DIA").Set(value, seek);
	            Vitals.Get("BP_DIA").ForceUpdate();
			}
        }
    }
	
	// current O2 level
    public float SP
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("SP").GetVal(); 
		}
        set { 
			if ( Vitals != null )
			{
	            Vitals.Get("SP").Set(value, seek);
	            Vitals.Get("SP").ForceUpdate();
			}
        }
    }
	
	// current temperature
    public float TEMP
    {
        get 
		{ 
			if ( Vitals == null ) 
				return 0;
			else
				return Vitals.Get("TEMP").GetVal(); 
		}
        set { 
			if ( Vitals != null )
			{
	            Vitals.Get("TEMP").Set(value, seek);
	            Vitals.Get("TEMP").ForceUpdate();
			}
        }
    }

    public string Airway
    {
        get { return PatientStatusMgr.GetInstance().Current.Airway.Status; }
        set { PatientStatusMgr.GetInstance().Current.Airway.Status = value; }
    }

    public string Breathing
    {
        get { return PatientStatusMgr.GetInstance().Current.Breathing.Status; }
        set { PatientStatusMgr.GetInstance().Current.Breathing.Status = value; }
    }

    public string Circulation
    {
        get { return PatientStatusMgr.GetInstance().Current.Circulation.Status; }
        set { PatientStatusMgr.GetInstance().Current.Circulation.Status = value; }
    }

	public float TIME
	{
		get { 
			Timer timer = FindObjectOfType<Timer>();
			float time = timer.GetTime(); // will this work without further conditioning ?

			return time; //Time.timeSinceLevelLoad;
		}
		/*
		 * get the time from this timer object, which gets advanced, etc...
		 * 
		Timer timer = FindObjectOfType(typeof(Timer)) as Timer;
		timer.AddTime(mins*60);
		*/


	}
	
	string heartbeat="normal";
	public string HeartbeatType
	{
		get { return heartbeat; }
		set { heartbeat = value; }
	}
	
	bool bpCuffError=false;
	public bool BPCuffError
	{
		get { return bpCuffError; }
		set { bpCuffError= value; }
	}
	
    int gcs_eyes;
    public int GCS_EYES
    {
        get { return gcs_eyes; }
        set { gcs_eyes = value; }
    }	
    int gcs_verbal;
    public int GCS_VERBAL
    {
        get { return gcs_verbal; }
        set { gcs_verbal = value; }
    }
    int gcs_motor;
    public int GCS_MOTOR
    {
        get { return gcs_motor; }
        set { gcs_motor = value; }
    }
    int gcs_total;
    public int GCS_TOTAL
    {
        get { return gcs_eyes+gcs_verbal+gcs_motor; }
        set { gcs_total = value; }
    }

	public float GCS_TOTAL_FLOAT
	{
		get { return (float)GCS_TOTAL; }
	}
/*
    public Patient()
        : base()
    {
        Intubated = false;
    }
*/
    public override void Start()
    {
        base.Start();

        LoadInfoXML( patientInfoXML );	
		
	}

    public void LoadInfoXML(string filename)
    {
        Serializer<PatientInfo> serializer = new Serializer<PatientInfo>();
        Info = serializer.Load(filename);
        Info.Debug();
		
		// setup the info.startingDateTime
		
		Info.startingDateTime = DateTime.Now;
		if (Info.startingTime != ""){
			// look for TraumaBrain and set the starting time there
			Brain tb = FindObjectOfType(typeof(Brain)) as Brain;
			if (tb != null){
				tb.SetStartTime(Info.startingTime);	
			}
			
			// make one for us, maybe we don't really need our own...
			string[] ss = Info.startingTime.Split (':')	;
			if (ss.Length == 2){
				int hours = 0;
				int mins = 0;
				int.TryParse(ss[0],out hours);
				int.TryParse(ss[1],out mins);
				TimeSpan ts = new TimeSpan(hours, mins, 0);
				Info.startingDateTime = Info.startingDateTime.Date + ts;
			}
		}

        // initialize initial patient vitals
        Vitals = new PatientVitals();
        Vitals.Set(Info.initialVitalState, 0.0f);
        VitalsMgr.GetInstance().SetCurrent(Vitals);
        // start behavior, null is nothing
        VitalsBehaviorManager.GetInstance().AddBehavior(Info.initialVitalsBehavior);
		VitalsBehaviorManager.GetInstance().StateChange += VitalsBehaviorChange;

		// lets create and load up the PatientRecords structure with this data
		if (PatientRecords == null){
			PatientRecords = new List<PatientRecord>();
		}
		Record = new PatientRecord();
		Record.Name = "filename"; //we currently only ever have one
		Record.Info = Info;
		Record.XRayRecords = new List<ScanRecord>();
		Record.FastRecords = new List<ScanRecord>();
		Record.CTRecords = new List<ScanRecord>();


		// load up the list of records
		Serializer<ScanRecord> scanSerializer = new Serializer<ScanRecord>();
		foreach (string recordPath in Info.scanRecords){
		//	string pathname = "XML/Patient/Records"+recordPath.Replace (".xml","");
			ScanRecord record = scanSerializer.Load(recordPath);
			LoadScanRecord( record );
		}
		
		// set XRAY images // deprecate these !!!  use the Record.XrayRecords, get by name.
		ChestXRAY = Info.chestXRAY;
		PelvicXRAY = Info.pelvicXRAY;

		gcs_eyes = Info.gcsEyes;
		gcs_verbal = Info.gcsVerbal;
		gcs_motor = Info.gcsMotor;
		gcs_total = Info.gcsTotal;
/*
		
		ScanRecord chestXrayRecord = new ScanRecord();
		chestXrayRecord.Name = "chest";
		chestXrayRecord.Region = "Chest";
		chestXrayRecord.Filename = Info.chestXRAY;
		Record.XRayRecords.Add (chestXrayRecord);

//		CreateStringXML( Info.fast);

		ScanRecord pelvicXrayRecord = new ScanRecord();
		pelvicXrayRecord.Name = "pelvis";
		pelvicXrayRecord.Region = "Pelvis";
		pelvicXrayRecord.Filename = Info.pelvicXRAY;	
		Record.XRayRecords.Add (pelvicXrayRecord);
		
		// make assumption about the order of fast filenames
		// perihepatic, perisplenic, pelvis, pericardium, chest
		ScanRecord perihepaticFastRecord = new ScanRecord();
		perihepaticFastRecord.Name = "perihepatic";
		perihepaticFastRecord.Region = "Perihepatic";
		perihepaticFastRecord.Filename = Info.fast[0];
		perihepaticFastRecord.Thumbnail = Info.fastThumbnail[0];
		Record.FastRecords.Add(perihepaticFastRecord);
		ScanRecord perisplenicFastRecord = new ScanRecord();
		perisplenicFastRecord.Name = "perisplenic";
		perisplenicFastRecord.Region = "Perisplenic";
		perisplenicFastRecord.Filename = Info.fast[1];
		perisplenicFastRecord.Thumbnail = Info.fastThumbnail[1];
		Record.FastRecords.Add(perisplenicFastRecord);
		ScanRecord pelvisFastRecord = new ScanRecord();
		pelvisFastRecord.Name = "pelvis";
		pelvisFastRecord.Region = "Pelvis";
		pelvisFastRecord.Filename = Info.fast[2];
		pelvisFastRecord.Thumbnail = Info.fastThumbnail[2];
		Record.FastRecords.Add(pelvisFastRecord);
		ScanRecord pericardiumFastRecord = new ScanRecord();
		pericardiumFastRecord.Name = "pericardium";
		pericardiumFastRecord.Region = "Pericardium";
		pericardiumFastRecord.Filename = Info.fast[3];
		pericardiumFastRecord.Thumbnail = Info.fastThumbnail[3];
		Record.FastRecords.Add(pericardiumFastRecord);
		if (Info.fast.Count > 4){
			ScanRecord chestFastRecord = new ScanRecord();
			chestFastRecord.Name = "chest";
			chestFastRecord.Region = "Chest";
			chestFastRecord.Filename = Info.fast[4];
			chestFastRecord.Thumbnail = Info.fastThumbnail[4];
			Record.FastRecords.Add(chestFastRecord);
		}
		// load CT results
		// make assumption about the order of fast filenames
		// brain, cspine, chest, abdomen, tlspine
		ScanRecord brainCTRecord = new ScanRecord();
		brainCTRecord.Name = "brain";
		brainCTRecord.Region = "Brain";
		brainCTRecord.Filename = Info.CT[0];
		brainCTRecord.Thumbnail = Info.CTThumbnail[0];
		Record.CTRecords.Add(brainCTRecord);
		ScanRecord cspineCTRecord = new ScanRecord();
		cspineCTRecord.Name = "cspine";
		cspineCTRecord.Region = "C-Spine";
		cspineCTRecord.Filename = Info.CT[1];
		cspineCTRecord.Thumbnail = Info.CTThumbnail[1];
		Record.CTRecords.Add(cspineCTRecord);
		ScanRecord chestCTRecord = new ScanRecord();
		chestCTRecord.Name = "chest";
		chestCTRecord.Region = "Chest";
		chestCTRecord.Filename = Info.CT[2];
		chestCTRecord.Thumbnail = Info.CTThumbnail[2];
		Record.CTRecords.Add(chestCTRecord);
		ScanRecord abdomenCTRecord = new ScanRecord();
		abdomenCTRecord.Name = "abdomen";
		abdomenCTRecord.Region = "Abdomen";
		abdomenCTRecord.Filename = Info.CT[3];
		abdomenCTRecord.Thumbnail = Info.CTThumbnail[3];
		Record.CTRecords.Add(abdomenCTRecord);
		ScanRecord tlspineCTRecord = new ScanRecord();
		tlspineCTRecord.Name = "tlspine";
		tlspineCTRecord.Region = "TL-Spine";
		tlspineCTRecord.Filename = Info.CT[4];
		tlspineCTRecord.Thumbnail = Info.CTThumbnail[4];
		Record.CTRecords.Add(tlspineCTRecord);
*/
		PatientRecords.Add(Record);
		// now these can be accessed with Patient.GetPatientRecord().GetXRay("name") etc



        // set initial state and seektimes
        // load decisions
        foreach( string decision in Info.decisions )
            DecisionMgr.GetInstance().LoadXML(decision);
    }

	public void LoadScanRecord(ScanRecord record){

		switch(record.Type){
		case eScanType.XRay:
			RemoveRecordByName(Record.XRayRecords, record);
			Record.XRayRecords.Add(record);
			break;
		case eScanType.FAST:
			RemoveRecordByName(Record.FastRecords, record);
			Record.FastRecords.Add(record);
			break;
		case eScanType.CT:
			RemoveRecordByName(Record.CTRecords, record);
			Record.CTRecords.Add(record);
			break;
		}
	}

	void RemoveRecordByName(List<ScanRecord> list, ScanRecord record){
		// this needs to only remove the null timestamp instance of the named record.
		foreach (ScanRecord test in list){
			if (test.Name == record.Name && (test.TimeTaken == null || test.TimeTaken == "")){
				list.Remove(test);
				return;
			}
		}

	}

/* just a one-off to create an xml file to make these from
	void CreateStringXML(List<string> list){

//		record.assessmentCommands = new List<FilterInteractions.CommandVariation>();
//		FilterInteractions.CommandVariation var = new FilterInteractions.CommandVariation();
//		record.assessmentCommands.Add(var);
//		record.assessmentCommands.Add(var);

		XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
		FileStream stream = new FileStream("Assets/Resources/CTPaths.xml", FileMode.Create);
		serializer.Serialize(stream, list);
		stream.Close();
	}
*/		
		


	
    public virtual void UpdateVitals()
    {
        // update vitals
        if (Vitals != null){
            Vitals.Update(Time.deltaTime);
		
			// handle discoloration of the patient at low SpO2 levels 
			float sp = Vitals.Get("SP").GetVal();
			if (sp < 94){ // the SpO2 at which discoloration begines
				// color the patient blue, down to 84
				float pct = sp-84; // the extreme bottom end -  fully discolored
				if (pct < 0) pct=0;
				float fade = 1.0f - pct/10.0f;
				if (fade < 0) fade=0;
//				pct = 0.75f + 0.25f* pct/10.0f; // percent at full discoloration, range of the effect
//				// just reduce the RED amount of the underlying color, which is normally a neutral gray
//				Color blueishness = gameObject.GetComponent<MultiMesh>().meshes[0].renderer.material.color;
//				blueishness.r = pct*blueishness.b;
//				gameObject.GetComponent<MultiMesh>().meshes[0].renderer.material.color = blueishness;
//				gameObject.GetComponent<MultiMesh>().meshes[1].renderer.material.color = blueishness;
				gameObject.GetComponent<MultiMesh>().meshes[0].renderer.material.SetFloat("_Fade", fade);
				gameObject.GetComponent<MultiMesh>().meshes[1].renderer.material.SetFloat("_Fade", fade);

			}
		}

        //if (Vitals.HasReached("Crashing",2.0f))
        //{
        //    Vitals.Set("Dead", 0.0f);
        //}
    }

    public virtual void UpdateMenu(InteractDialogMsg msg)
    {
        prettyname = msg.title;
        originXML = msg.baseXML;
    }
	
	bool doHRAudio = false;
    float hrTimer = 0.0f;
    public void AudioHR()
    {
		if ( doHRAudio == false )
			return;
		
        if (Time.time > hrTimer)
        {
            // compute remainder
            float diff = Time.time - hrTimer;
            // recompute timer
            hrTimer = 60.0f / HR - diff;
            // add current time
            hrTimer = hrTimer + Time.time;
            // play audio
            Brain.GetInstance().PlayAudio("AUDIO:HEARTBEAT");
        }
    }

//	bool doRRAudio = true;
	float rrTimer = 0.0f;
	public void AudioResp(){
		if (breathSounds.Length == 0)
						return;


		if (Time.time > rrTimer)
		{
			// if on BVM or ventilator, return.
			if (GetAttribute ("onbvm")=="True" || GetAttribute("intubating") == "True" || GetAttribute("%Intubated") == "True") {
				rrTimer = Time.time + 10;
				return;
			}

			float rate = 60.0f/RR; // seconds that each breath cycle shold take
			float pause = 1;
			rrTimer = Time.time + 1;
			if (!audio.isPlaying){
				// find the appropriate clip, if any, and play it
				AudioClip clip = null;
				foreach (BreathSound bs in breathSounds){
					if (bs.minResp <= RR && bs.maxResp >= RR){
						clip = bs.sound;
						pause = rate * bs.breathsPerClip - clip.length;
						if (pause <=1) pause = 1;
						break;
					}
				}
				if (clip != null){
					audio.clip = clip;
					audio.Play();
					rrTimer = Time.time + pause + clip.length;
				}
				// check again 1 sec after the clip will be done.
			}
			else
			{
				rrTimer = Time.time + 1;
			}
		}
	}

    float lastAlarm = 0.0f;
    public void Alarm()
    {
    }

    int showvitals = 0;
    public override void Update()
    {
        // play audio for HR
        AudioHR();
		// play audio for Breathing

		AudioResp ();
        
        base.Update();
		
#if SHORTCUT_KEYS
        if (Input.GetKeyUp(KeyCode.V))
		{		
			GUIScreen screen = GUIManager.GetInstance().FindScreenByType<VitalsGUI>();
			if ( screen == null )
			{
				// screen doesn't exist, make one
				DialogMsg dmsg = new DialogMsg();
				dmsg.xmlName = "dialog.vitals.grapher";
				dmsg.className = "VitalsGUI";
				GUIManager.GetInstance().LoadDialog( dmsg );
			}
			else
			{
				// exists, close it
				DialogMsg dmsg = new DialogMsg();
				dmsg.className = "VitalsGUI";
				GUIManager.GetInstance().CloseDialog( dmsg );
			}
		}

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            bloodbagsIV++;
            InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:CHANGE");
            this.PutMessage(ismsg);
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            if (--bloodbagsIV < 0)
                bloodbagsIV = 0;

            InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:CHANGE");
            this.PutMessage(ismsg);
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            bloodbagsRI++;
            InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:CHANGE");
            this.PutMessage(ismsg);
        }

        if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            if (--bloodbagsRI < 0)
                bloodbagsRI = 0;

            InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:CHANGE");
            this.PutMessage(ismsg);
        }
#endif

        UpdateVitals();
    }

    float HR_seektime = 0.0f;
    float BPSYS_seektime = 0.0f;
    float BPDIA_seektime = 0.0f;
    PatientState initialState;


    public void LoadPatientRecordsXML(string filename) // this is not currently used 11/14/13 PAA
    {
        Serializer<List<PatientRecord>> serializer = new Serializer<List<PatientRecord>>();
        PatientRecords = serializer.Load(filename);
    }

    public override void PutMessage(GameMsg msg)
    {
        // handle Meds
        MedAdministerMsg admin = msg as MedAdministerMsg;
        if (admin != null)
        {
            MedAdminister(admin);
        }

#if FIND_CHARACTER_FOR_TASK
        InteractMsg imsg = msg as InteractMsg;
        if (imsg != null && imsg.map != null)
        {
            string debug = "DEBUG";

            // patient can't really do anything so we need find a character
            // to do this interaction (if possible)
            List<ObjectInteraction> objects = ObjectInteractionMgr.GetInstance().GetEligibleObjects(imsg.map.item);
            foreach (ObjectInteraction obj in objects)
            {
                Character character = obj as Character;
                if (character != null)
                {
                    // make sure the available character is not busy and it isn't ME!
                    if (character.IsDone() == true && character.gameObject.name != this.Name)
                    {
                        Debug("Patient.PutMessage(" + imsg.map.item + ") : command sent to [" + character.gameObject.name + "]");
                        InteractionMap map = InteractionMgr.GetInstance().Get(imsg.map.item);
                        character.PutMessage(new InteractMsg(character.gameObject, map));
                        return;
                    }
                    else
                    {
                        debug += " : character[" + character.gameObject.name + "] is busy!";
                    }
                }
            }

            QuickInfoMsg qimsg = new QuickInfoMsg();
            qimsg.command = DialogMsg.Cmd.open;
            qimsg.title = "Nobody Available";
            qimsg.text = "All eligible characters for command are busy right now!! " + debug;
            qimsg.timeout = 4.0f;
            QuickInfoDialog.GetInstance().PutMessage(qimsg);
            return;
        }
#endif
		
        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
            ComputeBloodSalineEffect(ismsg);
			
			// quick hack to turn on Audio
			// Turning this off so that HearbeatGraph can handle a 3Dlocalized audio beep at the monitor,
			// includig pitch modulation with SAO2 level, which would affect other audio if done here.
//			if ( ismsg.InteractName == "PLACE:EKG:COMPLETE" )
//			{
//				hrTimer = Time.time;
//				doHRAudio = true;
//			}
        }

        base.PutMessage(msg);
    }

    public virtual void MedAdminister(MedAdministerMsg msg)
    {
        UnityEngine.Debug.Log("Patient<" + this + "> : MedAdminister : med=" + msg.Med.Name + " : delivery=" + msg.Type.ToString() + " : dosage=" + msg.Dosage);

        if ( Vitals != null )
            Vitals.MedAdminister(msg);
    }

    public void OnGUI()
    {
		base.OnGUI ();
		
		return;
		
        if (showvitals == 0)
            return;

        GUILayout.BeginVertical();

        int w1 = 80;
        int w2 = 80;
        GUILayout.BeginHorizontal();
        GUILayout.Box("HR",GUILayout.Width(w1));
        GUILayout.Box(Convert.ToInt32(HR).ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Box("BP_SYS", GUILayout.Width(w1));
        GUILayout.Box(Convert.ToInt32(BP_SYS).ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Box("BP_DIA", GUILayout.Width(w1));
        GUILayout.Box(Convert.ToInt32(BP_DIA).ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Box("SP", GUILayout.Width(w1));
        GUILayout.Box(Convert.ToInt32(SP).ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Box("TEMP", GUILayout.Width(w1));
        GUILayout.Box(TEMP.ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();
		
        GUILayout.BeginHorizontal();
        GUILayout.Box("RESP", GUILayout.Width(w1));
        GUILayout.Box(RR.ToString(), GUILayout.Width(w2));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public float TIME_IV = 360.0f;
    public float TIME_PR = 240.0f;
    public float TIME_RI = 120.0f;

    public float CHANGE_IV = 15.0f;
	
	// method called by Vitals behavior on status change
	public void VitalsBehaviorChange( List<string> param )
	{
		// parse callback
		InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:FINISHED");		
		// add map
		ismsg.InteractMap = new InteractionMap("FLUID:FINISHED",null,null,null,null,null,null,true);
		ismsg.InteractMap.param = param;
		// add parameters
		ismsg.Params = param;
		// broadcast
		ObjectManager.GetInstance().PutMessage(ismsg);
		// debug
		UnityEngine.Debug.Log("Patient.VitalsBehaviorChange(FLUID:FINISHED) status=<" + param[0] + ">");
	}

	// use number of bags of blood and saline to compute the rate of
	// decline in vital state
	public void ComputeBloodSalineEffect( InteractStatusMsg ismsg )
	{
        // only do something we we had a bag change
        if (ismsg.InteractName == "FLUID:CHANGE")
		{
			// some debug
			UnityEngine.Debug.Log("Patient.ComputeBloodSalineEffect(" + ismsg.InteractName + ")");
			
			// ok figure out params
			if ( ismsg.InteractMap == null || ismsg.InteractMap.param == null && ismsg.InteractMap.param.Count != 3 )
				return;
			
			// parse what to do
			string product="";
			string delivery="";
			string site="";
			string bagname="";
			string command="";
			string counter="";
			foreach( string param in ismsg.InteractMap.param )
			{			
//				UnityEngine.Debug.LogError("Patient.ComputeBloodSalineEffect(FLUID:CHANGE) : param=" + param);
				GetToken(param,"product",ref product);
				GetToken(param,"delivery",ref delivery);
				GetToken(param,"site",ref site);
				GetToken (param,"command",ref command);
				GetToken (param,"counter",ref counter);
			}
			
			if (command == "remove"){
				RemoveBag(site);
				return;	
			}
			
			// if we have an blood or saline IV or PR we are going to have
			// to animate the bag
			IVBagTextureOvertakeScript overtakeScript=null;	
			//
			if ( product != "" && delivery != "" && site != "")
			{
				GameObject hanger=null;
				if ( delivery == "IV" || delivery == "PR" )
				{
					// find the bag and animate
					
					if ( site == "ivleft" )
						hanger = GameObject.Find("prop_ivHangerCeilingLeft");
					if ( site == "ivright" )
						hanger = GameObject.Find("prop_ivHangerCeilingRight");
				}	
				if ( delivery == "RI") // even though animation isnt important here,
					// finding the bag later is.
				{
					// find the bag and animate
					if ( site == "ivleft" )
						hanger = GameObject.Find("prop_ivFluidWarmer01_door2"); // temporarily use the doors as parents
					if ( site == "ivright" )
						hanger = GameObject.Find("prop_ivFluidWarmer01_door1");
				}
						
				if ( hanger != null )
				{
					// get the UV anim script on the bag
					// there might still be an empty bag hanging here, so we need to check that the overtake amount is 0
					foreach (IVBagTextureOvertakeScript ots in hanger.GetComponentsInChildren<IVBagTextureOvertakeScript>(false)){
						if (ots.overtake == 0){
							overtakeScript = ots;
							break;
						}
					}
					if (overtakeScript!= null)
						bagname = overtakeScript.name;
				}

				// update appropriate counters for assessment, number of each type delivered
				if (counter == "Patient.%UnitsBlood") UnitsBlood++;
				if (counter == "Patient.%UnitsPRBC") UnitsPRBC++;
				if (counter == "Patient.%UnitsPlatelets") UnitsPlatelets++;
				if (counter == "Patient.%UnitsPlasma") UnitsPlasma++;
				if (counter == "Patient.%UnitsSaline") UnitsSaline++;
				if (counter == "Patient.%UnitsCrystal") UnitsCrystal++;
				if (counter == "Patient.%UnitsRingers") UnitsRingers++;
				if (counter == "Patient.%UnitsHypertonic") UnitsHypertonic++;
			}
			else
			{
				// error, return here
				UnityEngine.Debug.LogError("Patient.ComputeBloodSalineEffect(FLUID:CHANGE) : param missing!");
				return;
			}
					
			// Check for bag change, add or remove a bag
			VitalsBehavior behavior = AddBag(product, delivery);	

		
			// add parameters if we're ok
			if ( behavior != null )
			{
				// add the site to the name so we can find it uniquely if we have to remove it on order
				behavior.name += site;
				behavior.CallbackParams = new List<string>();
				behavior.CallbackParams.Add("product=\"" + product+"\""); // quotes are needed to distinguish string values
				behavior.CallbackParams.Add("delivery=\"" + delivery+"\"");
				behavior.CallbackParams.Add("site=\"" + site+"\"");
				behavior.CallbackParams.Add("bagname=\"" + bagname+"\""); // so i can remove it!
				behavior.CallbackParams.Add("counter=\"" + counter+"\""); 
				
			}
		
			// we're done with add/remove, now add a callback the behavior to animate the bag
			if ( behavior != null && overtakeScript != null )
			{
				behavior.AddCallback(overtakeScript.SetPercentage);	
			}
		}
    }
	
	public VitalsBehavior AddBag( string product, string delivery )
	{
		if ( product == "bloodbag" )
		{
			if ( totalBlood < maxBlood )
			{				
				totalBlood++;
				string type = delivery + product.Replace("bag","");
				VitalsBehavior behavior = VitalsBehaviorManager.GetInstance().AddBehavior(type);
				return behavior;
			}
		}
		if ( product == "salinebag" )
		{
			if ( totalSaline < maxSaline )
			{				
				totalSaline++;
				string type = delivery + product.Replace("bag","");
				VitalsBehavior behavior = VitalsBehaviorManager.GetInstance().AddBehavior(type);
				return behavior;
			}
		}
		return null;
	}
	
	public void RemoveBag( string site)
	{
		VitalsBehavior behavior = VitalsBehaviorManager.GetInstance().RemoveBehaviorContaining(site);
		
		if (behavior == null){
			UnityEngine.Debug.LogWarning("Patient RemoveBag Found no vitals behavior to remove from "+site);
			return;
		}
		
		List<string> _params = new List<string>();						
		_params.Add("name=" + behavior.name);
		_params.Add("status=removed");
		// add parameters coming from caller
		if ( behavior.CallbackParams != null )
		{
			foreach( string param in behavior.CallbackParams)
				_params.Add(param);
		}
		// do callback
		InteractStatusMsg ismsg = new InteractStatusMsg("FLUID:FINISHED");		
		// add map
		ismsg.InteractMap = new InteractionMap("FLUID:FINISHED",null,null,null,null,null,null,true);
		ismsg.InteractMap.param = _params;
		// add parameters
		ismsg.Params = _params;
		// broadcast
		ObjectManager.GetInstance().PutMessage(ismsg);
		// There is a script "FLUID:FINISHED" on the blood fridge that processes this
	}
	
	public void OrderFluids( string name, int bloodDrip, int bloodPressure, int bloodRapid, int salineDrip, int salinePressure, int salineRapid)
	{
		InteractMsg imsg;
		
		if ( bloodDrip > 0 )
		{
			// ORDER:BLOOD:1:IV
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:BLOOD:" + bloodDrip.ToString() + "IV");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER			
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + bloodDrip);
			imsg.map.param.Add("delivery=\"IV\"");
			imsg.map.param.Add("product=\"bloodbag\"");
			imsg.map.param.Add("affect=\"Patient.%BloodbagsIV\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
		if ( bloodPressure > 0 )
		{
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:BLOOD:" + bloodPressure.ToString() + "PR");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER			
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + bloodPressure);
			imsg.map.param.Add("delivery=\"PR\"");
			imsg.map.param.Add("product=\"bloodbag\"");
			imsg.map.param.Add("affect=\"Patient.%BloodbagsPR\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
		if ( bloodRapid > 0 )
		{
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:BLOOD:" + bloodRapid.ToString() + "RI");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + bloodRapid);
			imsg.map.param.Add("delivery=\"RI\"");
			imsg.map.param.Add("product=\"bloodbag\"");
			imsg.map.param.Add("affect=\"Patient.%BloodbagsRI\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
		if ( salineDrip > 0 )
		{
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:SALINE:" + salineDrip.ToString() + "IV");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + salineDrip);
			imsg.map.param.Add("delivery=\"IV\"");
			imsg.map.param.Add("product=\"salinebag\"");
			imsg.map.param.Add("affect=\"Patient.%SalinebagsIV\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
		if ( salinePressure > 0 )
		{
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:SALINE:" + salinePressure.ToString() + "PR");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + salinePressure);
			imsg.map.param.Add("delivery=\"PR\"");
			imsg.map.param.Add("product=\"salinebag\"");
			imsg.map.param.Add("affect=\"Patient.%SalinebagsPR\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
		if ( salineRapid > 0 )
		{
			InteractStatusMsg ismsg = new InteractStatusMsg("ORDER:SALINE:" + salineRapid.ToString() + "PR");
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(ismsg);
#if LATER
			imsg = new InteractMsg(null,"ORDER:BLOOD:SCRIPT",true);
			imsg.gameObject = name;
			imsg.map.param = new List<string>();
			imsg.map.param.Add("units=" + salineRapid);
			imsg.map.param.Add("delivery=\"RI\"");
			imsg.map.param.Add("product=\"salinebag\"");
			imsg.map.param.Add("affect=\"Patient.%SalineBagsRI\"");
			// send to primary nurse
			ObjectManager.GetInstance().GetBaseObject(name).PutMessage(imsg);
#endif
		}
	}
	
	
    public bool GetToken(string arg, string key, ref string value)
    {
        string[] args = arg.Split(' ');
        for (int i = 0; i < args.Length; i++)
        {
            string[] keyvalue = args[i].Split('=');
            if (keyvalue.Length == 2)
            {
                if (keyvalue[0] == key)
                {
                    value = keyvalue[1];
					// strip off quotes
					value = value.Replace("\"","");
                    return true;
                }
            }
        }
        return false;
    }
}


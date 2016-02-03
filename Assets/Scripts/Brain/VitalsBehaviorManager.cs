//#define DEBUG_VITALS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


/*  How does this system work ?
 * Patient Vitals are currently just independent scalar values. In a more sophisticated model, they may begin to effect
 * one another, perhaps through new types of behaviors or through some hardcoded model added here?
 * 
 * The set of currently active behavior act to increase or decrease a patient Vital value over time.
 * 
 * For each vital represented, we have 
 * 
 * HR;					This value is used to build the behavior mod, based on type:
 * type		absolute 	heartbeatMod = HR	// values are absolute increment/decrement values -1 means ignore, so don't use that!
 *      	target,     range=HR - patient.HR, InitRange(range), InitStop(HR) // values are target values from current Patient vitals
 *       	range       heartbeatMod = HR / timeOfEffect // values are ranges to change
 *  
 * heartbeatModStopAt = STOP_HR; // this gets called for all types, though it should be ignored for target -1 = don't stop
 * 
 * timeOfEffect how long the effect will last, copied into duration then decremented every update.  effect removed on 0
 * 	timeOfEffect currently has NO DEFAULT
 * 
 * Stackable means behavior can be active multiple times at once.
 * 
 * UpdateCallback(float) will get called every update with (timeOfEffect/duration)
 * OnCreateCallback  NOT CURRENTLY IMPLEMENTED
 * OnRemoveCallback  This is hardcoded to look for Patient.VitalsBehaviorChange and call the StateChange callback, which something must set...
 * 
 *  createEffects  List of conditions and effectes to create when those conditions are met:
 * 		IMMEDIATE  ensures that the named behavior is running when this one starts, continuously if endOnEffect=false
 * 		GREATER		as above, but won't trigger till specified condition is met
 * 		LESS		ditto
 * 		ONADD		seems identical to IMEEDIATE and EndOnEffect for create
 * 		ONREMOVE    create the named behavior when this one exists due to timeOfEffect expiring, not when removed by force.
 * 					use createEffect ONREMOVE to chain behaviors together in progressive sequences
 * 
 * 		endOnEffect in the EffectLogic means, once this create gets done, remove the create command, i.e. one shot.
 * 
 *  removeEffects   List of conditions and effectes to create when those conditions are met:
 * 		IMMEDIATE  if the named behavior ever gets added, remove it, once if endOnEffect=true or always if endOnEffect=false
 * 		GREATER		same as above, but only when the conditional is true
 * 		LESS
 * 		ONADD		if the named behavior is running when this one starts, remove it, but allow it to start up later.
 * 		ONREMOVE	remove named behavior when this behavior timeOfEffect expires
 * 
    // Remove effects
    public List<VBEffectLogic> removeEffects;
    protected List<VBEffectLogic> removeRemoved;
 * 
 */ 



public class VitalsBehaviorManager : BaseObject
{
    static VitalsBehaviorManager instance;

    public TextAsset dataFile;
    public bool displayDebug = false;
	public bool createLog = false;
    public bool startAsDying = false;
    // List of all behaviors in the game
    VitalsBehavior[] library;

    // List of current behaviors in play
    List<VitalsBehavior> behaviors;
    float timer;
	
	// State change callback
	public delegate void Callback( List<string> Params );
	public Callback StateChange;	

    Patient patient;

    ObjectManager objMgr;
	
	private bool addingByTrigger = false;
	private bool sendingTrigger = false;
	
	// patient hi/low vitals conditions (we should expose these as config variables)
	// had to make these private to get rid of the bad overrides in the scene.
	 float HR_HI = 170;
	 float HR_LO = 10;	
	 float BPSYS_HI = 99999;
	 float BPSYS_LO = 55;	
	 float BPDIA_HI = 99999;
	 float BPDIA_LO = 35;	
	 float SP_HI = 99999;
	 float SP_LO = 80;	
	 float TEMP_HI = 130;
	 float TEMP_LO = 70;
	 float RESP_HI = 44;
	 float RESP_LO = 5;
	
	
	// current vitals and rates of change, used to predict time of death
	float hbTotal = 0;
    float diasTotal = 0;
    float sysTotal = 0;
    float spTotal = 0;
    float tempTotal = 0;
	float respTotal = 0;
	float hbMaxEffect = 0;  // MaxEffect, for limiting projections based on timeOfEffect or Stop limits;
	float diasMaxEffect = 0;
	float sysMaxEffect = 0;
	float spMaxEffect = 0;
	float tempMaxEffect = 0;
	float respMaxEffect = 0;
	float hbAvgChange = 0; // a blended rolling average to drive the UI gauges
    float diasAvgChange = 0;
    float sysAvgChange = 0;
    float spAvgChange = 0;
    float tempAvgChange = 0;
	float respAvgChange = 0;
    float hbCurr = 0;
    float sysCurr = 0;
    float diasCurr = 0;
    float spCurr = 0;
    float tempCurr = 0;
	float respCurr = 0;

    public static VitalsBehaviorManager GetInstance()
    {
        return instance;
    }

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            enabled = false;
		
		
    }
	
	void Start()
	{
		Init();
	}

	void Init()
	{
        if (dataFile == null)
        {
            enabled = false;
            return;
        }
        // Load library
        Load();

        behaviors = new List<VitalsBehavior>();
        timer = 0;

        patient = FindObjectOfType(typeof(Patient)) as Patient;

        objMgr = ObjectManager.GetInstance();
        if (objMgr != null)
            objMgr.RegisterObject(this);

        if(startAsDying)
            AddBehavior("Dying");

		if ( CaseConfiguratorMgr.GetInstance().Data != null )
		{
			logFileName = (CaseConfiguratorMgr.GetInstance ().Data.loadedCase+"_VitalsLog_"+System.DateTime.Now+".txt").Replace("/","-");
			logFileName = (CaseConfiguratorMgr.GetInstance ().Data.loadedCase+"_VitalsLog.txt").Replace(" ","-");
		}
	}

    void Load()
    {
        // Grab the data
        Serializer<VitalsBehavior[]> serializer = new Serializer<VitalsBehavior[]>();
        library = serializer.Load("XML/BehaviorData/" + dataFile.name);
    }
	
	public void AddToLibrary(VitalsBehavior behavior){
		// replace or add new behavior
    	if (FindBehaviorInLibrary(behavior.name)!= null){ // was ExistInBehaviors, behaviors was empty.
			for (int i = 0;i<library.Length; i++){
				if (library[i].name == behavior.name){
					library[i] = behavior;
					break;
				}
			}
		}
		else
		{
			VitalsBehavior[] newLib = new VitalsBehavior[library.Length+1];
			for (int i = 0;i<library.Length; i++)
				newLib[i] = library[i];
			newLib[library.Length] = behavior;
			library = newLib;
		}
	}

    public VitalsBehavior AddBehavior(VitalsBehavior behavior)
    {
        if (!behavior.stackable && ExistInBehaviors(behavior.name))
            return null;
		actionString += "Add "+behavior.name+":";
		VitalsBehaviorLogItem logItem = new VitalsBehaviorLogItem(Time.time,"ADD:"+this.name);
		LogMgr.GetInstance ().GetCurrent().Add (logItem);
		if (!addingByTrigger && behavior.triggered != null && behavior.triggered != ""){
			// emit the 'triggered' message if it wasn't the cause of this add
			InteractStatusMsg msg = new InteractStatusMsg( behavior.triggered );
			sendingTrigger = true; // flag so we don't respond to our own message
            Brain.GetInstance().PutMessage(msg);
			sendingTrigger = false;
		}
		
		VitalsBehavior newBehavior = behavior.Copy().Init(patient);
        behaviors.Add(newBehavior);
		return newBehavior;
    }

    public VitalsBehavior AddBehavior(string name)
    {
#if DEBUG_VITALS
		UnityEngine.Debug.Log("VitalsBehaviorManager.AddBehavior(" + name + ")");
#endif
		if ( library != null )
		{
        	foreach (VitalsBehavior behavior in library)
        	{
	            if (behavior.name.ToLower() == name.ToLower())
            	{
	                return AddBehavior(behavior);
            	}
        	}
		}
#if DEBUG_VITALS
		UnityEngine.Debug.Log("VitalsBehaviorManager.AddBehavior(" + name + ") error, can't find it!");
#endif
		return null;
    }

    public bool RemoveBehavior(string name)
    {
        foreach (VitalsBehavior behavior in behaviors)
        {
            if (behavior.name.ToLower() == name.ToLower())
            {
				actionString += "Rem "+behavior.name+":";
                behaviors.Remove(behavior);
				VitalsBehaviorLogItem logItem = new VitalsBehaviorLogItem(Time.time,"REMOVE:"+this.name);
				LogMgr.GetInstance ().GetCurrent().Add (logItem);
                return true;
            }
        }
		return false;
    }
	
	public VitalsBehavior RemoveBehaviorContaining(string name)
    {
        foreach (VitalsBehavior behavior in behaviors)
        {
            if (behavior.name.ToLower().Contains (name.ToLower()))
            {
                behaviors.Remove(behavior);
				VitalsBehaviorLogItem logItem = new VitalsBehaviorLogItem(Time.time,"REMOVE:"+this.name);
				LogMgr.GetInstance ().GetCurrent().Add (logItem);
                return behavior;
            }
        }
		return null;
    }

    public VitalsBehavior FindBehaviorInLibrary(string name)
    {
		if ( library == null )
			return null;
		
        foreach (VitalsBehavior behavior in library)
        {
            if (behavior.name.ToLower() == name.ToLower())
                return behavior;
        }
        return null;
    }

    public bool ExistInBehaviors(string name)
    {
        foreach (VitalsBehavior behavior in behaviors)
        {
            if (behavior.name.ToLower() == name.ToLower())
                return true;
        }
        return false;
    }

    // Phases: Goes through all VitalsBehaviors and triggers these in the following order
    // Create
    // Ongoing
    // Remove
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
			if (patient == null)
				patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
			if (patient == null) return;
            // Create phase

			if (createLog) LogVitals();

            List<VitalsBehavior> createList = new List<VitalsBehavior>();
            foreach (VitalsBehavior behavior in behaviors)
            {
				// do behavior callbacks
				behavior.Update(); // this ONLY does the callbacks, nothing else.
				
				// create
                foreach (VitalsBehavior.VBEffectLogic el in behavior.createEffects)
                {					
                    // Check if the condition has been met
                    if ((el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONADD && behavior.justAdded) 
						|| el.logic.Check(patient))
                    {
                        // Behavior to be added. Check that it is in the library
                        VitalsBehavior lookForTest = FindBehaviorInLibrary(el.behaviorName);
                        //if (library.Contains(el.Behavior))
                        if(lookForTest != null)
                        {
                            // Check if the behavior is unique
                            if (!lookForTest.stackable)
                            {
                                if (ExistInBehaviors(lookForTest.name))
                                {
                                    // Already in the system and not stackable. Move on
                                    continue;
                                }
                            }

                            // Add behavior
                            //behaviors.Add(FindBehaviorInLibrary(el.behaviorLookup).Copy()); // Send a copy in
                            createList.Add(FindBehaviorInLibrary(el.behaviorName).Copy().Init(patient));


                            // Remove on effect
                            if (el.endOnEffect)
                            {
                                behavior.AddToRemoveCreated(el);
                            }
                        }
                    }
					if (el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONADD && !behavior.justAdded)
						behavior.AddToRemoveCreated(el); // no point in checking this every frame, it's done
                }
            }
            foreach (VitalsBehavior behavior in createList){
                AddBehavior(behavior);
			}
			createList.Clear();

            // Remove phase
            // For clearing behaviors after we know what to remove. Can't do it while in the -behaviors- iteration
            List<VitalsBehavior> removeList = new List<VitalsBehavior>();
            foreach (VitalsBehavior behavior in behaviors)
            {
                foreach (VitalsBehavior.VBEffectLogic el in behavior.removeEffects)
                {
                    // Check if the condition has been met
					if ((el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONADD && behavior.justAdded) 
					    || el.logic.Check(patient))
                    {
                        // Remove the condition.
                        foreach (VitalsBehavior check in behaviors)
                        {
                            if (check.name == el.behaviorName)
                            {
                                removeList.Add(check);
                                if (el.endOnEffect)
                                    behavior.AddToRemoveRemoved(el);
                                break;
                            }
                        }
                    }
					if (el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONADD && !behavior.justAdded)
						behavior.AddToRemoveRemoved(el); // no point in checking this every frame, it's done
                }

                // Clean up
                behavior.ClearUsedEffects();
				behavior.justAdded = false;
            }
            // Clean up behaviors
            foreach (VitalsBehavior behavior in removeList){
                RemoveBehavior(behavior.name);
			}
            removeList.Clear();

            hbTotal = 0;
            diasTotal = 0;
            sysTotal = 0;
            spTotal = 0;
            tempTotal = 0;
			respTotal = 0;
            hbMaxEffect = 0;
            diasMaxEffect = 0;
            sysMaxEffect = 0;
            spMaxEffect = 0;
            tempMaxEffect = 0;
			respMaxEffect = 0;
            hbCurr = patient.HR;
            diasCurr = patient.BP_DIA;
            sysCurr = patient.BP_SYS;
            spCurr = patient.SP;
            tempCurr = patient.TEMP;
			respCurr = patient.RR;

            // Ongoing effects on Patient
            foreach (VitalsBehavior behavior in behaviors)
            {
				// recalcualte time to target as time goes by...
				if (behavior.type == VitalsBehavior.Type.target && behavior.effectUntilTarget){
					behavior.timeOfEffect = 0;
					if (behavior.heartbeatMod != -1 && behavior.heartbeatMod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.HR - patient.HR)/behavior.heartbeatMod,behavior.timeOfEffect);
					if (behavior.diastolicMod != -1 && behavior.diastolicMod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.BPDIA - patient.BP_DIA)/behavior.diastolicMod,behavior.timeOfEffect);
					if (behavior.systolicMod != -1 && behavior.systolicMod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.BPSYS - patient.BP_SYS)/behavior.systolicMod,behavior.timeOfEffect);
					if (behavior.spo2Mod != -1 && behavior.spo2Mod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.SP - patient.SP)/behavior.spo2Mod,behavior.timeOfEffect);
					if (behavior.tempMod != -1 && behavior.tempMod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.TEMP - patient.TEMP)/behavior.tempMod,behavior.timeOfEffect);
					if (behavior.respirationMod != -1 && behavior.respirationMod != 0)
						behavior.timeOfEffect = Mathf.Max((behavior.RESP - patient.RR)/behavior.respirationMod,behavior.timeOfEffect);
				}

				float contrib=0;
                /// HEARTBEAT
                // Negative modifier
                if (behavior.heartbeatMod != -1)
                {
					
                    if (behavior.heartbeatModStopAt != -1)
                    {
                        if (behavior.heartbeatMod < 0)
                        {
                            if (hbCurr > behavior.heartbeatModStopAt){
                                hbTotal += behavior.heartbeatMod;
								contrib = behavior.heartbeatModStopAt - hbCurr;
								if (contrib < behavior.heartbeatMod*behavior.timeOfEffect)
									contrib = behavior.heartbeatMod*behavior.timeOfEffect;
							}
                        }
                        else // Positive
                        {
                            if (hbCurr < behavior.heartbeatModStopAt){
                                hbTotal += behavior.heartbeatMod;
								contrib = behavior.heartbeatModStopAt - hbCurr;
								if (contrib > behavior.heartbeatMod*behavior.timeOfEffect)
									contrib = behavior.heartbeatMod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        hbTotal += behavior.heartbeatMod;
						contrib = behavior.heartbeatMod*behavior.timeOfEffect;
					}
					hbMaxEffect += contrib;
                }

                /// DIASTOLIC
                // Negative modifier
                if (behavior.diastolicMod != -1)
                {
                    if (behavior.diastolicModStopAt != -1)
                    {
                        if (behavior.diastolicMod < 0)
                        {
                            if (diasCurr > behavior.diastolicModStopAt){
                                diasTotal += behavior.diastolicMod;
								contrib = behavior.diastolicModStopAt - diasCurr;
								if (contrib < behavior.diastolicMod*behavior.timeOfEffect)
									contrib = behavior.diastolicMod*behavior.timeOfEffect;								
							}
                        }
                        else // Positive
                        {
                            if (diasCurr < behavior.diastolicModStopAt){
                                diasTotal += behavior.diastolicMod;
								contrib = behavior.diastolicModStopAt - diasCurr;
								if (contrib > behavior.diastolicMod*behavior.timeOfEffect)
									contrib = behavior.diastolicMod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        diasTotal += behavior.diastolicMod;
						contrib = behavior.diastolicMod*behavior.timeOfEffect;
					}
					diasMaxEffect += contrib;
                }

                /// SYSTOLIC
                // Negative modifier
                if (behavior.systolicMod != -1)
                {
                    if (behavior.systolicModStopAt != -1)
                    {
                        if (behavior.systolicMod < 0)
                        {
                            if (sysCurr > behavior.systolicModStopAt){
                                sysTotal += behavior.systolicMod;
								contrib = behavior.systolicModStopAt - sysCurr;
								if (contrib < behavior.systolicMod*behavior.timeOfEffect)
									contrib = behavior.systolicMod*behavior.timeOfEffect;	
							}
                        }
                        else // Positive
                        {
                            if (sysCurr < behavior.systolicModStopAt){
                                sysTotal += behavior.systolicMod;
								contrib = behavior.systolicModStopAt - sysCurr;
								if (contrib > behavior.systolicMod*behavior.timeOfEffect)
									contrib = behavior.systolicMod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        sysTotal += behavior.systolicMod;
						contrib = behavior.systolicMod*behavior.timeOfEffect;
					}
					sysMaxEffect += contrib;
                }

                /// SPO2
                // Negative modifier
                if (behavior.spo2Mod != -1)
                {
                    if (behavior.spo2ModStopAt != -1)
                    {
                        if (behavior.spo2Mod < 0)
                        {
                            if (spCurr > behavior.spo2ModStopAt){
                                spTotal += behavior.spo2Mod;
								contrib = behavior.spo2ModStopAt - spCurr;
								if (contrib < behavior.spo2Mod*behavior.timeOfEffect)
									contrib = behavior.spo2Mod*behavior.timeOfEffect;
							}
                        }
                        else // Positive
                        {
                            if (spCurr < behavior.spo2ModStopAt){
                                spTotal += behavior.spo2Mod;
								contrib = behavior.spo2ModStopAt - spCurr;
								if (contrib > behavior.spo2Mod*behavior.timeOfEffect)
									contrib = behavior.spo2Mod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        spTotal += behavior.spo2Mod;
						contrib = behavior.spo2Mod*behavior.timeOfEffect;
					}
					spMaxEffect += contrib;
                }

                /// TEMP
                // Negative modifier
                if (behavior.tempMod != -1)
                {
                    if (behavior.tempModStopAt != -1)
                    {
                        if (behavior.tempMod < 0)
                        {
                            if (tempCurr > behavior.tempModStopAt){
                                tempTotal += behavior.tempMod;
								contrib = behavior.tempModStopAt - tempCurr;
								if (contrib < behavior.tempMod*behavior.timeOfEffect)
									contrib = behavior.tempMod*behavior.timeOfEffect;
							}
                        }
                        else // Positive
                        {
                            if (tempCurr < behavior.tempModStopAt){
                                tempTotal += behavior.tempMod;
								contrib = behavior.tempModStopAt - tempCurr;
								if (contrib > behavior.tempMod*behavior.timeOfEffect)
									contrib = behavior.tempMod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        tempTotal += behavior.tempMod;
						contrib = behavior.tempMod*behavior.timeOfEffect;
					}
					tempMaxEffect += contrib;
                }
				
                /// RESPIRATION
                // Negative modifier
                if (behavior.respirationMod != -1)
                {
					
                    if (behavior.respirationModStopAt != -1)
                    {
                        if (behavior.respirationMod < 0)
                        {
                            if (respCurr > behavior.respirationModStopAt){
                                respTotal += behavior.respirationMod;
								contrib = behavior.respirationModStopAt - respCurr;
								if (contrib < behavior.respirationMod*behavior.timeOfEffect)
									contrib = behavior.respirationMod*behavior.timeOfEffect;
							}
                        }
                        else // Positive
                        {
                            if (respCurr < behavior.respirationModStopAt){
                                respTotal += behavior.respirationMod;
								contrib = behavior.respirationModStopAt - respCurr;
								if (contrib > behavior.respirationMod*behavior.timeOfEffect)
									contrib = behavior.respirationMod*behavior.timeOfEffect;
							}
                        }
                    }
                    else{
                        respTotal += behavior.respirationMod;
						contrib = behavior.respirationMod*behavior.timeOfEffect;
					}
					respMaxEffect += contrib;
                }				

                // Clear timed out effects
				behavior.timeOfEffect -= 1; // we are only processing 1 sec worth here... // timer;
				// Calcuate percentage complete based on time
				behavior.percentage = (behavior.duration-behavior.timeOfEffect)/behavior.duration;
                //UnityEngine.Debug.Log("Effect name: " + behavior.name + " : time=" + behavior.timeOfEffect + " : hr=" + patient.HR + " : hrMod=" + behavior.heartbeatMod);
                if (behavior.timeOfEffect <= 0)
                {
					// remove effect
                    removeList.Add(behavior);
					
					// see about and chained add or remove behaviors
                	foreach (VitalsBehavior.VBEffectLogic el in behavior.createEffects)
					{
	                    if (el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONREMOVE) 
	                    {
	                        // Behavior to be added. Check that it is in the library
	                        VitalsBehavior lookForTest = FindBehaviorInLibrary(el.behaviorName);
	                        //if (library.Contains(el.Behavior))
	                        if(lookForTest != null)
	                        {
	                            // Check if the behavior is unique
	                            if (!lookForTest.stackable)
	                            {
	                                if (ExistInBehaviors(lookForTest.name))
	                                {
	                                    // Already in the system and not stackable. Move on
	                                    continue;
	                                }
	                            }
	
	                            // Add behavior
	                            //behaviors.Add(FindBehaviorInLibrary(el.behaviorLookup).Copy()); // Send a copy in
	                            createList.Add(FindBehaviorInLibrary(el.behaviorName).Copy().Init(patient));
	
	                            // Remove on effect
	                            if (el.endOnEffect)
	                            {
	                                behavior.AddToRemoveCreated(el);
	                            }
	                        }
	                    }
					}
	                foreach (VitalsBehavior.VBEffectLogic el in behavior.removeEffects)
	                {
	                    // Check if the condition has been met
	                    if (el.logic.type==VitalsBehavior.VBLogic.VBLogicType.ONREMOVE)
	                    {
	                        // Remove the condition.
	                        foreach (VitalsBehavior check in behaviors)
	                        {
	                            if (check.name == el.behaviorName)
	                            {
	                                removeList.Add(check);
	                            }
	                        }
	                    }
	                }
				
					// do callback
					if ( StateChange != null )
					{
						// WORKAROUND for behavior removal callbacks:
						// we don't always want to call the same routine, so we will add a
						// reflection based element in the behavior, such as 
						// OnRemoveCallback="Patient.VitalsBehaviorChange" to be called from here.
						
						if (behavior.OnRemoveCallback == "Patient.VitalsBehaviorChange"){
							// do basic params
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
							StateChange(_params);
						}
					}
                }
            }
            // Clean up behaviors
			foreach (VitalsBehavior behavior in createList)
                AddBehavior(behavior);
            foreach (VitalsBehavior behavior in removeList)
                RemoveBehavior(behavior.name);

            // Effect change!
            patient.HR = hbCurr + hbTotal;
            patient.BP_DIA = diasCurr + diasTotal;
            patient.BP_SYS = sysCurr + sysTotal;
            patient.SP = spCurr + spTotal;
            patient.TEMP = tempCurr + tempTotal;
			patient.RR = respCurr + respTotal;
			
            timer -= 1f;
        }
    }

	public void ProcessTimeLapsed(float amount){
		// add an amount of time to laps on the vitals. Since this time is processed at 1 sec/update,
 
		timer += amount;
		while (timer > 1) // lets try calling update repeatedly to get the full increment processed...
			Update ();
	}
	
	// i need to add checking both directions
	public float TimeToDeath( string type, bool smooth=true )
	{
		
		switch( type )
		{
		case "HR":
			return TimeToTarget("HR",smooth);
		case "SP":
			return TimeToTarget("SP",smooth);
		case "BPSYS":
			return TimeToTarget("BPSYS",smooth);
		}
		return 0.0f;
	}

	public float TimeToTarget( string type, bool smooth )
	{
		float maxTime = 1200;
		float seconds = maxTime; // max length of scenario, if vital is not a threat
		float currentValue = 0;
		float rate = 0;
		float max = 1000;
		float min = 0;
		float maxEffect = 0;
		
		// blend into the averages for the smoothed GUI indicators
		// blending in here because the vitals only update at 1hz
		float blendRate = 40.0f; // basically 40 frames to reach blend value
		
		switch( type )
		{
		case "HR":
				currentValue = hbCurr;
				max = HR_HI;
				min = HR_LO;
				maxEffect = hbMaxEffect;
				if (smooth){
					hbAvgChange = (hbTotal + hbAvgChange*blendRate)/(blendRate+1);
					rate = hbAvgChange; // blended average
				}
				else
					rate = hbTotal; // instantaneous rate of change
			break;
		case "BPDIA":
				currentValue = diasCurr;
				max = BPDIA_HI;
				min = BPDIA_LO;
				maxEffect = diasMaxEffect;
				if (smooth){
					diasAvgChange = (diasTotal + diasAvgChange*blendRate)/(blendRate+1);					
					rate = diasAvgChange; // blended average
				}
				else
					rate = diasTotal; // instantaneous rate of change
			break;
		case "BPSYS":
				currentValue = sysCurr;
				max = BPSYS_HI;
				min = BPSYS_LO;
				maxEffect = sysMaxEffect;
				if (smooth){
					sysAvgChange = (sysTotal + sysAvgChange*blendRate)/(blendRate+1);
					rate = sysAvgChange; // blended average
				}
				else
					rate = sysTotal; // instantaneous rate of change
			break;
		case "SP":
				currentValue = spCurr;
				max = SP_HI;
				min = SP_LO;
				maxEffect = spMaxEffect;
				if (smooth){
					spAvgChange = (spTotal + spAvgChange*blendRate)/(blendRate+1);
					rate = spAvgChange; // blended average
				}
				else
					rate = spTotal; // instantaneous rate of change
			break;
		case "TEMP":
				currentValue = tempCurr;
				max = TEMP_HI;
				min = TEMP_LO;
				maxEffect = tempMaxEffect;
				if (smooth){
					tempAvgChange = (tempTotal + tempAvgChange*blendRate)/(blendRate+1);
					rate = tempAvgChange; // blended average
				}
				else
					rate = tempTotal; // instantaneous rate of change
			break;
		case "RESP":
				currentValue = respCurr;
				max = RESP_HI;
				min = RESP_LO;
				maxEffect = respMaxEffect;
				if (smooth){
					respAvgChange = (respTotal + respAvgChange*blendRate)/(blendRate+1);
					rate = respAvgChange; // blended average
				}
				else
					rate = respTotal; // instantaneous rate of change
			break;
		}
		
		if (rate > 0 && maxEffect*1.1f >= (max-currentValue)){ // we might need a little tolerance here...
			if (currentValue < max)
				seconds = (max - currentValue)/rate;
			else 
				seconds = 0; // already exceeded limit.
		}
		else if (rate < 0 && maxEffect*1.1f <= (min-currentValue)) // might need a little tolerance
		{
			if (currentValue > min)
				seconds = (min - currentValue)/rate; // because rate is negative
			else
				seconds = 0; // already exceeded limit			
		}
		
		if (seconds > maxTime) seconds = maxTime;
		
		return seconds;
	}

    public override void PutMessage(GameMsg msg)
    {
		if (sendingTrigger) return; // don't respond to our own 'triggered' message
		string text = "";
        //TaskCompleteMsg tcMsg = msg as TaskCompleteMsg;
        //if (tcMsg != null)
        //    text = tcMsg.TaskName;

        InteractStatusMsg itMsg = msg as InteractStatusMsg;
        if (itMsg != null)
            text = itMsg.InteractName;

        if (text.Length > 0)
        {
            // Check each behavior in the library if it gets triggered by the message
			if ( library != null )
			{
            	foreach (VitalsBehavior behavior in library)
            	{
	                if (behavior.triggered != null && behavior.triggered.ToLower() == text.ToLower())
                	{
						addingByTrigger = true; // flip this so the trigger is not emittted
	                    AddBehavior(behavior);
						actionString += "Trig "+text+":";
						addingByTrigger = false;
                	}
            	}
			}
        }
    }
	
	int Yoffset( int value )
	{
		return value-25;
	}

    void OnGUI()
    {
		if (displayDebug)
        {
            foreach (VitalsBehavior behavior in behaviors)
            {
                GUILayout.Label(behavior.name);
            }
        }
    }
#if UNITY_EDITOR	
	public void OnInspectorGUI(){
		// add some stuff to the inspector window for currently running vitals
		GUILayout.Label("Running Vitals Behaviors");
		Color old = GUI.color;
		foreach (VitalsBehavior behavior in behaviors)
		{
			GUI.color = Color.cyan;
			GUILayout.Label(behavior.name+" for "+(int)behavior.timeOfEffect+"s "+behavior.percentage.ToString("F2")+"%");
			if (behavior.heartbeatMod != -1 && behavior.heartbeatMod != 0 )
			{
				GUI.color=Color.green;
				GUILayout.Label("  HR"+(int)hbCurr+" ~"+behavior.heartbeatMod.ToString("F4")+" to "+(int)behavior.heartbeatModStopAt);
			}
			if (behavior.spo2Mod != -1  && behavior.spo2Mod != 0 )
			{
				GUI.color=Color.yellow;
				GUILayout.Label("  SP"+(int)spCurr+" ~"+behavior.spo2Mod.ToString("F4")+" to "+(int)behavior.spo2ModStopAt);
			}
			if (behavior.respirationMod != -1 && behavior.respirationMod != 0 )
			{
				GUI.color=Color.yellow;
				GUILayout.Label("  RR"+(int)respCurr+" ~"+behavior.respirationMod.ToString("F4")+" to "+(int)behavior.respirationModStopAt);
			}
			if (behavior.systolicMod != -1 && behavior.systolicMod != 0 )
			{
				GUI.color=Color.red;
				GUILayout.Label("  SY"+(int)sysCurr+" ~"+behavior.systolicMod.ToString("F4")+" to "+(int)behavior.systolicModStopAt);
			}
			if (behavior.diastolicMod != -1 && behavior.diastolicMod != 0 )
			{
				GUI.color=Color.red;
				GUILayout.Label("  SY"+(int)diasCurr+" ~"+behavior.diastolicMod.ToString("F4")+" to "+(int)behavior.diastolicModStopAt);
			}
			GUI.color=Color.gray;
			foreach (VitalsBehavior.VBEffectLogic el in behavior.createEffects){
				GUILayout.Label("  +"+el.behaviorName+" "+el.logic.type.ToString());
			}
			foreach (VitalsBehavior.VBEffectLogic el in behavior.removeEffects){
				GUILayout.Label("  -"+el.behaviorName+" "+el.logic.type.ToString());
			}
		}
		GUI.color = old;
						//GUILayout.ExpandWidth(false));
	}
#endif

	string logFileName = "";
	string chartString = "";
	string actionString = "";
	float lastLogTime = 0;

	void LogVitals(){

		if (Time.time - lastLogTime > 10) {
			lastLogTime = Time.time;
			chartString = "                                                           ";
			chartString = SetMarker(chartString,'R', RESP_LO, respCurr, RESP_HI);
			chartString = SetMarker(chartString,'S', SP_LO, spCurr, 100);
			chartString = SetMarker(chartString,'B', BPSYS_LO, sysCurr, 180);
			chartString = SetMarker(chartString,'H', HR_LO, hbCurr, HR_HI);
			string valText = "H"+(int)hbCurr+" B"+(int)sysCurr+" S"+(int)spCurr+" R"+(int)respCurr+" ";
			string logString = (int)Time.time+":"+ chartString+valText+actionString+"\r\n";
#if UNITY_STANDALONE_WIN
			System.IO.File.AppendAllText(Application.dataPath+"/"+logFileName, logString);
#endif
			actionString = "";
		}
	}
	
	string SetMarker(string input, char marker, float min, float val, float max){
		// used to build a strip chart of the vitals for development
		float percent = (val - min) / (max - min);
		int pos = (int)((input.Length-1) * percent);
		string result = input.Substring (0, pos) + marker + input.Substring (pos + 2);
		return result;
	}
	
	
	




}

[System.Serializable]
public class VitalsBehavior
{
	// original name
    public string name;

    // How long the effect persists
    public float timeOfEffect;
	// Init time of effect
	public float duration;
	// Percentage of effect finished
	public float percentage;

    // Stackable means duplicates can exist
    public bool stackable;

	// keep this effect running until all it's targets have been reached.
	public bool effectUntilTarget;
	
	// update callback....gets called on every update
	public delegate void Callback( float percentage );
 	Callback UpdateCallback;
	
	// have to add a callback this way because it won't serialize (grrr)
	public void AddCallback( Callback callback )
	{
		UpdateCallback += callback;
	}
	
	// list of callback params
	public List<string> CallbackParams=null;
	
	public void Update()
	{
		if ( UpdateCallback != null )
			UpdateCallback(percentage);
	}
	
    // enum
    public enum Type
    {
        absolute=0, // values are absolute increment/decrement values
        target,     // values are target values from current Patient vitals
        range       // values are ranges to change
    }
    public Type type;
	
    // vitals
    public float HR;
    public float BPSYS;
    public float BPDIA;
    public float SP;
    public float TEMP;
	public float RESP;
   
    public float STOP_HR;
    public float STOP_BPSYS;
    public float STOP_BPDIA;
    public float STOP_SP;
    public float STOP_TEMP;
	public float STOP_RESP;

    // Vital stat modifiers
    public float heartbeatMod;
    public float heartbeatModStopAt;
    public float diastolicMod;
    public float diastolicModStopAt;
    public float systolicMod;
    public float systolicModStopAt;
    public float spo2Mod;
    public float spo2ModStopAt;
    public float tempMod;
    public float tempModStopAt;
    public float respirationMod;
    public float respirationModStopAt;
	
	
	// Waveform data
	public string heartbeatType;
	// more to follow?

    ///
    /// bloodLevelMod
    /// painMod;
    /// 
    ///

    // Send message on creation - THIS is currently being used to ADD this Behavior when this message is recieved!
    public string triggered; // <---

    // Create effects
    public List<VBEffectLogic> createEffects;
    protected List<VBEffectLogic> removeCreated;

    // Remove effects
    public List<VBEffectLogic> removeEffects;
    protected List<VBEffectLogic> removeRemoved;
	
	// Callback commands
	public string OnCreateCallback;
	public string OnRemoveCallback;
	public bool justAdded;

    public VitalsBehavior()
    {
        HR = -1;
        BPSYS = -1;
        BPDIA = -1;
        SP = -1;
		TEMP = -1;
		RESP = -1;

        heartbeatMod = -1;
        diastolicMod = -1;
        systolicMod = -1;
        spo2Mod = -1;
        tempMod = -1;
		respirationMod = -1;

        heartbeatModStopAt = -1;
        diastolicModStopAt = -1;
        systolicModStopAt = -1;
        spo2ModStopAt = -1;
        tempModStopAt = -1;
		respirationModStopAt = -1;
		
		duration = 0.0f;
		UpdateCallback = null;
		effectUntilTarget = false;
		
		OnCreateCallback = "";
		OnRemoveCallback = "";
    }

    public VitalsBehavior Init(Patient patient)
    {
        switch (type)
        {
            case Type.absolute:
                InitAbsolute(HR, BPSYS, BPDIA, SP, TEMP, RESP);
                break;
            case Type.range:
                InitRange(timeOfEffect, HR, BPSYS, BPDIA, SP, TEMP, RESP);
                break;
            case Type.target:
                InitTarget(patient, timeOfEffect, HR, BPSYS, BPDIA, SP, TEMP, RESP );
                break;
        }
        InitStop(STOP_HR, STOP_BPSYS, STOP_BPDIA, STOP_SP, TEMP, RESP);
		if (heartbeatType != null && heartbeatType != "")
			patient.HeartbeatType = heartbeatType;
		this.justAdded = true;
		VitalsBehaviorLogItem logItem = new VitalsBehaviorLogItem(Time.time,"ADD:"+this.name);
		LogMgr.GetInstance ().GetCurrent().Add (logItem);
        return this;
    }

    public VitalsBehavior Copy()
    {
        VitalsBehavior vb = new VitalsBehavior();

        vb.name = name;
        vb.timeOfEffect = vb.duration = timeOfEffect;
        vb.stackable = stackable;
		vb.effectUntilTarget = effectUntilTarget;
        vb.type = type;
        vb.HR = HR;
        vb.BPDIA = BPDIA;
        vb.BPSYS = BPSYS;
        vb.SP = SP;
		vb.TEMP = TEMP;
		vb.RESP = RESP;
        vb.STOP_HR = STOP_HR;
        vb.STOP_BPDIA = STOP_BPDIA;
        vb.STOP_BPSYS = STOP_BPSYS;
        vb.STOP_SP = STOP_SP;
		vb.STOP_TEMP = STOP_TEMP;
		vb.STOP_RESP = STOP_RESP;
        vb.heartbeatMod = heartbeatMod;
        vb.heartbeatModStopAt = heartbeatModStopAt;
        vb.diastolicMod = diastolicMod;
        vb.diastolicModStopAt = diastolicModStopAt;
        vb.systolicMod = systolicMod;
        vb.systolicModStopAt = systolicModStopAt;
        vb.spo2Mod = spo2Mod;
        vb.spo2ModStopAt = spo2ModStopAt;
        vb.tempMod = tempMod;
        vb.tempModStopAt = tempModStopAt;
		vb.respirationMod = respirationMod;
		vb.respirationModStopAt = respirationModStopAt;
        vb.triggered = triggered;
		vb.heartbeatType = heartbeatType;
		vb.OnCreateCallback = OnCreateCallback;
		vb.OnRemoveCallback = OnRemoveCallback;
        vb.createEffects = new List<VBEffectLogic>();
        foreach (VBEffectLogic el in createEffects)
            vb.createEffects.Add(el);
        vb.removeEffects = new List<VBEffectLogic>();
        foreach (VBEffectLogic el in removeEffects)
            vb.removeEffects.Add(el);

        return vb;
    }

    public void AddToRemoveCreated(VBEffectLogic el)
    {
        if (removeCreated == null)
            removeCreated = new List<VBEffectLogic>();
        removeCreated.Add(el);
    }

    public void AddToRemoveRemoved(VBEffectLogic el)
    {
        if (removeRemoved == null)
            removeRemoved = new List<VBEffectLogic>();
        removeRemoved.Add(el);
    }

    public void ClearUsedEffects()
    {
        if (removeCreated != null)
        {
            foreach (VBEffectLogic el in removeCreated)
                createEffects.Remove(el);
            removeCreated.Clear();
        }

        if (removeRemoved != null)
        {
            foreach (VBEffectLogic el in removeRemoved)
                removeEffects.Remove(el);
            removeRemoved.Clear();
        }
    }

    // Sets absolute values
    public void InitAbsolute(float HR, float BP_SYS, float BP_DIA, float SP, float TEMP, float RESP)
    {
#if DEBUG_VITALS
        UnityEngine.Debug.Log("VitalsBehavior.InitAbsolute(" + HR + "," + BP_SYS + "," + BP_DIA + "," + SP + "," + TEMP + ")");
#endif

        // just set values
        this.heartbeatMod = HR;
        this.diastolicMod = BP_DIA;
        this.systolicMod = BP_SYS;
        this.spo2Mod = SP;
        this.tempMod = TEMP;
		this.respirationMod = RESP;
    }

    // Sets mods and stops from target.  Uses current patient vitals as reference.
    public void InitTarget(Patient patient, float seconds, float HR, float BP_SYS, float BP_DIA, float SP, float TEMP, float RESP)
    {
#if DEBUG_VITALS
        UnityEngine.Debug.Log("VitalsBehavior.InitTarget(" + seconds + "," + HR + "," + BP_SYS + "," + BP_DIA + "," + SP + ")");
        UnityEngine.Debug.Log("VitalsBehavior.InitTarget() : current =" + patient.HR + "," + patient.BP_SYS + "," + patient.BP_DIA + "," + patient.SP); 
#endif
        if (patient == null)
        {
            UnityEngine.Debug.LogError("VitalsBehavior.InitTarget() : patient == null");
            return;
        }

        float rangeHR = (HR == -1) ? HR : (HR - patient.HR);
        float rangeDIA = (BP_DIA == -1) ? BP_DIA : (BP_DIA - patient.BP_DIA);
        float rangeSYS = (BP_SYS == -1) ? BP_SYS : (BP_SYS - patient.BP_SYS);
        float rangeSP = (SP == -1) ? SP : (SP - patient.SP);
        float rangeTEMP = (TEMP == -1) ? TEMP : (TEMP - patient.TEMP);
		float rangeRESP = (RESP == -1) ? RESP : (RESP - patient.RR);

        InitRange(seconds,rangeHR,rangeSYS,rangeDIA,rangeSP,rangeTEMP,rangeRESP);
		// as currently coded, XML must supply STOP_SP as well as SP for type==target
        InitStop(HR, BP_SYS, BP_DIA, SP, TEMP, RESP); // these values are overwritten by an InitStop called at the Init(patient) level...
    }

    // sets mods from range
    public void InitRange(float seconds, float HR, float BP_SYS, float BP_DIA, float SP, float TEMP, float RESP)
    {
#if DEBUG_VITALS
        UnityEngine.Debug.Log("VitalsBehavior.InitRange(" + seconds + "," + HR + "," + BP_SYS + "," + BP_DIA + "," + SP + ")");
#endif
        if (HR != -1)
            this.heartbeatMod = HR / seconds;
        if ( BP_DIA != -1 )
            this.diastolicMod = BP_DIA / seconds;
        if ( BP_SYS != -1 )
            this.systolicMod = BP_SYS / seconds;
        if ( SP != -1 )
            this.spo2Mod = SP / seconds;
        if (TEMP != -1)
            this.tempMod = TEMP / seconds;
        if (RESP != -1)
            this.respirationMod = RESP / seconds;
#if DEBUG_VITALS        
        UnityEngine.Debug.Log("VitalsBehavior.InitRange(" + seconds + "," + heartbeatMod + "," + systolicMod + "," + diastolicMod + "," + spo2Mod + ")");
#endif
	}
	
    // sets stop values
    public void InitStop(float HR, float BP_SYS, float BP_DIA, float SP, float TEMP, float RESP)
    {
        this.heartbeatModStopAt = HR;
        this.systolicModStopAt = BP_SYS;
        this.diastolicModStopAt = BP_DIA;
        this.spo2ModStopAt = SP;
        this.tempModStopAt = TEMP;
		this.respirationModStopAt = RESP;
    }

    // Check value of stat great/less than
    [System.Serializable]
    public class VBLogic
    {
        public enum VBLogicType { IMMEDIATE, GREATER, LESS, ONADD, ONREMOVE };
        public enum VBLogicStat { HEARTBEAT, DIASTOLIC, SYSTOLIC, SP02 };

        public VBLogicType type;
        public VBLogicStat stat;
        public float value;

        public bool Check(Patient patient)
        {
            switch (type)
            {
                case VBLogicType.IMMEDIATE:
                    {
                        return true;
                    }
                    break;
                case VBLogicType.GREATER:
                    {
                        if (GetValueFromPatient(patient) > this.value)
                            return true;
                    }
                    break;
                case VBLogicType.LESS:
                    {
                        if (GetValueFromPatient(patient) < this.value)
                            return true;
                    }
                    break;
            }
            return false;
        }

        float GetValueFromPatient(Patient patient)
        {
            switch (stat)
            {
                case VitalsBehavior.VBLogic.VBLogicStat.HEARTBEAT:
                    {
                        return patient.HR;
                    }
                    break;
                case VitalsBehavior.VBLogic.VBLogicStat.DIASTOLIC:
                    {
                        return patient.BP_DIA;
                    }
                    break;
                case VitalsBehavior.VBLogic.VBLogicStat.SYSTOLIC:
                    {
                        return patient.BP_SYS;
                    }
                    break;
                case VitalsBehavior.VBLogic.VBLogicStat.SP02:
                    {
                        return patient.SP;
                    }
                    break;
            }
            return -1;
        }
    }

    [System.Serializable]
    public class VBEffectLogic
    {
        public string behaviorName;
        public VBLogic logic;
        //protected VitalsBehavior behavior;  //Remove this and just run with the string name. This may cause pointer conflicts
        public bool endOnEffect;

        //public VitalsBehavior Behavior
        //{
        //    get { return behavior; }
        //    set { behavior = value; }
        //}
    }


}
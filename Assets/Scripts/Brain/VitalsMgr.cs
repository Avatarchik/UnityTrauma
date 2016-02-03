using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

public class Vital
{
    public string name;

    float baseline;
    float current;
    float variance;
    float interval;
    float original;

    float seek;
    float seektime;

    float time = 0.0f;
    float lasttime;

    public string WarningLevel;

    public bool IsDirty;

    public Vital(string name, float baseline, float variance, float interval)
    {
        this.name = name;
        this.baseline = baseline;
        this.seek = baseline;
        this.original = baseline;
        this.variance = variance;
        this.current = baseline;
        this.interval = interval;

        this.WarningLevel = "green";

        IsDirty = true;
    }

    public void Zero()
    {
        seek = 0.0f;
        baseline = 0.0f;
        current = 0.0f;
        variance = 0.0f;
    }

    public float GetVal() 
    {
        if (current < 0)
            Zero();
        return current; 
    }

    public float Seektime
    {
        set { seektime = value; }
        get { return seektime; }
    }

    public void Update(float elapsedTime)
    {
        time += elapsedTime;

        if (baseline != seek)
        {
            if (seektime == 0)
            {
                // we have 0 seektime, just set it
#if DEBUG_VITALS
                Debug.Log("VitalUpdate.Update() : name=" + name + " has zero seektime!");
#endif
                baseline = seek;
            }
            else
            {
#if DEBUG_VITALS
                Debug.Log("VitalUpdate.Update() : name=" + name + ",baseline=" + baseline + ", seek=" + seek + ", seektime=" + seektime + ", elapsedTime=" + elapsedTime);
#endif

                // seek in the right direction
                if (baseline > seek)
                    baseline -= elapsedTime * seektime;
                if (baseline < seek)
                    baseline += elapsedTime * seektime;
            }

            // if we go below 0 then force things to remain at 0
            if (baseline < 0.0f)
            {
                Zero();
            }
        }
        else
        {
#if DEBUG_VITALS
            Debug.Log("VitalUpdate.Update() : name=" + name + ", baseline=" + baseline + ", seek=" + seek );
#endif
        }

        if (time > (lasttime + interval))
        {
            int maxval = (int)(variance * 2.0f * 1000.0f);

            float value = ((float)UnityEngine.Random.Range(-maxval,maxval)) / 1000.0f;

            current = baseline + value;

            lasttime = time;

            IsDirty = true;
        }
    }

    public void ForceUpdate()
    {
        this.current = this.seek;
    }

    public void Set(float value, float time)
    {
#if DEBUG_VITALS
        Debug.Log("VitalUpdate.Set() : name=" + name + ", value=" + value + ", time=" + time);
#endif

        float seektime;

        if (time != 0.0f)
            seektime = Mathf.Abs(current - value) / time;
        else
            seektime = 0.0f;

        this.seek = value;
        this.seektime = seektime;
    }

    public void Set(float value, float time, float variance)
    {
        float seektime;

#if DEBUG_VITALS
        Debug.Log("VitalUpdate.Set() : name=" + name + ", value=" + value + ", time=" + time);
#endif

        if (time != 0.0f)
            seektime = Mathf.Abs(current - value) / time;
        else
            seektime = 0.0f;

        this.seek = value;
        this.seektime = seektime;
        this.variance = variance;
    }

    public void Set(float value, float time, bool absolute)
    {
#if DEBUG_VITALS
        Debug.Log("VitalUpdate.Set() : name=" + name + ", value=" + value + ", time=" + time);
#endif

        if (absolute == false)
        {
            // relative to current value....this is slightly off
            // because the current value is not the starting value
            // but it should be close enough
            value = GetVal() + value;
        }
        Set(value, time);
    }

    public void Change(float value, float time)
    {
        float seektime;

#if DEBUG_VITALS
        Debug.Log("VitalUpdate.Change() : name=" + name + ", value=" + value + ", time=" + time);
#endif

        if (time != 0.0f)
            seektime = Mathf.Abs(current - value) / time;
        else
            seektime = 0.0f;

        this.seek = baseline + value;
        this.seektime = time;
    }

    public void Reset()
    {
#if DEBUG_VITALS
        Debug.Log("VitalUpdate.Reset() : name=" + name);
#endif

        this.baseline = original;
        this.seek = this.baseline;

        current = baseline;
    }

    public void Recover(float time)
    {
        Set(this.original, time);
    }
}

public class VitalState
{
    public string Name;

    List<Vital> vitals;
    Dictionary<string, string> values;

    public VitalState()
    {
        values = new Dictionary<string, string>();
    }

    public void Reset()
    {
        for (int i = 0; i < vitals.Count; i++)
        {
            vitals[i].Reset();
        }
    }

    public void Recover(float time)
    {
        for (int i = 0; i < vitals.Count; i++)
        {
            vitals[i].Recover(time);
        }
    }

    public Vital GetVital(string name)
    {
        for (int i = 0; i < vitals.Count; i++)
        {
            if (vitals[i].name.Equals(name))
            {
                return vitals[i];
            }
        }
        return null;
    }

    public string GetVitalString(string name)
    {
        string value;
        Vital vital;

        if ((vital = GetVital(name)) != null)
        {
            if (vital.IsDirty)
            {
                value = ((int)vital.GetVal()).ToString();

                values[name] = value;

                vital.IsDirty = false;
            }
            else
                value = values[name];

            return value;
        }

        return "";
    }

    public void AddVital(Vital vital)
    {
        if (vitals == null)
            vitals = new List<Vital>();

        vitals.Add(vital);
    }

    public void Update(float elapsedTime)
    {
        for (int i = 0; i < vitals.Count; i++)
            vitals[i].Update(elapsedTime);
    }
}

public class PatientVitals
{
    public VitalState State;

    public void Set( PatientState state, float time )
    {
        //Debug.Log("PatientState.Set(" + state.Name + "," + time + ")");
        //state.Debug();

        if (State == null)
        {
            // state is uninitialized, just make one
#if DEBUG_VITALS
            UnityEngine.Debug.LogWarning("PatientVitals.Set(" + state.Name + "," + time + ")");
#endif
            State = new VitalState();
            State.Name = state.Name;
            State.AddVital(new Vital("HR", state.HR, 0.0f, 2.0f));
            State.AddVital(new Vital("BP_SYS", state.BP_SYS, 0.0f, 1.5f));
            State.AddVital(new Vital("BP_DIA", state.BP_DIA, 0.0f, 1.0f));
            State.AddVital(new Vital("SP", state.SP, 0f, 1.25f));
            State.AddVital(new Vital("TEMP", state.TEMP, 0f, 5.0f));
			State.AddVital(new Vital("RESP", state.RESP, 0f, 1.25f));
#if DEBUG_VITALS
            UnityEngine.Debug.Log("#1 :::: HR=" + state.HR + " : BP_SYS=" + state.BP_SYS + " : BP_DIA=" + state.BP_DIA);
#endif
        }
        else
        {
            // update current state
#if DEBUG_VITALS
            UnityEngine.Debug.LogWarning("PatientVitals.Set(" + state.Name + "," + time + ")");
#endif
            State.Name = state.Name;
            State.GetVital("HR").Set(state.HR, time);
            State.GetVital("BP_SYS").Set(state.BP_SYS, time);
            State.GetVital("BP_DIA").Set(state.BP_DIA, time);
            State.GetVital("SP").Set(state.SP, time);
            State.GetVital("TEMP").Set(state.TEMP, time);
			State.GetVital("RESP").Set(state.RESP, time);
#if DEBUG_VITALS
            UnityEngine.Debug.Log("#2 :::: HR=" + state.HR + " : BP_SYS=" + state.BP_SYS + " : BP_DIA=" + state.BP_DIA);
#endif
        }
    }

    public bool HasReached(string target, float range)
    {
        PatientState Target = VitalsMgr.GetInstance().GetState(target);
        if (Target != null)
        {
            // check all vitals
            if (Math.Abs(State.GetVital("HR").GetVal() - Target.HR) > range)
                return false;
            if (Math.Abs(State.GetVital("BP_SYS").GetVal() - Target.BP_SYS) > range)
                return false;
            if (Math.Abs(State.GetVital("BP_DIA").GetVal() - Target.BP_DIA) > range)
                return false;
            if (Math.Abs(State.GetVital("SP").GetVal() - Target.SP) > range)
                return false;
            if (Math.Abs(State.GetVital("TEMP").GetVal() - Target.TEMP) > range)
                return false;
            if (Math.Abs(State.GetVital("RESP").GetVal() - Target.RESP) > range)
                return false;
            // everything passed
            return true;
        }
        else
            return false;
    }

    public bool HasReached(string target, string item, float range)
    {
        PatientState Target = VitalsMgr.GetInstance().GetState(target);
        if (Target != null)
        {
            if (State.GetVital(item) != null)
            {
                if (Math.Abs(State.GetVital(item).GetVal() - Target.Get(item)) <= range)
                    return true;
            }
        }
        return false;
    }

    public bool HasReached(int target, string item, float range)
    {
        if (Math.Abs(State.GetVital(item).GetVal() - target) <= range)
            return true;

        return false;
    }

    public void Set(string name, float value, float time, bool absolute)
    {
        Vital vital = State.GetVital(name);
        if (vital != null)
        {
            // if relative then go from current value
            if (absolute == false)
                value = vital.GetVal() + value;

            vital.Set(value, time);
        }
    }

    public Vital Get(string vital)
    {
        return State.GetVital(vital);
    }

    public void Set( string name, float time )
    {
        PatientState state = VitalsMgr.GetInstance().GetState(name);
        if ( state != null )
        {
            Set(state,time);
        }
    }

    public void Update(float elapsedTime)
    {
        State.Update(elapsedTime);
    }

    public void ValueChange(MedEffect effect)
    {
        switch (effect.ChangeType)
        {
            case MedEffect.EffectChangeType.AbsoluteChange:
                Set(effect.Name, effect.Value, effect.Time, true);
                break;
            case MedEffect.EffectChangeType.RelativeChange:
                Set(effect.Name, effect.Value, effect.Time, false);
                break;
        }
    }

    public void StateChange(MedEffect effect)
    {
        this.Set(effect.Name, effect.Time);
    }

    public void MedAdminister(MedAdministerMsg msg)
    {
        // process effects
        foreach (MedEffect effect in msg.Med.Effects)
        {
            switch (effect.Type)
            {
                // state change
                case MedEffect.EffectType.StateChange:
                    StateChange(effect);
                    break;
                // setting individual values
                case MedEffect.EffectType.ValueChange:
                    ValueChange(effect);                    
                    break;                   
            }
        }
    }
}

public class PatientState
{
    public string Name;
    public int HR;
    public int BP_SYS;
    public int BP_DIA;
    public int SP;
    public float TEMP;
	public float RESP;

    public string Animation;

    public PatientState()
    {
        Name = "none";
        HR = -1;
        BP_SYS = -1;
        BP_DIA = -1;
        SP = -1;
        TEMP = 98.6f;
		RESP = 18.0f;
    }

    public PatientState(PatientState state)
    {
        Name = state.Name;
        HR = state.HR;
        BP_SYS = state.BP_SYS;
        BP_DIA = state.BP_DIA;
        SP = state.SP;
        TEMP = state.TEMP;
		RESP = state.RESP;
    }

    public float Get(string item)
    {
        switch (item)
        {
            case "HR":
                return HR;
            case "BP_SYS":
                return BP_SYS;
            case "BP_DIA":
                return BP_DIA;
            case "SP":
                return SP;
            case "TEMP":
                return TEMP;
            case "RESP":
                return RESP;
        }
        return -1;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("PatientVitals : name=" + Name + " : hr=" + HR + " : bp_sys=" + BP_SYS + " : bp_dia=" + BP_DIA + " : sp=" + SP + " : temp=" + TEMP + " : resp=" + RESP);
    }
}

public class PhysicalExam
{
    public string Heart;
    public string Lungs;
    public string Reflexes;
    public string VaginalBleeding;
    public string FundalTenderness;
    public string CervixDilation;
    public string CervicalEffacement;
    public string FetalStation;
    public string FetalPresentation;
    public string Temperature;
    public string RespiratoryRate;

    public PhysicalExam()
    {
    }

    public string Translate( string input )
    {
        Debug.Log("PhysicalExam.Translate(" + input + ")");

        string temp = input;

        if (temp.Contains("%PE:Heart"))
        {
            temp = temp.Replace("%PE:Heart",Heart);
        }
        if (temp.Contains("%PE:Lungs"))
        {
            temp = temp.Replace("%PE:Lungs", Lungs);
        }
        if (temp.Contains("%PE:Reflexes"))
        {
            temp = temp.Replace("%PE:Reflexes", Reflexes);
        }
        if (temp.Contains("%PE:VaginalBleeding"))
        {
            temp = temp.Replace("%PE:VaginalBleeding", VaginalBleeding);
        }
        if (temp.Contains("%PE:FundalTenderness"))
        {
            temp = temp.Replace("%PE:FundalTenderness", FundalTenderness);
        }
        if (temp.Contains("%PE:CervixDilation"))
        {
            temp = temp.Replace("%PE:CervixDilation", CervixDilation);
        }
        if (temp.Contains("%PE:CervicalEffacement"))
        {
            temp = temp.Replace("%PE:CervicalEffacement", CervicalEffacement);
        }
        if (temp.Contains("%PE:FetalStation"))
        {
            temp = temp.Replace("%PE:FetalStation", FetalStation);
        }
        if (temp.Contains("%PE:FetalPresentation"))
        {
            temp = temp.Replace("%PE:FetalPresentation", FetalPresentation);
        }
        if (temp.Contains("%PE:Temperature"))
        {
            temp = temp.Replace("%PE:Temperature", Temperature);
        }
        if (temp.Contains("%PE:RespiratoryRate"))
        {
            temp = temp.Replace("%PE:RespiratoryRate", RespiratoryRate);
        }
        return temp;
    }
}

public class VitalsMgr
{
    static VitalsMgr instance;

    VitalsMgr()
    {
    }

     public static VitalsMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new VitalsMgr();
        }
        return instance;
    }

    public List<PatientState> StateList;

    public void Init()
    {
        currentVitals = null;

        // load list
        Serializer<List<PatientState>> serializer = new Serializer<List<PatientState>>();
        StateList = serializer.Load("XML/PatientStates");
#if DEBUG_VITALS_MGR
        Debug();
#endif
    }

    public static void Update()
    {
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("VitalsMgr.Init() : num states=" + StateList.Count);

        foreach (PatientState state in StateList)
        {
            UnityEngine.Debug.Log("VitalsMgr.Init() : state=" + state.Name);
        }
    }

    public PatientState GetState(string name)
    {
        foreach (PatientState state in StateList)
        {
            if (state.Name == name)
                return state;
        }
        return null;
    }

    PatientVitals currentVitals;

    public void SetCurrent(PatientVitals current)
    {
        currentVitals = current;
    }

    public PatientVitals GetCurrent()
    {
        return currentVitals;
    }

    public float GetCurrent(string name)
    {
        if (currentVitals != null)
        {
            Vital vital = currentVitals.Get(name);
            if (vital != null)
                return vital.GetVal();
        }
        return 0.0f;
    }
}

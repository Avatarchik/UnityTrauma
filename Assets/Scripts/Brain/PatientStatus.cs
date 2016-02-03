using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PatientStatus
{
    public class PatientOption
    {
        public string Status;
        public List<string> Options;

        public void Debug( string name )
        {
            string options="<";
            foreach (string option in Options)
            {
                options += option;
                options += ",";
            }
            options += ">";
            UnityEngine.Debug.Log("PatientOption(" + name + ") : options=" + options);
        }
    }

    public class PatientInjury
    {
        public PatientOption Location;
        public PatientOption Type;
        public PatientOption Severity;
        public bool Assessed;
    }

    public class PatientAttachments
    {
        public bool IV;
        public bool BPCuff;
        public bool PulseOx;
        public bool PelvicBinder;
        public bool IDBracelet;
        public bool XRayPlate;
        public bool Clothed;
    }

    public class PatientFluidResuscitation
    {
        public bool BloodInfused;
        public float BloodInfusedRate;
        public bool CrystalloidInfused;
        public float CrystalloidInfusedRate;
        public float BloodLossRate;
    }

    public class PatientSkin
    {
        public PatientOption Temperature;
        public PatientOption Moisture;
        public PatientOption Color;
    }

    public class PatientTreatment
    {
        public PatientOption XRay;
        public PatientOption FAST;
        public bool Intubated;
    }

    public class PatientBodyParts
    {
        public PatientOption Pupils;
        public PatientOption Chest;
        public PatientOption Abdomen;
        public PatientOption Pelvis;
        public PatientOption Arms;
        public PatientOption Legs;
        public PatientOption Hands;
        public PatientOption Feet;
        public PatientOption Head;
    }

    public class PatientLabResults
    {
        public PatientOption Toxicity;
        public PatientOption Midichlorian;
        public PatientOption Hematology;
        public PatientOption GeneralChemistry;
        public PatientOption Coags;
        public PatientOption Urine;
        public PatientOption CerebrospinalFluid;
    }

    public class PatientBehavior
    {
        public bool Combative;
        public bool Unconscious;
        public bool Paralyzed;
        public bool Coherent;
        public bool Drunk;
        public bool Seizure;
        public bool Drugged;
    }

    public class PatientNeurological
    {
        public PatientOption EyeResponse;
        public PatientOption VerbalResponse;
        public PatientOption MotorResponse;
        public PatientOption Total;
    }

    public string Name;
    public PatientOption Airway;
    public PatientOption Breathing;
    public PatientOption Circulation;
    public PatientNeurological Neurological;
    public PatientBehavior Behaviour;
    public PatientBodyParts BodyParts;
    public PatientTreatment Treatment;
    public PatientAttachments Attachments;
    public PatientSkin Skin;
    public List<PatientInjury> Injuries;
    public PatientLabResults LabResults;

    public void CopyOptions(PatientStatus defaults)
    {
        Airway.Options = defaults.Airway.Options;
        Breathing.Options = defaults.Breathing.Options;
        Circulation.Options = defaults.Circulation.Options;
        Neurological.EyeResponse.Options = defaults.Neurological.EyeResponse.Options;
        Neurological.VerbalResponse.Options = defaults.Neurological.VerbalResponse.Options;
        Neurological.MotorResponse.Options = defaults.Neurological.MotorResponse.Options;
        Neurological.Total.Options = defaults.Neurological.Total.Options;
        Treatment.XRay.Options = defaults.Treatment.XRay.Options;
        Treatment.FAST.Options = defaults.Treatment.FAST.Options;
        Skin.Color.Options = defaults.Skin.Color.Options;
        Skin.Moisture.Options = defaults.Skin.Moisture.Options;
        Skin.Temperature.Options = defaults.Skin.Temperature.Options;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("PatientStatus(" + Name + ")");
        Airway.Debug("Airway");
    }
}

public class PatientStatusMgr
{
    static PatientStatusMgr instance;
    static public PatientStatusMgr GetInstance()
    {
        if (instance == null)
            instance = new PatientStatusMgr();
        return instance;
    }

    public PatientStatusMgr()
    {
        StatusList = new List<PatientStatus>();
    }

    public List<PatientStatus> StatusList;
    public PatientStatus Current;

    public PatientStatus LoadXML(string filename)
    {
        Serializer<PatientStatus> serializer = new Serializer<PatientStatus>();
        if (serializer != null)
        {
            PatientStatus status = serializer.Load(filename);
            if ( Default != null )
                status.CopyOptions(Default);
            return status;
        }
        else
            return null;
    }

    public void Add(PatientStatus status)
    {
        StatusList.Add(status);
    }

    public PatientStatus Get(string name)
    {
        foreach (PatientStatus status in StatusList)
        {
            if (status.Name == name)
                return status;
        }
        return null;
    }

    public void Set(PatientStatus status)
    {
        Current = status;
    }

    public PatientStatus Default;
    public void LoadDefaultXML(string filename)
    {
        Default = LoadXML(filename);
        Default.Debug();
        // set Current as backup
        Current = Default;
    }
}


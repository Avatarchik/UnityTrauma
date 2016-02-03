using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VitalsParser))]
public class HeartbeatGraph : VitalsGraph
{
    Patient patient;
	public AudioSource speaker=null;
	public AudioClip beep=null;

	// Use this for initialization
	protected override void Start ()
    {
        parser = GetComponent<VitalsParser>();
        if (parser == null || !parser.enabled)
        {
            enabled = false;
            return;
        }

        base.Start();
        patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		// if not set in the level, search for a vitals monitor with an audio component
		if (speaker == null){
			GameObject monitor = GameObject.Find("fab_cartMonitorVitals01");
			if (monitor != null){
				speaker = monitor.audio;	
			}
		}
	}
	
	// Update is called once per frame
	protected override void Update ()
    {
        base.Update();
		// set a waveform type here? we can at least transition to flatline.
		
		if (patient != null ) {
		if (patient.HR < 10 && patient.HeartbeatType!="flatline")
			patient.HeartbeatType="flatline";
		}
		/*
        parser.bpmMod = patient.HR / parser.normal;

        VitalsParser.VitalDataPoint lastPoint = parser.GetLastPoint();
        VitalsParser.VitalDataPoint nextPoint = parser.GetNextPoint();

        if (nextPoint == null)
            return;

        float percent, interp;
        if (lastPoint == null)
        {
            percent = parser.Timer / nextPoint.time;
            interp = nextPoint.point * percent;
        }
        else
        {
            percent = (parser.Timer - lastPoint.time) / (nextPoint.time - lastPoint.time);
            interp = (nextPoint.point - lastPoint.point) * percent + lastPoint.point;
        }
        y = (parser.DataMax - interp)/parser.MaxValue;
        */
	}

	public override void Resample (float time)
    {
        parser.bpmMod = patient.HR / parser.normal;
		
		parser.Resample(time);
/*
        VitalsParser.VitalDataPoint lastPoint = parser.GetLastPoint();
        VitalsParser.VitalDataPoint nextPoint = parser.GetNextPoint();

        if (nextPoint == null)
            return;

        float percent, interp;
        if (lastPoint == null)
        {
            percent = parser.Timer / nextPoint.time;
            interp = nextPoint.point * percent;
        }
        else
        {
            percent = (parser.Timer - lastPoint.time) / (nextPoint.time - lastPoint.time);
            interp = (nextPoint.point - lastPoint.point) * percent + lastPoint.point;
        }
 */
        y = (parser.DataMax - parser.GetNextPoint().point)/parser.MaxValue;
	}
	
	
	public override void CheckMode()
	{
		if (patient == false ) return;
		if (patient.HeartbeatType != parser.Mode) parser.Mode = patient.HeartbeatType;	
		// this is called by the parser when the waveform has cycled, so beeping here will track the pulse rate
		// HRAudio moved here from Patient.doHRAudio
		
		// modulate the EKG tone using 2xNormalized SAO2 level
		float mod = Mathf.Clamp((2*patient.SP-100)/100.0f,0.25f,1.0f);
		speaker.pitch = mod;
		
		if (speaker!=null && beep!=null && patient.HasAttribute("onvitalsmonitor"))
			speaker.PlayOneShot(beep);
		
		base.CheckMode();
	}
	
	public override bool Connected (){
		if (patient == null )
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient; // called from update, VERY BAD when there's no patient!
		if (patient == null ) return false;
		return(patient.GetAttribute("onvitalsmonitor")=="True");
	}
}

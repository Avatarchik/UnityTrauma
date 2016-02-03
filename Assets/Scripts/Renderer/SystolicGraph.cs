using UnityEngine;
using System.Collections;

public class SystolicGraph : VitalsGraph 
{
    Patient patient;

	// Use this for initialization
	protected override void Start ()
    {
        base.Start();

        patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		parser = GetComponent<VitalsParser>();
	}
	
	public override void Resample (float time)
    {
        parser.bpmMod = patient.HR / parser.normal;
		
		parser.Resample(time);

        if (patient){
            y = patient.BP_SYS / 200f;
			if (parser!=null){
				y+= parser.GetNextPoint().point*.25f;
				parser.bpmMod = patient.HR / parser.normal;
			}
		}
	}
	
	public override void CheckMode(){
		if (patient.HeartbeatType == "flatline") parser.Mode = "flatline";
		base.CheckMode ();
	}
	
	public override bool Connected (){
		if (patient == null)
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		if (patient == null) return false;
		return(patient.GetAttribute("autobpplaced")=="True" && 
			patient.GetAttribute ("cufferror")!="True");
	}
}

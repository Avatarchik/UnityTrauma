using UnityEngine;
using System.Collections;

public class O2Graph : VitalsGraph
{
    Patient patient;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		parser = GetComponent<VitalsParser>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.O))
        {
            display = !display;
        }


    }
	
	public override void Resample (float time)
    {
        parser.bpmMod = 14f / parser.normal; // should be respiration rate/normal
		
		parser.Resample(time);

        if (patient){
            y = patient.SP / 200f;
			if (parser!=null)
				y+= parser.GetNextPoint().point*0.25f;
		}
	}
	
	public override void CheckMode(){
		if (patient == null) return;
		if (patient.HeartbeatType == "flatline") parser.Mode = "flatline";
		base.CheckMode();
	}
	
	public override bool Connected (){
		if (patient == null )
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient; // called from update, VERY BAD when there's no patient!
		if (patient == null) return false;
		return(patient.GetAttribute("pulseoxplaced")=="True");
	}
}
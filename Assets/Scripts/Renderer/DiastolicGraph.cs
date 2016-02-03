using UnityEngine;
using System.Collections;

public class DiastolicGraph : VitalsGraph
{
    Patient patient;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (patient)
            y = patient.BP_DIA / 200f;
    }
}

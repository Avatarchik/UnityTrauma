using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TraumaPatient))]

// have to create an inspector for each specific class

public class TraumaPatientInspector : CharacterInspector 
{
	TraumaPatient myPatient;
	VitalsBehaviorManager vbm=null;

	override public void OnSelected() // look at a particular instance object
	{
		myPatient = target as TraumaPatient;
		base.OnSelected();
	}	
	
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		Color old = GUI.color;
		GUI.color = Color.cyan;
		if (Application.isPlaying)
			ShowVitals();
		GUI.color = old;
	}
	
	void ShowVitals(){
		GUILayout.Label("Vitals State "+myPatient.VITAL_STATE);
		if (vbm==null){
			vbm = FindObjectOfType(typeof(VitalsBehaviorManager)) as VitalsBehaviorManager;	
		}
		if (vbm != null)
			vbm.OnInspectorGUI();
	}
	
}

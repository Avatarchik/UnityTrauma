using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

[CustomEditor(typeof(HandPoser))]

public class HandPoserInspector : Editor {
	
	// This class lets you snapshot poses and then save the xml file of those poses for use by the poser.
	HandPoser myPoser = null;
	

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	virtual public void OnSelected() // look at a particular instance object
	{
		myPoser = target as HandPoser;
	}
	
	
	public override void OnInspectorGUI()
	{
		myPoser = target as HandPoser;
		DrawDefaultInspector();	
		if (GUILayout.Button ("Capture Open Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseOpen);	
		}
		if (GUILayout.Button ("Capture Closed Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseFist);	
		}
		if (GUILayout.Button ("Capture OneFingerPoint Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseOneFingerPoint);	
		}
		if (GUILayout.Button ("Capture OneFingerGrasp Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseOneFingerGrasp);	
		}
		if (GUILayout.Button ("Capture TwoFingerPoint Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseTwoFingerPoint);	
		}
		if (GUILayout.Button ("Capture TwoFingerGrasp Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseTwoFingerGrasp);	
		}
		if (GUILayout.Button ("Capture FourFingerPoint Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseFourFingerPoint);	
		}
		if (GUILayout.Button ("Capture FourFingerGrasp Pose")){
			myPoser.SampleRotations(myPoser.morphs.poseFourFingerGrasp);	
		}
		GUI.backgroundColor = Color.red;
		if (GUILayout.Button ("Write Poses to XML")){
			XmlSerializer serializer = new XmlSerializer(typeof(HandPoser.poseSet));
			FileStream stream = new FileStream(myPoser.XMLDirectory+myPoser.pathname, FileMode.Create);
			serializer.Serialize(stream, myPoser.morphs);
			stream.Close();	
		}
		if (GUILayout.Button ("Read Poses from XML")){
			myPoser.InitPoses();	
		}
		if (GUILayout.Button ("Call Start")){
			myPoser.Start();	
		}
		if (GUILayout.Button ("FLIP Poses")){
			myPoser.FlipPoses();	
		}
		GUI.backgroundColor = Color.green;
		if (GUILayout.Button ("SnapTo Open Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseOpen,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo Closed Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseFist,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo OneFingerPoint Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseOneFingerPoint,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo OneFingerGrasp Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseOneFingerGrasp,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo TwoFingerPoint Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseTwoFingerPoint,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo TwoFingerGrasp Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseTwoFingerGrasp,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo FourFingerPoint Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseFourFingerPoint,myPoser.fingers);	
		}
		if (GUILayout.Button ("SnapTo FourFingerGrasp Pose")){
			myPoser.SnapToPose(myPoser.morphs.poseFourFingerGrasp,myPoser.fingers);	
		}
	}
	
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

/*  This class allows animated control over the fingers and thumb of the 
 * 
 * 
 * 
 * 
 */

public class HandPoser : MonoBehaviour {
	
//	public AnimationClip poseClip = null; // contains the following poses at 1 sec intervalse:
//	public Animation poseAnimator = null;
	
	public bool rightHand = false;

	public float poseFist = -1;
	public float oneFingerPoint = -1; // index out, others curled, thumb over other fingers
	public float oneFingerGrasp = -1; // index out, thumb out, index touching thumb 
	public float twoFingerPoint = -1; //  two fingers out, others curled, thumb over other fingers
	public float twoFingerGrasp = -1; //  thumb out, two fingers touching thumb
	public float fourFingerPoint = -1; //  two fingers out, others curled, thumb over other fingers
	public float fourFingerGrasp = -1; //  thumb out, two fingers touching thumb
	
	[System.Serializable]
	public class digit{
		[XmlIgnore]
		public Transform[] xfm = new Transform[3];
		public Quaternion[] rotations = new Quaternion[3];		
	}
	[System.Serializable]
	public class pose{
		public digit[] digits = new digit[5];
	}
	[System.Serializable]
	public class poseSet{
		public pose poseOpen = new pose();
		public pose poseFist = new pose();
		public pose poseOneFingerPoint = new pose();
		public pose poseOneFingerGrasp = new pose();
		public pose poseTwoFingerPoint = new pose();
		public pose poseTwoFingerGrasp = new pose();
		public pose poseFourFingerPoint = new pose();
		public pose poseFourFingerGrasp = new pose();
	}
	
	public poseSet morphs = null; // these targets contain the rotations we will lerp to

	public pose fingers = new pose(); // this points to the actual transforms we are animating
	public Transform palmBone = null; 
	public string XMLDirectory = "Assets/Resources/XML/";
	public string pathname = "HandPosesLeft.xml"; // path to our pose xml file (created with inspector)

	// Use this for initialization
	public void Start () {
		if (palmBone != null)
			Setup(palmBone);
	}
	
	void Awake(){
		if (palmBone != null)
			Setup(palmBone);
		
	}
	
	public void Setup(Transform palm){
		// build the array of digits
		palmBone = palm;
		string prefix = "sitelHuman_LArmDigit";
		if (palm.name.Contains ("RArm")){
			prefix = "sitelHuman_RArmDigit";
			rightHand = true;
		}
		
		for (int d =1; d<6; d++){
			for ( int j =1; j<4;j++){
				// get the transforms for the bones we are controlling
				fingers.digits[d-1].xfm[j-1] = FindInChildren( palmBone.gameObject, prefix+d.ToString()+j.ToString());
				if (fingers.digits[d-1].xfm[j-1] == null)
					Debug.LogError(prefix+d.ToString()+j.ToString()+" NOT FOUND");
			}
		}
		InitPoses();
		if (rightHand)
			FlipPoses ();
	}
		
	
	private Transform FindInChildren(GameObject go, string childName){
		foreach (Transform t in go.GetComponentsInChildren<Transform>()){
			if (t.name == childName) return t;
		}
		return null;
	}
	
	public void SampleRotations(pose dst){ // copy the current transform local rotations into a pose
		// this is used when recording up the poses from the editor or an animation pose file.
		for (int d =0; d<5; d++){
			for ( int j =0; j<3;j++){
				// currently we only reference the rotations property of poses.		
				dst.digits[d].rotations[j]=fingers.digits[d].xfm[j].localRotation;
			}
		}		
	}
	
	public void CopyPose(pose src, pose dst){
		for (int d =0; d<5; d++){
			for ( int j =0; j<3;j++){
				// currently we only reference the rotations property of poses.		
				dst.digits[d].rotations[j]=src.digits[d].rotations[j];
			}
		}	
	}
	public void SnapToPose(pose src, pose dst){
		for (int d =0; d<5; d++){
			for ( int j =0; j<3;j++){
				// currently we only reference the rotations property of poses.		
				dst.digits[d].xfm[j].localRotation = src.digits[d].rotations[j];
			}
		}	
	}
	
	public void FlipPoses(){
		FlipPose (morphs.poseFist);
		FlipPose (morphs.poseOpen);
		FlipPose (morphs.poseOneFingerPoint);
		FlipPose (morphs.poseOneFingerGrasp);
		FlipPose (morphs.poseTwoFingerPoint);
		FlipPose (morphs.poseTwoFingerGrasp);
		FlipPose (morphs.poseFourFingerPoint);
		FlipPose (morphs.poseFourFingerGrasp);
	}
	public void FlipPose(pose p){ // flip the handedness of the poses by negating y and z values
		for (int d =0; d<5; d++){
			for ( int j =0; j<3;j++){
				// currently we only reference the rotations property of poses.	
				p.digits[d].rotations[j].y = -p.digits[d].rotations[j].y;
				p.digits[d].rotations[j].z = -p.digits[d].rotations[j].z;
			}
		}		
	}
	
	public void InitPoses(){
		/*     code fragment for reading these from an animation clip
		 *     animation["HandPoses"].enabled = true;
    			animation["HandPoses"].weight = 1f;
    			animation["HandPoses"].time = pose;
    			animation["HandPoses"].speed = 0; //to make the animation pause
    */
		if (Application.isEditor){ // from the editor, use the xml files directly
			XmlSerializer serializer = new XmlSerializer(typeof(poseSet));
			FileStream stream = new FileStream(XMLDirectory+pathname, FileMode.Open);
			morphs = serializer.Deserialize(stream) as poseSet;
			stream.Close();
		}
		else
		{	// use Rob's serializer to load from compiled resources folder at runtime
			Serializer<poseSet> serializer = new Serializer<poseSet>();
			pathname = "XML/"+pathname.Replace (".xml","");
			morphs = serializer.Load(pathname);
		}
		
	}
		
	// Update is called once per frame
	void Update () {
	
	}
	
	void LateUpdate(){
		UpdatePose ();
	}
	
	void UpdatePose(){
		float max=0;
		
		if (poseFist > max){
			LerpPose(morphs.poseFist,poseFist);
			max = poseFist;
		}
		if (oneFingerPoint > max){
			LerpPose(morphs.poseOneFingerPoint,oneFingerPoint);
			max = oneFingerPoint;
		}
		if (oneFingerGrasp > max){
			LerpPose(morphs.poseOneFingerGrasp,oneFingerGrasp);
			max = oneFingerGrasp;
		}
		if (twoFingerPoint > max){
			LerpPose(morphs.poseTwoFingerPoint,twoFingerPoint);
			max = twoFingerPoint;
		}
		if (twoFingerGrasp > max){
			LerpPose(morphs.poseTwoFingerGrasp,twoFingerGrasp);
			max = twoFingerGrasp;
		}
		if (fourFingerPoint > max){
			LerpPose(morphs.poseFourFingerPoint,fourFingerPoint);
			max = fourFingerPoint;
		}
		if (fourFingerGrasp > max){
			LerpPose(morphs.poseFourFingerGrasp,fourFingerGrasp);
			max = fourFingerGrasp;
		}
	}
	void LerpPose(pose p, float weight){
		for (int d =0; d<5; d++){
			for (int j=0; j<3;j++){
				if (fingers.digits[d].xfm[j] != null)
					fingers.digits[d].xfm[j].localRotation = Quaternion.Slerp(morphs.poseOpen.digits[d].rotations[j],p.digits[d].rotations[j],weight);
			}
		}
	}
	
	void OnDrawGizmos(){ // maybe this will get the pose set in the editor ?
		if (palmBone == null) return;
		if (palmBone != null && fingers.digits[0].xfm[0] == null)
			Setup(palmBone);
		UpdatePose ();
		for (int d =0; d<5; d++){
			for (int j=0; j<3;j++){
				Gizmos.DrawWireCube(fingers.digits[d].xfm[j].position,new Vector3(0.01f,0.01f,0.01f));	
				if (j==0)
					Gizmos.DrawLine (palmBone.position,fingers.digits[d].xfm[j].position);
				else
					Gizmos.DrawLine (fingers.digits[d].xfm[j-1].position,fingers.digits[d].xfm[j].position);
			}
		}
	}
	
}

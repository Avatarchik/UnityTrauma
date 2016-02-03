using UnityEngine;
using System.Collections;

public class RopeSolver : MonoBehaviour {
	
	public bool startLink = false;
	public bool hideUntilSettled = true;
	private RopeSolver prev;
	private RopeSolver next;
	private RopeSolver firstLink;
	public RopeSolver finalLink;
	private Vector3 e0,e1;
	private Vector3 axis;
	private float myLength;
	private int myIndex =1;
	public int numSegments = 40;
	public float overallLength = 1.25f; // on link 0, specify the ratio of initial placement to total length
	public float radius = 0.1f;
	public float droopiness = 0.0f;
	public float startSlack=1.0f;
	private float endweight;
	
	// mesh data
	
	public Material material;
	public int crossSegments= 12;
	private Vector3[] crossPoints;
	private int lastCrossSegments;
	private Mesh mesh;
	private Vector3 e0LastPos;  // detect if either end moves, and stop updating if no movement
	private Vector3 e1LastPos;
	private float movementSlack;
	public bool quiescent = false;

	// Use this for initialization
	void Start () {
		if (startLink) Build ();
	}
	
	public void Init(){
		// initialize eo,e1
		UpdateAxis();
		RopeSolver starter = this;
		while (starter.prev != null)
			starter = starter.prev;
		firstLink = starter;
		myIndex = -1; // one based
		numSegments = 0;
		
		while (starter != null){
			numSegments++;
			if (myIndex == -1 && starter == this) myIndex = numSegments;
			starter = starter.next;			
		}
		endweight = 0;
		if (myIndex < numSegments/4)
			endweight = numSegments/4 - myIndex;
		else 
			if (myIndex > numSegments*0.75f) endweight = myIndex-numSegments*.75f;
		endweight /= (numSegments/4);
	}
	
	// currently handles rope constrained at both ends, because that is what we needed 
	
	// Update is called once per frame
	void Update () {
		
		if (firstLink == null) return;
		if (this == firstLink){
			if (e0LastPos != transform.position || e1LastPos != finalLink.transform.position){
				e0LastPos = transform.position;
				e1LastPos = finalLink.transform.position;
				startSlack = movementSlack; // decreases till we settle out
				quiescent=false;
			}
			else
			{	// no movement, damper then stop updating

				if (startSlack < 0.001f){
					quiescent=true;
					if (hideUntilSettled){
						MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
						mr.enabled = true;
					}
				}
			}
	
			overallLength = 0;
			RopeSolver current = this;
			while (current.next != null){
				overallLength += Vector3.Distance(current.e0,current.next.e0);
				current = current.next;
			}
			overallLength -= 2*myLength; // makes the tube stabil.
			UpdateMesh ();
		}
		
		if (firstLink.quiescent) return; // bail if we have settled
		
		float droop = numSegments*myLength - firstLink.overallLength;
		if (droop < 0) droop = 0;
		
		startSlack *= 0.99f;
		// animate ends for testing
//		if (prev == null){
//			transform.rotation *= Quaternion.AngleAxis(0.5f*Mathf.Sin(Time.time),Vector3.forward);	
//		}
//		if (next == null){
//			transform.position -= new Vector3(0.04f*Mathf.Cos(Time.time/2),0,0);	
//		}
		// solve for half way between neighbors with blended orientation
		if (prev != null && next != null){
			
			float slack = myLength - Vector3.Distance(prev.e1,next.e0);

			if (slack < 0) slack = 0;
			
			
			slack = (Time.deltaTime*(droopiness+startSlack))+droop/numSegments;// += 0.005f; // this adds a gravitational droopiness
			if (slack > 0.01f) slack = 0.01f; // upper limit for stability
//			if (slack < 0.005f) slack = 0;
slack=0;		

			
			transform.position = Vector3.Lerp(prev.e1,next.e0,0.5f) - new Vector3(0,slack,0);
			Quaternion desiredRotation = Quaternion.LookRotation(next.e0-prev.e1)*Quaternion.AngleAxis (90,Vector3.right);
			if (myIndex <= numSegments/2){
				if (endweight > 0)
					desiredRotation = Quaternion.Lerp (desiredRotation,prev.transform.rotation,endweight);
				else
					desiredRotation = Quaternion.Lerp (desiredRotation,next.transform.rotation,0.2f); //anti-kink factor
			}
			else{
				if (endweight > 0)
					desiredRotation = Quaternion.Lerp (desiredRotation,next.transform.rotation,endweight);
				else
					desiredRotation = Quaternion.Lerp (desiredRotation,prev.transform.rotation,0.2f);
			}
			transform.rotation = Quaternion.Lerp (transform.rotation,desiredRotation,0.25f);
		}
		// update my e0,e1 points
		UpdateAxis();
	
	}
	
	void UpdateAxis(){
		axis = transform.up;
		e0 = transform.position-0.5f*axis*myLength;
		e1= transform.position+0.5f*axis*myLength;
	}
	
	void Build(){ // called by the first link START
		// assume next points to the final link.
		startLink = false;
		finalLink.transform.parent = null;
		movementSlack = startSlack;
		GameObject go;
		RopeSolver prevLink = this;
		RopeSolver thisLink;
		Vector3 pos = transform.position;
		float length = Vector3.Distance(finalLink.transform.position,transform.position);
		overallLength *= length;
		myLength = overallLength/numSegments;
		Vector3 delta = finalLink.transform.position - pos;
//		delta = myLength*delta.normalized; // start stretched out OR
		delta = delta/numSegments;		// start compressed
		for (int i=1; i<numSegments-1;i++){
		
			go = Instantiate(this.gameObject) as GameObject; // could just create new at this point...
			go.name = this.name+"-segment-"+i;
			pos += delta;
			go.transform.position = pos;
			go.transform.parent = finalLink.transform;
		
			thisLink = go.GetComponent("RopeSolver") as RopeSolver;
			prevLink.next = thisLink;
			thisLink.prev = prevLink;
			thisLink.myIndex = i+1;
			thisLink.myLength = myLength;
			thisLink.firstLink = this;
			thisLink.numSegments = numSegments;
			prevLink = thisLink;
		}
		prevLink.next = finalLink;
		finalLink.prev = prevLink;
		
		thisLink = this;
		while (thisLink != null){
			thisLink.Init ();
			thisLink = thisLink.next;
			
		}
		
		BuildMesh ();
	}
	
	void BuildMesh(){
		mesh = new Mesh();
//		gameObject.AddComponent<MeshFilter>();
		MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
		mr.material = material;
		if (!hideUntilSettled) mr.enabled = true;
		RopeSolver currentLink = this;
		
		crossPoints = new Vector3[crossSegments];
		float theta = 2.0f*Mathf.PI/crossSegments;
		for (int c=0;c<crossSegments;c++) {
			crossPoints[c] = new Vector3(Mathf.Cos(theta*c), Mathf.Sin(theta*c), 0);
		}
 
		Vector3[] meshVertices = new Vector3[numSegments*crossSegments];
	 	Vector2[] uvs = new Vector2[numSegments*crossSegments];

	 	int[] tris = new int[numSegments*crossSegments*6];
	 	int[] lastVertices = new int[crossSegments];
	 	int[] theseVertices = new int[crossSegments];
	 	Quaternion rotation = Quaternion.identity;
 
		for (int p=0;p<numSegments;p++) {
			if(p<numSegments-1)
				rotation = Quaternion.FromToRotation(Vector3.forward,currentLink.transform.up); //???
	 
			for (int c=0;c<crossSegments;c++) {
				int vertexIndex = p*crossSegments+c;
				meshVertices[vertexIndex] = transform.InverseTransformPoint (currentLink.transform.position + rotation * crossPoints[c] * radius);
				uvs[vertexIndex] = new Vector2((0.0f+c)/crossSegments,(0.0f+p)/numSegments);
	 
				lastVertices[c]=theseVertices[c];
				theseVertices[c] = p*crossSegments+c;
			}
	 
			//make triangles
			if (p>0) {
				for (int c=0;c<crossSegments;c++) {
					int start= (p*crossSegments+c)*6;
					tris[start] = lastVertices[c];
					tris[start+1] = lastVertices[(c+1)%crossSegments];
					tris[start+2] = theseVertices[c];
					tris[start+3] = tris[start+2];
					tris[start+4] = tris[start+1];
					tris[start+5] = theseVertices[(c+1)%crossSegments];
				}
			}
			currentLink = currentLink.next;
		}
		mesh.vertices = meshVertices;
		mesh.triangles = tris;
		mesh.uv = uvs;
		GetComponent<MeshFilter>().mesh = mesh;		
	}
	
	void UpdateMesh(){
		// just update the vertices, everything else should stay the same.
		Vector3[] meshVertices = mesh.vertices;
		Quaternion rotation = Quaternion.identity;
		RopeSolver currentLink = this;
		
		for (int p=0;p<numSegments;p++) {
			if(p<numSegments-1)
				rotation = Quaternion.FromToRotation(Vector3.forward,currentLink.next.e1-currentLink.e1); //???
	 
			for (int c=0;c<crossSegments;c++) {
				int vertexIndex = p*crossSegments+c;
				meshVertices[vertexIndex] = transform.InverseTransformPoint (currentLink.transform.position + rotation * crossPoints[c] * radius);
			}
			currentLink = currentLink.next;
		}
		mesh.vertices = meshVertices;
		mesh.RecalculateNormals();
	}
}

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class IconTag : MonoBehaviour
{
	public float width = 0.1f;
	public float height = 0.1f;
    public Camera camera;
    Mesh mesh;
	
	// Use this for initialization
	void Start ()
	{
        if (camera == null)
            camera = Camera.mainCamera;

		if(width <= 0) width = 0.2f;
		if(height <= 0) height = 0.2f;
		
		float width2 = width / 0.5f;
		float height2 = height / 0.5f;
		
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		
		// Define vertices
		Vector3[] newVertices = new Vector3[4];
		newVertices[0].x = -width2;
		newVertices[0].y = height2;
		newVertices[1].x = width2;
		newVertices[1].y = height2;
		newVertices[2].x = width2;
		newVertices[2].y = -height2;
		newVertices[3].x = -width2;
		newVertices[3].y = -height2;
		
		// Define UVs
		Vector2[] newUV = new Vector2[4];
        newUV[0].x = 0;
        newUV[0].y = 1;
        newUV[1].x = 1;
        newUV[1].y = 1;
        newUV[2].x = 1;
        newUV[2].y = 0;
        newUV[3].x = 0;
        newUV[3].y = 0;
		
		// Define triangles
		int[] newTris = new int[] {0,1,2, 0,2,3 };
		
		
		// Set data
		mesh.vertices = newVertices;
		mesh.uv = newUV;
		mesh.triangles = newTris;
		
	}
	
	// Update is called once per frame
	void Update () 
    {
        transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);
	}
}
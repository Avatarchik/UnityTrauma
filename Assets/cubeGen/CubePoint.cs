using UnityEngine;
using System.Threading;
using System.Collections;

public class CubePoint : MonoBehaviour
{
    public string cubePointName = "Name";
    public Cubemap cubemaps;
	
    public void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "../cubeGen/Gizmos/cGizmo.png");
    }

}
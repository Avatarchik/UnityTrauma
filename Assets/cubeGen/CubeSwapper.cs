using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class CubeSwapper : MonoBehaviour
{
    private static GameObject[] cubePoints;
	private static GameObject closest = null;
	private float distance;
	public Vector3 diff;

    void Awake()
    {
        cubePoints = GameObject.FindGameObjectsWithTag("Cubemap");
    }

    GameObject FindClosestCubemap()
    {
        this.distance = Mathf.Infinity;
        foreach (GameObject point in cubePoints)
        {
            this.diff = point.transform.position - transform.position;
            if (this.diff.sqrMagnitude < distance)
            {
                closest = point;
                this.distance = diff.sqrMagnitude;
            }
        }
        return closest;
    }

    void FixedUpdate()
    {
        //Now we can swap the cubemap texture realtime based on the players proximity to the cubemap point we placed
        this.renderer.material.SetTexture("_Cube", this.FindClosestCubemap().GetComponent<CubePoint>().cubemaps);
    }
}

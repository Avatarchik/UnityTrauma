using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// this is basically a dummy script so we can use a custom inspector
// to modify the global settings in CaseConfiguratorMgr.
//
public class CaseConfiguratorSettings : MonoBehaviour 
{
	public bool UseLocalData=true;
	public bool UseCaseOrder=true;
	public List<string> CaseOrder;

	// Use this for initialization
	void Start () {
		CaseConfiguratorMgr.GetInstance().UsingLocalData = UseLocalData;
		CaseConfiguratorMgr.GetInstance().UsingCaseOrder = UseCaseOrder;	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

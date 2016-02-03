using UnityEngine;
using System.Collections;

public class BuildVersion : MonoBehaviour 
{
	public string Version="V:(not defined)";

	static BuildVersion instance;
	public static BuildVersion GetInstance()
	{
		return instance;
	}

	void Awake()
	{
		instance = this;
	}
}

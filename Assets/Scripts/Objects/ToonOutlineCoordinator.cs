using UnityEngine;
using System.Collections;

public class ToonOutlineCoordinator : MonoBehaviour
{
    ToonOutline[] toScripts;

	// Use this for initialization
	void Start ()
    {
        toScripts = GetComponentsInChildren<ToonOutline>();
	}

    void OnMouseEnter()
    {
        foreach (ToonOutline to in toScripts)
            to.selected = true;
    }

    void OnMouseExit()
    {
        foreach (ToonOutline to in toScripts)
            to.selected = false;
    }
}

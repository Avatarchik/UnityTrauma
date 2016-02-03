using UnityEngine;
using System.Collections;

public class RailSwitchControl : MouseOverFeedbackC 
{
    CameraRailCoordinator crc;
    public enum RSCValues { NONE = 0, FORWARD = 1, REVERSE = 2 };
    public bool toggle = false;
    public RSCValues move = RSCValues.NONE;

    void Start()
    {
        base.DoStart();
        crc = Component.FindObjectOfType(typeof(CameraRailCoordinator)) as CameraRailCoordinator;
    }

    void OnMouseUpAsButton()
    {
        if (toggle)
        {
            if (crc != null)
                crc.Switch();
        }
    }

    void Update()
    {
        base.DoUpdate();
        if (mouseDown && !toggle)
        {
            if (move == RSCValues.FORWARD)
            {
                if (crc != null)
                    crc.GUIInput(-1);
            }
            else if (move == RSCValues.REVERSE)
            {
                if (crc != null)
                    crc.GUIInput(1);
            }
            else
            {
                if (crc != null)
                    crc.GUIInput(0);
            }
        }
    }
	
    protected override void DoReset()
    {
        base.DoReset();
        if (crc != null)
            crc.GUIInput(0);
    }
}

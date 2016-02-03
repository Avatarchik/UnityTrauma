using UnityEngine;
using System.Collections;

public class NavArrowControl : MouseOverFeedbackC
{
    public enum NACDirection { NONE = 0, UP, DOWN, LEFT, RIGHT, HOME };
    public NACDirection direction = 0;

    void Update()
    {
        base.DoUpdate();
        if (mouseDown)
        {
            switch (direction)
            {
                case NACDirection.UP:
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookUp();
                    }
                    break;
                case NACDirection.DOWN:
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookDown();
                    }
                    break;
                case NACDirection.LEFT:
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookLeft();
                    }
                    break;
                case NACDirection.RIGHT:
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookRight();
                    }
                    break;
                case NACDirection.HOME:
                    {
                        CameraRailCoordinator crc = Component.FindObjectOfType(typeof(CameraRailCoordinator)) as CameraRailCoordinator;
                        crc.Reset();
                    }
                    break;
            }
        }
    }
}
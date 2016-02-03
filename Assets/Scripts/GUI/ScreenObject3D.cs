using UnityEngine;
using System.Collections;

public class ScreenObject3D : MonoBehaviour
{
    public enum PositionLock { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, CenterLeft, CenterCenter, CenterRight}

    public PositionLock position;
    public Camera camera;

    void Start()
    {
        if(camera == null)
        {
            enabled = false;
            return;
        }
    }

    void SetPosition()
    {
        Vector3 pos = new Vector3();
        pos.z = camera.farClipPlane - 0.0001f;

        switch (position)
        {
            case PositionLock.TopLeft:
                {
                    pos.x = 0;
                    pos.y = 1;
                }
                break;
            case PositionLock.TopCenter:
                {
                    pos.x = 0.5f;
                    pos.y = 1;
                }
                break;
            case PositionLock.TopRight:
                {
                    pos.x = 1;
                    pos.y = 1;
                }
                break;
            case PositionLock.BottomLeft:
                {
                    pos.x = 0;
                    pos.y = 0;
                }
                break;
            case PositionLock.BottomCenter:
                {
                    pos.x = 0.5f;
                    pos.y = 0;
                }
                break;
            case PositionLock.BottomRight:
                {
                    pos.x = 1;
                    pos.y = 0;
                }
                break;
            case PositionLock.CenterLeft:
                {
                    pos.x = 0;
                    pos.y = 0.5f;
                }
                break;
            case PositionLock.CenterCenter:
                {
                    pos.x = 0.5f;
                    pos.y = 0.5f;
                }
                break;
            case PositionLock.CenterRight:
                {
                    pos.x = 1;
                    pos.y = 0.5f;
                }
                break;
        }

        transform.position = camera.ViewportToWorldPoint(pos);
    }

    void Update()
    {
        SetPosition();
    }
}
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class LineDraw : MonoBehaviour
{
    Material lineMaterial;

    int lineWidth = 1;
    Color lineColor = Color.white;
    bool drawLines = true;
    Camera cam;

    Vector2[] linePoints;
    Vector2[] origPoints;

    int numberOfPoints = 32;

    public void Start () 
    {
        linePoints = new Vector2[numberOfPoints];
        origPoints = new Vector2[numberOfPoints];
    
        // Plot points on a circle
        float radians = 360.0f/(numberOfPoints-1)*Mathf.Deg2Rad;
        float p = 0.0f;
        for (int i = 0; i < numberOfPoints; i++) 
        {
            linePoints[i] = new Vector2(.5f + .25f*Mathf.Cos(p), .5f + .25f*Mathf.Sin(p));
            origPoints[i] = linePoints[i];
            p += radians;
        }
    }

    public void Update () 
    { 
        float m,t;

        for (int i = 0; i < linePoints.Length; i++) 
        {
            if (i%2 == 0) 
            {
                m = .4f; 
                t = 1.0f;
            } 
            else 
            {
                m = .5f; 
                t = .5f;
            }
            linePoints[i] = (origPoints[i]-new Vector2(.5f, .5f))*(Mathf.Sin(Time.time*t)+Mathf.PI*m)+new Vector2(.5f, .5f);
        }
    }

    public void Awake () 
    {
        lineMaterial = new Material( "Shader \"Lines/Colored Blended\" {" +
            "SubShader { Pass {" +
            "   BindChannels { Bind \"Color\",color }" +
            "   Blend SrcAlpha OneMinusSrcAlpha" +
            "   ZWrite Off Cull Off Fog { Mode Off }" +
            "} } }");
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;

        cam = GameObject.Find("MainCamera").camera;
    }

    public void OnPostRender () 
    {
        if (!drawLines || linePoints == null || linePoints.Count() < 2) { return; }

        float nearClip = cam.nearClipPlane + .00001f; // Add a bit, else there's flickering when the camera moves
        float end = linePoints.Count() - 1;
        float thisWidth = 1.0f/Screen.width * lineWidth * .5f;
        
        lineMaterial.SetPass(0);
        GL.Color(lineColor);
    
        if (lineWidth == 1) 
        {
            GL.Begin(GL.LINES);
            for (int i = 0; i < end; i++) {
                GL.Vertex(cam.ViewportToWorldPoint(new Vector3(linePoints[i].x, linePoints[i].y, nearClip)));
                GL.Vertex(cam.ViewportToWorldPoint(new Vector3(linePoints[i+1].x, linePoints[i+1].y, nearClip)));
            }
        }
        else {
            GL.Begin(GL.QUADS);
            for (int i = 0; i < end; i++) {
                Vector3 perpendicular = (new Vector3(linePoints[i+1].y, linePoints[i].x, nearClip) -
                                     new Vector3(linePoints[i].y, linePoints[i+1].x, nearClip)).normalized * thisWidth;
                Vector3 v1 = new Vector3(linePoints[i].x, linePoints[i].y, nearClip);
                Vector3 v2 = new Vector3(linePoints[i+1].x, linePoints[i+1].y, nearClip);
                GL.Vertex(cam.ViewportToWorldPoint(v1 - perpendicular));
                GL.Vertex(cam.ViewportToWorldPoint(v1 + perpendicular));
                GL.Vertex(cam.ViewportToWorldPoint(v2 + perpendicular));
                GL.Vertex(cam.ViewportToWorldPoint(v2 - perpendicular));
            }
        }
        GL.End();
    }

    public void OnApplicationQuit () 
    {
        DestroyImmediate(lineMaterial);
    }
}




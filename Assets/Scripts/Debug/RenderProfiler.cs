using UnityEngine;
using System.Collections;

// Attach this to an object for it's profile data to be rendered by the RenderDebugger
public class RenderProfiler : DebugProfiler
{
    protected Renderer[] renderers;
    protected Renderer[] childRenderers;

    void Start()
    {
        renderers = GetComponents<Renderer>();
        childRenderers = GetComponentsInChildren<Renderer>();
        if (renderers != null || childRenderers != null)
            RenderDebugger.GetInstance().RegisterProfiler(this);
    }
    void Update()
    {
        if (Debug.isDebugBuild)
        {
            startTime = Time.time;
        }
    }

    void OnRenderObject()
    {
        if (Debug.isDebugBuild)
        {
            postTime = Time.time - startTime;
        }
    }

    public override bool IsOn()
    {
        if (renderers != null)
            foreach (Renderer render in renderers)
                if (render.enabled)
                    return true;
        if (childRenderers != null)
            foreach (Renderer render in childRenderers)
                if (render.enabled)
                    return true;
        return false;
    }

    public override void TurnOn()
    {
        if (renderers != null)
        {
            foreach (Renderer render in renderers)
            {
                render.enabled = true;
            }
        }
        if (childRenderers != null)
        {
            foreach (Renderer render in childRenderers)
            {
                render.enabled = true;
            }
        }
    }

    public override void TurnOff()
    {
        if (renderers != null)
        {
            foreach (Renderer render in renderers)
            {
                render.enabled = false;
            }
        }
        if (childRenderers != null)
        {
            foreach (Renderer render in childRenderers)
            {
                render.enabled = false;
            }
        }
    }
}
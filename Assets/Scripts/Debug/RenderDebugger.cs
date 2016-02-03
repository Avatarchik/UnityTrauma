using UnityEngine;
using System.Collections.Generic;

// Renders GUI to display the data from Profilers
public class RenderDebugger : Dialog
{
    // Style
    // Object name : render time in Milliseconds : button to hide/show

    private Rect windowRect = new Rect(600, 0, 200, 100);
    private Vector2 scrollPosition = new Vector2(0, 0);

    List<DebugProfiler> profilers;

    static private RenderDebugger instance;

    private RenderDebugger() { }

    static public RenderDebugger GetInstance()
    {
        if (instance == null)
            instance = new RenderDebugger();
        return instance;
    }

    public override void Awake()
    {
        instance = this;
        profilers = new List<DebugProfiler>();
    }

    void Start()
    {
        this.SetVisible(false);
    }

    public override void Update()
    {
        base.Update();
        if (Debug.isDebugBuild)
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                this.SetVisible(!this.IsVisible());
            }
        }
    }

    public void RegisterProfiler(DebugProfiler profiler)
    {
        foreach (DebugProfiler item in profilers)
            if (item == profiler)
                return;
        profilers.Add(profiler);
    }

    public override void OnGUI()
    {
        if (IsVisible())
        {
            base.OnGUI();

            windowRect = GUILayout.Window(1424, windowRect, DebugWindow, "Debug Window");
        }
    }

    void DebugWindow(int windowID)
    {
        bool isOn;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
        {
            foreach (DebugProfiler profiler in profilers)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(profiler.gameObject.name);
                    GUILayout.Label(((int)(profiler.TimePosted * 1000)).ToString() + "ms");

                    isOn = profiler.IsOn();
                    if (GUILayout.Button(isOn ? "Hide" : "Show"))
                    {
                        if (isOn)
                            profiler.TurnOff();
                        else
                            profiler.TurnOn();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
    }
}

public class DebugProfiler : MonoBehaviour
{
    protected double startTime = 0.0;
    protected double postTime = 0.0;

    public double TimePosted { get { return postTime; } }

    virtual public bool IsOn() { return false;  }
    virtual public void TurnOn() {}
    virtual public void TurnOff() {}
}
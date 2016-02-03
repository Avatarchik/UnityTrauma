using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class VitalsDialogueMsg : DialogMsg
{
    public VitalsDialogueMsg() : base()
    {
        time = 0.0f;
    }
}

public class VitalsDialogue : Dialog
{
    protected string textbox;
    public GUISkin vitals, test;
    public GUIStyle background, text;

    static VitalsDialogue instance;
    bool character;
    enum Position { close, mother, baby };
    Position position;
    public bool showSP = true;

    int hr, sys, dia, sp = 0;

    float interval;
    float loginterval;
    float vTimeout = 5f;
    float updateTime, logTime;
    bool shouldUpdate = false;

    public VitalsDialogue()
        : base()
    {
        Name = "VitalsDialogue";

        title = "Vitals";
        instance = this;
        position = Position.mother;
        showSP = true;
    }

    public static VitalsDialogue GetInstance()
    {
        return instance;
    }

    void Awake()
    {
		if(vitals != null)
		{
	        background = vitals.GetStyle("Background");
	        text = vitals.GetStyle("Text");
		}
        // create log
        LogMgr.GetInstance().CreateLog("peLog");
    }

    void Start()
    {
        VitalsMgr.GetInstance();

        SetRate(1f);
        SetLogRate(10f);
    }

    public void Update()
    {
        base.Update();
        VitalsMgr.Update();

        if (shouldUpdate)
        {
            if (Time.time > updateTime)
            {
                updateTime = Time.time + interval;
                UpdateVitals();
            }

            if (Time.time > logTime)
            {
                logTime = Time.time + loginterval;
                LogVitals();
            }
        }    
    }

    public void UpdateVitals()
    {
        hr = (int)VitalsMgr.GetInstance().GetCurrent("HR");
        sys = (int)VitalsMgr.GetInstance().GetCurrent("BP_SYS");
        dia = (int)VitalsMgr.GetInstance().GetCurrent("BP_DIA");
        sp = (int)VitalsMgr.GetInstance().GetCurrent("SP");
    }

    public override void OnGUI()
    {
        if (IsVisible() == false)
            return;

        //base.OnGUI();

        if (vitals)
            GUI.skin = vitals;

        x = 5;
        y = Screen.height - background.normal.background.height;
        w = background.normal.background.width;
        h = background.normal.background.height;

        //GUI.Box(new Rect(x, y, w, h), "");

        if (exit == true)
        {
            //if (buttonSkin)
            //    GUI.skin = buttonSkin;
            //if (GUI.Button(new Rect(x + w - 50, y + 10, 30, 20), ""))
            //{
            //    //SetVisible(false);
            //}
        }
        int xPos = 15;

        switch (position)
        {
            case Position.close:
                {
                    xPos = -63;
                    break;
                }
            default:
                {
                    xPos = 0;
                    break;
                }
        }

        int height = background.normal.background.height;
        int top = (Screen.height - height);
        int width = background.normal.background.width;
        int buttonH = 70;
        int buttonW = 20;

        GUI.skin = vitals;

        GUILayout.BeginArea(new Rect(xPos, top, width, height));
        GUILayout.Box("", background, GUILayout.Height(height), GUILayout.Width(width));
        GUILayout.EndArea();

        xPos = 15;

        GUILayout.BeginArea(new Rect(xPos, top, width, height));
        GUILayout.BeginVertical();
        GUILayout.Space(30);

        if (hr == 0)
            GUILayout.Label("---", text);
        else
            GUILayout.Label("" + hr, text);

        if (sys == 0)
            GUILayout.Label("---", text);
        else
            GUILayout.Label("" + sys, text);

        if (dia == 0)
            GUILayout.Label("---", text);
        else
            GUILayout.Label("" + dia, text);

        if (sp == 0)
            GUILayout.Label("---", text);
        else
            GUILayout.Label("" + sp, text);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    public void LogVitals()
    {
        // write vitals log
        PhysicalExamLogItem item;

        Log peLog = LogMgr.GetInstance().FindLog("peLog");
        if (peLog == null)
        {
            LogMgr.GetInstance().CreateLog("peLog");
        }

        if (showSP)
        {
            item = new PhysicalExamLogItem(Time.time, StringMgr.GetInstance().Get("VITALS:SP"), ((int)VitalsMgr.GetInstance().GetCurrent("SP")).ToString());
            LogMgr.GetInstance().FindLog("peLog").Add(item);
        }
        else
        {
            item = new PhysicalExamLogItem(Time.time, StringMgr.GetInstance().Get("VITALS:SP"), "---");
            LogMgr.GetInstance().FindLog("peLog").Add(item);
        }

        item = new PhysicalExamLogItem(Time.time, StringMgr.GetInstance().Get("VITALS:BP_DIA"), ((int)VitalsMgr.GetInstance().GetCurrent("BP_DIA")).ToString());
        LogMgr.GetInstance().FindLog("peLog").Add(item);
        item = new PhysicalExamLogItem(Time.time, StringMgr.GetInstance().Get("VITALS:BP_SYS"), ((int)VitalsMgr.GetInstance().GetCurrent("BP_SYS")).ToString());
        LogMgr.GetInstance().FindLog("peLog").Add(item);
        item = new PhysicalExamLogItem(Time.time, StringMgr.GetInstance().Get("VITALS:HR"), ((int)VitalsMgr.GetInstance().GetCurrent("HR")).ToString());
        LogMgr.GetInstance().FindLog("peLog").Add(item);
    }

    override public void PutMessage(GameMsg msg)
    {
        if (IsActive() == false)
            return;

        VitalsDialogueMsg dialogmsg = msg as VitalsDialogueMsg;
        if (dialogmsg != null)
        {
            // only call base if this message is for us
            base.PutMessage(msg);

            textbox = dialogmsg.text;
        }        
    }

    // this call initiates the Vitals update
    public void Show(bool withUpdate)
    {
        shouldUpdate = withUpdate;
        updateTime = Time.time + interval;
        logTime = Time.time + loginterval;

        // force update first time
        UpdateVitals();
    }

    public void SetRate(float sec)
    {
        this.interval = sec;
    }

    public void SetLogRate(float sec)
    {
        this.loginterval = sec;
    }
}

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TraumaScenarioReport
{
    public TraumaScenarioReport()
    {
    }
	
    public string UserID;
    public string GameID;
    public string SessionID;
    public string Date;
    public string ScenarioName;	
	public CaseOptionData CaseData;
    public ScenarioAssessmentReport Report;
	public List<string> TriggerEvents;

    public void SetInfo(string gameid, string userid, string sessionid) 
    {
        GameID = gameid;
        UserID = userid;
        SessionID = sessionid;
        Date = System.DateTime.Now.ToString();
    }

    public void SetScenarioName(string name)
    {
        ScenarioName = name;
    }
	
    public string ToString()
    {
        // serialize and return
        Serializer<TraumaScenarioReport> serializer = new Serializer<TraumaScenarioReport>();
        return serializer.ToString(this);
    }

    public void Evaluate( ScenarioAssessment scenario )
    {
        // evaluate scenario
        if ( scenario != null )
		{
			// create report
            Report = scenario.Evaluate(LogMgr.GetInstance().GetCurrent().Items);
			// save config data
		}
        // other stuff for the report
        if (Report == null)
            UnityEngine.Debug.LogError("TraumaScenarioReport.Evaluate() : Report=null");
    }

    public void writeCallback(bool status, string data, string error_msg, WWW downlaod)
	{
		// for testing
		LoadDatabase();
	}

    public void SaveDatabase()
    {
		WWWForm form = new WWWForm();
        form.AddField("command", "put");
        form.AddField("userid", UserID);
        form.AddField("sessionid", SessionID);
        form.AddField("scenario", ScenarioName);
        form.AddField("completed", (Report.Success)?"1":"0");
        form.AddField("percent", Report.Percentage.ToString());
        form.AddField("data", ToString());
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,writeCallback);
    }
	
    public void loadCallback(bool status, string data, string error_msg, WWW downlaod)
	{
	}	
	
	public void LoadDatabase()
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "get");
        form.AddField("userid", UserID);
        form.AddField("sessionid", SessionID);
        form.AddField("scenario", ScenarioName);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,loadCallback);
	}
}

public class TraumaReportMgr 
{
    static TraumaReportMgr instance;
    public static TraumaReportMgr GetInstance()
    {
        if (instance == null)
            instance = new TraumaReportMgr();
        return instance;
    }

	public TraumaScenarioReport Report;
	
	public TraumaScenarioReport CreateReport()
	{
		Report = new TraumaScenarioReport();
		Report.SetInfo(LoginMgr.GetInstance().Game,LoginMgr.GetInstance().Username,System.DateTime.Now.TimeOfDay.Ticks.ToString());
		Report.SetScenarioName(CaseConfiguratorMgr.GetInstance().Data.casename);
		Report.Evaluate(AssessmentMgr.GetInstance().GetCurrent());
		Report.CaseData = CaseConfiguratorMgr.GetInstance().Data;
		Report.TriggerEvents = VideoTriggerInfoContainer.Triggers;
		return Report;
	}

	public void SetReport( TraumaScenarioReport report )
	{
		Report = report;
	}
}
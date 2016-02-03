using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class AssessmentMgrDialogMsg : DialogMsg
{
    public ScenarioAssessmentReport Report;
    public List<AssessmentItemMsg> List;
    public AssessmentMgrDialogMsg()
        : base()
    {
    }
}

public class AssessmentMgrDialog : Dialog
{
    public ScenarioAssessmentReport Report;
    List<AssessmentItemMsg> List;

    Vector2 scrollposition;

    static AssessmentMgrDialog instance;
    public AssessmentMgrDialog()
        : base() 
    {
        instance = this;
    }

    static public AssessmentMgrDialog GetInstance()
    {
        if (instance == null)
            UnityEngine.Debug.LogError("AssessmentMgrDialog.GetInstance() : Script not connected!");
        return instance;
    }

    public class GUIReportObject : GUIObject
    {
        public GUIReportObject(ScenarioAssessmentReport report)
        {
            Report = report;
        }

        public ScenarioAssessmentReport Report;

        public GUIStyle GetCheckStyle(bool yesno)
        {
            if (yesno == true)
                return Skin.GetStyle("GreenCheckmark");
            else
                return Skin.GetStyle("RedCheckmark");
        }

        public GUIStyle GetCheckStyle(float score, float percentage)
        {
            if (score >= 1.0f)
                return Skin.GetStyle("GreenCheckmark");
            else if (score >= percentage)
                return Skin.GetStyle("YellowCheckmark");
            else
                return Skin.GetStyle("RedCheckmark");
        }

        public void DrawReport(List<AssessmentListReport> Items, int offset)
        {
            //GUILayout.Label(Report.ToString(), gSkin.GetStyle("HeaderText"), GUILayout.Height(20));
            foreach (AssessmentListReport listreport in Items)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(offset);
                GUILayout.Box("", GetCheckStyle((float)listreport.Score / (float)listreport.ScoreMax, listreport.SuccessPercentage), GUILayout.Width(20), GUILayout.Height(20));
                GUILayout.Label(listreport.Name, Skin.GetStyle("HeaderText"), GUILayout.Width(150), GUILayout.Height(20));
                GUILayout.Label(listreport.Note, Skin.GetStyle("HeaderText"), GUILayout.Width(250), GUILayout.Height(20));
                GUILayout.Label(listreport.Percentage.ToString(), Skin.GetStyle("HeaderText"), GUILayout.Width(35), GUILayout.Height(20));
                GUILayout.Label(listreport.Score + " of " + listreport.ScoreMax, Skin.GetStyle("HeaderText"), GUILayout.Width(50), GUILayout.Height(20));
                GUILayout.Label(listreport.Success.ToString(), Skin.GetStyle("HeaderText"), GUILayout.Width(50), GUILayout.Height(20));
                GUILayout.EndHorizontal();
                foreach (AssessmentItemReport item in listreport.Items)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(offset + 15);
                    //GUILayout.Label("", gSkin.GetStyle("HeaderText"), GUILayout.Width(15));
                    GUILayout.Box("", GetCheckStyle(item.Success), GUILayout.Width(20), GUILayout.Height(20));
                    GUILayout.Label(item.InteractionName, Skin.GetStyle("HeaderText"), GUILayout.Width(200), GUILayout.Height(20));
                    GUILayout.Label(item.Note, Skin.GetStyle("HeaderText"), GUILayout.Width(250), GUILayout.Height(20));
                    GUILayout.Label(item.Result.ToString(), Skin.GetStyle("HeaderText"), GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
                DrawReport(listreport.Lists, offset + 15);
            }
        }

        public override void Execute()
        {
            if (Report != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(0);
                GUILayout.Box("", GetCheckStyle((float)Report.Score / (float)Report.ScoreMax, Report.SuccessPercentage), GUILayout.Width(20), GUILayout.Height(20));
                GUILayout.Label(Report.Name, Skin.GetStyle("HeaderText"), GUILayout.Width(150), GUILayout.Height(20));
                GUILayout.Label(Report.Note, Skin.GetStyle("HeaderText"), GUILayout.Width(250), GUILayout.Height(20));
                GUILayout.Label(Report.Percentage.ToString(), Skin.GetStyle("HeaderText"), GUILayout.Width(35), GUILayout.Height(20));
                GUILayout.Label(Report.Score + " of " + Report.ScoreMax, Skin.GetStyle("HeaderText"), GUILayout.Width(50), GUILayout.Height(20));
                GUILayout.Label(Report.Success.ToString(), Skin.GetStyle("HeaderText"), GUILayout.Width(50), GUILayout.Height(20));
                GUILayout.EndHorizontal();

                DrawReport(Report.Items, 15);
            }
        }
    }

    public bool displayButton = false;
    public void OnGUI()
    {
		return;
		
        if (displayButton)
        {
            // assessment button
            GUILayout.BeginArea(new Rect(UnityEngine.Screen.width - 120, 2, 100, 25));
            if (GUILayout.Button("Assessment", GUILayout.Width(100), GUILayout.Height(15)))
            {
				DialogMsg dmsg = new DialogMsg();
				dmsg.className = "DecisionBreakdown";
				dmsg.xmlName = "AssessmentScreens";
				dmsg.modal = true;
				GUIManager.GetInstance().LoadDialog(dmsg);
            }
            GUILayout.EndArea();
        }
    }

    GUIScreen Screen;

    public override void PutMessage(GameMsg msg)
    {
        AssessmentMgrDialogMsg dmsg = msg as AssessmentMgrDialogMsg;
        if (dmsg != null)
        {
            if (dmsg.List != null)
                List = dmsg.List;
            if ( dmsg.Report != null )
                Report = dmsg.Report;

            // close info dialog
            InfoDialogMsg idmsg = new InfoDialogMsg();
            idmsg.command = DialogMsg.Cmd.close;
            InfoDialogLoader.GetInstance().PutMessage(idmsg);

            // put up assessment dialog
            if (Screen != null)
            {
                GUIManager.GetInstance().Remove(Screen.Parent);
                Screen = null;
            }

            DialogLoader dl = DialogLoader.GetInstance();
            if (dl != null)
            {
                dl.LoadXML("dialog.assessment");
                GUIScreen dp = Screen = dl.ScreenInfo.FindScreen("AssessmentScreen");
                dp.SetLabelText("titleBarText", "Scenario Assessment");
                GUIContainer guiobj = dp.Find("scrollBox") as GUIContainer;
                if (guiobj != null)
                {
                    GUIReportObject reportObj = new GUIReportObject(Report);
                    reportObj.SetSkin(gSkin);
                    guiobj.Elements.Add(reportObj);
                }
            }        
        }
        //base.PutMessage(msg);
    }

}

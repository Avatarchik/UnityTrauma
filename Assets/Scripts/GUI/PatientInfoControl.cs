using UnityEngine;
using System.Collections;

public class PatientInfoControl : MouseOverFeedbackC
{
    public TextAsset xmlFile;

    void OnMouseUpAsButton()
    {
        GUIManager manager = GUIManager.GetInstance();
        if (manager != null && xmlFile != null)
        {
            DialogMsg msg = new DialogMsg();
            msg.command = DialogMsg.Cmd.open;
            msg.modal = true;
            msg.xmlName = xmlFile.name;
            msg.className = "PatientChartGUI";
            manager.LoadDialog(msg);
        }
    }
}

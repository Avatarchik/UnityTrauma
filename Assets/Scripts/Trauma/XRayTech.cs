using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class XRayTech : NurseProvider
{
    public override void Update()
    {
        base.Update();
    }

    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg imsg = msg as InteractStatusMsg;
        if (imsg != null)
        {
            if (imsg.InteractName == "TASK:XRAY:SHOOT:COMPLETE")
            {
                //UnityEngine.Debug.Log("XRayTech : Got TASK:XRAY:SHOOT:COMPLETE MSG!");
                //PUT THE DIALOG HERE
                DialogLoader dl = GetComponent<DialogLoader>();
                if (dl != null)
                {
                    dl.LoadXML("dialog.xray.template");
                }
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ProcedureResident : NurseProvider
{
    float FASTdelay = 0.0f;

    public override void Update()
    {
        base.Update();

        if (FASTdelay != 0.0f)
        {
            if (Time.time > FASTdelay)
            {
                FASTdelay = 0.0f;
                DialogLoader dl = GetComponent<DialogLoader>();
                if (dl != null)
                {
                    dl.LoadXML("dialog.fast.template");
                }
            }
        }
    }

    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg imsg = msg as InteractStatusMsg;
        if (imsg != null)
        {
/*	Now that scripts are launching this dialog, this handler, and the Procedure Resident class, are no longer needed.	
            if (imsg.InteractName == "TASK:FAST:GET:COMPLETE")
            {
                FASTdelay = Time.time + 6.0f;
            }
*/
/* moved to Provider class where stethoscope is declared.
			if (imsg.InteractName == "PROCEDURERESIDENT:STETHOSCOPE:ON")
			{
				if(stethoscope != null)
				{
					stethoscope.GetComponent<MeshToggle>().Toggle(true);
				}
			}
			if (imsg.InteractName == "PROCEDURERESIDENT:STETHOSCOPE:OFF")
			{
				if(stethoscope != null)
				{
					stethoscope.GetComponent<MeshToggle>().Toggle(false);
				}
			}
*/
        }
    }
}

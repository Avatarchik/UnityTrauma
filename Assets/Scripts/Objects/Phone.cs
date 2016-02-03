using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Phone : ObjectInteraction
{
    //public bool inTriage;
    public TextAsset interactionFile;

    public Phone() : base()
    {
        prettyname = "Phone";
    }

    public override void Awake()
    {
        base.Awake();
        //LoadXML("XML/Interactions/Phone");
        LoadXML(interactionFile.name);
    }

    override public void PutMessage(GameMsg msg)
    {
        // call base
        base.PutMessage(msg);

        InteractMsg interactMsg = msg as InteractMsg;
        if (interactMsg != null)
        {
        //    if (interactMsg.map.item == "PHONE:CALL:ATTENDING")
        //    {
        //        // see if the cuff is enabled (anywhere).  we should change this so there is
        //        // only one cuff object!

        //        /* - CHECK CUFF ENABLED
        //        bool cuffEnabled = false;
        //        PatientBloodPressureCuff[] cuffs = GameObject.FindObjectsOfType(typeof(PatientBloodPressureCuff)) as PatientBloodPressureCuff[];
        //        if (cuffs != null)
        //        {
        //            foreach (PatientBloodPressureCuff cuff in cuffs)
        //                if (cuff.enabled && cuff.gameObject.active)
        //                {
        //                    if (cuff.Visible == true)
        //                        cuffEnabled = true;
        //                }
        //        }
        //         * */

        //        string dialog = EFMScenario.GetInstance().GetCurrentTurn().PhoneDialogAttending;
        //        if (dialog == null || dialog == "" )
        //            dialog = "MissingPhone";

        //        // CHECK CUFF ENABLED
        //        //if (cuffEnabled == false)
        //        //    dialog = "DefaultNoBP";
        //        DialogueTree.Instance.GoToDialogue(dialog, true);
        //    }
        //    else if (interactMsg.map.item == "PHONE:CALL:NURSE")
        //    {
        //        string dialog = EFMScenario.GetInstance().GetCurrentTurn().PhoneDialogNurse;
        //        if (dialog == null || dialog == "")
        //            dialog = "MissingPhoneNurse";
        //        DialogueTree.Instance.GoToDialogue(dialog, true);
        //    }
        //    else if (interactMsg.map.item == "PHONE:CALL:ANESTH")
        //    {
        //        string dialog = EFMScenario.GetInstance().GetCurrentTurn().PhoneDialogAnesth;
        //        if (dialog == null || dialog == "" )
        //            dialog = "MissingPhoneAnesth";
        //        DialogueTree.Instance.GoToDialogue(dialog, true);
        //    }
            // special
            HandleResponse(interactMsg);
        }
    }
}

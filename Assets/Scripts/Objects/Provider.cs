//#define DEBUG_INTERACTIONLIST

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ProviderRecord
{
    public ProviderRecord() { }

    public ProviderInfo Info;
}

public class ProviderInfo
{
    public ProviderInfo() { }

    public enum Type { NURSE, DOCTOR };

    public string name;
    public Type type;
    public string sex;
	
	
    public void Debug()
    {
        UnityEngine.Debug.Log("PatientInfo : name=" + name + " : sex=" + sex);
    }
}

public abstract class Provider : Character
{
    public ProviderRecord Record;
//	public GameObject stethoscope; // this is now handled generically with all meshToggle children of the taskCharacter class.
	//public RigController controller;

    public Provider()
    {
    }

    public override void Awake()
    {
		//controller = this.gameObject.AddComponent("RigController") as RigController;
		//controller.parent = this as Provider;
		
		base.Awake();
    }

    public void LoadRecordXML(string filename)
    {
        Serializer<ProviderRecord> serializer = new Serializer<ProviderRecord>();
        Record = serializer.Load(filename);
        Debug(Record);
    }

    public void Debug(ProviderRecord record)
    {
        record.Info.Debug();
    }
	
	public override void HandleInteractMsg(InteractMsg msg)
	{
        base.HandleInteractMsg(msg);
        //msg.map.Debug();

        // respond to med administer
        InteractMsg interact = msg as InteractMsg;
        if (interact != null)
        {
            if (interact.map.item == "MED:ADMINISTER")
            {
                UnityEngine.Debug.Log("Provider.PutMessage(" + interact.map.item + ")");

                MedAdministrationDialogMsg dmsg = new MedAdministrationDialogMsg();
                dmsg.command = DialogMsg.Cmd.open;
                dmsg.modal = true;
                dmsg.provider = this;
                dmsg.patient = null;
                MedAdministrationDialog.GetInstance().PutMessage(dmsg);
            }
        }

    }
/*	mesh toggles now handled generically in taskCharacter class
	public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg imsg = msg as InteractStatusMsg;
        if (imsg != null)
        {
			if (imsg.InteractName == name.ToUpper()+":STETHOSCOPE:ON")
			{
				if(stethoscope != null)
				{
					stethoscope.GetComponent<MeshToggle>().Toggle(true);
				}
			}
			if (imsg.InteractName == name.ToUpper()+":STETHOSCOPE:OFF")
			{
				if(stethoscope != null)
				{
					stethoscope.GetComponent<MeshToggle>().Toggle(false);
				}
			}
        }
    }
*/
}

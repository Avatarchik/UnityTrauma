using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class IntubationTable : ObjectInteraction
{
    public TableState State = TableState.InPlace;
    public enum TableState
    {
        NotInPlace,
        InPlace
    }

    public override void Start()
    {
        base.Start();
        Name = "IntubationTable";
        ObjectManager.GetInstance().RegisterObject(this);
    }

    public bool InPlace
    {
        get { return (State == TableState.InPlace); }
		set { if (value) State = TableState.InPlace; else State = TableState.NotInPlace; }
    }

    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
            if (ismsg.InteractName == "TASK:TABLE:PUTHOME")
            {
                State = TableState.NotInPlace;
                UnityEngine.Debug.Log("IntubationTable.PutMessage() : " + ismsg.InteractName + ", tray is not in place = <" + InPlace + ">");
            }
            if (ismsg.InteractName == "TASK:TABLE:PUTBEDSIDE:COMPLETE")
            {
                State = TableState.InPlace;
                UnityEngine.Debug.Log("IntubationTable.PutMessage() : " + ismsg.InteractName + ", tray is in place = <" + InPlace + ">");
            }
        }

        TaskRequestedMsg trmsg = msg as TaskRequestedMsg;
        if (trmsg != null)
        {
            if (trmsg.Request == "TASK:TABLE:PUTBEDSIDE")
            {
                UnityEngine.Debug.Log("IntubationTable.PutMessage() : TaskRequestMsg=<" + trmsg.Name + "," + trmsg.Request + ">");
            }
        }
    }
}

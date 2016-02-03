using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class IntubationTray : ObjectInteraction
{
    public TrayState State = TrayState.TrayNotInPlace;
    public enum TrayState
    {
        TrayNotInPlace,
        TrayInPlace
    }

    public override void Start()
    {
        base.Start();
        Name = "IntubationTray";
        ObjectManager.GetInstance().RegisterObject(this);
    }

    public bool InPlace
    {
        get { return (State == TrayState.TrayInPlace); }

		set { if (value) State = TrayState.TrayInPlace; else State = TrayState.TrayNotInPlace; }
    }

    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
            if (ismsg.InteractName == "PREP:INTUBATION:COMPLETE")
            {
                State = TrayState.TrayInPlace;
                UnityEngine.Debug.Log("IntubationTray.PutMessage() : PREP:INTUBATION:COMPLETE, tray in place = <" + InPlace + ">");
            }
        }

        TaskRequestedMsg trmsg = msg as TaskRequestedMsg;
        if (trmsg != null)
        {
            UnityEngine.Debug.Log("IntubationTray.PutMessage() : TaskRequestMsg=<" + trmsg.Name + "," + trmsg.Request + ">");
        }
    }
}

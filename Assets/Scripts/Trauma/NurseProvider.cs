using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class NurseProvider : Provider
{
    public override void Start()
    {
        base.Start();

        LoadInteractionXML(XMLName);
    }

    public bool WasPreviousInteraction(string name)
    {
        List<InteractLogItem> items = LogMgr.GetInstance().FindLogItems<InteractLogItem>();
        foreach (InteractLogItem item in items)
        {
            if (item.InteractName == name)
            {
                UnityEngine.Debug.LogWarning("NurseProvider.WasPreviousInteraction(" + name + ") : <" + Name + "> already did this interaction!");
                return true;
            }
        }
        return false;
    }
}
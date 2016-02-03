using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// container for meds

public class Med
{
    public Med() 
    {
        NeedsOrder = false;
        OrderTime = 0.0f;
    }

    public string Name;
    public bool NeedsOrder;
    public float OrderTime;
    public List<MedDelivery> DeliveryMethods;
    public List<MedEffect> Effects;
    public InteractionMap InteractionMap;

    public List<string> GetDeliveryMethods()
    {
        List<string> names = new List<string>();
        if (DeliveryMethods != null)
        {
            foreach (MedDelivery delivery in DeliveryMethods)
                names.Add(delivery.Type.ToString());
        }

        return names;
    }

    public MedMgr.MedType GetDeliveryType(string name)
    {
        foreach (MedDelivery delivery in DeliveryMethods)
        {
            if (delivery.Type.ToString() == name)
                return delivery.Type;
        }
        return MedMgr.MedType.NONE;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("Med : Name=" + Name + " : DeliveryMethods=" + DeliveryMethods.Count);
        foreach (MedDelivery delivery in DeliveryMethods)
        {
            delivery.Debug(Name);
        }
        UnityEngine.Debug.Log("Med : Name=" + Name + " : Effets=" + Effects.Count);
        foreach (MedEffect effect in Effects)
        {
            effect.Debug(Name);
        }
    }
}

public class MedDelivery
{
    public MedDelivery() { }

    public MedMgr.MedType Type;
    public int DosageLo;
    public int DosageHi;
    public int DosageInc;
    public int Recommended;

    public void Debug( string name )
    {
        UnityEngine.Debug.Log("Delivery<" + name + "> : Type=" + Type + " : Lo=" + DosageLo + " : Hi=" + DosageHi + " : Inc=" + DosageInc);
    }
}

public class MedEffect 
{
    public enum EffectType { None, StateChange, ValueChange };
    public enum EffectChangeType { None, RelativeChange, AbsoluteChange };
    public enum EffectBaselineType { None, Time, Value, Both };

    public EffectType Type;
    public EffectChangeType ChangeType;
    public string Name;  // can be state name or vital name (ie. HR)
    public float Time;  // time to change

    public float Value; // value change

    public EffectBaselineType BaselineType;
    public float BaseDosage;  // baseline dosage for this effect
    public float BaseWeight;

    public MedEffect() { }

    public void Debug( string name )
    {
        UnityEngine.Debug.Log("Effect<" + name + "> : Name=" + Name + " : Type=" + Type + " : Change=" + ChangeType + " : Time=" + Time + " : Amount=" + Value + " : BaselineType=" + BaselineType + " : Baseline=" + BaseDosage + " : BaseWeight=" + BaseWeight);
    }
}

public class MedAdministerMsg : GameMsg
{
    public Med Med;
    public MedMgr.MedType Type;
    public string Target;
    public string Who;
    public int Dosage;
    public float Time;
}

public class MedOrderMsg : GameMsg
{
    public Med Med;
    public string Who;
    public float Time;
}

public class MedOrderItem : LogItem
{
    public Med Med;
    public string Who;

    public MedOrderItem()
        : base()
    { }

    public MedOrderItem(float time, Med med, string who)
        : base()
    {
        this.Med = med;
        this.Who = who;
    }
}

public class MedAdministerLogItem : LogItem
{
    public Med Med;
    public MedMgr.MedType Type;
    public string Who;
    public float Dosage;

    public MedAdministerLogItem()
        : base()
    { }

    public MedAdministerLogItem(float time, Med med, MedMgr.MedType type, string who, float dosage)
        : base()
    {
        this.Med = med;
        this.Type = type;
        this.Who = who;
        this.Dosage = dosage;
    }
}

public class MedMgr
{
    public enum MedType { NONE, IV, PUSH, PILL, SUBQ };

    public string Name;
    public List<Med> Meds;
    public List<Med> Inventory;
    public List<MedOrderMsg> Ordered;

    protected List<Med> Administered;

    static MedMgr instance;

    public MedMgr()
    {
        Name = "MedMgr";
        Meds = new List<Med>();
        Administered = new List<Med>();
        Inventory = new List<Med>();
        Ordered = new List<MedOrderMsg>();

        instance = this;
    }

    static public MedMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new MedMgr();
            instance.LoadXML("XML/Meds");
        }

        return instance;
    }

    public List<string> GetMedNames()
    {
        List<string> names = new List<string>();

        if (Meds != null)
        {
            foreach (Med med in Meds)
            {
                names.Add(med.Name);
            }
        }
        return names;
    }

    public Med GetMed(string name)
    {
        if (Meds == null)
            return null;

        foreach (Med med in Meds)
        {
            if (med.Name == name)
                return med;
        }
        return null;
    }

    public void PutMessage(GameMsg msg)
    {
        MedAdministerMsg medmsg = msg as MedAdministerMsg;
        if (medmsg != null)
        {
            // check inventory (make sure med has been ordered)
            if (CheckInventory(medmsg.Med) == false)
            {
                // put up QI dialog, med is not ready
                return;
            }

            // save as administered
            Administered.Add(medmsg.Med);
            // log interaction
            MedAdministerLogItem logitem = new MedAdministerLogItem(Time.time, medmsg.Med, medmsg.Type, medmsg.Who, medmsg.Dosage);
            LogMgr.GetInstance().Add(logitem);
            // broadcast
            ObjectManager.GetInstance().PutMessage(medmsg);
        }

        // add order
        MedOrderMsg ordermsg = msg as MedOrderMsg;
        if (ordermsg != null)
        {
            Ordered.Add(ordermsg);
            // log interaction
            MedOrderItem logitem = new MedOrderItem(Time.time, ordermsg.Med, ordermsg.Who);
            LogMgr.GetInstance().Add(logitem);
        }
    }

    public bool CheckInventory( Med med )
    {
        // check if med needs to be ordered
        if (med.NeedsOrder == false)
            return true;

        // first go through all ordered meds and add them to inventory if they
        // passed the order time
        foreach (MedOrderMsg ordermsg in Ordered)
        {
            // check if it has been long enough
            if ((ordermsg.Time + ordermsg.Med.OrderTime) > Time.time)
            {
                // check to see if med is already in inventory list
                if (Inventory.Contains(ordermsg.Med) == false)
                {
                    // add it
                    Inventory.Add(ordermsg.Med);
                }               
            }
        }

        // now check inventory 
        return Inventory.Contains(med);
    }

    public void LoadXML(string filename)
    {
        Serializer<List<Med>> serializer = new Serializer<List<Med>>();
        Meds = serializer.Load(filename);
        if (Meds != null)
        {
#if DEBUG_MED_MGR
            Debug(Meds);
#endif
        } 
        else
            UnityEngine.Debug.Log("MedMgr.LoadXML() : Meds = null!");
    }

    public void Debug(List<Med> meds)
    {
        foreach (Med med in meds)
            med.Debug();
    }
}




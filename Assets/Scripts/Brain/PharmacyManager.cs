using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Change idea:
/// A data file that list the individual drug, dose, delivery, and interval to create a condition.
/// The GUI takes that data and merges all a drug's possible entries together to allow for the wrong selection.
/// </summary>

public class PharmacyManager : BaseObject
{
    public enum DoseType { MGs, CCs }
    public enum DeliveryType { Oral, Intermuscular, IVSP, IVBolus, Depository }
    public enum IntervalType { Dose, Min, Hour, PRN }

    [System.Serializable]
    public class DrugMerge
    {
        [System.Serializable]
        public class DoseOrder
        {
            public int amount;
            public DoseType type;
        }

        [System.Serializable]
        public class DeliveryOrder
        {
            public DeliveryType type;
        }

        [System.Serializable]
        public class IntervalOrder
        {
            public int amount;
            public IntervalType type;
        }

        public string drugName;
        //public int drugID;

        public List<DoseOrder> doses;
        public List<DeliveryOrder> delivery;
        public List<IntervalOrder> intervals;

        public DrugMerge()
        {
            doses = new List<DoseOrder>();
            delivery = new List<DeliveryOrder>();
            intervals = new List<IntervalOrder>();
        }
    }

    [System.Serializable]
    public class DrugEntry
    {
        public string name;
        public int dosage;
        public DoseType doseType;
        public DeliveryType delivery;
        public int time;
        public IntervalType interval;
        public string behavior;
        public string interaction;
    }

    static PharmacyManager instance;
    ObjectManager objMgr;

    static public PharmacyManager GetInstance()
    {
        return instance;
    }

    public TextAsset dataFile;
    public DrugEntry[] drugEntries;
    List<DrugMerge> drugMerge;

    Character character;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            enabled = false;
        }
    }

    void Start()
    {
        if (dataFile == null)
        {
            enabled = false;
            return;
        }

        // Grab the data
        Serializer<DrugEntry[]> serializer = new Serializer<DrugEntry[]>();
        drugEntries = serializer.Load("XML/DrugData/" + dataFile.name);

        if (drugEntries != null)
        {
            drugMerge = new List<DrugMerge>();
            // Form merger list
            foreach (DrugEntry entry in drugEntries)
            {
                // Check if the drug is in the merge list
                bool found = false;
                foreach(DrugMerge merge in drugMerge)
                    if (entry.name == merge.drugName)
                    {
                        found = true;
                        break;
                    }

                // Create entry if not found
                if(!found)
                {
                    DrugMerge newItem = new DrugMerge();
                    newItem.drugName = entry.name;
                    drugMerge.Add(newItem);
                }
            }

            // Compulate data
            foreach (DrugMerge merge in drugMerge)
            {
                foreach (DrugEntry entry in drugEntries)
                {
                    if (merge.drugName == entry.name)
                    {
                        bool found = false;

                        // Check dosage
						if(merge.doses != null)
						{
	                        foreach (DrugMerge.DoseOrder order in merge.doses)
	                        {
	                            if (entry.dosage == order.amount && entry.doseType == order.type)
	                            {
	                                found = true;
	                                break;
	                            }
	                        }
						}
                        if (!found)
                        {
                            // Add data to this merge entry
                            DrugMerge.DoseOrder newOrder = new DrugMerge.DoseOrder();
                            newOrder.amount = entry.dosage;
                            newOrder.type = entry.doseType;
                            merge.doses.Add(newOrder);
                        }
                        found = false;

                        // Check delivery
						if(merge.delivery != null)
						{
	                        foreach (DrugMerge.DeliveryOrder order in merge.delivery)
	                        {
	                            if (entry.delivery == order.type)
	                            {
	                                found = true;
	                                break;
	                            }
	                        }
						}
                        if (!found)
                        {
                            DrugMerge.DeliveryOrder newOrder = new DrugMerge.DeliveryOrder();
                            newOrder.type = entry.delivery;
                            merge.delivery.Add(newOrder);
                        }
                        found = false;

                        // Check Interval
						if(merge.intervals != null)
						{
	                        foreach (DrugMerge.IntervalOrder order in merge.intervals)
	                        {
	                            if (entry.time == order.amount && entry.interval == order.type)
	                            {
	                                found = true;
	                                break;
	                            }
	                        }
						}
                        if (!found)
                        {
                            DrugMerge.IntervalOrder newOrder = new DrugMerge.IntervalOrder();
                            newOrder.amount = entry.time;
                            newOrder.type = entry.interval;
                            merge.intervals.Add(newOrder);
                        }
                    }
                }
            }
        }

        objMgr = ObjectManager.GetInstance();
        if (objMgr != null)
            objMgr.RegisterObject(this);
        else
            return;

        InteractionMgr interactionMgr = InteractionMgr.GetInstance();
        if(interactionMgr != null)
        {
            character = gameObject.AddComponent<Character>();
            character.ItemResponse = new List<InteractionMap>();
            foreach (InteractionMap map in interactionMgr.Interactions)
            {
                if (map.item == "MED:ORDERDRUGS")
                {
                    character.ItemResponse.Add(map);
                    character.AllMaps = character.ItemResponse;
                    objMgr.RegisterObject(character);

                    character.Name = "PharMgr";
                    character.prettyname = "PharMgr";
                    //character.Awake();
                    character.Start();

                    return;
                }
            }
        }
    }

    public DrugMerge GetDrug(string name)
    {
        foreach (DrugMerge drug in drugMerge)
        {
            if (drug.drugName == name)
                return drug;
        }
        return null;
    }

    public DrugMerge GetDrug(int number)
    {
        if (number < drugMerge.Count && number >= 0)
            return drugMerge[number];
        return null;
    }

    //public DrugOrder GetDrugID(int id)
    //{
    //    foreach (DrugOrder drug in drugOrders)
    //    {
    //        if (drug.drugID == id)
    //            return drug;
    //    }
    //    return null;
    //}

    public DrugEntry CheckItem(string name, int dosage, DoseType doseType, DeliveryType delivery, int time, IntervalType interval)
    {
        foreach (DrugEntry entry in drugEntries)
        {
            if (entry.name == name && entry.dosage == dosage && entry.doseType == doseType && entry.delivery == delivery &&
                entry.time == time && entry.interval == interval)
                return entry;
        }
        return null;
    }

    public DrugEntry CheckItem(string name, DrugMerge.DoseOrder doseTest, DrugMerge.DeliveryOrder deliveryTest, DrugMerge.IntervalOrder intervalTest)
    {
        //CheckOrder(GetDrug(name), doseTest, deliveryTest, intervalTest);
        return CheckItem(name, doseTest.amount, doseTest.type, deliveryTest.type, intervalTest.amount, intervalTest.type);
    }

    //public bool CheckOrder(DrugMerge drug, DrugMerge.DoseOrder doseTest, DrugMerge.DeliveryOrder deliveryTest, DrugMerge.IntervalOrder intervalTest)
    //{
    //    if (drug != null)
    //    {
    //        // Check that the entries are valid for this drug
    //        bool checkDose = false, checkDelivery = false, checkInterval = false;
    //        foreach (DrugMerge.DoseOrder dose in drug.doses)
    //        {
    //            if (dose == doseTest)
    //            {
    //                checkDose = true;
    //                continue;
    //            }
    //        }

    //        foreach (DrugMerge.DeliveryOrder delivery in drug.delivery)
    //        {
    //            if (delivery == deliveryTest)
    //            {
    //                checkDelivery = true;
    //                continue;
    //            }
    //        }

    //        foreach (DrugMerge.IntervalOrder interval in drug.intervals)
    //        {
    //            if (interval == intervalTest)
    //            {
    //                checkInterval = true;
    //                continue;
    //            }
    //        }

    //        return checkDose && checkDelivery && checkInterval;
    //    }
    //    return false;
    //}

    public List<string> GetDrugList()
    {
        if (drugMerge != null)
        {
            int length = drugMerge.Count;
            List<string> list = new List<string>();
            for (int i = 0; i < length; i++)
                list.Add(drugMerge[i].drugName);

            list.TrimExcess();
            return list;
        }
        else
            return null;
    }

    List<DrugEntry> queuedDrugs = new List<DrugEntry>();
    public void QueueDrugs(List<DrugEntry> queuing)
    {
        if (queuing != null)
            queuedDrugs = queuing;
    }

    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        string text = "";
        //TaskCompleteMsg tcMsg = msg as TaskCompleteMsg;
        //if (tcMsg != null)
        //    text = tcMsg.TaskName;

        InteractStatusMsg itMsg = msg as InteractStatusMsg;
        if (itMsg != null)
            text = itMsg.InteractName;

        if (text.Length > 0)
        {
            switch (text)
            {
                case "TASK:RSIMEDS:INJECT:COMPLETE":
                    {
                        // Release the queue
                        VitalsBehaviorManager vMgr = VitalsBehaviorManager.GetInstance();
                        if (vMgr != null)
                        {
                            foreach (DrugEntry entry in queuedDrugs)
                            {
                                vMgr.AddBehavior(entry.behavior);
                            }
                        }
                    }
                    break;
                //case "TASK:RSIMEDS:GODRAW":
                //    {
                //        // Summon the DrugList GUI
                //        DrugListDialogLoader dldl = DrugListDialogLoader.GetInstance();
                //        if (dldl != null)
                //        {
                //            DialogMsg diamsg = new DialogMsg();
                //            diamsg.command = DialogMsg.Cmd.open;
                //            diamsg.modal = true;
                //            dldl.PutMessage(diamsg);
                //        }
                //    }
                //    break;
            };
        }
    }

    public void DialogClosed()
    {
        // Activate interaction!
        if(queuedDrugs.Count > 0 && Brain.GetInstance() != null)
            character.PutMessage(new InteractMsg(character.gameObject, character.ItemResponse[0], true));
    }
}
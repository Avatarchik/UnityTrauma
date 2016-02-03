//#define DEBUG_ASSESSMENT

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

public class AssessmentMsg : GameMsg
{}

public class AssessmentItemMsg : AssessmentMsg
{
    public InteractionMap Map;
    public AssessmentList List;
    public AssessmentItem Item;

    public AssessmentItemReport Report;

    public string PrettyPrint()
    {
        return "Assessment<" + List.Name + "," + Item.InteractionName + "> : " + Report.PrettyPrint();
    }
}

public class AssessmentListMsg : AssessmentMsg
{
    public AssessmentList List;

    public string PrettyPrint()
    {
        return "Assessment<" + List.Name + "> : Completed=" + List.Completed;
    }
}

public class AssessmentScenarioMsg : AssessmentMsg
{
    public ScenarioAssessment Scenario;

    public string PrettyPrint()
    {
        return "Scenario<" + Scenario.Name + "> : Completed=" + Scenario.Completed;
    }
}

public class AssessmentItemNotFoundMsg : AssessmentMsg
{
	public string InteractName;
}

public class MedAssessmentItem : AssessmentItem
{
    public MedAssessmentItem() : base() 
    {
        InteractionName = "MED:ADMINISTER";
    }

    public string MedName;
    public string NoteWrongType;
    public string NoteDosageHi;
    public string NoteDosageLo;
    public MedMgr.MedType Type;
    public int DosageLo;
    public int DosageHi;
}

public class AssessmentItem 
{
    public string Name;
    public string InteractionName;        
	public string Note;
    public string NoteSuccess;
    public string NoteFailure;
    public int Score;
    // conditions
    public bool Fatal;
    public int CountRequired;
	public int EvaluateOnCount;
    public bool NoDuplicates;
    public int TimeLimit;
	public string PrereqFlags;
	public List<string> Prereq;	// prereqs for inOrder
	public List<string> PrereqIgnore; // ignore interactions for inOrder
    public MedAssessmentItem MedInfo;
    public bool Completed;
    public bool InOrder;
	public bool CriticalAction;
	public bool Inappropriate;
	public bool Ignore;		// ignore reporting
	public bool Trigger;  	// set to clear Ignore flag on interaction
	public List<string> TriggeredLists;	// List<string> of activated assessment lists 
	public int Level; 		// embedded level
	public string Substitutes;	// string or comma seperated interactions that satisfies this assessment requirement

    public List<string> ActivateOnSuccess;
    public List<string> ActivateOnFail;

    public enum ResultType
    {
        None,
        Required,
		Inappropriate,
        FailFatal,
        FailMissing,
        FailDuplicate,
        FailCount,
        FailPrereq,
        FailTime,
        FailMedDosage,
        FailMedType,
        FailInOrder,
		Ignore,
    }
	
    public AssessmentItem()
    {
		EvaluateOnCount=-1;
        CountRequired = -1;
        TimeLimit = -1;
        NoteSuccess = "No Success Note";
        NoteFailure = "No Failure Note";
        Score = 0;
        NoDuplicates = false;
        Completed = false;
		CriticalAction = false;
		Trigger = false;
		TriggeredLists = null;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("AssessmentItem : Name=" + InteractionName + " : NoteSuccess=" + NoteSuccess + " : NoteFailure=" + NoteFailure + " : Score=" + Score);
        if ( MedInfo != null )
            UnityEngine.Debug.Log("MedInfo : Name=" + MedInfo.MedName + " : Type=" + MedInfo.Type.ToString() + " : DosageLo=" + MedInfo.DosageLo + " : DosageHi=" + MedInfo.DosageHi);
    }

    public List<string> CheckActivations( AssessmentItemReport report )
    {
        // activate on success
        if (report.Result == ResultType.Required && (ActivateOnSuccess != null && ActivateOnSuccess.Count > 0))
        {
            return ActivateOnSuccess;
        }
        // activate on failure
        if (report.Result != ResultType.Required && (ActivateOnFail != null && ActivateOnFail.Count > 0))
        {
            return ActivateOnFail;
        }
        return null;
    }

    public AssessmentItemReport EvaluateMed(List<LogItem> log)
    {
        // find item
        List<MedAdministerLogItem> logitems = FindMedLogItems(MedInfo, log);

        if (logitems.Count == 0)
        {
            // add missing if can't be found
            AssessmentItemReport itemReport = new AssessmentItemReport(this);
            itemReport.Result = AssessmentItem.ResultType.FailMissing;
            itemReport.Note = NoteFailure;
            itemReport.Score = 0;   // no score
            itemReport.Success = false;
            return itemReport;
        }
        else
        {
            // DON'T HANDLE DUPLICATES YET

            // check delivery type
            if (logitems[0].Type == MedInfo.Type)
            {
                // check dosage
                if (logitems[0].Dosage >= MedInfo.DosageLo && logitems[0].Dosage <= MedInfo.DosageHi)
                {
                    // SUCCESS CASE!
                    this.Completed = true;

                    // add the report, set duplicates (if any)
                    AssessmentItemReport itemReport = new AssessmentItemReport(this);
                    itemReport.Result = AssessmentItem.ResultType.Required;
                    itemReport.Note = NoteSuccess;
                    itemReport.Score = Score;
                    itemReport.Success = true;
                    itemReport.Duplicates = logitems.Count - 1;
                    return itemReport;
                }
                else
                {
                    // fail, bad dosage
                    AssessmentItemReport itemReport = new AssessmentItemReport(this);
                    itemReport.Result = AssessmentItem.ResultType.FailMedDosage;
                    itemReport.Note = (logitems[0].Dosage < MedInfo.DosageLo)? MedInfo.NoteDosageLo : MedInfo.NoteDosageHi;
                    itemReport.Score = 0;   // no score
                    itemReport.Success = false;
                    return itemReport;
                }
            }
            else
            {
                // fail, wrong delivery
                AssessmentItemReport itemReport = new AssessmentItemReport(this);
                itemReport.Result = AssessmentItem.ResultType.FailMedType;
                itemReport.Note = MedInfo.NoteWrongType;
                itemReport.Score = 0;   // no score
                itemReport.Success = false;
                return itemReport;
            }
        }
    }

	// this method checks to see if there are other conditions which would satisfy this interaction
	// if found then the method returns true
	public bool CheckSubstitutes( List<LogItem> log )
	{
		if ( Substitutes == null )
			return false;

		// read Substitutes string
		string[] interactions = this.Substitutes.Split(',');
		// iterate 
		foreach( string item in interactions )
		{
			List<LogItem> found = FindInteractLogItems(item,log);
			if ( found.Count > 0 )
				return true;
		}
		return false;
	}

    public AssessmentItemReport EvaluateNormal( List<LogItem> log)
    {
        // find item
        List<LogItem> found = FindInteractLogItems(InteractionName,log);

		// EVALUATE ON COUNT
		// This value means we will ignore checking this interaction until
		// found.Count = EvaluateOnCount... this allows duplicate events
		// that are only checked when needed.
		if (EvaluateOnCount != -1 && EvaluateOnCount > found.Count )
		{
             return null;
		}
		
        // add items found or not found
        if (found.Count == 0 )
        {
			// CHECK SUBSTITUTES 
			if ( CheckSubstitutes(log) == true )
			{
				// we have a substitute interaction so return NULL which 
				// removes this item from the report.
				return null;
			}
			// add missing if can't be found
            AssessmentItemReport itemReport = new AssessmentItemReport(this);
            itemReport.Result = AssessmentItem.ResultType.FailMissing;
			itemReport.CriticalAction = CriticalAction;
            itemReport.Note = NoteFailure;
            itemReport.Score = 0;   // no score
            itemReport.Success = false;
			itemReport.Time = "00:00";
            return itemReport;
        }
        else
        {
            // FATAL
            if (Fatal == true)
            {
                // add the report
                AssessmentItemReport itemReport = new AssessmentItemReport(this);
                itemReport.Result = AssessmentItem.ResultType.FailFatal;
				itemReport.CriticalAction = CriticalAction;
                itemReport.Note = NoteFailure;
                itemReport.Score = 0;
                itemReport.Success = false;
                itemReport.Duplicates = found.Count - 1;
				itemReport.Time = found[0].GetTimeString();
                return itemReport;
            }
            // NO DUPLICATES
            if (NoDuplicates == true && found.Count > 1)
            {
                // add the report, set duplicates (if any)
                AssessmentItemReport itemReport = new AssessmentItemReport(this);
                itemReport.Result = AssessmentItem.ResultType.FailDuplicate;
				itemReport.CriticalAction = CriticalAction;
                itemReport.Note = NoteFailure;
                itemReport.Score = 0;
                itemReport.Success = false;
                itemReport.Duplicates = found.Count - 1;
				itemReport.Time = found[0].GetTimeString();
                return itemReport;
            }
            // COUNT REQUIRED
            if (CountRequired != -1 && CountRequired > found.Count)
            {
                // add the report
                AssessmentItemReport itemReport = new AssessmentItemReport(this);
                itemReport.Result = AssessmentItem.ResultType.FailCount;
				itemReport.CriticalAction = CriticalAction;
                itemReport.Note = NoteFailure;
                itemReport.Score = 0;
                itemReport.Success = false;
                itemReport.Duplicates = found.Count - 1;
				itemReport.Time = found[0].GetTimeString();
                return itemReport;
            }
            // TIME LIMIT
            if (TimeLimit != -1)
            {
                // check if the first event happened after the time limit
                if (found[0].Time > (float)TimeLimit)
                {
                    // add the report
                    AssessmentItemReport itemReport = new AssessmentItemReport(this);
                    itemReport.Result = AssessmentItem.ResultType.FailTime;
					itemReport.CriticalAction = CriticalAction;
                    itemReport.Note = NoteFailure;
                    itemReport.Score = 0;
                    itemReport.Success = false;
                    itemReport.Duplicates = found.Count - 1;
					itemReport.Time = found[0].GetTimeString();
                    return itemReport;
                }
            }

			// PREREQ
            if (Prereq != null && Prereq.Count > 0)
            {
				bool orCondition = false;
				bool emptyCondition = false;
				// CHECK FOR OR FLAG
				if ( PrereqFlags != null && PrereqFlags.Contains ("%%flags") )
				{
					// parse flags
					string value;
					TokenMgr.GetToken (PrereqFlags,"%%flags",out value);
					string[] flags = value.Split (',');
					foreach( string item in flags )
					{
						if ( item.ToLower() == "or" )
							orCondition = true;
						if ( item.ToLower() == "empty" )
							emptyCondition = true;
					}
				}

				// tmp storage for results
				List<AssessmentItemReport> prereqResults=new List<AssessmentItemReport>();

				// search for all the prereq
                foreach (string prereq in Prereq)
                {
                    // first check to see if this is a completed objective
                    if (IsCompletedAssessmentList(prereq) == false)
                    {
                        // check to see if this is an "InOrder" item...if so then the prereq 
                        // must have been the last item before this one
                        if (InOrder == true)
                        {
							// WasInOrder now checks a list.... if there is one item the item
							// has to be the last one, if there are more than one item then only
							// one of them has to be the last one.
							if (WasInOrder(this,Prereq,log) == false)
							{
								// fail, item wasn't last item
								AssessmentItemReport itemReport = new AssessmentItemReport(this);
								itemReport.Result = AssessmentItem.ResultType.FailInOrder;
								itemReport.CriticalAction = CriticalAction;
								itemReport.Note = NoteFailure;
								itemReport.Score = 0;
								itemReport.Success = false;
								itemReport.Duplicates = 0;
								itemReport.Prereq = new List<string>();
								itemReport.Prereq.Add(prereq);
								itemReport.Time = found[0].GetTimeString();
								return itemReport;
							}
                        }
                        // get all the prereq items
                        List<LogItem> items = FindInteractLogItems(prereq, log);
                        // make sure first item is before this event
						if ( emptyCondition == false )
						{
							// fail, no items
	                        if (items != null && items.Count > 0)
	                        {
	                            // check time of event, make sure it happened before the found event
								UnityEngine.Debug.LogError ("items[0]=" + items[0].PrettyPrint () + " : found=" + found[0].PrettyPrint ());
	                            if (items[0].Time > found[0].Time)
	                            {
	                                // fail, no items before time
	                                // add the report, set duplicates (if any)
	                                AssessmentItemReport itemReport = new AssessmentItemReport(this);
	                                itemReport.Result = AssessmentItem.ResultType.FailPrereq;
									itemReport.CriticalAction = CriticalAction;
	                                itemReport.Note = NoteFailure;
	                                itemReport.Score = 0;
	                                itemReport.Success = false;
	                                itemReport.Duplicates = found.Count - 1;
	                                itemReport.Prereq = new List<string>();
	                                itemReport.Prereq.Add(prereq);
									itemReport.Time = found[0].GetTimeString();
									prereqResults.Add(itemReport);
	                                //return itemReport;
	                            }
	                        }
	                        else
	                        {
		                        // add the report
		                        AssessmentItemReport itemReport = new AssessmentItemReport(this);
		                        itemReport.Result = AssessmentItem.ResultType.FailPrereq;
								itemReport.CriticalAction = CriticalAction;
		                        itemReport.Note = NoteFailure;
		                        itemReport.Score = 0;
		                        itemReport.Success = false;
		                        itemReport.Duplicates = found.Count - 1;
		                        itemReport.Prereq = new List<string>();
		                        itemReport.Prereq.Add(prereq);
								itemReport.Time = found[0].GetTimeString();
								prereqResults.Add(itemReport);
								//return itemReport;
	                        }
						}
					}
                }

				// if we have some results they are bad, handle it based on OR flag
				if ( prereqResults.Count > 0 )
				{
					if ( orCondition == true )
					{
						// with the OR condition, one condition should have passed, so the
						// count shouldn't be equal to the number of prereqs
						if ( prereqResults.Count == Prereq.Count )
							// we're equal so send the first error
							return prereqResults[0];
					}
					else
						// if we don't have an OR condition then any failure blocks us, 
						// send the first one
						return prereqResults[0];
				}					
			}

			// NO ERRORS, SEND SUCCESS CASE
            {
                  // add the report
                AssessmentItemReport itemReport = new AssessmentItemReport(this);
				// an inappropriate action succeeds like a required interaction but
				// it has a negative score.
				if ( this.Inappropriate == true )
					itemReport.Result = AssessmentItem.ResultType.Inappropriate;
				else
				{
                	itemReport.Result = AssessmentItem.ResultType.Required;
					this.Completed = true;
				}
				// 
				itemReport.CriticalAction = CriticalAction;
                itemReport.Note = NoteSuccess;
                itemReport.Score = Score;
                itemReport.Success = true;
                itemReport.Duplicates = found.Count - 1;
				// handle count required, if we have a required count 
				// then use the time of that item
				if ( CountRequired != -1 )
					itemReport.Time = found[CountRequired-1].GetTimeString();
				else if ( EvaluateOnCount != -1 )
					itemReport.Time = found[EvaluateOnCount-1].GetTimeString();
				else
					itemReport.Time = found[0].GetTimeString();
                return itemReport;
            }
        }
    }

    public AssessmentItemReport Evaluate(List<LogItem> log)
    {
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("AssessmentItemReport.Evaluate() : Name=" + InteractionName);
#endif

        // special case, meds
        switch (InteractionName)
        {
            case "MED:ADMINISTER":
                return EvaluateMed(log);

            default:
                return EvaluateNormal(log);
        }
    }

    public bool IsCompletedAssessmentList(string name)
    {
        foreach (AssessmentList list in AssessmentMgr.GetInstance().Scenario.AssessmentLists)
        {
            if (list.Name == name && list.Completed == true)
                return true;
        }
        return false;
    }
    
    public List<LogItem> FindInteractLogItems(string name, List<LogItem> log)
    {
        List<LogItem> found = new List<LogItem>();

        // search list for the assessment item
        foreach (LogItem logitem in log)
        {
            InteractStatusItem istatusitem = logitem as InteractStatusItem;
            if (istatusitem != null)
            {
                if (name == istatusitem.Msg.InteractName)
                {
                    // add all items found to the list
                    found.Add(istatusitem);
                }
            }
        }

        return found;
    }

	public bool WasInOrder(AssessmentItem checkItem, List<string> prereqList, List<LogItem> log )
	{
		bool ok=false;

		// list with one item has to be in order
		if ( prereqList.Count == 1 )
		{
			return WasInOrder (checkItem,prereqList[0],log);
		}

		// list with more than one item must be an OR condition because two items could
		// never be in order at the same time
		foreach( string prereq in prereqList )
		{
			if ( WasInOrder (checkItem,prereq,log) )
				ok = true;
		}
		return ok;
	}

    public bool WasInOrder(AssessmentItem checkItem, string prereq, List<LogItem> log)
    {
		// find this checkItem
		int found=-1;
        for (int i = log.Count - 1; i >= 0 && found == -1 ; i--)
        {
            InteractStatusItem item = log[i] as InteractStatusItem;
            if (item != null)
            {
				if ( item.Msg.InteractName == checkItem.InteractionName )
				{
					// go to this item - 1
					found = i;
				}
			}
		}
		
        // traverse backwards looking for the first InteractLogItem
        for (int i = found - 1; i >= 0; i--)
        {
            InteractStatusItem item = log[i] as InteractStatusItem;
            if (item != null)
            {
				// check if we can ignore this
				if ( IsIgnore(item.Msg.InteractName) )
					continue;
                // check to see if name matches
                if (item.Msg.InteractName == prereq)
                    return true;
                else
				{
#if DEBUG_ASSESSMENT
					UnityEngine.Debug.Log ("AssessmentItem.WasInOrder() : item.Msg.InteractName=<" + item.Msg.InteractName + "> prereq=<" + prereq + ">");
#endif
                    return false;
				}
            }
        }
		// EMPTY allows prereq OR on the first interaction
		bool emptyCondition=false;
		if ( checkItem.PrereqFlags != null && checkItem.PrereqFlags.Contains ("%%flags") )
		{
			// parse flags
			string value;
			TokenMgr.GetToken (PrereqFlags,"%%flags",out value);
			string[] flags = value.Split (',');
			foreach( string item in flags )
			{
				if ( item.ToLower() == "empty" )
					emptyCondition = true;
			}
		}
		// if empty condition it means that no entries is as good as the correct entry
		if ( emptyCondition == true )
			return true;
        return false;
    }

	public bool IsIgnore( string name )
	{
		foreach( string item in PrereqIgnore )
		{
			if ( name == item )
				return true;
		}
		return false;
	}

    public List<MedAdministerLogItem> FindMedLogItems(MedAssessmentItem med, List<LogItem> log)
    {
        List<MedAdministerLogItem> found = new List<MedAdministerLogItem>();

        // search list for the assessment item
        foreach (LogItem logitem in log)
        {
            MedAdministerLogItem ilogitem = logitem as MedAdministerLogItem;
            if (ilogitem != null)
            {
#if DEBUG_ASSESSMENT
                UnityEngine.Debug.Log("FindMedLogItems : Found item =" + ilogitem.Med.Name);
#endif

                if (med.MedName == ilogitem.Med.Name)
                {
                    // add all items found to the list
                    found.Add(ilogitem);
                }
            }
        }
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("FindMedLogItems : Found count =" + found.Count);
#endif

        return found;
    }
}

public class AssessmentItemReport
{
    public string Name;
	public string InteractionName;
    public AssessmentItem.ResultType Result;     
    public string Note;
    public int Score;
    public int ScoreMax;
    public bool Success;
    public int Duplicates;
    public List<string> Prereq;
	public string Time;
	public bool CriticalAction;
	public int Level;
	public List<string> TriggeredLists;	// list of activated lists by this event

    public AssessmentItemReport()
    {
		Time = "00:00";
		TriggeredLists = null;
	}

    public AssessmentItemReport(AssessmentItem item)
    {
        Name = item.Note;
		InteractionName = item.InteractionName;
        Result = AssessmentItem.ResultType.None;
        Note = "None";
        Success = false;
        Duplicates = 0;
        ScoreMax = item.Score;
		Score = 0;
		TriggeredLists = item.TriggeredLists;
    }

    public string PrettyPrint()
    {
        string result = "Report <" + Result.ToString() + ">";
        if ( Prereq != null && Prereq.Count > 0)
        {
            result += " : Missing <" + Prereq[0] + ">";
        }
        return result;
    }

    public void Debug()
    {
        UnityEngine.Debug.Log("AssessmentItemReport : InteractionName=" + InteractionName + " : Result=" + Result.ToString() + " : Note=" + Note + " : Score=" + Score + " : ScoreMax=" + ScoreMax + " : Success=" + Success + " : Duplicates=" + Duplicates);
    }
}

public class AssessmentEventMsg : GameMsg
{
    public AssessmentEvent Event;
    public AssessmentEventMsg(AssessmentEvent ae) 
        : base()
    {
        Event = ae;
    }
}

// generate an event msg
public class AssessmentEvent
{
    public string Name;
    public string Text;
    public string Time;
    public string Type;

    public void PutMessage()
    {
        AssessmentEventMsg msg = new AssessmentEventMsg(this);
        ObjectManager.GetInstance().PutMessage(msg);
    }
}

public class AssessmentList
{
    public string Name;
    public string Objective;
    public string NoteSuccess;
    public string NoteFailure;
    public float SuccessPercentage;
    public bool Completed;
    public bool Active;
    public bool AlwaysScore;
	public bool WasActivated;
	public string Time;
	public int Level;

    public List<AssessmentItem> Items;
    public List<AssessmentList> Lists;

    public List<string> ActivateOnComplete;
    
    public AssessmentList()
    {
        AlwaysScore = false;
        Completed = false;
        Active = false;
		Time = "00:00";
		WasActivated = false;
    }

	public void SetEmbeddedLevel( AssessmentList list, int level )
	{
#if DEBUG_ASSESSMENT
		UnityEngine.Debug.LogError ("SetEmbeddedLevel(" + list.Name + ") = " + level);
#endif
		// set list level
		Level = level;

		// init each assessment item embedded level
		foreach( AssessmentItem item in list.Items )
		{
			item.Level = level;
		}
		foreach( AssessmentList item in list.Lists )
		{
			SetEmbeddedLevel(item,level+1);
		}
	}

	// init the list.
	public void Init()
	{
		SetEmbeddedLevel(this,0);
	}

	public bool CheckValidInteraction( string interactName )	
	{
        // iterate over all items required for assessment
        foreach (AssessmentItem item in Items)
        {
 			if (item.InteractionName == interactName )
				return true;
		}

        // create a report for all the children
        foreach (AssessmentList list in Lists)
        {
            if (list.Active || list.AlwaysScore)
            {
                if ( list.CheckValidInteraction(interactName) == true )
					return true;
            }
        }		
		return false;
	}	
		
    public AssessmentListReport Evalute(List<LogItem> log)
    {
        // create new report list
        AssessmentListReport report = new AssessmentListReport();
        report.Name = Name;
		report.Time = Time;
		report.Level = this.Level;

#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("AssessmentList.Evalute() : Name=" + Name);
#endif

        // iterate over all items required for assessment
        foreach (AssessmentItem item in Items)
        {
            // evaluate this item
			AssessmentItemReport tmp = item.Evaluate(log);
			if ( tmp != null )
			{
				// set level
				tmp.Level = item.Level;
				// add to report list
				if ( item.Ignore == false)
					report.Items.Add(tmp);
			}
        }

        // create a report for all the children
        foreach (AssessmentList list in Lists)
        {
            if (list.Active || list.AlwaysScore)
            {
                report.Lists.Add(list.Evalute(log));
            }
        }

        // compute score
        report.GetScore();
        report.GetScoreMax();

        // compute percent
        report.Percentage = (float)report.Score / (float)report.ScoreMax;

        // just save it
        report.SuccessPercentage = SuccessPercentage;

        // record success or failure based on score min
        if (report.Percentage >= SuccessPercentage)
        {
            report.Note = NoteSuccess;
            report.Success = true;
        }
        else
        {
            report.Note = NoteFailure;
            report.Success = false;
        }

        return report;
    }

    public void EvaluateInteraction(string interactName, InteractionMap map, List<LogItem> log)
    {
        // iterate over all items required for assessment
        foreach (AssessmentItem item in Items)
        {
            // evaluate this item
            if (interactName == item.InteractionName)
            {
#if DEBUG_ASSESSMENT
                UnityEngine.Debug.Log("AssessmentList.EvaluateInteraction(list=" + Name + " : AssessmentItem=" + item.InteractionName + " : map=" + interactName + ")");
#endif
                // get report for this item
                AssessmentItemReport report = item.Evaluate(log);
				if ( report != null )
				{
	                // send a msg
	                AssessmentItemMsg msg = new AssessmentItemMsg();
	                msg.List = this;
	                msg.Item = item;
	                msg.Map = map;
	                msg.Report = report;	
	                // check activations
	                AssessmentMgr.GetInstance().GetCurrent().CheckActivations(msg);
					// save msg
					AssessmentMgr.GetInstance().AddMsg(msg);
					// send to brain
					Brain.GetInstance().PutMessage(msg);			
					// log msg
	                UnityEngine.Debug.Log(msg.PrettyPrint());
				}
            }
        }
        CheckCompleted();
    }

    // NOTE!! this method is only valid if the list is active
    public bool CheckCompleted()
    {
        if (Completed == false)
        {
            // check all items
            bool completed = true;
            foreach (AssessmentItem item in Items)
            {
                if (item.Completed == false)
                    completed = false;
            }

            // check all children
            foreach (AssessmentList list in Lists)
            {
                // only check children if active
                if (list.Active == true && list.CheckCompleted() == false)
                    completed = false;
            }
                      
            Completed = completed;

            // we're done
            if (Completed == true)
            {
                CheckActivate();

				// send message to brain
                AssessmentListMsg amsg = new AssessmentListMsg();
                amsg.List = this;
		
				// save msg
				AssessmentMgr.GetInstance().AddMsg(amsg);
				
				// send to brain
				Brain.GetInstance().PutMessage(amsg);			

				// set time completed
				this.Time = AssessmentMgr.GetInstance().ToTimeString(UnityEngine.Time.time);
            }
        }
        return Completed;
    }

    public void CheckActivate()
    {
        if (ActivateOnComplete != null)
        {
            AssessmentMgr.GetInstance().GetCurrent().Activate(ActivateOnComplete);
        }
    }

    public void CheckTimeout(List<LogItem> log, float elapsedTime )
    {
        // don't do anything if not active
        if (Active == false)
            return;

        // iterate over all items required for assessment
        foreach (AssessmentItem item in Items)
        {
            if (item.TimeLimit != -1 && elapsedTime > item.TimeLimit )
            {
                // send message to brain
                AssessmentItemMsg msg = new AssessmentItemMsg();
                msg.List = this;
                msg.Item = item;
                msg.Map = null;
                msg.Report = null;

				// save msg
				AssessmentMgr.GetInstance().AddMsg(msg);
				
				// send to brain
				Brain.GetInstance().PutMessage(msg);			

                UnityEngine.Debug.Log(msg.PrettyPrint());
            }
        }
        // recurse
        foreach (AssessmentList list in Lists)
        {
            list.CheckTimeout(log, elapsedTime);
        }
    }

    public AssessmentList FindList(string name)
    {
        foreach (AssessmentList list in Lists)
        {
            if ( list.Name == name )
                return list;

            AssessmentList child = list.FindList(name);
            if (child != null)
                return child;
        }
        return null;
    }
	
	public void RemoveList( string name )
	{
        foreach (AssessmentList list in Lists)
        {
            if ( list.Name == name )
			{
				Lists.Remove (list);
				return;
			}

            AssessmentList child = list.FindList(name);
            if (child != null)
			{
				list.RemoveList(child.Name);
				return;
			}
        }
	}

    public void Debug()
    {
        UnityEngine.Debug.Log("AssessmentList : Name=" + Name + " : NoteSuccess=" + NoteSuccess + " : NoteFailure=" + NoteFailure + " : SuccessPercentage=" + SuccessPercentage);
        foreach (AssessmentItem item in Items)
        {
            item.Debug();
        }
    }
}

public class AssessmentListReport
{
    public string Name;
    public string Note;
    public int Score;
    public int ScoreMax;
    public bool Success;
    public float Percentage;
    public float SuccessPercentage;
    public List<AssessmentItemReport> Items;
    public List<AssessmentListReport> Lists;
	public string Time;
	public int Level;

    public AssessmentListReport()
    {
        Items = new List<AssessmentItemReport>();
        Lists = new List<AssessmentListReport>();
		Time = "00:00";
    }

	void getCounts( ref int total, ref int completed )
	{
		// go through all items counting total number and completed number
		foreach( AssessmentItemReport item in Items )
		{
			total++;
			if ( item.Success == true )
				completed++;
		}
		foreach( AssessmentListReport list in Lists )
		{
			int subCount=0;
			int subCompleted=0;
			list.getCounts (ref subCount, ref subCompleted);
			total += subCount;
			completed += subCompleted;
		}
	}

	public string MakeXOutOfXString()
	{
		int count=0;
		int completed=0;
		getCounts (ref count, ref completed);
		return String.Format("{0:00}",completed) + "/" + String.Format("{0:00}",count);
	}

    public void Debug()
    {
        UnityEngine.Debug.Log("AssessmentListReport : Name=" + Name + " : Note=" + Note + " : Score=" + Score + " : ScoreMax=" + ScoreMax + " : Success=" + Success + " : SuccessPercentage=" + SuccessPercentage + " : Percentage=" + Percentage);
        foreach (AssessmentItemReport item in Items)
        {
            item.Debug();
        }
    }

    public int GetScore()
    {
        int score = 0;

        // get score for each item
        foreach (AssessmentItemReport itemReport in Items)
        {
            score += itemReport.Score;
        }

        // get score of children
        foreach (AssessmentListReport list in Lists)
        {
            score += list.GetScore();
        }

        Score = score;

        return Score;
    }

    public int GetScoreMax()
    {
        int score = 0;

        // get score for each item
        foreach (AssessmentItemReport itemReport in Items)
        {
            score += itemReport.ScoreMax;
        }

        foreach (AssessmentListReport list in Lists)
        {
            score += list.GetScoreMax();
        }

        ScoreMax = score;

        return ScoreMax;
    }
	
	static int counter=6666666;
	static int GetSeconds( string timeString )
	{
		string[] time = timeString.Split(':');		
		int seconds = Convert.ToInt32(time[0])*60 + Convert.ToInt32(time[1]);
		return seconds;
	}
	
	public class SortByTime : IComparer<AssessmentItemReport>
	{
	    public int Compare(AssessmentItemReport x, AssessmentItemReport y)
	    {
			// convert time string to actual time for compare
			if ( GetSeconds(x.Time) > GetSeconds(y.Time) )
				return 1;
			else if ( GetSeconds(x.Time) < GetSeconds(y.Time) )
				return -1;
			else 
				return 0;
	    }
	}
	
	public List<AssessmentItemReport> TimeSort()
	{	List<AssessmentItemReport> sorted = Items;
		IComparer<AssessmentItemReport> comparer = new SortByTime();
		sorted.Sort(comparer);	
		return sorted;
	}
}

public class ScenarioAssessment
{
    public string Name;
    public List<AssessmentList> AssessmentLists;
    public string NoteSuccess;
    public string NoteFailure;
    public float SuccessPercentage;
    public bool Completed;

    public ScenarioAssessment()
    {
        Completed = false;
		AssessmentLists = new List<AssessmentList>();
		Name = "default";
		SuccessPercentage = 0.75f;
		NoteSuccess = "Scenario Status : Success";
		NoteFailure = "Scenario Status : Failed";
    }

    public ScenarioAssessmentReport Report;
	
	public void AddList( AssessmentList list )
	{
		AssessmentList oldlist = FindList(list.Name);
		if ( oldlist != null )
			RemoveList(oldlist.Name);
			
		AssessmentLists.Add(list);
	}

    public AssessmentList FindList( string name)
    {
        foreach (AssessmentList list in AssessmentLists)
        {
            if (list.Name == name)
                return list;
            // check children if not found
            AssessmentList child = list.FindList(name);
            if (child != null)
                return child;
        }
        return null;
    }
	
	public void RemoveList( string name )
	{
        foreach (AssessmentList list in AssessmentLists)
        {
            if (list.Name == name)
			{
				AssessmentLists.Remove (list);
				return;
			}
            // check children if not found
            AssessmentList child = list.FindList(name);
            if (child != null)
			{
				list.RemoveList(child.Name);
				return;
			}
        }
	}

    public ScenarioAssessmentReport Evaluate(List<LogItem> log)
    {
		// set time
        ScenarioAssessmentReport report = new ScenarioAssessmentReport();
        report.SetLog(log);
		
		// put in current time of report....this should do for the dialog???
		report.ElapsedTime = Time.time;

#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("ScenarioAssessment.Evalute() : Name=" + Name);
#endif

        foreach (AssessmentList list in AssessmentLists)
        {
            if ((list.Active == true ) || list.AlwaysScore == true )
            {
                AssessmentListReport listReport = list.Evalute(log);
                if (report != null)
                    report.Items.Add(listReport);
            }
        }

        report.Name = Name;
        report.Score = 0;
        report.ScoreMax = 0;

        // check for overall success or failure
        foreach (AssessmentListReport listReport in report.Items)
        {
            report.Score += listReport.Score;
            report.ScoreMax += listReport.ScoreMax;
        }

        // compute percent
        report.Percentage = (float)report.Score / (float)report.ScoreMax;

        // just save it
        report.SuccessPercentage = SuccessPercentage;

        // record success or failure based on score min
        if (report.Percentage >= SuccessPercentage)
        {
            report.Note = NoteSuccess;
            report.Success = true;
        }
        else
        {
            report.Note = NoteFailure;
            report.Success = false;
        }

        return report;
    }

    public bool Activate(List<string> activate)
    {
        foreach (string name in activate)
        {
            AssessmentList list = FindList(name);
            if (list != null)
            {
                UnityEngine.Debug.Log("ScenarioAssessment.Activate() : Activate list=<" + name + ">");
                // activate list
                list.Active = true;
				// set activated flag
				list.WasActivated = true;
				// return activated
				return true;
            }
        }
		// not found, return false
		return false;
    } 

    // this method activates new lists based on success or failure of AssessmentItems
    public void CheckActivations(List<AssessmentItemMsg> msglist)
    {
        foreach (AssessmentItemMsg msg in msglist)
        {
            CheckActivations(msg);
        }
    }

    public void CheckActivations(AssessmentItemMsg aimsg)
    {
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("CheckActivations(" + aimsg.Item + "," + aimsg.Report.InteractionName + "," + aimsg.Report.Result + ")");
#endif
        List<string> activate = aimsg.Item.CheckActivations(aimsg.Report);
        if (activate != null)
        {
            if ( Activate(activate) == true )
			{
				// the first case has an event which is ignored unless it is set to
				if ( aimsg.Item.Ignore == true && aimsg.Item.Trigger == true )
				{
					aimsg.Item.TriggeredLists = activate;
					aimsg.Item.Ignore = false;
				}
			}
        }
    }
	
	public bool CheckValidInteraction( string interactName )
	{
		foreach( AssessmentList list in AssessmentLists)
		{
			if ( list.CheckValidInteraction(interactName) == true )
				return true;
		}
		return false;
	}

    public void EvaluateInteraction(string interactName, InteractionMap map, List<LogItem> log)
    {
        bool completed = true;

        // access this interaction across all lists
        foreach (AssessmentList list in AssessmentLists)
        {
#if DEBUG_ASSESSMENT
            UnityEngine.Debug.Log("ScenarioAssessment.EvaluateInteraction(list=" + list.Name + " : map=" + interactName + ")");
#endif
            if (list.Active == true && list.Completed == false)
            {
                list.EvaluateInteraction(interactName, map, log);
				if ( list.Completed == false )
                	completed = false;
            }
        }

        // check if scenario is complete
        if (Completed == false)
        {
            // set scenario complete
            Completed = completed;

            // send msg
            if (Completed == true)
            {
                AssessmentScenarioMsg msg = new AssessmentScenarioMsg();
                msg.Scenario = this;
		
				// save msg
				AssessmentMgr.GetInstance().AddMsg(msg);
				
				// send to brain
				Brain.GetInstance().PutMessage(msg);			
            }
        }
    }

    public void Update(float elapsedTime)
    {
        
    }

    public void CheckTimeouts( List<LogItem> log, float elapsedTime)
    {
        foreach (AssessmentList list in AssessmentLists)
        {
            list.CheckTimeout(log,elapsedTime);
        }
    }

    public void Debug()
    {
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("ScenarioAssessment : Name=" + Name + " : NoteSuccess=" + NoteSuccess + " : NoteFailure=" + NoteFailure + " : SuccessPercentage=" + SuccessPercentage);
#endif
        foreach (AssessmentList list in AssessmentLists)
        {
            list.Debug();
        }
    }
}

public class ScenarioTimeReport
{
	public string Name;
	public bool Success;
	public List<string> Items;
	public List<int> Times;

	public ScenarioTimeReport()
	{}
	
	public int GetTime( string name )
	{
		if ( Items == null )
			return 0;
		
		for (int i=0 ; i<Items.Count ; i++)
		{
			if ( Items[i] == name )
				return Times[i];
		}
		return 0;
	}

	public int GetNumInteractions()
	{
		return Items.Count;
	}
	
	public void Load( ScenarioAssessmentReport report )
	{
		Name = report.Name;
		Success = report.Success;
		Items = new List<string>();
		Times = new List<int>();
		
		//if ( Success == false )
		//	return;
		
		foreach( AssessmentListReport list in report.Items )
		{
			foreach( AssessmentItemReport item in list.Items )
			{
				if ( item.Result == AssessmentItem.ResultType.Required )
				{
					// only add required interactions
					Items.Add (item.InteractionName);
					Times.Add (GetSeconds(item.Time));
				}
			}
		}
	}
	
	static int GetSeconds( string timeString )
	{
		string[] time = timeString.Split(':');		
		int seconds = Convert.ToInt32(time[0])*60 + Convert.ToInt32(time[1]);
		return seconds;
	}
	
    public string ToString()
    {
        // serialize and return
        Serializer<ScenarioTimeReport> serializer = new Serializer<ScenarioTimeReport>();
        return serializer.ToString(this);
    }
	
	public string ToCSV()
	{
		string output="";
		for (int i=0 ; i<Times.Count ; i++)
		{
			output += Times[i].ToString ();
			if ( i != Times.Count-1 )
				output += ",";
		}
		return output;
	}
	
	public string ToPairStrings()
	{
		string output="";
		for (int i=0 ; i<Times.Count ; i++)
		{
			output += Items[i] + "%" + Times[i];
			if ( i != Times.Count-1 )
				output += ",";
		}
		return output;
	}
	
	public string ToTimeStrings()
	{
		// format is #count:t1,t2,t3
		string output="";
		for (int i=0 ; i<Times.Count ; i++)
		{
			output += Times[i];
			if ( i != Times.Count-1 )
				output += ",";
		}
		return output;
	}
	
	public void SetAsAverages( string data )
	{
		// first split all data into sets
		string[] arrays = data.Split('#');
		bool first = true;
		
		// now extract the data
		for (int i=0 ; i<arrays.Length ; i++)
		{
			if ( arrays[i] == "" )
				continue;
		
			string[] pairStrings = arrays[i].Split(',');
			
			// if first time, fill in fields
			if ( first == true ) 
			{
				first = false;
				Items = new List<string>();
				Times = new List<int>();
				
				for (int j=0 ; j<pairStrings.Length ; j++)
				{
					// split items
					string[] pairs = pairStrings[j].Split ('%');
					if ( pairs.Length == 2 )
					{
						Items.Add (pairs[0]);
						Times.Add (Convert.ToInt32 (pairs[1]));
					}
				}
			}
			else
			{
				for (int j=0 ; j<pairStrings.Length ; j++)
				{
					// split items
					string[] pairs = pairStrings[j].Split ('%');
					if ( pairs.Length == 2 )
					{
						// add to total for this item
						if ( Items[j] == pairs[0] )
							Times[j] += Convert.ToInt32(pairs[1]);
						else
							UnityEngine.Debug.LogError ("ScenarioTimeReport.SetAsAverages() : expecting<" + Items[j] + "> + got<" + pairs[0] + ">");
					}
				}				
			}		
		}
		
		int total = arrays.Length;
		
		// now average
		if ( Times != null )
		{
			for ( int i=0 ; i<Times.Count() ; i++)
			{
				float tmp = Times[i];
				tmp /= (float)Times.Count();
				Times[i] = Mathf.RoundToInt(tmp);
			}
		}
	}
}

public class ScenarioAssessmentReport
{
    public string Name;
    public string Note;
    public int Score;
    public int ScoreMax;
    public bool Success;
    public float Percentage;
    public float SuccessPercentage;
    public List<AssessmentListReport> Items;
    public List<string> Log;
	public float ElapsedTime;

    public ScenarioAssessmentReport()
    {
        Items = new List<AssessmentListReport>();
    }

    public void SetLog(List<LogItem> log)
    {
        // make interact log
        Log = new List<string>();
        foreach (LogItem item in log)
        {
            Log.Add(item.XMLString());
        }
    }
	
	public AssessmentListReport GetReport( string name )
	{
		foreach( AssessmentListReport list in Items )
		{
			if ( list.Name == name )
				return list;
		}
		return null;
	}

    public void Debug()
    {
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("ScenarioAssessmentReport : Name=" + Name + " : Note=" + Note + " : Score=" + Score + " : ScoreMax=" + ScoreMax + " : Success=" + Success + " : SuccessPercentage=" + SuccessPercentage + " : Percentage=" + Percentage);
#endif
        foreach (AssessmentListReport report in Items)
        {
            report.Debug();
        }
    }

    public string ToString()
    {
        // serialize and return
        Serializer<ScenarioAssessmentReport> serializer = new Serializer<ScenarioAssessmentReport>();
        return serializer.ToString(this);
    }
}

public class AssessmentMgr
{
	List<GameMsg> assessmentMsgs;
	
	public enum MsgType
	{
		Interact,
		List,
		Scenario
	}
	
    static AssessmentMgr instance;
    public static AssessmentMgr GetInstance()
    {
        if (instance == null)
            instance = new AssessmentMgr();
        return instance;
    }
	
	public AssessmentMgr()
	{
		Init();
	}
	
    // loaded scenarios
    public ScenarioAssessment Scenario;

    public void LoadXML(string filename)
    {
		Init();
        Serializer<ScenarioAssessment> serializer = new Serializer<ScenarioAssessment>();
        Scenario = serializer.Load(filename);
        //if (Scenarios != null)
        //    Debug();
    }
	
	public ScenarioAssessment InitScenario( string name )
	{
		Scenario = new ScenarioAssessment();
		Scenario.Name = name;
		return Scenario;
	}
	
	public void LoadAssessmentListXML( string filename )
	{
		Serializer<AssessmentList> serializer = new Serializer<AssessmentList>();
		AssessmentList list = serializer.Load(filename);	
		if ( list != null )
		{
			list.Init ();
			Scenario.AddList(list);
		}
		else
		{
			UnityEngine.Debug.LogError ("AssessmentMgr.LoadAssessmentListXML(" + filename + ") problem loading XML");
		}
	}
	
	public void Init()
	{
		assessmentMsgs = new List<GameMsg>();
		// create new Scenario
		Scenario = new ScenarioAssessment();
		SetCurrent(Scenario);
	}

    private ScenarioAssessment current;
    public ScenarioAssessment GetCurrent()
    {
        return current;
    }
	public void SetCurrent( ScenarioAssessment assessment )
	{
		current = assessment;
	}
	
	public void AddMsg( GameMsg msg )
	{
		assessmentMsgs.Add(msg);
	}

    float elapsedTime = 0.0f;
    float lastTimeCheck = 0.0f;

    public void Update()
    {
        elapsedTime += Time.deltaTime;

        // check for time critical assessment events
        if (elapsedTime > lastTimeCheck)
        {
            // check every second
            lastTimeCheck = elapsedTime + 1.0f;
            // check
            CheckTimeouts(LogMgr.GetInstance().GetCurrent().Items,elapsedTime);
        }
    }

    // create a report based on the assessment criteria and the log
    public ScenarioAssessmentReport Evaluate(List<LogItem> log)
    {
        if (current != null)
        {
            return current.Evaluate(log);
        }
        UnityEngine.Debug.LogError("AssessmentMgr.Evalue() : current not set!");
        return null;
    }

    public void EvaluateInteraction( string interactName, InteractionMap map, List<LogItem> log )
    {
		if ( CheckValidInteraction(interactName) == false)
		{
			// item not found, let everyone knpow
			AssessmentItemNotFoundMsg msg = new AssessmentItemNotFoundMsg();
			msg.InteractName = interactName;
			AddMsg (msg);
			Brain.GetInstance().PutMessage (msg);
		}
		
#if DEBUG_ASSESSMENT
        UnityEngine.Debug.Log("AssessmentMgr.EvaluateInteraction(map=" + interactName + ") : current=" + current);
#endif
        if (current != null)
            current.EvaluateInteraction(interactName, map, log);
    }

	public bool CheckValidInteraction( string interactName )
	{
		if ( current != null)
			return current.CheckValidInteraction(interactName);
		return false;
	}

    public void CheckTimeouts(List<LogItem> log, float elapsedTime )
    {
        if (current != null)
            current.CheckTimeouts(log, elapsedTime); 
    }

    public void Debug()
    {
        Scenario.Debug();
    }
	
	public string ToTimeString( float time )
	{
		int hours, minutes, seconds;
		seconds = (int)(time);
		minutes = (int)(time / 60.0f);
		hours = (int)(minutes / 60.0f);
		minutes -= hours * 60;
		seconds -= (minutes * 60) + (hours * 3600);
		return minutes.ToString("00") + ":" + seconds.ToString("00");
	}	
	
	public void PutMessage( GameMsg msg )
	{
		// only check ISMs
		InteractStatusMsg ismsg = msg as InteractStatusMsg;
		if ( ismsg != null )
		{
			// do assessment
        	EvaluateInteraction(ismsg.InteractName, ismsg.InteractMap, LogMgr.GetInstance().GetCurrent().Items);
		}

		InteractMsg imsg = msg as InteractMsg;
		if ( imsg != null )
		{
			// do assessment
			EvaluateInteraction(imsg.map.item, imsg.map, LogMgr.GetInstance().GetCurrent().Items);
		}
	}
	
	public bool GotMsg( AssessmentMgr.MsgType type, string msgName )
	{
		foreach( GameMsg msg in assessmentMsgs )
		{
			AssessmentItemMsg aim = msg as AssessmentItemMsg;
			if ( aim != null && type == MsgType.Interact && aim.Item.InteractionName == msgName )
				return true;
			
			AssessmentListMsg alm = msg as AssessmentListMsg;
			if ( alm != null && type == MsgType.List && alm.List.Name == msgName )
				return true;
			
			AssessmentScenarioMsg asm = msg as AssessmentScenarioMsg;
			if ( asm != null && type == MsgType.List && asm.Scenario.Name == msgName )
				return true;			
		}
		
		return false;
	}
}


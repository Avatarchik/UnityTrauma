//#define SAVE_TEMPLATE
#define CHECK_ACTIVE
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class VideoTriggerInfoContainer
{
	public static List<string> Triggers;
}

public class VideoTriggerInfo
{
	public string Name;
	public string Case;
	public bool Active;
	public string Text;
	public string ConditionOperator;
	public List<string> Conditions;
	public string InteractionOperator;
	public List<string> Interactions;
	public string PrereqOperator;
	public List<string> Prereqs;
	public string Key;
	public string Movie;
	public string Thumbnail;
	public int ConditionStartTime;
	public bool TimedCondition;
	public bool Status;
	public bool Log;

	Patient patient;

	public VideoTriggerInfo() 
	{
		PrereqOperator = "and";
		InteractionOperator = "and";
		Log = true;
		ConditionStartTime = -1;
	}

	public void Init()
	{
		// first set false;
		Active = false;
		// get patient
		if ( patient == null )
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		// add case specific triggers
		if ( Case != null && Case.Length != 0 )
		{
			// get cases
			string[] cases = Case.Split(',');
			// check if any equal current case
			foreach( string item in cases )
			{
				if ( item == CaseConfiguratorMgr.GetInstance().Data.shortName || CaseConfiguratorMgr.GetInstance().Data.shortName == "" )
					Active = true;
			}
		}
		else
			Active = true;
	}

	// %HR less 120
	bool EvaluateCondition( string item )
	{
		if ( patient == null )
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient;

		if ( patient == null )
			return false;

		if ( item.ToLower ().Contains ("%") == true )
		{
			string[] tokens = item.Split ();
			DecisionVariable dv = patient.GetDecisionVariable(tokens[0]);
			if ( dv != null && tokens.Length == 3 )
			{
				// test decision variable condition
				if ( dv.Test(tokens[2],tokens[1]) )
				{
					if ( ConditionStartTime == -1 )
						ConditionStartTime = (int)Brain.GetInstance().elapsedTime;
					return true;
				}
			}
		}
		return false;
	}

	List<LogItem> FindISMs( string name )
	{
		List<LogItem> found = new List<LogItem>();		
		// search list for the assessment item
		foreach (LogItem logitem in LogMgr.GetInstance().GetCurrent().Items)
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

	bool EvaluateInteraction( string item )
	{
		// break out string into parts
		string[] tokens = item.Split (' ');
		if ( tokens == null || tokens.Length == 0 )
			return false;

		// check for NOT condition
		bool not;
		string interaction = tokens[0];
		if ( interaction.Contains ("!") )
		{
			not = true;
			interaction = interaction.Replace ("!","");
		}
		else
			not = false;

		//  if not is false then this is looking for an interaction
		if ( not == false )
		{
			// get ISMs
			List<LogItem> found = FindISMs(tokens[0]);		
			
			// we we have prereqs then check them
			if ( Prereqs != null && Prereqs.Count > 0 && found.Count > 0 )
			{
				if ( PrereqOperator.ToLower() == "and" )
				{
					bool status = false;
					foreach( string prereq in Prereqs )
					{
						List<LogItem> foundPrereq = FindISMs(prereq);
						// first check to see that we had this prereq
						if ( foundPrereq.Count == 0 )
							return true;
						// check last found ISM against the first prereq
						InteractStatusItem ism = found[0] as InteractStatusItem;
						InteractStatusItem isi = foundPrereq[0] as InteractStatusItem;
						if ( ism.Time < isi.Time )
						{
							// ism before prereq, this is an error
							if ( Log == true )
								UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : AND : ISM before prereq!");
							return true;
						}
					}
				}
				else if ( PrereqOperator.ToLower () == "or")
				{
					bool status = false;
					foreach( string prereq in Prereqs )
					{
						List<LogItem> foundPrereq = FindISMs(prereq);
						// first check to see that we had this prereq
						if ( foundPrereq.Count > 0 )
						{
							// check last found ISM against the first prereq
							InteractStatusItem ism = found[0] as InteractStatusItem;
							InteractStatusItem isi = foundPrereq[0] as InteractStatusItem;
							if ( ism.Time > isi.Time )
							{
								// criteria met, return false
								return false;
							}
						}
						// no prereqs available, error case
						if ( Log == true )
							UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : OR : ISM before prereq!");
						return true;
					}
				}
			}

			// check basic case of just looking for interactions
			if ( tokens.Length == 1 )
			{
				if ( found.Count > 0 )
					return true;
			}
			
			// if we have some params then start checking them
			if ( tokens.Length == 2 )
			{
				// check for params
				string time;
				if ( TokenMgr.GetToken(tokens[1],"before",out time) )
				{
					// if we found it then check to see if it happened before a time
					if ( found.Count > 0 )
					{
						float value;
						if ( float.TryParse (time,out value) )
						{
							foreach ( LogItem logitem in found )
							{
								// check to see that the time of event is less than this value
								if ( logitem.Time < value )
								{
									if ( Log == true )
										UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : before condition failed!");
									return true;
								}
							}
						}
					}
					else
					{
						// not found, we shouldn't have to check anything????
					}
				}
				if ( TokenMgr.GetToken(tokens[1],"after",out time) )
				{
					if ( found.Count > 0 )
					{
						// if we found some time items check to see if any happened before the after condition
						foreach ( LogItem logitem in found )
						{
							// check to see that the time of event is greater than this value
							float value;
							if ( float.TryParse (time,out value) )
							{
								// compute time to check against
								int checkTime;
								// if we have a timed condition, then test the time from
								// the condition start to the current time
								if ( TimedCondition == true )
									checkTime = ConditionStartTime + (int)value;
								else
									checkTime = (int)value;
								
								if ( logitem.Time > checkTime )
								{
									if ( Log == true )
										UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : after condition failed!");
									return true;
								}
							}
						}
					}
					else
					{
						// nothing found, see if we are over the elapsed time for this event
						float value;
						if ( float.TryParse (time,out value) )
						{
							int checkTime;
							// if we have a timed condition, then test the time from
							// the condition start to the current time
							if ( TimedCondition == true )
								checkTime = (int)Brain.GetInstance().elapsedTime - ConditionStartTime;
							else
								checkTime = (int)Brain.GetInstance().elapsedTime;
							//
							if ( checkTime > value )
							{
								if ( Log == true )
									UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : after elapsedTime condition failed!");
								return true;
							}
						}
					}
				}
			}
		}
		// THIS IS THE NOT CONDITION
		else
		{
			// get ISMs
			List<LogItem> found = FindISMs(tokens[0].Substring(1));		

			// only evaluate if wasn't found!
			if ( found.Count == 0 )
			{			
				if ( tokens.Length == 1 )
				{
					if ( Log == true )
						UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : NOT condition was found, triggered!");
					return true;
				}

				if ( tokens.Length == 2 )
				{
					// check for params
					string time;
					if ( TokenMgr.GetToken(tokens[1],"after",out time) )
					{
						// check to see that the time of event is less than this value
						float value;
						if ( float.TryParse (time,out value) )
						{
							int checkTime;
							// if we have a timed condition, then test the time from
							// the condition start to the current time
							if ( TimedCondition == true )
								checkTime = (int)Brain.GetInstance().elapsedTime - ConditionStartTime;
							else
								checkTime = (int)Brain.GetInstance().elapsedTime;
							// if check time is greated than the arg value then we triggered
							if ( checkTime > value )
							{
								if ( Log == true )
									UnityEngine.Debug.LogError ("VideoTriggers.EvaluateInteraction(" + item + ") : after condition failed!");
								return true;
							}
						}
					}
				}
			}
		}

		return false;
	}

	public bool Evaluate()
	{
#if CHECK_ACTIVE
		// item must match current case
		if ( Active == false )
			return false;
#endif
		// check to see that we have conditions and interactions
		if ( (Conditions == null || Conditions.Count==0) && (Interactions == null || Interactions.Count==0) )
			return false;
		// if we already evaluated this to true then we
		// don't need to test again
		if ( Status == true )
			return true;
		// evaluate conditions & interactions (if anything fails then we haven't
		// met the condition)
		foreach( string item in Conditions )
			if ( EvaluateCondition(item) == false )
				return false;
		// interactions
		int foundFalse=0;
		foreach( string item in Interactions )
		{
			if ( EvaluateInteraction(item) == false )
			{
				// if not OR then assume AND
				if ( InteractionOperator.ToLower () != "or" )	
				{
					// AND condition
					// return on any item failed, this means that one of the items
					// failed and thus condition is true
					return false;
				}
				else
				{
					// OR condition (count them)
					foundFalse++;
					// we all of them are false, return false
					if ( foundFalse == Interactions.Count )
						return false;
				}
			}
			else
			{
				// if we are in an OR condition then this successful condition
				// is enough to break us out
				if ( InteractionOperator.ToLower () == "or" )
					break;
			}
		}
		UnityEngine.Debug.LogError ("VideoTriggers.Evaluate() : condition met <" + this.Name + ">");
		// set triggered
		Status = true;
		// add to static list for use when debriefing runs
		if ( VideoTriggerInfoContainer.Triggers == null )
			VideoTriggerInfoContainer.Triggers = new List<string>();
		if ( VideoTriggerInfoContainer.Triggers.Contains (this.Name) == false )
			VideoTriggerInfoContainer.Triggers.Add (this.Name);
		// we're ok
		return true;
	}
}

public class VideoTriggers : MonoBehaviour {

	// Use this for initialization
	void Start () {
		// init container
		VideoTriggerInfoContainer.Triggers = null;
		// load in triggers
		LoadTriggers();
		// start coroutine to check triggers
		StartCoroutine(CheckTriggers());
	}

	List<VideoTriggerInfo> Triggers;
	void LoadTriggers()
	{
		Serializer<List<VideoTriggerInfo>> serializer = new Serializer<List<VideoTriggerInfo>>();
#if SAVE_TEMPLATE
		List<VideoTriggerInfo> tmpList = new List<VideoTriggerInfo>();
		VideoTriggerInfo tmpInfo = new VideoTriggerInfo();
		tmpInfo.Conditions = new List<string>();
		tmpInfo.Conditions.Add ("condition1");
		tmpInfo.Interactions = new List<string>();
		tmpInfo.Interactions.Add ("interactions");
		tmpInfo.Movies = new List<string>();
		tmpInfo.Movies.Add ("movies");
		tmpList.Add (tmpInfo);
		serializer.Save ("VideoTriggers",tmpList);
#else
		Triggers = serializer.Load ("XML/VideoTriggers");
		foreach( VideoTriggerInfo item in Triggers )
			item.Init();
#endif
	}

	IEnumerator CheckTriggers()
	{
		// wait for one second
		yield return new WaitForSeconds(1.0f);
		// evaluate
		EvaluateTriggers();
		// do again!
		StartCoroutine(CheckTriggers());
	}

	void EvaluateTriggers()
	{
		foreach( VideoTriggerInfo item in Triggers )
		{
			item.Evaluate();
		}
	}
	
	// Update is called once per frame
	void Update () {
		CheckTriggers ();
	}
}

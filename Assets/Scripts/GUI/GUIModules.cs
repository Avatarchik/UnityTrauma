#define PRELOAD_CASE
#define ISSUE_URL_COMMAND
#define USE_SELECTION_WEIGHTS
//#define LEARN_SELECTION_WEIGHTS
//#define GOTO_TUTORIAL
//#define GOTO_ASSESSMENT
//#define WRITE_CASE_XML
//#define ONLY_CRITICAL_ACTION
#define GOTO_PAUSE

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

public class CaseDebriefingScreen : GUIDialog
{
	GUIHorizontalCommand horiz;
	GUIButton template;
	GUIScrollView vidsScrollview;
	GUILabel text;
	GUILabel title;
	GUILabel heading;
	TriggerMapInfo triggerInfo;
	GUIMovie movie;
	GUIScrollView infoScroll;

	public override void Load( DialogMsg msg )
	{		
		base.Load (msg);

		GUILabel titleCaseName = Find("titleCaseName") as GUILabel;
		if ( titleCaseName != null )
		{
			if ( TraumaReportMgr.GetInstance().Report.Report != null )
				titleCaseName.text = TraumaReportMgr.GetInstance().Report.Report.Name;
			else
				titleCaseName.text = "No Case";
		}

		// version
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";

		// get container for carousel
		horiz = Find ("carouselHorz") as GUIHorizontalCommand;
		if ( horiz != null )
		{
			// make video template
			template = Find ("vid_01") as GUIButton;
			itemWidth = template.Style.fixedWidth;
			if ( template != null )
				horiz.Elements.Clear ();
		}
		movie = Find ("MovieClip") as GUIMovie;
		movie.Stop ();
		firstMovie = true;

		// get text for trigger info
		text = Find ("vidDescription") as GUILabel;
		title = Find ("vidTitle") as GUILabel;
		heading = Find ("vidHeading") as GUILabel;
		infoScroll = Find ("descScrollview") as GUIScrollView;

		// get scrollview
		vidsScrollview = Find("vidsScrollview") as GUIScrollView;
		// 
		LoadVideoTemplates();

		// init once in update
		forceInit = true;
		// start at first case
		vidIndex = 0;
	}

	bool firstMovie=true;
	float playNext = -1f;

	void MovieHandler()
	{
		if ( firstMovie == true )
		{
			movie.Stop ();
			firstMovie = false;
		}

		if ( playNext != -1f && Time.time > playNext )
		{
			movie.Play ();
			playNext = -1f;
		}
	}

	List<VideoTriggerInfo> Triggers;

	public class TriggerMapInfo
	{
		public string Key;
		public List<string> Headers;
		public List<string> Text;
		public string Movie;
		public TriggerMapInfo( string key, string header, string text, string movie )
		{
			Key = key;
			Headers = new List<string>();
			Headers.Add (header);
			Text = new List<string>();
			Text.Add (text);
			Movie = movie;
		}
	}

	List<TriggerMapInfo> TriggerMaps = new List<TriggerMapInfo>();

	bool MapTrigger( VideoTriggerInfo vti )
	{
		// first look to see if we have a duplicate movie already loaded
		foreach( TriggerMapInfo item in TriggerMaps )
		{
			if ( item.Key == vti.Key )
			{
				item.Headers.Add (vti.Name);
				item.Text.Add (vti.Text);
				return false;
			}
		}
		// we got here, this must be new
		TriggerMaps.Add (new TriggerMapInfo(vti.Key,vti.Name,vti.Text,vti.Movie));
		// returnt true means new item
		return true;
	}

	TriggerMapInfo GetTriggerMap( string key )
	{
		foreach( TriggerMapInfo item in TriggerMaps )
		{
			if ( item.Key == key )
			{
				return item;
			}
		}
		return null;
	}

	void LoadVideoTemplates()
	{
		if ( horiz == null )
			return;

		// load trigger info
		Serializer<List<VideoTriggerInfo>> serializer = new Serializer<List<VideoTriggerInfo>>();
		Triggers = serializer.Load ("XML/VideoTriggers");
		if ( Triggers == null )
		{
			UnityEngine.Debug.LogError ("CaseDebriefingScreen.LoadVideoTemplates() : can't load XML/VideoTriggers");
			return;
		}

		// clear
		TriggerMaps.Clear ();

		foreach( VideoTriggerInfo vti in Triggers )
		{
			bool useTrigger = true;
			// get list of triggers
			//List<string> triggers = VideoTriggerInfoContainer.Triggers;
			List<string> triggers = TraumaReportMgr.GetInstance().Report.TriggerEvents;
			// only test if Triggers is not null and has a count
			if ( triggers != null && triggers.Count != 0 )
			{
				if ( triggers.Contains (vti.Name) == false )
					useTrigger = false;
			}
			if ( useTrigger == true )
			{
				// combine triggers here!
				if ( MapTrigger(vti) == true )
				{
					// get map
					TriggerMapInfo tmi = GetTriggerMap(vti.Key);
					// add the button
					GUIButton clone = template.Clone() as GUIButton;
					clone.name = tmi.Key;
					clone.text = tmi.Key;
					clone.SetStyle(new GUIStyle(clone.Style));
					clone.Style.normal.background = Resources.Load (vti.Thumbnail) as Texture2D;
					clone.Style.normal.textColor = Color.white;
					horiz.Elements.Add (clone);
				}
			}
		}

		// do this after the mapping, make sure we have one entry!
		if ( TriggerMaps.Count > 0 )
			SetTriggerInfo(TriggerMaps[0]);
	}

	public override void OnClose()
	{
		movie.Stop ();
	}

	public override void ButtonCallback(GUIButton button)
	{	
		// default
		base.ButtonCallback(button);

		// handle left panel
		if ( button.name == "bt_dashboard" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseOverviews";
			msg.className = "CaseOverview";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_breakdown" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaDecisionBreakdown";
			msg.className = "DecisionBreakdown";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_caseSelect" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseSelection";
			msg.className = "TraumaCaseSelection";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_next" )
		{
			NextCase ();
		}
		if ( button.name == "bt_prev" )
		{
			PrevCase ();
		}
		// test trigger names
		foreach( TriggerMapInfo tmi in TriggerMaps )
		{
			if ( button.name == tmi.Key )
			{
				SetTriggerInfo(tmi);
			}
		}
	}

	void SetTriggerInfo( TriggerMapInfo tmi )
	{
		// set everything
		if ( text != null )
		{
			title.text = tmi.Key;
			text.text = "";
			//text.text = info.Text;
			infoScroll.Elements.Clear ();
			for( int i=0 ; i<tmi.Text.Count ; i++)
			{
				GUILabel newHeading = heading.Clone () as GUILabel;
				newHeading.text = tmi.Headers[i];
				infoScroll.Elements.Add (newHeading);

				GUILabel newLabel = text.Clone () as GUILabel;
				newLabel.text = tmi.Text[i];
				infoScroll.Elements.Add (newLabel);

				infoScroll.Elements.Add (new GUISpace(20));
			}
		}
		if ( movie != null )
		{
			if ( triggerInfo == tmi )
			{
				if ( movie.IsPlaying() == true )				
					movie.Stop();
				else
				{
					movie.Stop();
					movie.Play();
				}
			}
			else
			{
				movie.Stop ();
				movie.SetFilename(tmi.Movie);
				playNext = Time.time + 0.1f;
				//movie.Play ();
			}
		}
		triggerInfo = tmi;
	}
	
	void NextCase()
	{
		if ( vidIndex < TriggerMaps.Count-4 )
		{
			vidIndex++;
			//DoPrevNextButtons();
			scrollSeek = vidIndex*(itemWidth+padding);
			scrollInc = 1f;
		}
	}
	
	void PrevCase()
	{
		if ( vidIndex > 0 )
		{
			vidIndex--;
			//DoPrevNextButtons();
			scrollSeek = vidIndex*(itemWidth+padding);
			scrollInc = -1f;	
		}
	}
	
	bool forceInit;	
	float itemWidth;
	float scrollSeek=0.0f;
	float scrollScale=4000;
	float scrollInc=0;
	int vidIndex=0;
	int padding=8;
	
	public override void Update()
	{
		// movie handler
		MovieHandler();

		// if the timeScale is 0 then just set the position without increment
		if ( Time.timeScale == 0.0f )
		{
			if ( scrollSeek == 0.0f )
				scrollSeek = vidIndex*itemWidth;
			
			vidsScrollview.scroll.x = scrollSeek;
			return;
		}			
		
		// force init the case scroll because somehow the scroller init happens later!!
		if ( forceInit == true )
		{
			forceInit = false;
			scrollSeek=0.0f;
		}
		
		if ( scrollInc > 0 && vidsScrollview.scroll.x < scrollSeek )
		{
			vidsScrollview.scroll.x += Time.deltaTime*scrollScale;
			if ( vidsScrollview.scroll.x >= scrollSeek )
			{
				// at end
				scrollInc = 0;
				vidsScrollview.scroll.x = scrollSeek;
			}
		}
		
		if ( scrollInc < 0 && vidsScrollview.scroll.x > scrollSeek )
		{
			vidsScrollview.scroll.x -= Time.deltaTime*scrollScale;
			if ( vidsScrollview.scroll.x <= scrollSeek )
			{
				scrollInc = 0;
				vidsScrollview.scroll.x = scrollSeek;
			}
		}
	}
	
}


public class DebriefingScreen : GUIDialog
{
    public DebriefingScreen()
        : base()
    {
    }

	public class DebriefingInfo
	{
		public string InteractionName;
		public string Text;
		public string Movie;
	}

	ScenarioAssessmentReport report;
	
	// important areas to save 
	GUIScrollView header;
	GUIHorizontalCommand button,subButton;
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);

		// get current report
		report = TraumaReportMgr.GetInstance().Report.Report;
		
		// find the task button area
		header = this.Find("TaskScrollBox") as GUIScrollView;
		// find the horizontal element
		button = this.Find("TaskItem") as GUIHorizontalCommand; 
		// find the sub item
		subButton = this.Find ("subTask01") as GUIHorizontalCommand;
		// fill it
		FillTasks();
	}
	
	bool A=true,B=false,C=false,D=false,R=false;
	
	public override void Update()
	{		
		//if ( Input.GetKeyUp(KeyCode.F) )
		//	FillTasks();		
	}
	
	List<AssessmentItemReport> tasks;
	
	static string textIncomplete="-----";
	
	public void AddTasks( AssessmentListReport tmpReport, List<AssessmentItemReport> tasks )
	{
		foreach( AssessmentItemReport item in tmpReport.Items  )
		{
			if ( DebriefInfo != null )
			{
				if ( GetDebriefInfo(item) != null )
					tasks.Add (item);
			}
			else
				tasks.Add(item);
		}
		// this handles activated lists
		foreach( AssessmentListReport list in tmpReport.Lists )
		{
			AddTasks(list,tasks);
		}
	}

	List<DebriefingInfo> DebriefInfo;
	
	public void LoadDebriefInfo()
	{
		Serializer<List<DebriefingInfo>> serializer = new Serializer<List<DebriefingInfo>>();
		DebriefInfo = serializer.Load ("XML/DebriefInfo");
	}

	public DebriefingInfo GetDebriefInfo( AssessmentItemReport report )
	{
		if ( DebriefInfo == null )
			return null;
		foreach( DebriefingInfo item in DebriefInfo )
		{
			if ( item.InteractionName == report.InteractionName && report.Result != AssessmentItem.ResultType.Required )
				return item;
		}
		return null;
	}
	
	public void FillTasks()
	{
		if ( report == null || report == null)
			return;

		// load debriefing info
		LoadDebriefInfo();
		
		// clear this
		tasks = new List<AssessmentItemReport>();
		
		// fill tasks from report
		foreach( AssessmentListReport tmpReport in report.Items )
		{
			bool filter = false;
			
			// check filters
			switch( tmpReport.Name )
			{
			case "OBJECTIVE:AIRWAY":
			case "OBJECTIVE:PRIMARY":
				{				
				filter = A;
				// get the time label
				GUILabel tmp = Find("PrimaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:BREATHING":
			case "OBJECTIVE:ADJUNCTS":
				{
				filter = B;
				// get the time label
				GUILabel tmp = Find("AdjuctsTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:CIRCULATION":
			case "OBJECTIVE:SECONDARY":
				{
				filter = C;				
				// get the time label
				GUILabel tmp = Find("SecondaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:TREATMENT":
				{
				filter = D;
				// get the time label
				GUILabel tmp = Find("TreatmentTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			}
			
			// check to see if filter is on
			if ( filter == true )
			{
				AddTasks(tmpReport,tasks);
			}
		}
		
		if ( header != null && button != null )
		{		
			// clear out everything!
			header.Elements.Clear();
			
			int cnt=0;
			
			// all all the tasks as buttons
			foreach( AssessmentItemReport air in tasks )
			{
#if ONLY_CRITICAL_ACTIONS
				// skip everything that is NOT a critical action
				if ( air.CriticalAction == false )
					continue;
#endif				
				if ( air.Level == 0 )
				{
					// clone the taskarea (contains everything)
					GUIHorizontalCommand newbutton = button.Clone() as GUIHorizontalCommand;		
					// find the name of the button
					GUILabel tmp = newbutton.Find("TaskNameText") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Name;
					}
					// find the time field
					tmp = newbutton.Find("TaskYourTime") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Time;
						if ( tmp.text == "00:00" )
							tmp.text = textIncomplete;
					}				
					// find the time field
					tmp = newbutton.Find("TaskAverageTime") as GUILabel;
					if ( tmp != null )
					{
						// change text to incomplete until we have some average times!
						tmp.text = textIncomplete;
					}				
					// change background
					GUIToggle toggle = newbutton.Find("TimeDetailBackground") as GUIToggle;
					if ( toggle != null )
					{
						// change name of button
						toggle.name = "B" + (cnt++).ToString();
						
						// change style depending on state of item
						switch( air.Result )
						{
						case AssessmentItem.ResultType.FailMissing:
						{						
							// set the style
							GUIStyle style = toggle.Skin.FindStyle("decisionbreakdown.task.gray");
							toggle.SetStyle( style );
							break;
						}
						case AssessmentItem.ResultType.Required:
						{
							// set the style
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.green") );
							break;
						}
						default:
						{
							// set the style
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );
							break;
						}
						}

						// if this item triggered a new list then make it RED
						if ( air.TriggeredLists.Count != 0 )
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );
					}
					// add it
					header.Elements.Add(newbutton);
				}
				else if ( air.Level == 1 )
				{
					// subtask
					// clone the taskarea (contains everything)
					GUIHorizontalCommand newbutton = subButton.Clone() as GUIHorizontalCommand;		
					// find the name of the button
					GUILabel tmp = newbutton.Find("subTaskNameText") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Name;
					}
					// find the time field
					tmp = newbutton.Find("subTaskYourTime") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Time;
						if ( tmp.text == "00:00" )
							tmp.text = textIncomplete;
					}				
					// find the time field
					tmp = newbutton.Find("subTaskAverageTime") as GUILabel;
					if ( tmp != null )
					{
						// change text to incomplete until we have some average times!
						tmp.text = textIncomplete;
					}				
					// change background
					GUIToggle toggle = newbutton.Find("subTask01-Label") as GUIToggle;
					if ( toggle != null )
					{
						// change name of button
						toggle.name = "B" + (cnt++).ToString();
						
						// change style depending on state of item
						switch( air.Result )
						{
						case AssessmentItem.ResultType.FailMissing:
						{						
							// set the style
							GUIStyle style = toggle.Skin.FindStyle("decisionbreakdown.task.gray");
							toggle.SetStyle( style );
							break;
						}
						case AssessmentItem.ResultType.Required:
						{
							// set the style
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.green") );
							break;
						}
						default:
						{
							// set the style
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );
							break;
						}
						}
					}
					// add it
					header.Elements.Add(newbutton);
				}
			}
		}		
	}
	
	public void HandleRightPanel( GUIButton b )
	{
		// check name for 'B'
		if ( b.name[0] != 'B' )
			return;
		
		// get index
		string name = b.name;
		// get rid of 'B'
		name = name.Replace("B","");
		// get index
		int index = System.Convert.ToInt32(name);
		// find index in tasks
		AssessmentItemReport item = tasks[index];
		// turn off all button toggles except this one
		List<GUIObject> toggles = header.FindObjectsOfType(typeof(GUIToggle));
		foreach( GUIToggle toggle in toggles )
		{
			if ( toggle.name != b.name )
				toggle.toggle = false;
		}
		// now populate left lower panel
		GUILabel detailtitle = this.Find("TaskDetailTitle") as GUILabel;
		if ( detailtitle != null )
		{
			detailtitle.text = item.Name;
		}
		GUILabel detailsummary = this.Find("taskDetailSummaryText") as GUILabel;
		if ( detailsummary != null )
		{
			detailsummary.text = item.PrettyPrint() + " : " + item.Note;
		}
	}
		
    public override void ButtonCallback(GUIButton button)
    {	
		// default
        base.ButtonCallback(button);
		
		// handle left panel
		if ( button.name == "AirwayToggle" )
		{
			A = (A)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "BreathingToggle" )
		{
			B = (B)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "CirculationToggle" )
		{
			C = (C)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "DiagnosisToggle" )
		{
			D = (D)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "ResusToggle" )
		{
			R = (R)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "PreviousButton" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaDecisionBreakdown";
			msg.className = "DecisionBreakdown";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		// handle click from right panel
		HandleRightPanel(button);

		// main buttons
		if ( button.name == "NextButton" )
		{
			// set start for traumaMainMenu
			TraumaStartScreen.GetInstance().StartXML = "traumaCaseSelection";
			// go to intro screen
			Application.LoadLevel("traumaMainMenu");
			// force timeScale back to normal
			Time.timeScale = 1.0f;
		}
		if ( button.name == "bt_dashboard" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseOverviews";
			msg.className = "CaseOverview";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
    }
}

public class DecisionBreakdown : GUIDialog
{
    public DecisionBreakdown()
        : base()
    {
    }
	
	ScenarioAssessmentReport report;
	
	// important areas to save 
	GUIScrollView header;
	GUIHorizontalCommand button;
	GUIHorizontalCommand subButton;
	GUILabel title;
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);
		
		// get report 
		report = TraumaReportMgr.GetInstance().Report.Report;
		
		GUILabel titleCaseName = Find("titleCaseName") as GUILabel;
		if ( titleCaseName != null )
		{
			if ( TraumaReportMgr.GetInstance().Report.Report != null )
				titleCaseName.text = TraumaReportMgr.GetInstance().Report.Report.Name;
			else
				titleCaseName.text = "No Case";
		}

		// find the task button area
		header = this.Find("TaskScrollBox") as GUIScrollView;
		// find the horizontal element
		button = this.Find("TaskItem") as GUIHorizontalCommand; 
		// find the sub item
		subButton = this.Find ("subTask01") as GUIHorizontalCommand;
		
		// find airway toggle
		GUIToggle airwayToggle = this.Find ("AirwayToggle") as GUIToggle;
		airwayToggle.toggle = true;
		
		// version
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
		
		// fill it
		FillTasks();
	}
	
	bool A=true,B=false,C=false,D=false,R=false;
	
	public override void Update()
	{		
		//if ( Input.GetKeyUp(KeyCode.F) )
		//	FillTasks();		
	}
	
	List<AssessmentItemReport> tasks;

	static string textIncomplete="-----";
	
	public void AddTasks( AssessmentListReport tmpReport, List<AssessmentItemReport> tasks )
	{
		foreach( AssessmentItemReport item in tmpReport.Items  )
		{
			tasks.Add(item);

			// embed list in report
			if ( item.TriggeredLists.Count > 0 )
			{
				foreach( string triggeredList in item.TriggeredLists )
				{
					foreach( AssessmentListReport list in tmpReport.Lists )
					{
						if ( triggeredList == list.Name )
							AddTasks (list,tasks);
					}
				}
			}
		}

#if NOT_EMBEDDED
		// this handles activated lists
		foreach( AssessmentListReport list in tmpReport.Lists )
		{
			AddTasks(list,tasks);
		}
#endif
	}

	public void FillTasks()
	{
		if ( report == null )
			return;
		
		// clear this
		tasks = new List<AssessmentItemReport>();
		
		// fill tasks from report
		foreach( AssessmentListReport tmpReport in report.Items )
		{
			bool filter = false;
			
			// check filters
			switch( tmpReport.Name )
			{
			case "OBJECTIVE:AIRWAY":
			case "OBJECTIVE:PRIMARY":
				{				
				filter = A;
				// get the time label
				GUILabel tmp = Find("PrimaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.MakeXOutOfXString();
				}
				}
				break;
			case "OBJECTIVE:BREATHING":
			case "OBJECTIVE:ADJUNCTS":
				{
				filter = B;
				// get the time label
				GUILabel tmp = Find("AdjunctsTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.MakeXOutOfXString();
				}
				}
				break;
			case "OBJECTIVE:CIRCULATION":
			case "OBJECTIVE:SECONDARY":
				{
				filter = C;				
				// get the time label
				GUILabel tmp = Find("SecondaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.MakeXOutOfXString();
				}
				}
				break;
			case "OBJECTIVE:TREATMENT":
				{
				filter = D;
				// get the time label
				GUILabel tmp = Find("TreatmentTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.MakeXOutOfXString();
				}
				}
				break;
			case "OBJECTIVE:RESUSCITATION":
				{
				filter = R;
				// get the time label
				GUILabel tmp = Find("ResusTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.MakeXOutOfXString();
				}
				}
				break;
			}
			
			// check to see if filter is on
			if ( filter == true )
			{
				AddTasks(tmpReport,tasks);
			}
		}
		
		if ( header != null && button != null )
		{		
			// clear out everything!
			header.Elements.Clear();
			
			int cnt=0;
			
			// all all the tasks as buttons
			foreach( AssessmentItemReport air in tasks )
			{
				if ( air.Level == 0 )
				{
					// clone the taskarea (contains everything)
					GUIHorizontalCommand newbutton = button.Clone() as GUIHorizontalCommand;		
					// find the name of the button
					GUILabel tmp = newbutton.Find("TaskNameText") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Name;
					}
					// find the time field
					tmp = newbutton.Find("TaskYourTime") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Time;
						if ( tmp.text == "00:00" )
							tmp.text = textIncomplete;
					}				
					// find the time field
					tmp = newbutton.Find("TaskAverageTime") as GUILabel;
					if ( tmp != null )
					{
						// change text to incomplete until we have some average times!
						tmp.text = textIncomplete;
					}				
					// change background
					GUIToggle toggle = newbutton.Find("TimeDetailBackground") as GUIToggle;
					if ( toggle != null )
					{
						// change name of button
						toggle.name = "B" + (cnt++).ToString();
						
						// change style depending on state of item
						switch( air.Result )
						{
							case AssessmentItem.ResultType.FailMissing:
							{						
								// set the style
								GUIStyle style = toggle.Skin.FindStyle("decisionbreakdown.task.gray");
								toggle.SetStyle( style );
								break;
							}
							case AssessmentItem.ResultType.Required:
							{
								// set the style
								toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.green") );
								break;
							}
							default:
							{
								// set the style
								toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );
								break;
							}
						}		
						// if this item triggered a new list then make it RED
						if ( air.TriggeredLists.Count != 0 )
							toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );					}
					// add it
					header.Elements.Add(newbutton);
				}
				else if ( air.Level == 1 )
				{
					// subtask
					// clone the taskarea (contains everything)
					GUIHorizontalCommand newbutton = subButton.Clone() as GUIHorizontalCommand;		
					// find the name of the button
					GUILabel tmp = newbutton.Find("subTaskNameText") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Name;
					}
					// find the time field
					tmp = newbutton.Find("subTaskYourTime") as GUILabel;
					if ( tmp != null )
					{
						// change text
						tmp.text = air.Time;
						if ( tmp.text == "00:00" )
							tmp.text = textIncomplete;
					}				
					// find the time field
					tmp = newbutton.Find("subTaskAverageTime") as GUILabel;
					if ( tmp != null )
					{
						// change text to incomplete until we have some average times!
						tmp.text = textIncomplete;
					}				
					// change background
					GUIToggle toggle = newbutton.Find("subTask01-Label") as GUIToggle;
					if ( toggle != null )
					{
						// change name of button
						toggle.name = "B" + (cnt++).ToString();
						
						// change style depending on state of item
						switch( air.Result )
						{
							case AssessmentItem.ResultType.FailMissing:
							{						
								// set the style
								GUIStyle style = toggle.Skin.FindStyle("decisionbreakdown.task.gray");
								toggle.SetStyle( style );
								break;
							}
							case AssessmentItem.ResultType.Required:
							{
								// set the style
								toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.green") );
								break;
							}
							default:
							{
								// set the style
								toggle.SetStyle( toggle.Skin.FindStyle("decisionbreakdown.task.red") );
								break;
							}
						}
					}
					// add it
					header.Elements.Add(newbutton);
				}
			}
		}		
	}
	
	public void HandleRightPanel( GUIButton b )
	{
		// check name for 'B'
		if ( b.name[0] != 'B' )
			return;
		
		// get index
		string name = b.name;
		// get rid of 'B'
		name = name.Replace("B","");
		// get index
		int index = System.Convert.ToInt32(name);
		// find index in tasks
		AssessmentItemReport item = tasks[index];
		// turn off all button toggles except this one
		List<GUIObject> toggles = header.FindObjectsOfType(typeof(GUIToggle));
		foreach( GUIToggle toggle in toggles )
		{
			if ( toggle.name != b.name )
				toggle.toggle = false;
		}
		// now populate left lower panel
		GUILabel detailtitle = this.Find("TaskDetailTitle") as GUILabel;
		if ( detailtitle != null )
		{
			detailtitle.text = item.Name;
		}
		GUILabel detailsummary = this.Find("taskDetailSummaryText") as GUILabel;
		if ( detailsummary != null )
		{
			detailsummary.text = item.PrettyPrint() + " : " + item.Note;
		}
	}
		
    public override void ButtonCallback(GUIButton button)
    {	
		// default
        base.ButtonCallback(button);
		
		// handle left panel
		if ( button.name == "AirwayToggle" )
		{
			A = (A)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "BreathingToggle" )
		{
			B = (B)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "CirculationToggle" )
		{
			C = (C)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "DiagnosisToggle" )
		{
			D = (D)?false:true;
			FillTasks();
			return;
		}
		if ( button.name == "ResusToggle" )
		{
			R = (R)?false:true;
			FillTasks();
			return;
		}		
		if ( button.name == "NextButton" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseDebriefing";
			msg.className = "CaseDebriefingScreen";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
			return;
		}
		if ( button.name == "PreviousButton" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseOverviews";
			msg.className = "CaseOverview";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
			return;
		}
		if ( button.name == "bt_debriefing" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseDebriefing";
			msg.className = "CaseDebriefingScreen";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_dashboard" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseOverviews";
			msg.className = "CaseOverview";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_caseSelect" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseSelection";
			msg.className = "TraumaCaseSelection";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		// handle click from right panel
		HandleRightPanel(button);
		// main buttons
		if ( button.name == "NextButton" )
		{
			// go to intro screen
			//Application.LoadLevel(1);
		}
    }
}

public class CaseOverview : GUIDialog
{
    public CaseOverview()
        : base()
    {
    }
	
	ScenarioAssessmentReport report;
	
	// important areas to save 
	GUIScrollView header;
	GUIHorizontalCommand button;
	GUIHorizontalCommand DPArea;
	GUILabel DPLinePath;
	GUIButton DPButtonI,DPButtonO,DPButtonS;
	
	GUILabel heartbeat;
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);

		report = TraumaReportMgr.GetInstance().Report.Report;

		SetStatusGraphics();
		SetCategoryTimes();
		SetOverallTime();
		
		DPArea = Find("pathHorizontal") as GUIHorizontalCommand;
		DPLinePath = Find("connectingLine") as GUILabel;
		DPButtonI = Find("buttonIncomplete") as GUIButton;
		DPButtonO = Find("buttonOutstanding") as GUIButton;
		DPButtonS = Find("buttonSatisfactory") as GUIButton;
		DPArea.Elements.Clear();
		
		heartbeat = Find("PatientStatus") as GUILabel;
		
		SetDecisionPath();
		LoadAnimations();
		
		CreatePieChart();
		SetFlatline(false);
		
		pieUpdate = Time.time;
	}
	
	List<Texture2D> normal,flat;
	public void LoadAnimations()
	{
		normal = new List<Texture2D>();
		flat = new List<Texture2D>();
		for( int i=0 ; i<10 ; i++)
		{
			string filename;
			filename = "GUI/Animations/heartBeat-flat/heartBeat-flat-" + string.Format("{0:00}", i+1);
			flat.Add( Resources.Load(filename) as Texture2D );
		}
		for( int i=0 ; i<33 ; i++)
		{
			string filename;
			filename = "GUI/Animations/heartBeat-normal/heartBeat-normal-" + string.Format("{0:00}", i+1);
			normal.Add( Resources.Load(filename) as Texture2D );
		}
	}
	
	float lastTime=0.0f;
	float delay=0.9f;
	int currAnim=0, maxAnim;
	bool flatline=false;
	
	public override void Update()
	{
		if ( Time.time > lastTime )
		{
			// set anim
			if ( heartbeat != null )
			{
				if ( flatline == false )
					heartbeat.Style.normal.background = normal[currAnim++];
				else 
					heartbeat.Style.normal.background = flat[currAnim++];
			}
			// loop
			if ( currAnim >= maxAnim )
			{
				currAnim = 0;
				lastTime = Time.time + delay/1.0f;
			}
			else
				lastTime = Time.time+(1.0f-delay)/maxAnim;
		}
		UpdatePieChart();
	}

	public override void Execute()
	{
		base.Execute();

		string hover = GUI.tooltip;
		if ( hover != null )
		{
			GUI.color = Color.black;
			GUI.Label(new Rect(Input.mousePosition.x,Screen.height-Input.mousePosition.y-40.0f,200,50),hover);
		}
	}
	
	void SetFlatline( bool yesno )
	{
		if ( yesno == true )
			maxAnim = flat.Count;
		else
			maxAnim = normal.Count;
	}
	
	Texture2D pieChartTexture = null;	
	float[] pieData = new float[5];
	float[] pieSeek = new float[5];
	GUIPieChartMeshController pieController;
	bool clearPie;
	
	void CreatePieChart()
	{
		GameObject go = GameObject.Find("PieChart") as GameObject;
		if ( go != null )
		{
			pieController = go.GetComponent(typeof(GUIPieChartMeshController)) as GUIPieChartMeshController;
			if ( pieController != null )
			{
				clearPie = true;
				pieData[0] = 100.0f;
				pieData[1] = 0.0f;
				pieData[2] = 0.0f;
				pieData[3] = 0.0f;
				pieData[4] = 0.0f;

				pieSeek[0] = 0.0f;
				pieSeek[1] = 10.0f;
				pieSeek[2] = 50.0f;
				pieSeek[3] = 20.0f;
				pieSeek[4] = 5.0f;
				pieController.SetData(pieData);
				pieChartTexture = pieController.CreateTexture() as Texture2D;
				GUILabel wheel = Find("TimeWheel") as GUILabel;
				wheel.Style.normal.background = pieChartTexture;
			}
		}
	}
	
	
	float pieUpdate=0.0f;
	
	void UpdatePieChart()
	{		
		if ( pieController == null )
			return;
		
		if ( pieUpdate != 0 && Time.time > pieUpdate )
		{
			if ( clearPie == true )
			{
				pieData[0] = 100.0f;
				pieData[1] = 0.0f;
				pieData[2] = 0.0f;
				pieData[3] = 0.0f;
				pieData[4] = 0.0f;
				clearPie = false;
			}
			
			pieUpdate = Time.time + 0.025f - (Time.time-pieUpdate);
			if ( pieData[1] < pieSeek[1] )
			{
				pieData[0] -= 1.0f;
				pieData[1] += 1.0f;
			}
			else
			{
				if ( pieData[2] < pieSeek[2] )
				{
					pieData[0] -= 1.0f;
					pieData[2] += 1.0f;
				}
				else
				{
					if ( pieData[3] < pieSeek[3] )
					{
						pieData[0] -= 1.0f;
						pieData[3] += 1.0f;
					}
					else
					{
						if ( pieData[4] < pieSeek[4] )
						{
							pieData[0] -= 1.0f;
							pieData[4] += 1.0f;
						}
						else
						{
							pieUpdate = 0.0f; //Time.time + 1.0f; // use this to test
							clearPie = true;
						}
					}
				}
			}
			pieController.SetData(pieData);
			pieChartTexture = pieController.CreateTexture() as Texture2D;
			GUILabel wheel = Find("TimeWheel") as GUILabel;
			wheel.Style.normal.background = pieChartTexture;
		}
	}
	
	int lastValue=-1;
	
	string[] lineMap = 
	{
		"GUI/pathLine07",
		"GUI/pathLine06",
		"GUI/pathLine09",
		"GUI/pathLine05",
		"GUI/pathLine04",
		"GUI/pathLine03",
		"GUI/pathLine08",
		"GUI/pathLine02",
		"GUI/pathLine01",
	};
		
	System.Random rand = new System.Random();
	
	public void AddDecisionItem( AssessmentItemReport item )
	{
#if ONLY_CRITICAL_ACTION
		// don't do items that aren't a critical action
		if ( item.CriticalAction == false )
			return;
#endif
		
		int newValue;
		
		if ( item.Result == AssessmentItem.ResultType.Required )
			newValue = 2;
		else if ( item.Result == AssessmentItem.ResultType.FailMissing )
			newValue = 0;
		else
			newValue = 1;
		
#if RANDOM_TEST
		newValue = ( item.Success == true ) ? 2 : 0;
		// for test, randomize 
		newValue = rand.Next(0,3);
#endif
		//
		if ( lastValue != -1 )
		{
			int lineType=0;
			// add a line between last value and this value
			if ( lastValue == 0 && newValue == 0 )
				lineType = 0;
			if ( lastValue == 0 && newValue == 1 )
				lineType = 1;
			if ( lastValue == 0 && newValue == 2 )
				lineType = 2;
			if ( lastValue == 1 && newValue == 0 )
				lineType = 3;
			if ( lastValue == 1 && newValue == 1 )
				lineType = 4;
			if ( lastValue == 1 && newValue == 2 )
				lineType = 5;
			if ( lastValue == 2 && newValue == 0 )
				lineType = 6;
			if ( lastValue == 2 && newValue == 1 )
				lineType = 7;
			if ( lastValue == 2 && newValue == 2 )
				lineType = 8;
			
			// add the line
			GUILabel newLine = DPLinePath.Clone() as GUILabel;
			// make a unique style
			GUIStyle unique = new GUIStyle(newLine.Style);
			newLine.SetStyle(unique);
			// load the right image
			unique.normal.background = Resources.Load(lineMap[lineType]) as Texture2D;
			// add it
			DPArea.Elements.Add(newLine);			
		}
		lastValue = newValue;
		
		// add new decision icon
		if ( newValue == 0 )
		{
			GUIButton newButton = DPButtonI.Clone() as GUIButton;
			newButton.name = "DI:" + item.Note;
			newButton.tooltip = item.Name;
			DPArea.Elements.Add(newButton);
		}
		if ( newValue == 1 )
		{
			GUIButton newButton = DPButtonS.Clone() as GUIButton;
			newButton.name = "DI:" + item.Note;
			newButton.tooltip = item.Name;
			DPArea.Elements.Add(newButton);
		}
		if ( newValue == 2 )
		{
			GUIButton newButton = DPButtonO.Clone() as GUIButton;
			newButton.name = "DI:" + item.Note;
			newButton.tooltip = item.Name;
			DPArea.Elements.Add(newButton);
		}		
	}
	
	public void SetDecisionPath()
	{
		lastValue = -1;
		foreach( AssessmentListReport list in report.Items  )
		{
			foreach( AssessmentItemReport item in list.Items )
			{
				AddDecisionItem(item);
			}
		}
	}
		
	static string textIncomplete="-----";
	
	public void SetStatusGraphics()
	{
		if ( report == null || report == null)
			return;

		// get overall passed/failed
		bool passed = report.Success;

		AssessmentListReport a = report.GetReport("OBJECTIVE:AIRWAY");
		if ( a == null )
			a = report.GetReport ("OBJECTIVE:PRIMARY");

		AssessmentListReport b = report.GetReport("OBJECTIVE:BREATHING");
		if ( b == null )
			b = report.GetReport ("OBJECTIVE:SECONDARY");

		AssessmentListReport c = report.GetReport("OBJECTIVE:CIRCULATION");
		if ( c == null )
			c = report.GetReport ("OBJECTIVE:ADJUNCTS");

		// make sure nothing is NULL
		if ( a == null || b == null || c == null )
		{
			UnityEngine.Debug.LogError ("CaseOverview.SetStatusGraphics() : LIST REPORT MISSING! a=" + a + " b=" + b + " c=" + c);
			return;
		}
		
		bool passedABC = false;
		if ( a.Success && b.Success && c.Success )
			passedABC = true;
		
		AssessmentListReport t = report.GetReport("OBJECTIVE:TREATMENT");

		if ( t == null )
		{
			UnityEngine.Debug.LogError ("CaseOverview.SetStatusGraphics() : LIST REPORT MISSING! t=" + t);
			return;
		}

		bool passedDiag = false;
		if ( t.Success == true )
			passedDiag = true;
		
		bool passedTime = false;

		GUILabel label;
		
		// scenario passed text
		label = Find("PassFailText") as GUILabel;
		if ( label != null )
		{
			if ( passed == true )
				label.text = "SCENARIO PASSED";
			else
				label.text = "SCENARIO FAILED";
		}
		
		label = Find("PassFailOverall") as GUILabel;
		if ( label != null )
		{
			if ( passed == true )
				label.SetStyle(label.Skin.FindStyle("passfail.passed"));
			else 
				label.SetStyle(label.Skin.FindStyle("passfail.failed"));
		}
		
		label = Find("PassFailABC") as GUILabel;
		if ( label != null )
		{
			if ( passedABC == true )
				label.SetStyle(label.Skin.FindStyle("passfail.abc.correct"));
			else 
				label.SetStyle(label.Skin.FindStyle("passfail.abc.incorrect"));
		}
		
		label = Find("PassFailTimeM") as GUILabel;
		if ( label != null )
		{
			if ( passedTime == true )
				label.SetStyle(label.Skin.FindStyle("passfail.timemgmt.correct"));
			else 
				label.SetStyle(label.Skin.FindStyle("passfail.timemgmt.incorrect"));
		}
		
		label = Find("PassFailDiag") as GUILabel;
		if ( label != null )
		{
			if ( passedDiag == true )
				label.SetStyle(label.Skin.FindStyle("passfail.diag.correct"));
			else 
				label.SetStyle(label.Skin.FindStyle("passfail.diag.incorrect"));
		}
	}
	
	public void SetOverallTime()
	{
		if ( report == null )
			return;
	
		// get time (for now this is when report is generated)
		string str = AssessmentMgr.GetInstance().ToTimeString(report.ElapsedTime);
		
		// now set time field
		GUILabel label = Find("timeElapsedNumber") as GUILabel;
		if ( label != null )
		{
			label.text = str;
		}
	}
	
	public void SetCategoryTimes()
	{
		if ( report == null )
			return;
		
		// fill tasks from report
		foreach( AssessmentListReport tmpReport in report.Items )
		{
			// check filters
			switch( tmpReport.Name )
			{
			case "OBJECTIVE:AIRWAY":
			case "OBJECTIVE:PRIMARY":
				{				
				// get the time label
				GUILabel tmp = Find("PrimaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:BREATHING":
			case "OBJECTIVE:ADJUNCTS":
				{
				// get the time label
				GUILabel tmp = Find("AdjunctsTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:CIRCULATION":
			case "OBJECTIVE:SECONDARY":
				{
				// get the time label
				GUILabel tmp = Find("SecondaryTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			case "OBJECTIVE:TREATMENT":
				{
				// get the time label
				GUILabel tmp = Find("TreatmentTime") as GUILabel;
				if ( tmp != null )
				{
					tmp.text = tmpReport.Time;
					if ( tmp.text == "00:00" )
						tmp.text = textIncomplete;
				}
				}
				break;
			}
		}		
	}
	
    public override void ButtonCallback(GUIButton button)
    {	
		// default
        base.ButtonCallback(button);
		
		if ( button.name == "NextButton" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaDecisionBreakdown";
			msg.className = "DecisionBreakdown";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_debriefing" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseDebriefing";
			msg.className = "CaseDebriefingScreen";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_breakdown" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaDecisionBreakdown";
			msg.className = "DecisionBreakdown";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "bt_dashboard" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseOverviews";
			msg.className = "CaseOverview";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonExit" )
		{
			// set start for traumaMainMenu
			TraumaStartScreen.GetInstance().StartXML = "traumaCaseSelection";
			// go to intro screen
			Application.LoadLevel("traumaMainMenu");
			// force timeScale back to normal
			Time.timeScale = 1.0f;
		}
    }
}

public class AssessmentScreen : GUIDialog
{
    public AssessmentScreen()
        : base()
    {
    }
	
    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);
		
        switch (button.name)
        {
            case "buttonConfirm":
                {
                    // kill screen
                    GUIManager manager = GUIManager.GetInstance();
                    manager.Remove(this.parent);
                    // go back to main menu
					Application.LoadLevel("mainmenu");
                    //Brain.GetInstance().PutMessage(new ChangeStateMsg("EndScenario"));
                }
                break;
        }
    }
}

public class GUITest : GUIScreen
{
    public GUITest()
        : base()
    {
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            case "TBTest":
                {
                    GUIManager manager = GUIManager.GetInstance();
                    manager.Remove(this.parent);
                }
                break;
        }
    }
}

public class MainMenu : GUIScreen
{
    public MainMenu()
        : base()
    {
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            // Title screen
            case "StartGame":
                {
                    //parent.NextScreen();
                }
                break;
            case "OptionsScreen":
                {
                }
                break;
            case "ExitGame":
                {
                    //(GUIManager.FindObjectOfType(typeof(GUIManager)) as GUIManager).Remove(parent);
                    Application.Quit();
                }
                break;

            // Case Select
            case "BackArrow":
                {
                    //parent.SetScreenTo(0);
                }
                break;
            case "ConfirmArrow":
                {
                    GUIManager man = GUIManager.GetInstance();
                    man.Remove(parent);
                }
                break;
            case "LeftArrow":
                {
                    //parent.LastScreen();
                }
                break;
            case "RightArrow":
                {
                    //parent.NextScreen();
                }
                break;

            default:
                {
                    //Debug.LogWarning("Button Not Found.");
                }
                break;
        }
    }
}

public class Testimonial : GUIMoviePlayer
{
    public Testimonial()
        : base()
    {
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);
        if (button.name == "nextButton")
        {
            Application.LoadLevel("mainmenu");
        }
    }
}

public class GameHUD : GUIScreen
{
    CameraRailCoordinator crc;
    GUIEditbox chatBar;
	GUIArea chatBarArea;
    DummyInteractObject dioObject;
    GUIObject clockLabel;
    Timer clock;
	
	GUIArea HR,BP,SP;
	GUIArea areaL,areaR;
	GUIArea manuveringControlsArea;
	GUILabel fpsLabel;
	GUILabel wifiLabel;

	GUIArea recentOrders;
	GUIScrollView recentOrdersSV;
	GUIToggle recentOrderTemplate;
	List<ScriptedObject> characters;

	float recentOrdersSeek=480;
	float recentOrdersCurr=480;
	bool recentOrdersOpen=false;
	bool hideHUD = false;
	string escapeText;

	public class SOinfo : IComparable<SOinfo>
	{
		public SOinfo( ScriptedObject obj, ScriptedObject.QueuedScript script, bool killable)
		{
			SO = obj;
			Script = script;
			Killable = killable;
		}
		public ScriptedObject SO;
		public ScriptedObject.QueuedScript Script;
		public bool Killable;

		public int CompareTo( SOinfo info )
		{
			if ( info.Script.TimeQueued > this.Script.TimeQueued )
				return 1;
			else
				return 0;
		}
	}
	List<SOinfo> taskList;
	Dictionary<string,SOinfo> killMap;
	
	Microphone microphone;
	
	Texture2D wifi1,wifi2,wifi3;
		
	bool sayButton,lastSayButton;
	bool infoOpen;

	FilterInteractions fi;

    public GameHUD()
        : base()
    {
    }

    public override void Initialize(ScreenInfo parent)
    {
		GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
		GUIManager.GetInstance().FitToScreen = true;
		GUIManager.GetInstance().Letterbox = true;

		base.Initialize(parent);

        crc = Component.FindObjectOfType(typeof(CameraRailCoordinator)) as CameraRailCoordinator;

        chatBar = Find("Chatbar") as GUIEditbox;
		if ( chatBar != null )
			chatBar.text = escapeText = StringMgr.GetInstance().Get ("PRESS:ESCAPE");

        // Add dummy component
        dioObject = GUIManager.GetInstance().gameObject.AddComponent<DummyInteractObject>();
        dioObject.prettyname = "HUD Dummy Object";
        dioObject.onTeamMenu = false;

        clockLabel = Find("Clock");
        clock = Component.FindObjectOfType(typeof(Timer)) as Timer;
		
		BP = Find("BPArea") as GUIArea;
		SP = Find("SAO2Area") as GUIArea;
		HR = Find("HRArea") as GUIArea;
		
		areaL = this.Find("leftEdgeMovement") as GUIArea;		
		areaR = this.Find("rightEdgeMovement") as GUIArea;
		
		chatBarArea = this.Find("ChatBarArea") as GUIArea;
		manuveringControlsArea = this.Find("ManuveringControlsArea") as GUIArea;
		
		fpsLabel = this.Find("fpsLabel") as GUILabel;
		
		// get images for WIFI
		wifi1 = Resources.Load("GUI/wifi.excellent") as Texture2D;
		wifi2 = Resources.Load("GUI/wifi.moderate") as Texture2D;
		wifi3 = Resources.Load("GUI/wifi.low") as Texture2D;
		wifiLabel = this.Find("wifiLabel") as GUILabel;

		recentOrders = this.Find ("RecentOrdersArea") as GUIArea;
		recentOrderTemplate = this.Find ("buttonRecentOrder01") as GUIToggle;
		recentOrdersSV = this.Find ("recentOrdersScrollView") as GUIScrollView;
		recentOrdersSV.Elements.Clear ();
		GetRecentCommands ();
		recentOrders.Style.contentOffset = new Vector2(450,30);
		killMap = new Dictionary<string,SOinfo>();		
		taskList = new List<SOinfo>();

		sayButton = false;
		lastSayButton = false;
		infoOpen = false;
 
		GUILabel version = Find ("medstarLogo") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}

	void BuildRecentTaskList()
	{
		if ( killMap == null )
			return;
		// sort list
		taskList.Sort();
		// build GUI
		killMap.Clear();
		foreach( SOinfo info in taskList )
		{
			GUIToggle newTask = recentOrderTemplate.Clone () as GUIToggle;
			if ( newTask != null )
			{
				if ( info.Killable == true )
					newTask.text = info.SO.name + " : " + info.Script.script.prettyname + " (Pending)";
				else
					newTask.text = info.SO.name + " : " + info.Script.script.prettyname;
				newTask.UpdateContent();
				newTask.name = "T" + recentOrdersSV.Elements.Count.ToString ();
				killMap[newTask.name] = info;
				// add it
				recentOrdersSV.Elements.Add (newTask);
			}
		}
	}

	void GetRecentCommands()
	{
		// clear
		recentOrdersSV.Elements.Clear ();
		// make new map
		if ( killMap != null )
			killMap.Clear ();
		// make new list
		if ( taskList != null )
			taskList.Clear ();

		// get current tasks
		List<ObjectInteraction> characters = ObjectInteractionMgr.GetInstance().GetCharacters();
		foreach( ObjectInteraction character in characters )
		{
			ScriptedObject SO = character.GetComponent<ScriptedObject>() as ScriptedObject;
			if ( SO != null )
			{
				if ( SO.scriptStack != null )
				{
					// this commands happen after stacked commands (scriptStack or scriptArray?)
					foreach (object o in SO.scriptArray)
					{
						ScriptedObject.QueuedScript q = o as ScriptedObject.QueuedScript;
						if ( q != null /*&& q.executing == false*/ )
						{
							// there are a few tasks we dont want to see in the recent orders list:
							bool skip = false;
							foreach (string s in q.script.triggerStrings){
								if (s == "GO:HOME" || s == "PATIENT:LOGROLL:END" || s == "RECORD:RESULT"){
									skip = true;
									break;
								}
							}
							if (q.script.AddToMenu == false) skip = true;
							if (!skip)
								taskList.Add (new SOinfo(SO,q,!q.executing));
						}
					}
				}
			}
		}
		BuildRecentTaskList();
	}

	float updateRecent;
	void UpdateRecentCommands()
	{
		if ( Time.time > updateRecent )
		{
			GetRecentCommands ();
			updateRecent = Time.time  + 0.5f;
		}
	}
	
	public void SetChatbar( string text )
	{
		if ( chatBar != null )
			chatBar.text = text;
	}

	public void ClearChatbar()
	{
		if ( chatBar != null )
			chatBar.text = "";
	}

	public override void Update()
	{
        if (clock != null && clockLabel != null)
            clockLabel.text = clock.GetTimeText();
		UpdateDeathClock();
		UpdateFPS();
		UpdateRecentCommands();
		//UpdateNetworkSpeed();
		HandleSayButton();
	}

	float deltaTime = 0.0f;
	float fps = 0.0f;
	int fpsCount=0;
	int fpsMax=100;
	
	float wifiUpdate=0.0f;
	float wifiUpdateTime=10.0f;
	float pingStart;
	int wifi=0;
	
	public virtual void pingCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true && data == "ok" )
		{
			float pingTime = Time.time - pingStart;
			pingTime = pingTime*1000.0f;
			
			if ( pingTime <= 60 )
				wifiLabel.Style.normal.background = wifi1;
			else if ( pingTime <= 120 )
				wifiLabel.Style.normal.background = wifi2;
			else
				wifiLabel.Style.normal.background = wifi3;
		}
	}

	void UpdateNetworkSpeed()
	{
		if ( wifiLabel == null )
			return;
		if ( Time.time > wifiUpdate )
		{
			wifiUpdate = Time.time + wifiUpdateTime;
			// ping our server
			WWWForm form = new WWWForm();
        	form.AddField("command", "ping");
			DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,pingCallback);
			pingStart = Time.time;
		}
	}
	
	void UpdateFPS()
	{
		if ( ++fpsCount < fpsMax )
			deltaTime += Time.deltaTime;
		else
		{
			deltaTime = deltaTime / fpsMax;
			fps = 1.0f/deltaTime;
			if ( fps > 99.0f )
				fps = 99.0f;
			deltaTime = 0.0f;
			fpsCount = 0;
		}
			
		fpsLabel.text = ((int)fps).ToString();
	}
	
	void UpdateDeathClock()
	{
		if ( VitalsBehaviorManager.GetInstance() == null )
			return;
		
		float time,xPos;
		Vector2 offset;
		// HR
		time = VitalsBehaviorManager.GetInstance().TimeToDeath("HR");
		xPos = 580.0f-time/(20.0f*60.0f)*580.0f;
		offset = HR.Style.contentOffset;
		offset.x = xPos;
		HR.Style.contentOffset = offset;
		// SP
		time = VitalsBehaviorManager.GetInstance().TimeToDeath("SP");
		xPos = 565.0f-time/(20.0f*60.0f)*565.0f;
		offset = SP.Style.contentOffset;
		offset.x = xPos;
		SP.Style.contentOffset = offset;
		// BP
		time = VitalsBehaviorManager.GetInstance().TimeToDeath("BPSYS");
		xPos = 580.0f-time/(20.0f*60.0f)*580.0f;
		offset = BP.Style.contentOffset;
		offset.x = xPos;
		BP.Style.contentOffset = offset;
	}
	
	public void HandleKeyInput()
	{
		if (Event.current == null )
			return;
		
		if (quickCommand == null || quickCommand.ShowGUI == false )
		{
			if ( quickCommand != null )
				quickCommand.RemoveFocus();
			return;
		}
		
 		if (Event.current.type == EventType.KeyDown )
		{
			if ( Event.current.keyCode == KeyCode.DownArrow )
			{
				quickCommand.IncrementKey();
				Event.current.Use();
			}
			if ( Event.current.keyCode == KeyCode.UpArrow )
			{
				quickCommand.DecrementKey();
				Event.current.Use();
			}
			if ( Event.current.keyCode == KeyCode.Return )
			{
				quickCommand.ReturnKey();
				Event.current.Use();
			}
			if ( Event.current.character == '\n' )
			{
				Event.current.Use();
			}
			if ( Event.current.keyCode == KeyCode.Escape)
			{
				// close the window
				//GUIManager.GetInstance().Remove(quickCommand.Parent);
				//quickCommand.ShowGUI = false;
				// clear the chatbar
				ClearChatbar();
			}
			if ( Event.current.keyCode == KeyCode.Tab)
			{
				if (open == true)
					CloseQC ();
			}
		}
	}

	void HandleRecentOrders()
	{
		int inc=20;

		if ( recentOrdersCurr != recentOrdersSeek )
		{
			if ( recentOrdersSeek > recentOrdersCurr )
			{
				recentOrdersCurr+=inc;
				if ( recentOrdersCurr > 480 )
					recentOrdersCurr = 480;
			}
			else
			{
				recentOrdersCurr-=inc;
				if ( recentOrdersCurr < 40 )
					recentOrdersCurr = 40;
			}
			recentOrders.Style.contentOffset = new Vector3(recentOrdersCurr,30);
		}
	}

	public void ShowRecentOrders(){
		recentOrdersSeek = 40;
		recentOrdersOpen = true;
		// send an ISM for tracking (tutorial uses this)
		InteractMsg imsg = new InteractMsg(null,"SHOW:RECENT:ORDERS");
		InteractStatusMsg ism = new InteractStatusMsg(imsg);
		Brain.GetInstance().PutMessage(ism);
	}

	public void HideRecentOrders(){
		recentOrdersSeek = 480;
		recentOrdersOpen = false;
		// send an ISM for tracking (tutorial uses this)
		InteractMsg imsg = new InteractMsg(null,"HIDE:RECENT:ORDERS");
		InteractStatusMsg ism = new InteractStatusMsg(imsg);
		Brain.GetInstance().PutMessage(ism);
	}
	
    public override void Execute()
    {
		if (hideHUD)
			return;
		HandleQuickCommand();		
		HandleRecentOrders();
		base.Execute();

        if (chatBar != null)
        {
			// handle enter key for chatbar
			Event e = Event.current;
			if ( e != null && e.keyCode == KeyCode.Return )
                HandleChatbar();
		}

		// check side buttons
		if ( areaL != null)
		{
			if ( areaL.GetArea().Contains(Event.current.mousePosition) )
			{
                CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
               	if (lerp != null)
                	lerp.LookLeft();
			}
		}
		if ( areaR != null)
		{
			if ( areaR.GetArea().Contains(Event.current.mousePosition) )
			{
                CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
               	if (lerp != null)
                	lerp.LookRight();
			}
		}
    }

	string lastChatbar="";
	FilterCommandGUI quickCommand;
	string lastFocus="";
	bool open=false;

	void OpenQC()
	{
		// pop open if not already open
		if ( GUIManager.GetInstance().FindScreen("traumaQuickCommand") == null )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaQuickCommand";
			msg.className = "FilterCommandGUI";
			msg.modal = false;
			quickCommand = GUIManager.GetInstance().LoadDialog(msg) as FilterCommandGUI;
			open = true;
		}
	}

	public void CloseQC()
	{
		if ( GUIManager.GetInstance().FindScreen("traumaQuickCommand") != null )
		{
			GUIManager.GetInstance().Remove ("traumaQuickCommand");
			open = false;
		}
	}
				
	public void HandleQuickCommand()
	{
		HandleKeyInput();
		
		string focus = GUI.GetNameOfFocusedControl();
		if ( chatBar.text == escapeText )
		{
			if ( focus == "Chatbar" )
				ClearChatbar();
			return;
		}
		
		// put up quick command on txt change
		if ( chatBar.text != lastChatbar || (focus == "Chatbar" && lastFocus != "Chatbar") )
		{
			lastChatbar = chatBar.text;		
			// open 
			OpenQC();
			// Filter and build results
			FilterInteractions.GetInstance().lastText = lastChatbar;
			quickCommand.Filter(chatBar.text);
		}
		lastFocus = focus;
	}

	public void HandleSayButton()
	{
		if ( sayButton == true )
		{
			if ( lastSayButton == false )
			{
				if (!SAPISpeechManager.HandleSayButton(sayButton))
				{
					// start MIC
					UnityEngine.Debug.Log("GameHUD.HandleSayButton() : start MIC");
					if ( MicrophoneMgr.GetInstance().Microphone != null )
						MicrophoneMgr.GetInstance().Microphone.StartRecording();
				}
			}
			// set this state before we clear
			lastSayButton = sayButton;
			// clear, but this should be set again if the 
			// button is still pushed
			sayButton = false;
		}
		else
		{
			if ( lastSayButton == true )
			{
				if (!SAPISpeechManager.HandleSayButton(sayButton))
				{
				// stop MIC
				UnityEngine.Debug.Log("GameHUD.HandleSayButton() : stop MIC");
				if ( MicrophoneMgr.GetInstance().Microphone != null )
					MicrophoneMgr.GetInstance().Microphone.StopRecording();
				}
			}
			lastSayButton = sayButton;
		}
	}

    public override void ButtonCallback(GUIButton button)
    {		
		base.ButtonCallback(button);

		// log it
		LogMgr.GetInstance().Add (new ButtonClickLogItem("HUD",button.name));

		if ( button.name.Contains ("T") )
		{
			// lookup kill 
			if ( killMap.ContainsKey(button.name))
			{
				SOinfo info = killMap[button.name];
				if ( info.SO.scriptArray.Contains (info.Script) )
				{
					UnityEngine.Debug.LogError ("Script is in list! [" + info.Script.script.prettyname + "]");
					if ( info.Killable == true )
						info.SO.scriptArray.Remove(info.Script);
				}
				else
					UnityEngine.Debug.LogError ("Script is NOT in list! [" + info.Script.script.prettyname + "]");
			}
		}

        switch (button.name)
        {
		case "recentOrdersToggleButton":
			{
				if ( recentOrdersOpen == false )
				{
					// open it
					ShowRecentOrders();
					//recentOrdersSeek = 40;
					//recentOrdersOpen = true;
				}
				else
				{
					HideRecentOrders();
					//recentOrdersSeek = 480;
					//recentOrdersOpen = false;
				}
			}
			break;
        case "RailToggle":
            {
                //CameraRailCoordinator crc = Camera.current.GetComponent<CameraRailCoordinator>();
                if (crc != null)
                    crc.Switch();
            }
            break;

        case "SettingsButton":
            {
				MenuLoader loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
				if ( loader != null )
				{
#if GOTO_TUTORIAL
					loader.StartCase ("trauma05MissionStart","Tutorial Case");
#endif
#if GOTO_ASSESSMENT
					loader.GotoAssessment();
#endif
#if GOTO_PAUSE
					DialogMsg msg = new DialogMsg();
					msg.xmlName = "traumaPauseMenu";
					msg.className = "TraumaPauseMenu";
					msg.modal = true;
					GUIManager.GetInstance().LoadDialog(msg);
#endif
				}
            }
            break;

		case "HelpButton":
			{
				if ( infoOpen == false )
				{
					DialogMsg dmsg = new DialogMsg();
					dmsg.xmlName = "TraumaHelperHud";
					dmsg.className = "TraumaHelperHUD";
					dmsg.modal = false;
					GUIManager.GetInstance().LoadDialog (dmsg);
					infoOpen = true;
				}
				else
				{
					GUIManager.GetInstance().Remove ("HelperHud");
					infoOpen = false;
				}
			}
			break;

        case "DiagnosticsButton":
            {
                dioObject.CompileInteractions("diagnostics");
            }
            break;

        case "TreatmentButton":
            {
                dioObject.CompileInteractions("treatment");
            }
            break;

        case "ChatEnter":
            {			
				// chatbar (Say Button) handler
				HandleChatbar();
				// set sayButton
				sayButton = true;
			}
            break;

        case "LeftShiftButton":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    // Move forward on the rail
                    if (rb.pushed && crc != null)
                        crc.GUIInput(-1);
                    else if (!rb.pushed && crc != null)
                        crc.GUIInput(0);
                }
            }
            break;

        case "RightShiftButton":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    // Move forward on the rail
                    if (rb.pushed && crc != null)
                        crc.GUIInput(1);
                    else if (!rb.pushed && crc != null)
                        crc.GUIInput(0);
                }
            }
            break;

        case "CameraLeft":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    if (rb.pushed)
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookLeft();
                    }
                }
            }
            break;

        case "CameraRight":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    if (rb.pushed)
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookRight();
                    }
                }
            }
            break;

        case "CameraUp":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    if (rb.pushed)
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookUp();
                    }
                }
            }
            break;

        case "CameraDown":
            {
                GUIRepeatButton rb = button as GUIRepeatButton;
                if (rb != null)
                {
                    if (rb.pushed)
                    {
                        CameraLERP lerp = Camera.main.GetComponent<CameraLERP>();
                        if (lerp != null)
                            lerp.LookDown();
                    }
                }
            }
            break;

        case "CameraReset":
            {
                if (crc != null)
                {
                    crc.Reset();
                }
            }
            break;
        }
    }

    void HandleChatbar()
    {
#if ISSUE_URL_COMMAND
		if ( chatBar.text.ToLower().Contains("url=") )
		{
			NluMgr.GetInstance().Utterance(chatBar.text.ToLower(),null);
			chatBar.text = "";
		}
#endif
#if USE_TEXT_NLU
        // Check NLU dialogue for how to send this out
        if (chatBar == null || chatBar.text.Length == 0) return;
        NluPrompt prompt = NluPrompt.GetInstance();
        NluMgr nluMgr = NluMgr.GetInstance();
        if (prompt == null || nluMgr == null) return;

        // send to Nlu
        nluMgr.SetUtteranceCallback(new NluMgr.UtteranceCallback(prompt.Callback));
        nluMgr.SetErrorCallback(new NluMgr.ErrorCallback(prompt.ErrorCallback));
        nluMgr.Utterance(chatBar.text, "nurse");

        chatBar.text = "";
#endif
    }
	
	public override void PutMessage(GameMsg msg){
		
		base.PutMessage(msg);
		
		GUIScreenMsg screenMsg = msg as GUIScreenMsg;
		if (screenMsg != null){
			string tokenValue;
			
			// messages to show or hide elements of the HUD <area>=<on,off>
			if (GetToken (screenMsg.arguments, "cameracontrols",out tokenValue)){
				ShowMovementControls(tokenValue.ToLower()=="on");	
			}
			if (GetToken (screenMsg.arguments, "chatbar",out tokenValue)){
				ShowChatBar(tokenValue.ToLower()=="on");	
			}
			if (GetToken (screenMsg.arguments, "chatbar-say",out tokenValue)){
				ShowChatBarSay(tokenValue.ToLower()=="on");	
			}
			if (GetToken (screenMsg.arguments, "hidehud",out tokenValue)){
				hideHUD = (tokenValue.ToLower()=="on");	
			}
			if (GetToken (screenMsg.arguments, "quickCommand",out tokenValue)){
				if ( tokenValue.ToLower()=="on")
					OpenQC();
				else
					CloseQC();
			}
			if (GetToken (screenMsg.arguments, "showinfo",out tokenValue)){ // show/hide the helperhud
				if (infoOpen != (tokenValue.ToLower()=="on")){
					if ( infoOpen == false )
					{
						DialogMsg dmsg = new DialogMsg();
						dmsg.xmlName = "TraumaHelperHud";
						dmsg.className = "TraumaHelperHUD";
						dmsg.modal = false;
						GUIManager.GetInstance().LoadDialog (dmsg);
						infoOpen = true;
					}
					else
					{
						GUIManager.GetInstance().Remove ("HelperHud");
						infoOpen = false;
					}
				}
			}
			// allow adding an ISM to a button
			if (GetToken (screenMsg.arguments, "buttonism",out tokenValue)){
				string buttonName = "";
				if (GetToken (screenMsg.arguments, "buttonname",out buttonName)){
					GUIButton btn = Find ( buttonName ) as GUIButton;
					if (btn != null)
						btn.interactStatusMsg = tokenValue;
				}
			}
		}
	}
	
	void ShowMovementControls(bool show){
		manuveringControlsArea.visible = show;
		if ( areaL != null && areaR != null )
		{
			areaL.visible = show;
			areaR.visible = show;
		}
		// this isnt pretty, but since the Camera Rail Coordinator in Core, it can't check on this setting to disable movement keyboard shortcuts
		// so we are going to set a toggle on that class to prevent movement when the movement controls are hidden.
		CameraRailCoordinator crc = Component.FindObjectOfType<CameraRailCoordinator> ();
		if (crc != null)
			crc.bAllowMovement = show;
	}

	
	void ShowChatBar(bool show)
	{
		// disable this for now because the chatbar appears to disappear
		// when the decision panel comes up...
		return;

		chatBarArea.visible = show;
		GUIEditbox eb = chatBarArea.Find("Chatbar") as GUIEditbox;
		if ( eb != null )
			eb.visible = show;
	}

	void ShowChatBarSay(bool show)
	{
		GUIEditbox eb = chatBarArea.Find("Chatbar") as GUIEditbox;
		if ( eb != null )
			eb.visible = show;
	}
}

public class LoadingScreen : GUIScreen
{
    static string LoadGameURL = "http://unity.sitelms.org/Trauma/TraumaGame.unity3d";
    static string LoadGameScene = "incoming";

    /// Delete section for real Loading screen
    struct TimerData
    {
        public float time;
        public float percentage;
    }
    TimerData[] data = new TimerData[10];
    float timer;
    int last = 0;
    ////////

    int maxWidth;
    float percentage;
    bool run = true;

    List<GUILoadingBar> bars;

    public LoadingScreen()
        : base()
    {
    }

    protected void LoadCompleted()
    {
        run = false;
        percentage = 1f;

        GUIManager manager = GUIManager.GetInstance();
        Application.LoadLevel(LoadGameScene);
    }

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
        bars = new List<GUILoadingBar>();
        // Find loading bars
        List<GUIObject> temp = FindObjectsOfType(typeof(GUILoadingBar));
        foreach (GUIObject obj in temp)
            bars.Add(obj as GUILoadingBar);

        // Delete
        for (int i = 0; i < 10; i++)
        {
            data[i].time = 1f * (i + 1);
            data[i].percentage = (i + 1) / 10f;
        }
        //
#if LOAD_ASSETS_VIA_WEB
        if (Application.CanStreamedLevelBeLoaded(LoadGameScene) == true)
        {
            // already in memory
            Application.LoadLevel(LoadGameScene);
            // set percentage 100%
            percentage = 1.0f;
        }
        else
        {
            // need to load from the web
            AssetLoader.GetInstance().LoadSceneURL(LoadGameURL, LoadGameScene);
        }
#endif
    }

    public override void Execute()
    {
        base.Execute();
        //return;     // Remove when the timer stuff is ready
        // Check loading
        if (run)
            timer += Time.deltaTime;

#if LOAD_ASSETS_VIA_WEB
        percentage = AssetLoader.GetInstance().GetProgress();
#else
        // Delete
        if (run && timer > data[last].time)
        {
            if (last + 1 == 10)
                LoadCompleted();
            else
            {
                percentage = data[last].percentage;
                last++;
            }
        }
#endif
        //
        foreach (GUILoadingBar bar in bars)
            bar.Percentage = percentage;
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            default:
                {
                    Debug.LogWarning("Button not found");
                }
                break;
        }
    }
}

public class IncomingGUI : GUIScreen
{
    // teamRoles
    AudioSource audio;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
        GameObject obj = GameObject.Find("WalkieTalkie01");
        if (obj != null && obj.audio != null)
            audio = obj.audio;
    }

    public override void Execute()
    {
        base.Execute();

        if (audio != null && (audio.time > audio.clip.length || !audio.isPlaying))
            Application.LoadLevel("teamRoles");
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            default:
                {
                    Debug.LogWarning("Button not found");
                }
                break;
        }
    }
}

public class TeamRoles : GUIScreen
{
    //traumaBayEnvTest
    AudioSource audio;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
        GameObject obj = GameObject.Find("AudioSource");
        if (obj != null && obj.audio != null)
            audio = obj.audio;
    }

    public override void Execute()
    {
        base.Execute();

        if (audio != null && (audio.time > audio.clip.length || !audio.isPlaying))
            Application.LoadLevelAsync("Trauma_05");
    }

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);

        switch (button.name)
        {
            default:
                {
                    Debug.LogWarning("Button not found");
                }
                break;
        }
    }
}

public class XRayScreenGUI : GUIDialog
{
	public override void Load(DialogMsg msg)
	{
		// ScreenInfo.LoadFromFile calls initialize on all it's screens,
		// then, if the class passed in is a Dialog, calls load.  This wipes out the Initialize with the file version...
		//base.Load(msg); // this will take care of overloading button labels, etc.

		// can we look for a command to drill us right into the CT index ?

		string requestedScreen = "";
		if ( GetToken(msg.arguments,"screen",out requestedScreen)){
			parent.SetScreenTo(requestedScreen);
		}

		UpdateRecords();
	}



	public void UpdateRecords()
	{
		// find the elements we need to manipulate
		GUILabel xrayTotalText = Find("xrayIcon.totalTag") as GUILabel;
		GUILabel xrayNew = Find("xrayNewRecordLabel") as GUILabel;
		GUILabel fastTotalText = Find("fastIcon.totalTag") as GUILabel;
		GUILabel fastNew = Find("fastNewRecordLabel") as GUILabel;
		GUILabel ctTotalText = Find("ctIcon.totalTag") as GUILabel;
		GUILabel ctNew = Find("ctNewRecordLabel") as GUILabel;

		// count new and total records for each category and set the labels.

		Patient patient;
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;

		PatientRecord pRecord = patient.GetPatientRecord();
		int takenCount=0;
		int newCount = 0;


		foreach (ScanRecord record in pRecord.XRayRecords){
			if (record.TimeTaken != null && record.TimeTaken != ""){
				takenCount++;
				if (record.Assessment == null || record.Assessment == "") // can only be new if it's been taken
					newCount++;
			}
		}
		xrayTotalText.text = takenCount.ToString();
		if (newCount == 0){
			xrayNew.visible = false;
		}
		else
		{
			xrayNew.visible = true;
			xrayNew.text = newCount.ToString()+" New!";
		}

		takenCount = 0;
		newCount = 0;
		foreach (ScanRecord record in pRecord.FastRecords){
			if (record.TimeTaken != null && record.TimeTaken != ""){
				takenCount++;
				if (record.Assessment == null || record.Assessment == "") 
					newCount++;
			}
		}
		fastTotalText.text = takenCount.ToString();
		if (newCount == 0){
			fastNew.visible = false;
		}
		else
		{
			fastNew.visible = true;
			fastNew.text = newCount.ToString()+" New!";
		}

		takenCount=0;
		newCount = 0;
					
		foreach (ScanRecord record in pRecord.CTRecords){
			if (record.TimeTaken != null && record.TimeTaken != ""){
				takenCount++;
				if (record.Assessment == null || record.Assessment == "") 
					newCount++;
			}
		}
		ctTotalText.text = takenCount.ToString();
		if (newCount == 0){
			ctNew.visible = false;
		}
		else
		{
			ctNew.visible = true;
			ctNew.text = newCount.ToString()+" New!";
		}
	}

	// provide a button callback to leave the viewer when the dialog is closed
	public override void ButtonCallback(GUIButton button)
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("XRayScreenGUI",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
		
		if (button.name.Contains ("exitButton")){
			// kick off an interaction to leave the viewer
			InteractMsg imsg = new InteractMsg(null,"LEAVE:VIEWER");
			InteractStatusMsg ism = new InteractStatusMsg(imsg);
			Brain.GetInstance().PutMessage(ism);
		}
	}
}


public class XRayIndexGUI : GUIDialog
{
	Dictionary<GUIButton,ScanRecord> records;

	//A list of styles holding xray images per ScanRecord
	//used in button call back to easily match a button click to its
	//corresponding record image.
	List<GUIStyle> styleList; 

	Texture2D listTexture;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
		
		records = new Dictionary<GUIButton, ScanRecord>();
		
		Patient patient;
		
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;

		GUIHorizontalCommand templateCommand = Find ("xrayImageHorz01") as GUIHorizontalCommand;
		GUIScrollView indexScrollView = Find ("xrayListScrollView") as GUIScrollView;
		// we can duplicate the label and line, too...
		indexScrollView.Elements.Clear();
		
		int xrayCount = 0;
		// we could easily handle a list of xrays here.
		// the Patient.xray needs to be a data struct with things like
		// time taken, etc, and the ScreenTarget of this viewer with the right input buttons
		// for the case at hand.

		styleList = new List<GUIStyle>();

		foreach (ScanRecord record in patient.GetPatientRecord().XRayRecords){
			if (record.TimeTaken != null && record.TimeTaken != ""){

				Texture2D thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;
				listTexture = thumbnailTexture;
				GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
				newEntry.name = record.Region+"XrayHorz";
				// we need to make a copy of the style for each button so they can be different.
				GUIButton thumbButton = newEntry.Find("xrayImage01") as GUIButton;
				GUIButton wideButton = newEntry.Find("xrayImageShortDetail") as GUIButton;

				// rename this so we can detect which was hit.
				thumbButton.name = "ViewXray"+xrayCount;
				wideButton.name = "ViewXray"+xrayCount;

				xrayCount++;

				GUIStyle newStyle = new GUIStyle(thumbButton.Style);
				thumbButton.SetStyle(newStyle);
				thumbButton.Style.normal.background = thumbnailTexture;
				thumbButton.Style.hover.background = thumbnailTexture;
				thumbButton.Style.active.background = thumbnailTexture;

				styleList.Add(newStyle);

				thumbButton.screenTarget = "xrayImageBreakdown";
				wideButton.screenTarget = "xrayImageBreakdown";

				wideButton.text = "Xray Taken "+record.TimeTaken+" - "+record.Region;
				indexScrollView.Elements.Add(newEntry);

				records.Add(thumbButton,record);
				records.Add(wideButton,record);
			}
		}

/*		
		if (patient.ChestXRAY != "" && patient.ChestXRAY != "none"){
			Texture2D xrayTexture = Resources.Load("Scans/"+patient.ChestXRAY) as Texture2D;
			if (xrayTexture != null){
				// we need to make a copy of the style for each button so they can be different.
				GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
				newEntry.name = "chestXrayHorz";
				// we need to make a copy of the style for each button so they can be different.
				GUIButton newButton = newEntry.Find("xrayImage01") as GUIButton;
				GUILabel newLabel = newEntry.Find("xrayImageShortDetail") as GUILabel;
				// rename this so we can detect which was hit.
				newButton.name = "ViewXray"+patient.ChestXRAY;
				GUIStyle newStyle = new GUIStyle(newButton.Style);
				newButton.SetStyle(newStyle);
				newButton.Style.normal.background = xrayTexture;
				newButton.Style.hover.background = xrayTexture;
				newButton.Style.active.background = xrayTexture;
				newButton.screenTarget = "xrayImageBreakdown";
				newLabel.text = "Xray Taken "+patient.GetPatientRecord().GetXRayRecord("chest").TimeTaken+" - Chest";
				indexScrollView.Elements.Add(newEntry);				
				records[newButton] = patient.GetPatientRecord().GetXRayRecord("chest");
				xrayCount++;	
			}
		}
		if (patient.PelvicXRAY != "" && patient.PelvicXRAY != "none"){
			Texture2D xrayTexture = Resources.Load("Scans/"+patient.PelvicXRAY) as Texture2D;
			if (xrayTexture != null){
				// we need to make a copy of the style for each button so they can be different.
				GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
				newEntry.name = "pelvicXrayHorz";
				// we need to make a copy of the style for each button so they can be different.
				GUIButton newButton = newEntry.Find("xrayImage01") as GUIButton;
				GUILabel newLabel = newEntry.Find("xrayImageShortDetail") as GUILabel;
				// rename this so we can detect which was hit.
				newButton.name = "ViewXray"+patient.PelvicXRAY;
				GUIStyle newStyle = new GUIStyle(newButton.Style);
				newButton.SetStyle(newStyle);
				newButton.Style.normal.background = xrayTexture;
				newButton.Style.hover.background = xrayTexture;
				newButton.Style.active.background = xrayTexture;
				newButton.screenTarget = "xrayImageBreakdown";
				newLabel.text = "Xray Taken "+patient.GetPatientRecord().GetXRayRecord("pelvis").TimeTaken+" - Pelvis";
				indexScrollView.Elements.Add(newEntry);				
				records[newButton] = patient.GetPatientRecord().GetXRayRecord("pelvis");
				xrayCount++;
			}
		}	
*/

		if (xrayCount==0){
			GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
			newEntry.name = "xrayHorzNone";
			// we need to make a copy of the style for each button so they can be different.
			GUIButton newButton = newEntry.Find("xrayImage01") as GUIButton;
			GUIButton newButton2 = newEntry.Find("xrayImageShortDetail") as GUIButton;
			//GUILabel newLabel = newEntry.Find("xrayImageShortDetail") as GUILabel;
			// rename this so we can detect which was hit.
			newEntry.Elements.Remove(newButton); // take away the button
			//newLabel.text = "No XRays Available";
			newButton2.text = "No XRays Available";
			indexScrollView.Elements.Add(newEntry);
		}
    }
		
		// provide a button callback to pass the desired xray to the detailed view
	public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("XRAYIndexGUI",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);

		if (button.name.Contains ("ViewXray")){
			// set up the ScreenTarget
			// find the screentarget
			GUIScreen target = Parent.FindScreen(button.screenTarget);
			if (target != null){
				GUIButton displayButton = target.Find ("xrayImage01") as GUIButton;

				if (displayButton != null){
					// should probably create a new style here...
					int index = Convert.ToInt32(button.name.Replace("ViewXray", ""));		
					GUIStyle newStyle = new GUIStyle(styleList[index]);
					displayButton.Style.normal.background = newStyle.normal.background;


				} 
				// assign some label texts here as well...
				ScanRecord record = records[button];
				if (record != null){
					// mark assessment as viewed
					record.Assessment = "viewed";
					// update viewer
					XRayScreenGUI screen = GUIManager.GetInstance().FindScreenByType<XRayScreenGUI>() as XRayScreenGUI;
					if ( screen != null )
						screen.UpdateRecords();
					//
					GUILabel label1 = target.Find ("xrayTakenText") as GUILabel;
					if (label1 != null)
						label1.text = record.TimeTaken;
					GUILabel label2 = target.Find ("xrayTitle") as GUILabel;
					if (label2 != null)
						label2.text = record.Region;
					/*
					GUILabel fluidText = target.Find ("xray01Fluid-Text") as GUILabel;
					GUILabel fractureText = target.Find ("xray01Fracture-Text") as GUILabel;
					GUILabel placementText = target.Find ("xray01Placement-Text") as GUILabel;
					string assessment = "";
					if (GetToken(record.Assessment,"fluid",out assessment)){ // also 2817 and 
						fluidText.text = assessment;	
					}
					if (GetToken(record.Assessment,"placement",out assessment)){
						placementText.text = assessment;	
					}
					if (GetToken(record.Assessment,"fracture",out assessment)){
						fractureText.text = assessment;	
					}
					*/
				}
			}
		}
    }
}

public class FASTIndexGUI : GUIDialog
{
//	Dictionary<GUIButton,FastRecord> records;
	
	bool sessionAbdomen = false;
	GUIButton buttonAbdomen;
	bool sessionChest = false;
	GUIButton buttonChest;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

//		records = new Dictionary<GUIButton, FastRecord>();
		
		// we are going to cheat here, and just handle one abdomen session and one chest scan.
		// the general case would require building session structure that could contain arbitrary collections,
		// but to get to where we currently need to go, we'll cheat.

		Patient patient;
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		
		if (patient.GetPatientRecord().GetFastRecord("perihepatic").TimeTaken != null && patient.GetPatientRecord().GetFastRecord("perihepatic").TimeTaken != "")
			sessionAbdomen = true;
		if (patient.GetPatientRecord().GetFastRecord("chest").TimeTaken != null && patient.GetPatientRecord().GetFastRecord("chest").TimeTaken != "")
			sessionChest = true;	
		
		// find the fastImageHoriz01 object, we will remove that and replace it with a clone for each fast Session 
		GUIHorizontalCommand templateCommand = Find ("fastImageHorz01") as GUIHorizontalCommand;
		GUIScrollView indexScrollView = Find ("fastListScrollView") as GUIScrollView;
		// we can duplicate the label and line, too...
		indexScrollView.Elements.Clear();

		if (!sessionChest && !sessionAbdomen){
			GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
			newEntry.name = "fastSessionNone";
			//we need to make a copy of the style for each button so they can be different.
			GUIButton thumbButton = newEntry.Find("fastImage01") as GUIButton;
			GUIButton wideButton = newEntry.Find ("fastImageShortDetail") as GUIButton;

			//rename this so we can detect which was hit.
			newEntry.Elements.Remove(thumbButton); // take away the button
			wideButton.text = "No FAST Sessions Available";

			indexScrollView.Elements.Add(newEntry);

		}


		if (sessionAbdomen){
			Texture2D thumbnailTexture = Resources.Load("Scans/"+patient.GetPatientRecord().GetFastRecord("perihepatic").Thumbnail) as Texture2D;
			GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
			newEntry.name = "fastSessionAbdomen";
			// we need to make a copy of the style for each button so they can be different.
			GUIButton thumbButton = newEntry.Find("fastImage01") as GUIButton;
			GUIButton wideButton = newEntry.Find ("fastImageShortDetail") as GUIButton;

			// rename this so we can detect which was hit.
			thumbButton.name = "buttonFastAbdomen";
			wideButton.name = "buttonFastAbdomen";

			GUIStyle newStyle = new GUIStyle(thumbButton.Style);
			thumbButton.SetStyle(newStyle);
			thumbButton.Style.normal.background = thumbnailTexture;
			thumbButton.Style.hover.background = thumbnailTexture;
			thumbButton.Style.active.background = thumbnailTexture;

			thumbButton.screenTarget = "fastImageBreakdown";
			wideButton.screenTarget = "fastImageBreakdown";

			wideButton.text = "Fast Taken "+patient.GetPatientRecord().GetFastRecord("perihepatic").TimeTaken+" - Abdomen";
			indexScrollView.Elements.Add(newEntry);

		}


		if (sessionChest){
			Texture2D thumbnailTexture = Resources.Load("Scans/"+patient.GetPatientRecord().GetFastRecord("chest").Thumbnail) as Texture2D;
			GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
			newEntry.name = "fastSessionChest";
			// we need to make a copy of the style for each button so they can be different.
			GUIButton thumbButton = newEntry.Find("fastImage01") as GUIButton;
			GUIButton wideButton = newEntry.Find("fastImageShortDetail") as GUIButton;

			// rename this so we can detect which was hit.
			thumbButton.name = "buttonFastChest";
			wideButton.name = "buttonFastChest";

			GUIStyle newStyle = new GUIStyle(thumbButton.Style);
			thumbButton.SetStyle(newStyle);
			thumbButton.Style.normal.background = thumbnailTexture;
			thumbButton.Style.hover.background = thumbnailTexture;

			thumbButton.screenTarget = "fastImageBreakdown";
			wideButton.screenTarget = "fastImageBreakdown";

			wideButton.text = "Fast Taken "+patient.GetPatientRecord().GetFastRecord("chest").TimeTaken+" - Chest";
			indexScrollView.Elements.Add(newEntry);
		}
    }

		
		// provide a button callback to setup the desired layout of the breakdown view
    public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("FASTIndexGUI",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
		
		if (button.name.Contains("buttonFastAbdomen")){
			// set up the label of the Breakdown to read "abdomen" so the screen will know how to set itself up
			FastViewer viewer = Parent.FindScreen(button.screenTarget) as FastViewer;
			viewer.SetupBreakdown("Abdomen");

		}
		if (button.name.Contains("buttonFastChest")){
			// set up the label of the Breakdown to read "abdomen" so the screen will know how to set itself up
			FastViewer viewer = Parent.FindScreen(button.screenTarget) as FastViewer;
			viewer.SetupBreakdown("Chest");
		}
    }
}

public class FastViewer : GUIDialog
{
	Dictionary<GUIButton,ScanRecord> records;
	
	// declare all the elements we have to manipulate
	GUILabel regionPerihepatic;
	GUILabel regionPerisplenic;
	GUILabel regionPelvis;
	GUILabel regionPericardium;
	
	GUIButton buttonPerihepatic;
	GUIButton buttonPerisplenic;
	GUIButton buttonPelvis;
	GUIButton buttonPericardium;
	
	GUILabel fastTakenText;
	GUILabel fastTitle;
	GUILabel fluidText;
	GUILabel placementText;
	
	GUIMovie fastImage01;
	
	Patient patient;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		
		records = new Dictionary<GUIButton, ScanRecord>(); // used by the button callback
		
	// locate all the elements we have to manipulate
//		regionPerihepatic = Find("regionPerihepatic") as GUILabel;
//		regionPerisplenic = Find("regionPerisplenic") as GUILabel;
//		regionPelvis = Find("regionPelvis") as GUILabel;
//		regionPericardium = Find("regionPericardium") as GUILabel;
	
		buttonPerihepatic = Find("buttonPerihepatic01") as GUIButton;
		buttonPerisplenic = Find("buttonPerisplenic01") as GUIButton;
		buttonPelvis = Find("buttonPelvis01") as GUIButton;
		buttonPericardium = Find("buttonPericardium") as GUIButton;
	
		fastTakenText = Find ("fastTakenText") as GUILabel;
		fastTitle = Find ("fastTitle") as GUILabel;
		fluidText = Find ("fast01Fluid-Text") as GUILabel;
		placementText = Find ("fast01Placement-Text") as GUILabel;
	
		fastImage01 = Find("fastImage01") as GUIMovie;
	

    }
	
	public void SetupBreakdown(string session){ // called by the index when the button is hit
		fastTitle.text = session;
		if (session == "Abdomen"){ // this comes in as Abdomen or Chest, we change to the region
			// be sure all 4 buttons and labels are visible
//			regionPerihepatic.visible = true;
//			regionPerisplenic.visible = true;
//			regionPelvis.visible = true;
//			regionPericardium.visible = true;
	
			buttonPerihepatic.visible = true;
			buttonPerisplenic.visible = true;
			buttonPelvis.visible = true;
			buttonPericardium.visible = true;
			
			// make the first label read Perihepatic 
//			regionPerihepatic.text = "Perihepatic";
			
			// We have to create duplicate styles so each button can have a different thumbnail
			// Load up thumbnails from records, link buttons to records with dictionary
			GUIStyle newStyle = new GUIStyle(buttonPerihepatic.Style);
			ScanRecord record = patient.GetPatientRecord().GetFastRecord("perihepatic");
			Texture2D thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;

			newStyle.normal.background = thumbnailTexture;
			newStyle.hover.background = thumbnailTexture;
			newStyle.active.background = thumbnailTexture;
			buttonPerihepatic.SetStyle(newStyle);
			records[buttonPerihepatic]=record;
			
			DisplayRecord(record);
			
			newStyle = new GUIStyle(buttonPerihepatic.Style);
			record = patient.GetPatientRecord().GetFastRecord("perisplenic");
			thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;
			newStyle.normal.background = thumbnailTexture;
			newStyle.hover.background = thumbnailTexture;
			newStyle.active.background = thumbnailTexture;
			buttonPerisplenic.SetStyle(newStyle);
			records[buttonPerisplenic]=record;
			
			newStyle = new GUIStyle(buttonPerihepatic.Style);
			record = patient.GetPatientRecord().GetFastRecord("pelvis");
			thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;
			newStyle.normal.background = thumbnailTexture;
			newStyle.hover.background = thumbnailTexture;
			newStyle.active.background = thumbnailTexture;
			buttonPelvis.SetStyle(newStyle);
			records[buttonPelvis]=record;
			
			newStyle = new GUIStyle(buttonPerihepatic.Style);
			record = patient.GetPatientRecord().GetFastRecord("pericardium");
			thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;
			newStyle.normal.background = thumbnailTexture;
			newStyle.hover.background = thumbnailTexture;
			newStyle.active.background = thumbnailTexture;
			buttonPericardium.SetStyle(newStyle);
			records[buttonPericardium]=record;
		}
		else // assume chest
		{
			// hide 3 buttons and labels
//			regionPerihepatic.visible = true;
//			regionPerisplenic.visible = false;
//			regionPelvis.visible = false;
//			regionPericardium.visible = false;
	
			buttonPerihepatic.visible = true;
			buttonPerisplenic.visible = false;
			buttonPelvis.visible = false;
			buttonPericardium.visible = false;
			
			// make the first label read Perihepatic 
//			regionPerihepatic.text = "Chest";
			
			// We have to create duplicate styles so each button can have a different thumbnail
			// Load up thumbnails from records, link buttons to records with dictionary
			GUIStyle newStyle = new GUIStyle(buttonPerihepatic.Style);
			ScanRecord record = patient.GetPatientRecord().GetFastRecord("chest");
			Texture2D thumbnailTexture = Resources.Load("Scans/"+record.Thumbnail) as Texture2D;
			newStyle.normal.background = thumbnailTexture;
			newStyle.hover.background = thumbnailTexture;
			newStyle.active.background = thumbnailTexture;
			buttonPerihepatic.SetStyle(newStyle);
			records[buttonPerihepatic] = record;
			
			DisplayRecord(record);
		}			
	}
	
	float playNext=-1f;
	public override void Update()
	{
		if ( playNext != -1 && Time.time > playNext )
		{
			playNext = -1f;
			fastImage01.Stop();
			fastImage01.Loop(true);
			fastImage01.Play();
		}
	}
	
	void DisplayRecord(ScanRecord record){
					
		// load up the selected movie and the currently viewing label, and the user assessment into the labels
		// the movie should still be cached in the record from it's earlier viewing
		fastImage01.SetFilename (record.Filename);
		playNext = Time.time + 0.1f;
		// parse out the arguments for fluid and placement
		record.Assessment = "viewed";
		// update viewer
		XRayScreenGUI screen = GUIManager.GetInstance().FindScreenByType<XRayScreenGUI>() as XRayScreenGUI;
		if ( screen != null )
			screen.UpdateRecords();
		//
		/*
		if (GetToken(record.Assessment,"fluid",out assessment)){
			fluidText.text = assessment;	
		}
		if (GetToken(record.Assessment,"placement",out assessment)){
			placementText.text = assessment;	
		}
		*/
		fastTitle.text = record.Region;
		fastTakenText.text = record.TimeTaken;
	}

	// handle pause, unpause of video when game is paused
	bool wasPlaying=false;
	public void Pause( bool yesno )
	{
		if ( yesno == true )
		{
			if ( fastImage01 != null )
			{
				wasPlaying = fastImage01.IsPlaying();
				fastImage01.Stop ();
			}
		}
		else
		{
			if ( wasPlaying == true )
				fastImage01.Play ();
		}
	}
		
	// provide a button callback to setup the desired contents of the breakdown view
    public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("FastViewer",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);	

		if (records.ContainsKey(button)){
			DisplayRecord(records[button]);
		}
    }
}


public class CTIndexGUI : GUIDialog
{
	Dictionary<GUIButton,ScanRecord> records;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
		
		Patient patient;
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		records = new Dictionary<GUIButton,ScanRecord>();
		GUIHorizontalCommand templateCommand = Find ("ctImageHorz01") as GUIHorizontalCommand;
		GUIScrollView indexScrollView = Find ("ctListScrollView") as GUIScrollView;
		// we can duplicate the label and line, too...
		indexScrollView.Elements.Remove(templateCommand);
		
		int recordsFound = 0;
		
		foreach (ScanRecord record in patient.GetPatientRecord().CTRecords){
			if (record.TimeTaken != null && record.TimeTaken != ""){
				recordsFound++;
				
				Texture2D thumbnailTexture = Resources.Load(record.Thumbnail) as Texture2D;
				GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
				newEntry.name = "CTScan"+record.Region+"Horz";
				// we need to make a copy of the style for each button so they can be different.
				GUIButton thumbButton = newEntry.Find("ctImage01") as GUIButton;
				GUIButton wideButton = newEntry.Find("ctImageShortDetail") as GUIButton;

				// rename this so we can detect which was hit.
				thumbButton.name = "buttonCT"+record.Region;
				wideButton.name = "buttonCT"+record.Region;
				GUIStyle newStyle = new GUIStyle(thumbButton.Style);
				thumbButton.SetStyle(newStyle);
				thumbButton.Style.normal.background = thumbnailTexture;
				thumbButton.Style.hover.background = thumbnailTexture;
				thumbButton.Style.active.background = thumbnailTexture;
				thumbButton.screenTarget = "CTImageBreakdown";
				wideButton.screenTarget = "CTImageBreakdown";

				wideButton.text = "CT Taken "+record.TimeTaken+" - "+record.Region;
				indexScrollView.Elements.Add(newEntry);

				//Add both buttons to the record so that both buttons call the same record
				records.Add(thumbButton,record);
				records.Add(wideButton, record);
			}
		}
		
		if (recordsFound == 0){
			GUIHorizontalCommand newEntry = templateCommand.Clone() as GUIHorizontalCommand;
			newEntry.name = "ctScansNone";
			// we need to make a copy of the style for each button so they can be different.
			GUIButton thumbButton = newEntry.Find("ctImage01") as GUIButton;
			GUIButton wideButton = newEntry.Find("ctImageShortDetail") as GUIButton;

			// rename this so we can detect which was hit.
			newEntry.Elements.Remove(thumbButton); // take away the button

			wideButton.text = "No CT Sessions Available";
			indexScrollView.Elements.Add(newEntry);
		}
    }
		
		// provide a button callback to setup the desired layout of the breakdown view
    public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("CTIndexGUI",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
		
		if (records.ContainsKey(button)){
			// set up the label of the Breakdown to read "abdomen" so the screen will know how to set itself up
			CTViewer viewer = Parent.FindScreen(button.screenTarget) as CTViewer;
			viewer.SetupBreakdown(records[button]);
		}
    }
}

public class CTViewer : GUIDialog
{
	
	// declare all the elements we have to manipulate
	
	GUILabel ctTakenText;
	GUILabel ctTitle;
//	GUIScreen ctScreen;
//	GUILabel fluidText;
//	GUILabel placementText;
	
	GUIArea ctImage01;
	Texture2D[] textures;
	Rect sliderRect;
	float sliderValue;
	
	Patient patient;

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
		patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		
// 		locate all the elements we have to manipulate
	
		ctTakenText = Find ("ctTakenText") as GUILabel;
		ctTitle = Find ("ctTitle") as GUILabel;
//		fluidText = Find ("ct01Fluid-Text") as GUILabel;
//		placementText = Find ("fast01Placement-Text") as GUILabel;
	
		ctImage01 = Find("ctImage01") as GUIArea;
		sliderRect = ctImage01.GetRect();
		Rect area = GetArea();
		sliderRect.x += area.x;
		sliderRect.y += area.y;
		sliderRect.y += 340;//sliderRect.height-50;
		sliderRect.height=50;
		sliderRect.width = 256; // we should be able to get this from the image area.
     }

	public void SetupBreakdown(ScanRecord record){ // called by the index when the button is hit
		
		ctTitle.text = record.Region;
		ctTakenText.text = record.TimeTaken;

		Serializer<List<string>> serializer = new Serializer<List<string>>();
		List<string> imagePaths = serializer.Load("XML/Patient/Records/"+record.Filename.Replace(".xml",""));

		sliderValue = 0;
		int imageCount = imagePaths.Count;
		textures = new Texture2D[imageCount];
		// lets assume resources for now...
		for (int i=0;i<imageCount;i++){
			textures[i] = Resources.Load(imagePaths[i].Replace(".jpg","")) as Texture2D;
		}
		ctImage01.Style.normal.background = textures[0];
		ctImage01.Style.hover.background = textures[0];
		ctImage01.Style.active.background = textures[0];
//		ctImage01.SetMovieTexture(record.movieTexture);
//		ctImage01.Stop(); // rewinds it
//		ctImage01.Play();

/*
		// parse out the arguments for fluid and placement
		string assessment = "";
		if (GetToken(record.Assessment,"fluid",out assessment)){
			fluidText.text = assessment;	
		}
		if (GetToken(record.Assessment,"placement",out assessment)){
			placementText.text = assessment;	
		}
*/
		
	}

	public override void Execute(){
		base.Execute ();

		// now draw the slider and set the appropriate image from the texture array to fiew
		sliderValue = GUI.HorizontalSlider(sliderRect,sliderValue,0,textures.Length-.1f);
		ctImage01.Style.normal.background = textures[(int)sliderValue];
		ctImage01.Style.hover.background = textures[(int)sliderValue];
		ctImage01.Style.active.background = textures[(int)sliderValue];
	}
		
	// provide a button callback to scroll through the image stack ?
    public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("CTViewer",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
    }
}





public class XrayPopupGUI : GUIDialog
{
	string region;
	string timestamp = "hh:mm";
	ScanRecord record;
	
    public override void Load(DialogMsg msg)
    {
		base.Load(msg); // this will take care of overloading button labels, etc.
		
		Patient patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		
		TraumaBrain tb = Component.FindObjectOfType(typeof(TraumaBrain)) as TraumaBrain;
		if (tb != null){
			timestamp = tb.GetSimulatedTimeText();	
		}
		
		// look for the "xrayarea" value in the arguments.  If not found, default to chest, or pevlis if no chest
		string xrayArea = "";
		GetToken(msg.arguments,"xrayarea",out xrayArea);
		xrayArea = xrayArea.ToLower().Replace ("\"","");
		if (xrayArea == "null") xrayArea = "chest";
		
		record = patient.GetPatientRecord().GetXRayRecord(xrayArea);
		// test for no record incase there was no matching record
		if (record == null) return;

		Texture2D xrayTexture = Resources.Load("Scans/"+record.Filename) as Texture2D;
		region = xrayArea.ToUpper();
		
		record.TimeTaken = timestamp;
		record.Region = region.ToUpper ();
		
		GUILabel xrayLabel = Find("xrayImage01") as GUILabel;
		//xrayLabel.Style.normal.background = 
		xrayLabel.Style.normal.background = xrayTexture;
		xrayLabel.Style.hover.background = xrayTexture;
		xrayLabel.Style.active.background = xrayTexture;
/*
		msg = new DialogMsg();
		msg.xmlName = "traumaDecisionPanel";
		msg.className = "DecisionPanel";
		msg.arguments.Add ("screen=" + this.name);
		msg.arguments.Add ("question=" + "\"Check anything wrong in the XRAY, leave blank for ok.\"" + " " + "checklist=true");
		msg.arguments.Add ("checkbox=placement text=\"placement\"");
		msg.arguments.Add ("checkbox=blood text=\"blood\"");
		msg.arguments.Add ("checkbox=fracture text=\"fracture\"");
		GUIManager.GetInstance().LoadDialog(msg);
*/
	}

	public override void PutMessage(GameMsg msg )
	{
		GUIScreenMsg gsmsg = msg as GUIScreenMsg;
		if ( gsmsg != null )
		{

			// we're no longer using the decision panel to get user input, and we no longer get a message, so 
			// this method is currently unused.  the Button Callback is used instead.

			// parse return from Decision Panel
/*
			string results;
			if ( TokenMgr.GetToken(gsmsg.arguments[0],"options", out results) )
			{
				InteractStatusMsg ismsg=null;
				if ( results == "" )
				{
					ismsg = new InteractStatusMsg("PLAYER:XRAY:"+region.ToUpper()+":CLEAR");
					LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
				} 
				else 
				{
					UnityEngine.Debug.LogError ("Xray.PutMessage() : results=" + results);
					string[] options = results.Split(',');

					foreach( string item in options )
					{
						switch( item )
						{
						case "placement":
							ismsg = new InteractStatusMsg("PLAYER:XRAY:PLACEMENT:"+region.ToUpper()+":INCORRECT");
							LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
							record.Assessment += "placement=IncorrectPlacement ";
							break;	
						case "blood":
							ismsg = new InteractStatusMsg("PLAYER:XRAY:BLOOD:"+region.ToUpper()+":POSITIVE");
							LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
							record.Assessment += "fluid=Found ";
							break;
						case "fracture":
							ismsg = new InteractStatusMsg("PLAYER:XRAY:FRACTURE:"+region.ToUpper()+":POSITIVE");
							LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
							record.Assessment += "fracture=FractureFound ";
							break;
						}
					}
				}
			}
*/

			// do fake button here, naming close will allow the script to finish
			GUIButton b = new GUIButton();
			b.name = "close";
			b.closeWindow = true;
			ButtonCallback (b);
		}
	}

	public override void ButtonCallback(GUIButton button)
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("traumaQuickXray",button.name)); // TODO what should this name be ?
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();
		
		// could check if the button was already pressed and just bail...
		base.ButtonCallback (button);
		
		if (button.name == "ButtonNext") { // note that this button name seems to be different every time someone makes a dialog xml... :/
			// fake a button even
			GUIButton b = new GUIButton();
			b.name = "button.done";
			ButtonCallback(b);
			Close ();
		}
	}
}

public class XRayDetailGUI : GUIDialog
{
    public override void Load(DialogMsg msg)
    {
		base.Load(msg);
    }
}

public class PatientChartGUI : GUIDialog
{
    public override void Load(DialogMsg msg)
    {
    }
}

public class PatientLabsGUI : GUIDialog
{
    public override void Load(DialogMsg msg)
    {
    }
}

public class VitalsGUI : GUIDialog
{
    VitalsGraph hbGraph;
    VitalsGraph dGraph;
    VitalsGraph sGraph;
    VitalsGraph oGraph;

    GUILabel hbLabel;
    GUILabel pulseOxLabel;
    GUILabel bpSysLabel;
    GUILabel bpDiaLabel;
    GUILabel temperature;

    Patient patient;

    //~VitalsGUI() 
    //{
    //    if (hbGraph != null)
    //        hbGraph.run = false;

    //    if (dGraph != null)
    //        dGraph.run = false;

    //    if (sGraph != null)
    //        sGraph.run = false;

    //    if (oGraph != null)
    //        oGraph.run = false;
    //}

    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

        hbGraph = Component.FindObjectOfType(typeof(HeartbeatGraph)) as HeartbeatGraph;
        dGraph = Component.FindObjectOfType(typeof(DiastolicGraph)) as DiastolicGraph;
        sGraph = Component.FindObjectOfType(typeof(SystolicGraph)) as SystolicGraph;
        oGraph = Component.FindObjectOfType(typeof(O2Graph)) as O2Graph;

        hbLabel = Find("heartRate") as GUILabel;
        pulseOxLabel = Find("pulseOx") as GUILabel;
        bpSysLabel = Find("bpSystolic") as GUILabel;
        bpDiaLabel = Find("bpDiastolic") as GUILabel;
        temperature = Find("patientTemp") as GUILabel;
        patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
    }

    public override void Execute()
    {
        base.Execute();
		
		if (patient == null)
			patient = Component.FindObjectOfType(typeof(Patient)) as Patient;
		if (patient == null) return;
		
		if (patient.GetAttribute("onvitalsmonitor")=="True"){
        	hbLabel.text = ((int)Math.Round(patient.HR,0)).ToString();
//			temperature.text = (System.Convert.ToInt32(Math.Round(patient.TEMP,1) * 10f) / 10f).ToString();
		}
		else
			hbLabel.text = "---";
		
		if (patient.GetAttribute ("pulseoxplaced") == "True") {
			pulseOxLabel.text = ((int)Math.Round (patient.SP, 0)).ToString ();
			temperature.text =  ((int)Math.Round (patient.RR, 0)).ToString (); // showing respiration now in the temp area.
		} else {
			pulseOxLabel.text = "---";
			temperature.text = "--";
		}
		
		if (patient.GetAttribute("cufferror")=="True"){
			bpSysLabel.text = "CUFF";
			bpDiaLabel.text = "ERROR";			
		}
		else
		{
			if (patient.GetAttribute("autobpplaced")=="True"){
	        	bpSysLabel.text = ((int)Math.Round(patient.BP_SYS,0)).ToString();
	        	bpDiaLabel.text = ((int)Math.Round(patient.BP_DIA,0)).ToString();
			}
			else{
				bpSysLabel.text = "--";
				bpDiaLabel.text = "--";
			}
		}
        
    }

    public override void Load(DialogMsg msg)
    {
		//  whether the graphs run is not dependent on patient attributes which are checked by the various
		// VitalsGraph.cs classes when they are asked to update
		return;
/*	
        if (msg != null)
        {
            if (hbGraph != null)
                hbGraph.run = true;

            if (dGraph != null)
                dGraph.run = true;

            if (sGraph != null)
                sGraph.run = true;

            if (oGraph != null)
                oGraph.run = true;
        }
 */
    }

	public override void OnClose(){
		// send a message to clear the flag that says we are open (just to disable multiple clicks)
		InteractMsg msg = new InteractMsg(null,"VITALS:MONITOR:ONCLOSE",false);
		Brain.GetInstance().PutMessage(msg);

		base.OnClose ();
	}
}

public class InteractConfirmDialog : GUIDialog
{
    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);
    }

    public override void Load(DialogMsg msg)
    {
        base.Load(msg);

		// NOTE!! close this immediately because we are just
		// redirecting this dialog for now...
		Close ();

		// get question from args
		string question;
		TokenMgr.GetToken(msg.arguments[1],"text",out question);

		msg.xmlName = "traumaDecisionPanel";
		msg.className = "DecisionPanel";
		msg.arguments.Add ("question=" + question + " " + "checklist=false");
		msg.arguments.Add ("button=ok text=Yes");
		msg.arguments.Add ("button=cancel text=No");
		GUIManager.GetInstance().LoadDialog(msg);
	}

    public override void ButtonCallback(GUIButton button)
    {
        base.ButtonCallback(button);
    }
}

public class FastDialog : GUIDialog
{
	string region="pelvis";
	string timestamp = "hh:mm";
	ScanRecord record; // update this with timestamp, region, assessment
	GUIMovie movie;
    public FastDialog()
        : base()
    {
    }	

	
    public override void Load(DialogMsg msg)
    {
        base.Load(msg);

		TraumaBrain tb = Component.FindObjectOfType(typeof(TraumaBrain)) as TraumaBrain;
		if (tb != null){
			timestamp = tb.GetSimulatedTimeText();	
		}

		// get the FAST region from the msg

		GetToken(msg.arguments,"region",out region);

		string buttonText = "";
		GetToken(msg.arguments,"buttontext",out buttonText);
		buttonText = buttonText.Replace("\"","");
		if (buttonText == "") buttonText = "Next Region";
		GUIButton nextButton = Find ("buttonNext") as GUIButton;
		if (nextButton != null)
			nextButton.text = buttonText;
		
		// get patient
		Patient patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;
		if ( patient != null )
		{
			record = patient.GetPatientRecord().GetFastRecord(region.ToLower());
			if (record != null){
				record.Region = region.ToUpper();
				record.TimeTaken = timestamp;
				// URL
				movie = Find("Movie") as GUIMovie;			
				// get movie 
				movie.SetFilename (record.Filename) ;  	// get from Patient
				movie.Loop(true);
				movie.Play();
				record.Assessment = "";
			}
		}
		GUILabel label = Find("fast.name.label") as GUILabel;
		label.text = region.ToUpper()+" Taken at "+timestamp;


		// as of 1.0, we are not going to ask question with this dialog.  instead, add a button to the FAST image dialog 'CLOSE'
		// TODO make sure this new button gets called back
		/*
		msg.xmlName = "traumaDecisionPanel";
		msg.className = "DecisionPanel";
		msg.arguments.Add ("screen=" + this.name);  // is this how the callback gets 
		msg.arguments.Add ("question=" + "\"Is there blood in the " + region + "?\"");
		msg.arguments.Add ("button=ok text=Yes");
		msg.arguments.Add ("button=cancel text=No");
		GUIManager.GetInstance().LoadDialog(msg);
		*/
	}

	// handle pause, unpause of video when game is paused
	bool wasPlaying=false;
	public void Pause( bool yesno )
	{
		if ( yesno == true )
		{
			if ( movie != null )
			{
				wasPlaying = movie.IsPlaying();
				movie.Stop ();
			}
		}
		else
		{
			if ( wasPlaying == true )
				movie.Play ();
		}
	}

	public override void PutMessage( GameMsg msg )
	{

		GUIScreenMsg smsg = msg as GUIScreenMsg;
		if ( smsg != null )
		{
			InteractStatusMsg ismsg;
			string arg = smsg.arguments[0];
			if ( arg == "ok" )
			{
				ismsg = new InteractStatusMsg("PLAYER:FAST:"+region.ToUpper()+":BLOOD");
				LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
				// fake a button even
				GUIButton b = new GUIButton();
				b.name = "button.done";
				ButtonCallback(b);
				Close ();
			}
			else if ( arg == "cancel" )
			{
				ismsg = new InteractStatusMsg("PLAYER:FAST:"+region.ToUpper ()+":NOBLOOD");
				LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
				// fake a button even
				GUIButton b = new GUIButton();
				b.name = "button.done";
				ButtonCallback(b);
				Close ();
			}
		}
	}

	public override void ButtonCallback(GUIButton button)
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("FASTDialog",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		// could check if the button was already pressed and just bail...
		base.ButtonCallback (button);
	
		if (button.name == "buttonNext") {
			// fake a button even
			GUIButton b = new GUIButton();
			b.name = "button.done";
			ButtonCallback(b);
			Close ();
		}
	}
}
	
public class CTDialog : GUIDialog
{
	string region="brain";
	GUIToggle normalToggle;
	GUIToggle abnormalToggle;
	GUIToggle placementToggle;
	string timestamp = "hh:mm";
	ScanRecord record; // update this with timestamp, region, assessment
	GUIMovie movie;
    public CTDialog()
        : base()
    {
    }	

	
    public override void Load(DialogMsg msg)
    {
		// we use load to set up the timestamp and movie, then we close this dialog for CT scans have no popup
		
		
        base.Load(msg);

		normalToggle = Find("fastexam.button.normal") as GUIToggle;
		abnormalToggle = Find("fastexam.button.abnormal") as GUIToggle;
		placementToggle = Find("fastexam.button.placement") as GUIToggle;
		
		normalToggle.toggle = true; // need to start off in some valid state.
		
		TraumaBrain tb = Component.FindObjectOfType(typeof(TraumaBrain)) as TraumaBrain;
		if (tb != null){
			timestamp = tb.GetSimulatedTimeText();	
		}
		
		
		// get the FAST region from the msg

		GetToken(msg.arguments,"region",out region);
		
		// get patient
		Patient patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;
		if ( patient != null )
		{
			record = patient.GetPatientRecord().GetCTRecord(region.ToLower());
			if (record != null){
//				record.Region = region.ToUpper();
				record.TimeTaken = timestamp;
				// URL
				// we don't really need this movie in the dialog, we're just using this to timestamp the record.
//				movie = Find("Movie") as GUIMovie;
//				movie.SetFilename (record.Filename) ;  	// get from Patient
//				movie.Loop(true);
//				movie.Play();
				record.Assessment = "";
			}
		}
		GUILabel label = Find("fast.name.label") as GUILabel;
		label.text = region.ToUpper()+" Taken at "+timestamp;
		
		Close (); // hide this dialog immediately, now that the record is stamped as taken
		
    }
	
    public override void ButtonCallback(GUIButton button)
    {
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("CTDialog",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		// could check if the button was already pressed and just bail...
        base.ButtonCallback(button);
		 
		// we might have to change the 'TRIGGER' settings if we want people to be able to 'unsay' what they said
		
		GUIToggle asToggle = button as GUIToggle;
		if (asToggle != null){
			// implement a kind of radio button feature, excluding 'normal if any other states are checked.
			if (asToggle.name == "fastexam.button.normal" && asToggle.toggle){
				abnormalToggle.toggle = false;
			}
			if  ((asToggle.name == "fastexam.button.abnormal" && asToggle.toggle))
			{
				normalToggle.toggle = false;	
			}
		}		
		// only send the InteractMessage at the end, when the player hits DONE		
		if (button.name == "button.done" ){
			record.movieTexture = movie.GetMovieTexture(); // cache this for display in the Viewer later
			record.Assessment = "";
			// look at all the toggles, and send the appropriate messages for their final states.	
			if (placementToggle.toggle){
				InteractMsg msg = new InteractMsg(null,"PLAYER:PLACEMENT:"+region+":INCORRECT",true);
				Brain.GetInstance().PutMessage(msg);
				record.Assessment += "placement=Incorrect ";
			}
			else
			{
				record.Assessment += "placement=Correct/NA ";
			}
			if (abnormalToggle.toggle){
				InteractMsg msg = new InteractMsg(null,"PLAYER:CT:"+region+":TRUE",true);
				Brain.GetInstance().PutMessage(msg);
				record.Assessment += "fluid=Found";
			}
			if (normalToggle.toggle){
				InteractMsg msg = new InteractMsg(null,"PLAYER:CT:"+region+":NORMAL",true);
				Brain.GetInstance().PutMessage(msg);	
				record.Assessment += "fluid=None";
			}
		}
	}
}


public class BloodCartDialog : GUIDialog
{
    public BloodCartDialog()
        : base()
    {
		
    }	
	
	int bloodDrip;
	int bloodPressure;
	int bloodRapid;
	
	int salineDrip;
	int salinePressure;
	int salineRapid;
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);
		
		bloodDrip = 0;
		bloodPressure = 0;
		bloodRapid = 0;

		salineDrip = 0;
		salinePressure = 0;
		salineRapid = 0;
	}
	
	bool canOrder()
	{
		if ( (bloodDrip + bloodPressure + bloodRapid + salineDrip + salinePressure + salineRapid) > 0 )
			return true;
		else
			return false;
	}
	
	public void OrderBags()
	{
		Patient patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;
		if ( patient != null )
			patient.OrderFluids("Dispatcher",bloodDrip,bloodPressure,bloodRapid,salineDrip,salinePressure,salineRapid);
	}
	
    public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		// first check for order or cancel
		if ( button.name == "button.cancel" )
		{
			return;
		}
		if ( button.name == "button.order" )
		{
			// figure out what has been ordered and order it!
			if ( canOrder() == true )
			{
				OrderBags();
				Close();
			}
			return;
		}
		// this must have been a +/- button
		bool dirty=false;

		// BLOOD DRIP
		if ( button.name == "bloodDripMinus" )
		{
			if ( bloodDrip > 0 )
			{
				dirty = true;
				bloodDrip--;
			}
		}
		if ( button.name == "bloodDripPlus" )
		{
			dirty = true;
			bloodDrip++;
		}		
		// handle blood drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("bloodDripCount") as GUILabel;
			if ( label != null )
				label.text = bloodDrip.ToString();
			// change graphic
			GUIArea area = Find("BloodDripArea") as GUIArea;
			if ( area != null )
			{
				if ( bloodDrip > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.blooddrip.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.blooddrip.normal"));
			}
		}
		// BLOOD PRESSURE
		if ( button.name == "bloodPressureMinus" )
		{
			if ( bloodPressure > 0 )
			{
				dirty = true;
				bloodPressure--;
			}
		}
		if ( button.name == "bloodPressurePlus" )
		{
			dirty = true;
			bloodPressure++;
		}		
		// handle blood drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("bloodPressureCount") as GUILabel;
			if ( label != null )
				label.text = bloodPressure.ToString();
			// change graphic
			GUIArea area = Find("BloodPressureArea") as GUIArea;
			if ( area != null )
			{
				if ( bloodPressure > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.bloodpressure.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.bloodpressure.normal"));
			}
		}
		// BLOOD INFUSION
		if ( button.name == "bloodRapidMinus" )
		{
			if ( bloodRapid > 0 )
			{
				dirty = true;
				bloodRapid--;
			}
		}
		if ( button.name == "bloodRapidPlus" )
		{
			dirty = true;
			bloodRapid++;
		}		
		// handle blood drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("bloodRapidCount") as GUILabel;
			if ( label != null )
				label.text = bloodRapid.ToString();
			// change graphic
			GUIArea area = Find("BloodRapidArea") as GUIArea;
			if ( area != null )
			{
				if ( bloodRapid > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.bloodrapid.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.bloodrapid.normal"));
			}
		}
		
		// SALINE DRIP
		if ( button.name == "salineDripMinus" )
		{
			if ( salineDrip > 0 )
			{
				dirty = true;
				salineDrip--;
			}
		}
		if ( button.name == "salineDripPlus" )
		{
			dirty = true;
			salineDrip++;
		}		
		// handle saline drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("salineDripCount") as GUILabel;
			if ( label != null )
				label.text = salineDrip.ToString();
			// change graphic
			GUIArea area = Find("SalineDripArea") as GUIArea;
			if ( area != null )
			{
				if ( salineDrip > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinedrip.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinedrip.normal"));
			}
		}
		// BLOOD PRESSURE
		if ( button.name == "salinePressureMinus" )
		{
			if ( salinePressure > 0 )
			{
				dirty = true;
				salinePressure--;
			}
		}
		if ( button.name == "salinePressurePlus" )
		{
			dirty = true;
			salinePressure++;
		}		
		// handle saline drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("salinePressureCount") as GUILabel;
			if ( label != null )
				label.text = salinePressure.ToString();
			// change graphic
			GUIArea area = Find("SalinePressureArea") as GUIArea;
			if ( area != null )
			{
				if ( salinePressure > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinepressure.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinepressure.normal"));
			}
		}
		// saline INFUSION
		if ( button.name == "salineRapidMinus" )
		{
			if ( salineRapid > 0 )
			{
				dirty = true;
				salineRapid--;
			}
		}
		if ( button.name == "salineRapidPlus" )
		{
			dirty = true;
			salineRapid++;
		}		
		// handle saline drip
		if ( dirty == true )
		{
			// change count
			GUILabel label = Find("salineRapidCount") as GUILabel;
			if ( label != null )
				label.text = salineRapid.ToString();
			// change graphic
			GUIArea area = Find("SalineRapidArea") as GUIArea;
			if ( area != null )
			{
				if ( salineRapid > 0 )
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinerapid.active"));
				else
					area.SetStyle(area.Skin.GetStyle("bloodcart.salinerapid.normal"));
			}
		}
		// handle ORDER button
		if ( canOrder() == true )
		{
			// turn on button
			GUIButton tmp = Find("button.order") as GUIButton;
			if ( tmp != null )
				tmp.SetStyle(button.Skin.GetStyle("button.order.active"));
		}
		else
		{
			// turn off button
			GUIButton tmp = Find("button.order") as GUIButton;
			if ( tmp != null )
				tmp.SetStyle(button.Skin.GetStyle("button.order.inactive"));
		}
    }
}

[Serializable]
public class PlanOfCareInfo
{
	public PlanOfCareInfo() {}
	public string item;
	public string interaction;
}

[Serializable]
public class PlanOfCareForm
{
	public PlanOfCareForm() {}
	public List<PlanOfCareInfo> disposition;
	public List<PlanOfCareInfo> consultation;
}

public class PlanOfCare : GUIScreen
{
	PlanOfCareForm form;
	GUIScrollView disposition;
	GUIScrollView consultation;
	GUIScrollView orders;
	
	GUIHorizontalCommand UpperMenuItem;
	GUIHorizontalCommand LowerMenuItem;
	
    public override void Initialize(ScreenInfo parent)
    {
        base.Initialize(parent);

		LoadXML("XML/PlanOfCareForm");	

		disposition = Find("dispositionScroll") as GUIScrollView;
		consultation = Find("consultationScroll") as GUIScrollView;
		orders = Find("ordersScroll") as GUIScrollView;
		
		UpperMenuItem = Find("containerItem") as GUIHorizontalCommand;
		LowerMenuItem = Find("orderItem") as GUIHorizontalCommand;
		

		Init();
	}
	
	public void AddDispositionItem( PlanOfCareInfo info, int idx )
	{
		string newname = "D" + idx;
		
		GUIHorizontalCommand a = UpperMenuItem.Clone() as GUIHorizontalCommand;
		disposition.Elements.Add(a);
		a.name = newname;
		// change text
		GUILabel label = a.Find("label") as GUILabel;
		label.text = info.item;
		label.UpdateContent();		
		GUIButton button = a.Find("button") as GUIButton;
		button.name = newname;
	}
	
	public void AddConsultationItem( PlanOfCareInfo info, int idx )
	{
		string newname = "C" + idx;
		
		GUIHorizontalCommand a = UpperMenuItem.Clone() as GUIHorizontalCommand;
		consultation.Elements.Add(a);
		a.name = newname;
		// change text
		GUILabel label = a.Find("label") as GUILabel;
		label.text = info.item;
		label.UpdateContent();
		GUIButton button = a.Find("button") as GUIButton;
		button.name = newname;
	}
	
	public void Init()
	{
		int idx;
		disposition.Elements.Clear();
		idx = 0;
		foreach( PlanOfCareInfo info in form.disposition )
		{
			AddDispositionItem(info, idx++);
		}
		
		consultation.Elements.Clear();
		idx = 0;
		foreach( PlanOfCareInfo info in form.consultation )
		{
			AddConsultationItem(info,idx++);
		}
		
		orders.Elements.Clear();
	}
	
	public void LoadXML( string filename )
	{
		Serializer<PlanOfCareForm> serializer = new Serializer<PlanOfCareForm>();
		form = serializer.Load(filename);
	}
	
	public void SaveTemplate()
	{		
		PlanOfCareInfo info = new PlanOfCareInfo();
		info.item = "home";
		info.interaction = "interaction";
		PlanOfCareForm form = new PlanOfCareForm();
		form.disposition = new List<PlanOfCareInfo>();
		form.disposition.Add(info);
		form.consultation = new List<PlanOfCareInfo>();		
		form.consultation.Add(info);
		Serializer<PlanOfCareForm> serializer = new Serializer<PlanOfCareForm>();
		serializer.Save("PlanOfCareForm.xml",form);		
	}
	
	public string FindInteractionName( string element )
	{
		foreach( PlanOfCareInfo info in form.consultation )
		{
			if ( info.item == element )
				return info.interaction;
		}
		foreach( PlanOfCareInfo info in form.disposition )
		{
			if ( info.item == element )
				return info.interaction;
		}
		return null;
	}
	
	void AddToOrders( string name )
	{
		GUIHorizontalCommand item = null;
		string newname = name;
		
		if ( name[0] == 'D' )
		{
			// find item in disposition
			item = disposition.Find(name) as GUIHorizontalCommand;
			// remove
			disposition.Elements.Remove(item);
			// change name
			newname = newname.Remove(0,1);
			newname = newname.Insert(0,"d");
		}
		if ( name[0] == 'C' )
		{
			// find item in disposition
			item = consultation.Find(name) as GUIHorizontalCommand;
			// remove
			consultation.Elements.Remove(item);
			// change name
			newname = newname.Remove(0,1);
			newname = newname.Insert(0,"c");
		}
		
		// create new orders item
		GUIHorizontalCommand order = LowerMenuItem.Clone() as GUIHorizontalCommand;
		// change name
		order.name = newname;
		// get name of item
		GUILabel label = item.Find("label") as GUILabel;
		// get label of new order
		GUILabel orderLabel = order.Find("label") as GUILabel;
		// set name of label
		orderLabel.text = label.text;
		orderLabel.UpdateContent();
		// set name of button
		GUIButton button = order.Find("button") as GUIButton;
		button.name = newname;
		// add it
		orders.Elements.Add(order);
	}
	
	public int SortFunc( GUIObject a, GUIObject b )
	{
		return a.name.CompareTo(b.name);
	}
	
	public void AddToDisposition( string name )
	{
		GUIHorizontalCommand item=null;
		// find this GUIHorizonal in orders
		foreach( GUIHorizontalCommand h in orders.Elements )
		{
			GUIButton b = h.Find(name) as GUIButton;
			if ( b != null )
				item = h;
		}
		// remove from orders
		orders.Elements.Remove(item);
		// create new element
		GUIHorizontalCommand hc = UpperMenuItem.Clone() as GUIHorizontalCommand;
		if ( hc != null )
		{
			// change name
			GUIButton b = hc.Find("button") as GUIButton;
			b.name = name;
			b.name = b.name.Remove(0,1);
			b.name = b.name.Insert(0,"D");
			hc.name = b.name;
			// change label
			GUILabel label = hc.Find("label") as GUILabel;
			label.text = ((GUILabel)(item.Find("label"))).text;			
			// add this item at correct index
			disposition.Elements.Add(hc);
			disposition.Elements.Sort(SortFunc);
		}
	}
	
	public void AddToConsultation( string name )
	{
		GUIHorizontalCommand item=null;
		// find this GUIHorizonal in orders
		foreach( GUIHorizontalCommand h in orders.Elements )
		{
			GUIButton b = h.Find(name) as GUIButton;
			if ( b != null )
				item = h;
		}
		// remove from orders
		orders.Elements.Remove(item);
		// create new element
		GUIHorizontalCommand hc = UpperMenuItem.Clone() as GUIHorizontalCommand;
		if ( hc != null )
		{
			// change name
			GUIButton b = hc.Find("button") as GUIButton;
			b.name = name;
			b.name = b.name.Remove(0,1);
			b.name = b.name.Insert(0,"C");
			hc.name = b.name;
			// change label
			GUILabel label = hc.Find("label") as GUILabel;
			label.text = ((GUILabel)(item.Find("label"))).text;			
			// add this item at correct index
			string tmp = name.Remove(0,1);
			int index = Convert.ToInt32(tmp);
			consultation.Elements.Add(hc);
			consultation.Elements.Sort(SortFunc);
		}
	}
	
	public void SubmitOrders()
	{
		foreach( GUIObject go in orders.Elements )
		{
			GUIHorizontalCommand horiz = go as GUIHorizontalCommand;
			GUILabel orderLabel = horiz.Find("label") as GUILabel;
			
			string interaction;
			if ( (interaction=FindInteractionName(orderLabel.text)) != null )
			{
				InteractMsg msg = new InteractMsg(null,interaction,true);
				Brain.GetInstance().PutMessage(msg);
			}
		}
	}
		
	public override void ButtonCallback(GUIButton button)
    {	
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("PlanOfCare",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
		
		// first check for order or cancel
		if ( button.name == "buttonSubmit" )
		{
			Close();
			SubmitOrders();
            //Brain.GetInstance().PutMessage(new ChangeStateMsg("Assessment"));
			MenuLoader loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
			if ( loader != null )
				loader.GotoAssessment();
		}
		if ( button.name[0] == 'D' )
		{
			AddToOrders(button.name);
			return;
		}
		if ( button.name[0] == 'C' )
		{
			AddToOrders(button.name);
			return;
		}
		if ( button.name[0] == 'd' )
		{
			AddToDisposition(button.name);
			return;
		}
		if ( button.name[0] == 'c' )
		{
			AddToConsultation(button.name);
			return;
		}
	}
}

public class TraumaLogin : GUIDialog
{
	GUIEditbox username,password;
	string realPassword;

    public override void Initialize(ScreenInfo parent)
    {
		GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
		GUIManager.GetInstance().FitToScreen = true;
		GUIManager.GetInstance().Letterbox = true;
		
		base.Initialize(parent);
		
		// check for valid login... if we have one then go directly
		// to main menu screen.
		if ( LoginMgr.GetInstance().ValidLogin == true )
		{
			// go to main menu
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaMainMenu";
			msg.className = "TraumaMainMenu";
			msg.arguments.Add("create");
			GUIManager.GetInstance().LoadDialog(msg);						
			Close();
		}
		
		GUIManager.GetInstance().Fade = true;
		
		username = Find("usernameBox") as GUIEditbox;
		password = Find("passwordBox") as GUIEditbox;		

		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}

	void loginCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			// if valid then return
			if ( LoginMgr.GetInstance().ValidLogin == true )
			{
				// go to main menu
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaMainMenu";
				msg.className = "TraumaMainMenu";
				msg.arguments.Add("create");
				GUIManager.GetInstance().LoadDialog(msg);
				Close();
			} 
			else
			{
				// bad username/password put up the error dialog
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaLoginError";
				msg.className = "TraumaLoginError";
				msg.arguments.Add ("Invalid Username or Password!");
				msg.arguments.Add ("login");
				GUIManager.GetInstance().LoadDialog(msg);
				Close ();
			}			
		}
		else
		{
			// setup to use local data and case order
			CaseConfiguratorMgr.GetInstance().UsingLocalData = true;
			CaseConfiguratorMgr.GetInstance().UsingCaseOrder = true;
			// set user as guest
			LoginMgr.GetInstance().Username = "guest";
			// bad username/password put up the error dialog
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoginError";
			msg.className = "TraumaLoginError";
			msg.arguments.Add ("Connection Error, logging in as Guest.  Results will not be saved.");
			msg.arguments.Add ("mainmenu");
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
	}
	
	public void CheckLogin()
	{
		// if we're supposed to use the offline container then just call the callback with false
		//if ( TraumaOfflineAssetContainer.GetInstance() != null && TraumaOfflineAssetContainer.GetInstance().bUseOfflineAssets == true )
		//	loginCallback (false,null,null,null);
		//else
		{
			// allow blank here
			if ( username.text == "" && password.text == "" )
			{
				LoginMgr.GetInstance().ValidLogin = true;
				loginCallback (true,null,null,null);
			}
			else
				LoginMgr.GetInstance().CheckLoginWithPing(username.text,password.text,loginCallback);
		}
	}
	
	public override void Execute()
	{
		base.Execute();
		
		// handle enter key for chatbar
		Event e = Event.current;
		if ( e != null && e.keyCode == KeyCode.Return )
		{
			CheckLogin();
		}
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "loginButton" )
		{
			CheckLogin();
		}
	}		
}

public class TraumaLoginError : GUIDialog
{
	public override void Initialize (ScreenInfo parent)
	{
		base.Initialize (parent);
	}

	bool continueOnError=false;
	GUILabel loginErrorText;

	public override void Load( DialogMsg msg )
	{
		loginErrorText = this.Find ("loginErrorText") as GUILabel;
		if ( msg.arguments.Count >= 1 )
			loginErrorText.text = msg.arguments[0];
		if ( msg.arguments.Count >= 2 )
		{
			if ( msg.arguments[1] == "mainmenu" )
				continueOnError=true;
			else
				continueOnError=false;
		}
		base.Load (msg);
	}

	public override void ButtonCallback (GUIButton button)
	{
		base.ButtonCallback (button);

		if ( continueOnError == false )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoginScreen";
			msg.className = "TraumaLogin";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		else
		{
			// now go to main menu
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaMainMenu";
			msg.className = "TraumaMainMenu";
			msg.arguments.Add("create");
			GUIManager.GetInstance().LoadDialog(msg);
		}

		Close ();
	}
}

public class TraumaMainMenu : GUIDialog
{
	GUILabel usernameLabel, config_info, caseSelect_info, record_info, settings_info, logout_info;
	GUIArea area;
	GUIButton config,selection,record,logout, settings;
	
	public override void Initialize (ScreenInfo parent)
	{
		GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
		GUIManager.GetInstance().FitToScreen = true;
		GUIManager.GetInstance().Letterbox = true;
		
		base.Initialize (parent);
		
#if OLD
		if ( LoginMgr.GetInstance().ValidLogin == false )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoginScreen";
			msg.className = "TraumaLogin";
			msg.modal = true;
			GUIManager.GetInstance().LoadDialog(msg);
		}
#endif
		
		// get buttons
		area = Find("menuArea") as GUIArea;
		config = area.Find("caseConfiguration") as GUIButton;
		selection = area.Find("caseSelection") as GUIButton;
		record = area.Find("playerRecord") as GUIButton;
		logout = area.Find("logout") as GUIButton;
		settings = Find ("settings") as GUIButton;

		//get button info
		config_info = Find("caseConfig_info") as GUILabel;
		caseSelect_info = Find("caseSelect_info") as GUILabel;
		record_info = Find("playerRecord_info") as GUILabel;
		settings_info = Find("settings_info") as GUILabel;
		logout_info = Find("logout_info") as GUILabel;
	
		usernameLabel = Find("usernameText") as GUILabel;
	
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}
	
	public void CheckIsAdmin()
	{
		if ( LoginMgr.GetInstance().Admin == true && area.Elements.Count != 4 )
		{
			area.Elements.Clear();
			area.Elements.Add(selection);
			area.Elements.Add(caseSelect_info);
			area.Elements.Add(config);
			area.Elements.Add(config_info);
			area.Elements.Add(record);
			area.Elements.Add(record_info);
			area.Elements.Add(settings);
			area.Elements.Add(settings_info);
			area.Elements.Add(logout);
			area.Elements.Add(logout_info);
		}
		if ( LoginMgr.GetInstance().Admin != true && area.Elements.Count != 3 )
		{
			area.Elements.Clear();
			area.Elements.Add(selection);
			area.Elements.Add(caseSelect_info);
			area.Elements.Add(record);
			area.Elements.Add(record_info);
			area.Elements.Add(settings);
			area.Elements.Add(settings_info);
			area.Elements.Add(logout);
			area.Elements.Add(logout_info);
		}
	}
	
	public override void Update()
	{
		if ( usernameLabel != null )
			usernameLabel.text = LoginMgr.GetInstance().Username;
		
		// don't really like to do this but it will work 
		CheckIsAdmin();
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "caseSelection" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseSelection";
			msg.className = "TraumaCaseSelection";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "caseConfiguration" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Main";
			msg.className = "CaseConfigMain";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "playerRecord" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaPlayerRecords";
			msg.className = "TraumaRecords";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "logout" )
		{
			Close();
			// make login invalid
			LoginMgr.GetInstance().ValidLogin = false;
			// put up login dialog
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoginScreen";
			msg.className = "TraumaLogin";
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}			
}

public class CaseConfigMain : GUIDialog
{
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonLoadCase" )
		{
			Close();
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-LoadCase";
			msg.className = "ConfigLoadCase";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonCreateCase" )
		{
			Close();
			// load dialog
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-BasicInfo";
			msg.className = "CaseConfigBasic";
			msg.arguments.Add("create");
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonCaseManager" )
		{
			Close();
			// load dialog
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseManager";
			msg.className = "TraumaCaseManager";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonExit" )
		{
			Close();
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaMainMenu";
			msg.className = "TraumaMainMenu";
			msg.arguments.Add("create");
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}
}

public class CaseConfigBasic : GUIDialog
{
	GUIEditbox CaseDesc;
	GUIEditbox CaseSubTitle;
	GUIEditbox CaseTitle;
	
	GUILabel PreviewTitle;
	GUILabel PreviewSubTitle;
	GUILabel PreviewDetail;
	
	bool firstDesc=true;
	bool firstSubTitle=true;
	bool firstTitle=true;
	
	GUIScrollView imageScroll;
	float imageSize;
	float scrollPosition=0.0f;
	int numElements=0;
	int imageIdx=0;
	string thumbnail;
	
	public override void Initialize (ScreenInfo parent)
	{
		base.Initialize (parent);
		
		CaseDesc = Find("CaseDescEditbox") as GUIEditbox;
		CaseSubTitle = Find("CaseSubTitleEditbox") as GUIEditbox;
		CaseTitle = Find("CaseTitleEditbox") as GUIEditbox;
		
		PreviewTitle = Find("caseTitle") as GUILabel;
		PreviewSubTitle = Find("caseDateText") as GUILabel;
		PreviewDetail = Find("caseDetailText") as GUILabel;
		
		imageScroll = Find("imageScrollView") as GUIScrollView;
		GUIHorizontalCommand imageHorizontal = Find("thumbnailHorizontal") as GUIHorizontalCommand;
		numElements = imageHorizontal.Elements.Count;
		// get size of thumb image
		GUIButton thumb = Find("thumbnail01") as GUIButton;
		imageSize = thumb.Style.fixedWidth + thumb.Style.margin.left + thumb.Style.margin.right;
		
		GetCaseData();
	}
	
	public override void Load( DialogMsg msg )
	{
		base.Load(msg);
		if ( msg.arguments.Count > 0 && msg.arguments[0] == "create" )
		{
			CaseConfiguratorMgr.GetInstance().CreateCase();
			CaseConfiguratorMgr.GetInstance().Data.casename = CaseTitle.text;
			CaseConfiguratorMgr.GetInstance().Data.caseDescription = CaseDesc.text;
			CaseConfiguratorMgr.GetInstance().Data.description = CaseSubTitle.text;
		}
	}
	
	public void PutCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			if ( CaseTitle.text != data.casename )
				data.changed = true;						
			data.casename = CaseTitle.text;
			if ( CaseSubTitle.text != data.description )
				data.changed = true;
			data.description = CaseSubTitle.text;
			if ( CaseDesc.text != data.caseDescription )
				data.changed = true;
			data.caseDescription = CaseDesc.text;
			
			if ( thumbnail == null )
				thumbnail = data.caseThumbnail;			
			if ( data.caseThumbnail != thumbnail )
				data.changed = true;
			data.caseThumbnail = thumbnail;
		}
	}
	
	public void GetCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			if ( data.casename != "" )
			{
				CaseTitle.text = data.casename;
				firstTitle = false;
			}
			if ( data.description != "" )
			{
				CaseSubTitle.text = data.description;
				firstSubTitle = false;
			}
			if ( data.caseDescription != "" )
			{
				CaseDesc.text = data.caseDescription;
				firstDesc = false;
			}
			
			thumbnail = data.caseThumbnail;
						
			// check thumbnail
			if ( thumbnail == "" || thumbnail == null )
				thumbnail = "thumbnail01";
			
			// load image
			GUIButton button = Find(thumbnail) as GUIButton;
			if ( button != null )
			{
				// grab this style texture and set the preview
				Texture2D texture = button.Style.normal.background;
				// get the preview label and set that style image
				GUILabel label = Find("caseImage") as GUILabel;
				label.Style.normal.background = texture;
			}
		}
	}
	
	public override void Execute()
	{
		base.Execute();
		
		string focusName = GUI.GetNameOfFocusedControl();
		if ( focusName != "" )
		{
			if ( firstTitle == true && focusName == "CaseTitleEditbox" )
			{
				firstTitle = false;
				CaseTitle.text = "";
			}
			if ( firstSubTitle == true && focusName == "CaseSubTitleEditbox" )
			{
				firstSubTitle = false;
				CaseSubTitle.text = "";
			}
			if ( firstDesc == true && focusName == "CaseDescEditbox" )
			{
				firstDesc = false;
				CaseDesc.text = "";
			}
		}
	}
	
	public override void Update()
	{
		// update preview
		if ( firstTitle == false )
			PreviewTitle.text = CaseTitle.text;
		if ( firstSubTitle == false )
			PreviewSubTitle.text = CaseSubTitle.text;
		if ( firstDesc == false )
			PreviewDetail.text = CaseDesc.text;
		
		imageScroll.scroll = new Vector2(imageIdx*imageSize,0.0f);
	}

	public override void OnClose()
	{
		base.OnClose();
		PutCaseData();
	}
	
	// this method allows closing of the config menu from the save dialog
	public void CloseMe( string button )
	{
		Close();
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonSave" )
		{
			PutCaseData();
			
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.modal = true;
				GUIManager.GetInstance().LoadDialog(msg);		
			}
		}
		
		if ( button.name == "buttonEvents" )
		{
			Close();
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Events";
			msg.className = "CaseConfigEvents";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonBack" || button.name == "buttonTeam" )
		{
			Close();
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Team";
			msg.className = "CaseConfigTeam";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonNext" || button.name == "buttonInjuries" )
		{
			Close();
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Injuries";
			msg.className = "CaseConfigInjuries";
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonMenu" )
		{
			PutCaseData();
			DialogMsg msg = new DialogMsg();
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.callback = CloseMe;
			} 
			else 
			{
				msg.xmlName = "traumaCaseConfig-Main";
				msg.className = "CaseConfigMain";
				Close ();
			}
			msg.arguments.Add("gotoMain");
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name.Contains("thumbnail") )
		{
			// save name
			thumbnail = button.name;
			// grab this style texture and set the preview
			Texture2D texture = button.Style.normal.background;
			// get the preview label and set that style image
			GUILabel label = Find("caseImage") as GUILabel;
			label.Style.normal.background = texture;
		}
		if ( button.name == "buttonThumbPrev" )
		{
			if ( imageIdx > 0 )
				imageIdx--;
		}		
		if ( button.name == "buttonThumbNext" )
		{
			if ( imageIdx < numElements-1-4 )
				imageIdx++;
		}
		if ( button.name == "buttonCaseStart" )
		{
			// close dialog
			Close ();
			// only do this if we are running from the main menu
			string loadedName = Application.loadedLevelName.ToLower ();
			if ( loadedName.Contains ("traumamainmenu") )
			{
				UnityEngine.Debug.Log ("CaseConfigBase.ButtonCallback() : Going to mission start for case <" + CaseConfiguratorMgr.GetInstance().Data.casename + ">");
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "trauma05MissionStart";
				msg.className = "TraumaMissionStart";
				msg.arguments.Add("CaseConfigBasic");
				GUIManager.GetInstance().LoadDialog(msg);
				return;
			}
			else
			{
				// running from inside the game, just start
				CaseConfiguratorMgr.GetInstance().StartCase();
			}
		}
	}
}

public class TraumaLoadingScreen : GUIDialog
{
	bool startLoad=false;
	GUILabel status;

	public override void Initialize( ScreenInfo parent )
	{
		base.Initialize(parent);
		status = Find ("loadingText") as GUILabel;
	}

	public override void Update()
	{
		// wait until curtain fades up
		if ( GUIManager.GetInstance().Curtain == false )
		{
			TraumaLoader loader = TraumaLoader.GetInstance();
			if ( loader != null )
			{
				GUIManager.GetInstance().Fade = false;				
				// case data already set, just request loading
				// load scene
				if ( startLoad == false )
				{
					loader.Load();
					startLoad = true;
				}
			}
		}
		base.Update();
	}

	public override void PutMessage( GameMsg msg )
	{
		GUIScreenMsg smsg = msg as GUIScreenMsg;
		if ( smsg != null )
		{
			// change status msg
			if ( status != null )
				status.text = smsg.arguments[0];
		}
	}
}

public class TraumaMissionStart : GUIDialog
{
	GUIMovie movie;
	GUILabel videoTime;
	float elapsedTime;
	string returnString;

	public override void Initialize( ScreenInfo parent )
	{
		base.Initialize(parent);
		// get movie
		movie = Find ("MovieClip") as GUIMovie;
		videoTime = Find ("videoLength") as GUILabel;
		
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}

	public override void Load( DialogMsg msg )
	{
		base.Load (msg);

		DialogMsg dmsg = new DialogMsg();
		returnString = msg.arguments[0];
		elapsedTime = 0.0f;
	}

	public override void Update()
	{
		if ( videoTime == null )
			return;

		elapsedTime += Time.deltaTime;
		string str1 = AssessmentMgr.GetInstance().ToTimeString(elapsedTime);
		string str2 = AssessmentMgr.GetInstance().ToTimeString(movie.GetMovieTexture().duration);
		if ( movie.GetMovieTexture().duration != -1 )
			videoTime.text = str1 + " / " + str2;
		else
			videoTime.text = "";
	}

	public override void ButtonCallback(GUIButton button)
	{	
		base.ButtonCallback(button);
		
		if ( button.name == "continue" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaLoadingScreen";
			msg.className = "TraumaLoadingScreen";
			GUIManager.GetInstance().LoadDialog(msg);
			movie.Stop ();
			Close ();
		}
		if ( button.name == "buttonVidPlay")
		{
			movie.Play();
		}
		if ( button.name == "buttonVidStop")
		{
			movie.Stop();
		}
		if ( button.name == "backToMenu")
		{		
			DialogMsg msg = new DialogMsg();
			// check arg for where to return
			if ( returnString == "TraumaCaseSelection")
			{
				msg.xmlName = "traumaCaseSelection";
				msg.className = "TraumaCaseSelection";
			}
			else
			{
				msg.xmlName = "traumaCaseConfig-BasicInfo";
				msg.className = "CaseConfigBasic";
			}
			GUIManager.GetInstance().LoadDialog (msg);
			movie.Stop ();
			Close ();
		}
	}
}

public class CaseConfigSave : GUIDialog
{
	bool gotoMain = false;
	GUIEditbox nameEditbox;
	bool firstEdit;
	
	public override void Load(DialogMsg msg)
	{
		base.Load(msg);
		// check to see whether we're going back to main menu
		if ( msg.arguments.Count > 0 )
		{
			if ( msg.arguments[0].Contains("gotoMain")  )
				gotoMain = true;
		}
		
		nameEditbox = Find("caseNameEditbox") as GUIEditbox;
		// check to see if there is a save name already
		if ( CaseConfiguratorMgr.GetInstance().Data.casename != "" )
		{
			// save name
			nameEditbox.text = CaseConfiguratorMgr.GetInstance().Data.casename;
			firstEdit = false;
		}
		else		
			firstEdit = true;
	}
	
	public override void Execute()
	{
		base.Execute();
		
		string focusName = GUI.GetNameOfFocusedControl();
		if ( focusName != "" )
		{
			if ( firstEdit == true && focusName == "caseNameEditbox" )
			{
				firstEdit = false;
				nameEditbox.text = "";
			}
		}
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonSave" )
		{
			CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
			
			// if owner is NULL then make the owner the currently logged in user
			if ( data.owner == null || data.owner == "" )
				data.owner = LoginMgr.GetInstance().Username;
	
			// check to see if a non-owner is trying to save a config
			if ( data != null && data.loadedCase == nameEditbox.text)
			{
				if ( LoginMgr.GetInstance().Username.ToLower() != data.owner.ToLower() && LoginMgr.GetInstance().Username.ToLower () != "admin" )
				{
					DialogMsg msg = new DialogMsg();
					msg.xmlName = "traumaErrorPopup";
					msg.className = "TraumaError";
					msg.modal = true;
					msg.arguments.Add("NOT CASE OWNER");
					msg.arguments.Add("You are not the case creator/owner (owner=" + data.owner.ToLower() + ").  To save this modified case configuration you need to change the case name(title) to a unique name.");
					GUIManager.GetInstance().LoadDialog(msg);
					return;
				}
			}
			else
			{
				// we're ok, make the current user the owner
				data.owner = LoginMgr.GetInstance().Username;
			}
			
			if ( firstEdit == false )
			{
				// save case config
				CaseConfiguratorMgr.GetInstance().SaveCaseConfiguration(nameEditbox.text);
				// goto main?
				if ( gotoMain == true )
				{
					// go back to main
					DialogMsg msg = new DialogMsg();
					msg.xmlName = "traumaCaseConfig-Main";
					msg.className = "CaseConfigMain";
					GUIManager.GetInstance().LoadDialog(msg);
				}
				Close();
			}
		}
		if ( button.name == "buttonCancel" )
		{
			if ( gotoMain == true )
			{
				// go back to main
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaCaseConfig-Main";
				msg.className = "CaseConfigMain";
				GUIManager.GetInstance().LoadDialog(msg);
			}
			Close();
		}
	}
}
	
public class CaseConfigEvents : GUIDialog
{
	public void PutCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			// create new tmp list
			List<CaseConfigOption> options = new List<CaseConfigOption>();			
			// enable toggles for options
			foreach( GUIHorizontalCommand element in scrollView.Elements )
			{
				// if toggle is on then add this element to options
				GUIToggle toggle = element.Find("eventTickbox") as GUIToggle;
				
				// save each one, checking for toggled
				CaseConfigOption option = new CaseConfigOption();
				option = CaseConfiguratorMgr.GetInstance().GetOptionConfig(element.name);
				option.Enabled = toggle.toggle;
				options.Add(option);
			}
			// set options
			if ( options.Count == 0 )
			{
				if ( data.options.Length != 0 )
					data.changed = true;
				data.options = null;
			}
			else
			{
				CaseConfigOption[] newoptions = options.ToArray();
				// check to see if options have changed
				if ( data.options == null && newoptions.Length == 0 )
					data.changed = false;
				else if ( data.options == null && newoptions.Length != 0 )
					data.changed = true;
				else if ( data.options.Length != newoptions.Length )
					data.changed = true;
				else
				{
					if ( data.options != null )
					{
						// check all elements
						for( int i=0 ; i<data.options.Length ; i++)
						{	
							if ( data.options[i].Name == newoptions[i].Name )
							{
								if ( data.options[i].Enabled != newoptions[i].Enabled )
									data.changed = true;
							}
						}
					}
				}
				// copy list to data
				data.options = newoptions;
			}
		}
	}
	
	public void GetCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			// enable toggles for options
			foreach( GUIHorizontalCommand element in scrollView.Elements )
			{
				// set toggle off
				GUIToggle toggle = element.Find("eventTickbox") as GUIToggle;
				toggle.toggle = false;
				// now loop and see if it is turned on
				if ( data.options != null )
				{
					foreach( CaseConfigOption option in data.options )
					{
						if ( element.name == option.Name )
						{
							if ( option.Enabled == true )
								toggle.toggle = true;
						}
					}
				}
			}
		}
	}
	
	GUIScrollView scrollView;
	GUIHorizontalCommand itemTemplate;
	
	public override void Initialize (ScreenInfo parent)
	{
		base.Initialize (parent);
		
		scrollView = Find("EventListScroll") as GUIScrollView;
		itemTemplate = scrollView.Find("eventItem01") as GUIHorizontalCommand;
		scrollView.Elements.Clear();
		
		LoadOptions();
		GetCaseData();
	}
	
	public void LoadOptions()
	{
		if ( CaseConfiguratorMgr.GetInstance().OptionConfig == null )
			return;
		
		foreach (CaseConfigOption opt in CaseConfiguratorMgr.GetInstance().OptionConfig)
		{
			GUIHorizontalCommand item = itemTemplate.Clone() as GUIHorizontalCommand;
			item.name = opt.Name;
			
			GUIToggle toggle = item.Find("eventTickbox") as GUIToggle;
			toggle.toggle = opt.Enabled;
			GUILabel nameLabel = item.Find("eventItemText") as GUILabel;
			nameLabel.text = opt.shortDescription;
			GUIEditbox param = item.Find("parameterBox") as GUIEditbox;
			param.text = "n/a";
			GUILabel paramTxt = item.Find("parameterSubText") as GUILabel;
			if ( opt.param != null && opt.param.Length > 0 )
			{
				paramTxt.text = "";
				foreach( string tmp in opt.param )
				{
					paramTxt.text += tmp;
					paramTxt.text += " ";
				}
			}
			else
				paramTxt.text = "";
			scrollView.Elements.Add(item);
		}
	}
		
	// this method allows closing of the config menu from the save dialog
	public void CloseMe( string button )
	{
		Close();
	}
	
	public override void OnClose()
	{
		PutCaseData();
		base.OnClose();
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonSave" )
		{
			PutCaseData();
			
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.modal = true;
				GUIManager.GetInstance().LoadDialog(msg);		
			}
		}
		
		if ( button.name == "buttonBack" || button.name == "buttonInjuries" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Injuries";
			msg.className = "CaseConfigInjuries";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonBasicInfo" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-BasicInfo";
			msg.className = "CaseConfigBasic";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonNext" || button.name == "buttonTeam" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Team";
			msg.className = "CaseConfigTeam";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonMenu" )
		{
			PutCaseData();
			DialogMsg msg = new DialogMsg();
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.callback = CloseMe;
			} 
			else 
			{
				msg.xmlName = "traumaCaseConfig-Main";
				msg.className = "CaseConfigMain";
				Close ();
			}
			msg.arguments.Add("gotoMain");
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}
}

public class CaseConfigInjuries : GUIDialog
{
	public List<InjuryInfo> InjuryConfig;
	
	GUILabel primary;
	GUIVerticalCommand v1,v2;
	GUIHorizontalCommand secondaryTemplate;
	int currInjuryIndex=0;
	
	GUIEditbox HRi,SYSi,DIAi,SPi;
	GUIEditbox HRe,SYSe,DIAe,SPe;
	GUILabel ToD;
	
	public override void Initialize (ScreenInfo parent)
	{
		base.Initialize (parent);

#if BUILD_XML
		List<InjuryInfo> config = new List<InjuryInfo>();
		InjuryInfo info = new InjuryInfo();
		info.Name = "Broken Nose";
		info.SecondaryOptions = new List<string>();
		info.SecondaryOptions.Add("Bloody Nose");
		info.SecondaryOptions.Add("Black Eye");
		config.Add(info);
		serializer.Save("CaseConfigInjuries.xml",config);
#endif
		
		// grab injuries config
		InjuryConfig = CaseConfiguratorMgr.GetInstance().InjuryConfig;

		// get GUI fields
		primary = Find("primaryInjury") as GUILabel;

		v1 = Find("column1") as GUIVerticalCommand;
		v2 = Find("column2") as GUIVerticalCommand;
		secondaryTemplate = Find("injuryItem01") as GUIHorizontalCommand;
		v1.Elements.Clear();
		v2.Elements.Clear();
		
		// get vitals info
		HRi = Find("HRInitial-Number") as GUIEditbox;
		SYSi = Find("SYSInitial-Number") as GUIEditbox;
		DIAi = Find("DIAInitial-Number") as GUIEditbox;
		SPi = Find("SPO2Initial-Number") as GUIEditbox;

		HRe = Find("HRTarget-Number") as GUIEditbox;
		SYSe = Find("SYSTarget-Number") as GUIEditbox;
		DIAe = Find("DIATarget-Number") as GUIEditbox;
		SPe = Find("SPO2Target-Number") as GUIEditbox;
		
		ToD = Find("timeToDeath-Number") as GUILabel;
		
		GetCaseData();
	}
	
	public void LoadInjury( int index )
	{		
		if ( index >= InjuryConfig.Count || index == -1 )
			return;
		
		InjuryInfo info = InjuryConfig[index];
		primary.text = info.Name;
		
		v1.Elements.Clear();
		v2.Elements.Clear();
		for (int i=0 ; i<info.SecondaryOptions.Count ; i++)
		{
			CaseConfigOption cco = info.SecondaryOptions[i];
			// make template		
			GUIHorizontalCommand template = secondaryTemplate.Clone() as GUIHorizontalCommand;
			template.Find("injuryItem01Tag").text = cco.Name;
			
			if ( i%2 == 0 )
			{
				UnityEngine.Debug.Log("LoadInjury() : add <" + template.Find("injuryItem01Tag").text + "> to v1");
				v1.Elements.Add(template);
			}
			else
			{
				UnityEngine.Debug.Log("LoadInjury() : add <" + template.Find("injuryItem01Tag").text + "> to v2");
				v2.Elements.Add(template);
			}
		}		
	}
	
	public int FindInjury( string name )
	{
		int idx = 0;
		foreach( InjuryInfo info in InjuryConfig )
		{
			if ( info.Name == name )
				return idx;
			idx++;
		}
		return -1;
	}
	
	public void GetCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			// copy vitals parameters for start/end
			HRi.text = data.start.HR;
			SYSi.text = data.start.BPSYS;
			DIAi.text = data.start.BPDIA;
			SPi.text = data.start.SP;
			
			HRe.text = data.end.HR;
			SYSe.text = data.end.BPSYS;
			DIAe.text = data.end.BPDIA;
			SPe.text = data.end.SP;
			
			// copy ToD
			ToD.text = data.timeOfDeath;

			// set injury
			LoadInjury(FindInjury(data.injury.Name));
			
			// now toggle options currently enabled
			foreach( GUIObject go in v1.Elements )
			{
				// get each element and add it if checked
				GUIHorizontalCommand hc = go as GUIHorizontalCommand;
				if ( hc != null )
				{
					// get name
					GUILabel label = hc.Find("injuryItem01Tag") as GUILabel;
					GUIToggle toggle = hc.Find("injuryToggleBox") as GUIToggle;
					
					// now check to see if this option is in the secondary list
					foreach( CaseConfigOption tmp in data.injury.SecondaryOptions)
					{
						// if found set the toggle on
						if ( tmp.Name == label.text )
						{
							label.text = tmp.Name;
							toggle.toggle = tmp.Enabled;
						}
					}
				}
			}
			// now toggle options currently enabled
			foreach( GUIObject go in v2.Elements )
			{
				// get each element and add it if checked
				GUIHorizontalCommand hc = go as GUIHorizontalCommand;
				if ( hc != null )
				{
					// get name
					GUILabel label = hc.Find("injuryItem01Tag") as GUILabel;
					GUIToggle toggle = hc.Find("injuryToggleBox") as GUIToggle;
					
					// now check to see if this option is in the secondary list
					foreach( CaseConfigOption tmp in data.injury.SecondaryOptions)
					{
						// if found set the toggle on
						if ( tmp.Name == label.text )
						{
							label.text = tmp.Name;
							toggle.toggle = tmp.Enabled;
						}
					}
				}
			}
		}
	}

	public void PutCaseData()
	{
		CaseOptionData data = CaseConfiguratorMgr.GetInstance().Data;
		if ( data != null )
		{
			// copy vitals parameters for start/end
			if ( data.start.HR != HRi.text )
				data.changed = true;
			data.start.HR = HRi.text;
			if ( data.start.BPSYS != SYSi.text )
				data.changed = true;
			data.start.BPSYS = SYSi.text;
			if ( data.start.BPDIA != DIAi.text )
				data.changed = true;
			data.start.BPDIA = DIAi.text;
			if ( data.start.SP != SPi.text )
				data.changed = true;
			data.start.SP = SPi.text;
			
			if ( data.end.HR != HRe.text )
				data.changed = true;
			data.end.HR = HRe.text;
			if ( data.end.BPSYS != SYSe.text )
				data.changed = true;
			data.end.BPSYS = SYSe.text;
			if ( data.end.BPDIA != DIAe.text )
				data.changed = true;
			data.end.BPDIA = DIAe.text;
			if ( data.end.SP != SPe.text )
				data.changed = true;
			data.end.SP = SPe.text;
			
			// copy ToD
			if ( ToD.text != data.timeOfDeath )
				data.changed = true;
			data.timeOfDeath = ToD.text;

			// find this case data
			InjuryInfo injury = CaseConfiguratorMgr.GetInstance().FindInjury(primary.text);
			if ( injury != data.injury )
				data.changed = true;
			data.injury = injury;
			
			// do first column
			foreach( GUIObject go in v1.Elements )
			{
				// get each element and add it if checked
				GUIHorizontalCommand hc = go as GUIHorizontalCommand;
				if ( hc != null )
				{
					GUILabel label = hc.Find("injuryItem01Tag") as GUILabel;
					GUIToggle toggle = hc.Find("injuryToggleBox") as GUIToggle;
					
					// check against every injury and set if equal
					foreach( CaseConfigOption option in data.injury.SecondaryOptions )
					{
						if (option.Name == label.text )
						{
							if ( option.Enabled != toggle.toggle )
								data.changed = true;
							option.Enabled = toggle.toggle;
						}
					}
				}
			}
			// do second column
			foreach( GUIObject go in v2.Elements )
			{
				// get each element and add it if checked
				GUIHorizontalCommand hc = go as GUIHorizontalCommand;
				if ( hc != null )
				{
					GUILabel label = hc.Find("injuryItem01Tag") as GUILabel;
					GUIToggle toggle = hc.Find("injuryToggleBox") as GUIToggle;
					
					// check against every injury and set if equal
					foreach( CaseConfigOption option in data.injury.SecondaryOptions )
					{
						if (option.Name == label.text )
						{
							option.Enabled = toggle.toggle;
						}
					}
				}
			}
		}
	}
	
	public override void OnClose()
	{
		PutCaseData();
		base.OnClose();
	}

	// this method allows closing of the config menu from the save dialog
	public void CloseMe( string button )
	{
		Close();
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonSave" )
		{
			PutCaseData();
			
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.modal = true;
				GUIManager.GetInstance().LoadDialog(msg);		
			}
		}
		
		if ( button.name == "buttonPreviousInjury" )
		{
			if ( currInjuryIndex > 0 )
				currInjuryIndex--;
			LoadInjury(currInjuryIndex);
		}
		if ( button.name == "buttonNextInjury" )
		{
			if ( currInjuryIndex < InjuryConfig.Count-1 )
				currInjuryIndex++;
			LoadInjury(currInjuryIndex);
		}
		
		// NAV
		if ( button.name == "buttonTeam" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Team";
			msg.className = "CaseConfigTeam";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonNext" || button.name == "buttonEvents" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Events";
			msg.className = "CaseConfigEvents";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonBack" || button.name == "buttonBasicInfo" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-BasicInfo";
			msg.className = "CaseConfigBasic";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonMenu" )
		{
			PutCaseData();
			DialogMsg msg = new DialogMsg();
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.callback = CloseMe;
			} 
			else 
			{
				msg.xmlName = "traumaCaseConfig-Main";
				msg.className = "CaseConfigMain";
				Close ();
			}
			msg.arguments.Add("gotoMain");
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}
}

public class CaseConfigTeam : GUIDialog
{
	public void PutCaseData()
	{
	}
	
	public override void OnClose()
	{
		PutCaseData();
		base.OnClose();
	}

	// this method allows closing of the config menu from the save dialog
	public void CloseMe( string button )
	{
		Close();
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonSave" )
		{
			PutCaseData();
			
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.modal = true;
				GUIManager.GetInstance().LoadDialog(msg);		
			}
		}
		if ( button.name == "buttonNext" || button.name == "buttonBasicInfo" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-BasicInfo";
			msg.className = "CaseConfigBasic";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonInjuries" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Injuries";
			msg.className = "CaseConfigInjuries";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonBack" || button.name == "buttonEvents" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Events";
			msg.className = "CaseConfigEvents";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name == "buttonMenu" )
		{
			PutCaseData();
			DialogMsg msg = new DialogMsg();
			if ( CaseConfiguratorMgr.GetInstance().Data.changed ==  true )
			{
				msg.xmlName = "traumaCaseConfig-Save";
				msg.className = "CaseConfigSave";
				msg.callback = CloseMe;
			} 
			else 
			{
				msg.xmlName = "traumaCaseConfig-Main";
				msg.className = "CaseConfigMain";
				Close ();
			}
			msg.arguments.Add("gotoMain");
			GUIManager.GetInstance().LoadDialog(msg);
		}
	}
}

public class TraumaRecords : GUIDialog
{
	GUIScrollView scroll;
	GUIHorizontalCommand template;
	
	public override void Initialize (ScreenInfo parent)
	{
		base.Initialize (parent);
		LoadResults();
		
		template = Find("caseItem01") as GUIHorizontalCommand;
		scroll = Find("caseListScrollview") as GUIScrollView;
		scroll.Elements.Clear();
				
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}
		
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonBack" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaMainMenu";
			msg.className = "TraumaMainMenu";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		
		if ( button.name[0] == 'B' )
		{
			// get index
			string name = button.name;
			name = name.Remove(0,1);
			int index = Convert.ToInt32(name);
			if ( index >= 0 && index < ResultList.Count )
			{
				LoadReport(index);
			}
		}
	}
	
	public virtual void LoadReport( int index )
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "loadReport");
        form.AddField("userid", LoginMgr.GetInstance().Username);
		form.AddField("sessionid", ResultList[index].sessionid);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,loadReport);
	}
	
	public virtual void loadReport(bool status, string data, string error_msg, WWW download)
	{
		TraumaScenarioReport report;
		
		if ( status == true )
		{
			Serializer<TraumaScenarioReport> serializer = new Serializer<TraumaScenarioReport>();
			report = serializer.FromString(data);
			// save the report to the AssessmentMgr
			TraumaReportMgr.GetInstance().SetReport(report);
			// call assessment dialog
			DialogMsg dmsg = new DialogMsg();
			dmsg.xmlName = "traumaDecisionBreakdown";
			dmsg.className = "DecisionBreakdown";
			dmsg.callback = BackToRecords;
			GUIManager.GetInstance().LoadDialog(dmsg);	
			Close ();
		}
	}
	
	public void BackToRecords( string status )
	{
	}
	
	public void LoadResults()
	{
		WWWForm form = new WWWForm();
        form.AddField("command", "loadResults");
        form.AddField("userid", LoginMgr.GetInstance().Username);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,loadResults);
	}

	public class ResultInfo
	{
		public string name;
		public string sessionid;
		public string datetime;
		public string scenario;
	}

	protected List<ResultInfo> ResultList = new List<ResultInfo>();	

	void loadResults(bool status, string data, string error_msg, WWW download)
	{
		ResultList.Clear();
		
		string[] split = data.Split('#');
		foreach( string item in split )
		{
			string[] fields = item.Split('&');
			if ( fields.Length == 4 )
			{
				ResultInfo ri = new ResultInfo();
				ri.name = fields[0];
				ri.sessionid = fields[1];
				ri.datetime = fields[2];
				ri.scenario = fields[3];
				ResultList.Add(ri);
			}
		}
		
		// now put items in GUI list
		int idx=0;
		foreach ( ResultInfo ri in ResultList )
		{
			GUIHorizontalCommand h = template.Clone() as GUIHorizontalCommand;
			GUIButton button = h.Find("buttonCaseItem01") as GUIButton;
			button.name = "B" + idx++;
			button.text = ri.scenario;
			button.UpdateContent();
			GUILabel date = h.Find("caseItem01Time") as GUILabel;
			date.text =  ri.datetime;
			scroll.Elements.Add(h);
		}
	}
}

public class ConfigLoadCase : GUIDialog
{
	GUIScrollView scroll;
	GUIHorizontalCommand template;
	
	public override void Initialize( ScreenInfo parent)
	{
		base.Initialize(parent);	
		
		// get all the stuff
		template = Find("caseItem01") as GUIHorizontalCommand;
		scroll = Find("caseListScrollview") as GUIScrollView;
		scroll.Elements.Clear();

		CaseConfiguratorMgr.GetInstance().LoadCaseConfigurations(loadCases);
	}
	
	void loadCases(bool status, string data, string error_msg, WWW download)
	{
		UnityEngine.Debug.LogError ("ConfigLoadCase.loadCases");
		if ( status == true )
		{
			int idx = 0;
			foreach( CaseConfiguratorMgr.CaseInfo ci in CaseConfiguratorMgr.GetInstance().CaseList )
			{
				GUIHorizontalCommand h = template.Clone() as GUIHorizontalCommand;
				GUIButton button = h.Find("buttonCaseItem01") as GUIButton;
				button.name = "B" + idx++;
				button.text = ci.name;
				button.UpdateContent();
				GUILabel date = h.Find("caseItem01Time") as GUILabel;
				date.text = ci.datetime;
				scroll.Elements.Add(h);
			}
		}
	}
	
	public void gotoConfigBasic(bool status, string data, string error_msg, WWW download)
	{
		UnityEngine.Debug.LogError ("ConfigLoadCase.gotoConfigBasic");
		// go to config
		DialogMsg msg = new DialogMsg();
		msg.xmlName = "traumaCaseConfig-BasicInfo";
		msg.className = "CaseConfigBasic";
		GUIManager.GetInstance().LoadDialog(msg);
		// close
		Close();
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonBack" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Main";
			msg.className = "CaseConfigMain";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		
		if ( button.name[0] == 'B' )
		{
			// get index
			string name = button.name;
			name = name.Remove(0,1);
			int index = Convert.ToInt32(name);
			// load case config
			CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().CaseList[index],gotoConfigBasic);
		}
	}	
}

public class TraumaCaseManager : GUIDialog
{
	GUIScrollView caseScroll;
	GUIScrollView userScroll;

	GUIHorizontalCommand userElement;
	GUIToggle caseInfo;
	
	GUILabel autoSave;

	GUILabel HRi,SYSi,DIAi,SPi;
	GUILabel HRe,SYSe,DIAe,SPe;
	
	GUILabel primaryInjury,secondaryInjury;
				
	public override void Initialize( ScreenInfo parent)
	{
		base.Initialize(parent);	

		caseScroll = Find("caseListScroll") as GUIScrollView;		
		userScroll = Find("usersScroll") as GUIScrollView;
		caseInfo = Find("buttonCaseItem01") as GUIToggle;
		userElement = Find("user01Horizontal") as GUIHorizontalCommand;
		caseScroll.Elements.Clear();
		userScroll.Elements.Clear();
		
		autoSave = Find("autoSaveText") as GUILabel;
		autoSave.text = "";
		
		HRi = Find("HRInitial-Number") as GUILabel;
		SYSi = Find("SYSInitial-Number") as GUILabel;
		DIAi = Find("DIAInitial-Number") as GUILabel;
		SPi = Find("SPO2Initial-Number") as GUILabel;

		HRe = Find("HRTarget-Number") as GUILabel;
		SYSe = Find("SYSTarget-Number") as GUILabel;
		DIAe = Find("DIATarget-Number") as GUILabel;
		SPe = Find("SPO2Target-Number") as GUILabel;
		
		primaryInjury = Find("primaryInjury") as GUILabel;
		secondaryInjury = Find("secondaryInjury") as GUILabel;

		LoadCases();
		LoadUsers();					
	}	
	
	public override void Update()
	{
		if ( Time.time > autoSaveTime )
		{
			autoSave.text = "";
		}
	}
	
	void ToggleCaseOff()
	{
		foreach( GUIObject go in caseScroll.Elements )
		{
			GUIToggle toggle = go as GUIToggle;
			if ( toggle != null )
			{
				toggle.toggle = false;
			}
		}
	}
	
	void ToggleUsers( bool onoff )
	{
		foreach( GUIObject go in userScroll.Elements )
		{
			GUIHorizontalCommand user = go as GUIHorizontalCommand;
			if ( user != null )
			{
				// set checkmark
				GUIToggle checkBox = user.Find("checkBox") as GUIToggle;
				checkBox.toggle = onoff;
			}
		}
	}
	
	void LoadCaseInfo()
	{
		CaseOptionData caseData = CaseConfiguratorMgr.GetInstance().Data;
		// set data
		primaryInjury.text = caseData.description;
		secondaryInjury.text = caseData.injury.Name;
		
		HRi.text = caseData.start.HR;
		DIAi.text = caseData.start.BPDIA;
		SYSi.text = caseData.start.BPSYS;
		SPi.text = caseData.start.SP;

		HRe.text = caseData.end.HR;
		DIAe.text = caseData.end.BPDIA;
		SYSe.text = caseData.end.BPSYS;
		SPe.text = caseData.end.SP;
	}
	
	void createCallback( string status )
	{
		LoadUsers();
		LoadCase(lastCase);
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name.Contains("user01") )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaNewUser";
			msg.className = "TraumaNewUser";
			msg.modal = true;
			msg.callback = createCallback;
			msg.arguments.Add(button.text);
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonBack" )
		{
			SaveCase();
			
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaCaseConfig-Main";
			msg.className = "CaseConfigMain";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		if ( button.name.Contains("C:") )
		{
			// save current case
			SaveCase();
			// init for new case
			ToggleCaseOff();
			GUIToggle toggle = button as GUIToggle;
			toggle.toggle = true;
			// get index from name
			int idx = Convert.ToInt32(toggle.name.Remove(0,2));
			// load case
			LoadCase(idx);
		}
		if ( button.name == "buttonAddUser" ) 
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaNewUser";
			msg.className = "TraumaNewUser";
			msg.modal = true;
			msg.callback = createCallback;
			GUIManager.GetInstance().LoadDialog(msg);
		}
		if ( button.name == "buttonDeleteCase" )
		{
			DeleteCase();
		}
		if ( button.name == "buttonSave" )
		{
			SaveCase();
		}
		if ( button.name == "selectAll" )
		{
			ToggleUsers(true);
		}
		if ( button.name == "selectNone" )
		{
			ToggleUsers(false);
		}
	}
	
	void reloadUsersCases( string status )
	{
		LoadCases();
		LoadUsers();
	}
	
	void DeleteCase()
	{
		CaseOptionData caseData = CaseConfiguratorMgr.GetInstance().Data;
		// check if we own this case
		if ( LoginMgr.GetInstance().Username == caseData.owner || LoginMgr.GetInstance().Username.ToLower () == "admin" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaDeleteConfirm";
			msg.className = "TraumaDeleteCaseConfirm";
			msg.modal = true;
			msg.arguments.Add(caseData.casename);
			msg.callback = reloadUsersCases;
			GUIManager.GetInstance().LoadDialog(msg);
		}
		else
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaErrorPopup";
			msg.className = "TraumaError";
			msg.modal = true;
			msg.arguments.Add("NOT CASE OWNER");
			msg.arguments.Add("You cannot delete this case because you are not the case creator/owner.");
			GUIManager.GetInstance().LoadDialog(msg);
			return;
		}
	}
	
	string currCaseName="";
	
	void loadUsers(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			// users loaded
			CaseOptionData caseData = CaseConfiguratorMgr.GetInstance().Data;
			// load info
			LoadCaseInfo();
			// save name
			currCaseName = caseData.casename;
			// turn off all users
			ToggleUsers(false);
			// enable users
			foreach( string name in caseData.users )
			{
				// enable all user buttons in the user list
				foreach( GUIObject obj in userScroll.Elements )
				{
					GUIHorizontalCommand user = obj as GUIHorizontalCommand;
					if ( user != null )
					{
						GUIButton userName = user.Find("user01") as GUIButton;
						if ( userName.text == name )
						{
							// set checkmark
							GUIToggle checkBox = user.Find("checkBox") as GUIToggle;
							checkBox.toggle = true;
						}
					}
				}
			}
		}
	}
	
	int lastCase=-1;
	public void LoadCase( int index )
	{
		if ( index == -1 )
			return;
		// load case config
		CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().CaseList[index],loadUsers);		
		lastCase = index;
	}
	
	float autoSaveTime=0.0f;
	
	List<string> usersAdd;		
	List<string> usersDel;		

	public void SaveCase()
	{
		// create new list
		usersAdd = new List<string>();		
		usersDel = new List<string>();		
		// set message time
		autoSaveTime = Time.time + 2.0f;
		// enable
		autoSave.text = "Case Saved...";			
		// get case data
		CaseOptionData caseData = CaseConfiguratorMgr.GetInstance().Data;
		// get list of enabled users
		foreach( GUIObject go in userScroll.Elements )
		{
			GUIHorizontalCommand user = go as GUIHorizontalCommand;
			if ( user != null )
			{
				// set checkmark
				GUIToggle checkBox = user.Find("checkBox") as GUIToggle;
				if ( checkBox.toggle == true )
				{
					GUIButton userName = user.Find("user01") as GUIButton;
					usersAdd.Add(userName.text);					
				}
				else
				{
					GUIButton userName = user.Find("user01") as GUIButton;
					usersDel.Add(userName.text);					
				}
			}
		}
		// set users into case
		caseData.users = usersAdd;			
		// save case config
		CaseConfiguratorMgr.GetInstance().SaveCaseConfiguration(currCaseName);
		// now save this case into all users
		foreach( string tmp in usersAdd )
		{
			LoginMgr.GetInstance().AddUserCase(tmp,currCaseName);
		}
		foreach( string tmp in usersDel )
		{
			LoginMgr.GetInstance().removeUserCase(tmp,currCaseName);
		}
	}

	public class userCases
	{
		public string name;
		public List<string> cases;
	}
	
	void loadCases(bool status, string data, string error_msg, WWW download)
	{
		caseScroll.Elements.Clear();
		
		if ( status == true )
		{			
			int idx = 0;
			foreach( CaseConfiguratorMgr.CaseInfo ci in CaseConfiguratorMgr.GetInstance().CaseList )
			{
				// create new
				GUIToggle tmp = caseInfo.Clone() as GUIToggle;
				caseScroll.Elements.Add(tmp);
				tmp.text = ci.name;
				tmp.UpdateContent();
				// set name
				tmp.name = "C:" + idx.ToString();
				// select if first case
				if ( idx == 0 )
					tmp.toggle = true;
				// inc
				idx++;
			}
			// start off with case 0
			LoadCase(0);
		}
	}
	
	public void LoadCases()
	{
		CaseConfiguratorMgr.GetInstance().LoadCaseConfigurations(loadCases);
	}
	
	void loginsCallback(bool status, string data, string error_msg, WWW download)
	{
		userScroll.Elements.Clear();
		foreach( LoginMgr.LoginInfo info in LoginMgr.GetInstance().LoginList )
		{
			// populate
			GUIHorizontalCommand user = userElement.Clone() as GUIHorizontalCommand;
			GUIButton name = user.Find("user01") as GUIButton;
			name.text = info.username;
			// change button name 
			GUILabel date = user.Find("userLastLogin") as GUILabel;
			date.text = info.datetime;
			userScroll.Elements.Add(user);
		}
	}
	
	public void LoadUsers()
	{
		LoginMgr.GetInstance().GetLogins(loginsCallback);
	}
}

public class TraumaDeleteCaseConfirm : GUIDialog
{
	string caseName;
	
	GUIDialog.GUIDialogCallback callback;
	
	public override void Load( DialogMsg msg )
	{
		caseName = msg.arguments[0];
		
		GUILabel confirmText = Find("confirmText") as GUILabel;
		confirmText.text = "Are you sure you want to delete the case <" + caseName + "> ??";
		callback = msg.callback;
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonOK" )
		{
			DeleteCase();
			Close();
		}
		if ( button.name == "buttonCancel" )
		{
			Close();
		}
	}
	
	void DeleteCase()
	{
		// delete caseName
		CaseConfiguratorMgr.GetInstance().DeleteCase(caseName);
		if ( callback != null )
			callback("ok");
	}
}

public class TraumaNewUser : GUIDialog
{
	GUIDialog.GUIDialogCallback callback;
	
	GUIEditbox username;
	GUIEditbox password;
	GUIEditbox firstName;
	GUIEditbox lastName;
	GUIToggle toggle;

	public override void Initialize( ScreenInfo parent)
	{
		base.Initialize(parent);	

		username = Find("usernameBox") as GUIEditbox;
		password = Find("passwordBox") as GUIEditbox;
		firstName = Find("FirstNameBox") as GUIEditbox;
		lastName = Find("LastNameBox") as GUIEditbox;
		toggle = Find("makeAdminTick") as GUIToggle;
	}
	
	public override void Load( DialogMsg msg )
	{
		base.Load(msg);
		// save callback
		callback = msg.callback;
		// load dialog if we are updating otherwise we are creating new
		if ( msg.arguments.Count > 0 )
		{
			LoginMgr.LoginInfo info = LoginMgr.GetInstance().GetLoginInfo(msg.arguments[0]);
			if ( info != null )
			{
				username.text = info.username;
				password.text = info.password;
				firstName.text = info.first;
				lastName.text = info.last;							
				toggle.toggle = info.admin;
				
				GUIButton button = Find("buttonCreate") as GUIButton;
				button.text = "UPDATE";
				GUILabel label = Find("titleText") as GUILabel;
				label.text = "Update User";
			}
		}
	}
	
	public void createCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( callback != null )
			callback((status==true)?"ok":"error");
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonCreate" )
		{
			if ( button.text == "CREATE" && LoginMgr.GetInstance().UserExists(username.text) == true )
			{
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaErrorPopup";
				msg.className = "TraumaError";
				msg.modal = true;
				msg.arguments.Add("Username Already Exists");
				msg.arguments.Add("The username <" + username.text + "> already exists.  Please select another username.");
				GUIManager.GetInstance().LoadDialog(msg);
			}
			else
			{
				bool admin = toggle.toggle;
				if ( username.text == null || username.text == "" )
					return;
				LoginMgr.GetInstance().CreateUser(username.text,password.text,firstName.text,lastName.text,admin,false,createCallback);
				Close();
			}
		}
		if ( button.name == "buttonCancel" )
		{
			Close();
		}
	}	
}

public class TraumaCaseSelection : GUIDialog
{
	GUIScrollView caseScroll;
	GUIHorizontalCommand caseArea;
	GUIHorizontalCommand caseInfo;
	GUIArea caseMenuArea;
	GUIButton buttonUnlock,buttonLock;
	GUIButton lastButtonChanged;
	//GUIHorizontalCommand caseBlank;
	
	float itemWidth;
	float scrollSeek=0.0f;
	float scrollScale=3000;
	float scrollInc=0;
	int caseIndex=0;

	// actual order
	List<string> Order;
	// order read from file
	List<string> FileOrder;
	
	void loadCases(bool status, string data, string error_msg, WWW download)
	{
		TraumaWaiting.CloseWaitingScreen();

		// get the case order
		FileOrder = CaseConfiguratorMgr.GetInstance().LoadCaseOrder();

		// make new "real" order list
		Order = new List<string>();

		if ( status == true )
		{
			// add blank at the beginning
			//caseArea.Elements.Add(caseBlank);
			
			int idx = 0;
			//foreach( CaseConfiguratorMgr.CaseInfo ci in CaseConfiguratorMgr.GetInstance().CaseList )
			foreach( string caseName in FileOrder )
			{
				// make sure case exists
				CaseConfiguratorMgr.CaseInfo ci = CaseConfiguratorMgr.GetInstance().LoadCaseInfo(caseName);
				if ( ci == null )
					continue;
				// add this existing case to real case order
				Order.Add (caseName);
				// create new
				GUIHorizontalCommand tmp = caseInfo.Clone() as GUIHorizontalCommand;
				caseArea.Elements.Add(tmp);
				// set info
				GUILabel title = tmp.Find("caseTitle") as GUILabel;
				title.text = ci.name;			
				GUILabel detailShort = tmp.Find("caseDateText") as GUILabel;
				detailShort.text = ci.descriptionShort;
				GUILabel detail = tmp.Find("caseDetailText") as GUILabel;
				detail.text = ci.description;
				GUILabel image = tmp.Find("caseImage") as GUILabel;
				if ( ci.thumbnail != null && ci.thumbnail != "" )
					image.SetStyle(image.Skin.GetStyle(ci.thumbnail));
				// set name
				tmp.name = "C" + idx.ToString();

				// add left panel button
				GUIButton panelButton = buttonUnlock.Clone() as GUIButton;
				panelButton.text = caseName;
				panelButton.name = "BB" + idx.ToString ();
				caseMenuArea.Elements.Add (panelButton);

				// inc
				idx++;
			}
			// add blank at the end
			//caseArea.Elements.Add(caseBlank);		
		}
		// load first config
		if ( CaseConfiguratorMgr.GetInstance().CaseList.Count > 0 )
			CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().LoadCaseInfo(Order[0]),null);
#if WRITE_CASE_XML
		// save cases here
		CaseConfiguratorMgr.GetInstance().SaveXML("CaseInfo.xml");
#endif
		// init once in update
		forceInit = true;
	}
	
	bool forceInit;
	public override void Initialize( ScreenInfo parent)
	{
		forceInit = false;
		base.Initialize(parent);	
			
		GUIManager.GetInstance().NativeSize = new Vector2(1920,1080);
		GUIManager.GetInstance().FitToScreen = true;
		GUIManager.GetInstance().Letterbox = true;

		TraumaWaiting.LoadWaitingScreen(); 

		caseScroll = Find("caseListScrollview") as GUIScrollView;
		caseArea = Find("casesHorizontal") as GUIHorizontalCommand;
		caseInfo = Find("caseItem01") as GUIHorizontalCommand;

		buttonUnlock = Find("bt_caseListUNLOCK") as GUIButton;
		buttonLock = Find("bt_caseListLOCK") as GUIButton;
		caseMenuArea = Find("caseMenuArea") as GUIArea;
		caseMenuArea.Elements.Clear ();

		//caseBlank = Find("caseBlank") as GUIHorizontalCommand;
		caseArea.Elements.Clear();
		itemWidth = caseInfo.Style.fixedWidth;

		CaseConfiguratorMgr.GetInstance().LoadUserAssignedCases(loadCases);		
				
		GUILabel version = Find ("versionText") as GUILabel;
		if ( version != null )
			version.text = (BuildVersion.GetInstance() != null ) ? BuildVersion.GetInstance().Version : "+BuildVersion";
	}
	
	public override void Update()
	{
		// if the timeScale is 0 then just set the position without increment
		if ( Time.timeScale == 0.0f )
		{
			if ( scrollSeek == 0.0f )
				scrollSeek = itemWidth/2.0f + caseIndex*itemWidth;
				
			caseScroll.scroll.x = scrollSeek;
			return;
		}			
		
		// force init the case scroll because somehow the scroller init happens later!!
		if ( forceInit == true )
		{
			forceInit = false;
			scrollSeek=caseScroll.scroll.x=itemWidth/2.0f;
		}
		
		if ( scrollInc > 0 && caseScroll.scroll.x < scrollSeek )
		{
			caseScroll.scroll.x += Time.deltaTime*scrollScale;
			if ( caseScroll.scroll.x >= scrollSeek )
			{
				// at end
				scrollInc = 0;
				caseScroll.scroll.x = scrollSeek;
			}
		}
		
		if ( scrollInc < 0 && caseScroll.scroll.x > scrollSeek )
		{
			caseScroll.scroll.x -= Time.deltaTime*scrollScale;
			if ( caseScroll.scroll.x <= scrollSeek )
			{
				scrollInc = 0;
				caseScroll.scroll.x = scrollSeek;
			}
		}
	}

	void StartLoading(bool status, string data, string error_msg, WWW download)
	{
		TraumaWaiting.CloseWaitingScreen();
		// close dialog
		Close ();
		// check scene we are in now for traumaMainMenu
		string loadedName = Application.loadedLevelName.ToLower ();
		// 
		if ( loadedName.Contains ("traumamainmenu") )
		{
			// running from main menu
			if ( CaseConfiguratorMgr.GetInstance().Data.missionStartXml != "" )
			{
				// has a mission start, start it
				UnityEngine.Debug.Log ("TraumaCaseSelection.ButtonCallback() : Going to mission start for case <" + CaseConfiguratorMgr.GetInstance().Data.casename + ">");
				DialogMsg msg = new DialogMsg();
				msg.xmlName = CaseConfiguratorMgr.GetInstance().Data.missionStartXml;
				msg.className = "TraumaMissionStart";
				msg.arguments.Add("TraumaCaseSelection");
				GUIManager.GetInstance().LoadDialog(msg);
				return;
			}
			else
			{
				// this is how the tutorial case starts, no mission start screen...
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "traumaLoadingScreen";
				msg.className = "TraumaLoadingScreen";
				GUIManager.GetInstance().LoadDialog(msg);
				return;
			}
		}
		else
		{
			// running from inside the game, just start
			CaseConfiguratorMgr.GetInstance().StartCase();
		}
	}
	
	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonBack" )
		{
			DialogMsg msg = new DialogMsg();
			msg.xmlName = "traumaMainMenu";
			msg.className = "TraumaMainMenu";
			GUIManager.GetInstance().LoadDialog(msg);
			Close();
		}
		
		if ( button.name == "buttonStart" )
		{
#if PRELOAD_CASE
			// put up loading screen
			TraumaWaiting.LoadWaitingScreen();
			// try this...
			CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().LoadCaseInfo(Order[caseIndex]),StartLoading);	
#else
			// close dialog
			Close ();
			// only do this if we are running from the main menu
			string loadedName = Application.loadedLevelName.ToLower ();
			if ( loadedName.Contains ("traumamainmenu") )
			{
				UnityEngine.Debug.Log ("TraumaCaseSelection.ButtonCallback() : Going to mission start for case <" + CaseConfiguratorMgr.GetInstance().Data.casename + ">");
				DialogMsg msg = new DialogMsg();
				msg.xmlName = "trauma05MissionStart";
				msg.className = "TraumaMissionStart";
				//msg.xmlName = "traumaLoadingScreen";
				//msg.className = "TraumaLoadingScreen";
				msg.arguments.Add("TraumaCaseSelection");
				GUIManager.GetInstance().LoadDialog(msg);
				return;
			}
			else
			{
				// running from inside the game, just start
				CaseConfiguratorMgr.GetInstance().StartCase();
			}
#endif
		}

		if ( button.name.Contains ("BB") )
		{
			if (lastButtonChanged != null){
				UnityEngine.Debug.Log("Resetting Last Button");
				lastButtonChanged.Style.normal.background = buttonUnlock.Style.normal.background;
				lastButtonChanged.Style.hover.background = buttonUnlock.Style.hover.background;
			}

			GUIStyle newStyle = new GUIStyle(buttonUnlock.Style);
			button.SetStyle(newStyle);
			button.Style.normal.background = button.Style.active.background;
			button.Style.hover.background = button.Style.active.background;

			string name = button.name.Substring(2);
			int caseIdx = Convert.ToInt32(name);
			if ( caseIdx > caseIndex )
				scrollInc = 1f;
			else
				scrollInc = -1f;
			caseIndex = caseIdx;
			scrollSeek = itemWidth/2.0f + caseIndex*itemWidth;

#if PRELOAD_CASE
			// we're here, load the case
			CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().LoadCaseInfo(Order[caseIndex]),null);

			lastButtonChanged = button;
#endif
		}
		
		if ( button.name == "buttonPrevCase" )
		{
			if ( Order.Count > 0 && caseIndex > 0 )
			{
				caseIndex--;
				scrollSeek = itemWidth/2.0f + caseIndex*itemWidth;
				scrollInc = -1f;

#if PRELOAD_CASE
				// we're here, load the case
				CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().LoadCaseInfo(Order[caseIndex]),null);	
#endif
			}
		}

		if ( button.name == "buttonNextCase" )
		{
			if ( Order.Count > 0 && caseIndex < Order.Count-1 )
			{
				caseIndex++;
				scrollSeek = itemWidth/2.0f + caseIndex*itemWidth;
				scrollInc = 1f;
#if PRELOAD_CASE
				// we're here, load the case
				CaseConfiguratorMgr.GetInstance().LoadCaseConfiguration(CaseConfiguratorMgr.GetInstance().LoadCaseInfo(Order[caseIndex]),null);		
#endif
			}
		}
	}

	public void resetButton(GUIButton button){

		UnityEngine.Debug.Log("Entering Reset Button Method");

		GUIStyle newStyle = new GUIStyle(buttonUnlock.Style);
		button.Style.normal.background = button.Style.active.background;
	}
}

public class TraumaWaiting : GUIDialog
{
	GUIArea area;

	static TraumaWaiting instance;
	public static TraumaWaiting GetInstance()
	{
		return instance;
	}

	public override void Load( DialogMsg msg )
	{
		instance = this;
		base.Load (msg);
		area = this.Find ("WaitingArea") as GUIArea;
	}

	public override void Update()
	{
		if ( area != null )
			area.rotation += Time.deltaTime*360.0f*2.5f;
	}

	public static void LoadWaitingScreen()
	{
		if ( Time.timeScale == 0.0f )
			return;
		GUIManager.GetInstance().Fade = false;
		// put up loading screen
		DialogMsg msg = new DialogMsg();
		msg.xmlName = "traumaWaiting";
		msg.className = "TraumaWaiting";
		msg.modal = true;
		GUIManager.GetInstance().LoadDialog(msg);
	}
	
	public static void CloseWaitingScreen()
	{
		if ( Time.timeScale == 0.0f )
			return;
		
		GUIManager.GetInstance().Fade = false;
		// close loading
		GUIManager.GetInstance().Remove ("traumaWaiting",true);
	}

}

class TraumaError : GUIDialog
{
	public override void Load( DialogMsg msg )
	{
		base.Load(msg);
		// change text based on params
		GUILabel title = Find("titleText") as GUILabel;
		GUILabel text = Find("loginErrorText") as GUILabel;
		if ( msg.arguments.Count == 2 )
		{
			title.text = msg.arguments[0];
			text.text = msg.arguments[1];
		}
	}

	public override void ButtonCallback(GUIButton button)
    {	
		base.ButtonCallback(button);
		
		if ( button.name == "buttonOk" )
		{
			Close();
		}
	}
}

class TraumaHelperHUD : GUIDialog
{
	GUIArea OptionsArea;
	GUIArea NetworkArea;
	GUIArea FpsArea;
	GUIArea SayArea;
	GUIArea HS_SayButton;
	GUIArea RightArea;
	GUIArea LeftArea;
	GUIArea PatientStatusArea;
	GUIArea CameraLookArea;
	GUIArea HS_CameraLook;
	GUIArea HomeKeyArea;
	GUIArea TeamViewArea;
	GUIArea HS_TeamView;
	GUIArea WalkLeftArea;
	GUIArea HS_WalkLeft;
	GUIArea WalkRightArea;
	GUIArea HS_WalkRight;
	GUIArea ClockArea;
	GUIArea ChatBarArea;
	GUIArea HS_ChatBar;

	List<GUIArea> hotspots;
	List<GUIObject> forcedHotspots;

	// hotspot and area image!
	Dictionary<GUIObject,GUIObject> hsDict;
	GUIObject[] keys;
	GUIObject[] values;

	void AddHotspot( string hotspot, string graphic )
	{
		GUIArea Ahotspot = Find(hotspot) as GUIArea;
		GUIArea Agraphic = Find(graphic) as GUIArea;
		if ( Ahotspot != null && Agraphic != null )
			hsDict.Add (Ahotspot,Agraphic);
	}

	public override void Load( DialogMsg msg )
	{
		base.Load (msg);

		// these are the areas that get lit up
		hotspots = new List<GUIArea>(); 

		hsDict = new Dictionary<GUIObject,GUIObject>();

		AddHotspot("HS-Options","OptionsMenuArea");
		AddHotspot("HS-NetworkSignal","networkSignalArea");
		AddHotspot("HS-FPS","FPS-QualityArea");
		AddHotspot("HS-SayButton","sayButton-Helper");
		AddHotspot("HS-CameraLook","cameraLookArea");
		AddHotspot("HS-HomePosition","homeKey-Area");
		AddHotspot("HS-TeamView","teamviewArea");
		AddHotspot("HS-WalkLeft","walkLeftArea");
		AddHotspot("HS-WalkRight","walkRightArea");
		AddHotspot("HS-Clock","clockHelper");
		AddHotspot("HS-ChatBar","chatBar-Helper");

		// make keys and values because stupid C# v2 doesn't support ToArray... uggg
		keys = new GUIObject[hsDict.Count];
		hsDict.Keys.CopyTo(keys,0);
		values = new GUIObject[hsDict.Count];
		hsDict.Values.CopyTo(values,0);

		HS_SayButton = Find ("HS-SayButton") as GUIArea; // actual hotspot
		HS_CameraLook = Find ("HS-CameraLook") as GUIArea; // actual hotspot
		HS_TeamView = Find ("HS-TeamView") as GUIArea; // actual hotspot
		HS_WalkLeft = Find ("HS-WalkLeft") as GUIArea; // actual hotspot
		HS_WalkRight = Find ("HS-WalkRight") as GUIArea; // actual hotspot
		HS_ChatBar = Find ("HS-ChatBar") as GUIArea; // actual hotspot

		forcedHotspots = new List<GUIObject>();
		
		AllInvisible ();
	}

	void AllInvisible()
	{
		return;
		// turn off any hotspots not active
		for( int i=0 ; i<hsDict.Count ; i++)
		{
			GUIObject hs = keys[i];
			GUIObject helper = values[i];
			// only turn an element off if it isn't in a hotspot
			if (GUIManager.GetInstance().Hotspots.Contains (hs) == false && 
			    forcedHotspots.Contains (hs) == false )
			{
				helper.visible = false;
			}
		}
	}

	void HandleHotspot( GUIObject obj )
	{
		if ( hsDict.ContainsKey(obj) )
		{
			GUIObject area = hsDict[obj];
			if ( area != null )
				area.visible = true;
		}
	}

	public override void Update()
	{
		AllInvisible ();

		foreach (GUIObject obj in GUIManager.GetInstance().Hotspots) 
			HandleHotspot(obj);
		
		foreach (GUIObject obj in forcedHotspots)
			HandleHotspot(obj);
	}

	public override void PutMessage(GameMsg msg){

		base.PutMessage(msg);
		
		GUIScreenMsg screenMsg = msg as GUIScreenMsg;
		if (screenMsg != null){
			string tokenValue;
			
			// messages to show or hide elements of the HUD <area>=<on,off>
			if (GetToken (screenMsg.arguments, "cameramove",out tokenValue)){
				if (tokenValue == "on"){
					forcedHotspots.Add (HS_WalkLeft);
					forcedHotspots.Add (HS_WalkRight);
				}

				else
				{
				if (forcedHotspots.Contains(HS_WalkLeft))
					forcedHotspots.Remove (HS_WalkLeft);
				if (forcedHotspots.Contains(HS_WalkRight))
					forcedHotspots.Remove (HS_WalkRight);
				}
			}

			if (GetToken (screenMsg.arguments, "cameralook",out tokenValue)){
				if (tokenValue == "on"){
					forcedHotspots.Add (HS_CameraLook);
				GUIManager.GetInstance ().Hotspots.Add (HS_CameraLook);
				}
				else
				{
				if (forcedHotspots.Contains(HS_CameraLook))
					forcedHotspots.Remove (HS_CameraLook);
				}	
			}

			if (GetToken (screenMsg.arguments, "chatbar",out tokenValue)){
				if (tokenValue == "on"){
					forcedHotspots.Add (HS_ChatBar);
				}
				else
				{
					if (forcedHotspots.Contains(HS_ChatBar))
						forcedHotspots.Remove (HS_ChatBar);
				}	
			}
			if (GetToken (screenMsg.arguments, "saybutton",out tokenValue)){
				if (tokenValue == "on"){
					forcedHotspots.Add (HS_SayButton);
				}
				else
				{
					if (forcedHotspots.Contains(HS_SayButton))
						forcedHotspots.Remove (HS_SayButton);
				}	
			}
			if (GetToken (screenMsg.arguments, "teamview",out tokenValue)){
				if (tokenValue == "on"){
					forcedHotspots.Add (HS_TeamView);
				}
				else
				{
					if (forcedHotspots.Contains(HS_TeamView))
						forcedHotspots.Remove (HS_TeamView);
				}	
			}
		}
	}
}

class TraumaPauseMenu : GUIDialog
{
	GrayscaleEffect grayscale;
	Blur blur;

	public override void Initialize( ScreenInfo parent )
	{
		base.Initialize(parent);
		Time.timeScale = 0.0f;
		grayscale = Camera.main.GetComponent<GrayscaleEffect>() as GrayscaleEffect;
		if ( grayscale )
			grayscale.enabled = true;
		blur = Camera.main.GetComponent<Blur>() as Blur;
		if ( blur )
			blur.enabled = true;
		(TraumaBrain.GetInstance() as TraumaBrain).StopAmbientSounds();
		// pause fast if screen active
		Pause(true);
	}

	public void Pause( bool yesno )
	{
		FastDialog gui1 = GUIManager.GetInstance().FindScreen("traumaQuickFast") as FastDialog;
		if ( gui1 != null )
			gui1.Pause(yesno);
		FastViewer gui2 = GUIManager.GetInstance().FindScreen("fastImageBreakdown") as FastViewer;
		if ( gui2 != null )
			gui2.Pause(yesno);
	}

	public override void OnClose ()
	{
		if ( grayscale != null )
			grayscale.enabled = false;
		if ( blur != null )
			blur.enabled = false;
		Time.timeScale = 1.0f;
		(TraumaBrain.GetInstance() as TraumaBrain).StartAmbientSounds();
		Pause(false);
	}

	public override void Update ()
	{
		if ( Input.GetKeyUp (KeyCode.Space))
		{
			Close ();
		}
	}

	public override void ButtonCallback( GUIButton button )
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("PauseMenu",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		base.ButtonCallback(button);
		switch( button.name )
		{
		case "buttonResume":
			Close ();
			break;
		case "buttonRestart":
			{
			Close();
			TraumaLoader loader = new GameObject("tmp").AddComponent<TraumaLoader>() as TraumaLoader;
			if ( loader != null )
				loader.Restart();
			}
			break;
		case "buttonCaseMenu":
			{
			Close();
			MenuLoader loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
			if ( loader != null )
				loader.GotoCase();
			}
			break;
		case "buttonMainMenu":
			{
			Close();
			MenuLoader loader = new GameObject("tmp").AddComponent<MenuLoader>() as MenuLoader;
			if ( loader != null )
				loader.GotoMain();
			}
			break;
		}
	}
}

public class TraumaNPCMenu : GUIDialog
{
	GUILabel lastLabel;
	GUILabel timeLabel;
	GUILabel lastTask;
	GUILabel npcTitle;
	GUILabel statusLabel;
	GUILabel statusImage;

	GUIButton currentTask;

	GUIScrollView secondaryMenuScrollArea;
	GUIHorizontalCommand task01;

	public override void Initialize( ScreenInfo parent )
	{	
		base.Initialize (parent);

		lastLabel = Find ("lastLabel") as GUILabel;
		lastTask = Find ("lastTask") as GUILabel;
		timeLabel = Find ("timeLabel") as GUILabel;
		npcTitle = Find ("npcTitle") as GUILabel;
		currentTask = Find ("currentTask") as GUIButton;
		secondaryMenuScrollArea = Find ("secondaryMenuScrollArea") as GUIScrollView;
		task01 = Find ("task01.Horz") as GUIHorizontalCommand;
		statusLabel = Find ("NPC-StatusLabel") as GUILabel;
		statusImage = Find ("NPC-CurrentStatus") as GUILabel;
	}

	public override void Load( DialogMsg msg )
	{	
		secondaryMenuScrollArea.Elements.Clear ();
		npcTitle.text = "Susanita";
		statusLabel.text = "Listening";
		currentTask.text = "Prepare Intubation";
		lastTask.text = "";
		timeLabel.text = "";

		ClearTasks ();

		InteractDialogMsg dmsg = msg as InteractDialogMsg;
		if (dmsg != null)
			Setup(dmsg);

		timeCommandStart = 0.0f;
		lastCommand = "";

		base.Load (msg);

		draggable = true;
	}

	GUIHorizontalCommand deleteObject;


	float redrawUpdateRate=0.2f;
	float redrawTime;

	public override void Update()
	{
		if ( deleteObject != null )
		{
			// do dequeing here!
			secondaryMenuScrollArea.Elements.Remove (deleteObject);
			deleteObject = null;
		}

		if ( Time.time > redrawTime )
		{
			redrawTime = Time.time + redrawUpdateRate;
			Redraw();
		}
		base.Update ();
	}

	public override void ButtonCallback( GUIButton button )
	{
		base.ButtonCallback(button);
		if ( button.name.Contains ("T") )
		{
			// find the button and get the index
			foreach ( GUIObject item in secondaryMenuScrollArea.Elements )
			{
				GUIHorizontalCommand horiz = item as GUIHorizontalCommand;
				if ( horiz != null )
				{
					GUIButton tmp = horiz.Find (button.name) as GUIButton;
					if ( tmp != null )
					{
						// we found our match
						deleteObject = horiz;
						// delete the script in the killmap
						SO.scriptArray.Remove(killMap[button.name]);
						return;
					}
				}
			}
		}
	}

	ObjectInteraction OI;
	ScriptedObject SO;
	Stack<ScriptedObject.QueuedScript> Queue;

	void Setup( InteractDialogMsg idmsg )
	{
		OI = idmsg.baseobj;
		if ( OI == null )
			return;

		SO = OI.GetComponent<ScriptedObject>() as ScriptedObject;
		Queue = SO.scriptStack;
		npcTitle.text = OI.Name;

		killMap = new Dictionary<string,ScriptedObject.QueuedScript>();

		Redraw();
	}

	float timeCommandStart;
	float timeCommandEnd;
	string lastCommand;

	Dictionary<string,ScriptedObject.QueuedScript> killMap;

	void Redraw()
	{
		if ( OI == null )
		{
			currentTask.text = "ObjectInteraction=NULL";
			return;
		}

		// set current and status
		if ( SO.GetCurrentScript() != null )
			currentTask.text = OI.actingInScript.prettyname;
		else
			currentTask.text = "Waiting for command";

		// set icon
		switch( OI.CurrentState )
		{
		case ObjectInteraction.State.idle:
			statusImage.SetStyle(Skin.FindStyle("NPC-StatusListening"));
			statusLabel.text = "Listening";
			break;
		case ObjectInteraction.State.busy:
			statusImage.SetStyle(Skin.FindStyle("NPC-StatusBusy"));
			statusLabel.text = "Busy";
			break;
		case ObjectInteraction.State.talking:
			statusImage.SetStyle(Skin.FindStyle("NPC-StatusTalking"));
			statusLabel.text = "Talking";
			break;
		}

		// clear the killmap
		killMap.Clear ();
		// clear the list
		secondaryMenuScrollArea.Elements.Clear ();
		if ( Queue != null )
		{
			// these are commands that can't be killed
			bool first = true;
			foreach( ScriptedObject.QueuedScript item in Queue )
			{
				if ( first == false )
					AddTask (item,false);
				first = false;
			}

			// this commands happen after stacked commands
			first=true;
			foreach (object o in SO.scriptArray)
			{
				ScriptedObject.QueuedScript q = o as ScriptedObject.QueuedScript;
				if ( q != null )
				{
					if ( first == false )
						AddTask (q,true);
					first = false;
				}
			}
		}

		// draw last command time and label
		if ( SO.lastScriptExecuted != null )
		{
			lastTask.text = SO.lastScriptExecuted.prettyname;
			timeLabel.text = GetTimeString(Time.time-SO.lastScriptExecutedTime);
		}
	}

	public string GetTimeString( float time )
	{
		if ( time < 0.0f )
			time = 0.0f;
		
		int hours, minutes, seconds;
		seconds = (int)(time);
		minutes = (int)(time / 60.0f);
		hours = (int)(minutes / 60.0f);
		minutes -= hours * 60;
		seconds -= (minutes * 60) + (hours * 3600);
		//return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
		return minutes.ToString("00") + ":" + seconds.ToString("00");
	}
	
	void ClearTasks()
	{
		secondaryMenuScrollArea.Elements.Clear ();
	}

	void AddTask( ScriptedObject.QueuedScript q, bool killable )
	{
		GUIHorizontalCommand newTask = task01.Clone () as GUIHorizontalCommand;
		if ( newTask != null )
		{
			// change name
			GUIButton label = newTask.Find ("button01.task") as GUIButton;
			label.text = q.script.prettyname;
			// change button name
			GUIButton close = newTask.Find ("buttonTaskCancel") as GUIButton;
			if ( killable == true )
			{
				close.name = "T" + secondaryMenuScrollArea.Elements.Count.ToString ();
				killMap[close.name] = q;
			}
			else
				newTask.Elements.Remove (close);
			// add it
			secondaryMenuScrollArea.Elements.Add (newTask);
		}
	}
}

public class FilterCommandGUI : GUIDialog
{
	GUIScrollView buttonArea;
	GUIButton buttonTemplate;
	GUIEditbox inputArea;
	GUILabel systemMessage;
	GUIStyle buttonNormal,buttonActive;
	
	int selectedIndex;
	
	bool showGUI=false;
	public bool ShowGUI
	{
		set { 
			// remove focus once :)
			if ( value == false )
				removeFocus = true;
			showGUI = value;	
		}
		get { return showGUI; }
	}
	
	// have to call this from HUD to make sure it 
	// is called at the right time :)
	bool removeFocus = false; // this was uninitialzed, check for problem in OSX Web Player

	public void RemoveFocus()
	{
		string focusName = GUI.GetNameOfFocusedControl();
		if ( removeFocus == true )
			GUI.FocusControl("");
		removeFocus = false;
	}
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);
		
		buttonArea = Find("taskButtonScrollview") as GUIScrollView;
		
		buttonTemplate = buttonArea.Find("taskButton01") as GUIButton;
		sizeButton = buttonTemplate.Style.fixedHeight;
		
		buttonNormal = buttonTemplate.Skin.FindStyle("taskButton01");		
		buttonActive = buttonTemplate.Skin.FindStyle("taskButton01Active");		
		
		systemMessage = Find("systemMessage") as GUILabel;
		
		lastText = "";
		selectedIndex = 0;
		removeFocus = false;
		
		buttonArea.Elements.Clear();
		
		ShowGUI = true;
	}
	
	List<FilterInteractions.CommandVariation> results;
	string lastText;
	
	public override void OnClose()
	{
		ShowGUI = false;
		base.OnClose();
	}
	
	public int MaxCount = 100; //11;
	
	public void BuildButtons()
	{
		if ( results != null && results.Count > 0 && results.Count <= MaxCount	 )
		{
			if ( selectedIndex >= results.Count )
				selectedIndex = results.Count-1;
			
			buttonArea.Elements.Clear();
			systemMessage.text = "";
			int cnt=0;
			foreach( FilterInteractions.CommandVariation cv in results )
			{
				GUIButton button = buttonTemplate.Clone() as GUIButton;
				button.name = "cmd" + cnt++;
#if SHOW_SELECTION_WEIGHTS
				button.text =  "["+cv.Record.Percentage.ToString("F2")+"]"+cv.CmdString; // temporarily show percentage weights
#else
				button.text =  cv.CmdString; 
#endif
				button.UpdateContent();
				buttonArea.Elements.Add(button);
			}
			HighlightButton();
		}
		else
		{
			buttonArea.Elements.Clear();
			if ( results.Count == 0 )
				systemMessage.text = "No Results found : please refine search\nPress ESC key to clear text field";
			else
				systemMessage.text = "(" + results.Count + ") Results found : please refine search\nPress ESC key to clear text field";
		}
	}
	
	public void HighlightButton()
	{
		// set highlight
		for ( int i=0 ; i<buttonArea.Elements.Count ; i++)
		{
			if ( i == selectedIndex )
				buttonArea.Elements[i].SetStyle(buttonActive);				
			else
				buttonArea.Elements[i].SetStyle(buttonNormal);				
		}
	}
	
	float scrollPosition=0;
	float sizeButton;
	
	public void SetScroller()
	{
		if ( selectedIndex < 5 )
			scrollPosition = 0;
		else
		{
			scrollPosition = (selectedIndex-4)*sizeButton;
		}
		buttonArea.scroll.y = scrollPosition;
	}
	
	public void IncrementKey()
	{
		if ( selectedIndex < (results.Count-1) )
		{
			selectedIndex++;
			HighlightButton();
			SetScroller ();
		}
	}
	
	public void DecrementKey()
	{
		if ( selectedIndex > 0 )
		{
			selectedIndex--;
			HighlightButton();
			SetScroller ();
		}
	}
	
	public void ReturnKey()
	{
		if ( results.Count > 0 && results.Count <= MaxCount )
		{
			Dispatcher.GetInstance().ExecuteCommand(results[selectedIndex].Cmd);
			Close();
		}
	}

	public void SetFromTag( string tag){
		results = FilterInteractions.GetInstance().FindFromTag( tag );
		BuildButtons();
	}
	
	public void Filter( string input )
	{
		results = FilterInteractions.GetInstance().Filter(input);
		BuildButtons();
	}

	public override void Update()
	{
		return;
		if ( inputArea.text != lastText )
		{
			results = FilterInteractions.GetInstance().Filter(inputArea.text);
			lastText = inputArea.text;
			BuildButtons();
		}
		base.Update();
	}
	
	public override void ButtonCallback(GUIButton button)
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("FilterCommandGUI",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		if ( button.name.Contains("cmd") )
		{
			string name = button.name.Replace("cmd","");
			int idx = System.Convert.ToInt32(name);
#if LEARN_SELECTION_WEIGHTS
			FilterInteractions.GetInstance().LogSelection( results[idx].Cmd);// learn the association between the input text and the desired command
#endif
			Dispatcher.GetInstance().ExecuteCommand(results[idx].Cmd);
			Close();
		}
		base.ButtonCallback(button);
	}
}

public class DecisionPanel : GUIDialog
{
	GUIScrollView buttonArea;
	GUIHorizontalCommand checkboxArea;
	GUIButton buttonTemplate;
	GUIEditbox inputArea;
	GUILabel systemMessage;
	GUILabel questionLabel;
	GUIStyle buttonNormal,buttonActive;
	GUIButton sayButton;
	GUIButton cancelButton;
	
	int selectedIndex;
	
	bool showGUI=false;
	bool allowNone=false;

	bool forceSubmit=false;
	bool submitEnabled=false;

	public bool ShowGUI
	{
		set { 
			// remove focus once :)
			if ( value == false )
				removeFocus = true;
			showGUI = value;	
		}
		get { return showGUI; }
	}
	
	// have to call this from HUD to make sure it 
	// is called at the right time :)
	bool removeFocus = false;
	public void RemoveFocus()
	{
		string focusName = GUI.GetNameOfFocusedControl();
		if ( removeFocus == true )
			GUI.FocusControl("");
		removeFocus = false;
	}
	
	public override void Initialize(ScreenInfo parent)
	{
		base.Initialize(parent);

		buttonArea = Find("taskButtonScrollview") as GUIScrollView;
		
		buttonTemplate = buttonArea.Find("taskButton01") as GUIButton;
		sizeButton = buttonTemplate.Style.fixedHeight;
		
		buttonNormal = buttonTemplate.Skin.FindStyle("taskButton01");		
		buttonActive = buttonTemplate.Skin.FindStyle("taskButton01Active");		

		checkboxArea = buttonArea.Find("checkboxHorizontal") as GUIHorizontalCommand;
		
		systemMessage = Find("systemMessage") as GUILabel;
		systemMessage.text = "";

		questionLabel = Find ("questionLabel") as GUILabel;

		sayButton = Find ("submit") as GUIButton;

		cancelButton = Find ("cancel") as GUIButton;
		cancelButton.visible = false;

		lastText = "";
		selectedIndex = 0;
		removeFocus = false;
		
		buttonArea.Elements.Clear();

		forceSubmit = false;
		submitEnabled = false;
		
		ShowGUI = true;
	}

	Dictionary<string,string> interactDict;
	Dictionary<string,string> scriptsDict;
	Dictionary<string,string> setvarDict;

	void AddInteract( GUIButton button, string interaction )
	{
		if ( interaction==null || interaction=="")
			return;

		if ( interactDict == null )
			interactDict = new Dictionary<string,string>();
		if ( interactDict != null && interaction != null && interaction != "" )
			interactDict[button.name] = interaction;
	}

	void AddScript( GUIButton button, string script )
	{
		if ( script == null || script == "")
			return;

		if ( scriptsDict == null )
			scriptsDict = new Dictionary<string,string>();
		if ( scriptsDict != null && script != null && script != "" )
			scriptsDict[button.name] = script;
	}

	void AddSetVar( GUIButton button, string setvar )
	{
		if ( setvar == null || setvar == "")
			return;
		
		if ( setvarDict == null )
			setvarDict = new Dictionary<string,string>();
		if ( setvarDict != null && setvar != null && setvar != "" )
			setvarDict[button.name] = setvar;
	}
	
	string GetInteract( string button )
	{
		if ( interactDict == null )
			return null;
		if ( interactDict.ContainsKey (button) )
			return interactDict[button];
		return null;
	}
	
	string GetScript( string button )
	{
		if ( scriptsDict == null )
			return null;
		if ( scriptsDict.ContainsKey (button) )
			return scriptsDict[button];
		return null;
	}
	
	string GetSetVar( string button )
	{
		if ( setvarDict == null )
			return null;
		if ( setvarDict.ContainsKey (button) )
			return setvarDict[button];
		return null;
	}

	// FORMAT for SetVar
	// setvar=object.variable:value (NOTE, : has to replace the = because of parsing
	//
	void DoSetVar( string setvar )
	{
		// parse
		string expression;
		if ( TokenMgr.GetToken(setvar,"setvar",out expression) )
		{
			// split the expression
			string[] tokens = expression.Split (':');
			if ( tokens.Length == 2 )
			{
				// create a DecisionVariable
				DecisionVariable var = new DecisionVariable(tokens[0]);
				if ( var != null )
				{
					// set the variable
					var.Set(tokens[1]);
				}
			}
		}
	}
	
	void AddButton( string arg, List<string> pairs )
	{	
		string name;
		TokenMgr.GetToken (pairs,"button",out name);
		string text;

		// change old style cancel to use the new button
		if ( name.ToLower() == "cancel" )
		{
			TokenMgr.GetToken(pairs,"text",out text);
			if ( text.ToLower () != "no" )
			{
				// NOTE, HACK ALERT!!!
				// if name of button is "No" then let this go, otherwise
				// use the cancel button on the bar
				cancelButton.visible = true;
				return;
			}
		}

		TokenMgr.GetToken (pairs,"text",out text);
		GUIButton button = buttonTemplate.Clone() as GUIButton;
		button.name = name;
		button.AddMessage(arg);
		button.text = text.Replace("\"","");
		button.UpdateContent();
		buttonArea.Elements.Add(button);

		string interaction;
		TokenMgr.GetToken (pairs,"interact", out interaction);
		AddInteract (button, interaction);
		string script;
		TokenMgr.GetToken (pairs,"script", out script);
		AddScript (button, script);
	}

	void AddCheckbox( string arg, List<string> pairs )
	{
		string name;
		TokenMgr.GetToken (pairs,"checkbox",out name);
		string text;
		TokenMgr.GetToken (pairs,"text",out text);
		string interaction;
		// check both "interact" and "interaction"
		if ( TokenMgr.GetToken (pairs,"interact", out interaction) == false )
			TokenMgr.GetToken (pairs,"interaction", out interaction);
		string script;
		TokenMgr.GetToken (pairs,"script", out script);
		string setvar;
		TokenMgr.GetToken (pairs,"setvar", out setvar);
//		setvar = "setvar=patient.HR:100";

		if ( checkboxArea != null )
		{
			GUIHorizontalCommand area = checkboxArea.Clone() as GUIHorizontalCommand;
			if ( area != null )
			{
				GUIToggle toggle = area.Find ("checkboxToggle") as GUIToggle;
				if ( toggle != null )
				{
					area.name = "CB" + name;
					toggle.name = name;
					// NOTE!! take this out so that we don't generate a msg
					// when the toggle is pressed
					//toggle.AddMessage (arg);

					AddInteract (toggle,interaction);
					AddScript (toggle,script);
					AddSetVar (toggle,setvar);

				}
				GUILabel label = area.Find ("checkboxLabel") as GUILabel;
				if ( label != null )
				{
					label.name = name;
					label.text = text.Replace("\"","");
				}
				buttonArea.Elements.Add (area);
			}
		}
	}

	void SetQuestion( List<string> pairs )
	{
		string question;
		TokenMgr.GetToken(pairs,"question",out question);

		string multi;
		if ( TokenMgr.GetToken(pairs,"checklist",out multi) == true && multi == "true")
		{
			// enable submit button by default
			sayButton.visible = true;
			submitEnabled = true;

			// this is a muti-select checkbox list
			GUIScreenMsg gsmsg = new GUIScreenMsg("HUDScreen");
			gsmsg.arguments.Add ("chatbar=off");
			GUIManager.GetInstance().PutMessage(gsmsg);		
		}
		else
		{
			// no checklist then disable both buttons
			sayButton.visible = cancelButton.visible = false;
			
			// this is a button list
			GUIScreenMsg gsmsg = new GUIScreenMsg("HUDScreen");
			gsmsg.arguments.Add ("chatbar=off");
			GUIManager.GetInstance().PutMessage(gsmsg);		
		}

		if ( questionLabel != null )
			questionLabel.text = question;
	}

	string screenToMsg;
	GUIScreen infoDialog;

	public override void Load (DialogMsg msg)
	{
		base.Load (msg);

		// disable logger behind us
		infoDialog = GUIManager.GetInstance().FindScreen("InfoDialog");
		if ( infoDialog != null )
			infoDialog.visible = false;
		
		// send msg to HUD to close the quick command panel
		GameHUD hud = GUIManager.GetInstance().FindScreen ("HUDScreen") as GameHUD;
		if ( hud != null )
		{
			GUIScreenMsg guiMsg = new GUIScreenMsg();
			guiMsg.arguments.Add ("quickCommand=off");
			hud.PutMessage(guiMsg);
		}

		// clear button area
		buttonArea.Elements.Clear ();

		// init
		allowNone = false;

		// get arguments
		foreach( string arg in msg.arguments )
		{
			List<string> pairs = TokenMgr.GetKeyValuePairs (arg);
			string question;
			if ( TokenMgr.GetToken(pairs,"question",out question) == true )
			{
				SetQuestion(pairs);
			}
			string button;
			if ( TokenMgr.GetToken(pairs,"button",out button) == true )
			{
				AddButton(arg,pairs);
			}
			string checkbox;
			if ( TokenMgr.GetToken(pairs,"checkbox",out checkbox) == true )
			{
				AddCheckbox(arg,pairs);
			}
			string screen;
			if ( TokenMgr.GetToken(pairs,"screen",out screen) == true )
			{
				screenToMsg = screen;
			}
			string allow;
			if ( TokenMgr.GetToken(pairs,"allownone",out allow) == true )
			{
				if (allow == "true")
					allowNone = true;
			}
			string cancelbutton;
			if ( TokenMgr.GetToken(pairs,"cancelbutton", out cancelbutton ) )
			{
				if ( cancelbutton == "true" )
					cancelButton.visible = true;
				if ( cancelbutton == "false" )
					cancelButton.visible = false;
			}
			string forcesubmit;
			if ( TokenMgr.GetToken(pairs,"forcesubmit", out forcesubmit ) )
			{
				if ( forcesubmit == "true" )
					forceSubmit = true;
				if ( forcesubmit == "false" )
					forceSubmit = false;
			}
		}
	}
	
	List<FilterInteractions.CommandVariation> results;
	string lastText;
	
	public override void OnClose()
	{
		// turn off chatbar
		GUIScreenMsg gsmsg = new GUIScreenMsg("HUDScreen");
		gsmsg.arguments.Add ("chatbar=on");
		GUIManager.GetInstance().PutMessage(gsmsg);

		// turn back on infoDialog
		if ( infoDialog != null )
			infoDialog.visible = true;

		ShowGUI = false;
		base.OnClose();
	}
	
	public void HighlightButton()
	{
		// set highlight
		for ( int i=0 ; i<buttonArea.Elements.Count ; i++)
		{
			if ( i == selectedIndex )
				buttonArea.Elements[i].SetStyle(buttonActive);				
			else
				buttonArea.Elements[i].SetStyle(buttonNormal);				
		}
	}
	
	float scrollPosition=0;
	float sizeButton;
	
	public void SetScroller()
	{
		if ( selectedIndex < 5 )
			scrollPosition = 0;
		else
		{
			scrollPosition = (selectedIndex-4)*sizeButton;
		}
		buttonArea.scroll.y = scrollPosition;
	}
	
	public void IncrementKey()
	{
		if ( selectedIndex < (results.Count-1) )
		{
			selectedIndex++;
			HighlightButton();
			SetScroller ();
		}
	}
	
	public void DecrementKey()
	{
		if ( selectedIndex > 0 )
		{
			selectedIndex--;
			HighlightButton();
			SetScroller ();
		}
	}

	public override void PutMessage( GameMsg msg )
	{
		GUIScreenMsg smsg = msg as GUIScreenMsg;
		if ( smsg != null )
		{
			HandleSayButton ();
		}
	}

	string HandleScript( string name )
	{
		// build scripts list
		string script = GetScript (name);
		if ( script != null )
		{
			// do special handling here to log the ISM
			string[] tokens = script.Split ('@');
			if ( tokens.Length == 2 )
			{
				// pick out the script and record it as an ISM
				InteractStatusMsg ismsg = new InteractStatusMsg(tokens[1]);
				LogMgr.GetInstance().Add (new InteractStatusItem(ismsg));
			}
		}
		return script;
	}

	void HandleCheckboxes()
	{
		foreach( GUIObject obj in buttonArea.Elements )
		{
			GUIHorizontalCommand area = obj as GUIHorizontalCommand;
			if ( area != null )
			{
				GUIToggle toggle = area.FindObjectOfType(typeof(GUIToggle)) as GUIToggle;
				if ( toggle != null && toggle.toggle == true )
				{
					// handle scripts
					string script = HandleScript(toggle.name);

					// handle setting
					string setvar = GetSetVar(toggle.name);
					if ( setvar != null )
						DoSetVar(setvar);	
					
					// try to dispatch interact
					string interaction = GetInteract(toggle.name);
					if ( interaction != null )
						Dispatcher.GetInstance().ExecuteCommand(interaction);
				}
			}
		}
	}

	void GetCheckboxOptionsAndScripts( ref string options, ref string scripts )
	{
		// add checkboxes
		int optionsCnt=0;
		int scriptsCnt=0;
		
		foreach( GUIObject obj in buttonArea.Elements )
		{
			GUIHorizontalCommand area = obj as GUIHorizontalCommand;
			if ( area != null )
			{
				GUIToggle toggle = area.FindObjectOfType(typeof(GUIToggle)) as GUIToggle;
				if ( toggle != null && toggle.toggle == true )
				{
					// build options list
					if ( optionsCnt > 0  )
						options += ",";
					options += (toggle.name);
					optionsCnt++;
					
					// build scripts list
					string script = GetScript(toggle.name);
					if ( script != null )
					{
						// add comma if 1 or more
						if ( scriptsCnt > 0 )
							scripts += ",";
						scripts += script;
						// inc
						scriptsCnt++;
					}					
				}
			}
		}
	}

	bool IsCheckboxChecked()
	{
		foreach( GUIObject obj in buttonArea.Elements )
		{
			GUIHorizontalCommand area = obj as GUIHorizontalCommand;
			if ( area != null )
			{
				GUIToggle toggle = area.FindObjectOfType(typeof(GUIToggle)) as GUIToggle;
				if ( toggle != null && toggle.toggle == true )
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void Update()
	{
		if ( forceSubmit == true )
			return;

		if ( IsCheckboxChecked() == true )
		{
			// normal
			submitEnabled = true;
			sayButton.visible = true;
		}
		else
		{
			// gray out style
			submitEnabled = false;
			sayButton.visible = false;
		}
	}

	string GetSayButtonOptions()
	{
		// get optionss from checkboxes
		string options = "options=";
		string scripts = "script=";			
		GetCheckboxOptionsAndScripts(ref options, ref scripts);		
		return "say " + "onbutton=say" + " " + options + " " + scripts;
	}

	void HandleSayButton()
	{
		// load button name with "say" and options from checkboxes
		GUIButton b = new GUIButton();
		b.name = GetSayButtonOptions();
		// callback
		ButtonCallback (b);
	}

	public override void ButtonCallback(GUIButton button)
	{
		// log it & Save
		LogMgr.GetInstance().Add (new ButtonClickLogItem("DecisionPanel",button.name));
		if ( InteractPlaybackMgr.GetInstance() != null )
			InteractPlaybackMgr.GetInstance().Save ();

		// if we get the submit button, mimic the say button call
		if ( button.name == "submit" && submitEnabled == true )
		{
			// load button name with "say" and options from checkboxes
			button.name = GetSayButtonOptions();
		}

		// handle straight buttons
		if ( button.name != "question" && (button as GUIToggle) == null )
		{
			// check and send screen msg
			if ( screenToMsg != null )
			{
				GUIScreenMsg smsg = new GUIScreenMsg(screenToMsg);
				smsg.arguments.Add (button.name);
				GUIManager.GetInstance().PutMessage(smsg);
			}
			// do interaction if the button has one
			string interaction = GetInteract(button.name);
			if ( interaction !=  null )
			{
				// execute command
				Brain.GetInstance().PutMessage (new InteractStatusMsg(interaction));
			}
			// handle button scripts
			HandleScript (button.name);
			// log checkboxes if exist
			string options="",scripts="";
			HandleCheckboxes();
			// we're outa here
			Close ();
		}

		// toggle checkboxe state
		GUIToggle toggle = button as GUIToggle;
		if ( toggle != null )
		{
			// find parent
			GUIHorizontalCommand area = Find("CB"+button.name) as GUIHorizontalCommand;
			if ( area != null )
			{
				if ( toggle.toggle == true )
					area.SetStyle (area.Skin.FindStyle("checkBoxHorizontal-Active"));
				else
					area.SetStyle (area.Skin.FindStyle("checkBoxHorizontal-normal"));
			}
		}

		base.ButtonCallback(button);
	}

}









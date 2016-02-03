using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

[Serializable]
public class LogItem
{
    public float time;
    static protected string format = "{0:0000.}";

    public LogItem()
    {
        this.time = Brain.GetInstance().elapsedTime;
    }

    //public LogItem(float time)
    //{
    //    this.time = time;
    //}

    public virtual string XMLString()
    {
        string xml = "";
        return xml;
    }

    public virtual void WriteXML(TextWriter writer)
    {
        writer.WriteLine(XMLString());
    }

    public virtual string PrettyPrint()
    {
        return "";
    }

    public float Time
    {
        get { return time; }
    }

	public virtual string GetTimeString()
	{
        int hours, minutes, seconds;
		seconds = (int)(time);
        minutes = (int)(time / 60.0f);
        hours = (int)(minutes / 60.0f);
        minutes -= hours * 60;
		seconds -= (minutes * 60) + (hours * 3600);
        //return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
        return minutes.ToString("00") + ":" + seconds.ToString("00");
	}

    public virtual void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
    }

    public virtual void Load(string xmlstring)
    {
        XmlReader reader = XmlReader.Create(new StringReader(xmlstring));
        if (reader != null)
        {
            reader.Read();
            reader.MoveToContent();
            time = Convert.ToSingle(reader.GetAttribute("time"));
        }
    }
}

[Serializable]
public class StringLogItem : LogItem
{
    public string value;

    public StringLogItem()
        : base()
    { }

    public StringLogItem(float time, string name)
        : base()
    {
        this.value = name;
    }

    public override string XMLString()
    {
        string xml = "\t<string text=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + value;
    }

    public override void GetItems( ref List<string> items )
    {
        base.GetItems(ref items);
        items.Add(value);
    }
}

public class InfoDialogLogItem : StringLogItem
{
    public InfoDialogLogItem()
        : base()
    { }

    public InfoDialogLogItem(float time, string name)
        : base(time,name)
    {
    }

    public override string XMLString()
    {
        string xml = "\t<InfoDialog text=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : InfoDialog : " + value;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("InfoDialog");
        items.Add(value);
    }
}

[Serializable]
public class VitalsBehaviorLogItem : StringLogItem
{
    public VitalsBehaviorLogItem()
        : base()
    { }

    public VitalsBehaviorLogItem(float time, string name)
        : base(time,name)
    {
    }

    public override string XMLString()
    {
        string xml = "\t<VitalsBehavior text=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : VitalsBehavior : " + value;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("VitalsBehavior");
        items.Add(value);
    }
}


[Serializable]
public class ParamLogItem : LogItem
{
    public string param;
    public string value;

    public ParamLogItem()
        : base()
    {
    }

    public ParamLogItem(float time, string param, string value)
        : base()
    {
        this.param = param;
        this.value = value;
    }

    public override string XMLString()
    {
        string xml = "\t<ParamLogItem text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + param + " : " + value;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add(param);
        items.Add(value);
    }

    public override void Load(string xmlstring)
    {
        base.Load(xmlstring);

        XmlReader reader = XmlReader.Create(new StringReader(xmlstring));
        if (reader != null)
        {
            reader.Read();
            reader.MoveToContent();
            param = reader.GetAttribute("text");
            value = reader.GetAttribute("value");
        }
    }
}

public class LabLogItem : ParamLogItem
{
    public bool Updated;

    public LabLogItem() : base()
    { }

    public LabLogItem(float time, string param, string value)
        : base(time, param, value)
    {
        Updated = false;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Lab" + " : " + param + " : " + value;
    }

    public override string XMLString()
    {
        string xml = "\t<LabLogItem text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Lab");
        items.Add(param);
        items.Add(value);
    }

    public void SetLab(string text)
    {
        value = text;
    }
}

public class MedLogItem : ParamLogItem
{
    public MedLogItem() : base()
    {
    }

    public MedLogItem(float time, string param, string value)
        : base(time, param, value)
    { }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Med" + " : " + param + " : " + value;
    }

    public override string XMLString()
    {
        string xml = "\t<MedLogItem text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Med");
        items.Add(param);
        items.Add(value);
    }
}

public class PhysicalExamLogItem : ParamLogItem
{
    public PhysicalExamLogItem()
        : base()
    { }

    public PhysicalExamLogItem(float time, string param, string value)
        : base(time, param, value)
    {
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Exam" + " : " + param + " : " + value;
    }

    public override string XMLString()
    {
        string xml = "\t<PhysicalExamLogItem text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Exam");
        items.Add(param);
        items.Add(value);
    }
}

public class ButtonClickLogItem : StringLogItem
{
	public string gui;
	public string button;

	public ButtonClickLogItem()
		: base()
	{ }
	
	public ButtonClickLogItem(string gui, string button)
		: base()
	{
		this.gui = gui;
		this.button = button;
	}
	
	public override string XMLString()
	{
		string xml = "\t<ButtonClick gui=\"" + gui + "\" button=\"" + button + "\" time=\"" + String.Format(format, time) + "\"/>";
		return xml;
	}
	
	public override string PrettyPrint()
	{
		return String.Format(format, time) + " : ButtonClick : " + gui + "." + button;
	}
	
	public override void GetItems(ref List<string> items)
	{
		items.Add(GetTimeString());
		items.Add("ButtonClick");
		items.Add(gui);
		items.Add(button);
	}
}

public class InteractStatusItem : LogItem
{
    public InteractStatusMsg Msg;

    public InteractStatusItem(InteractStatusMsg msg)
        : base()
    {
        Msg = msg;
    }

	public override string PrettyPrint()
	{
		return String.Format(format, time) + " : " + "InteractStatusMsg" + " : " + Msg.InteractName;
	}
	
    public override string XMLString()
    {
        string xml = "\t<InteractStatusItem text=\"" + Msg.InteractName + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }
}

public class InteractLogItem : ParamLogItem
{
    public string who;
    public string response;
	public string args;
	public string scripted;

    public InteractLogItem() : base()
    {
    }

    public string InteractName
    {
        get
        {
            return value;
        }
    }

	string MakeParamString( InteractMsg msg )
	{
		string arg="";
		
		if ( msg == null || msg.map.param == null )
			return arg;

		foreach( string item in msg.map.param )
		{
			arg += item + " ";
		}

		return arg;
	}

    public InteractLogItem(float time, string name, string action, string response, InteractMsg imsg=null)
        : base(time, name, action)
    {
		this.args = MakeParamString(imsg);
		this.args = this.args.Replace ("\"","&quot;");
		this.scripted = imsg.scripted.ToString ();
        this.response = response;
    }

    public override string XMLString()
    {
		string xml = "\t<InteractLogItem text=\"" + param + "\" value=\"" + value + "\" args=\"" + args + "\" scripted=\"" + scripted + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Interact" + " : " + param + " : " + value;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Interact");
        items.Add(param);
        items.Add(value);
        items.Add(response);
    }

	public override void Load(string xmlstring)
	{
		base.Load(xmlstring);
		
		XmlReader reader = XmlReader.Create(new StringReader(xmlstring));
		if (reader != null)
		{
			reader.Read();
			reader.MoveToContent();
			args = reader.GetAttribute("args");
			scripted = reader.GetAttribute("scripted");
		}
	}
}

public class RecordLogItem : ParamLogItem
{
    public string who;
    public string response;

    public RecordLogItem()
        : base()
    {
    }

    public RecordLogItem(float time, string name, string action, string response, string who)
        : base(time, name, action)
    {
        this.response = response;
        this.who = who;
    }

    public override string XMLString()
    {
        string xml = "\t<RecordLogItem text=\"" + param + "\" value=\"" + value + "\" who=\"" + who + "\" time=\"" + String.Format(format, time) + "\"/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Interact" + " : " + param + " : " + value + " : " + who;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Record");
        items.Add(param);
        items.Add(value);
        items.Add(response);
        items.Add(who);
    }
}

public class TimeLogItem : ParamLogItem
{
    public TimeLogItem()
        : base()
    { }

    public TimeLogItem(float time, string param, string value)
        : base(time, param, value)
    { }

    public override string XMLString()
    {
        float paramF = Convert.ToSingle(value);
        string xml = "\t<time text=\"" + param + "\" value=\"" + String.Format(format, paramF) + "\" time=\"" + String.Format("{0:0.00}", time) + "\"/>";
        return xml;
    }
}

public class BoolLogItem : ParamLogItem
{
    public BoolLogItem()
        : base()
    { }

    public BoolLogItem(float time, string param, string value)
        : base(time, param, value)
    { }

    public override string XMLString()
    {
        string xml = "\t<bool text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\" />";
        return xml;
    }
}

public class PercentageLogItem : ParamLogItem
{
    public PercentageLogItem()
        : base()
    { }

    public PercentageLogItem(float time, string param, string value)
        : base(time, param, value)
    { }

    public override string XMLString()
    {
        string xml = "\t<percentage text=\"" + param + "\" value=\"" + value + "\" time=\"" + String.Format(format, time) + "\" />";
        return xml;
    }
}

public class RangeLogItem : LogItem
{
    public string param;
    public string value;
    public string min, max;

    public RangeLogItem()
        : base()
    { }

    public RangeLogItem(float time, string param, string value, string min, string max)
        : base()
    {
        this.param = param;
        this.value = value;
        this.min = min;
        this.max = max;
    }

    public override string XMLString()
    {
        string xml = "\t<range text=\"" + param + "\" value=\"" + value + "\" min=\"" + min + "\" max=\"" + max + "\" time=\"" + String.Format(format, time) + "\" />";
        return xml;
    }
}

public class DialogueLogItem : LogItem
{
    public DialogueLogItem()
        : base()
    { }

    public string title;
    public string answer;
    public bool result;

    public DialogueLogItem(float time, string title, string answer, bool result)
        : base()
    {
        this.title = title;
        this.answer = answer;
        this.result = result;
    }

    public override string XMLString()
    {
        string xml = "\t<DialogueLogItem title=\"" + title + "\" text=\"" + answer + "\" value=\"";
        xml += result ? "Correct" : "Wrong";
        xml += "\" time=\"" + String.Format(format, time) + "\" />";
        return xml;
    }

    public override void GetItems(ref List<string> items)
    {
        items.Add(GetTimeString());
        items.Add("Dialogue");
        items.Add(title);
        items.Add(answer);
        //items.Add(result.ToString());
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Dialog" + " : " + title + " : " + answer;
    }
}

public class ObjectiveLogItem : LogItem
{
    public ObjectiveLogItem()
        : base()
    { }

    public string n;
    public string id;
    public string rawscore;
    public string minscore;
    public string maxscore;
    public string status;

    public ObjectiveLogItem(float time, string n, string id, string rawscore, string minscore, string maxscore, string status)
        : base()
    {
        this.time = time;
        this.n = n;
        this.id = id;
        this.rawscore = rawscore;
        this.minscore = minscore;
        this.maxscore = maxscore;
        this.status = status;
    }

    public override string XMLString()
    {
        string xml = "\t<ObjectiveLogItem n=\"" + n + "\" id=\"" + id + "\" rawscore=\"" + rawscore + "\" minscore=\"" + minscore + "\" maxscore=\"" + maxscore + "\" status=\"" + status +  "/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "Objective" + " n: " + n + " id: " + id + " raw: " + rawscore + " min: " + minscore + " max: " + maxscore + " status: " + status;
    }
}

public class TurnScoreItem : LogItem
{
    public TurnScoreItem()
        : base()
    { }

    public string turn;
    public int scorecard;
    public int interaction;
    public int classification;

    public TurnScoreItem(float time, string turn, int scorecard, int interaction, int classification)
        : base()
    {
        this.time = time;
        this.turn = turn;
        this.scorecard = scorecard;
        this.interaction = interaction;
        this.classification = classification;
    }

    public override string XMLString()
    {
        string xml = "\t<TurnScoreItem turn=\"" + 	 scorecard + "\" interaction=\"" + interaction + "\" classification=\"" + classification + "/>";
        return xml;
    }

    public override string PrettyPrint()
    {
        return String.Format(format, time) + " : " + "TurnScoreItem" + " turn: " + turn + " scorecard: " + scorecard + " interaction: " + interaction + " classification: " + classification;
    }
}

public class ButtonLogItem : StringLogItem
{
	public ButtonLogItem() : base() {}
	public ButtonLogItem( string value ) : base()
	{
		this.value = value;
	}
	
    public override string XMLString()
    {
        string xml = "\t<ButtonItem value=\"" + value + "\" time=\"" + String.Format(format, time) + "/>";
        return xml;
    }
}

public class DialogButtonItem : LogItem
{
	public string dialog;
	public string button;

	public DialogButtonItem() : base()
	{}

	public DialogButtonItem( string dialog, string button ) : base()
	{
		this.dialog = dialog;
		this.button = button;
	}

	public override string XMLString()
	{
		string xml = "\t<DialogButtonItem dialog=\"" + dialog + "\" button=\"" + button + "\" time=\"" + String.Format(format, time) + "\"/>";
		return xml;
	}

	public override void Load(string xmlstring)
	{
		base.Load(xmlstring);
		
		XmlReader reader = XmlReader.Create(new StringReader(xmlstring));
		if (reader != null)
		{
			reader.Read();
			reader.MoveToContent();
			dialog = reader.GetAttribute("dialog");
			button = reader.GetAttribute("button");
		}
	}
}

public class XApiLogItem : LogItem
{
	public string Actor;
	public string Verb;
	public string Object;

	public XApiLogItem() : base()
	{}

	public XApiLogItem( string actor, string verb, string obj )
	{
		this.Actor = actor;
		this.Verb = verb;
		this.Object = obj;
	}

	public override string XMLString()
	{
		string xml = "\t<XApiLogItem actor=\"" + Actor + "\" Verb=\"" + Verb + "\" Object=\"" + Object + "\" time=\"" + String.Format(format, time) + "\"/>";
		return xml;
	}

	public JSONObject ToJSON()
	{
		JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
		j.AddField("actor",Actor);
		j.AddField("verb",Verb);
		JSONObject arr = new JSONObject(JSONObject.Type.ARRAY);
		j.AddField("object", arr);
		arr.AddField("time",String.Format (format,time));
		arr.AddField("display",Object);
		return j;
	}
}

public class Log
{
    public string Name;
    public List<LogItem> Items;
	
	public Log()
	{
	}

    public Log(string name)
    {
        Name = name;
        Items = new List<LogItem>();
    }

    public string Username;

    public void Add(LogItem item)
    {
        Items.Add(item);
    }

    public void PreAlloc(int size)
    {
        Items.Capacity = size;
    }

    public void WriteXML(string filename)
    {
        StringWriter output = new StringWriter();
        WriteXML(output);
        output.Close();

        StringReader input = new StringReader(output.ToString());

        XmlDocument document = new XmlDocument();
        document.Load(input);
        document.Save(filename);
    }

    public void WriteXML(TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException("writer == null");

        writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
        writer.WriteLine("<" + Name + ">");

        // write all the log records
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].WriteXML(writer);
        }

        writer.WriteLine("</" + Name + ">");
    }

	public List<string> CreateLogItemStrings()
	{
		List<string> items = new List<string>();
		foreach( LogItem item in Items )
		{
			items.Add(item.XMLString());
		}
		return items;
	}
}

public class LogMgr
{
    public List<Log> Logs;

    public LogMgr()
    {
        Logs = new List<Log>();
    }

    Log current;

    /// <summary>
    /// The singleton instance
    /// </summary>
    private static LogMgr instance;
    /// <summary>
    /// Creates the game screen manager using the specified texture
    /// </summary>
    /// <param name="texture">The texture to use when fading the screen</param>
    /// <returns>The singleton instance of this class</returns>
    public static LogMgr CreateInstance()
    {
        if (instance == null)
            instance = new LogMgr();

        return instance;
    }

    /// <summary>
    /// Returns the singleton instance of this class
    /// </summary>
    /// <returns></returns>
    public static LogMgr GetInstance()
    {
        if (instance == null)
            instance = CreateInstance();
        return instance;
    }

    public void ClearLogs()
    {
        Logs = new List<Log>();
        current = null;
    }

    public Log CreateLog(string name)
    {
//        Debug.Log("LogMgr.CreateLog(" + name + ")");
        Log log = new Log(name);
        Logs.Add(log);
        return log;
    }

    public Log FindLog(string name)
    {
        for (int i = 0; i < Logs.Count; i++)
        {
            if (Logs[i].Name == name)
                return Logs[i];
        }

        return null;
    }

    public void Add( string logname, LogItem item )
    {
        // get log
        Log log = FindLog(logname);

        // add item
        if (log != null)
            Add(log,item);

        Incoming inc = Component.FindObjectOfType(typeof(Incoming)) as Incoming;
        if(inc != null)
            inc.SendLogEntry(logname, item);
    }

    public void Add(Log log, LogItem item)
    {
        // add it
        log.Add(item);

        // debug
		if ( item.PrettyPrint() != "" )
        	Debug.Log(item.PrettyPrint());

		// hook for InteractPlaybackMgr
		if ( InteractPlaybackMgr.GetInstance() != null && item as InteractLogItem != null )
			InteractPlaybackMgr.GetInstance().Save();

#if USE_INCOMING
        Incoming inc = Component.FindObjectOfType(typeof(Incoming)) as Incoming;
        if(inc != null)
            inc.SendLogEntry(log.Name, item);
#endif
    }

    public void Add(LogItem item)
    {
        if (current != null)
        {
            Add(current, item);
        }
    }

    public void SetCurrent(string logname)
    {
        Log log = FindLog(logname);

        if (log != null)
        {
            Debug.Log("LogMgr.SetCurrent(" + logname + ")");
            current = log;
        }
    }

    public Log GetCurrent()
    {
        if (current == null)
            Debug.Log("LogMgr.GetCurrent() = null");

        return current;
    }
	
	public void Save( string filename )
	{
		Log current = GetCurrent();
		if ( current != null )
		{
			current.WriteXML(filename);
		}
	}
	
    public LogItem CreateItemFromString(string xmlstring)
    {
        // make sure item doesn't have &gt, &lt symbols
        if (xmlstring.Contains("&gt"))
        {
            xmlstring.Replace("&gt", ">");                
        }
        if (xmlstring.Contains("&lt"))
        {
            xmlstring.Replace("&lt", "<");
        }
		if (xmlstring.Contains("&quot"))
		{
			xmlstring.Replace("&quot;","\"");
		}

        LogItem value;

        try
        {
            // ok, now translate item
            XmlReader reader = XmlReader.Create(new StringReader(xmlstring));
            reader.Read();
            if (reader != null && reader.EOF == false)
            {
                reader.MoveToContent();
                //Debug.Log("CreateItemFromString() : reader.Name=" + reader.Name);
                switch (reader.Name)
                {
                    case "PhysicalExamLogItem":
                        value = new PhysicalExamLogItem();
                        value.Load(xmlstring);
                        Debug.Log("CreateItemFromString() : " + value.XMLString());
                        return value;
                    case "LabLogItem":
                        value = new LabLogItem();
                        value.Load(xmlstring);
                        Debug.Log("CreateItemFromString() : " + value.XMLString());
                        return value;
                    case "MedLogItem":
                        value = new MedLogItem();
                        value.Load(xmlstring);
                        Debug.Log("CreateItemFromString() : " + value.XMLString());
                        return value;
                    case "InteractLogItem":
                        value = new InteractLogItem();
                        value.Load(xmlstring);
                        Debug.Log("CreateItemFromString() : " + value.XMLString());
                        return value;
					case "DialogButtonItem":
						value = new DialogButtonItem();
						value.Load(xmlstring);
						Debug.Log("CreateItemFromString() : " + value.XMLString());
						return value;
                }
            }
        } 
        catch( Exception ex )
        {
            Debug.Log("LogMgr.CreateItemFromString() : Exception ex=" + ex.Message);
        }
        return null;
    }

    public List<T> FindLogItems<T>()
    {
        List<T> list = new List<T>();

        if (current != null)
        {
            foreach (LogItem item in current.Items)
            {
                if (item.GetType() == typeof(T))
                {
                    T newitem = (T)Convert.ChangeType(item,typeof(T));
                    list.Add(newitem);
                }
            }
        }

        return list;
    }
}

public class LogRecord
{
	public string DateTime;
	public List<string> Items;
	
	public LogRecord()
	{
		Items = new List<string>();
	}
	
	public void SetDateTime()
	{
		DateTime = System.DateTime.Now.ToString();
	}
	
	public void Add( Log log )
	{
		if ( log != null )
		{
			foreach( LogItem item in log.Items )
			{
				Items.Add(item.XMLString());
			}
		}
	}
	
	public void SaveXML( string filename )
	{
		string path=null;
		
		if ( filename == null )
		{
#if UNITY_EDITOR
			path = EditorUtility.SaveFilePanel("Enter log file name","",filename,"xml");
#endif
			if ( path == "" || path == null )
				return;
		}
		else
			path = filename;
		
		Serializer<LogRecord> serializer = new Serializer<LogRecord>();
		if ( serializer != null && path != null)
		{
			serializer.Save(path,this);
		}		
	}	
	
	// for now assume that this reads from a stream, not from a text asset
	public void Load()
	{
		string path=null;

#if UNITY_EDITOR
		path = EditorUtility.OpenFilePanel("Select log file...","","xml");
#endif
		if ( path == "" || path == null )
			return;
		
		Serializer<LogRecord> serializer = new Serializer<LogRecord>();
		if ( serializer != null)
		{
			LogRecord record = serializer.Load(new StreamReader(path));
			if ( record != null )
			{
				this.Items.Clear();
				// copy
				this.DateTime = record.DateTime;
				foreach (string item in record.Items )
					this.Items.Add(item);
			}
		}		
	}
	
	public List<LogItem> CreateLogItems()
	{
		List<LogItem> LogItems = new List<LogItem>();
		
		foreach( String item in Items )
		{
			// only create items that are defined
			LogItem LogItem = LogMgr.GetInstance().CreateItemFromString(item);
			if ( LogItem != null )
				LogItems.Add(LogItem);	
		}
		
		return LogItems;
	}
}


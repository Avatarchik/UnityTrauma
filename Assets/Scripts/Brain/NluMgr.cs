#define RESULT_ONLY_IF_BAD

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;

public class NluMgr : MonoBehaviour
{
    public class match_record
    {
        public int index;
        public string sim_command;
        public float score;
        public string input;
        public string command_subject;
        public List<sim_command_param> parameters;
        public List<missing_sim_command_param> missing_parameters;
        public string readback;
        public string feedback;
		
		public string GetParameter(string param)
		{
			foreach( sim_command_param p in parameters )
			{
				if ( p.name == param )
					return p.value;
			}
			return null;
		}

        public void Debug()
        {
            UnityEngine.Debug.Log("match_record: index=" + index + " : sim_command=" + sim_command + " : score=" + score + " : input=" + input);
        }
    }

    public class sim_command_param
    {
        public string name;
        public string value;
        public string mandatory;

        public void Debug()
        {
            UnityEngine.Debug.Log("sim_command_param : name=<" + name + "> : value=<" + value + "> : mandatory=<" + mandatory + ">");
        }
    }

    public class missing_sim_command_param
    {
        public string name;
        public string mandatory;

        public void Debug()
        {
            UnityEngine.Debug.Log("missing_sim_command_param : name=<" + name + "> : mandatory=<" + mandatory + ">");
        }
    }

    public class feedback_sentence
    {
        public string kind;
        public string value;

        public void Debug()
        {
            UnityEngine.Debug.Log("feedback_sentence : kind=<" + kind + "> : value=<" + value + ">");
        }
    }

    public class context_setup_reply
    {
        public string apiversion;
        public int setup_request_status;
        public string context_uid;

        public context_setup_reply()
        {
            context_uid = "NONE";
        }
    }

    public class process_utterance_reply
    {
        public int utterance_request_status;
        public List<match_record> match_records;

        public process_utterance_reply()
        {
            match_records = new List<match_record>();
        }
    }

    public NluMgr() 
    {
        instance = this;
		url = urlDefault;
    }

    public string TestUtterance = "roll the patient on the left side";

    private static NluMgr instance;
    public static NluMgr GetInstance()
    {
        if (instance == null)
            UnityEngine.Debug.Log("NluMgr.GetInstance() == null : you need to add the NluMgr script to the brain");

        return instance;
    }

    public void Awake()
    {
    }

    float elapsedTime = 0.0f;
    float timer = 1.0f;
    bool first = true;
    string status;
    string command;
    public bool debug=false;

    public bool Initialized = false;

    public void Update()
    {
#if DEBUG_NLU
        if (Input.GetKeyUp(KeyCode.F12))
        {
            debug = 1 - debug;
        }
#endif

        elapsedTime += Time.deltaTime;

        if (elapsedTime > timer)
        {
            if (first == true)
            {
                ContextSetupRequest("1.0", "trauma", null);
                first = false;
            }
            timer = elapsedTime + 10.0f;
        }
    }

    public void ContextSetupRequest(string apiversion, string title, List<string> nlustates)
    {
        StringBuilder memout = new StringBuilder();
        XmlWriter writer = XmlWriter.Create(memout);
        writer.WriteStartDocument();
        writer.WriteStartElement("context_setup_request");

        writer.WriteStartElement("context_uid");
        writer.WriteAttributeString("desc", "text");
        writer.WriteAttributeString("action", "create");
        writer.WriteEndElement();

        writer.WriteStartElement("apiversion");
        writer.WriteString(apiversion);
        writer.WriteEndElement();

        writer.WriteStartElement("title");
        writer.WriteString(title);
        writer.WriteEndElement();

        // do NLU states later

        writer.WriteEndElement();
        writer.WriteEndDocument();

        // send command
        StartCoroutine(SendCmdWWW(memout.ToString()));
    }
	
	public void SetURL( string url )
	{
		this.url = url;
		// reissue setup command
        ContextSetupRequest("1.0", "trauma", null);
	}
	
	public string GetURL()
	{
		return this.url;
	}

    public void Utterance( string utterance, string speaker )
    {
		if (utterance.Contains("url=") )
		{
			string oldurl = url;
			// replace send URL
			url = utterance.Replace("url=","");		
			// if *, replace with default
			if ( url == "default" )
				url = urlDefault;
			
			// reissue setup command
            ContextSetupRequest("1.0", "trauma", null);

			QuickInfoMsg msg = new QuickInfoMsg();
            msg.title = "NLU URL";
            msg.text = "new NLU url <" + url + "> old was <" + oldurl + ">";
            msg.timeout = 0.0f;
            QuickInfoDialog.GetInstance().PutMessage(msg);
			
			return;
		}
        if (csr.context_uid == "NONE" || Initialized == false)
        {
            QuickInfoMsg msg = new QuickInfoMsg();
            msg.title = "NLU STATUS";
            msg.text = "Waiting for NLU Initialization! : status=" + status;
            msg.h = Screen.height - 50;
            msg.timeout = 0.0f;
            QuickInfoDialog.GetInstance().PutMessage(msg);
            return;
        }

        StringBuilder memout = new StringBuilder();
        XmlWriter writer = XmlWriter.Create(memout);
        writer.WriteStartDocument();

        writer.WriteStartElement("process_utterance_request");

        writer.WriteStartElement("context_uid");
        writer.WriteString(csr.context_uid);
        writer.WriteEndElement();

        writer.WriteStartElement("utterance");
        writer.WriteString(utterance);
        writer.WriteEndElement();

        writer.WriteStartElement("speaker");
        writer.WriteString(speaker);
        writer.WriteEndElement();

        writer.WriteEndElement();

        writer.WriteEndDocument();

        // send command
		StartCoroutine(SendCmdWWW(memout.ToString()));
	}
	
	public void ContextTerminate(string apiversion, string title)
    {
        StringBuilder memout = new StringBuilder();
        XmlWriter writer = XmlWriter.Create(memout);
        writer.WriteStartDocument();
        writer.WriteStartElement("context_setup_request");

        writer.WriteStartElement("context_uid");
        writer.WriteAttributeString("desc", "text");
        writer.WriteAttributeString("action", "terminate");
        writer.WriteString(csr.context_uid);
        writer.WriteEndElement();

        writer.WriteStartElement("apiversion");
        writer.WriteString(apiversion);
        writer.WriteEndElement();

        writer.WriteStartElement("title");
        writer.WriteString(title);
        writer.WriteEndElement();

        // do NLU states later

        writer.WriteEndElement();
        writer.WriteEndDocument();

        // send command
		StartCoroutine(SendCmdWWW(memout.ToString()));
	}
	
	public void UtteranceAudio( AudioClip clip )
	{
		// strip off silence, making a new clip
		AudioClip newClip = SaveWav.TrimSilence(clip,0.01f);		
		// make a WAV file in memory
		MemoryStream memStream = SaveWav.Save(newClip);		
		
#if TEST_WAVFILE
		// save this to a temp file for testing
		FileStream file = new FileStream("test.wav", FileMode.Create, FileAccess.Write); 
		memStream.WriteTo(file);
		file.Close();
#endif
		
		// Convert the binary input into Base64 UUEncoded output.
        string base64String = System.Convert.ToBase64String(memStream.ToArray(),0,memStream.ToArray().Length);
		
		// form XML packet command
        StringBuilder memout = new StringBuilder();
        XmlWriter writer = XmlWriter.Create(memout);
        writer.WriteStartDocument();
        writer.WriteStartElement("process_audio_request");

        writer.WriteStartElement("context_uid");
        writer.WriteString(csr.context_uid);
        writer.WriteEndElement();

        writer.WriteStartElement("utterance");
        writer.WriteAttributeString("utter_type", "audiobytes");
        writer.WriteEndElement();

        writer.WriteStartElement("req_type");
        writer.WriteAttributeString("file_type", ".wav");
		writer.WriteString("audio_file");
        writer.WriteEndElement();
		
		writer.WriteStartElement("audiobytes");
		writer.WriteString(base64String);
		writer.WriteEndElement();

		writer.WriteEndElement();
        writer.WriteEndDocument();

#if WRITE_XML
		// save this to a temp file for testing
		FileStream file = new FileStream("audioreq.xml", FileMode.Create, FileAccess.Write); 
		file.Write(GetBytes(memout.ToString()),0,memout.Length);
		file.Close();
#endif

		StartCoroutine(SendCmdWWW(memout.ToString()));
	}
	
	byte[] GetBytes(string str)
	{
	    byte[] bytes = new byte[str.Length * sizeof(char)];
	    System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
	    return bytes;
	}	
	
    public void SendCmd(string title, string cmd)
    {
        Debug.Log("NluMgr.SendCmd(" + cmd + ")");

        // make external call to Unity
        Application.ExternalCall("NluSend", cmd);
    }

    byte[] strToByteArray(string str)
    {
        System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();//.ASCIIEncoding();  
        byte[] array = enc.GetBytes(str);
        return array;
    }

    float timeCmdWWW = 0.0f;
    float CmdTime = 0.0f;

    List<string> replies = new List<string>();
	
    string urlDefault = "http://lms2dev.sitelms.org:8080/nlu/Command";
	string url;
	
	public string RawWWWString;
	
	public IEnumerator SendCmdWWW(string cmd)
    {
#if DEBUG_REPLIES
        replies.Add(cmd);
#endif

        WWW www = new WWW(url, strToByteArray(cmd));
        status = "SendCmdWWW : URL=" + url + " : CMD=<" + cmd + ">";
        UnityEngine.Debug.Log(status);
        command = cmd;

        timeCmdWWW = Time.time;

		yield return www;
		
		if (www.error == null)
		{
			CmdTime = Time.time - timeCmdWWW;
			UnityEngine.Debug.Log("NLUMgr.WaitForRequest() : Time=" + CmdTime.ToString() + " : <" + www.data + ">");
			#if DEBUG_REPLIES
			replies.Add("NLUMgr.WaitForRequest() : Time=" + CmdTime.ToString() + " : <" + www.data + ">");
			#endif
			ParseXMLResponse(www.data);
			// save raw string
			RawWWWString = www.data;
		}
		else
		{
			UnityEngine.Debug.Log("NLUMgr.WaitForRequest() : ERROR! : <" + www.error + ">");
			#if DEBUG_REPLIES
			replies.Add("NLUMgr.WaitForRequest() : ERROR! : <" + www.error + ">");
			#endif
		}
	}
	
    context_setup_reply csr = new context_setup_reply();

    public void ParseContextSetupReply(XmlReader reader)
    {
        // verify node is "context_setup_reply
        if (reader.Name == "context_setup_reply")
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "apiversion")
                {
                    csr.apiversion = reader.ReadElementContentAsString();
                    //UnityEngine.Debug.Log("apiversion=" + csr.apiversion);
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "setup_request_status")
                {
                    csr.setup_request_status = reader.ReadElementContentAsInt();
                    //UnityEngine.Debug.Log("setup_request_status=" + csr.setup_request_status);
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "context_uid")
                {
                    csr.context_uid = reader.ReadElementContentAsString();
					
#if SHOW_INIT
                    QuickInfoMsg msg = new QuickInfoMsg();
                    msg.title = "NLU STATUS";
                    msg.text = "NLU Initialized : " + CmdTime + " seconds";
                    msg.timeout = 2.0f;
                    QuickInfoDialog.GetInstance().PutMessage(msg);
#endif

                    Initialized = true;

                    //UnityEngine.Debug.Log("context_uid=" + csr.context_uid);
                }
            }
        }
    }

    process_utterance_reply pur;

    public void ParseUtteranceReply(XmlReader reader)
    {
        pur = new process_utterance_reply();

        // verify node is "context_setup_reply
        if (reader.Name == "process_utterance_reply")
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "utterance_request_status")
                {
                    pur.utterance_request_status = reader.ReadElementContentAsInt();
#if DEBUG_REPLY
                    UnityEngine.Debug.Log("utterance status=" + pur.utterance_request_status);
#endif
                    // report error
                    if (pur.utterance_request_status < 0 && errorCallback != null)
                    {
		                //'utterance_request_status' indicates if utterance process is final or NLU server starts 
		                // 0: OK. the 'match_record' list contains the simulation command(s) for the utterance
		                // 1: NLU server executes 'clarifying NLU sub-session' (see "SiTEL-SMS API Reqs & Specs" document)
		                //-1: cannot process utterance
		                //-2: MLU not ready for utterance processing
		                //-3: missing utterance text
                        string error="none";
                        switch (pur.utterance_request_status)
                        {
                            case -1:
                                error = "error -1: cannot process utterance";
                                break;
                            case -2:
                                error = "error -2: MLU not ready for utterance processing";
                                break;
                            case -3:
                                error = "error -3: missing utterance text";
                                break;
                        }
                        errorCallback(error);
                    }
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "match_records")
                {
                    List<sim_command_param> parameters=null;
                    List<missing_sim_command_param> missing = null;
                    match_record record = null;

                    while (reader.Read())
                    {
#if DEBUG_REPLY
                        if (reader.NodeType == XmlNodeType.Element)
                            UnityEngine.Debug.Log("NODE_NAME=" + reader.Name + ", NODE_TYPE=" + reader.NodeType.ToString());
                        else if (reader.NodeType == XmlNodeType.EndElement)
                            UnityEngine.Debug.Log("NODE_END=" + reader.Name + ", NODE_TYPE=" + reader.NodeType.ToString());
                        else
                            UnityEngine.Debug.Log("NODE_TYPE=" + reader.NodeType.ToString());
#endif
                       
                        // match START element
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "match")
                        {
                            if (reader.IsEmptyElement == false)
                            {
                                record = new match_record();
                                if ( reader.MoveToAttribute("index") == false )
                                    UnityEngine.Debug.Log("CAN'T FIND INDEX!");
                                record.index = reader.ReadContentAsInt();
                                if ( reader.MoveToAttribute("sim_command") == false )
                                    UnityEngine.Debug.Log("CAN'T FIND SIM_COMMAND!");
                                record.sim_command = reader.ReadContentAsString();
                                if ( reader.MoveToAttribute("score") == false )
                                    UnityEngine.Debug.Log("CAN'T FIND SCORE!");
                                record.score = reader.ReadContentAsFloat();
                                if ( reader.MoveToAttribute("input") == false )
                                    UnityEngine.Debug.Log("CAN'T FIND INPUT!");
                                record.input = reader.ReadContentAsString();

#if DEBUG_REPLY
                                UnityEngine.Debug.Log("START MATCH : sim_command=" + record.sim_command);
#endif
                            }
                        }
                        // match END element
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "match")
                        {
                            if (reader.IsEmptyElement == false)
                            {
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("END MATCH");
#endif

                                pur.match_records.Add(record);

                                // do callback here, we have a valid match!!
                                if (utteranceCallback != null)
                                    utteranceCallback(record); 

                                record.Debug();
                            }
                        }
                        // sim command parameters
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "sim_command_params")
                        {
                            record.parameters = new List<sim_command_param>();

                            if (reader.IsEmptyElement == false)
                            {
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("SIM_COMMAND_PARAMS");
#endif
                            }
                        }
                        // sc_param
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "sc_param")
                        {
                            sim_command_param sc_param = new sim_command_param();

                            if (reader.IsEmptyElement == false)
                            {
                                if (reader.MoveToAttribute("mandatory") == false)
                                    UnityEngine.Debug.Log("CAN'T FIND MANDATORY!");
                                sc_param.mandatory = reader.ReadContentAsString();
                                if ( reader.MoveToAttribute("name") == false )
                                    UnityEngine.Debug.Log("CAN'T FIND NAME!");
                                sc_param.name = reader.ReadContentAsString();
                                // value
                                reader.Read();
                                sc_param.value = reader.Value.Trim();
                                // add it
                                record.parameters.Add(sc_param);
    
                                sc_param.Debug();
                            }
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "missing_sim_command_params")
                        {
                            record.missing_parameters = new List<missing_sim_command_param>();

                            if (reader.IsEmptyElement == false)
                            {
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("MISSING_SIM_COMMAND_PARAMS");
#endif
                            }
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "missing_sc_param") 
                        {
                            if (reader.IsEmptyElement == false)
                            {
                                missing_sim_command_param sc_param = new missing_sim_command_param();
#if DEBUG_REPLY
                                if (reader.MoveToAttribute("mandatory") == false)
                                    UnityEngine.Debug.Log("MISSING CAN'T FIND MANDATORY!");
                                sc_param.mandatory = reader.ReadContentAsString();
                                if (reader.MoveToAttribute("name") == false)
                                    UnityEngine.Debug.Log("MISSING CAN'T FIND NAME!");
#endif
                                sc_param.name = reader.ReadContentAsString();
                                record.missing_parameters.Add(sc_param);

                                sc_param.Debug();
                            }
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "readback")
                        {
                            if (reader.IsEmptyElement == false)
                            {
                                reader.Read();
                                record.readback = reader.Value.Trim();
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("READBACK=<" + record.readback + ">");
#endif
                            }
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "feedback")
                        {
                            if (reader.IsEmptyElement == false)
                            {
                                reader.Read();
                                record.feedback = reader.Value.Trim();
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("FEEDBACK=<" + record.feedback + ">");
#endif
                            }
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "command_subject")
                        {
                            if (reader.IsEmptyElement == false)
                            {
                                reader.Read();
                                record.command_subject = reader.Value.Trim();
#if DEBUG_REPLY
                                UnityEngine.Debug.Log("SUBJECT=<" + record.command_subject + ">");
#endif
                            }
                        }
                    }
                }
            }
        }
    }

    public void ParseXMLResponse(string data)
    {
        using (XmlReader reader = XmlReader.Create(new StringReader(data)))
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        //UnityEngine.Debug.Log("Element    : " + reader.Name);
                        switch (reader.Name)
                        {
                            case "context_setup_reply":
                                ParseContextSetupReply(reader);
                                break;
                            case "process_utterance_reply":
                                ParseUtteranceReply(reader);
                                break;
                        }
                        break;
                }
            }
        }
    }

    public delegate void UtteranceCallback( NluMgr.match_record record );
    UtteranceCallback utteranceCallback;

    public delegate void ErrorCallback(string error);
    ErrorCallback errorCallback;

    public void SetUtteranceCallback(UtteranceCallback callback)
    {
        this.utteranceCallback = callback;
    }

    public void SetErrorCallback(ErrorCallback callback)
    {
        this.errorCallback = callback;
    }

    public void Receive(string response)
    {
        Debug.Log("NluMgr : Receive : " + response);
    }

    Vector2 scroll;
	
	public List<StringMap> GoldenPathStrings;
	int gpIdx;
	
    public void GPErrorCallback(string data)
    {
		// save this one		
		GoldenPathStrings[gpIdx].value = data;
		
		// kick off next one
		if ( ++gpIdx < GoldenPathStrings.Count )
		{
        	NluMgr.GetInstance().Utterance(GoldenPathStrings[gpIdx].key, "nurse");
		}
		else
		{
			// we're done, save the file
			Serializer<List<StringMap>> serializer = new Serializer<List<StringMap>>();
			serializer.Save("GoldenPath.xml",GoldenPathStrings);
			// do the analysis
			AnalyzeGoldenPath(GoldenPathRecords);
		}
	}	
	
	public class ResultRecord
	{
		public string Input;
		public string Command;
		public string Result;
		public string Feedback;
		public string Avatar;
		public bool Ok;
#if RESULT_INCLUDE_RECORD
		public NluMgr.match_record Record;
#endif
	}
	
	public List<ResultRecord> GoldenPathResults;
	
	public void AnalyzeGoldenPath( List<NluMgr.match_record> list )
	{
		GoldenPathResults = new List<ResultRecord>();
		
		foreach( NluMgr.match_record record in list )
		{
			ResultRecord rr = new ResultRecord();
#if RESULT_INCLUDE_RECORD
			rr.Record = record;
#endif
			rr.Input = record.input;
			rr.Command = record.sim_command;
			
			// get sim command and subject
            // no subject, find someone to do the command
            List<ObjectInteraction> objects = ObjectInteractionMgr.GetInstance().GetEligibleObjects(record.sim_command);
			if ( objects.Count > 0 )
			{
				bool found = false;
				foreach( ObjectInteraction obj in objects )
				{
					if ( obj.Name == record.command_subject )
					{
						rr.Result = "command interaction found for subject '" + record.command_subject + "'";
						rr.Feedback = record.feedback;
						rr.Avatar = record.command_subject;
						rr.Ok = true;
						found = true;
					}
				}
				if ( found == false )
				{
					if ( record.command_subject == "" || record.command_subject == null )
						rr.Result = "command interaction found but no command_subject";
					else
						rr.Result = "command interaction found for subject '" + record.command_subject + "' not found";
					rr.Feedback = record.feedback;
					rr.Avatar = record.command_subject;
					rr.Ok = false;
				}
			}
			else
			{
				if ( record.sim_command.Contains("PLAYER") == true )
				{
					rr.Result = "command is for player";
					rr.Feedback = record.feedback;
					rr.Avatar = record.command_subject;
					rr.Ok = true;
				}
				else
				{
					rr.Result = "no avatar to play this command";
					rr.Feedback = record.feedback;
					rr.Avatar = record.command_subject;
					rr.Ok = false;
				}
			}
			
#if RESULT_ONLY_IF_BAD
			if ( rr.Ok == false && rr.Command != "BAD:COMMAND")
				GoldenPathResults.Add(rr);
#endif
		}
		// we're done, save the file
		Serializer<List<ResultRecord>> serializer = new Serializer<List<ResultRecord>>();
		serializer.Save("GoldenPathResults.xml",GoldenPathResults);
	}
	
	List<NluMgr.match_record> GoldenPathRecords; 
	
    public void GPCallback(NluMgr.match_record record)
    {
		// save the record
		GoldenPathRecords.Add(record);
		
        if (record == null)
        {
            UnityEngine.Debug.Log("NluPrompt.Callback() : record = null!");
        }

        // make to upper case
        record.sim_command = record.sim_command.ToUpper();

        string text = "NLU : sim_command=<" + record.sim_command + ">";
        // subject 
        if (record.command_subject != null && record.command_subject != "")
            text += " : s=<" + record.command_subject + ">";
        // params
        foreach (NluMgr.sim_command_param param in record.parameters)
            text += " : p=<" + param.name + "," + param.value + ">";
        // missing params
        foreach (NluMgr.missing_sim_command_param param in record.missing_parameters)
            text += " : m=<" + param.name + ">";
        // readback
        if (record.readback != null && record.readback != "")
            text += " : r=<" + record.readback + ">";
        // feedback
        if (record.feedback != null && record.feedback != "")
            text += " : f=<" + record.feedback + ">";
		
		// save this one
		GoldenPathStrings[gpIdx].value = text;
		
		// kick off next one
		if ( ++gpIdx < GoldenPathStrings.Count )
		{
        	NluMgr.GetInstance().Utterance(GoldenPathStrings[gpIdx].key, "nurse");
		}
		else
		{
			// we're done, save the file
			Serializer<List<StringMap>> serializer = new Serializer<List<StringMap>>();
			serializer.Save("GoldenPath.xml",GoldenPathStrings);
			// do the analysis
			AnalyzeGoldenPath(GoldenPathRecords);
		}
    }
	
	public void TestGoldenPath()
	{
		// make new path records list
		GoldenPathRecords = new List<NluMgr.match_record>();

		// load golden path strings
		Serializer<List<StringMap>> serializer = new Serializer<List<StringMap>>();
		GoldenPathStrings = serializer.Load("XML/LogReplay/GoldenPath");

		// kick off first string
		if ( GoldenPathStrings != null )
		{
	        NluMgr.GetInstance().SetUtteranceCallback(new NluMgr.UtteranceCallback(GPCallback));
	        NluMgr.GetInstance().SetErrorCallback(new NluMgr.ErrorCallback(GPErrorCallback));
        	NluMgr.GetInstance().Utterance(GoldenPathStrings[0].key, "nurse");
			gpIdx = 0;
		}
	}
	
    public void OnGUI()
    {
        if (debug == false)
            return;

        GUI.Box(new Rect(0,0,Screen.width,Screen.height),"");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SetupRequest"))
        {
            ContextSetupRequest("1.0", "trauma", null);
        }
        if (GUILayout.Button("Utterance"))
        {
            Utterance(TestUtterance,"nurse");
        }
        if (GUILayout.Button("Terminate"))
        {
            ContextTerminate("1.0", "trauma");
        }		
		if (GUILayout.Button("Test Golden Path"))
		{
			TestGoldenPath();
		}
		
        GUILayout.EndHorizontal();
        GUILayout.BeginArea(new Rect(0, 30, Screen.width, Screen.height - 30));
        GUILayout.BeginVertical();
        scroll = GUILayout.BeginScrollView(scroll, false, true);
        for (int i = replies.Count - 1; i >= 0; i--)
            GUILayout.Label(replies[i]);
        /*
        foreach (string reply in replies)
        {
            GUILayout.Label(reply);
        }
         * */
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

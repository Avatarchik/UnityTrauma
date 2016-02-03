//#define DEBUG_NLU_CMD
#define SHOW_READBACK
#define SHOW_FEEDBACK
#define SHOW_INPUT

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class NluPrompt : BaseObject
{
    public NluPrompt()
        : base()
    {
        instance = this;
    }

    public static NluPrompt instance;
    public static NluPrompt GetInstance()
    {
        return instance;
    }

    int toggle=0;

    public void Update()
    {
		/*
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            DialogLoader dl = DialogLoader.GetInstance();
            dl.LoadXML("dialog.editbox.template");
            GUIScreen dp = dl.FindScreen("dialogGenericEditBox");
            dp.CenterX();
            dp.CenterY();
            dp.SetLabelText("titleBarText", "Enter Utterance");
            GUIButton button = dp.Find("buttonConfirm") as GUIButton;
            if (button != null)
            {
                button.AddCallback(OkCallback);
            }
            dl.Show("dialogGeneric",true);
        }
        if (Input.GetKeyUp(KeyCode.F8))
        {
            QuickInfoMsg imsg = new QuickInfoMsg();
            imsg.command = DialogMsg.Cmd.open;
            if (toggle == 1)
            {
                imsg.title = "RAW NLU OUTPUT" + ", Hit F8 again for Cmd Debug";
                imsg.text = NluMgr.GetInstance().RawWWWString;
            }
            else
            {
                imsg.title = response_title + ", Hit F8 again for Raw Output";
                imsg.text = response;
            }
            toggle = 1 - toggle;

            imsg.h = Screen.height-50;
            imsg.timeout = 0.0f;
            QuickInfoDialog.GetInstance().PutMessage(imsg);
        }
 		*/
	}
	
	public override void Start ()
	{
		base.Start ();
		// set callbacks
        NluMgr.GetInstance().SetUtteranceCallback(new NluMgr.UtteranceCallback(Callback));
        NluMgr.GetInstance().SetErrorCallback(new NluMgr.ErrorCallback(ErrorCallback));
	}

    int cnt = 0;
    string[] commands = { "PATIENT:SIT", "PATIENT:LAY:FLAT", "PATIENT:ROLL:LEFT", "PATIENT:ROLL:RIGHT", "IV:FLUIDTT:START", "IV:FLUIDTT:STOP" };

    public string Error;

    public bool ExecuteCommand(string command)
    {
		return Dispatcher.GetInstance().ExecuteCommand(command);
/*
        // no subject, find someone to do the command
        List<ObjectInteraction> objects = ObjectInteractionMgr.GetInstance().GetEligibleObjects(command);
        foreach (ObjectInteraction obj in objects)
        {
			
        	Error += "<EX(1):command executed by " + obj.Name + ">";
            Debug("NLUPrompt.ExecuteCommand(" + command + ") : Executed=" + Error);

            InteractionMap map = InteractionMgr.GetInstance().Get(command);
            obj.PutMessage(new InteractMsg(obj.gameObject, map));
            return true;
        }
        Error += "<EX(1):can't find character to execute command>";
        Debug("NLUPrompt.ExecuteCommand(" + command + ") : Error=" + Error);
        return false;
*/
    }

    public bool ExecuteCommand(string command, string avatar)
    {
        string status;

        if (avatar == "alexandra")
            avatar = "patient";

        BaseObject obj = ObjectManager.GetInstance().GetBaseObject(avatar);
        if (obj != null)
        {
            Character character = obj as Character;
            if (character != null)
            {
#if CHECK_ISDONE
                if (character.IsDone() == true)
                {
#endif
                    if (character.IsValidInteraction(command) == true)
                    {
                        Error += "<EX(2) : command ok>";
                        Debug("NLUPrompt.ExecuteCommand(" + command + "," + avatar + ") : Error=" + Error);
                        InteractionMap map = InteractionMgr.GetInstance().Get(command);
                        character.PutMessage(new InteractMsg(character.gameObject, map));
                        return true;
                    }
                    else
                    {
                        Error += "<EX(2) : not valid command for this avatar>";
                        Debug("NLUPrompt.ExecuteCommand(" + command + "," + avatar + ") : Error=" + Error);
                        return ExecuteCommand(command);
                    }
#if CHECK_ISDONE
                }
                else
                {
                    Error += "<EX(2) : avatar busy>";
                    Debug("NLUPrompt.ExecuteCommand(" + command + "," + avatar + ") : Error=" + Error);
                    return ExecuteCommand(command);
                }
#endif
            }
            else
            {
                Error += "<EX(2) : avatar is not character>";
                Debug("NLUPrompt.ExecuteCommand(" + command + "," + avatar + ") : Error=" + Error);
                return ExecuteCommand(command);
            }
        }
        else
        {
            // can't find character, try to execute
            Error += "<EX(2) : can't find avatar " + avatar + ">";
            Debug("NLUPrompt.ExecuteCommand(" + command + "," + avatar + ") : Error=" + Error);
            return ExecuteCommand(command);
        }
    }

    string response;
    string response_title;
	
	public void OrderFluids(NluMgr.match_record record)
	{
		int bloodDrip=0;
		int bloodPressure=0;
		int bloodRapid=0;
		
		int salineDrip=0;
		int salinePressure=0;
		int salineRapid=0;
		
		// figure out what to do based on the record
		if ( record.sim_command.Contains("IV:BLOOD") )
		{			
			int units=0;
			// get params
			string param = record.GetParameter("bloodvolume");
			if ( param != null )
				units = Convert.ToInt32(param);
			// set to bloodDrip for now
			bloodDrip = units;				
		}
		else if ( record.sim_command.Contains("IV:CRYSTAL") )
		{
			int units=0;
			// get params
			string param = record.GetParameter("volume");
			if ( param != null )
				units = Convert.ToInt32(param);
			// set to bloodDrip for now
			salineDrip = units;				
		}
		
		// do the order
		Patient patient = ObjectManager.GetInstance().GetBaseObject("Patient") as Patient;
		if ( patient != null )
		{
			patient.OrderFluids("Dispatcher",bloodDrip,bloodPressure,bloodRapid,salineDrip,salinePressure,salineRapid);
		}
	}

    public void Callback(NluMgr.match_record record)
    {
        if (record == null)
        {
            UnityEngine.Debug.Log("NluPrompt.Callback() : record = null!");
        }

        // make to upper case
        record.sim_command = record.sim_command.ToUpper();

        string text = "<NLU Command> <" + record.sim_command + ">";
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

        Error = "";
                
		InfoDialogMsg idm;
		
#if DEBUG_NLU_CMD
		idm = new InfoDialogMsg();
        idm.command = DialogMsg.Cmd.open;
		idm.text = text;
		InfoDialogLoader.GetInstance().PutMessage(idm);
#endif
#if SHOW_INPUT
		if ( record.input != null )
		{
			idm = new InfoDialogMsg();
        	idm.command = DialogMsg.Cmd.open;
			idm.text = "<NLU input> " + record.input;
			InfoDialogLoader.GetInstance().PutMessage(idm);
		}
#endif
#if SHOW_READBACK
		if ( record.readback != null && record.readback != "null")
		{
			idm = new InfoDialogMsg();
        	idm.command = DialogMsg.Cmd.open;
			idm.text = "<NLU readback> " + record.readback;
			InfoDialogLoader.GetInstance().PutMessage(idm);
		}
#endif		
#if SHOW_FEEDBACK
		if ( record.feedback != null && record.feedback != "null")
		{
			// send to log
			idm = new InfoDialogMsg();
        	idm.command = DialogMsg.Cmd.open;
			idm.text = "<NLU feedback> " + record.feedback;
			InfoDialogLoader.GetInstance().PutMessage(idm);
			
			// pop up dialog
			QuickInfoMsg imsg = new QuickInfoMsg();
        	imsg.title = "NLU Feedback";
        	imsg.command = DialogMsg.Cmd.open;
        	imsg.text = record.feedback;
        	imsg.h = 200;
        	imsg.timeout = 4.0f;
        	QuickInfoDialog.GetInstance().PutMessage(imsg);
		}
#endif

		// I really hate doing this but for now it is the best way to handle our only command with parameters
		if (record.sim_command.Contains("IV:BLOOD") || record.sim_command.Contains("IV:CRYSTAL"))
		{
			OrderFluids(record);
		} 
		else if (record.sim_command == "BAD:COMMAND")
        {
            Brain.GetInstance().PlayAudio("BAD:COMMAND:" + (int)UnityEngine.Random.Range(1, 4));
        } 
        else if (record.command_subject != null)
        {
            // has command subject
            if (ExecuteCommand(record.sim_command, record.command_subject.ToLower()) == false)
            {
                Debug("ExecuteCommand(CMD:" + record.sim_command + ",SUBJECT:" + record.command_subject + ") Failed : debug=" + text + " : Error=" + Error);
            }
        }
        else
        {
            // no command subject, find someone to do this...
            if (ExecuteCommand(record.sim_command) == false)
            {
                Debug("ExecuteCommand(CMD:" + record.sim_command + ", No SUBJECT) Failed : debug=" + text + " : Error=" + Error);
            }
        }

        // NLU response
        if ((record.feedback != null && record.feedback != "") || (record.readback != null && record.readback != ""))
        {
            // put up quickinfo with feedback
            QuickInfoMsg msg = new QuickInfoMsg();
            if (record.feedback != null && record.readback != null)
            {
                response_title = msg.title = "READBACK/FEEDBACK";
                response = msg.text = "SIM_COMMAND <" + record.sim_command + "> : CMD_SUBJECT <" + record.command_subject + "> : READBACK <" + record.readback + "> : FEEDBACK <" + record.feedback + "> : ERROR <" + Error + ">";
            }
            else if (record.feedback != null)
            {
                response_title = msg.title = "FEEDBACK";
                response = msg.text = "SIM_COMMAND <" + record.sim_command + "> : CMD_SUBJECT <" + record.command_subject + "> : FEEDBACK <" + record.feedback + "> : ERROR <" + Error + ">";
            }
            else if (record.readback != null)
            {
                response_title = msg.title = "READBACK";
                response = msg.text = "SIM_COMMAND <" + record.sim_command + "> : CMD_SUBJECT <" + record.command_subject + "> : READBACK <" + record.readback + "> : ERROR <" + Error + ">";
            }
            msg.w = 600;
            msg.h = 200;
            msg.timeout = 0.0f;
            //QuickInfoDialog.GetInstance().PutMessage(msg);
        }

        //InfoDialogLoader idl = InfoDialogLoader.GetInstance();
        //if (idl != null)
        //{
        //    InfoDialogMsg msg = new InfoDialogMsg();
        //    msg.command = DialogMsg.Cmd.open;
        //    msg.text = response;
        //    idl.PutMessage(msg);
        //}

#if DEBUG_CMD_STRINGS
        if (++cnt > 3)
            cnt = 0;
        data = commands[cnt];
#endif
    }

    public void Debug(string text)
    {
        UnityEngine.Debug.Log(text);

#if DEBUG_NLU_CMD
        QuickInfoMsg imsg = new QuickInfoMsg();
        imsg.title = "NLU Status, hit F8 for more NLU Debug";
        imsg.command = DialogMsg.Cmd.open;
        imsg.text = text;
        imsg.h = 300;
        imsg.timeout = 0.0f;
        QuickInfoDialog.GetInstance().PutMessage(imsg);
#endif
    }

    public void ErrorCallback(string data)
    {
        Brain.GetInstance().PlayAudio("BAD:COMMAND:" + (int)UnityEngine.Random.Range(1,4));

        UnityEngine.Debug.Log("NluPrompt.ErrorCallback=" + data);

#if DEBUG_NLU_CMD
        // put up quickinfo with feedback
        QuickInfoMsg msg = new QuickInfoMsg();
        msg.text = "NLU : error=<" + data + ">";
        msg.title = "NLU ERROR";
        msg.h = 300;
        msg.timeout = 0.0f;
        QuickInfoDialog.GetInstance().PutMessage(msg);
#endif

#if DEBUG_CMD_STRINGS
        if (++cnt > 3)
            cnt = 0;
        data = commands[cnt];
#endif
    }

    public void OkCallback( GUIScreen screen, GUIObject guiobj, string args )
    {
        // find edit box
        string utterance;
        GUIEditbox editbox = screen.Find("contentText") as GUIEditbox;
        if (editbox != null)
        {
            utterance = editbox.text;
            UnityEngine.Debug.Log("NluPrompt().OkCallback() : Text=" + utterance);
        }
        else
            return;

        // close the dialog
        GUIManager.GetInstance().Remove(screen.Parent);

        // send txt to Nlu
        UnityEngine.Debug.Log("Say <" + utterance + ">");

        // send to Nlu
        NluMgr.GetInstance().Utterance(utterance, "nurse");

        // send to brain
        //SpeechProcessor.GetInstance().SpeechToText(utterance);
    }

    public void SpeechToText(string command)
    {
        // send txt to Nlu
        UnityEngine.Debug.Log("Say <" + command + ">");
		
#if DEBUG_NLU_CMD
		InfoDialogMsg idm = new InfoDialogMsg();
       	idm.command = DialogMsg.Cmd.open;
		idm.text = "Say <" + command + ">";
		InfoDialogLoader.GetInstance().PutMessage(idm);
#endif		

        // send to Nlu
        NluMgr.GetInstance().Utterance(command, "nurse");
    }
}

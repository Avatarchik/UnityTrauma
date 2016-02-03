using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SpeechRecord
{
    public SpeechRecord()  
    {
        Expressions = new List<string>();
    }

    public string Command;
    public List<string> Expressions;
}

public class SpeechTable
{
    public SpeechTable()
    {
        Records = new List<SpeechRecord>();
    }
    public List<SpeechRecord> Records;
}

public class SpeechProcessor
{
    public SpeechTable TranslationTable;

    static SpeechProcessor instance;
    static public SpeechProcessor GetInstance()
    {
        if (instance == null)
            instance = new SpeechProcessor();

        return instance;
    }

    public SpeechProcessor()
    {
    }

    public void Load()
    {
        LoadXML("XML/speech");
    }

    public void LoadXML(string filename)
    {
        Serializer<SpeechTable> serializer = new Serializer<SpeechTable>();
        TranslationTable = serializer.Load(filename);
    }

    public string Translate(string command)
    {
        if (TranslationTable == null)
        {
            Debug.Log("SpeechProcessor.Translate() : TranslationTable=null");
            return command;
        }

        // go through all the strings in the speech table looking for a match
        foreach (SpeechRecord record in TranslationTable.Records)
        {
            foreach (string item in record.Expressions)
            {
                Debug.Log("Checking Expression <" + item + ">");
                if (CheckUtterance(command, item) > 0.5f)
                {
                    Debug.Log("Found Expression <" + record.Command + ">");
                    return record.Command;
                }
            }
        }

        return command;
    }

    public void SpeechToText(string command)
    {
        // move to lower case
        command = command.ToLower();

        // translate command to game world
        command = Translate(command);

        // put info qi
        //QuickInfoMsg qimsg = new QuickInfoMsg();
        //qimsg.command = DialogMsg.Cmd.open;
        //qimsg.title = "Speech Command";
        //qimsg.text = "Cmd=<" + command + "> ...";
        //QuickInfoDialog.GetInstance().PutMessage(qimsg);

        // check dialog manager
        DialogMgr.GetInstance().SpeechToText(command);

        // run it through the dialog
        if (DialogueTree.Instance.ActiveDialogue != null)
            DialogueTree.Instance.ActiveDialogue.SpeechToText(command);

        // send to everyone
        SpeechMsg speechmsg = new SpeechMsg(command);
        ObjectManager.GetInstance().PutMessage(speechmsg);

        // find closest command by interrigating all the objects
        if (speechmsg.Stats.Count > 0)
        {
            SpeechMsg.Info info = null;
            foreach (SpeechMsg.Info tmp in speechmsg.Stats)
            {
                if (info == null)
                    // first one
                    info = tmp;
                else
                {
                    // check for higher percentage
                    if (tmp.percent > info.percent)
                        info = tmp;
                }
            }
            if (info != null)
            {
                // send this command
                InteractMsg interact = new InteractMsg(info.obj.gameObject, info.map, true);
                info.obj.PutMessage(interact);

                // close the interact menu....just in case
                InteractDialogMsg msg = new InteractDialogMsg();
                msg.command = DialogMsg.Cmd.close;
                InteractDialogLoader.GetInstance().PutMessage(msg);
            }
        }
    }

    public float CheckUtterance(string command, string test)
    {
        // first break into words
        string[] words = command.Split(' ');

        if (words.Length == 0)
            return 0.0f;

        float count = 0.0f;
        foreach (string word in words)
        {
            if (test.Contains(word))
                count++;
        }

        float result = count / (float)words.Length;

        return result;
    }
}

public class SpeechMsg : GameMsg
{
    public string Utterance;
    public SpeechMsg(string utterance)
        : base() 
    {
        Utterance = utterance;
        Stats = new List<Info>();
    }
    public class Info
    {
        public Info(BaseObject obj, InteractionMap map, float percent)
        {
            this.obj = obj;
            this.map = map;
            this.percent = percent;
        }
        public BaseObject obj;
        public InteractionMap map;
        public float percent;
    }
    public List<Info> Stats;
}




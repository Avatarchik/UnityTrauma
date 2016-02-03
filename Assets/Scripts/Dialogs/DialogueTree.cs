using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DialogueTree : MonoBehaviour
{
    public enum DialogueLanguage { English, Spanish, French, YoMama };
    [Serializable]
    public class Dialogue
    {
        public class DialogueOption
        {
            public string text;    // Text to display
            public int goToID;     // Next dialogue to move to
            public bool correctAnswer;  // Is this the correct answer for the scenario conversation

            // Clear on visited control booleans
            public bool visited;
            public bool clearOnVisited;

            // Interact message trigger
            public string interactMsg;

            public string audio;

            public DialogueOption() { }
        }
        public int id;         // ID is this dialogue
        public string name;     // Name to use to find dialogue
        public string title;    // GUI Title 
        public string text;    // Text to display
        public int image;    // Character image to display
        public string audio;

        public DialogueOption dialogueOption1;   // Selectable dialogue options
        public DialogueOption dialogueOption2;   // Selectable dialogue options
        public DialogueOption dialogueOption3;   // Selectable dialogue options
        public DialogueOption dialogueOption4;   // Selectable dialogue options

        public Dialogue() { }
    }

    private static DialogueTree instance;
    DialogueDialog activeDialogueDialog;

    List<Dialogue> dialogues;
    Dialogue activeDialogue;

    public DialogueLanguage language;
    public string filePathName;
    public List<Texture2D> dialogueProfiles = new List<Texture2D>();
    public GUISkin skin;

    private DialogueTree() {}

    void Awake() { if (instance == null) instance = this; }

    public static DialogueTree Instance
    {
        get{ return instance; }
    }

    public static DialogueTree GetInstance()
    {
        return instance;
    }

    public DialogueDialog ActiveDialogue { set { activeDialogueDialog = value; } get { return activeDialogueDialog; } }

    void Start()
    {
        string fileName = filePathName;
        switch (language)
        {
            case DialogueLanguage.English:
                fileName += "-en";
                break;
            case DialogueLanguage.French:
                fileName += "-fr";
                break;
            case DialogueLanguage.Spanish:
                fileName += "-sp";
                break;
            default:
                fileName += "-en";
                break;
        };
        LoadDialogue(fileName);

        DialogueDialog temp;
        temp = gameObject.GetComponent<DialogueDialog>();
        if(temp == null)
            temp = gameObject.AddComponent(typeof(DialogueDialog)) as DialogueDialog;
        temp.skin = skin;
		//GoToDialogue(0, true);
    }

    private bool LoadDialogue(string fileName)
    {
        Serializer<List<Dialogue>> serializer = new Serializer<List<Dialogue>>();
        dialogues = serializer.Load(fileName);
       
        return dialogues != null;
    }

    public void GoToDialogue(int goToID, bool forceVisible)
    {
        // Display dialogue
        if (dialogues == null)
            return;

        List<Dialogue>.Enumerator iter = dialogues.GetEnumerator();
        while (iter.MoveNext())
        {
            if (iter.Current.id == goToID)
            {
                LoadToDialog(iter.Current, forceVisible);
                break;
            }
        }
    }

    public bool GoToDialogue(string name, bool forceVisible)
    {
        // Display dialogue
        if (dialogues == null)
            return false;

        List<Dialogue>.Enumerator iter = dialogues.GetEnumerator();
        while (iter.MoveNext())
        {
            if (iter.Current.name != null && iter.Current.name == name)
            {
                LoadToDialog(iter.Current, forceVisible);
                return true;
            }
        }

        Debug.Log("DialogTree.GotoDialogue: Can't find <" + name + ">");
        return false;
    }

    private void LoadToDialog(Dialogue dialogue, bool forceVisible)
    {
        activeDialogue = dialogue;
        // Set data to the dialog
        activeDialogueDialog.displayDialogue = dialogue.text;
        activeDialogueDialog.dialogueTitle = dialogue.title;
        if(activeDialogueDialog.options == null)
            activeDialogueDialog.options = new Dialogue.DialogueOption[4];
        activeDialogueDialog.options[0] = dialogue.dialogueOption1;
        activeDialogueDialog.options[1] = dialogue.dialogueOption2;
        activeDialogueDialog.options[2] = dialogue.dialogueOption3;
        activeDialogueDialog.options[3] = dialogue.dialogueOption4;
        activeDialogueDialog.image = dialogue.image;

        if (dialogue.audio != null)
        {
            Brain.GetInstance().PlayVocals(dialogue.audio);
        }

        // Check if no dialogue from NPC and if all valid options have been selected leaving only a "Next" option
        bool check = false;
        if (activeDialogueDialog.displayDialogue.Length == 0)
        {
            foreach (Dialogue.DialogueOption option in activeDialogueDialog.options)
            {
                if (option != null)
                {
                    if ((!option.clearOnVisited || !option.visited) && option.text != "Next")
                    {
                        check = true;
                        break;
                    }
                }
            }
        }
        else
            check = true;
        // If no valid option found, go to "Next" dialogue
        if (!check)
        {
            foreach (Dialogue.DialogueOption option in activeDialogueDialog.options)
            {
                if (option != null && option.text == "Next")
                {
                    GoToDialogue(option.goToID, forceVisible);
                    return;
                }
            }
            // If this failed, the dialogue doesn't have a Next
            EndActiveDialogue();
            return;
        }

        if (forceVisible)
        {
            InfoDialogMsg infomsg1 = new InfoDialogMsg();
            infomsg1.command = DialogMsg.Cmd.close;
            InfoDialogLoader.GetInstance().PutMessage(infomsg1);

            //activeDialogueDialog.SetModal(true);
            activeDialogueDialog.SetVisible(true);
        }
    }

    public void ReportDialogue(string title, int option, bool result)
    {
        DialogueMsg msg = new DialogueMsg();
        msg.title = title;
        msg.option = option;
        msg.result = result;

        // Send message somewhere
        Brain.GetInstance().PutMessage(msg);
    }

    public void EndActiveDialogue()
    {
        activeDialogueDialog.SetVisible(false);
        activeDialogue = null;

        //GameObject.Find("HUD").GetComponent<HUD>().canHint = true;
        //GameObject.Find("HUD").GetComponent<HUD>().canNavigate = true;
    }

    public Texture2D GetProfileImage(int id)
    {
        if (id >= 0 && id < dialogueProfiles.Count)
            return dialogueProfiles[id];
        return null;
    }
}

public class DialogueDialog : Dialog
{
    public string displayDialogue;
    public string dialogueTitle;
    public DialogueTree.Dialogue.DialogueOption[] options;
    public int image;
    public GUISkin skin;
    private Vector2 scrollPositionDialogue;
    private Vector2 scrollPositionOptions;

    public DialogueDialog() : base()
    {
        DialogueTree.Instance.ActiveDialogue = this;
        w = Screen.width;
        h = Screen.height;
        x = 0;
        y = 0;
        exit = false;
        scrollPositionDialogue = Vector2.zero;
        scrollPositionOptions = Vector2.zero;
        dialogueTitle = "";
        modal = true;
        Name = "Dialogue Dialog";
    }

    public override void OnOpen()
    {
        Brain.GetInstance().PlayAudio("DIALOGTREE:OPEN");
        SetModal(true);
    }
    public override void OnClose()
    {
        base.OnClose();
        SetModal(false);
    }

    public override void OnGUI()
    {
        if(!IsVisible())
            return;

        Texture2D texture;
        base.OnGUI();

        float x = Screen.width * 0.5f - 300;
        float y = Screen.height * 0.5f - 190;
        float width = 600;
        float height = 420;

        // Render background
        GUILayout.BeginArea(new Rect(x, y, width, height), skin.box);
        {
            // Render title
            GUILayout.Label(dialogueTitle, skin.customStyles[1], GUILayout.MaxWidth(width));

            // Render NPC dialogue
            GUILayout.BeginHorizontal();
            {
                // Render NPC picture
                GUILayout.BeginVertical();
                {
                    texture = DialogueTree.Instance.GetProfileImage(image);
                    if (texture != null)
                        GUILayout.Label(texture, skin.label);
                }
                GUILayout.EndVertical();

                // Render text
                float textWidth = width - (texture.width + skin.label.border.top + skin.label.border.bottom + skin.scrollView.border.left + skin.scrollView.border.right);
                float textHeight = skin.label.border.top + skin.label.border.bottom + texture.height;
                scrollPositionDialogue = GUILayout.BeginScrollView(scrollPositionDialogue, false, false, skin.horizontalScrollbar, skin.verticalScrollbar, skin.label, GUILayout.MaxWidth(textWidth), GUILayout.MaxHeight(textHeight));
                {
                    // Render dialogue
                    //GUILayout.Space(10);
                    GUILayout.Label(displayDialogue, skin.customStyles[0]);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            // Render PC choices
            GUILayout.BeginVertical();
            {
                scrollPositionOptions = GUILayout.BeginScrollView(scrollPositionOptions, false, false, skin.horizontalScrollbar, skin.verticalScrollbar, skin.label);
                {
                    // Render option buttons
                    for (int i = 0; i < options.Length; i++)
                    {
                        if (options[i] != null && options[i].text.Length > 0 && !(options[i].visited && options[i].clearOnVisited))
                        {
                            // Render option 1
                            if (GUILayout.Button(options[i].text, skin.button) || CommandOption == i )
                            {
                                // reset command option
                                CommandOption = -1;

                                Brain.GetInstance().StopVocals();
                                // play special audio or click if nothing here
                                if (options[i].audio != null)
                                    Brain.GetInstance().PlayAudio(options[i].audio);
                                else
                                    Brain.GetInstance().PlayAudio("GENERIC:CLICK");

                                options[i].visited = true;

                                // Log dialogue result.
                                if(!(displayDialogue.Length == 0 && options[i].text == "Next"))
                                    LogMgr.GetInstance().Add(new DialogueLogItem(Time.time, dialogueTitle, options[i].text, options[i].correctAnswer));

                                // Report titled dialogues to brain
                                if (title.Length > 0)
                                    DialogueTree.Instance.ReportDialogue(title, i, options[i].correctAnswer);

                                // Report InteractMsg
                                if (options[i].interactMsg != null && options[i].interactMsg.Length > 0)
                                {
                                    InteractMsg iMsg = new InteractMsg(this.gameObject, options[i].interactMsg);
                                    // log it
                                    InteractLogItem logitem = new InteractLogItem(Time.time, this.name, iMsg.map.item, StringMgr.GetInstance().Get(iMsg.map.response),iMsg);
                                    if (iMsg.log == true)
                                        LogMgr.GetInstance().Add(logitem);
                                    // send to brain
                                    Brain.GetInstance().PutMessage(iMsg);
                                }

                                if (options[i].goToID == -1)
                                {
                                    // End dialogue
                                    DialogueTree.Instance.EndActiveDialogue();
                                }
                                else if (options[i].goToID >= 0)
                                {
                                    // Move to next dialogue
                                    DialogueTree.Instance.GoToDialogue(options[i].goToID, false);
                                }
                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();

    }

    int CommandOption = -1;
    public void SpeechToText(string command)
    {
        if (command.Contains("option"))
        {
            if (command.Contains("1") || command.Contains("one") )
                CommandOption = 0;
            if (command.Contains("2") || command.Contains("two"))
                CommandOption = 1;
            if (command.Contains("3") || command.Contains("three"))
                CommandOption = 2;
            if (command.Contains("4") || command.Contains("four"))
                CommandOption = 3;
        }

        // check command
        CheckDialogOption(command);
    }

    public void CheckDialogOption(string command)
    {
        for (int i = 0; i < options.Length; i++)
        {
            float result = 0.0f;
            if (options[i] != null && options[i].text != null && (result = SpeechProcessor.GetInstance().CheckUtterance(command, options[i].text)) > .50f)
            {
                //QuickInfoMsg qimsg = new QuickInfoMsg();
                //qimsg.command = DialogMsg.Cmd.open;
                //qimsg.text = result + "%:" + options[i].text;
                //QuickInfoDialog.GetInstance().PutMessage(qimsg);

                CommandOption = i;

                return;
            }
        }
    }
}

public class DialogueMsg : GameMsg
{
    public string title;
    public bool result;
    public int option;

    public DialogueMsg() : base()
    {
        title = "";
        result = false;
        option = -1;
    }
}
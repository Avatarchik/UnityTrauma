using UnityEngine;
using System.Collections;

public class HUDGUI : MonoBehaviour 
{
	int width = 75;
	int height = 75;
	
	public GUISkin gSkin;
    public Texture2D blank;
    public bool canNavigate, canHint, confirmation, muted = false;
	GUIStyle exit, exitDisabled, help, helpDisabled, mute, muteDisabled, unmute, unmuteDisabled, 
			navigation, navigationDisabled, tooltip, tooltipText = new GUIStyle();
    float volume = 0f;
	
	bool hasSetup = false;
	
	void Setup() {
		hasSetup = true;		
		GUI.skin = gSkin;
		exit = GUI.skin.FindStyle("Exit");
		exitDisabled = GUI.skin.FindStyle("Exit Disabled");
		help = GUI.skin.FindStyle("Help");
		helpDisabled = GUI.skin.FindStyle("Help Disabled");
		mute = GUI.skin.FindStyle("Mute");
		muteDisabled = GUI.skin.FindStyle("Mute Disabled");
		unmute = GUI.skin.FindStyle("Unmute");
		unmuteDisabled = GUI.skin.FindStyle("Unmute Disabled");
		navigation = GUI.skin.FindStyle("Navigate");
		navigationDisabled = GUI.skin.FindStyle("Navigate Disabled");
		tooltip = GUI.skin.FindStyle("Tooltip");
		tooltipText = GUI.skin.FindStyle("Tooltip Text");
	}
	
    public void OnGUI()
    {
		if(!hasSetup)
			Setup();

        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
        GUILayout.BeginHorizontal();

        if (DialogMgr.GetInstance().IsModal() == false)
        {
            if (GUILayout.Button(new GUIContent(blank, "Click to quit"), exit))
            {
                //PopupMsg msg = new PopupMsg();
                //msg.okMsg = new StringMsg("main");
                //msg.cancelMsg = null;
                //msg.hasCancel = true;
                //msg.text = StringMgr.GetInstance().Get("HUD:MAIN:MSG");
                //Popup.GetInstance().PutMessage(msg);
            }

            if (canHint)
            {
                if (GUILayout.Button(new GUIContent(blank, "Click for a hint."), help))
                {
                    //StringMsg msg = new StringMsg();
                    //msg.message = "hint";
                    //Brain.GetInstance().PutMessage(msg);
                }
            }
            else
                GUILayout.Box("", help);

            if (!muted)
            {
                if (GUILayout.Button(new GUIContent(blank, "Click to mute."), mute))
                {
                    muted = !muted;
                    AudioSource audioSource = GameObject.Find("Main Camera").audio;
                    if (audioSource != null)
                    {
                        if (audioSource.volume == 0)
                            audioSource.volume = volume;
                        else
                            audioSource.volume = 0;
                    }
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent(blank, "Click to unmute."), unmute))
                {
                    muted = !muted;
                    AudioSource audioSource = GameObject.Find("Main Camera").audio;
                    if (audioSource != null)
                    {
                        if (audioSource.volume == 0)
                            audioSource.volume = volume;
                        else
                            audioSource.volume = 0;
                    }
                }
            }if (canNavigate)
            {
                if (GUILayout.Button(new GUIContent(blank, "Click to navigate."), navigation))
                {
                    //NavigationDialogueMsg navmsg1 = new NavigationDialogueMsg();
                    //navmsg1.command = DialogMsg.Cmd.open;
                    //navmsg1.modal = true;
                    //NavigationDialogue.GetInstance().PutMessage(navmsg1);
                }
            }
            else
                GUILayout.Box("", navigation);
        }
        else
        {
            GUILayout.Button("", exitDisabled);
            GUILayout.Button("", helpDisabled);
            if ( muted == true )
                GUILayout.Button("", muteDisabled);
            else
                GUILayout.Button("", unmuteDisabled);
            GUILayout.Button("", navigationDisabled);
        }
        
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        DrawTooltip(GUI.tooltip, Color.black);
    }

    public void DrawTooltip(string tooltip, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;

        GUILayout.BeginArea(new Rect(200, 0, 500, 50));
        GUILayout.Label(GUI.tooltip, tooltipText);
        GUILayout.EndArea();

        GUI.skin = gSkin;
        GUI.color = oldColor;
    }
}
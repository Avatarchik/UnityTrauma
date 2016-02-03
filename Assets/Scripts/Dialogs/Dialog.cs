using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Dialog : BaseObject
{
    public int x, y, w ,h;
    public int exit_x, exit_y, exit_w, exit_h;

    public int bar_h;
    public int gap_w, gap_h;

    public bool visible;
    public string title;
    public bool exit;
    public float time;
    public float timeout;

    public GUISkin gSkin;
    public GUISkin gExitButtonSkin;

    public int whiteSpaceX, whiteSpaceY;
    public bool modal;

    public Dialog() : base()
    {
        x = 0;
        y = 0;
        w = 0;
        h = 0;

        bar_h = 30;
        gap_w = 5;
        gap_h = 20;

        visible = false;
        title = "";
        exit = true;
        time = 0.0f;

        // offsets from left edge, top and sizes
        exit_x = 40;
        exit_y = 16;
        exit_w = 23;
        exit_h = 23;
    }

    protected void SetModal(bool yesno)
    {
        DialogMgr.GetInstance().SetModal(this, yesno);    
    }

    public virtual void OnOpen()
    {
        Brain.GetInstance().PlayAudio("GENERIC:OPEN");
    }

    public virtual void OnClose()
    {
        Brain.GetInstance().PlayAudio("GENERIC:CLOSE");
    }

    bool askChange = false;
    bool askVisible;

    public void UpdateVisibility()
    {
        // change visibility flag
        if (askChange == true)
        {
            askChange = false;
            visible = askVisible;
        }
    }

    override public void Update()
    {
        base.Update();

        if (time != 0.0f)
        {
            if (elapsedTime > timeout)
                SetVisible(false);
        }
        UpdateVisibility();
    }

    public virtual void SetVisible(bool yesno)
    {
        // handle changing visibility
        askVisible = yesno;
        askChange = true;

        // don't do anything if not active
        if (IsActive() == false)
          return;

        // if we're closing turn off modal
        if (yesno == false)
        {
            OnClose();
            SetModal(false);
        }
        else
            OnOpen();
    }

    public virtual bool IsVisible()
    {
        return visible;
    }

    public virtual bool IsActive()
    {
        // no manager, we're ok
        if (DialogMgr.GetInstance() == null)
        {
            return true;
        }

        if (DialogMgr.GetInstance().IsModal() == true)
        {
            // check to see if're we're modal or it is someone else
            if (DialogMgr.GetInstance().IsModal(this) == true)
            {
                //Debug.Log("Dialog.IsActive() : Dialog<" + this + "> Modal, is active = true");
                return true;
            }
            else
            {
                //Debug.Log("Dialog.IsActive() : Dialog<" + this + "> Modal, is active = false");
                return false;
            }
        }
        else
        {
            //Debug.Log("Dialog.IsActive() : Dialog<" + this + "> NO MODAL");
            return true;
        }
    }

    virtual public void PutMessage(GameMsg msg)
    {
        // check to see if we're active
        if (IsActive() == false)
            return;

        DialogMsg dialogmsg = msg as DialogMsg;
        if (dialogmsg != null)
        {
            //Debug.Log("Dialog: DialogMsg - " + dialogmsg.command);

            if (dialogmsg.command == DialogMsg.Cmd.open)
            {
                modal = dialogmsg.modal;
                SetModal(dialogmsg.modal);
                SetVisible(true);
            }
            if (dialogmsg.command == DialogMsg.Cmd.close)
            {
                SetVisible(false);
            }
            if (dialogmsg.command == DialogMsg.Cmd.position)
            {
                x = dialogmsg.x;
                y = dialogmsg.y;
            }
            if (dialogmsg.command == DialogMsg.Cmd.size)
            {
                w = dialogmsg.w;
                h = dialogmsg.h;
            }
            if (dialogmsg.title != "")
            {
                title = dialogmsg.title;
            }
            // handle time
            time = dialogmsg.time;
            if (dialogmsg.time != 0.0f)
            {
                timeout = elapsedTime + time;
            }
        }
    }

    virtual public Vector2 ExitButtonPosition()
    {
        Vector2 pos;

        pos.x = x + w - exit_x;
        pos.y = y + exit_y;

        return pos;
    }

    virtual public void DrawExitButton()
    {
        // exit button
        if (exit == true)
        {
            int button_w = exit_w;
            int button_h = exit_h;

            if (gExitButtonSkin)
            {
                GUI.skin = gExitButtonSkin;
                //button_w = gExitButtonSkin.button.normal.background.width;
                //button_h = gExitButtonSkin.button.normal.background.height;
            }

            Vector2 pos = ExitButtonPosition();

            if (GUI.Button(new Rect(x + w - exit_x, y + exit_y, exit_w, exit_h), ""))
            {
                Brain.GetInstance().PlayAudio("GENERIC:CLICK");
                SetVisible(false);
            }
            GUI.skin = gSkin;
        }
    }

    virtual public void DrawTitle()
    {
        GUI.Label(new Rect(x + 16, y + 22, w - 32, y - 44), title);
    }

    virtual public void DrawBox()
    {
        GUILayout.BeginArea(new Rect(x, y, w, h));
        GUILayout.Box("");
        GUILayout.EndArea();
    }

    virtual public void OnGUI()
    {
        if (modal)
        {
            GUI.depth = -1;
            // put full screen box under the whole screen
            GUI.Box(new Rect(0,0,Screen.currentResolution.width,Screen.currentResolution.height),"");
        }

        // set skin
        GUI.skin = gSkin;

        if (gSkin != null)
        {
            // draw box 
            DrawBox();

            // draw label
            DrawTitle();
        }

        // exit button
        DrawExitButton();

        /* FOR FIXED WIDTH IMAGE DIALOGS
        if (gSkin)
        {
            GUI.skin = gSkin;
            x = (Screen.width / 2) - ((GUI.skin.box.normal.background.width) / 2);
            y = (Screen.height / 2) - ((GUI.skin.box.normal.background.height) / 2);
            w = GUI.skin.box.normal.background.width;
            h = GUI.skin.box.normal.background.height;
            
            GUI.Box(new Rect(x, y, w, h), "");

            if (exit == true)
            {
                GUI.skin = gExitButtonSkin;
                if (GUI.Button(new Rect(x + GUI.skin.box.normal.background.width - 50, y + 18, 30, 20), ""))
                {
                    SetVisible(false);
                }
                GUI.skin = gSkin;
            }

            GUI.Label(new Rect(x + 22, y + 18, w, h), title);
        }
        else
        {
            // outline
            GUI.Box(new Rect(x, y, w, h), "");

            // title
            GUI.Label(new Rect(x + 12, y + 5, w, h), title);

            if (exit == true)
            {
                if (GUI.Button(new Rect(x + w - 40, y + 5, 30, 20), "X"))
                {
                    SetVisible(false);
                }
            }
        }
         * */
    }
}

public class DialogMgr
{
    Dialog Modal;

    DialogMgr() 
    {
        Modal = null;
    }

    static DialogMgr instance;
    static public DialogMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new DialogMgr();
        }
        return instance;
    }

    public void SetModal( Dialog dialog, bool yesno )
    {
        if (yesno == true)
        {
            // setting dialog modal, make sure nobody else is Modal
            if (Modal == null)
            {
                Modal = dialog;
            }
        }
        else
        {
            // clearing, make sure we're only clearing ourselves
            if (Modal == dialog )
            {
                Modal = null;
            }
        }
    }

    public bool IsModal()
    {
        if (Modal != null)
            return true;
        else
            return false;
    }

    public bool IsModal(Dialog dialog)
    {
        if (dialog == Modal)
            return true;
        else
            return false;
    }

    public void SpeechToText(string command)
    {
        if (command.Contains("dialogue") && command.Contains("close"))
        {
            if (Modal != null)
            {
                Modal.SetVisible(false);
            }
        }
    }
}


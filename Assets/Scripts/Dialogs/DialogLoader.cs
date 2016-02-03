using UnityEngine;
using System.Collections;

public class DialogLoader : MonoBehaviour
{
    public TextAsset xmlFile;
    public string screenName;

    public ScreenInfo ScreenInfo;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    static DialogLoader instance;
    public static DialogLoader GetInstance()
    {
        return instance;
    }

    public void LoadXML(string xmlfile)
    {
        ScreenInfo = GUIManager.GetInstance().LoadFromFile(xmlfile);
    }

    public void Show( string screenName )
    {
        DialogMsg msg = new DialogMsg();
        msg.command = DialogMsg.Cmd.open;
        Show(screenName, msg, false);
    }

    public void Show(string screenName, bool modal)
    {
        DialogMsg msg = new DialogMsg();
        msg.command = DialogMsg.Cmd.open;
        Show(screenName, msg, modal);
    }

    public void SetModal()
    {
        if ( ScreenInfo != null )
            GUIManager.GetInstance().SetModal(ScreenInfo);
    }

    public void Show(string screenName, DialogMsg msg, bool Modal)
    {
        if (ScreenInfo != null)
        {
            GUIScreen screen = ScreenInfo.FindScreen(screenName);
            if (screen != null && screen.GetType() == typeof(GUIDialog))
            {
                ScreenInfo.SetScreenTo(screen.name);
                (screen as GUIDialog).Load(msg);
                if ( Modal == true )
                    SetModal();
            }
        }
    }

    public GUIScreen FindScreen(string name)
    {
        if (ScreenInfo != null)
        {
            return ScreenInfo.FindScreen(name);
        }
        else
            return null;
    }

    public void LoadXML(DialogMsg msg, string entry)
    {
        GUIManager manager = GUIManager.GetInstance();
        GUIScreen screen;
        screenName = entry;
        if (manager != null && !manager.IsModal() && msg != null && msg.command == DialogMsg.Cmd.open)
        {
            //screen = manager.FindScreen(screenName);
            //if (screen != null && screen.GetType() == typeof(GUIDialog))
            //{
            //    (screen as GUIDialog).Load(msg);
            //    manager.SetModal(screen.Parent);
            //}
            //else
            //{
                // Screen wasn't found
            ScreenInfo si = manager.LoadFromFile(xmlFile.name);
            if (si != null)
            {
                screen = si.FindScreen(screenName);
                if (screen != null && screen.GetType() == typeof(GUIDialog))
                {
                    si.SetScreenTo(screen.name);
                    (screen as GUIDialog).Load(msg);
                    manager.SetModal(si);
                }
            }
            //}
        }
    }

    public void LoadXML(DialogMsg msg, int entry)
    {
        GUIManager manager = GUIManager.GetInstance();
        GUIScreen screen;
        if (manager != null && !manager.IsModal() && msg != null && msg.command == DialogMsg.Cmd.open)
        {
            ScreenInfo si = manager.LoadFromFile(xmlFile.name);
            if (si != null && entry < si.Screens.Count-1 && entry > 0)
            {
                si.SetScreenTo(entry);
                screen = si.Screen;
                screenName = screen.name;
                if (screen != null && screen.GetType() == typeof(GUIDialog))
                {
                    (screen as GUIDialog).Load(msg);
                    manager.SetModal(si);
                }
            }
        }
    }
}
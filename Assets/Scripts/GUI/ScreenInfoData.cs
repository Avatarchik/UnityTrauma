using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class ScreenInfoData : MonoBehaviour
{
    public ScreenInfo screenInfo;
    public TextAsset XMLFile;
}

//public class ScreenInfo : MonoBehaviour
//{
//    public List<GUIScreen> Screens;

//    protected bool windowed = false;
//    protected int activeScreen = 0;
//    public bool isModal = false;
//    public TextAsset XMLFile;

//    public GUIScreen Screen
//    {
//        get
//        {
//            if (activeScreen < Screens.Count)
//                return Screens[activeScreen];
//            return null;
//        }
//    }

//    public bool Windowed
//    {
//        get { return windowed; }
//        set { windowed = value; }
//    }

//    public void LastScreen()
//    {
//        if (activeScreen != 0)
//            activeScreen--;
//    }

//    public void NextScreen()
//    {
//        if (activeScreen < Screens.Count - 1)
//            activeScreen++;
//    }

//    public void SetScreenTo(int index)
//    {
//        if (index >= 0 && index < Screens.Count)
//            activeScreen = index;
//    }

//    public void SetScreenTo(string name)
//    {
//        for (int i = 0; i < Screens.Count; i++)
//        {
//            if (Screens[i].name == name)
//            {
//                SetScreenTo(i);
//                break;
//            }
//        }
//    }

//    public void Initialize()
//    {
//        List<GUIScreen> morph = new List<GUIScreen>();
//        foreach (GUIScreen screen in Screens)
//        {
//            if (screen.type != null && screen.type.Length > 0)
//            {
//                System.Type screenType = System.Type.GetType(screen.type);
//                if (screenType != null)
//                {
//                    GUIScreen newScreen = System.Activator.CreateInstance(screenType) as GUIScreen;
//                    newScreen.CopyFrom(screen);
//                    morph.Add(newScreen);
//                }
//                else
//                {
//                    Debug.LogWarning("GUIScreen -" + screen.name + "- Type not found. Defaulting to GUIScreen type.");
//                    morph.Add(screen);
//                }
//            }
//            else
//                morph.Add(screen);
//        }

//        Screens = morph;

//        foreach (GUIScreen screen in Screens)
//            screen.Initialize(this);
//    }

//    public void Execute()
//    {
//        Screen.Windowed = windowed;
//        Screen.Execute();
//    }

//    public static ScreenInfo Load(string fileName)
//    {
//        Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
//        ScreenInfo screen = serializer.Load(fileName);

//        return screen;
//    }

//    public void SetModal()
//    {
//        GUIManager manager = GUIManager.GetInstance();
//        if (manager != null)
//            manager.SetModal(this);
//    }

//    public GUIScreen FindScreen(string name)
//    {
//        foreach (GUIScreen screen in Screens)
//        {
//            if (screen.name == name)
//                return screen;
//        }
//        return null;
//    }

//    public GUIScreen FindScreenByType<T>()
//    {
//        foreach (GUIScreen screen in Screens)
//        {
//            if (screen.GetType() == typeof(T))
//                return screen;
//        }
//        return null;
//    }

//    public GUIScreen FindScreenByType(System.Type type)
//    {
//        foreach (GUIScreen screen in Screens)
//        {
//            if (screen.GetType() == type)
//                return screen;
//        }
//        return null;
//    }

//    public void OnClose()
//    {
//        foreach (GUIScreen screen in Screens)
//        {
//            screen.OnClose();
//        }
//    }

//    public void AddScreen(GUIScreen screen)
//    {
//        Screens.Add(screen);
//    }

//    public void RemoveScreen(GUIScreen screen)
//    {
//        Screens.Remove(screen);
//    }
//}
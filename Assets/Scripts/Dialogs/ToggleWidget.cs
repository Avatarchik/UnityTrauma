using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ToggleWidget
{
    List<string> options;
    int textW;
    int textH;
    int selected;
    int perRow;
    List<GUIStyle> gStyle;

    public ToggleWidget()
    {
        textW = 200;
        textH = 25;
        perRow = -1;
        options = new List<string>();
        gStyle = new List<GUIStyle>();
    }

    public void SetWidth(int w)
    {
        textW = w;
    }

    public void SetPerRow(int r)
    {
        perRow = r;
    }

    public void Add(string option)
    {
        options.Add(option);
    }

    public void SetSkin(params GUIStyle[] styles)
    {
        foreach (GUIStyle style in styles)
        {
            if (style != null)
            {
                Debug.LogWarning(style);
                gStyle.Add(style);
            }
        }
    }

    public void OnGUI()
    {
        int old = selected;
        if (selected < 0)
            selected = GUILayout.SelectionGrid(selected, options.ToArray(), perRow >= 0 ? perRow : options.Count, gStyle[0], GUILayout.Width(textW), GUILayout.Height(textH));
        else
            selected = GUILayout.SelectionGrid(selected, options.ToArray(), perRow >= 0 ? perRow : options.Count, gStyle[selected], GUILayout.Width(textW), GUILayout.Height(textH));
        if ( old != selected )
            Brain.GetInstance().PlayAudio("GENERIC:CLICK");
    }

    public int GetSelected()
    {
        return selected;
    }

    public void SetSelected(int value)
    {
        selected = value;
    }

    public bool GetSelectedBool()
    {
        return (selected==1) ? true : false;
    }
}

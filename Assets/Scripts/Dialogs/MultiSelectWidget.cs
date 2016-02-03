using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiSelectWidget
{
    public List<string> stringList;
    public int position;
    public int xpos, ypos, box;
    int length = 0;
    GUIStyle left, right, boxS;

    string noValueString;

    int buttonW;
    int buttonH;
    int textW;
    int textH;

    public MultiSelectWidget()
    {
        position = 0;

        // defaults
        buttonW = 25;
        buttonH = 25;
        textW = 148;
        textH = 25;
    }

    public void SetNoValue( string noval )
    {
        position = -1;
        this.noValueString = noval;
    }

    public void SetValues(string[] values)
    {
        stringList = new List<string>();
        for (int i=0 ; i<values.Length ; i++)
        {
            string str = values[i];
            stringList.Add(str);
            int tempLen = str.Length;
            if (tempLen > length)
                length = tempLen;
        }
    }

    public void SetSkins(GUIStyle leftStyle, GUIStyle rightStyle, GUIStyle boxStyle)
    {
        right = rightStyle;
        left = leftStyle;
        boxS = boxStyle;
    }

    public void SetValues(List<string> values)
    {
        stringList = new List<string>();
        foreach (string str in values)
        {
            stringList.Add(str);
            int tempLen = str.Length;
            if (tempLen > length)
                length = tempLen;
        }
    }

    public void SetValues(int start, int end, int increment)
    {
        stringList = new List<string>();
        for (int i = start; i <= end; i += increment)
        {
            stringList.Add(i.ToString());
        }
    }

    public void SetPosition(int position)
    {
        this.position = position;
        Mathf.Clamp(this.position, 0, stringList.Count - 1);
    }

    public int GetPosition()
    {
        return this.position;
    }

    public string GetString()
    {
        if (stringList != null)
            return stringList[position];
        return null;
    }

    public void SetSizes(int buttonW, int buttonH, int textW, int textH)
    {
        this.buttonW = buttonW;
        this.buttonH = buttonH;
        this.textW = textW;
        this.textH = textH;
    }

    public void OnGUI()
    {
        if ( (left != null && GUILayout.Button("", left, GUILayout.Width(buttonW), GUILayout.Height(buttonH))) || 
             (left == null && GUILayout.Button("",GUILayout.Width(buttonW), GUILayout.Height(buttonH))) )
        {
            Brain.GetInstance().PlayAudio("GENERIC:CLICK");

            if (--position < 0) 
                position = 0;
        }

        if (stringList != null)
        {
            if (boxS != null)
            {
                if (position == -1)
                    GUILayout.Box(noValueString, boxS, GUILayout.Width(textW), GUILayout.Height(textH));
                else
                    GUILayout.Box(stringList[position], boxS, GUILayout.Width(textW), GUILayout.Height(textH));
            }
            else
            {
                if (position == -1)
                    GUILayout.Box(noValueString, GUILayout.Width(textW), GUILayout.Height(textH));
                else
                    GUILayout.Box(stringList[position], GUILayout.Width(textW), GUILayout.Height(textH));
            }
        }

        if ( (right != null && GUILayout.Button("", right, GUILayout.Width(buttonW),GUILayout.Height(buttonH))) ||
             (right == null && GUILayout.Button("", GUILayout.Width(buttonW), GUILayout.Height(buttonH))) )
        {
            Brain.GetInstance().PlayAudio("GENERIC:CLICK");

            if (++position > stringList.Count - 1) 
                position = stringList.Count-1;
        }
    }
}
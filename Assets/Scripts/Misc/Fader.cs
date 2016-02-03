using UnityEngine;
using System.Collections;

public class Fader : MonoBehaviour
{
    public Texture2D fadeTexture;
    public float fadeSpeed = .3f;

    private int drawDepth = 1000;
    private float fadeDir = 0f;
    private float alpha = 1f;

    void OnGUI()
    {
        alpha += fadeDir * fadeSpeed * Time.deltaTime;
        alpha = Mathf.Clamp01(alpha);
 
        GUI.color = new Color(1, 1, 1, alpha);

        GUI.depth = drawDepth;

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
    }

    public void In()
    {
        fadeDir = -1;
    }

    public void Out()
    {
        fadeDir = 1;
    }

    public bool OutDone()
    {
        if (alpha == 1 && fadeDir == 1)
        {
            fadeDir = 0;
            return true;
        }
        else
            return false;
    }

    public void Reset()
    {
        alpha = 0f;
    }
}

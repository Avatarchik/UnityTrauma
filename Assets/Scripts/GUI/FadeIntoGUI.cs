using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GUITexture))]
public class FadeIntoGUI : MonoBehaviour 
{
    public TextAsset guiFile;
    public float transitionTime = 1f;
    public bool startImmediately = false;
    public bool fadeOut = true;
    public bool running = false;
    bool waiting = false;
    float timer = 0;
    GUITexture texture;
    float alpha = 0;
    public TextAsset[] loadOnFinish;

	// Use this for initialization
	void Start () 
    {
        texture = gameObject.GetComponent<GUITexture>();
        if (texture == null || texture.texture == null || guiFile == null || GUIManager.GetInstance() == null)
        {
            enabled = false;
            return;
        }

        alpha = texture.color.a;
        transform.position = Vector3.zero;
        transform.localScale = Vector3.zero;
        texture.pixelInset = new Rect(0, 0, Screen.width, Screen.height);
	}
	
    void Update()
    {
        if (startImmediately)
        {
            GUIManager guiMgr = GUIManager.GetInstance();
            guiMgr.SetModal(guiMgr.LoadFromFile(guiFile.name));
            waiting = true;
            startImmediately = false;
            return;
        }

        // Waiting for GUIManager IsModal to clear
        if (waiting)
        {
            GUIManager guiMgr = GUIManager.GetInstance();
            if (!guiMgr.IsModal())
            {
                waiting = false;
                FadeIn();
            }
        } 
        else if (running)
        {
            alpha += (Time.deltaTime / transitionTime) * (fadeOut ? 1 : -1);

            if (alpha < 0)
            {
                alpha = 0;
                running = false;
                fadeOut = !fadeOut;

                // Load other GUIs when this finishes
                if (loadOnFinish != null && loadOnFinish.Length > 0)
                {
                    GUIManager guiMgr = GUIManager.GetInstance();
                    foreach (TextAsset ta in loadOnFinish)
                    {
                        guiMgr.LoadFromFile(ta.name);
                    }
                }
            }
            else if (alpha > 1f)
            {
                alpha = 1f;
                running = false;
                fadeOut = !fadeOut;

                // Load GUI once the screen is completely covered
                GUIManager guiMgr = GUIManager.GetInstance();
                guiMgr.SetModal(guiMgr.LoadFromFile(guiFile.name));
                waiting = true;
            }
        }

        texture.color = new Color(texture.color.r, texture.color.g, texture.color.b, alpha);
    }

    public void FadeOut()
    {
        fadeOut = true;
        alpha = 0;
        running = true;
    }

    public void FadeIn()
    {
        fadeOut = false;
        alpha = 1;
        running = true;
    }
}

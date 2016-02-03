using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ViewportSwap : MonoBehaviour
{
    public virtual void Update()
    {
        Camera main = GameObject.Find("Main Camera").camera;
        Rect rect1 = main.rect;
        Camera vitals = GameObject.Find("Vitals Camera").camera;
        Rect rect2 = vitals.rect;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Swap();
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (main.depth > vitals.depth || main.depth == vitals.depth)
            {
                Debug.Log("#1 Mouse Postion = " + Input.mousePosition + " rect=" + vitals.rect);

                // check to see if we're within vitals
                if (main.pixelRect.Contains(Input.mousePosition))
                    Swap();
            }
            else
            {
                Debug.Log("#2 Mouse Postion = " + Input.mousePosition + " rect=" + main.rect);

                // check to see if we're within vitals
                if (vitals.pixelRect.Contains(Input.mousePosition))
                    Swap();
            }
        }    
    }

    public void Swap()
    {
        // swap cameras
        Camera main = GameObject.Find("Main Camera").camera;
        Rect rect1 = main.rect;
        Camera vitals = GameObject.Find("Vitals Camera").camera;
        Rect rect2 = vitals.rect;
        main.rect = rect2;
        vitals.rect = rect1;

        if (rect1.width > rect2.width)
        {
            main.depth = 1;
            vitals.depth = 0;
        }
        else
        {
            main.depth = 0;
            vitals.depth = 1;
        }
        //vitals.pixelRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        Debug.Log("main=" + rect1 + " vitals=" + rect2);
    }
}

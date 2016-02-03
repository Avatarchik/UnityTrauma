using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewMouseLook : MonoBehaviour
{

    public float intensity = 16.0f;

    public int[] bounds;

    public bool showBoundaries = false;

    public enum Status { Left, None, Right };
    public Status status = Status.None;

    public float leftRange = 180;
    public float rightRange = 180;
    public float origin = 180;
    public float current = 0;
    public float relative = 0;
    public float rotateX = 0;
	
	GUIStyle left, right = new GUIStyle();

    public bool freeCam = true;
	public GUISkin gSkin;

    void Start()
    {
        relative = 0;
    }

    public void SetRange(float range)
    {
        leftRange = range;
        rightRange = range;
        relative = 0.0f;
    }

    void Update()
    {
        if (DialogMgr.GetInstance().IsModal() == true)
            return;

        status = Status.None;
        if (bounds[0].ToString() != "")
        {
            int index = 0;
            int counter = (bounds.Length / 2);

            while (true)
            {
                if(bounds[index] != 0)
                    if (Input.mousePosition.x < ((Screen.width * bounds[index])/100) && 
                        (Input.mousePosition.y > (Screen.height * .4f) && (Input.mousePosition.y < Screen.height * .6f)))
                    {
                        if (Input.mousePosition.x < Screen.width)
                        {
                            float increment = (intensity * counter * Time.deltaTime);

                            // check for free camera movement
                            if (freeCam == false)
                            {
                                if (Mathf.Abs(relative - increment) < leftRange)
                                {
                                    relative -= increment;

                                    transform.rotation = Quaternion.Euler(rotateX, transform.rotation.eulerAngles.y - increment, 0);
                                    current = transform.rotation.eulerAngles.y;
                                    status = Status.Left;
                                    break;
                                }
                            }
                            else
                            {
                                relative -= increment;

                                transform.rotation = Quaternion.Euler(rotateX, transform.rotation.eulerAngles.y - increment, 0);
                                current = transform.rotation.eulerAngles.y;
                                status = Status.Left;
                                break;
                            }
                        }
                    }
                counter--;
                index++;
                if (index > ((bounds.Length / 2) - 1))
                    break;
            }

            index = bounds.Length-1;
            counter = (bounds.Length / 2);

            while (true)
            {
                if (bounds[index] != 0)
                    if (Input.mousePosition.x > (Screen.width - (Screen.width * bounds[index]) / 100) &&
                        (Input.mousePosition.y > (Screen.height * .4f) && (Input.mousePosition.y < Screen.height * .6f)))
                    {
                        if (Input.mousePosition.x < Screen.width )
                        {
                            float increment = (intensity * counter * Time.deltaTime);
                            if (freeCam == false)
                            {
                                if (Mathf.Abs(relative + increment) < rightRange)
                                {
                                    relative += increment;

                                    transform.rotation = Quaternion.Euler(rotateX, transform.rotation.eulerAngles.y + increment, 0);
                                    current = transform.rotation.eulerAngles.y;
                                    status = Status.Left;
                                    break;
                                }
                            }
                            else
                            {
                                relative += increment;

                                transform.rotation = Quaternion.Euler(rotateX, transform.rotation.eulerAngles.y + increment, 0);
                                current = transform.rotation.eulerAngles.y;
                                status = Status.Left;
                                break;
                            }
                        }
                    }
                counter--;
                index--;
                if (index < (bounds.Length / 2))
                    break;
            }
        }
    }
	
	bool hasSetup = false;
	
	void Setup() {
		hasSetup = true;
		GUI.skin = gSkin;
		
		right = GUI.skin.FindStyle("Left");
		left = GUI.skin.FindStyle("Right");
	}
	
    public void OnGUI()
    {
		int buttonSize = 50;
		
		if(!hasSetup)
			Setup();
		
        if (DialogMgr.GetInstance().IsModal() == true)
            return;

        if (showBoundaries)
        {
            if (bounds[0].ToString() != "")
            {
                for (int i = ((bounds.Length / 2) - 1); i >= 0; i--)
                {
                    if (bounds[i] != 0)
                    GUI.Box(new Rect(0, (Screen.height/2) - (buttonSize/2), ((Screen.width * bounds[i])/100), buttonSize), "");
                }
                for (int i = (bounds.Length / 2); i < bounds.Length; i++)
                {
                    if (bounds[i] != 0)
                        GUI.Box(new Rect((Screen.width - ((Screen.width * bounds[i]) / 100)), (Screen.height/2) - (buttonSize/2), 
                            Screen.width, buttonSize), "");
                }
            }
        }
		
		GUILayout.BeginArea(new Rect(0, (Screen.height/2) - (buttonSize/2), Screen.width, Screen.height/2));
		GUILayout.Button("", right);
		GUILayout.EndArea();
		
		GUILayout.BeginArea(new Rect(Screen.width - buttonSize, (Screen.height/2) - (buttonSize/2), Screen.width, Screen.height/2));
		GUILayout.Button("", left);
		GUILayout.EndArea();
    }
}
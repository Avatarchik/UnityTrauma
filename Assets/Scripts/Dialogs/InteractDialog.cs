using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class InteractDialogMsg : DialogMsg
{
    public List<InteractionMap> items;
    public ObjectInteraction baseobj;
	public string baseXML;

    public InteractDialogMsg()
        : base()
    {
        items = new List<InteractionMap>();
        baseobj = null;
    }

    public void LoadXML(string xmlname)
    {
        Serializer<ObjectInteractionInfo> serializer = new Serializer<ObjectInteractionInfo>();
        ObjectInteractionInfo info = serializer.Load(xmlname);
        if (info != null)
        {
            // copy items
            items.Clear();
            foreach (string item in info.DialogItems)
            {
                InteractionMap map = InteractionMgr.GetInstance().Get(item);
                if (map != null)
                    items.Add(map);
            }
            // set title
            this.title = info.DialogTitle;
        }
        else
            UnityEngine.Debug.Log("InteractionDialogMsg.LoadXML(" + xmlname + ") = null");
    }
}

public class InteractDialog : Dialog
{
    public List<InteractionMap> items;
    ObjectInteraction interactObject;

    static InteractDialog instance;

    public GUISkin tooltipSkin;
	
	public List<Vector2> positions = new List<Vector2>();
	List<String> stupidStrings = new List<String>();
	Vector3 position = new Vector2(Screen.width / 2, Screen.height / 2);
	List<InteractionMap> displayList = new List<InteractionMap>();
	String menuLocation;
	List<string> breadcrumbs = new List<string>();
	bool start = false;
	string baseXML;
	int page = 0;
	GUISkin defaultSkin;
	bool setupDefault = false;
	
	void Start() {
		base.Start();
				
		positions.Add(new Vector2(30, -50));
		positions.Add(new Vector2(30, 30));
		positions.Add(new Vector2(40, -10));
		positions.Add(new Vector2(-80, -50));
		positions.Add(new Vector2(-80, 30));
	}

    public InteractDialog()
        : base()
    {
        Name = "InteractDialog";

        title = "Interact";
        items = null;
        instance = this;
    }

    public static InteractDialog GetInstance()
    {
        return instance;
    }

    public override void OnOpen()
    {
        Brain.GetInstance().PlayAudio("INTERACT:OPEN");
    }

    override public void PutMessage(GameMsg msg)
    {
        Debug.Log("ObjectInteraction:PutMessage() : " + msg.ToString());

        if (IsActive() == false)
            return;

        InteractMsg imsg = msg as InteractMsg;
        if (imsg != null)
        {
            // default msg, create an interaction
            InteractionMgr.GetInstance().SendInteractionMap("", imsg.map);
        }

        InteractDialogMsg dialogmsg = msg as InteractDialogMsg;
        if (dialogmsg != null)
        {
            if (dialogmsg.command == DialogMsg.Cmd.open && dialogmsg.items != null)
            {
                // close info dialog
                InfoDialogMsg dmsg = new InfoDialogMsg();
                dmsg.command = DialogMsg.Cmd.close;
                InfoDialog.GetInstance().PutMessage(dmsg);

                //print(dialogmsg.items[0].response);
                // copy new items
                items = dialogmsg.items;

                // set game object
                interactObject = dialogmsg.baseobj;

#if BRAIN_DEBUG
                if (interactObject != null)
                    Debug.Log("InteractDialog : interactObject = " + interactObject.name);
                else
                    Debug.Log("InteractDialog : no object");
#endif
				
				//menuLocation = interactObject.Name;
				baseXML = dialogmsg.baseXML;

                // set position
                x = dialogmsg.x;
                y = dialogmsg.y;

                // set visible
                SetVisible(true);
            }
        }
        base.PutMessage(msg);
    }
	
	void LoadNewMenu(InteractionMap item) {
		page = 0;
		breadcrumbs.Add(item.item);
		menuLocation = item.item;
		string newXML = item.item.Remove(0, 4);
		
		InteractDialogMsg idmsg = new InteractDialogMsg();
        //idmsg.command = DialogMsg.Cmd.open;
        idmsg.LoadXML("XML/Interactions/" + newXML);
		items = idmsg.items;
        //InteractDialog.GetInstance().PutMessage(idmsg);
	}
	
	void LoadOldMenu() {
		page = 0;
		menuLocation = breadcrumbs.Last();
		breadcrumbs.RemoveAt(breadcrumbs.Count-1);
		string newXML;
		if(breadcrumbs.Count > 0) {
			newXML = breadcrumbs.Last().Remove(0, 4);
			Debug.Log("Reverting to " + newXML);
			InteractDialogMsg idmsg = new InteractDialogMsg();
	        idmsg.LoadXML("XML/Interactions/" + newXML);
			items = idmsg.items;
			menuLocation = newXML;
		}
		else {
			newXML = baseXML;
			Debug.Log("Reverting to " + newXML);
			InteractDialogMsg idmsg = new InteractDialogMsg();
	        idmsg.LoadXML(newXML);
			items = idmsg.items;
			menuLocation = newXML;
		}
	}
	
	void Execute(InteractionMap item)
	{
//		InteractionMgr.GetInstance().EvaluateInteractionSet(item.item);	
		
        SetVisible(false);
		if (interactObject != null)
        {
            // send message to object
            interactObject.PutMessage(new InteractMsg(interactObject.gameObject, item, item.log));
        }
        else
        {
            // send message to ourselves (for classes that inherit the base class)
            this.PutMessage(new InteractMsg(null, item, item.log));
        }
	}


    public override void OnGUI()
    {		
        if (IsVisible() == false)
            return;
		
		if(!setupDefault) {
			defaultSkin = GUI.skin;
			setupDefault = true;
		}
		
		GUI.skin = defaultSkin;
		GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
		
		GUI.skin = gSkin;
		
		string test = "";
		
		GUI.Button(new Rect(position.x-25, position.y-25, 50, 50), ""/*menuLocation*/);
			
		for(int i = 0; i < displayList.Count; i++) {
			int location = i;
			if(displayList.Count > 5) {
				location += page * 4;
				if(displayList.Count <= location) {
					break;
				}
				if(i < 4) {
					//Debug.Log(displayList[location].item);
					//Button sizes here
					if(GUI.Button(new Rect(position.x + positions[i].x, position.y + positions[i].y, 125, 20), StringMgr.GetInstance().Get(displayList[location].item))) {
						if(displayList[location].item.Contains("XML:")) {
						   LoadNewMenu(displayList[location]);
							page = 0;
							location = 0;
						}
						else {
						   Execute(displayList[location]);
							page = 0;
							location = 0;
							displayList = new List<InteractionMap>();
							breadcrumbs = new List<String>();
						}
					}
					if(page > 0) {
						if(GUI.Button(new Rect(position.x + positions[4].x, position.y + positions[4].y, 125, 20), "Next Page")) {
							page++;
						}
					}
				} else {
					if(GUI.Button(new Rect(position.x + positions[i].x, position.y + positions[i].y, 125, 20), "Next Page")) {
						page++;
					}
					break;
				}
				if((page*4) >= displayList.Count)
					page = 0;
			} else {
				//Button sizes here too
				//Debug.Log(displayList[location].item);
				if(GUI.Button(new Rect(position.x + positions[i].x, position.y + positions[i].y, 125, 20), StringMgr.GetInstance().Get(displayList[location].item))) {
					if(displayList[location].item.Contains("XML:")) {
					   LoadNewMenu(displayList[location]);
						page = 0;
						location = 0;
					}
					else {
					   Execute(displayList[location]);
						page = 0;
						location = 0;
						displayList = new List<InteractionMap>();
						breadcrumbs = new List<String>();
					}
				}
			}
//				test = displayList[i];
//				breadcrumbs.Add(menuLocation);
//				menuLocation = displayList[i].Remove(0, breadcrumbs[breadcrumbs.Count-1].Length+1);
		}
		
		displayList = new List<InteractionMap>();
		
		if(items != null)
			for(int i = 0; i < items.Count; i++) {
				displayList.Add(items[i]);
			}
		
		if(displayList.Count == 0) {
			displayList = new List<InteractionMap>();
			breadcrumbs = new List<String>();
			menuLocation = interactObject.Name;
			page = 0;
			start = false;
			
			if(GUI.Button(new Rect(position.x -90, position.y -10, 50, 20), "Return")) {
				displayList = new List<InteractionMap>();
				breadcrumbs = new List<String>();
				menuLocation = interactObject.Name;
				start = false;
				SetVisible(false);
			}
			
			return;
		}
		
		if(breadcrumbs.Count > 0) {
			if(GUI.Button(new Rect(position.x -90, position.y -10, 50, 20), "Return")){
				Debug.Log("Returning to " + breadcrumbs.Last());
				LoadOldMenu();
				page = 0;
			}
		}
		else {
			if(GUI.Button(new Rect(position.x -90, position.y -10, 50, 20), "Return")) {
				page = 0;
				displayList = new List<InteractionMap>();
				breadcrumbs = new List<String>();
				if(interactObject != null)
					menuLocation = interactObject.Name;
				else
					menuLocation = "";
				start = false;
				SetVisible(false);
			}
		}
		
		return;
//
//        if (modal)
//        {
//            GUI.depth = -1;
//            // put full screen box under the whole screen
//            GUI.Box(new Rect(0,0,Screen.currentResolution.width,Screen.currentResolution.height),"");
//        }
//
//        if (gSkin)
//            GUI.skin = gSkin;
//
//        GUI.skin = gSkin;
//        w = 300;
//        h = 40 * 2 + items.Count * 25;
//        x = (Screen.width / 2) - ((w) / 2);
//        y = (Screen.height / 2) - ((h) / 2);
//        
//        GUI.Box(new Rect(x, y, w, 40), title);
//
//        int button_w = w;
//        int button_h = 27;
//        gap_h = 0;
//
//        // draw our GUI
//        int count = 0;
//
//        if (items != null)
//        {
//            foreach (InteractionMap item in items)
//            {
//                if (GUI.Button(new Rect(x, 42 + y + count * (button_h + gap_h), button_w, button_h), new GUIContent(StringMgr.GetInstance().Get(item.item), StringMgr.GetInstance().Get(item.tooltip))))
//                {
//                    Brain.GetInstance().PlayAudio("CLICK:INTERACT:DIALOG");
//
//                    // play audio
//                    Brain.GetInstance().PlayAudio(item.sound);
//
//                    // make invisible
//                    SetVisible(false);
//
//                    // send a msg to this object
//                    if (interactObject != null)
//                    {
//                        // send message to object
//                        interactObject.PutMessage(new InteractMsg(interactObject.gameObject, item, item.log));
//                    }
//                    else
//                    {
//                        // send message to ourselves (for classes that inherit the base class)
//                        this.PutMessage(new InteractMsg(null, item, item.log));
//                    }
//                }
//                count++;
//            }
//        }
//
//        GUI.Label(new Rect(x, 42 + y + count * (button_h + gap_h), button_w, button_h), "");
//
//        if (tooltipSkin)
//            GUI.skin = tooltipSkin;
//        else
//            GUI.skin = null;
//
//        //GameObject.Find("HUD").GetComponent<HUD>().DrawTooltip(GUI.tooltip, Color.white);
//
//        GUI.tooltip = "";
//
//        DrawExitButton();
    }

    override public void DrawExitButton()
    {
        // exit button
        if (exit == true)
        {
            if (gExitButtonSkin)
            {
                GUI.skin = gExitButtonSkin;
            }

            Vector2 pos = ExitButtonPosition();

            if (GUI.Button(new Rect(x + w - exit_x - 2, y + exit_y, exit_w + 2, exit_h), ""))
            {
                Brain.GetInstance().PlayAudio("GENERIC:CLICK");
                SetVisible(false);
            }
            GUI.skin = gSkin;
        }
    }
}

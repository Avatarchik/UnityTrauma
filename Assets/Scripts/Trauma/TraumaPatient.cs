using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TraumaPatient : Patient
{
	public GameObject cp;
	public Texture2D suturedTexture; // this is somewhat of a hack, not a good general solution for suturing

	// once meshToggles are working, deprecate these members, and move this up to the patient or higher class
/* done.
	public GameObject straps;
	public GameObject blanketTop;
	public GameObject blanketBottom;
	public GameObject bpCuffRight;
	public GameObject bpDeviceRight;
	public GameObject bpCuffLeft;
	public GameObject bpDeviceLeft;
	public GameObject gauzeRight;
	public GameObject gauzeLeft;
	public GameObject pelvicWrap;
	public GameObject ekg1;
	public GameObject ekg2;
	public GameObject ekg3;
	public GameObject angioCatheterLeft;
	public GameObject angioCatheterRight;
	public GameObject oxygenCannula;
*/
    public override void Start()
    {
        base.Start();

        LoadInteractionXML("XML/Interactions/Patient");

        // load default patient status
        PatientStatusMgr.GetInstance().LoadDefaultXML("XML/PatientStatus");
        // load case specific patient status
        PatientStatus status = PatientStatusMgr.GetInstance().LoadXML("XML/PatientStatus");
        PatientStatusMgr.GetInstance().Add(status);
		
		// reparent this to the _Characters group, just for ease of editing in development
		if (Application.isEditor && transform.parent != null && transform.parent.name != "_Characters"){
			GameObject group = GameObject.Find("_Characters");
			if (group != null)
				transform.parent = group.transform;
		}
/*       
		bpCuffLeft.renderer.material.renderQueue = 3050;
		bpDeviceLeft.renderer.material.renderQueue = 3050;
		bpCuffRight.renderer.material.renderQueue = 3050;
		bpDeviceRight.renderer.material.renderQueue = 3050;
		gauzeLeft.renderer.material.renderQueue = 3050;
		gauzeRight.renderer.material.renderQueue = 3050;
		pelvicWrap.renderer.material.renderQueue = 3050;
		ekg1.renderer.material.renderQueue = 3050;
		ekg2.renderer.material.renderQueue = 3050;
		ekg3.renderer.material.renderQueue = 3050;
		straps.renderer.material.renderQueue = 3050;
		angioCatheterLeft.renderer.material.renderQueue = 3050;
		angioCatheterRight.renderer.material.renderQueue = 3050;
		oxygenCannula.renderer.material.renderQueue = 3050;
		blanketTop.renderer.material.renderQueue = 3100;
		blanketBottom.renderer.material.renderQueue = 3200;
*/
/* defer these mesh toggles to the child object's mesh toggle component startState setting 
 * so the patient is configurable from the editor.			
 * 
		blanketTop.GetComponent<MeshToggle>().Toggle(false, 0f);
		blanketBottom.GetComponent<MeshToggle>().Toggle(false, 0f);
		bpCuffLeft.GetComponent<MeshToggle>().Toggle(false, 0f);
		bpDeviceLeft.GetComponent<MeshToggle>().Toggle(false, 0f);
		bpCuffRight.GetComponent<MeshToggle>().Toggle(false, 0f);
		bpDeviceRight.GetComponent<MeshToggle>().Toggle(false, 0f);
//		straps.GetComponent<MeshToggle>().Toggle(false, 0f); // letting the primary nurse do this for mow...
		gauzeRight.GetComponent<MeshToggle>().Toggle(false, 0f);
		pelvicWrap.GetComponent<MeshToggle>().Toggle(false, 0f);
		ekg1.GetComponent<MeshToggle>().Toggle(false, 0f);
		ekg2.GetComponent<MeshToggle>().Toggle(false, 0f);
		ekg3.GetComponent<MeshToggle>().Toggle(false, 0f);
		angioCatheterRight.GetComponent<MeshToggle>().Toggle(false, 0f);
		oxygenCannula.GetComponent<MeshToggle>().Toggle(false, 0f);
*/		
    }
	
	public override void DoInteractMenu ()
	{
		return; // disable this menu
        // causes object to dim
        OnMouseExit();

        // interact menu
        InteractDialogMsg msg = new InteractDialogMsg();
		msg.title = this.prettyname;
        msg.command = DialogMsg.Cmd.open;
        msg.baseobj = this;
		ScriptedObject so = GetComponent<ScriptedObject>();
		if (so != null)
			msg.items = so.QualifiedInteractions();
		else
			msg.items = ItemResponse; // this is where added items get placed on the menu, because they are in here...

        msg.modal = true;
        print(InteractDialog.GetInstance().title);
        //InteractDialog.GetInstance().PutMessage(msg);
        InteractDialogLoader.GetInstance().PutMessage(msg);

        Brain.GetInstance().PlayAudio("OBJECT:INTERACT:CLICK");
	}

    public override void UpdateVitals()
    {
        base.UpdateVitals();
    }

    public override void HandleResponse(GameMsg msg)
    {
        // InteractStatusMsg is broadcast...
        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
            //UnityEngine.Debug.Log("TraumaPatient.HandleResponse(" + ismsg.InteractName + ")");
            if (ismsg.InteractName == "PREP:INTUBATION:COMPLETE")
            {
                Intubated = true;
            }
        }

        // InteractMsg is just for me...
        InteractMsg imsg = msg as InteractMsg;
        if ( imsg != null )
        {
            if (imsg.map.item == "CHECK:ETCO2")
            {
                if (Intubated == false)
                {
                    // not intubataed
                    imsg.map.response = "Patient has not been prepped for intubation.";
                    imsg.map.sound = null;
                    base.HandleResponse(imsg);
                }
                else
                {
                    // default case
                    base.HandleResponse(msg);
                }
            }
        }

        base.HandleResponse(msg);
    }
	
    public override void HandleInteractionError(InteractMsg imsg, string error)
    {
        // no subject, find someone to do the command
        List<ObjectInteraction> objects = ObjectInteractionMgr.GetInstance().GetEligibleObjects(imsg.map.item);
        foreach (ObjectInteraction obj in objects)
        {
            Character character = obj as Character;
            if (character != null)
            {
                if (character.IsDone() == true)
                {
                    UnityEngine.Debug.Log("TraumaPatient.HandleInteractionError(" + imsg.map.item + ") : found character=" + character.name);
                    InteractionMap map = InteractionMgr.GetInstance().Get(imsg.map.item);
                    character.PutMessage(new InteractMsg(character.gameObject, map));
                    return;
                }
            }
        }
		base.HandleInteractionError(imsg,error);
    }
	
/*
    public void InnerView() // are these still used ?
    {
        SetMesh(straps, false);
        SetMesh(blanketTop, true);
        SetMesh(blanketBottom, true);
        gameObject.GetComponent<MultiMesh>().SwitchMesh(1);
    }

    public void OuterView()
    {
        SetMesh(straps, false);
        SetMesh(blanketTop, false);
        SetMesh(blanketBottom, false);
        gameObject.GetComponent<MultiMesh>().SwitchMesh(0);
    }
*/
	
	string view = "RAIL:SWITCH:OUTER";
	
    public override void PutMessage(GameMsg msg)
    {
        base.PutMessage(msg);

        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
            if (ismsg.InteractName == "RAIL:SWITCH:INNER")
            {
                //InnerView();
				view = ismsg.InteractName;
            }
            if (ismsg.InteractName == "RAIL:SWITCH:OUTER")
            {
                //OuterView();
				view = ismsg.InteractName;
            }
			// turn off idles, like when patient gets tranquilized.
			if (ismsg.InteractName == "PATIENT:IDLES:OFF")
			{
				AnimationManager am = gameObject.GetComponent<AnimationManager>();
				if (am != null){
					foreach (Posture p in am.Postures){
						p.CanIdle = false;
					}
				}
			}
			if (ismsg.InteractName == "PATIENT:IDLES:ON")
			{
				AnimationManager am = gameObject.GetComponent<AnimationManager>();
				if (am != null){
					foreach (Posture p in am.Postures){
						if (p.IdleClips.Count > 0 && p.IdleRate > 0)
							p.CanIdle = true;
					}
				}
			}

			// we should migrate this down to the meshtoggle, maybe subclass meshtoggle with multimesh
			if (ismsg.InteractName == "PATIENT:CLOTHING:OFF")
			{
//				SetMesh(straps, false);
				gameObject.GetComponent<MultiMesh>().SwitchMesh(1);
			}
			if (ismsg.InteractName == "PATIENT:CLOTHING:ON")
			{
				gameObject.GetComponent<MultiMesh>().SwitchMesh(0);
			}
			if (ismsg.InteractName == "PATIENT:SUTURE:ON")
			{
				// find the renderer for the unclothed model, and swap it's texture to the sutured texture...
				MultiMesh mm = GetComponent<MultiMesh>();
				if (mm != null){
					foreach (GameObject go in mm.meshes){
						if (go.renderer != null && suturedTexture != null)
							go.renderer.material.mainTexture = suturedTexture; // we can't go backwards unles we stash the original texture.
					}

				}
//				if (cp != null && cp.renderer != null && suturedTexture != null)
//					cp.renderer.material.mainTexture = suturedTexture; // we can't go backwards unles we stash the original texture.
			}

/*			
			
			
			
			if (ismsg.InteractName == "PATIENT:BPDEVICELEFT:ON")
			{
				SetMesh(bpDeviceLeft, true);
			}
			if (ismsg.InteractName == "PATIENT:BPDEVICELEFT:OFF")
			{
				SetMesh(bpDeviceLeft, false);
			}
			if (ismsg.InteractName == "PATIENT:BPCUFFLEFT:ON")
			{
				SetMesh(bpCuffLeft, true);
			}
			if (ismsg.InteractName == "PATIENT:BPCUFFLEFT:OFF")
			{
				SetMesh(bpCuffLeft, false);
			}
			if (ismsg.InteractName == "PATIENT:ANGIOCATHETER:LEFT:ON")
			{
				SetMesh(angioCatheterLeft, true);
			}
			if (ismsg.InteractName == "PATIENT:ANGIOCATHETER:LEFT:OFF")
			{
				SetMesh(angioCatheterLeft, false);
			}
			if (ismsg.InteractName == "PATIENT:GAUZE:LEFT:ON")
			{
				SetMesh(gauzeLeft, true);
			}
			if (ismsg.InteractName == "PATIENT:GAUZE:LEFT:OFF")
			{
				SetMesh(gauzeLeft, false);
			}
			if (ismsg.InteractName == "PATIENT:ANGIOCATHETER:RIGHT:ON")
			{
				SetMesh(angioCatheterRight, true);
			}
			if (ismsg.InteractName == "PATIENT:ANGIOCATHETER:RIGHT:OFF")
			{
				SetMesh(angioCatheterRight, false);
			}
			if (ismsg.InteractName == "PATIENT:GAUZE:RIGHT:ON")
			{
				SetMesh(gauzeRight, true);
			}
			if (ismsg.InteractName == "PATIENT:GAUZE:RIGHT:OFF")
			{
				SetMesh(gauzeRight, false);
			}
			if (ismsg.InteractName == "PATIENT:OXYGENCANNULA:ON")
			{
				SetMesh(oxygenCannula, true);
			}
			if (ismsg.InteractName == "PATIENT:OXYGENCANNULA:OFF")
			{
				SetMesh(oxygenCannula, false);
			}
			if (ismsg.InteractName == "PATIENT:STRAPS:ON")
			{
				SetMesh(straps, true);
			}
			if (ismsg.InteractName == "PATIENT:STRAPS:OFF")
			{
				SetMesh(straps, false);
			}

			if (ismsg.InteractName == "PATIENT:BLANKET:TOP:ON")
			{
				SetMesh(blanketTop, true);
			}
			if (ismsg.InteractName == "PATIENT:BLANKET:TOP:OFF")
			{
				SetMesh(blanketTop, false);
			}
			if (ismsg.InteractName == "PATIENT:BLANKET:BOTTOM:ON")
			{
				SetMesh(blanketBottom, true);
			}
			if (ismsg.InteractName == "PATIENT:BLANKET:BOTTOM:OFF")
			{
				SetMesh(blanketBottom, false);
			}
			if (ismsg.InteractName == "PATIENT:PELVICWRAP:ON")
			{
				SetMesh(pelvicWrap, true);
			}
			if (ismsg.InteractName == "PATIENT:PELVICWRAP:OFF")
			{
				SetMesh(pelvicWrap, false);
			}
*/
        }
    }
	
	public override void OnMouseUp ()
	{
		if ( view == "RAIL:SWITCH:OUTER" )
			base.OnMouseUp ();
		else
			cp.GetComponent<ColorPicker>().Click();
	}
	
	public override void OnMouseExit ()
	{
		base.OnMouseExit ();
	}
	
	public void SetMesh(GameObject target, bool state)
	{
		target.GetComponent<MeshToggle>().Toggle(state);
	}
	
	/*public void OnGUI()
	{
		if(GUILayout.Button("Straps Off"))
			SetMesh(straps, false);
		if(GUILayout.Button("Straps On"))
			SetMesh(straps, true);
		if(GUILayout.Button("Top Blanket Off"))
			SetMesh(blanketTop, false);
		if(GUILayout.Button("Top Blanket On"))
			SetMesh(blanketTop, true);
	}*/
}

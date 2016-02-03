using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MedAdministrationDialogMsg : DialogMsg
{
    public Provider provider;
    public Patient patient;
    public MedAdministrationDialogMsg() : base()
    {
    }
}

public class MedAdministrationDialog : Dialog
{
    List<string> deliverys;
    List<string> deliveryNames;
    int selected,lastselected;
    int delivery,lastdelivery;
    Vector2 scrollposition;
    Med selectedMed;

    MultiSelectWidget selectWidget;

    static MedAdministrationDialog instance;

    public MedAdministrationDialog() : base() 
    {
        instance = this;
    }

    static public MedAdministrationDialog GetInstance()
    {
        return instance;
    }

    public void Awake()
    {
    }

    public void Start()
    {
        deliverys = MedMgr.GetInstance().GetMedNames();

        delivery = -1;
        selected = -1;
        lastselected = -1;
        lastdelivery = -1;

        selectWidget = new MultiSelectWidget();
        selectWidget.SetSizes(20, 20, 50, 25);
        selectWidget.SetNoValue("---");
    }

    public override void Update()
    {
        if (selected != -1 && selected != lastselected)
        {
            Debug.Log("selected=" + selected + " lastselected=" + lastselected);

            selectedMed = MedMgr.GetInstance().GetMed(deliverys[selected]);
            if (selectedMed != null)
            {
                deliveryNames = selectedMed.GetDeliveryMethods();
            }

            lastselected = selected;
            delivery = -1;
            selectWidget.SetNoValue("---");
        }

        if (delivery != -1 && delivery != lastdelivery)
        {
            selectWidget.SetNoValue("---");
            selectWidget.SetValues(selectedMed.DeliveryMethods[delivery].DosageLo, selectedMed.DeliveryMethods[delivery].DosageHi, selectedMed.DeliveryMethods[delivery].DosageInc);
            lastdelivery = delivery;
        }
        base.Update();
    }

    public void OnGUI()
    {
        if (IsVisible() == false)
            return;

        w = 800;
        h = 500;
        x = Screen.width/2 - w / 2;
        y = Screen.height/2 - h / 2;

        GUILayout.BeginArea(new Rect(x,y,w,h));
        GUILayout.Box("MedAdminister",GUILayout.Width(w),GUILayout.Height(h));
        GUILayout.EndArea();

        int selectw = 700;
        int selecth = 400;
        int selectx = Screen.width / 2 - selectw / 2;
        int selecty = Screen.height / 2 - selecth / 2;
        GUILayout.BeginArea(new Rect(selectx,selecty,selectw,selecth));
        scrollposition = GUILayout.BeginScrollView(scrollposition,GUILayout.Width(selectw),GUILayout.Height(selecth/2));
        selected = GUILayout.SelectionGrid(selected, deliverys.ToArray(), 4);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (selected != -1)
        {
            int adminx = selectx;
            int adminy = selecty + selecth/2 + 20;
            int adminw = selectw;
            int adminh = 150;

            GUILayout.BeginArea(new Rect(adminx,adminy,adminw,adminh));
            GUILayout.Box("", GUILayout.Width(adminw), GUILayout.Height(adminh));
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(adminx + 20, adminy + 10, adminw - 40, adminy - 40));
            GUILayout.BeginVertical();

            if (selectedMed != null)
            {
                GUILayout.Label("Selected : " + deliverys[selected], GUILayout.Width(200));
                GUILayout.Label(selectedMed.InteractionMap.note);

                GUILayout.Space(5);
                delivery = GUILayout.SelectionGrid(delivery, deliveryNames.ToArray(), deliveryNames.Count, GUILayout.Width(300));

                if (delivery != -1)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Dosage (mg) : ", GUILayout.Width(100));
                    selectWidget.OnGUI();
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        GUILayout.BeginArea(new Rect(selectx, selecty + selecth - 30, selectw, selecth));
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (selectWidget.position != -1)
        {
            if (GUILayout.Button("Administer", GUILayout.Width(100)))
            {
                // NOTE....We may need the concept of "ordering" meds first.  This is part of the
                // med manager but I am sending the Med Administer command first

                // broadcast Administer message
                MedAdministerMsg msg = new MedAdministerMsg();
                msg.Med = selectedMed;
                msg.Type = selectedMed.GetDeliveryType(deliveryNames[delivery]);
                msg.Dosage = Convert.ToInt32(selectWidget.GetString());
                msg.Time = elapsedTime;
                // send to all objects
                ObjectManager.GetInstance().PutMessage(msg);
                // send to MedMgr
                MedMgr.GetInstance().PutMessage(msg);
                //
                SetVisible(false);
                selected = -1;
                selectWidget.SetNoValue("---");
            }
        }
        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
        {
            SetVisible(false);
            selected = -1;
            selectWidget.SetNoValue("---");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    public override void PutMessage(GameMsg msg)
    {
        MedAdministrationDialogMsg dmsg = msg as MedAdministrationDialogMsg;
        if (dmsg != null)
        {
            UnityEngine.Debug.Log("MedAdministrationDialog.PutMessage(" + msg + ") : Provider=" + dmsg.provider);
        }
        base.PutMessage(msg);
    }
}


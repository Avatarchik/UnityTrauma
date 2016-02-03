using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DummyInteractObject : ObjectInteraction
{
    public class DIOInteractions
    {
        public InteractionMap item;
        public ObjectInteraction objectInteraction;
    }

    public List<DIOInteractions> dioItems = new List<DIOInteractions>();

    public override void Start()
    {
        base.Start();
        onTeamMenu = false;
    }

    public void CompileInteractions(string category)
    {
        dioItems = new List<DIOInteractions>();
        Object[] temp = ObjectInteraction.FindObjectsOfType(typeof(ObjectInteraction));
        List<InteractionMap> items = new List<InteractionMap>();
        List<InteractionMap> tempList;
        foreach (Object obj in temp)
        {
            // Ignore this instance
            if (obj == this) continue;
            ObjectInteraction objI = obj as ObjectInteraction;
            if (!objI.onTeamMenu) continue;

            tempList = new List<InteractionMap>();

            ScriptedObject so = objI.GetComponent<ScriptedObject>();
            if (so != null)
                tempList = so.QualifiedInteractions();
            else
                tempList = ItemResponse;

            // Check each interactionmap's category to see if it matches the passed-in category
            foreach (InteractionMap im in tempList)
            {
                string front;
                int start = 0;

                // check if subcatagories listed in string
                if (im.category == null) continue;
                foreach (string cat in im.category)
                {
                    if ((start = cat.IndexOf("/")) >= 0)
                        front = cat.Substring(0, start);
                    else
                        front = cat;

                    if (front.ToLower() == category.ToLower())
                    {
                        items.Add(im);

                        DIOInteractions dio = new DIOInteractions();
                        dio.item = im;
                        dio.objectInteraction = objI;
                        dioItems.Add(dio);
                        break;
                    }
                }
            }
        } // End of object looping

        // Create Interact dialog message
        prettyname = category.ToUpper();
        ItemResponse = items;
        InteractDialogMsg msg = new InteractDialogMsg();
        msg.command = DialogMsg.Cmd.open;
        msg.baseobj = this;
        msg.title = category;
        msg.x = (int)Input.mousePosition.x;
        msg.y = (int)Input.mousePosition.y;
        msg.items = items; // this is where added items get placed on the menu, because they are in here...
        msg.modal = true;
        msg.baseXML = "";
        InteractDialogLoader.GetInstance().PutMessage(msg);

    }

    public override void PutMessage(GameMsg msg)
    {
        //base.PutMessage(msg);
        InteractMsg iMsg = msg as InteractMsg;
        if (iMsg != null)
        {
            foreach (DIOInteractions dio in dioItems)
            {
                if (dio.item == iMsg.map)
                {
                    dio.objectInteraction.PutMessage(new InteractMsg(dio.objectInteraction.gameObject, dio.item, dio.item.log));
                    return;
                }
            }
        }
    }
}

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Sphere : ObjectInteraction
{
    public Sphere() : base()
    {
    }

    public override void Awake()
    {
        base.Awake();
    
        this.AddItem(new InteractionMap("Hello World", "You activated the hello world command", "TITLE", null, null, null, null, true));
    }

    override public void PutMessage(GameMsg msg)
    {
        HandleResponse(msg);
    }
}
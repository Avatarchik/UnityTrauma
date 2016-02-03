using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TraumaObject : ObjectInteraction
{
    public string XMLName;

    public override void Start()
    {
        base.Start();
        base.LoadXML(XMLName);
    }
}

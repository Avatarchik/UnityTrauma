using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

[CustomEditor(typeof(ScriptedObjectMonitor))]

// you have to create an subclass of this inspector for each sub class that supplies the XMLName

public class ScriptedObjectMonitorInspector : Editor 
{
	ScriptedObjectMonitor monitor;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if ( monitor == null )
			monitor = target as ScriptedObjectMonitor;

		if ( monitor != null )
		{
			monitor.Display();
			EditorUtility.SetDirty (monitor);
		}
	}

}

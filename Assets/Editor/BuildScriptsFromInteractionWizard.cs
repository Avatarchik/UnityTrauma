//  2012 Trailzen Designs

/*
	BuildScriptsFromInteractionWizard.cs
	by Phil Abbott
	
This thing should: Load up the Interactions, stringMap, whatever else is needed, then let you browse to an interaction,
select it, then it should build a hierarchy of game obejcts to the scene with the right components on them, and initialize
whatever fields it can to match the selected InteractionMap	
	
*/
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;

public class BuildScriptsFromInteractionWizard : ScriptableWizard {
	
//	public string interactionToBuild = "PREP:INTUBATION";
//	public string path = "Assets/Resources/XML/";
	public string XMLName = "XML/Interactions/AirwayMD/AirwayMDMain";
	
	public bool countIfs = false;
	
	public bool remove3secDelays = false;
	public bool removeInitialMessage = false;
	public bool removeAllTaskMessages = false;
	public bool printScriptNames = false;
	public bool checkItemTriggers = false;
	
	
	public string[] exclusions = new string[20]{"TASK:VENT:REPORT", // AM  messages that voicemaps know about
												"TASK:VENT:CONFIRM",
												"PREP:INTUBATION",
												"TASK:BREATHE:RESULTS",
												"TASK:CHESTRISE:REPORT",
												"CHECK:ETCO2",
												"TASK:GCS:CHECK", //PN
												"TASK:SKINTEMP:REPORT",
												"TASK:PLACEIV:USE",
												"TASK:PLACEIV:RESULTS",
												"TASK:RADIALPULSE:REPORT",
												"TASK:CAROTIDPULSE:REPORT",
												"TASK:BLOOD:GET",
												"TASK:BLOOD:REPORT",
												"TASK:CRYSTAL:GET",
												"TASK:CRYSTAL:REPORT",
												"TASK:IVFLOW:GET",
												"TASK:VENT:CALLBACK", //RT
												"TASK:PHONE:REPORT", //SN
												"TASK:PELVICREPORT:ECHO"
												};
//	public bool modifyFinalTSKCOMPLETE = false;
	MenuTreeNode menuTree; // defined in the ObjectInteractionInspector.cs file
	ScriptedObject SO;
	int currentScript = 0;
	
	void OnWizardCreate() { // do we need another button for something ?
		
		if (countIfs){
			foreach (ScriptedAction SA in FindObjectsOfType(typeof(ScriptedAction)) as ScriptedAction[]){
				if (SA.type == ScriptedAction.actionType.ifThenElse){
					Debug.Log ("found if "+SA.name+SA.transform.parent.name );		
					
				}
			}
		}
		
		if (printScriptNames){
			string nameString = "";
			foreach (InteractionScript SA in FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[]){
				nameString += SA.name + "\n";
			}
			Debug.Log (nameString);
		}
		
		if (checkItemTriggers){
			foreach (InteractionScript SA in FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[]){
				if (SA.triggerStrings != null && SA.triggerStrings.Length>0){
					if (SA.triggerStrings[0] != SA.item)
						Debug.Log("MISMATCH IN "+SA.name+" "+SA.item);
				}
			}
		}
		
		if (remove3secDelays){
			// look for any scripted action of type put message where message type is interactmessage and delay=3, and
			// set the delay to 0
			foreach (ScriptedAction SA in FindObjectsOfType(typeof(ScriptedAction)) as ScriptedAction[]){
				if (SA.type == ScriptedAction.actionType.putMessage && SA.gameMsgForm != null &&
					SA.gameMsgForm.msgType == GameMsgForm.eMsgType.interactMsg && 
					SA.fadeLength == 3){
					SA.fadeLength = 0;
					Debug.Log ("Changing delay time from 3 to 0 in "+SA.name+SA.transform.parent.name);
				}
			}
		}
		
		if (removeInitialMessage){
			// now look for scripts that have a send interact message as their first line where the
			// message item matches the script name, and get rid of those.
			foreach (InteractionScript IS in FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[]){
				if (IS.scriptLines != null &&
					IS.scriptLines[0] != null &&
					IS.scriptLines[0].type == ScriptedAction.actionType.putMessage &&
					IS.scriptLines[0].name == "sendInteract"+IS.name
					
				)
				DeleteScriptLine(IS,0);
			}
		}
		
		if (removeAllTaskMessages){
			
			foreach (InteractionScript IS in FindObjectsOfType(typeof(InteractionScript)) as InteractionScript[]){
				bool found = true; // so we can loop thru as many as needed
				while(found){
					found = false;
					for (int i = 0; i<IS.scriptLines.Length; i++){
						if (!IS.scriptLines[i].name.Contains("VM") &&
							IS.scriptLines[i].name.Contains("sendInteractTASK:")){
							bool isException = false;
							for (int n = 0;n<exclusions.Length;n++){
								if 	(IS.scriptLines[i].name == "sendInteract"+exclusions[n]){
									isException = true;
									Debug.Log ("Renaming VoiceMapped message "+IS.scriptLines[i].name);
									IS.scriptLines[i].name = "VM"+IS.scriptLines[i].name;
									break;
								}
							}
								if (IS.scriptLines[i].name.Contains ("REPORT")){
									isException = true;
									IS.scriptLines[i].type = ScriptedAction.actionType.wait;
									Debug.Log ("Turning REPORT into WAIT for "+IS.scriptLines[i].name);
								}
								if (!isException){
								Debug.Log ("Deleting Line "+IS.scriptLines[i].name);
								DeleteScriptLine(IS,i);
								found = true;
							break;
							}
						}
					}
				}
			}			
		}
		
//		if (modifyFinalTSKCOMPLETE){
		// look for character tasks which are the last ones and whose complete message matches the task
		
//		}
	}
				
	void DeleteScriptLine(InteractionScript script, int index){
		
		ScriptedAction lineToDelete = script.scriptLines[index];
		
		// delete from the list
		ScriptedAction[] tmp = new ScriptedAction[script.scriptLines.Length];
		for (int i=0; i < script.scriptLines.Length; i++){
				tmp[i]= script.scriptLines[i];
		}
		script.scriptLines = new ScriptedAction[tmp.Length-1];
		for (int i=0; i < index; i++){
			script.scriptLines[i] = tmp[i];
		}
		for (int i=index+1; i < tmp.Length; i++){
			script.scriptLines[i-1] = tmp[i];
		}
		// clean up the game object.
		DestroyImmediate(lineToDelete.gameObject);  // that should get rid of all the components, too?
	}
	
	void OnWizardOtherButton(){
		
		menuTree = MenuTreeNode.BuildMenu(XMLName);
		// assuming that worked, we'll build a script of character tasks for every node that is an interaction.
		string[] parts = XMLName.Split('/');
		GameObject scriptedObjectGO = new GameObject(parts[parts.Length-1]+"Scripts");
		SO = scriptedObjectGO.AddComponent<ScriptedObject>();
		int numScripts = CountScripts(menuTree);
		SO.scripts = new InteractionScript[numScripts];
		// now traverse the menutree, and as we encounter maps, build them into scripts
		currentScript = 0; // this is a global
		AddScripts(menuTree, SO, currentScript);
		
		SO.prettyname = parts[parts.Length-1]+" Scripts";
		
		SO.XMLName = XMLName+" "+DateTime.Now.ToString(); // we really don't need this value, but it shows where we came from
		
		SO.moveToParentOnDrop = true;

	}
	
	int AddScripts(MenuTreeNode node,ScriptedObject SO, int index){
		if (node.map != null){
			InteractionScript IS = BuildScriptFromInteraction(node.map);
			SO.scripts[index] = IS;
			// build the unity hierachy
			IS.gameObject.transform.parent = SO.gameObject.transform;
			index++;
		}
		else
		{
			foreach (MenuTreeNode n in node.children)
				index = AddScripts(n,SO,index);
		}
		return index;
	}
	
	int AddLines(InteractionMap map, InteractionScript IS, int index){

		// if we have an interaction list, then accumulate the number of character tasks in each map's task
		if (map.list != null && map.list.Length > 0){ // foreach isn't guaranteed to traverse in order, should index
			foreach (Interaction intr in InteractionMgr.GetInstance ().GetList(map.list).Interactions){
				index = AddLines(intr.Map,IS,index);
			}
		}
		else
		{	// no list. if we have a task, then add lines for each character tasks in it's data
			if (map.task != null 
				&& TaskMaster.GetInstance().GetTask(map.task) != null
				&& TaskMaster.GetInstance().GetTask(map.task).data != null
				&& TaskMaster.GetInstance().GetTask(map.task).data.characterTasks != null)
			{
				int hasSound = 0;
				if (map.sound != null && map.sound != "" ){
					hasSound = 1;
Debug.Log ("found sound");
				}
		
				ScriptedAction SA = BuildSendInteractMessage(map);
				IS.scriptLines[index] = SA;
				// build the unity hierachy
				SA.gameObject.transform.parent = IS.gameObject.transform;			
				index++;
				
				// add handling the sound here from the interaction map
				if (hasSound > 0)
				{
					SA = BuildPlaySound(map.sound);
					IS.scriptLines[index] = SA;
					// build the unity hierachy
					SA.gameObject.transform.parent = IS.gameObject.transform;			
					index++;
				}
												
				int lineCount = TaskMaster.GetInstance().GetTask(map.task).data.characterTasks.Count;
				foreach (CharacterTask c in TaskMaster.GetInstance().GetTask(map.task).data.characterTasks)
				{
					SA = BuildLineFromCharacterTask(c);
					IS.scriptLines[index] = SA;
					// build the unity hierachy
					SA.gameObject.transform.parent = IS.gameObject.transform;
					index++;
				}
				
				// crosslink the lines within this character task to wait for each other,
				if (lineCount > 1){
					for (int sai = index-lineCount; sai<index; sai++){
						IS.scriptLines[sai].syncToTasks = new ScriptedAction[lineCount-1];
						int insertPtr = 0;
						for (int linkPtr = index-lineCount; linkPtr < index; linkPtr++){
							if (linkPtr != sai)
								IS.scriptLines[sai].syncToTasks[insertPtr++] = IS.scriptLines[linkPtr];
						}
					}
				}
				
				// set the COMPLETE mesasge on the last one, and WaitForCompletion only on the last one
				IS.scriptLines[index-1].stringParam4 = map.item+":COMPLETE";
				IS.scriptLines[index-1].waitForCompletion = true;
			}
				
			else
			{
				ScriptedAction SAE = BuildLineFromMap(map);
				IS.scriptLines[index] = SAE;
				// build the unity hierachy
				SAE.gameObject.transform.parent = IS.gameObject.transform;
				index++; // this is really an error condition we need to resolve
				
			}
		}
		return index;
	}
	
	
	InteractionScript BuildScriptFromInteraction(InteractionMap map){
		// assume we start with the top level map, either there's a single task, or an interaction list.
		GameObject ISGO = new GameObject(map.item);
		InteractionScript IS = ISGO.AddComponent<InteractionScript>();
		IS.triggerStrings = new string[1];
		IS.triggerStrings[0] = map.item;
		
		int hasSound = 0;
		if (map.sound != null && map.sound != "" )
			hasSound = 1;
		
		IS.scriptLines = new ScriptedAction[CountLines(map)+hasSound+1]; // gonna send an interact message too?  add one
		int index = 0;
		
		///* lets not built this top level message, as the menu probably already sent it.
		ScriptedAction SA = BuildSendInteractMessage(map);
		IS.scriptLines[index] = SA;
		// build the unity hierachy
		SA.gameObject.transform.parent = IS.gameObject.transform;			
		index++;
		//*/
		
		if (hasSound > 0)
		{
			// add handling the sound here from the interaction map
			SA = BuildPlaySound(map.sound);
			IS.scriptLines[index] = SA;
			// build the unity hierachy
			SA.gameObject.transform.parent = IS.gameObject.transform;			
			index++;
		}
		
		index = AddLines (map,IS,index);
		
		IS.scriptLines[index-1].stringParam4 = map.item+":COMPLETE"; //override the final task message
		// we could move that overwritten message up to a blank slot if it's needed...
		// or add one extra script line just to send that message up.
		
		// the InteractionScript should probably be using an interaction map for these, but they are individual fields
		IS.item = map.item;
		IS.prettyname = StringMgr.GetInstance().Get(map.item); // maybe this is a reasonable default?
		IS.response = "Script for "+map.item+" is running";
		IS.response_title = map.item;
		IS.task = ""; // putting map.item here might either help logging or trigger unwantede interactions ?
		
		return IS;
	}
	
	ScriptedAction BuildLineFromCharacterTask(CharacterTask c){
		GameObject SAGO = new GameObject(c.characterName+"-"+c.nodeName+"-"+c.posture+"-"+c.lookAt+"-"+c.animatedInteraction);
		ScriptedAction SA = SAGO.AddComponent<ScriptedAction>();
		
		SA.type = ScriptedAction.actionType.characterTask;
		
		SA.objectName = c.characterName;
		SA.moveToName = c.nodeName;
		SA.stringParam = c.posture;
		SA.stringParam2 = c.lookAt;
		SA.stringParam3 = c.animatedInteraction;
		SA.fadeLength = c.delay;
		
		SA.waitForCompletion = false; // we'll set this to true for the last line of a list of CT's
	
		return SA;	
	}
	
	ScriptedAction BuildSendInteractMessage(InteractionMap map){
		GameObject SAGO = new GameObject("sendInteract"+map.item);
		ScriptedAction SA = SAGO.AddComponent<ScriptedAction>();
		
		InteractionMapForm MF = SAGO.AddComponent<InteractionMapForm>();
		SA.gameMsgForm = SAGO.AddComponent<GameMsgForm>();
		MF.InitFromMap(map);
		SA.gameMsgForm.map = MF;
		SA.gameMsgForm.msgType = GameMsgForm.eMsgType.interactMsg;
		SA.type = ScriptedAction.actionType.putMessage;
		SA.waitForCompletion = false; 
	
		return SA;	
	}
	
	ScriptedAction BuildPlaySound(string sound){
		GameObject SAGO = new GameObject("playSound"+sound);
		ScriptedAction SA = SAGO.AddComponent<ScriptedAction>();
		
		SA.type = ScriptedAction.actionType.playAudio;
		SA.stringParam = sound;
		SA.waitForCompletion = false; 
	
		return SA;	
	}
	
	ScriptedAction BuildLineFromMap(InteractionMap map){
		GameObject SAGO = new GameObject("TaskXMLIncompleteFor"+map.item);
		ScriptedAction SA = SAGO.AddComponent<ScriptedAction>();
	
		SA.type = ScriptedAction.actionType.characterTask;
		SA.fadeLength = 0;
		SA.stringParam4 = map.item+":COMPLETE"; // send this just so we don't bog down waiting for it...
		
		SA.waitForCompletion = false; // we'll set this to true for the last line of a list of CT's
	
		return SA;	
	}
	
	
	int CountScripts(MenuTreeNode node){ // we use this just to dimension the SO.scripts array
		int count = 0;
		if (node.map != null) 
			count = 1;
		else
		{
			foreach (MenuTreeNode n in node.children)
				count += CountScripts(n);
		}
		return count;
	}

	int CountLines(InteractionMap map){ // we use this just to dimension each IS.lines array
		// how many character interactions will it take for this script	
		int count = 0;
		// if we have an interaction list, then accumulate the number of character tasks in each map's task
		if (map.list != null && map.list.Length > 0){
			foreach (Interaction intr in InteractionMgr.GetInstance ().GetList(map.list).Interactions){
				count += CountLines (intr.Map);
			}
		}
		else
		{	// no list. if we have a task, then count the character tasks in it's data
			if (map.task != null 
				&& TaskMaster.GetInstance().GetTask(map.task) != null
				&& TaskMaster.GetInstance().GetTask(map.task).data != null
				&& TaskMaster.GetInstance().GetTask(map.task).data.characterTasks != null){
				count += TaskMaster.GetInstance().GetTask(map.task).data.characterTasks.Count;
				count++; // to add one line for an interact message for this task
				if (map.sound != null && map.sound != "" )
					count++; // we'll add a play sound here or voice message
			}
			else
				count++; // this is really an error condition we need to resolve
		}
		return count;
	}
	
	void OnWizardUpdate() {
		isValid = true;
		errorString = "";

	}
	
	void OnEnable(){
		//Load up the interactions, etc.	
		InteractionMgr.GetInstance().LoadXML("XML/interactions/interactions");
		// if there's no Brain in the scene, add one because the task master needs it'
		if (GameObject.Find("Brain") == null){
			GameObject brainGO = new GameObject("Brain");
			brainGO.AddComponent<TraumaBrain>();
		}
		TaskMaster.GetInstance().LoadXML("XML/Tasks");
		StringMgr.GetInstance().Load ();
	}
	
	[MenuItem("Custom/ScriptBuilder/BuildScriptFromInteraction...")]
	public static void MenuItemHandler() {
		ScriptableWizard.DisplayWizard("BuildScriptFromInteraction...", typeof(BuildScriptsFromInteractionWizard), "FIX CHKd ITEMS","Build");
	}
}

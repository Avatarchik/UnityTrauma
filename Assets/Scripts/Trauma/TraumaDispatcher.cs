using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TraumaDispatcher : Dispatcher {

	// handles any trauma specific dispatcher tasks.
	// for the tutorial, allow limiting of interactions to the short list we are looking for
	bool limitInteractions = false;
	List<string> enabledInteractions = new List<string> ();

	public override void LimitInteractions(string interactionList, bool remove = false ,bool append = false){
	
		if (interactionList == null || interactionList == "") {
			limitInteractions = false;
		} else {
			if (!append)
				enabledInteractions = new List<string>();
			string[] interactions = interactionList.Split(' ');
			foreach (string interaction in interactions){
				if (remove)
					enabledInteractions.Add (interaction);
				else
					enabledInteractions.Add (interaction);
			}
			if (enabledInteractions.Count > 0) limitInteractions = true;
		}
	}

	// Initially, this will be just Handle User COmmands, but should also migrate any character responses like acknowledgements in here.

	public override bool ExecuteCommand( string command, string preferredHandler = ""){
		// commands initialed thru the NLU, Menu, or Filter should pass thru this for processing
		
		//TODO migrate these next two blocks down to a Trauma Brain override of this method 
		if (HandleUserCommand (command)) { // decision panel, abort, confirm handled here
			// send out the ISM for tracking...
			InteractMsg imsg = new InteractMsg(null, command );
			InteractStatusMsg ism = new InteractStatusMsg(imsg);
			Brain.GetInstance().PutMessage(ism);
			return true;
		}

		if (limitInteractions){
			if (!enabledInteractions.Contains(command)){
				if (SAPISpeechManager.Instance != null)
					SAPISpeechManager.Speak ("That's not what we need to do right now.");
				// give some other kind of feedback ?
				return false;
			}
			else{
				// the desired interaction was called, now open up so scripts can use interactions to run...
				limitInteractions = false;
			}
		}
		
		if (!IsCommandAvailable( command)){ // voice commands can come in that are not currently possible
			VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:CANT:DO:THAT");
//			if (SAPISpeechManager.Instance != null)
//				SAPISpeechManager.Speak ("We can't do that right now");
			return false;
		}

		if (IsCommandQueued( command)){ // voice commands can come in that are already queued
			VoiceMgr.GetInstance().Play ("ProcedureResident","VOICE:ALREADY:WORKING");
//			if (SAPISpeechManager.Instance != null)
//				SAPISpeechManager.Speak ("We're already working on that.");
			return false;
		}
		
		// Handle Confirm Command here ?
		SAPISpeechManager.Speak (InteractionMgr.GetInstance ().Get (command).response);
		return base.ExecuteCommand (command, preferredHandler);

	}


	bool HandleUserCommand(string command){
		/*"USER:COMMAND:YES";</tag></item>
      <item> no <tag> out="USER:COMMAND:NO";</tag></item>
      <item> stop <tag>out="USER:COMMAND:STOP";</tag> </item><!-- might be a conflict with other stop commands -->
      <item> abort <tag> out="USER:COMMAND:ABORT";</tag></item>
      <item> confirm <tag> out="USER:COMMAND:CONFIRM"
		 USER:COMMAND:QUERY */
		
		if (!command.Contains ("USER:"))
			return false;

		if (command.Contains("ABORT")){
			// see if a specific character is called out...
			ObjectInteraction target = GetCharacter( command );
			if ( target != null){
				ScriptedObject so = target.GetComponent<ScriptedObject>();
				if ( so != null){
					so.AbortAllScripts();
				}
				((TaskCharacter)target).Reset();
				InteractMsg imsg = new InteractMsg(null,"GO:HOME");
				target.PutMessage(imsg);
			}
			else
			{
				// stop everyone's scripts. and send them home.
				SAPISpeechManager.Speak("Stopping Everything"); 
				foreach (ObjectInteraction oi in actors){
					if (oi as TaskCharacter != null){
						ScriptedObject so = oi.GetComponent<ScriptedObject>();
						if ( so != null){
							so.AbortAllScripts();
						}
						((TaskCharacter)oi).Reset();
//						TaskMaster.GetInstance().GoHome(oi.name); // this creates a Task. not a Character task, so it never gets updated... :/

						InteractMsg imsg = new InteractMsg(null,"GO:HOME");
						oi.PutMessage(imsg);
					}
				}
			}
			return true;
		}
		if (command.Contains("QUERY")){
			
			ObjectInteraction target = GetCharacter( command );
			if ( target != null){
				ScriptedObject so = target.GetComponent<ScriptedObject>();
				if ( so != null){
					// show somehow what scripts are being run...
					if (so.scriptStack.Count != 0){
						GameHUD gh = GUIManager.GetInstance().FindScreenByType<GameHUD>() as GameHUD;
						if (gh != null){
							gh.ShowRecentOrders();
							Invoke ("HideRecentOrders",5);
						}
						SAPISpeechManager.Speak(target.Name+" is ");
						foreach (ScriptedObject.QueuedScript qs in so.scriptStack){
							string taskDescription = qs.script.prettyname;
							if (taskDescription == "") taskDescription = qs.script.commandVariation.CmdString;
							if (taskDescription == "") taskDescription = qs.script.commandVariation.Cmd;
							if (taskDescription == "") taskDescription = qs.script.triggerStrings[0];
							if (taskDescription == "") taskDescription = "doing something with no description.";
							SAPISpeechManager.Speak(taskDescription);
						}
					}else{
						SAPISpeechManager.Speak("Nothing");
					}
				}
			}
			else
			{
				GameHUD gh = GUIManager.GetInstance().FindScreenByType<GameHUD>() as GameHUD;
				if (gh != null){
					gh.ShowRecentOrders();
					Invoke ("HideRecentOrders",5);
				}
				else{

				foreach (ObjectInteraction oi in actors){
					if (oi as TaskCharacter != null){
						ScriptedObject so = oi.GetComponent<ScriptedObject>();
						if ( so != null){
							// show somehow what scripts are being run...
							if (so.scriptStack.Count != 0){
								SAPISpeechManager.Speak(oi.Name+" is ");
								foreach (ScriptedObject.QueuedScript qs in so.scriptStack){
									string taskDescription = qs.script.prettyname;
									if (taskDescription == "") taskDescription = qs.script.commandVariation.CmdString;
									if (taskDescription == "") taskDescription = qs.script.commandVariation.Cmd;
									if (taskDescription == "") taskDescription = qs.script.triggerStrings[0];
									if (taskDescription == "") taskDescription = "doing something with no description.";
									SAPISpeechManager.Speak(taskDescription);
								}
							}
						}
					}
				}
				}
			}
			return true;
		}
		// find a dialog that might contain selected buttons
		
		GUIScreen panel = GUIManager.GetInstance().FindScreen("XrayViewerMain");//  FindScreenByType<InteractConfirmDialog>(); 
		if (panel == null)
			panel = GUIManager.GetInstance().FindScreen ("traumaDecisionPanel"); // FindScreenByType<DecisionPanel>();
		if (panel == null)
			panel = GUIManager.GetInstance().FindScreen ("dialogConfirm"); 
		if (panel == null)
			return true;
		
		
		if (command.Contains(":SELECT:")){
			// look for some toggles or buttons with names that match the selected option
			string buttonName = command.Replace ("USER:SELECT:","").ToLower();
			if (buttonName == "chatenter"){
				buttonName = "submit"; // was ChatEnter
			}
			if (buttonName == "exitbutton") buttonName = "exitButton";
			if (buttonName == "all"){
				// for this, find all the checkboxes, toggle them all to ON, then activate the chat enter button.
				// how to do this will take a bit of digging...
				//TODO
			}
			
			GUIButton selectedButton = panel.Find (buttonName) as GUIButton;
			
			
			if (selectedButton != null) {
				selectedButton.SimulateButtonAction ();
				panel.Update(); // get the submit button updated.
			}
			else
			{
				GUIToggle selectedToggle = panel.Find (buttonName) as GUIToggle;
				if (selectedToggle != null) {
					selectedToggle.SimulateButtonAction ();
				}
			}
			return true;
		}
		
		if (command.Contains ("YES")) {
			// see if there is an InteractConformDialog and a button with the text "YES"
			GUIButton okButton = panel.Find ("ok") as GUIButton;
			if (okButton != null) {
				okButton.SimulateButtonAction ();
			}
		}
		if (command.Contains ("NO")){
			// see if there is an InteractConformDialog and a button with the text "YES"
			GUIButton okButton = panel.Find("cancel") as GUIButton; // find by text would be better
			if (okButton != null){
				okButton.SimulateButtonAction();
			}
		}

		return true; // because it WAS a user:command
	}

	void HideRecentOrders(){
		GameHUD gh = GUIManager.GetInstance().FindScreenByType<GameHUD>() as GameHUD;
		if (gh != null){
			gh.HideRecentOrders();
		}
	}


}

//#define DEBUG_MATCH
#define DO_INPUT_IN_WINDOW
#define USE_SELECTION_WEIGHTS
//#define BUILD
//#define LOADXML
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class FilterInteractions : MonoBehaviour
{
	public bool ExactMatch=true;

	static FilterInteractions instance;
	public static FilterInteractions GetInstance()
	{
		return instance;
	}
	
	public void Awake()
	{
		instance = this;
	}
	
	public void Start()
	{
#if BUILD
		BuildCommandVariations("Variations");
#endif
#if LOADXML
		Serializer<List<CommandVariation>> serializer2 = new Serializer<List<CommandVariation>>();
		variations = serializer2.Load("XML/CommandVariations");
#endif
#if USE_SELECTION_WEIGHTS
		LoadSelectionWeightsFromXML ();
#endif
	}
	
	
	[System.Serializable]
	public class CommandVariation
	{
		public string Cmd;
		public string CmdString;
		public List<string> Variations;
		public MatchRecord Record;
		public CommandVariation()
		{
			Variations = new List<string>();
		}
		
		public void AddVariations( List<string> variations )
		{
			List<string> newVariations = new List<string>();
			
			// search to see if there are any new items
			foreach( string testItem in variations )
			{
				bool found=false;
				foreach( string item in Variations )
				{
					if ( testItem == item )
						found = true;
				}
				if ( found == false )
					newVariations.Add(testItem);
			}

			// add new ones
			foreach( string newItem in newVariations )
				Variations.Add(newItem);
		}

		public static bool operator ==( CommandVariation a, CommandVariation b )
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
			{
				return true;
			}
			
			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}
			
			if ( a.Cmd != b.Cmd )
			{
				UnityEngine.Debug.LogError("a.Cmd=" + a.Cmd + " != b.Cmd=" + b.Cmd);
				return false;
			}

			if ( a.CmdString != b.CmdString )
			{
				UnityEngine.Debug.LogError("Cmd=" + a.Cmd + " a.CmdString=" + a.CmdString + " != b.CmdString=" + b.CmdString);
				return false;
			}
			if ( a.Variations.Count != b.Variations.Count )
			{
				UnityEngine.Debug.LogError("Cmd=" + a.Cmd + " a.Variations.Count=" + a.Variations.Count + " != b.Variations.Count=" + b.Variations.Count);
				return false;
			}
			for ( int i=0 ; i<a.Variations.Count ; i++)
			{
				if ( a.Variations[i] != b.Variations[i] )
				{
					UnityEngine.Debug.LogError("Cmd=" + a.Cmd + " [" + i + "] a.Variations[]=" + a.Variations[i] + " != b.Variations[]=" + b.Variations[i]);
					return false;
				}
			}
			return true;
		}
		public static bool operator !=( CommandVariation a, CommandVariation b )
		{
			return !(a==b);
		}
	}
	
	public class MatchRecord
	{
		public int Full;
		public int Partial;
		public float Percentage;
		public MatchRecord() {}
		public MatchRecord( int full, int part, float percentage )
		{
			Full = full;
			Partial = part;
			Percentage = percentage;
		}

		public bool GreaterThan( MatchRecord b )
		{
			if ( Full == b.Full )
			{
				if ( Partial > b.Partial )
					return true;
				else
					return false;
			}
			else
			{
				if ( Full > b.Full )
					return true;
				else
					return false;
			}
		}
	}
	
	public List<CommandVariation> variations; // made this public so we could inspect in the editor
	CommandVariation executedCmd;
	
	public List<CommandVariation> Variations
	{
		get { return variations; }
	}
	
	public void AddVariation( FilterInteractions.CommandVariation variation )
	{
#if LOADXML
		return;
#endif
		if ( variation == null )
			return;
		if ( variation.Cmd == "" || variation.Cmd == null || variation.Variations == null || variation.Variations.Count == 0 )
			return;
		
		if ( variations == null )
			variations = new List<CommandVariation>();

		// add a stripped down version of the CmdString to the variations
		// this may need some tuning, but seems to work faily well and saves a lot of editing command variations
		string strippedCommand = variation.CmdString.ToLower();
		strippedCommand = strippedCommand.Replace (" the ", " ");
		strippedCommand = strippedCommand.Replace (" and ", " ");
		strippedCommand = strippedCommand.Replace (" patient ", " ");
		strippedCommand = strippedCommand.Replace (" patient's ", " ");
		strippedCommand = strippedCommand.Replace ("perform ", "");
		strippedCommand = strippedCommand.Replace (" a ", " ");
		strippedCommand = strippedCommand.Replace (" an ", " ");
		strippedCommand = strippedCommand.Replace (".", " ");
		strippedCommand = strippedCommand.Replace (",", " ");
		strippedCommand = strippedCommand.Replace (" them ", " ");
		strippedCommand = strippedCommand.Replace (" we ", " ");
		variation.Variations.Add (strippedCommand);
				
		bool found=false;
		foreach( CommandVariation cv in variations )
		{
			// see if command is already in list
			if ( cv.Cmd == variation.Cmd )
			{
				found = true;
				// add any variations that aren't duplicates
				cv.AddVariations(variation.Variations);
			}
		}
		// nothing found, just add it
		if ( found == false )
			variations.Add(variation);
	}
	
#if BUILD
	// this method is for adding additional strings to a command 
	void AddVariation( string cmd, List<string> strings )
	{
		bool found = false;
		
		if ( variations == null )
			variations = new List<CommandVariation>();
		
		foreach(CommandVariation var in variations )
		{
			if ( var.Cmd == cmd )
			{
				found = true;
				// add list
				foreach ( string str in strings )
				{
					if ( str != "" && str != null  )
						var.Variations.Add(str);
				}
			}
		}
		if ( found == false )
		{
			CommandVariation cv = new CommandVariation();
			cv.Cmd = cmd;
			foreach( string str in strings )
				cv.Variations.Add(str);
			variations.Add(cv);
		}
	}
	
	void BuildCommandVariations( string filename )
	{
		TextAsset asset = Resources.Load(filename) as TextAsset;
		string data = asset.ToString();
		string[] tokens = data.Split('^');
		
		bool cmdFound=false;

		for (int i=0 ; i<tokens.Length ; i++)
		{
			if ( tokens[i].Contains(":") )
			{				
				string cmd = tokens[i];
				cmd = cmd.Replace("\n","");
				cmd = cmd.Replace("\r","");
				List<string> strings = new List<string>();
				UnityEngine.Debug.Log("cmd=<" + cmd + ">");
				// get variations
				for (i=i+1 ; i<tokens.Length && cmdFound==false ; i++)
				{
					if ( tokens[i].Contains(":") )
					{
						UnityEngine.Debug.Log("found cmd=<" + tokens[i] + ">");
						cmdFound = true;
					} 
					else
					{
						if ( tokens[i] != null && tokens[i] != "" && tokens[i] != " ")
						{
							UnityEngine.Debug.Log("add=<" + tokens[i] + ">");
							strings.Add(tokens[i]);
						}
					}
				}
				i-=2;
				// we're done here
				cmdFound = false;
				AddVariation(cmd,strings);
			}
		}
		UnityEngine.Debug.Log("TOTAL COUNT=" + variations.Count);
		// save the XML
		Serializer<List<CommandVariation>> serializer = new Serializer<List<CommandVariation>>();
		serializer.Save("CommandVariations.xml",variations);		
	}
#endif
	
	List<CommandVariation> results;

	public List<CommandVariation> FindFromTag( string tag ) // find the CV matching the tag
	{
		List<CommandVariation> tmp = new List<CommandVariation> ();

		foreach (CommandVariation cv in variations) {
			if (cv.Cmd == tag){
				tmp.Add(cv);
				break;
			}
		}
		return tmp;
	}
			
	public List<CommandVariation> Filter( string input )
	{
		List<CommandVariation> tmp = new List<CommandVariation>();
		
		foreach( CommandVariation cv in variations )
		{
			float avgPercentage = 0;
			for (int i=0 ; i<cv.Variations.Count ; i++)
			{
				string variation = cv.Variations[i];
				MatchRecord record = Match (variation,input, cv.Cmd);
				if (cv.Record == null)
					cv.Record = record; // this is new PAA
				avgPercentage += record.Percentage;
				// return results based on whether we need an exact match.  The record returned
				// percentage is based on a percentage of words found in the filter string
				if ( ExactMatch == true )
				{
					if ( record != null && record.Percentage == 1.0f)
						AddResult(tmp, cv, record);
				}
				else
				{
					if ( record != null && record.Percentage > 0.0f )
						AddResult(tmp, cv, record);  // This may swap one variation for another if the patch is better
				}
			}
			avgPercentage /= cv.Variations.Count;
			// bump the match record of the copy of cv that's in tmp. will this work ?
			if (cv.Record != null)
				cv.Record.Percentage += avgPercentage;
			else
				Debug.LogError("null match record in command variation");
		}
		
		// create final list
		results = new List<CommandVariation>();
		
		// find greatest match count
		int maxFull=0;
		int maxPart=0;
		foreach ( CommandVariation cv in tmp )
		{
			if ( cv.Record.Full >= maxFull )
			{
				// if the max full is new, then clear the partial
				if ( cv.Record.Full > maxFull )
					maxPart = 0;
				
				maxFull = cv.Record.Full;
				if ( cv.Record.Partial > maxPart )
					maxPart = cv.Record.Partial;
			}
		}
		
#if DEBUG_MATCH
		UnityEngine.Debug.Log("Filter<" + input + ">");
#endif
		// now only include the max counts
		foreach ( CommandVariation cv in tmp )
		{
			if ( cv.Record.Full == maxFull )
			{
				if ( cv.Record.Partial == maxPart )
				{
#if DEBUG_MATCH
					UnityEngine.Debug.Log("Result=<" + cv.Variations[0] + ">, full=" + cv.Record.Full + " partial=" + cv.Record.Partial + " percent=" + cv.Record.Percentage);
#endif
					results.Add (cv);
				}
			}
		}		
		results = SortListByPercentage (results);
		return results;
	}

	List<CommandVariation> SortListByPercentage(List<CommandVariation> inputList){

		List<CommandVariation> returnList = new List<CommandVariation>();
		List<CommandVariation> toRemove = new List<CommandVariation> ();

		while (inputList.Count > 0) {
			float bestMatch = float.MinValue;
			toRemove.Clear();
			foreach (CommandVariation check in inputList)
				if (check.Record.Percentage > bestMatch) bestMatch = check.Record.Percentage;
			foreach (CommandVariation check in inputList){
				if (check.Record.Percentage >= bestMatch){
					returnList.Add (check);
					toRemove.Add (check);
				}
			}
			foreach (CommandVariation rem in toRemove){
				inputList.Remove(rem);
			}
		}
		return returnList;
		// cant use sorted list because it requires all keys be unique
	}


void AddResult( List<CommandVariation> list, CommandVariation CV, MatchRecord record )
	{
		foreach( CommandVariation cv in list )
		{
			// check if already here
			if ( cv.Cmd == CV.Cmd )
			{
				if ( record.GreaterThan(cv.Record) )
				{
					// swap this record for the one that is better
					list.Remove (cv);
					CV.Record = record;
					list.Add (CV); // this is going to add the new, better variation at the end of the list
#if DEBUG_MATCH	
					UnityEngine.Debug.Log (">>>> Replacing " + CV.Cmd + "<" + record.Full + "," + cv.Record.Full + ">");
#endif
				}
				else
				{
#if DEBUG_MATCH
					UnityEngine.Debug.Log (">>>> Ignorning " + CV.Cmd + "<" + record.Full + "," + cv.Record.Full + ">");
#endif
				}
				return;
			}
		}

		// check if command is currently available
		if ( Dispatcher.GetInstance().IsCommandAvailable(CV.Cmd) == false )
			return;

#if DEBUG_MATCH
		UnityEngine.Debug.Log (">>>> Adding " + CV.Cmd + "<" + record.Full + ">");
#endif
		CV.Record = record;
		list.Add(CV);
	}

	bool BaseWord( string word )
	{
		switch( word )
		{
		case "the":
		case "a":
		case "and":
		case "in":
		case "for":
			return true;
		}
		return false;
	}
	
	bool MatchWord( string full, string word )  // is this different than string.Contains() ?
	{
		if ( full == word )
			return true;
		
		// try matching partial word
		if ( full.Length >= word.Length )
		{
			string partialWord = full.Substring(0,word.Length);
			if ( partialWord == word )
				return true;
		}		
		return false;
	}
	
	string[] GetInputWords( string input )
	{
		input = input.ToLower();
		input = input.Replace(',',' ');
		input = input.Replace('.',' ');
		
		// make tokens
		string[] tokens = input.Split(' ');
		
		List<string> validWords = new List<string>();
		
		// count the valid words
		int total = 0;
		for (int i=0 ; i<tokens.Length ; i++)
		{
			if ( tokens[i] == "")
				continue;
			// check for duplicate
			bool found = false;
			foreach( string str in validWords )
			{
				if ( str == tokens[i] )
					found = true;
			}
			if ( found == false )	
			{
				// if not a duplicate and not a base word, then add it
				if ( tokens[i].Length >= 2 && BaseWord (tokens[i]) == false )
					validWords.Add (tokens[i]);
			}
		}
		return validWords.ToArray();		
	}
	
	MatchRecord Match( string key, string filterString, string cmd )
	{
		string[] tokens = GetInputWords (filterString);
		
		// get words in key
		string[] words = GetInputWords(key);//key.Replace(',',' ').Split(' ');
		
		int partialMatch=0;
		int fullMatch=0;
		

		for (int i=0 ; i<tokens.Length ; i++)
		{
			for(int j=0 ; j<words.Length ; j++)
			{				
				string word = words[j].ToLower();
				string token = tokens[i].ToLower();
				
				if ( word == token )
				{
#if DEBUG_MATCH
					UnityEngine.Debug.Log ("********* key <" + key + "> filterString <" + filterString + "> fullMatch=" + word);		
#endif
					fullMatch++;
				}
				else
				{				
					if ( MatchWord(word,token) ) // contains
					{
#if DEBUG_MATCH
						UnityEngine.Debug.Log ("********* key <" + key + "> filterString <" + filterString + "> partMatch : " + word + " : " + token);		
#endif
						partialMatch++;
					}
				}
			}
		}
		
		
		float percentage=0.0f;
		// calc percentage
		if ( fullMatch > 0 || partialMatch > 0 )
		{
			percentage = (float)(fullMatch+partialMatch)/(float)tokens.Length;
#if DEBUG_MATCH
			UnityEngine.Debug.Log ("********* fullMatch=" + fullMatch + " : partMatch=" + partialMatch + " percentage=" + percentage);
#endif
		}

#if USE_SELECTION_WEIGHTS
		// try adding in the result of the selectionWeights
		float selection = RateCommand (filterString, cmd);  // if there is an exact input match, bump the percentage for the commands historically selected by the user
		if (selection > 0)
						percentage += selection*5.0f; // how much should these apply ?
		// this works pretty well, but we may not need it if the command variations have enough info in their words
#endif
		
		return new MatchRecord(fullMatch,partialMatch,percentage);
	} 	
	
	[System.Serializable]
	public class CommandVariationResult
	{
		public CommandVariation CV;
		public bool ValidInteraction;
		public List<string> NLUResults;		
#if ADD_NLU_RECORDS
		public List<NluMgr.match_record> NLURecords;
#endif
		
		public CommandVariationResult()
		{
#if ADD_NLU_RECORDS
			NLURecords = new List<NluMgr.match_record>();
#endif
			NLUResults = new List<string>();
		}
	}
	
	CommandVariationResult currCV;
	int currCVIdx;
	int currVariationIdx;
	
	List<CommandVariationResult> CVResults = new List<CommandVariationResult>();
	
	CommandVariationResult NextResult()
	{
		CommandVariationResult result=currCV;
		if ( currCV != null )
		{	
			++currVariationIdx;
			if ( currVariationIdx >= currCV.CV.Variations.Count )
			{
				// we're at the end of the current, do next one
				if ( currCVIdx < variations.Count )
				{
					result = new CommandVariationResult();
					result.CV = variations[currCVIdx];
					CVResults.Add(result);
					currCVIdx++;
					currVariationIdx = 0;
				}
				else
					result = null;		
			}
		}
		else
		{
			// brand new start it
			currCVIdx = 0;
			result = new CommandVariationResult();
			result.CV = variations[currCVIdx];
			CVResults.Add(result);
			currCVIdx++;
			currVariationIdx = 0;
		}
			
		return result;
	}
	
	public void TestCommandVariations()
	{
		currCV = NextResult();
		if ( currCV == null )
			return;
		
        NluMgr.GetInstance().SetUtteranceCallback(new NluMgr.UtteranceCallback(GPCallback));
        NluMgr.GetInstance().SetErrorCallback(new NluMgr.ErrorCallback(GPErrorCallback));
       	NluMgr.GetInstance().Utterance(currCV.CV.Variations[currVariationIdx], "nurse");
	}

	[System.Serializable]
	public class selectionRecord{
		public string cmd;
		public int timesSelected;
	}
	public class SelectionWeights{
		public string inputPhrase; // the exact text, lower case, single spaced, minus any punctuation, the user entered.
		public int numSelections; // the number of times matching text has been logged
		public List<selectionRecord> selectionRecords; // the commands the user has picked after entering this text phrase.  Expect strong bias toward one command
	}

	public void LogSelection( string selectedCommand){
		// see if this text matches an existing inputPhrase
		SelectionWeights weight = null;
		foreach (SelectionWeights check in selectionLogs) {
			if (check.inputPhrase == lastText.ToLower()){
				weight = check;
				break;
			}
		}
		// if not, add the phrase
		if (weight == null) {
			weight = new SelectionWeights();
			weight.selectionRecords = new List<selectionRecord>();
			weight.inputPhrase = lastText.ToLower();
			selectionLogs.Add (weight);
		}
		weight.numSelections++;
		// increment or add the selected command tag
		selectionRecord match = null;
		foreach (selectionRecord checkRec in weight.selectionRecords) {
			if (checkRec.cmd == selectedCommand){
				match = checkRec;
				break;
			}
		}
		if (match == null) {
			match = new selectionRecord();
			match.cmd = selectedCommand;
			match.timesSelected = 0;
			weight.selectionRecords.Add (match);
		}
		match.timesSelected++;
		// save out the post log result to XML for now... TODO handle this in the database.  Include the case name, so we can selectively map interactions by case ?
		SaveSelectionWeightsToXML ();
	}

	public string lastText = ""; // to save the input string that created the buttons
	public List<SelectionWeights> selectionLogs = new List<SelectionWeights>(); // read this from the XML at startup...

	public float RateCommand(string inputPhrase, string command){
		// if this input phrase is in the logs, return the percentage of times this command was the chosen one,
		// else return -1;
		foreach (SelectionWeights check in selectionLogs) {
			if (check.inputPhrase == inputPhrase.ToLower ()) { // exact phrase match
				// the phrase has been encountered before. see it this command rates
				foreach (selectionRecord record in check.selectionRecords) {
					if (record.cmd == command) 
						return (float)(record.timesSelected) / check.numSelections;
				}
				return 0; // the phrase was a hit, but this command has never been chosen
				break;
			}
			/*
			foreach (SelectionWeights check1 in selectionLogs) {
				if (inputPhrase.ToLower ().Contains (check1.inputPhrase)) { // contains might be expensive here
					// the phrase has been encountered before. see it this command rates
					foreach (selectionRecord record in check1.selectionRecords) {
						if (record.cmd == command) 
							return 0.75f * (float)(record.timesSelected) / check1.numSelections; // might want to rate a bit lower for contains
					}
					return 0; // the phrase was a hit, but this command has never been chosen
					break;
				}
			}
			*/
		}
		return -1; // the phrase was not found, so no rating
	}

	public void SaveSelectionWeightsToXML(){
		Serializer<List<SelectionWeights>> serializer = new Serializer<List<SelectionWeights>>();
		if ( serializer != null)
		{
			serializer.Save(Application.dataPath+"/Resources/XML/SelectionWeights",selectionLogs);
		}
	}

	public void LoadSelectionWeightsFromXML(){
		// load from stream reader
		Serializer<List<SelectionWeights>> serializer = new Serializer<List<SelectionWeights>>();
		selectionLogs = serializer.Load(new StreamReader(Application.dataPath+"/Resources/XML/SelectionWeights"));
	}



    public void GPErrorCallback(string data)
    {
#if ADD_NLU_RECORDS
		// save this one
		currCV.NLURecords.Add(null);
#endif
		currCV.NLUResults.Add("NLU ERROR <" + data + ">");
		
		// get next result
		currCV = NextResult();
		
		// kick off next one
		if ( currCV != null )
		{
        	NluMgr.GetInstance().Utterance(currCV.CV.Variations[currVariationIdx], "nurse");
		}
		else
		{
			// we're done, save the file
			Serializer<List<CommandVariationResult>> serializer = new Serializer<List<CommandVariationResult>>();
			serializer.Save("CommandVariationResult.xml",CVResults);
		}
	}	
	
    public void GPCallback(NluMgr.match_record record)
    {
        // make to upper case
        record.sim_command = record.sim_command.ToUpper();

        string text = "NLU : sim_command=<" + record.sim_command + ">";
        // subject 
        if (record.command_subject != null && record.command_subject != "")
            text += " : s=<" + record.command_subject + ">";
        // params
        foreach (NluMgr.sim_command_param param in record.parameters)
            text += " : p=<" + param.name + "," + param.value + ">";
        // missing params
        foreach (NluMgr.missing_sim_command_param param in record.missing_parameters)
            text += " : m=<" + param.name + ">";
        // readback
        if (record.readback != null && record.readback != "")
            text += " : r=<" + record.readback + ">";
        // feedback
        if (record.feedback != null && record.feedback != "")
            text += " : f=<" + record.feedback + ">";
		
		// save this one
#if ADD_NLU_RECORDS
		currCV.NLURecords.Add(record);
#endif
		if ( record.input != currCV.CV.Variations[currVariationIdx] )
			currCV.NLUResults.Add(text + " : input=<" + record.input + "> doesn't match Variation");
		else
			currCV.NLUResults.Add(text + " : input=<" + record.input + ">");
		
		// get next result
		currCV = NextResult();
		
		// kick off next one
		if ( currCV != null )
		{
        	NluMgr.GetInstance().Utterance(currCV.CV.Variations[currVariationIdx], "nurse");
		}
		else
		{
			// we're done, save the file
			Serializer<List<CommandVariationResult>> serializer = new Serializer<List<CommandVariationResult>>();
			serializer.Save("CommandVariationResult.xml",CVResults);
		}
    }	
}


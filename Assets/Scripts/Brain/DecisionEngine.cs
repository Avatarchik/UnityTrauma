//#define DEBUG_DECISION_ENGINE

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
/*
public class DecisionMapNew
{
    public string Name;
    public string StartState;
    public float Interval;
    public List<DecisionStateNew> DecisionStates;
    public DecisionState Current;
    public bool Finished;

    public float CheckTime = 0.0f;
	
	public DecisionMapNew(){}
	
	public DecisionMapNew(DecisionMap old)
	{
		Name = old.Name;
		StartState = old.StartState;
		Interval = old.Interval;
		DecisionStates = new List<DecisionStateNew>();
		
		foreach (DecisionState ods in old.DecisionStates){
			DecisionStateNew ds = new DecisionStateNew();
		 	ds.Name = ods.Name;
			ds.Transitions = new List<DecisionStateTransition>();
			DecisionStateTransition dst = new DecisionStateTransition();
			dst.DecisionAction = ods.DecisionAction;
			dst.DecisionCondition = ods.DecisionCondition;
			dst.SuccessState = ods.SuccessState;
			ds.Transitions.Add (dst);
			DecisionStates.Add(ds);
		}
	}
}
*/

public class DecisionMap
{
    public string Name;
    public string StartState;
    public float Interval;
    public List<DecisionState> DecisionStates;
    public DecisionState Current;
    public bool Finished;

    public float CheckTime = 0.0f;

    public void Init()
    {
        foreach (DecisionState state in DecisionStates)
            state.Init();

        Current = FindState(StartState);
        Finished = false;
    }

    public DecisionState FindState(string name)
    {
        foreach (DecisionState state in DecisionStates)
        {
            if (state.Name == name)
                return state;
        }
        return null;
    }

    public void Evaluate( float elapsedTime )
    {
        // don't do evaluation if we're finished with this map
        if (Finished == true)
            return;

        if (elapsedTime > CheckTime)
        {
            //UnityEngine.Debug.Log("DecisionMap.Evaluate() : Evaluate map= <" + Name + ">");

            // set next time
            CheckTime = elapsedTime + Interval;
            // process
            if (Current != null)
            {
				foreach (DecisionStateTransition dst in Current.Transitions)
                if (dst.DecisionCondition.Test() == true)
                {
                    // execute actions
					if (dst.DecisionAction != null)
                    	dst.DecisionAction.Execute();

                    // move to next state
                    DecisionState next = FindState(dst.SuccessState);
                    if (next != null)
                    {
                        UnityEngine.Debug.Log("DecisionMap.Evaluate(MAP=" + Name + ") : Move to state <" + next.Name + ">");
                        Current = next;
                    }
                    else
                    {
                        // no next state, just set this state as finished
                        Finished = true;
                    }
                }
            }
        }
    }
}

public class DecisionMgr
{
    static DecisionMgr instance;
    public static DecisionMgr GetInstance()
    {
        if (instance == null)
            instance = new DecisionMgr();
        return instance;
    }

    public DecisionMgr()
    {
        Maps = new List<DecisionMap>();
        Interactions = new List<InteractStatusMsg>();
    }

    public List<DecisionMap> Maps;

    public void Init()
    {
        Maps = new List<DecisionMap>();
        Interactions = new List<InteractStatusMsg>();
    }

    public void LoadXML(string filename)
    {
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("DecisionMgr.LoadXML(" + filename + ")");
#endif

        Serializer<DecisionMap> serializer = new Serializer<DecisionMap>();
        DecisionMap map = serializer.Load(filename);
        if (map != null)
        {
            Maps.Add(map);
            map.Init();
/*			
			// save out in the new format
			DecisionMapNew made = new DecisionMapNew(map);
			Serializer<DecisionMapNew> serializernew = new Serializer<DecisionMapNew>();
			string outfile = "Assets/Resources/"+filename.Replace(".xml","")+"_new.xml";
        	serializernew.Save(outfile,made);
*/
			
        }
    }

    float elapsedTime;

    public void Update()
    {
        elapsedTime += Time.deltaTime;
        EvaluateMaps();
        Execute();
    }

    public void EvaluateMaps()
    {
        foreach (DecisionMap map in Maps)
        {
            map.Evaluate(elapsedTime);
        }
    }

    public class QueueItem
    {
        public QueueItem(string item, float time)
        {
            this.item = item;
            this.time = time;
        }
        public string item;
        public float time;

        public bool Timeout()
        {
            if (Time.time > time)
                return true;
            else
                return false;
        }
    }

    List<QueueItem> ActionQueue;

    public void Execute()
    {
        List<QueueItem> removeList = new List<QueueItem>();

        if (ActionQueue != null)
        {
            // execute
            foreach( QueueItem qi in ActionQueue )
            {
                if (qi.Timeout())
                {
                    DecisionAction.Parse(qi.item);
                    removeList.Add(qi);
                }
            }
            // remove items
            foreach (QueueItem qi in removeList)
            {
                ActionQueue.Remove(qi);
            }
        }
    }

    public void QueueAction(string action, float time)
    {
        if (ActionQueue == null)
            ActionQueue = new List<QueueItem>();
        ActionQueue.Add(new QueueItem(action, time));
    }

    public List<InteractStatusMsg> Interactions;

    public void PutMessage(GameMsg msg)
    {
        InteractStatusMsg ismsg = msg as InteractStatusMsg;
        if (ismsg != null)
        {
#if CHECK_VALID_INTERACTION
            // check for valid map
            if (ismsg.InteractMap == null)
            {
                // can't find it, look it up
                ismsg.InteractMap = InteractionMgr.GetInstance().Get(ismsg.InteractName);
                if (ismsg.InteractMap == null)
                {
#if DEBUG_DECISION_ENGINE
                    UnityEngine.Debug.Log("DecisionMgr.PutMessage() : InteractMap = null. InteractName=" + ismsg.InteractName);
#endif
                    return;
                }
            }
#endif

#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionMgr.PutMessage(" + ismsg.InteractName + ")");
#endif
            // add msg
            Interactions.Add(ismsg);
            // evaluate immediately
            EvaluateMaps();
        }
    }
}
/*
public class DecisionStateNew
{
    public string Name;
	public List<DecisionStateTransition> Transitions;
}
*/

public class DecisionStateTransition
{
	public DecisionCondition DecisionCondition;
    public DecisionAction DecisionAction;
    public string SuccessState;
}

public class DecisionState
{
    public string Name;
	public List<DecisionStateTransition> Transitions;
    //public List<DecisionCondition> Conditions;
    //public List<DecisionAction> Actions;
//    public DecisionCondition DecisionCondition;
//    public DecisionAction DecisionAction;
//    public string SuccessState;

    public void Init()
    {
		foreach (DecisionStateTransition dst in Transitions)
        	dst.DecisionCondition.Init();
    }
/*
    public bool Test()
    {
        return DecisionCondition.Test();
    }

    public void Execute()
    {
        DecisionAction.Execute();
    }
*/
}

public class DecisionVariable
{
    public string Object;
    public string Variable;

    bool valid;
    public bool Valid
    {
        get { return valid; }
        set { valid = value; }
    }

    FieldInfo fieldInfo;
    PropertyInfo propInfo;
    BaseObject baseObject;

    public DecisionVariable(string objvar, bool lowercase=false)
    {
        // split command into
        var tokens = objvar.Split('.');
        if (tokens.Count() == 2)
        {
            Object = tokens[0];
            Variable = tokens[1];
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable);
#endif

            valid = Assign(lowercase);
        }
    }

    public string Get()
    {
        if (GetType() == typeof(System.Boolean))
        {
            return this.GetBoolean().ToString();
        }
        if (GetType() == typeof(System.Int32))
        {
            return this.GetInt().ToString();
        }
        if (GetType() == typeof(System.Single))
        {
            return this.GetFloat().ToString();
        }
        if (GetType() == typeof(System.String))
        {
            return this.GetString();
        }
        return "DecisionVariable.Get() : Bad Type";
    }

    public char GetTypeCharacter()
    {
        if (GetType() == typeof(System.Boolean))
        {
            return 'b';
        }
        if (GetType() == typeof(System.Int32))
        {
            return 'i';
        }
        if (GetType() == typeof(System.Single))
        {
            return 'f';
        }
        if (GetType() == typeof(System.String))
        {
            return 's';
        }
        return 'E';
    }

    public void Set(string value)
    {
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : Type(" + GetType() + ")");
#endif

        if (GetType() == typeof(System.Single))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : SetFloat(" + value + ")");
#endif
            this.SetFloat(float.Parse(value));
        }
        if (GetType() == typeof(System.String))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : SetString(" + value + ")");
#endif
            this.SetString(value);
        }
        if (GetType() == typeof(System.Boolean))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : SetBoolean(" + value + ")");
#endif
            this.SetBoolean(bool.Parse(value));
        }
        if (GetType() == typeof(System.Int32))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : SetInt(" + value + ")");
#endif
            this.SetInt(int.Parse(value));
        }
    }

    public void Inc(string value)
    {
        if ( GetType() == typeof(System.Single))
        {
            this.SetFloat(this.GetFloat()+Convert.ToSingle(value));
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : Inc(" + this.GetFloat() + ")");
#endif
        }
        else
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : can't Inc, value not float");
#endif
        }
    }

    public void Dec(string value)
    {
        if ( GetType() == typeof(System.Single))
        {
            this.SetFloat(this.GetFloat()-Convert.ToSingle(value));
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : Dec(" + this.GetFloat() + ")");
#endif
        }
        else
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable : Object=" + Object + " : Variable=" + Variable + " : can't Inc, value not float");
#endif
        }
    }

    public bool Test(string condition)
    {
        return false;
    }

    public bool Test(string constant, string condition)
    {
        if (GetType() == typeof(System.Single))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable.Test(" + constant + "," + condition + ") : value=" + this.GetFloat() + " : constant=" + float.Parse(constant));
#endif
            switch (condition)
            {
			case "=":
            case "equal":
                 return (this.GetFloat() == float.Parse(constant));
			case "!=":
            case "notequal":
                return (this.GetFloat() != float.Parse(constant));
			case "<":
            case "less":
                return (this.GetFloat() < float.Parse(constant));
			case ">":
            case "greater":
                return (this.GetFloat() > float.Parse(constant));
			default:
               return false;
            }
        }
        if (GetType() == typeof(System.Boolean))
        {
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("DecisionVariable.Test(" + constant + "," + condition + ") : value=" + this.GetBoolean() + " : constant=" + Boolean.Parse(constant));
#endif
            bool status;

            switch (condition)
            {
			case "=":
			case "equal":
                status = (this.GetBoolean() == Boolean.Parse(constant));
#if DEBUG_DECISION_ENGINE
                UnityEngine.Debug.Log("DecisionVariable.Test(" + constant + "," + condition + ") result=<" + status + ">");
#endif
                return status;
			case "!=":
            case "notequal":
                status = (this.GetBoolean() != Boolean.Parse(constant));
#if DEBUG_DECISION_ENGINE
                UnityEngine.Debug.Log("DecisionVariable.Test(" + constant + "," + condition + ") result=<" + status + ">");
#endif
                return status;
            }
        }
        if (GetType() == typeof(System.String))
        {
            return (this.GetString() == constant);
        }
        return false;
    }

    public bool Test(DecisionVariable var2, string condition)
    {
        if (GetType() == typeof(System.Single))
        {
            switch (condition)
            {
			case "=":
            case "equal":
                return (this.GetFloat() == var2.GetFloat());
			case "!=":
            case "notequal":
                return (this.GetFloat() != var2.GetFloat());
			case "<":
            case "less":
                return (this.GetFloat() < var2.GetFloat());
			case ">":
            case "greater":
                return (this.GetFloat() > var2.GetFloat());
            default:
                return false;
            }
        }
        if (GetType() == typeof(System.Single))
        {
            return (this.GetString() == var2.GetString());
        }
        return false;
    }

    public Type GetType()
    {
        if (fieldInfo != null)
            return fieldInfo.FieldType;
        else if (propInfo != null)
            return propInfo.PropertyType;
        else
            return null;
    }

    public string GetVariableFromLowerCase(string name)
    {
        string match = "";

        baseObject = ObjectManager.GetInstance().GetBaseObject(Object);
        if (baseObject != null)
        {
            // incoming is lower case, check for matches
            Type type = baseObject.GetType();
            if (type != null)
            {
                // try to match all fields
                foreach (FieldInfo f in type.GetFields())
                {
                    if (f.Name.ToLower() == name.ToLower())
                        return f.Name;
                }
                // try to match all fields
                foreach (PropertyInfo p in type.GetProperties())
                {
                    if (p.Name.ToLower() == name.ToLower())
                    {
#if DEBUG_DECISION_ENGINE
                        UnityEngine.Debug.LogError("GetVariableFromLowerCase() : found property=" + p.Name);                           
#endif
                        return p.Name;
                    }
                }
            }
        }
        return match;
    }

    public bool Assign( bool checkLowercase )
    {
        // get object from ObjectManager
        baseObject = ObjectManager.GetInstance().GetBaseObject(Object);
        if (baseObject != null)
        {
            Type type = baseObject.GetType();
            if (type != null)
            {
                if ( checkLowercase == true )
				{
					string name = GetVariableFromLowerCase(Variable);
                    fieldInfo = type.GetField(name);
				} else
                    fieldInfo = type.GetField(Variable);

                if (fieldInfo != null)
                {
#if DEBUG_DECISION_ENGINE
                    if (fieldInfo.FieldType == typeof(System.Single))
                    {
                        UnityEngine.Debug.Log("DecisionVariable.Assign() : Value <FLOAT:" + Object + "." + Variable + ">=" + GetFloat());
                    }
                    if (fieldInfo.FieldType == typeof(System.String))
                    {
                        UnityEngine.Debug.Log("DecisionVariable.Assign() : Value <STRING:" + Object + "." + Variable + ">=" + GetString());
                    }
                    if (fieldInfo.FieldType == typeof(System.Boolean))
                    {
                        UnityEngine.Debug.Log("DecisionVariable.Assign() : Value <BOOLEAN:" + Object + "." + Variable + ">=" + GetBoolean());
                    }
                    if (fieldInfo.FieldType == typeof(System.Int32))
                    {
                        UnityEngine.Debug.Log("DecisionVariable.Assign() : Value <INT:" + Object + "." + Variable + ">=" + GetInt());
                    }
#endif
                    return true;
                }
                else
                {
                    // check for Property (get/set)
                    if ( checkLowercase == true )
					{
						string name = GetVariableFromLowerCase(Variable);
                        propInfo = type.GetProperty(name);
					}
                    else
					{
                        propInfo = type.GetProperty(Variable);
					}

                    if (propInfo == null)
					{
#if DEBUG_DECISION_ENGINE
                        UnityEngine.Debug.LogError("DecisionVariable.Assign() : Can't GetField or GetMethod=" + Object + "." + Variable);
#endif
					}
                    else
                    {
#if DEBUG_DECISION_ENGINE
                        UnityEngine.Debug.Log("DecisionVariable.Assign() : Variable is member=" + Object + "." + Variable);
#endif
						return true;
                    }
                }
            }
            else
                UnityEngine.Debug.LogError("DecisionVariable.Assign() : Can't GetType=" + Object);
        }
        else
            UnityEngine.Debug.LogError("DecisionVariable.Assign() : Can't find Object=" + Object);

        return false;
    }

    public int GetInt()
    {
        if (fieldInfo != null)
        {
#if DEBUG_DECISION_ENGINE
            //UnityEngine.Debug.Log("GetFloat() : fieldInfo.GetValue()=" + (float)fieldInfo.GetValue(baseObject));
#endif
            return (int)fieldInfo.GetValue(baseObject);
        }
        if (propInfo != null)
        {
#if DEBUG_DECISION_ENGINE
            //UnityEngine.Debug.Log("GetFloat() : propInfo.GetValue()=" + (float)propInfo.GetValue(baseObject,null));
#endif
            return (int)propInfo.GetValue(baseObject, null);
        }
        UnityEngine.Debug.LogError("DecisionVariable.GetInt(" + Object + "." + Variable + ") : no field or prop info!");
        return 0;
    }

    public float GetFloat()
    {
        if (fieldInfo != null)
        {
#if DEBUG_DECISION_ENGINE
            //UnityEngine.Debug.Log("GetFloat() : fieldInfo.GetValue()=" + (float)fieldInfo.GetValue(baseObject));
#endif
            return (float)fieldInfo.GetValue(baseObject);
        }
        if (propInfo != null)
        {
#if DEBUG_DECISION_ENGINE
            //UnityEngine.Debug.Log("GetFloat() : propInfo.GetValue()=" + (float)propInfo.GetValue(baseObject,null));
#endif
            return (float)propInfo.GetValue(baseObject, null);
        }
        UnityEngine.Debug.LogError("DecisionVariable.GetInt(" + Object + "." + Variable + ") : no field or prop info!");
        return 0.0f;
    }

    public bool GetBoolean()
    {
        if (fieldInfo != null)
        {
            return (bool)fieldInfo.GetValue(baseObject);
        }
        if (propInfo != null)
        {
            return (bool)propInfo.GetValue(baseObject, null);
        }
        UnityEngine.Debug.LogError("DecisionVariable.GetInt(" + Object + "." + Variable + ") : no field or prop info!");
        return false;
    }

    public string GetString()
    {
        if (fieldInfo != null)
            return (string)fieldInfo.GetValue(baseObject);
        if (propInfo != null)
            return (string)propInfo.GetValue(baseObject,null);

        UnityEngine.Debug.LogError("DecisionVariable.GetInt(" + Object + "." + Variable + ") : no field or prop info!");
        return "null";
    }

    public void SetFloat(float val)
    {
        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(System.Single))
            {
                float before = (float)fieldInfo.GetValue(baseObject);
                fieldInfo.SetValue(baseObject, val);
                UnityEngine.Debug.Log("SetFloat(" + val + ") : fieldInfo.FieldType=" + fieldInfo.FieldType + " : before=" + before + " : after=" + fieldInfo.GetValue(baseObject));
            }
        }
        if (propInfo != null)
        {
            if (propInfo.PropertyType == typeof(System.Single))
            {
                float before = (float)propInfo.GetValue(baseObject,null);
                try
                {
                    propInfo.SetValue(baseObject, val, null);
                    UnityEngine.Debug.Log("SetFloat(" + val + ") : propInfo.PropertyType=" + propInfo.PropertyType + " : before=" + before + " : after=" + propInfo.GetValue(baseObject, null));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("exception:" + ex.Message);
                }
            }
        }
    }

    public void SetBoolean(bool val)
    {
        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(System.Boolean))
            {
                fieldInfo.SetValue(baseObject, val);
            }
        }
        if (propInfo != null)
        {
            if (propInfo.PropertyType == typeof(System.Boolean))
            {
                bool before = (bool)propInfo.GetValue(baseObject, null);
                try
                {
                    propInfo.SetValue(baseObject, val, null);
                    UnityEngine.Debug.Log("SetBoolean(" + val + ") : propInfo.PropertyType=" + propInfo.PropertyType + " : before=" + before + " : after=" + propInfo.GetValue(baseObject, null));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("exception:" + ex.Message);
                }
            }
        }
    }

    public void SetInt(int val)
    {
        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(System.Int32))
            {
                fieldInfo.SetValue(baseObject, val);
            }
        }
        if (propInfo != null)
        {
            if (propInfo.PropertyType == typeof(System.Int32))
            {
                int before = (int)propInfo.GetValue(baseObject, null);
                try
                {
                    propInfo.SetValue(baseObject, val, null);
                    UnityEngine.Debug.Log("SetInt(" + val + ") : propInfo.PropertyType=" + propInfo.PropertyType + " : before=" + before + " : after=" + propInfo.GetValue(baseObject, null));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("exception:" + ex.Message);
                }
            }
        }
    }

    public void SetString(string val)
    {
        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(System.String))
            {
                float before = (System.Single)propInfo.GetValue(baseObject, null);
                fieldInfo.SetValue(baseObject, val);
#if DEBUG_DECISION_ENGINE
                UnityEngine.Debug.Log("DecisionVariable(" + Variable + ") before=<" + before + "> after=<" + propInfo.GetValue(baseObject, null) + ">");
#endif
            }
        }
        if (propInfo != null)
        {
            if (propInfo.PropertyType == typeof(System.String))
            {
                string before = (System.String)propInfo.GetValue(baseObject, null);
                propInfo.SetValue(baseObject, val, null);
#if DEBUG_DECISION_ENGINE
                UnityEngine.Debug.Log("DecisionVariable(" + Variable + ") before=<" + before + "> after=<" + propInfo.GetValue(baseObject, null) + ">");
#endif
            }
        }
    }
}

public class DecisionParser
{
    public static string GetArg(string strval, string arg)
    {
        if (strval.Contains(arg))
        {
            string[] tokens = strval.Split(' ');
            for (int i = 0; i < tokens.Count(); i++)
            {
                if (tokens[i].Contains(arg) && tokens[i].Contains(":"))
                {
					// get index of value after :
					int idx = tokens[i].IndexOf(":") + 1;
					// return substring of everything after :
					return tokens[i].Substring(idx,tokens[i].Length-idx);
                }
            }
            return null;
        }
        else
            return null;
    }

    // gets string behind "arg".  String is quoted like this >> text:"this is some text"
    public static string GetString(string strval, string arg)
    {
        if (strval.Contains(arg))
        {
            int arg_idx;
            string substr;

            // get position of arg in strval
            arg_idx = strval.IndexOf(arg);
            // get substring for this arg
            substr = strval.Substring(arg_idx);
            // get start of string
            arg_idx = substr.IndexOf(":");
            // get substring again
            substr = substr.Substring(arg_idx + 1);
            // get char array
            char[] carray = substr.ToCharArray();
            // get string
            int start = -1, end = -1;
            for (int i = 0; i < substr.Length; i++)
            {
                if (start == -1 && carray[i] == '\"')
                    start = i+1;
                else if (end == -1 && carray[i] == '\"')
                    end = i;
            }
            // return quoted string
            if (start != -1 && end != -1)
                return substr.Substring(start, end - start);
        }
        return "";
    }

    public static DecisionVariable GetVar(string condition, string var)
    {
        string arg = GetArg(condition, var);
        if (arg != null)
        {
            DecisionVariable variable = new DecisionVariable(arg);
            // return var
            return variable;
        }
        return null;
    }
}

public abstract class ConditionTest : DecisionParser
{
    protected string input;

    public string Name;

    public abstract bool Parse(string condition);
    public abstract bool Test();

    public abstract void Debug();
}

public class ConditionEvent : ConditionTest
{
    public string Interact;
    public bool delete;

    public ConditionEvent()
    {
        delete = true;
    }

    public override bool Test()
    {
        // check log for this event
        foreach (InteractStatusMsg msg in DecisionMgr.GetInstance().Interactions)
        {
#if DEBUG_DECISION_ENGINE
            //UnityEngine.Debug.Log("ConditionEvent.Test(" + Interact + ") : InteractStatusMsg=<" + msg.InteractMap.item + ">");
#endif
            if (msg.InteractName == Interact)
            {
                if (delete == true)
                {
                    // remove it
                    DecisionMgr.GetInstance().Interactions.Remove(msg);
                }
#if DEBUG_DECISION_ENGINE
                //UnityEngine.Debug.Log("ConditionEvent.Test(" + Interact + ") : result=true");
#endif
                return true;
            }
        }
        return false;
    }

    public override bool Parse(string strval)
    {
        input = strval;

        // strip off event:
        if (strval.Contains("interact"))
        {
            Interact = GetString(strval, "interact");
            if (strval.Contains("nodelete"))
            {
                delete = false;
            }
#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("ConditionEvent.Parse: Interact=<" + Interact + "> : delete=" + delete);
#endif
       }
        return true;
    }

    public override void Debug()
    {
        UnityEngine.Debug.Log("ConditionEvent() : input=" + input);
    }
}

public class ConditionTestVar1 : ConditionTest
{
    DecisionVariable variable;
    string constant;
    string condition;

    public override bool Test()
    {
        return variable.Test(constant, condition);
    }

    public override bool Parse(string strval)
    {
        input = strval;

        // get var1
        variable = GetVar(strval, "var");
        if (variable == null)
            return false;

        // get constant
        string tmp = GetArg(strval, "constant");
        if (tmp != null)
            constant = tmp;
        else
            return false;

        // get condition
        condition = GetArg(strval, "condition");
        if (condition == null)
            return false;

        return true;
    }

    public override void Debug()
    {
        UnityEngine.Debug.Log("ConditionTestVar1() : input=" + input);
    }
}

public class ConditionTestVar2 : ConditionTest
{
    DecisionVariable var1;
    DecisionVariable var2;
    string condition;

    public override bool Test()
    {
        return var1.Test(var2,condition);
    }

    public override bool Parse(string strval)
    {
        input = strval;

        // get var1
        var1 = GetVar(strval, "var1");
        var2 = GetVar(strval, "var2");
        if (var1 == null || var2 == null)
            return false;

        // get condition
        condition = GetArg(strval, "condition");

        return true;
    }

    public override void Debug()
    {
        UnityEngine.Debug.Log("ConditionTestVar2() : input=" + input);
    }
}

public class DecisionCondition
{
    public string Name;
    public List<string> Tests;

    List<ConditionTest> ConditionTests;

    public DecisionCondition()
    {
        ConditionTests = new List<ConditionTest>();
    }

    public void Init()
    {
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("Condition.Init() : Tests=" + Tests.Count);
#endif

        foreach (string test in Tests)
        {
            Parse(test);
        }
    }

    public void Debug()
    {
        foreach (string test in Tests)
        {
            UnityEngine.Debug.Log("DecisionCondition(" + Name + ") : condition=<" + test + ">");
        }
    }

    public virtual bool Test()
    {
        foreach (ConditionTest test in ConditionTests)
        {
            if (test.Test() == false)
                return false;
        }
        return true;
    }

    public void Parse(string condition)
    {
        // var1:object.variable condition:= var2:object.variable
        // var1:object.variable condition:= const:0.5
        // interact:INTERACTMAP
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("Condition.Parse(" + condition + ")");
#endif

        // TWO VARS
        if (condition.Contains("var1") && condition.Contains("var2"))
        {
            ConditionTestVar2 ct = new ConditionTestVar2();
            if (ct.Parse(condition) == true)
            {
#if DEBUG_DECISION_ENGINE
                ct.Debug();
#endif
                ConditionTests.Add(ct);
            }
        }
        // ONE VAR 
        else if (condition.Contains("var"))
        {
            ConditionTestVar1 ct = new ConditionTestVar1();
            if (ct.Parse(condition) == true)
            {
#if DEBUG_DECISION_ENGINE
                ct.Debug();
#endif
                ConditionTests.Add(ct);
            }
        }
        else if (condition.Contains("interact:"))
        {
            ConditionEvent ct = new ConditionEvent();
            if (ct.Parse(condition) == true)
            {
#if DEBUG_DECISION_ENGINE
                ct.Debug();
#endif
                ConditionTests.Add(ct);
            }
        }
        /*
        // LOG
        else if (condition.Contains("log"))
        {
            ConditionTestLog ct = new ConditionTestLog();
            if (ct.Parse(condition) == true)
            {
                Test.Add(ct);
            }
        }
         * */
    }
}

public class DecisionAction : DecisionParser
{
    public string Name;
    public List<string> Actions;

    public DecisionAction()
    {
    }
	
    public void Execute()
    {
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("Action.Execute() : Actions=" + Actions.Count);
#endif

        foreach (string action in Actions)
        {
            float delay = 0.0f;
            if (action.Contains("delay"))
            {
                UnityEngine.Debug.Log("DecisionAction.Execute() : action <" + action + "> has delay");
                try
                {
                    delay = Convert.ToSingle(GetArg(action, "delay"));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("DecisionAction.Execute() : Format error in action <" + action + ">");
                }
            }
            DecisionMgr.GetInstance().QueueAction(action,Time.time+delay);
        }
    }

    public static void Parse(string action)
    {
        // var:object.variable equals:0.5 .. sets equal
        // var:object.variable dec:0.5 .. decrements by .5
#if DEBUG_DECISION_ENGINE
        UnityEngine.Debug.Log("Action.Parse(" + action + ")");
#endif
        if (action.Contains("var") && action.Contains("equals"))
        {
            DecisionVariable variable = GetVar(action, "var");
            if (variable != null)
            {
                // get value of operator
                string val = GetArg(action,"equals");
                if (val != null)
                {
#if DEBUG_DECISION_ENGINE
                    UnityEngine.Debug.Log("Action.Parse(equals:" + action + ") : variable=" + variable.Object + "." + variable.Variable + " : value=" + variable.Get());
#endif
                    variable.Set(val);
                }
            }
        }

        // var:object.variable inc:0.5 .. increments by .5
        if (action.Contains("var") && action.Contains("inc"))
        {
            DecisionVariable variable = GetVar(action, "var");
            if (variable != null)
            {
                // get value of operator
                string val = GetArg(action,"inc");
                if (val != null)
                {
#if DEBUG_DECISION_ENGINE
                    UnityEngine.Debug.Log("Action.Parse(inc:" + action + ") : variable=" + variable.Object + "." + variable.Variable + " : value=" + val);
#endif
                    variable.Inc(val);
                }
            }
        }

        if (action.Contains("var") && action.Contains("dec"))
        {
            DecisionVariable variable = GetVar(action, "var");
            if (variable != null)
            {
                // get value of operator
                string val = GetArg(action,"dec");
                if (val != null)
                {
#if DEBUG_DECISION_ENGINE
                    UnityEngine.Debug.Log("Action.Parse(dec:" + action + ") : variable=" + variable.Object + "." + variable.Variable + " : value=" + val);
#endif
                    variable.Dec(val);
                }
            }
        }

        // var:object interact:MSG:EXAMPLE
        if (action.Contains("var") && action.Contains("interact"))
        {
            string objname = GetArg(action, "var");
            string interaction = GetString(action, "interact");

#if DEBUG_DECISION_ENGINE
            UnityEngine.Debug.Log("Action.Parse(interact:" + action + ") : objname=" + objname + " : interaction=" + interaction);
#endif
            ObjectInteraction objint = ObjectManager.GetInstance().GetBaseObject(objname) as ObjectInteraction;
            if (objint != null)
            {
                InteractionMap map = InteractionMgr.GetInstance().Get(interaction);
                if (map != null)
                {
                    // send interaction
                    InteractMsg imsg = new InteractMsg(objint.gameObject,map,true);
                    objint.PutMessage(imsg);
                }
            }
        }

        if (action.Contains("status:"))
        {
            string status = GetString(action, "status");
            if (status != null)
            {
                InteractStatusMsg ismsg = new InteractStatusMsg(status);
                Brain.GetInstance().PutMessage(ismsg);
            }
        }
		
#if CHANGE_STATE
		if (action.Contains("state:"))
		{
			string state = GetArg(action,"state");
			if ( state != null )
			{
				DecisionState newstate = Parent.FindState(state);
				if ( newstate != null )
				{
					Parent.Current = newstate;
				}
			}
		}
#endif

        // changestate:BRAINSTATE
        if (action.Contains("changestate"))
        {
            string statename = GetArg(action, "changestate");
            if (statename != null)
            {
                ChangeStateMsg msg = new ChangeStateMsg(statename);
                Brain.GetInstance().PutMessage(msg);
            }
        }

        // sound effect
        if (action.Contains("audio:"))
        {
            string audio = GetString(action, "audio");
            if (audio != null)
            {
                Brain.GetInstance().PlayAudio(audio);
            }
        }

        // dialog title:"title" text:"text" timeout:seconds
        if (action.Contains("dialog"))
        {
            string text="none", title="title";
            float time=0.0f;

            if (action.Contains("title"))
            {
                title = GetString(action, "title");
            }
            if (action.Contains("text"))
            {
                text = GetString(action, "text");
            }
            if (action.Contains("timeout"))
            {
                time = Convert.ToSingle(GetArg(action, "timeout"));
            }
            QuickInfoMsg msg = new QuickInfoMsg();
            msg.command = DialogMsg.Cmd.open;
            msg.title = title;
            msg.text = text;
            msg.timeout = time;
            QuickInfoDialog.GetInstance().PutMessage(msg);
        }
    }
}


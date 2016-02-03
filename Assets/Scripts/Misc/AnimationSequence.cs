using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

public class Parameter {
    public string type;
    public string value;
}

public class AnimationPackage
{
    public int ID;
    public string target;
    public string script;
    public string function;
    public bool simultaneous;
    public int nextAction;
    public string origin;
    public bool lockOut;
    public bool getBuffer;

    public List<Parameter> ParameterList;

    public AnimationPackage()
    {
    }

    public AnimationPackage(int ID, string target, string script, string function, List<Parameter> ParameterList, bool simultaneous, int nextAction, string origin, bool lockOut, bool getBuffer)
    {
        this.ID = ID;
        this.target = target;
        this.script = script;
        this.function = function;
        this.ParameterList = ParameterList;
        this.simultaneous = simultaneous;
        this.nextAction = nextAction;
        this.origin = origin;
        this.lockOut = lockOut;
        this.getBuffer = getBuffer;
    }
}

public class AnimationSequence
{
    int location = 0;
    public bool animating = false;
    bool waiting = false;
    string origin = null;
    float buffer = 0f;
    string delayed = "";
    bool delay = false;
    float delayTime = 0f;

    public List<AnimationPackage> packages;

    public AnimationSequence(string filename)
    {
        LoadXML(filename);
    }

    public void DebugAnims()
    {
        if (packages != null)
        {
            Debug.LogWarning("AnimationSequenceController: Debug:");
            foreach (AnimationPackage ap in packages)
            {
                Debug.Log("ID: " + ap.ID);
                Debug.Log("--Target GameObject: " + ap.target);
                Debug.Log("--Script: " + ap.script);
                Debug.Log("--Function: " + ap.function);
                foreach (Parameter p in ap.ParameterList)
                {
                    Debug.Log("----Next Parameter: " + p.value + " of type " + p.type);
                }
                Debug.Log("--Simultaneous? " + ap.simultaneous);
                Debug.Log("--Next ID: " + ap.nextAction);
                Debug.Log("--String to begin next anim: " + ap.origin);
                Debug.Log("--Lock Out? " + ap.lockOut);
                Debug.Log("---------------------------------------");
            }
        }
    }

    public void LoadXML(string filename)
    {
        Debug.Log("AnimationSequenceController: Loading file'" + filename + "'");
        Serializer<List<AnimationPackage>> serializer = new Serializer<List<AnimationPackage>>();
        packages = serializer.Load(filename);

        //DebugAnims();
    }

    public void Begin()
    {
        animating = true;
    }

    public bool Update()
    {

        if (delay)
        {
            if (Time.time >= delayTime)
            {
                delay = false;
                NextAnim(delayed);
            }
            else
                return true;
        }

        if (!animating)
            return true;

        if (animating && packages != null)
        {
            if (location == 0)
            {
                //Lock out camera movement and interactable objects if flagged
                if (packages[0].lockOut)
                {
                    ObjectInteractionMgr.GetInstance().Highlight = false;
                    ObjectInteractionMgr.GetInstance().Clickable = false;
                    GameObject.FindGameObjectWithTag("MainCamera").GetComponent<NewMouseLook>().enabled = false;
                }
            }
            
            //Break out and reset if end is found
            if (packages[location].target == "END")
            {
                Debug.Log("AnimationSequenceController: Animation sequence complete!");

                ObjectInteractionMgr.GetInstance().Highlight = true;
                ObjectInteractionMgr.GetInstance().Clickable = true;
                GameObject.FindGameObjectWithTag("MainCamera").GetComponent<NewMouseLook>().enabled = true;

                animating = false;
                this.origin = "";
                return false;
            }

            AnimationPackage current = packages[location];

            //Set the target to the GameObject we want to animate
            GameObject target = GameObject.Find(current.target);

            Debug.Log("AnimationSequenceController: Searching for function " + current.function + " in script " + current.script + " on object " + current.target);

            //Grab the function in the correct script
            MethodInfo function = target.GetComponent(current.script).GetType().GetMethod(current.function);

            if (function != null)
            {
                Debug.Log("AnimationSequenceController: " + function);
                Debug.Log("AnimationSequenceController: Executing function " + function + " in script " + current.script + " on object " + current.target);

                //Just run the function if there are no parameters
                if (current.ParameterList == null)
                {
                    //check to make sure the target function has no parameters
                    if (function.GetParameters().Length == 0)
                        function.Invoke(GameObject.Find(current.target).GetComponent(current.script), null);
                    else
                        Debug.LogError("AnimationSequenceController: Mismatching parameters in XML to function " + function);
                }
                else
                {
                    //create a list for the parameters of indeterminate Type
                    List<object> parameters = new List<object>();
                    string unsupported = "";
                    foreach (Parameter param in current.ParameterList)
                    {
                        //Convert and add each parameter
                        switch (param.type)
                        {
                            case "int":
                                parameters.Add(Int32.Parse(param.value));
                                break;
                            case "double":
                                parameters.Add(Double.Parse(param.value));
                                break;
                            case "float":
                                parameters.Add(Single.Parse(param.value));
                                break;
                            case "bool":
                                parameters.Add(bool.Parse(param.value));
                                break;
                            case "string":
                                parameters.Add(param.value);
                                break;
                            default:

                                unsupported = param.type;
                                break;
                        }
                    }

                    if (unsupported != "")
                        Debug.LogError("AnimationSequenceController: Unsupported parameter type '" + unsupported + "' in XML");
                    else
                    {
                        //Copy parameters into an array for transfer
                        object[] paramList = parameters.ToArray();

                        //Grab all the parameters in target function
                        ParameterInfo[] pInfo = function.GetParameters();

                        int i = 0;
                        bool matches = true;

                        //Check to make sure parameters match
                        if (pInfo.Length != paramList.Length)
                            matches = false;
                        else
                        {
                            foreach (ParameterInfo pi in pInfo)
                            {
                                if (paramList.Length < i + 1)
                                {
                                    matches = false;
                                    break;
                                }

                                if (pInfo[i].ParameterType != paramList[i++].GetType())
                                {
                                    matches = false;
                                    break;
                                }
                            }
                        }

                        if (matches)
                            function.Invoke(GameObject.Find(current.target).GetComponent(current.script), paramList);
                        else
                            Debug.LogError("AnimationSequenceController: Mismatching parameters in XML to function " + function);
                    }
                }
            }
            else
                Debug.LogError("AnimationSequenceController: Cannot find " + current.function + " in script " + current.script + " on object " + current.target);

            //Set up for next anim
            location = current.nextAction;

            //If the next anim is not supposed to start yet, set up what string we wait for
            if (!current.simultaneous) {
                animating = false;
                origin = current.origin;
            }

            //If end is not found, set up check
            waiting = true;
        }
        return true;
    }

    public void NextAnim(string origin)
    {
        Debug.Log("AnimationSequenceController: Testing '" + origin + "' against '" + this.origin + "'");
        if (waiting && this.origin == origin)
        {
            Debug.Log("AnimationSequenceController: " + origin + " and " + this.origin + " match!");
            waiting = false;
            animating = true;
        }
        else if(waiting)
            Debug.LogWarning("AnimationSequenceController: " + origin + " and " + this.origin + " dont match! Current location = " + location);
        
        //Check and apply a buffer period if we think the anim will interfere with controller states
        if (this.origin == null || packages[location].getBuffer)
            DelayCall(origin);
    }

    void DelayCall(string origin)
    {
        Debug.Log("AnimationSequenceController: Delayed");
        delayed = origin;
        delay = true;
        delayTime = Time.time + 1;
    }
}
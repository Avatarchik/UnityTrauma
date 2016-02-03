//#define DEBUG_TASK
//#define DEBUG_NODES
//#define DEBUG_TASK_HIGH
//#define DEBUG_CONDITION

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterTask 
{	
	public string characterName;            // character name doing this task   
	public string nodeName;                 // the Unity node where they need to be
	public string posture;                  // the target posture they need to be in to start animation
	public string lookAt;                   // what object to look at
    public float lookAtTime;                // how long to look at "lookAt" object
	public string animatedInteraction;      // animation for this task
    public float delay;                     // start delay for animation
}

public class TaskDecisionCondition : DecisionCondition
{
    public TaskDecisionCondition() : base()
    {
    }

    public string TaskRequest;
}

public class TaskData 
{
    // name of task, refered to by InteractionMap, InteractionList.  also used for
    // generating :COMPLETE messages on task complete
    public string name;                     
    
    // list of CharacterTasks required to execute task
    public List<CharacterTask> characterTasks;  
    
    // force execution but specified characters in CharacterTask
    public bool locked;                     

    // condition which must be met before task starts.  if condition is not met then
    // a message gets broadcast to all objects that something needs to be executed
    // not currently being used.
    public TaskDecisionCondition condition; 

    //public TaskType type;
	
	public TaskData() {
		characterTasks = new List<CharacterTask>();
		name = "";
	}

    public void Init()
    {
        if (condition != null)
        {
            UnityEngine.Debug.Log("TaskData.Init() : condition.Count=" + condition.Tests.Count);
            condition.Init();

            // if condition fails on start then send the msg immediately
            if (CheckCondition() == false)
                SendRequest();
        }
    }
	
	public void Debug()
    {
        UnityEngine.Debug.Log("TaskData : Name=" + name);
    }

    public void SendRequest()
    {
        if (condition.TaskRequest != null && condition.TaskRequest != "")
        {
            TaskRequestedMsg trmsg = new TaskRequestedMsg();
            trmsg.Name = condition.Name;
            trmsg.Request = condition.TaskRequest;
            ObjectManager.GetInstance().PutMessage(trmsg);
        }
    }

    public bool CheckCondition()
    {
        // if we don't have a condition then just return true
        if (condition == null)
            return true ;

        bool result = condition.Test();
#if DEBUG_CONDITION
        UnityEngine.Debug.Log("TaskData.CheckCondition() : result=<" + result + ">");
#endif
        return result;
    }
}

public class TaskRequestedMsg : GameMsg
{
    public string Name;
    public string Request;
}

public class Task 
{	
	public TaskData data;
	public bool ready = false;
	public string trackingName;
	
	public Task()  
    {
		data = new TaskData();
		ready = false;
	}
	
	public Task(TaskData data) 
    {
		this.data = data;
        ready = false;
	}
	
    //For each of the individual tasks in this task check that each character is ready
    public bool SetupTask()
    {
        bool isReady = true;

        // check to see that everyone is ready...
        for (int i = 0; i < data.characterTasks.Count; i++)
        {
            TaskCharacter tc = TaskMaster.GetInstance().GetCharacter(data.characterTasks[i].characterName);
            if (tc != null)
            {
                // check to see if we're ready
                if (tc.IsReady(data.characterTasks[i], this) == false)
                {
                    // if anyone is not ready then the task isn't ready
                    isReady = false;
                }
            }
        }

        // if ready then start everyone executing task
        if (isReady == true)
        {
            for (int i = 0; i < data.characterTasks.Count; i++)
            {
                TaskCharacter tc = TaskMaster.GetInstance().GetCharacter(data.characterTasks[i].characterName);
                if (tc != null)
                {
                    // sets up current task
                    tc.Setup(data.characterTasks[i], this);
                }
            }
        }

        return isReady;
    }

    //Check if the character is already doing something, and if not, do(animate) the task
	public void UpdateTask() 
    {
        // setup task first
        if (ready == false)
        {
            ready = SetupTask();
        }
        else
        {
            // task is ready, keep executing until all character tasks are
            // finished animating
            bool end = true;
            for (int i = 0; i < data.characterTasks.Count; i++)
            {
                TaskCharacter tCharacter = TaskMaster.GetInstance().GetCharacter(data.characterTasks[i].characterName);
                if (tCharacter != null)
                {
                    // if the character is still animating the
                    // task is not finished
                    if (tCharacter.IsAnimating() == true )
                        end = false;
                }
            }
            if (end == true)
            {
                // everyone's ended, force delete
                TaskMaster.GetInstance().DeleteTask(this);
            }
        }
	}		
}


public class TaskMaster : MonoBehaviour
{
	static TaskMaster instance;
	
    static public TaskMaster GetInstance()
    {
        if (instance == null){
			if ( Brain.GetInstance() != null )
			{
				instance = Brain.GetInstance().gameObject.GetComponent<TaskMaster>();
				if (instance == null)
	            	instance = Brain.GetInstance().gameObject.AddComponent<TaskMaster>();
			}
		}

        return instance;
    }
	
	List<TaskData> tData = new List<TaskData>();
	List<Task> tasks = new List<Task>();
	List<Task> availableTasks = new List<Task>();
	List<TaskCharacter> characters = new List<TaskCharacter>();
	Dictionary<string,string> blockingPairs = new Dictionary<string,string>();


    public class NodeInfo
    {
        public TaskCharacter Character;
        public string NodeName;

        public NodeInfo(TaskCharacter character, string nodeName)
        {
            Character = character;
            NodeName = nodeName;
        }
    }

    List<NodeInfo> occupiedNodes = new List<NodeInfo>();
		
	void Start() 
    {		
	}
	
	public void LoadXML( string filename )
    {
        Serializer<List<TaskData>> serializer = new Serializer<List<TaskData>>();
        tData = serializer.Load(filename);
        if (tData != null) {
			ImportTasks(tData);
		}
    }
	
	public void ImportTasks(List<TaskData> newTasks) 
    {
		foreach(TaskData newTask in newTasks) 
        {
            availableTasks.Add(new Task(newTask));
		}
	}
	
    public void Debug(List<TaskData> tData)
    {
        foreach (TaskData data in tData)
            data.Debug();
    }

	public void LockNode(TaskCharacter character, string nodeName) 
    {
		SceneNode.LockNode(nodeName, character as BaseObject);		
		
//		occupiedNodes.Add(new NodeInfo(character,nodeName));
#if DEBUG_NODES
        UnityEngine.Debug.Log("TaskMaster.LockNode(" + nodeName + ")");
#endif
	}

    public void UnlockNode(TaskCharacter character, string nodeName) 
    {
		SceneNode.UnlockNode(nodeName, character as BaseObject);
		
/*		
		for(int i = 0; i < occupiedNodes.Count; i ++) 
        {
			if (occupiedNodes[i].NodeName == nodeName && occupiedNodes[i].Character == character) 
            {
#if DEBUG_NODES
                UnityEngine.Debug.Log("TaskMaster.UnockNode(" + nodeName + ")");
#endif
                occupiedNodes.RemoveAt(i);
				i--;
			}
		}
*/
	}
	
	public bool CheckNode(string nodeName) 
    {
		return SceneNode.IsLocked(nodeName);
	}
	
	public bool CheckNode(TaskCharacter character, string nodeName) 
    {
		bool result = SceneNode.IsLockedFor (nodeName, character as BaseObject);
		if (result == false)
			blockingPairs.Remove(character.Name);
		return SceneNode.IsLockedFor (nodeName, character as BaseObject);
	}	

	public TaskCharacter GetLockingObject( string nodeName )
	{
		SceneNode node = SceneNode.Get (nodeName);
		if (node == null) return null;
		
		return node.LockingObject () as TaskCharacter;
	}

    public bool FreeNode(string nodeName, string requestor)
    {
		TaskCharacter character = GetLockingObject(nodeName);
		if (character == null){
#if DEBUG_NODES
			UnityEngine.Debug.Log("Node "+nodeName+" locked but not by character");
#endif
			return false;
		}

		if (character.IsDone() == true)
        {
            UnityEngine.Debug.LogWarning("TaskMaster.FreeNode(" + nodeName + "), GoHome(" + character.Name + ")");

            // send character home!!
            GoHome(character.Name);
            return true;
        }

#if DEBUG_NODES
        UnityEngine.Debug.LogWarning("TaskMaster.FreeNode(" + nodeName + "), Character BUSY(" + character.Name + ")");
		UnityEngine.Debug.Log (nodeName+" locked by busy character "+character.name);
#endif

		// the free has failed.  check for a race condition
		if (blockingPairs.ContainsKey (character.Name)){
			// the blocking character is blocking on a node, see if it's ours
			if (blockingPairs[character.Name] == requestor){
				// yes, he's waiting for us.  
				GoHome (requestor); // could sending us home first help ? probably will break the script
				return true;
			}
		}
		blockingPairs[requestor] = character.Name; // record the block.
        return false;
    }

    public void GoHome(string charName)
    {
        // find the character and their home node and sends them home
        TaskCharacter character = GetCharacter(charName);
        if (character)
        {
            NewTask("TASK:GO:HOME", character.Name);
        }
    }

    // get a task by name
    public Task GetTask(string taskname)
    {
        foreach (Task task in availableTasks)
        {
            if (taskname == task.data.name)
            {
                return task;
            }
        }
        return null;
    }

    //Are characters available for this task?
    public bool CanDoTask(string taskname)
    {
        Task task = GetTask(taskname);
        if (task == null)
            return false;

        TaskData data = task.data;
        foreach (CharacterTask chtask in data.characterTasks)
        {
            // get character
            foreach (TaskCharacter character in characters)
            {
                if (character.IsDone() == false )
                {
                    UnityEngine.Debug.LogError("TaskMaster.CanDoTask(" + task.data.name + ") : " + chtask.characterName + " is busy");
                    return false;
                }
            }
        }
        UnityEngine.Debug.LogError("TaskMaster.CanDoTask(" + task.data.name + ") : is ok");
        return true;
    }

    List<Task> addTasks;
    public void AddTask(Task task)
    {
#if DEBUG_TASK
        UnityEngine.Debug.Log("TaskMaster.AddTask() : Name=" + task.data.name);
#endif
        if (addTasks == null)
            addTasks = new List<Task>();
        addTasks.Add(task);
    }

    public void AddTasks()
    {
        if (addTasks == null)
            return;

        foreach (Task task in addTasks)
        {
            tasks.Add(task);
        }
        addTasks.Clear();
    }

    List<Task> deleteTasks;
    public void DeleteTask(Task task)
    {
#if DEBUG_TASK
        UnityEngine.Debug.Log("TaskMaster.DeleteTask() : Name=" + task.data.name);
#endif

        if (deleteTasks == null)
            deleteTasks = new List<Task>();
        deleteTasks.Add(task);
    }

    public void DeleteTasks()
    {
        if (deleteTasks == null)
            return;

        foreach (Task task in deleteTasks)
        {
            EndTask(task);
            tasks.Remove(task);
        }
        deleteTasks.Clear();
    }

    public void UpdateTasks()
    {
        // update all tasks
        foreach (Task task in tasks)
        {
            task.UpdateTask();
        }
    }

	//Make sure to make a NEW task not a pointer to the old one
	public void NewTask(string newTask, string gameObject) 
    {
#if DEBUG_TASK
		UnityEngine.Debug.LogError("TaskMaster.NewTask() : Searching for task: " + newTask);
#endif
        Task task = GetTask(newTask);

        if ( task != null )
        {
#if DEBUG_TASK
                UnityEngine.Debug.Log("TaskMaster.NewTask() : Adding task: " + newTask);
#endif
			Task nT = new Task(task.data);
            nT.data.Init();
            nT.data.CheckCondition();
			nT.ready = false;
			nT.data = task.data;
			nT.trackingName = nT.data.name + Time.time.ToString();
			if(!nT.data.locked)
				nT.data.characterTasks[0].characterName = gameObject;
            AddTask(nT);
		} else
            UnityEngine.Debug.LogError("Couldn't find task : " + newTask);
	}

	// update function, updates all tasks
    void Update()
    {
        AddTasks();
        UpdateTasks();
        DeleteTasks();
    }

	public void EndTask(Task task) 
    {
        // create new interact msg from task name and this object
        InteractStatusMsg msg = new InteractStatusMsg(task.data.name + ":COMPLETE");
        // send to brain
		Brain.GetInstance().PutMessage(msg);
#if DEBUG_TASK
        UnityEngine.Debug.Log("TaskManager.EndTask() : name=" + msg.InteractName);
#endif
        // end tasks
        foreach (TaskCharacter character in characters)
        {
            character.EndTask(task);
        }
        // remove, we're doe
		tasks.Remove(task);
	}
	
	public TaskCharacter GetCharacter(string targetCharacter) 
    {
		foreach(TaskCharacter character in characters) 
        {
			if(character.charName == targetCharacter)
				return character;
		}
		return null;			
	}

#if LATER
	TaskCharacter FindAlternative(Task task, TaskType type) 
    {
		List<TaskCharacter> potentials = characters;
		foreach(TaskCharacter potential in potentials) {
			foreach(string character in task.data.characters) {
				if(GetCharacter(character).charName == potential.charName) {
					potentials.Remove(potential);
				}
			}
			bool foundType = false;
			foreach(TaskType pType in potential.types) {
				if(type == pType)
					foundType = true;
			}
		    if(!foundType)
				potentials.Remove(potential);
			if(!potential.currentTask.data.inturruptible)
				potentials.Remove(potential);
			if(potential.currentTask.data.name == task.data.name)
				potentials.Remove(potential);
		}
		if(potentials.Count > 0)
			return potentials[0];
		else
			return null;
	}
#endif
	
	//Each character registers with the taskmaster at start
	public void RegisterCharacter(TaskCharacter character) 
    {
		foreach(TaskCharacter tc in characters) {
			if(tc.charName == character.charName) {
				string cName = character.charName;
				char[] c = cName.Remove(0, cName.Length - 1).ToCharArray();
				int ci = (int)c[0];
				int i = 1;
				ci -= 48;
				if(ci > -1 && ci < 10)
					i += ci;
				if(i > 1) 
					cName = character.charName.Remove(character.charName.Length-1, 1) + i.ToString();
				else
					cName = character.charName + i.ToString();
				character.charName = cName;
			}
		}
		characters.Add(character);
#if DEBUG_TASK
		UnityEngine.Debug.LogError("TaskMaster: Added character: " + character.charName);
#endif
	}
	
	public void PutMessage( GameMsg msg ) 
    {
		InteractMsg imsg = msg as InteractMsg;
		if(imsg != null) {
            if (imsg.map.task.Contains("TASK:"))
            {
#if DEBUG_TASK
                UnityEngine.Debug.Log("TaskMaster.PutMessage(Interact=" + imsg.map.task + ") : name=" + this.name);
#endif
                NewTask(imsg.map.task, imsg.gameObject);
            }
		}
	}
}
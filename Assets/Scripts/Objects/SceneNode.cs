using UnityEngine;
using System.Collections.Generic;

public class SceneNode : MonoBehaviour {

	public bool debugMe = false; // allow selecting a particular node in the editor to break in
	public static Dictionary<string,SceneNode> allNodes;
	List<SceneNode> lockedBy;
	List<SceneNode> coLocked;
	public bool isNav = true; // can characters move to this node (is it on the ground, etc...
	public bool initiallyLocked = false;
	public float lockRadius = 0.44f; // used as default and also for self blocking
	Vector3 lockedPosition = Vector3.zero;
	public BaseObject lockingObject = null; //may Behaviour as character,
	private int locked = 0; // use an int here, count the number of proximity locked, check for reference errors...
	public bool Locked
    {
        get
        {
            return (locked > 0);
        }
    }

	void OnDestroy()
	{
		if ( allNodes != null )
		{
			allNodes = null;
		}
	}
	
	public static bool IsLocked(string nodeName){
		SceneNode node = allNodes[nodeName];
		if (node != null) return node.Locked;
		else {
			// we could search for a game object of this name with a navNode child and add the component here...
			return false; // what to return if not found
		}
	}

	public static bool IsLockedFor(string nodeName, BaseObject obj){
		SceneNode node = null;
		if (allNodes.ContainsKey(nodeName)) node =allNodes[nodeName];
		if (node != null) return (node.Locked && node.LockingObject()!=obj);
		else {
			// we could search for a game object of this name with a navNode child and add the component here...
			return false; // what to return if not found
		}
	}
	
	public static void LockNode(string nodeName, BaseObject locker=null){
		SceneNode node = allNodes[nodeName];
		// look for a meaningful radius...
		if (node != null) node.Lock(node.lockRadius, locker);
	}
	
	public static void UnlockNode(string nodeName, BaseObject locker=null){
		if (nodeName == "") return;
		if (!allNodes.ContainsKey(nodeName)){
			Debug.LogWarning(nodeName+" not found in All Nodes list"); return;
		}
		
		SceneNode node = allNodes[nodeName];
		if (node != null) node.UnlockBy(locker); // null unlocker might be a problem. how should we handle ?
	}
	
	public static SceneNode Get(string nodeName){
		return allNodes[nodeName];	
	}
	
	public static void LockLocation(Vector3 pos, float radius){
		foreach (KeyValuePair<string, SceneNode> pair in allNodes){
			if (Vector3.Distance(pos, pair.Value.transform.position) <= radius){
				pair.Value.Lock( 0, null);
			}	
		}
	}


	public static void UnlockLocation(Vector3 pos, float radius){
		foreach (KeyValuePair<string, SceneNode> pair in allNodes){
			if (Vector3.Distance(pos, pair.Value.transform.position) <= radius){
				pair.Value.Unlock(); // this previously said Lock(0,null);, seemed wrong
			}	
		}
	}

		

	// Use this for initialization
	void Start () {
		if (allNodes == null)
			allNodes = new Dictionary<string,SceneNode>();
		allNodes[this.name] = this;
		lockedBy = new List<SceneNode>();
		coLocked = new List<SceneNode>();
		if (initiallyLocked) locked++;
	}
/*	
	void Reset(){
		if (initiallyLocked) locked=1; else locked = 0;
		lockedBy.Clear();
		coLocked.Clear();
	}
*/
	
	// Update is called once per frame

	void Update () {
		if (initiallyLocked && lockRadius > 0){
			// see if we've moved, and if so, unlock co-locked nodes and re lock
			if (transform.position != lockedPosition &&
				Vector3.Distance(transform.position, lockedPosition)>0.1f){
				CallUnlock ();	
				Lock (lockRadius,null);
			}
		}
	}

		 
	public void Lock(float radius, BaseObject locker=null){
		lockedPosition = transform.position;
		locked++;
		if (locker != null)
			lockingObject = locker;
		if (radius > 0){
			foreach (KeyValuePair<string, SceneNode> pair in allNodes){
				if (pair.Key != name &&
					Vector3.Distance(transform.position,pair.Value.transform.position) <= radius){
					pair.Value.ProximityLock (this,true);
					coLocked.Add (pair.Value);
				}
			}
		}
	}
	
	public bool UnlockBy(BaseObject obj){ // only perform if locked by obj
		if (locked>0 && lockingObject == obj){
			Unlock ();
			return true;
		}
		else {
			return false;
		}
	}

	public void CallUnlock(){
				Debug.Log ("Calling unlock on " + this.name);
				Unlock ();
		}
	
	public void Unlock(){
		if (locked == 0)
		{
			Debug.LogWarning("SceneNode Unlock failed on "+name);
			return;
		}
		locked--;
		lockingObject = null;
		foreach (SceneNode n in coLocked){
			n.ProximityLock (this,false);
		}
		coLocked.Clear();
	}
	
	public void ProximityLock(SceneNode locker, bool Lock){
		if (Lock && !lockedBy.Contains(locker)){
			locked++;
			lockedBy.Add(locker);
		}
		else
		{
			if (!lockedBy.Contains(locker) || locked == 0)
			{
				Debug.LogWarning("SceneNode ProximityUnlock failed on "+name+", not locked by "+locker.name);
				return;
			}
			locked--;
			lockedBy.Remove(locker);	
		}
	}
	
	public BaseObject LockingObject(){
		if (locked <= 0) return null;
		if (lockingObject != null) return lockingObject;
		bool lockedByProp = false;
		foreach (SceneNode node in lockedBy){
			if (node.lockingObject != null)
				return node.lockingObject;
			if (node.initiallyLocked) //
				// then is a travelling node, so return null but this isn't an error
				lockedByProp = true;
		}
		if (lockedByProp) return null;
		Debug.LogWarning(name+" locked but no locking object found. Decrementing locked");
		locked--;
		return null;
	}
	
	
	public static SceneNode GetRandomUnlockedNode(){
		// pick a random, then cycle thru all nodes from there till finding an unlocked one or return null
		// have to use this clunky iterator logic to index numerically into the dictionary...
		System.Collections.Generic.Dictionary<string, SceneNode>.Enumerator num = allNodes.GetEnumerator();
		num.MoveNext(); // move to the first position
		int checkNode = (int)(UnityEngine.Random.value*allNodes.Count);
		for (int i=0;i<checkNode;i++)
			num.MoveNext();
		for (int i=0;i<allNodes.Count;i++){
			// for now, use the TaskMaster do determine locked state of node, this should use the SceneNode test later
			string nn = num.Current.Key;
			if (!TaskMaster.GetInstance().CheckNode(nn)){
				return num.Current.Value;
			}
			if (!num.MoveNext()){
				num = allNodes.GetEnumerator(); // get a fresh iterator
				num.MoveNext();
			}
		}
		return null;		
	}
	
	void OnDrawGizmos()
	{
		if (locked<=0) Gizmos.color = Color.green;
		else Gizmos.color = Color.red;
		
		Gizmos.DrawWireSphere(transform.position, 0.1f);
	}
				
}

using UnityEngine;
using System.Collections;

public class BinaryExpressionNode {
	
	public enum compareType{
		NOP, // there's no comparison operator in the nodevalue
		EQ,
		NEQ,
		LT,
		GT,		
		LEQ,
		GEQ,
	} 
	
	BinaryExpressionNode m_Left;
	BinaryExpressionNode m_Right;
	BinaryExpressionNode m_Parent;
	bool m_bNot;
	string m_Data;
	int m_dwDataHash;
	GameObject cachedGO = null;
	
	public BinaryExpressionNode(string FromString){
	
		// creates and returns a node from the first word in the string, absorbing the ! operator if present
		// it doesn't process the whole string.  If there is a parenthesis, this will create a node from it,
		// and build tree has to write over that node later.
		// this only creates a single node, you must call buildtree to get a full tree from a string
		
		if (FromString == null || FromString.Length == 0){
Debug.Log("null/empty source for binary expression node constructor");
			return;
		}
		
		while(FromString[0] == ' ')
		{
			if (FromString.Length == 1) return;
			FromString = FromString.Substring(1);//Skip the ' '
		}
		
		Init();
		if(m_bNot = (FromString[0] == '!'))
		{
			FromString = FromString.Substring(1); //Parse past the ! symbol;
		}
		// truncate at first blank
		if(FromString.IndexOf(" ") >=0 )
			m_Data = FromString.Substring(0,FromString.IndexOf(" "));
		else
			m_Data = FromString;
		
		m_dwDataHash = m_Data.GetHashCode();		
	}
	
	void Init(){
		m_Left = null;
		m_Right = null;
		m_Parent = null;
		m_Data = "";
		m_dwDataHash = 0;
		m_bNot = false;
	}
	
	public BinaryExpressionNode GetRoot(){
		BinaryExpressionNode pNode = this;
		while (pNode.m_Parent != null)
			pNode = pNode.m_Parent;
		return pNode;
	}
	
	public static BinaryExpressionNode BuildTree(string szStringToEvaluate){
		string sParser = szStringToEvaluate;
		if(szStringToEvaluate != null)
		{
			BinaryExpressionNode pNode = new BinaryExpressionNode(sParser); // start a tree from the first token
			if (sParser.IndexOf(" ") >= 0){
				sParser = sParser.Substring(sParser.IndexOf(" "));
				pNode.BuildOnToNode(sParser);					// and build onto it.
			}
			return pNode.GetRoot();							//Returns the root
		}
		return null;		
	}
	
	void BuildOnToNode(string szString)
	{
		// throws away the first token, so call this with the string that built the node you are on ?
		string sParser = szString;
		if(sParser != null)
		{
			BinaryExpressionNode pInsertSpot = this;
			while(sParser != null) // we throw away the first token
			{
				if (sParser.IndexOf (" ") >= 0)
					sParser = sParser.Substring(sParser.IndexOf (" "));
				else
					sParser = null;
				if(sParser != null)
				{
					while(sParser[0] == ' ')
					{
						if (sParser.Length == 1) return;
						sParser = sParser.Substring(1);//Skip the ' '
					}
					//New Node
					BinaryExpressionNode pNewNode = new BinaryExpressionNode(sParser);
					
					if(pNewNode.IsOpenParenthesis())
					{
						pNewNode.SetParent(pInsertSpot, pInsertSpot.m_Left == null);    //Choose a free spot
						pInsertSpot = pNewNode;											//And change the target
						continue;
					}
					else if(pInsertSpot.IsOpenParenthesis())
					{	
						pInsertSpot.m_bNot = pNewNode.m_bNot;			//Parenthesese should never exist in the tree after building
						pInsertSpot.m_dwDataHash = pNewNode.m_dwDataHash;			//So, we copy the data over the Parenthesis
						pInsertSpot.m_Data = pNewNode.m_Data;
						//pNewNode->Release();
						continue;
					}
	
					if(pNewNode.IsCloseParenthesis())
					{
						if(pInsertSpot != null && pInsertSpot.m_Parent != null)
							pInsertSpot = pInsertSpot.m_Parent;		//pop back up a level
					}
					else if(pNewNode.IsOperator())
					{
						if(pInsertSpot.m_Parent != null)						//If we have a parent we have to make sure to inform the parent
						{													//that we are being adopted
							pNewNode.SetParent(pInsertSpot.m_Parent, pInsertSpot.m_Parent.m_Left == pInsertSpot);
						}
	
						pNewNode.SetLeft(pInsertSpot);					//First thing will always go left
						pInsertSpot = pNewNode;
					}
					else
					{
						pInsertSpot.SetRight(pNewNode);					//Always goes right
					}
				}
			}
		}
	}
	
	bool IsAnd(){ return (m_Data == "&"); }
	bool IsOr(){ return (m_Data == "|"); }
	bool IsOperator(){ return (IsAnd() | IsOr()); }
	bool IsOpenParenthesis(){ return (m_Data == "("); }
	bool IsCloseParenthesis(){ return (m_Data == ")"); }
	
	public bool Evaluate(BaseObject Entity) //make this
	{
		
		if(IsOperator())
		{
			if(IsAnd())
				return m_Left.Evaluate(Entity) & m_Right.Evaluate(Entity);			//Recursive
			else
				return m_Left.Evaluate(Entity) | m_Right.Evaluate(Entity);			//Recursive
		}
		else
		{
			if(m_bNot)
				return !NodeValue(m_Data, Entity); // look this up in the object's dictionary
			return NodeValue(m_Data, Entity);
		}
	}
	
	bool NodeValue(string expression, BaseObject entity){
		// come up with a boolean value for this node expression for this entity
		
		// here is all the crazy special syntax we support:
		// <boolexpression> something that is true or false, like the presence of an attribute
		// <valueexpression><comparator><valueexpression> where comparator can be "=" ">=" "<=" ">" "<" "!="
		// <expression> can be <objectname>.<expression>, but only one level deep please!
		//  if it has a ".", then we should find an object whos name appears before the dot, and evaluate it for that entity.
		// <expression> can be "%"<decisionVariableName>  if used without a comparator, then a non zero, non blank value would be considered "true"
		// <expression> can be "#"<scriptParameter> (should have been replaced with the script's arg value before the script called evaluate)
		// <expression> can be "@"<nodename>, meaning a scenenode @bedside=unlocked
		
		// we've picked up some stray \" sequences, so strip them out...
		// expression = expression.Replace("\\\"","\"");
		
		string leftSide = "";
		string rightSide = "";
		compareType comparator = ParseComparison(expression,out leftSide,out rightSide);
		
		if (comparator == compareType.NOP){
			return HasAttribute(leftSide,entity); // legacy use, does this attribute exist on the object ?  good enough for many situations and easy to specify.
		}
		// more complex.  each operand could be a constant, an object.attribute, a 
		leftSide = ProcessAndEncode(leftSide,entity);
		rightSide = ProcessAndEncode(rightSide,entity);
		// at this point, the two sides have been evaluated, retrieved, and encoded into strings with leading {i,f,s,b} indicating type.
		if (leftSide.Length == 0) return false; //
		// handle the simple equal/neq as encoded strings, ignoring case and any embedded quotes
		if (comparator == compareType.EQ)
			return (leftSide.Replace("\"","").ToLower() == rightSide.Replace("\"","").ToLower());
		if (comparator == compareType.NEQ)
			return (leftSide.Replace("\"","").ToLower() != rightSide.Replace("\"","").ToLower()); 
		// check for compatible types for other comparisons
		// lets see what types we have
		int iLeft;
		int iRight;
		if (int.TryParse(leftSide,out iLeft) && int.TryParse(rightSide,out iRight)){ // lets try as integers
			if (comparator == compareType.LT)
				return (iLeft < iRight);
			if (comparator == compareType.LEQ)
				return (iLeft <= iRight);
			if (comparator == compareType.GT)
				return (iLeft > iRight);
			if (comparator == compareType.GEQ)
				return (iLeft >= iRight);			
		}
		float fLeft;
		float fRight;
		if (float.TryParse(leftSide,out fLeft) && float.TryParse(rightSide,out fRight)){ // do float compares
			if (comparator == compareType.LT)
				return (fLeft < fRight);
			if (comparator == compareType.LEQ)
				return (fLeft <= fRight);
			if (comparator == compareType.GT)
				return (fLeft > fRight);
			if (comparator == compareType.GEQ)
				return (fLeft >= fRight);
		}
		// that leaves strings and booleans ordinals.  would anyone really do this ?
		Debug.LogWarning("Can't compare booleans or strings with <, etc.");
		return false;

	}
	
	bool HasAttribute(string operand, BaseObject entity){
		if (operand.Contains(".")){
			string[] parts = operand.Split('.');
			if (cachedGO == null)
				cachedGO = GameObject.Find(parts[0]);
			BaseObject bob = null;
			if (cachedGO != null){
				if ( (bob=cachedGO.GetComponent<BaseObject>()) != null){
					return HasAttribute(parts[1],bob);
				}
			}
			Debug.Log ("Error Evaluating "+parts[1]+" for "+parts[0]+": not found");
			return false;
		}
		else
			return entity.HasAttribute(operand);
		
	}
	
	string ProcessAndEncode(string operand, BaseObject entity){
		// return a string that represents the final value of this thing
		
		// try valid int, float and bool first, mainly to catch 30.0 as a float, not an object.attribute
		// a valid float, int, or boolean constant? - return "f1.5" "i1" "btrue"
		float fVal;
		int iVal;
		bool bVal;
		if (int.TryParse(operand,out iVal)){
			return iVal.ToString();
		}
		if (float.TryParse(operand,out fVal)){
			return fVal.ToString();
		}
		if (bool.TryParse(operand,out bVal)){
			return bVal.ToString();
		}	
		
		// see if it's a special case:
		// are we referencing another entity ? "." - find it and process for that entity
		if (operand.Contains(".")){
			string[] parts = operand.Split('.');
			if (cachedGO == null)
				cachedGO = GameObject.Find(parts[0]);
			BaseObject bob = null;
			if (cachedGO != null){
				if ( (bob=cachedGO.GetComponent<BaseObject>()) != null){
					return ProcessAndEncode (parts[1],bob);
				}
			}
			Debug.Log ("Error Evaluating "+parts[1]+" for "+parts[0]+": not found");
			return "error";
		}
		// @nodename - return slocked or sunlocked
		if (operand[0] == '@'){
			if (SceneNode.IsLocked(operand.Substring(1)))
				return "slocked";
			else
				return "sunlocked"; // TODO find the node and get this...
		}
		// %decisionVariable - get from entity
		if (operand[0] == '%'){
			return entity.GetAttribute(operand);
		}
		// "stringconstant" - return "sstringconstant"
		if (operand[0] == '\"'){
			return operand.Substring (1,operand.Length-2);
		}
		// otherwise, assume it's an attribute and get the entity's attribute value
		return entity.GetAttribute(operand);
	}
	
	compareType ParseComparison(string expression, out string left, out string right){
		if (expression == null){
			Debug.LogError("Null Binary Expression!");
			left = ""; right = "";
			return compareType.EQ;
		}
		compareType foundOp = compareType.NOP; // default to noop
		int opIndex = -1; 
		left = expression;
		right = "";
		if ((opIndex=expression.IndexOf(">=")) >= 0){
			foundOp = compareType.GEQ;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+2);
			return foundOp;
		}
		if ((opIndex=expression.IndexOf("<=")) >=0){
			foundOp = compareType.LEQ;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+2);
			return foundOp;
		}
		if ((opIndex=expression.IndexOf("!=")) >=0){
			foundOp = compareType.NEQ;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+2);
			return foundOp;
		}
		if ((opIndex=expression.IndexOf("=")) >=0){
			foundOp = compareType.EQ;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+1);
			return foundOp;
		}
		if ((opIndex=expression.IndexOf(">")) >=0){
			foundOp = compareType.GT;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+1);
			return foundOp;
		}
		if ((opIndex=expression.IndexOf("<")) >=0){
			foundOp = compareType.LT;
			left = expression.Substring(0,opIndex);
			right = expression.Substring(opIndex+1);
			return foundOp;
		}
		return foundOp;
	}
	
	bool HasActionFlag(BaseObject Entity, string key){ // TODO
		// see if the entity's action flag dictionary has the key, or if there's an "=", check the value
		return Entity.HasAttribute(key);
	}

	void SetLeft(BinaryExpressionNode pLeft)			
	{
		//Relying on garbage collection to clean up old references...
		m_Left = pLeft;
	
		if(pLeft != null)
		{
			pLeft.m_Parent = this;
		}
	}

	void SetRight(BinaryExpressionNode pRight)			
	{	
		m_Right = pRight;
	
		if(pRight != null)
		{
			pRight.m_Parent = this;
		}
	}

	void SetParent(BinaryExpressionNode pParent, bool bLeft)	
	{
		//Safe goodness, nothing to see here
		if(bLeft)
			pParent.SetLeft(this);
		else
			pParent.SetRight(this);
	}	
	
	// GUI debug draw tree:
	public void DrawNode(int px, int py){
		GUI.Label(new Rect(px,py,200,20),m_Data);
		if (m_Left != null)
			m_Left.DrawNode(px-50,py+20);
		if (m_Right != null)
			m_Right.DrawNode(px+50,py+20);
	}
}

using UnityEngine;
using System;

public class GUIEditWidget : MonoBehaviour
{
	public GUIEditWidget ()
	{
	}
	
	bool dragging = false;
	Vector2 startPos,currPosition,deltaPos,firstPos;	
	GUIObject guiObject;
	
	Vector2 origGUIPos,origOffset,origSize;
	
	public void SetGUIObject( GUIObject obj )
	{
		// make sure this is a GUIArea
		guiObject = obj;
	}
	
	public void UpdateGUIObject()
	{
		if ( dragging == true && guiObject.Style != null )
		{
			if ( (guiObject as GUIArea) != null )
			{
				if ( Input.GetKey(KeyCode.LeftControl) )
				{
					guiObject.Style.fixedWidth = origSize.x + deltaPos.x;
					guiObject.Style.fixedHeight = origSize.y + deltaPos.y;
				} 
				else
					guiObject.Style.contentOffset = deltaPos + origOffset;					
			}
			else 
			{
				guiObject.Style.margin.left = (int)(deltaPos.x + origOffset.x);
				guiObject.Style.margin.top = (int)(deltaPos.y + origOffset.y);
			}
		}			
	}
		
	
	public void Update()
	{
		currPosition = Input.mousePosition;
		currPosition.y = Screen.height-Input.mousePosition.y;	
		
		// undo on cntrl-z or cntrl-u
		if ( (Input.GetKeyUp(KeyCode.U) && Input.GetKey(KeyCode.LeftControl)) || 
			 (Input.GetKeyUp(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl)) )
			Undo();
			
		if ( Input.GetMouseButtonDown(0) && guiObject != null)
		{
			dragging = true;
			
			firstPos = currPosition;
			
			if ( dragging == true && guiObject.Style != null )
			{
				if ( (guiObject as GUIArea) != null )
				{
					origOffset = guiObject.Style.contentOffset;
					origSize.x = guiObject.Style.fixedWidth;
					origSize.y = guiObject.Style.fixedHeight;
				}
				else
				{
					origOffset.x = guiObject.Style.margin.left;
					origOffset.y = guiObject.Style.margin.top;
				}
			} 
		}
		
		// calc deltaPos
		deltaPos = currPosition-firstPos;
		
		if ( Input.GetMouseButtonUp(0) )
		{
			dragging = false;
		}
			
		UpdateGUIObject();
	}
	
	public void OnGUI()
	{
	}
	
	public void Undo()
	{
		if ( (guiObject as GUIArea) != null )
		{
			guiObject.Style.contentOffset = origOffset;
			guiObject.Style.fixedWidth = origSize.x;
			guiObject.Style.fixedHeight = origSize.y;
		}
		else
		{
			guiObject.Style.margin.left = (int)origOffset.x;
			guiObject.Style.margin.top = (int)origOffset.y;
		}
	}
}


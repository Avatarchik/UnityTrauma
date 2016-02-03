using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
	
public class GameObjAndCoords {
    public GameObject obj;
	public Character character;
	public Vector3 coords;
	public float dist;
    public List<Texture2D> textures;
	
	public GameObjAndCoords() {
		obj = new GameObject();
		coords = new Vector3(0, 0, 0);
		dist = 0f;
    }

    public void LoadImages()
    {
        Texture2D texture;
        textures = new List<Texture2D>();

        // form name
        string basename = "GUI/label." + character.name.ToLower() + ".";
//        UnityEngine.Debug.Log("GameObjAndCoords.LoadImages() : basename=" + basename);

        texture = (Texture2D)Resources.Load(basename + "default", typeof(Texture2D));
        if (texture == null)
            UnityEngine.Debug.Log("GameObjAndCoords.LoadImages() : can't load default : basename=" + basename);
        textures.Add(texture);
        texture = (Texture2D)Resources.Load(basename + "busy", typeof(Texture2D));
        if (texture == null)
            UnityEngine.Debug.Log("GameObjAndCoords.LoadImages() : can't load busy : basename=" + basename);
        textures.Add(texture);
        texture = (Texture2D)Resources.Load(basename + "talking", typeof(Texture2D));
        if (texture == null)
            UnityEngine.Debug.Log("GameObjAndCoords.LoadImages() : can't load talking : basename=" + basename);
        textures.Add(texture);
    }
}

public class CharacterSelectorDialogue : Dialog {
	
	public bool displayNames = false;
	
	static CharacterSelectorDialogue instance;
	List<GameObjAndCoords> charAndCoords = new List<GameObjAndCoords>();
	
	public List<GUISkin> skins = new List<GUISkin>();
	public List<string> names = new List<string>();
	public Vector2 textureSize = new Vector2(30, 30);
	
//	GUISkin defaultSkin; // not referenced anywhere in this class
		
	bool view = false;
	
	void Start() {
		charAndCoords = new List<GameObjAndCoords>();
		
//		defaultSkin = GUI.skin;
	}
	
	CharacterSelectorDialogue() {
		instance = this;
	}
	
	public static CharacterSelectorDialogue GetInstance() {
		if(instance == null){
			GameObject BO = Brain.GetInstance().gameObject;
			instance = BO.GetComponent<CharacterSelectorDialogue>();
			if (instance == null)
				instance = BO.AddComponent<CharacterSelectorDialogue>();
		}
		return instance;
	}
	
	public void Register(TaskCharacter character ) {
//        UnityEngine.Debug.Log("CharacterSelectorDeialog.Register(" + character.Name + ")");
        GameObjAndCoords cac = new GameObjAndCoords();
        cac.character = character.gameObject.GetComponent<Character>();
		cac.obj = character.gameObject;
        cac.LoadImages();
        charAndCoords.Add(cac);
    }
	
	public void Update() {
        if (Input.GetKeyUp(KeyCode.C))
            view = (view == true) ? false : true;

        //view = true;
		//if (Input.GetButton ("Fire3"))
	    //    view = true;
	}
	
    public float anchorOffsetY = 1.25f;
    public float anchorHeight = 100.0f;

	void OnGUI() {
		
		if(!view)
			return;
			
		if(gSkin)
			GUI.skin = gSkin;
		
		for(int i = 0; i < charAndCoords.Count; i++) {
			charAndCoords[i].dist = Vector3.Distance(charAndCoords[i].obj.transform.position, Camera.main.gameObject.transform.position);
			Vector3 newAnchor = charAndCoords[i].obj.transform.position;
            newAnchor.y += anchorOffsetY;
			charAndCoords[i].coords = Camera.main.WorldToScreenPoint(newAnchor);
			charAndCoords[i].coords = 
				new Vector3(Mathf.Clamp(charAndCoords[i].coords.x, 100, Screen.width - 100), 
				Mathf.Clamp(charAndCoords[i].coords.y, Screen.height/6, Screen.height - (Screen.height/6)) - (charAndCoords[i].dist * 4), 
				charAndCoords[i].coords.z);
		}
		
		//FindOverlaps();
		
		List<Vector3> coordinates = new List<Vector3>();
		
		charAndCoords = Sort(charAndCoords);		
		
		foreach( GameObjAndCoords gO in charAndCoords)
			coordinates.Add(gO.coords);
		
//		Debug.Log("start");
//		foreach(Vector3 coordinate in coordinates)
//			Debug.Log(coordinate.y);
			
				
		float lastHeight = 0;
		for(int i = 0; i < coordinates.Count; i++) {
			for(int j = 0; j < i; j++) {
				float distanceX = Math.Abs(coordinates[i].x - coordinates[j].x);
				float distanceY = coordinates[i].y - coordinates[j].y;
				if(coordinates[i].z < 0)
					coordinates[i] = new Vector3(85, coordinates[i].y, coordinates[i].z);
				if(coordinates[j].z < 0)
					coordinates[j] = new Vector3(85, coordinates[j].y, coordinates[j].z);
                if (distanceY < anchorHeight && distanceX < anchorHeight)
                {
//					Debug.Log("LastHeight before " + lastHeight);
//					Debug.Log("distanceY overlap " + distanceY + " " + i + " and " + j);
//					Debug.Log("Y " + coordinates[j].y);
					if(coordinates[j].y < lastHeight)
						lastHeight += anchorHeight;
					else
                        lastHeight = coordinates[j].y + anchorHeight;
//					Debug.Log("last height after " +lastHeight);
					coordinates[i] = new Vector3(coordinates[i].x, lastHeight, coordinates[i].z);
				}
			}
		}
		
		
		for(int i = 0; i < charAndCoords.Count; i++)
			charAndCoords[i].coords = coordinates[i];
		
		foreach(GameObjAndCoords gObj in charAndCoords) {
			
			string name = "";
			if(displayNames)
				name = gObj.obj.name;

            // default to idle
            Texture2D image = gObj.textures[0];
            if (gObj.character.IsDone() == false)
            {
                // for now we're just busy
                image = gObj.textures[1];
            }
			if (gObj.character.Talking)
            {
                // for now we're just busy
                image = gObj.textures[2];
            }

            // create a style which is the same w/h as the orignal image
            GUIStyle style = new GUIStyle();
            if (image != null)
            {
                style.fixedHeight = image.height;
                style.fixedWidth = image.width;
            }

            // check if image will be on screen or not
            if (gObj.coords.z >= 0) 
            {
                float centerx = gObj.coords.x - style.fixedWidth / 2.0f;
                if (GUI.Button(new Rect(centerx, Screen.height - gObj.coords.y, style.fixedWidth, style.fixedHeight), image, style))
                {
					gObj.obj.GetComponent<ObjectInteraction>().OnMouseUp();
				}
			}
            else if (GUI.Button(new Rect(85, Screen.height - gObj.coords.y, style.fixedWidth, style.fixedHeight), image, style))
            {
				Debug.Log(gObj.coords);
				gObj.obj.GetComponent<TaskCharacter>().GetState();
				gObj.obj.GetComponent<ObjectInteraction>().OnMouseUp();
			}
		}
	}
	
	List<GameObjAndCoords> Sort(List<GameObjAndCoords> gO) {
		
		while(true) {
			bool leave = true;
			for(int i = 0; i < gO.Count; i++) {
				for(int j = 0; j < i; j++) {
					if(gO[j].coords.y > gO[i].coords.y) {
						leave = false;
						GameObjAndCoords temp = gO[j];
						gO[j] = gO[i];
						gO[i] = temp;
					}
				}
			}
			if(leave)
				break;
		}
		return gO;
	}		
}
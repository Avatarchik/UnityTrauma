// use this define in the build settings to make standalone
//#define GUIMANAGER_STANDALONE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if GUIMANAGER_STANDALONE
public class GameMsg
{
}
#endif

// GUIManager handles display management of GUIObjects, along with sending Input down to the top layer.
public class GUIManager : MonoBehaviour
{
    enum GM_Command { MOVE_FORWARD, MOVE_BACK, MOVE_TO_FRONT, MOVE_TO_BACK, DELETE, ADD, MODAL };
    struct QueueCommand
    {
        public GM_Command command;
        public ScreenInfo screenInfo;

        public QueueCommand(GM_Command command, ScreenInfo screen)
        {
            this.command = command;
            this.screenInfo = screen;
        }
    }
	
	public static Vector2 GUIEditWindowSize = new Vector2(1024,768);
	
	static GUIManager instance = null;

	public static GUIManager GetInstance()
	{
		if ( instance == null )
		{
			instance = GameObject.FindObjectOfType (typeof(GUIManager)) as GUIManager;
			if ( instance == null )
				UnityEngine.Debug.LogError ("No GUIManager Script in the Scene!");
		}
		return instance;
	}
	
	public List<GUISkin> skins = new List<GUISkin>();
	
    private List<ScreenInfo> screens = new List<ScreenInfo>();
	public List<ScreenInfo> Screens
	{
		get { return screens; }
	}
	
	// this hold the command queue... we need to create an add list to make
	// sure the list isn't being updated in the middle
    private List<QueueCommand> commandQueue = new List<QueueCommand>();
	private List<QueueCommand> commandQueueAdd = new List<QueueCommand>();

    public TextAsset GUIScript;
    private string oldScript;
	
	public List<TextAsset> GUIScripts = new List<TextAsset>();

	bool fade=true;
	public bool Fade
	{
		set { 
			fade=value; 
		}
		get { return fade; }
	}

	public float FadeSpeed=0.1f;

    bool displayDebug = false;
    bool modalBack = true;
    ScreenInfo controlledScreen;
    int selected = -1;
    int oldSelected = -1;

    ScreenInfo modal;

	public float Width
	{
		get {
			if ( FitToScreen == true )
				return NativeSize.x;
			else
				return Screen.width;
		}
	}

	public float Height
	{
		get {
			if ( FitToScreen == true )
				return NativeSize.y;
			else
				return Screen.height;
		}
	}

	public float X
	{
		get {
			if ( FitToScreen == true && Letterbox == true )
				return LetterboxRect.x;
			else
				return 0;
		}
	}
	
	public float Y
	{
		get {
			if ( FitToScreen == true && Letterbox == true )
				return LetterboxRect.y;
			else
				return 0;
		}
	}

	public float FracX;
	public float FracY;
	
	public void ComputeLetterboxRatio()
	{		
		// don't do anything if not letterboxed
		if ( FitToScreen == false || Letterbox == false )
			return;

		// set the desired aspect ratio (the values in this example are
		// hard-coded for 16:9, but you could make them into public
		// variables instead so you can set them at design time)
		float targetaspect = NativeSize.x / NativeSize.y ;
		
		// determine the game window's current aspect ratio
		float windowaspect = (float)Screen.width / (float)Screen.height;
		
		// current viewport height should be scaled by this amount
		float scaleheight = windowaspect / targetaspect;
		
		// if scaled height is less than current height, add letterbox
		if (scaleheight < 1.0f)
		{  
			Rect rect = new Rect();
			
			rect.width = 1.0f;
			rect.height = scaleheight;
			rect.x = 0;
			rect.y = (1.0f - scaleheight) / 2.0f;
			
			LetterboxRect = rect;
		}
		else // add pillarbox
		{
			float scalewidth = 1.0f / scaleheight;
			
			Rect rect = new Rect();
			
			rect.width = scalewidth;
			rect.height = 1.0f;
			rect.x = (1.0f - scalewidth) / 2.0f;
			rect.y = 0;
			
			LetterboxRect = rect;
		}

		// letterbox camera is letterbox is on
		if ( Camera.main != null )
			Camera.main.rect = LetterboxRect;
		
	}

	public void DrawLetterboxRects()
	{
		if ( Letterbox == false || FitToScreen == false )
			return;
		GL.Clear (false,true,Color.black);
	}

	bool fitToScreen=false;
	public bool FitToScreen
	{
		get { return fitToScreen; }
		set {
			// if FitToScreen is false then also force Letterbox to false
			if ( value == false )
				Letterbox = false;
			fitToScreen = value;
		}
	}

	bool letterbox=false;
	public bool Letterbox
	{
		get { return letterbox; }
		set {
			letterbox = value;
			// if Letterbox is false then set the camera rect to default, 
			// if true then compute the letterbox
			if ( letterbox == false )
				LetterboxRect = new Rect(0,0,1,1);
			else
				ComputeLetterboxRatio();
			// set camera rect
			if ( Camera.main != null )
				Camera.main.rect = LetterboxRect;
		}
	}
	public Vector2 NativeSize;
	public Rect LetterboxRect ;	

    void Awake()
    {
        if (GUIScript != null)
            oldScript = GUIScript.name;
        else
            oldScript = "";
        instance = this;		
    }
	
	bool swiping=false;
	public bool IsSwiping()
	{
		return swiping;
	}

	Vector2 swipeDelta;
	public Vector2 SwipeDelta
	{
		get { return swipeDelta; }
	}

	public class GUISwipeMessage : GameMsg
	{
		public Touch Touch;
		public Vector2 SwipeDelta;
	}

	public delegate void SwipeCallback( GUISwipeMessage msg );
	SwipeCallback swipeCallback=null;

	public int MinSwipeDelta=50;
	
	void HandleTouches()
	{
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) 
		{
			swipeDelta = new Vector2(); 
		}
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) 
		{
			swipeDelta += Input.GetTouch(0).deltaPosition;
			if ( swipeCallback != null )
			{
				GUISwipeMessage msg = new GUISwipeMessage();
				msg.Touch = Input.GetTouch(0);
				msg.SwipeDelta = SwipeDelta;
				swipeCallback(msg);
			}
			swiping = true;
		}
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) 
		{
			if ( swipeCallback != null )
			{
				GUISwipeMessage msg = new GUISwipeMessage();
				msg.Touch = Input.GetTouch(0);
				msg.SwipeDelta = SwipeDelta;
				swipeCallback(msg);
			}
			swiping = false;
		}
	}
	
	public void AddSwipeCallback( SwipeCallback callback )
	{
		swipeCallback += callback;
	}

	public void RemoveSwipeCallback( SwipeCallback callback )
	{
		swipeCallback -= callback;
	}
	
	void LoadGUIScript( string name, bool modal )
	{
        ScreenInfo temp = ScreenInfo.Load("GUIScripts/" + name);
        if (temp != null && temp.Screen != null)
        {
            temp.Initialize();
            screens.Add(temp);

            GUIDialog diag = temp.Screen as GUIDialog;
            if(diag != null)
            {
                DialogMsg msg = new DialogMsg();
                msg.command = DialogMsg.Cmd.open;
                msg.modal = modal;
                diag.Load(msg);
            }
        }
	}

    // Use this for initialization
    void Start()
    {
        if (GUIScript != null)
        {
			LoadGUIScript(GUIScript.name,true);
        }
		
		if (GUIScripts.Count > 0 )
		{
			foreach( TextAsset asset in GUIScripts )
				LoadGUIScript(asset.name,false);
		}

		// init curtain to enabled if using fader
		if ( Fade == true )
			curtainAlpha = 1.0f;
		else
			curtainAlpha = 0.0f;
		
        gameObject.AddComponent<AudioSource>();
        audio.panLevel = 0;
    }
	
	GUIObject _selectedGUIObject=null;
	public GUIObject SelectedGUIObject
	{
		set { _selectedGUIObject = value; }
		get { return _selectedGUIObject; }		
	}
	
	public void CheckKeys()
	{
		if ( _selectedGUIObject == null )
			return;
		
		if (Input.GetKeyUp(KeyCode.RightArrow))
		{			
			_selectedGUIObject.RightArrow();
		}
		if (Input.GetKeyUp(KeyCode.LeftArrow))
		{
			_selectedGUIObject.LeftArrow();
		}
		if (Input.GetKeyUp(KeyCode.UpArrow))
		{
			_selectedGUIObject.UpArrow();
		}
		if (Input.GetKeyUp(KeyCode.DownArrow))
		{
			_selectedGUIObject.DownArrow();
		}				
	}

	public List<GUIObject> Hotspots=new List<GUIObject>();

	public void ClearHotspots()
	{
		if ( Hotspots == null || Hotspots.Count == 0 )
			return;

		Hotspots.Clear ();
	}

	public void CheckHotspot( GUIObject element )
	{
		if ( Hotspots == null )
			Hotspots = new List<GUIObject>();

		// only areas for now
		GUIArea area = element as GUIArea;
		if ( area != null && area.hotspot == true )
		{
			Vector2 position = (new Vector2( Input.mousePosition.x, Screen.height-Input.mousePosition.y));
			position.x -= GUIManager.GetInstance().FracX;
			position.y -= GUIManager.GetInstance().FracY;
			Rect areaRect = GUITransformRect(area.GetScreenArea());
			if ( areaRect.Contains(position) )
			{
				area.isHotspot = true;
				Hotspots.Add(area);
			}
			else
				area.isHotspot = false;
		}
		else
			area.isHotspot = false;
	}

	public void ExecuteCommandQueue()
	{
		// add new commands from screens
		foreach (QueueCommand com in commandQueueAdd)
			commandQueue.Add (com);
		commandQueueAdd.Clear ();
		
		// Go through commands and do them
		foreach (QueueCommand com in commandQueue)
		{
			switch (com.command)
			{
			case GM_Command.ADD:
			{
				Add_Implement(com.screenInfo);
			}
				break;
			case GM_Command.DELETE:
			{
				Remove_Implement(com.screenInfo);
			}
				break;
			case GM_Command.MOVE_TO_BACK:
			{
				MoveToBack_Implement(com.screenInfo);
			}
				break;
			case GM_Command.MOVE_BACK:
			{
				MoveBack_Implement(com.screenInfo);
			}
				break;
			case GM_Command.MOVE_TO_FRONT:
			{
				MovetoFront_Implement(com.screenInfo);
			}
				break;
			case GM_Command.MOVE_FORWARD:
			{
				MoveForward_Implement(com.screenInfo);
			}
				break;
			case GM_Command.MODAL:
			{
				SetModal_Implement(com.screenInfo);
			}
				break;
			}
		}
		commandQueue.Clear();
	}
	
    // Update is called once per frame
    void Update()
    {
		ComputeLetterboxRatio();
		HandleRegisteredAreas();
		HandleTouches();
		FadeCurtain();
		SetScale ();
		
		if ( SelectedGUIObject != null )
			CheckKeys();
		
		// call update for all screens
        foreach (ScreenInfo screen in screens)
        {
			screen.Update();
		}
		
        // Cheap way to allow run-time loading of XML file
        if (GUIScript != null && GUIScript.name != oldScript)
        {
			LoadFromFile(GUIScript.name);
            oldScript = GUIScript.name;
        }

		ExecuteCommandQueue();
		
        // Debug controls
        if (Debug.isDebugBuild)
        {
            if(Input.GetKeyDown(KeyCode.Home))
            {
                displayDebug = !displayDebug;
            }
            if (displayDebug)
            {
                if (Input.GetKeyDown(KeyCode.End))
                {
                    if (controlledScreen != null)
                        Remove(controlledScreen);
                    controlledScreen = null;
                    selected = -1;
                    oldSelected = -1;
                }
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    if (controlledScreen != null)
                        controlledScreen.LastScreen();
                }
                if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    if (controlledScreen != null)
                        controlledScreen.NextScreen();
                }
            }
        }
    }
	
	void LateUpdate()
	{
		// call update for all screens
        foreach (ScreenInfo screen in screens)
        {
			screen.Screen.LateUpdate();
		}
	}

	float offX,offY;
	public Matrix4x4 GUIMatrix;

	public void SetGUIMatrix()
	{
		Vector3 scale;

		if ( FitToScreen == true )
		{
			// compute offset
			offX = 0.0f;
			offY = 0.0f;
			
			if ( Letterbox == true )
			{
				// compute scale
				scale = new Vector3((float)Screen.width*LetterboxRect.width / NativeSize.x, 
				                    (float)Screen.height*LetterboxRect.height / NativeSize.y, 
				                    1.0f);
				
				if ( LetterboxRect.width != 1.0f )
					offX = (Screen.width - Screen.width*LetterboxRect.width)/2;
				if ( LetterboxRect.height != 1.0f )
					offY = (Screen.height - Screen.height*LetterboxRect.height)/2;
			}
			else
			{
				// compute scale
				scale = new Vector3((float)Screen.width / NativeSize.x, 
				                    (float)Screen.height / NativeSize.y, 
				                    1.0f);
			}
			
			// build matrix
			GUI.matrix = GUIMatrix = Matrix4x4.TRS(new Vector3(offX,offY,0), Quaternion.identity, scale);
		}
		else
		{
			GUI.matrix = GUIMatrix = Matrix4x4.identity;
			offX = 0.0f;
			offY = 0.0f;
		}

		// compute frac values
		Vector2 tmp = GUIUtility.GUIToScreenPoint(new Vector2(0,0));
		FracX = tmp.x;
		FracY = tmp.y;
	}

	public void ClearGUIMatrix()
	{
		GUI.matrix = Matrix4x4.identity;
	}

	public Rect GUITransformRect( Rect inRect )
	{
		Rect outRect = new Rect();
		Vector2 vec;
		vec.x = inRect.x;
		vec.y = inRect.y;
		vec = GUIManager.GetInstance().GUIMatrix.MultiplyVector(vec);
		outRect.x = vec.x;
		outRect.y = vec.y;
		
		vec.x = inRect.width;
		vec.y = inRect.height;
		vec = GUIManager.GetInstance().GUIMatrix.MultiplyVector(vec);
		outRect.width = vec.x;
		outRect.height = vec.y;
		
		return outRect;
	}
	
	public void DrawDebugInfo()
	{
		return;
		Vector2 pos = GUIUtility.ScreenToGUIPoint(new Vector2(0,Screen.height-40));
		GUI.color = Color.red;
		GUILayout.BeginArea(new Rect(0,pos.y,600,30));
		GUILayout.Label("Letterbox=" + LetterboxRect + " : ScreenWH=" + Screen.width + "," + Screen.height);
		GUILayout.EndArea();
		GUI.color = Color.white;
	}

	public void OnGUI()
	{
		DrawGUI ();
		DrawDebugInfo();
	}

	public int Depth=0;
	void SetDepth()
	{
		if ( Depth != 0.0f )
			GUI.depth = Depth;
	}
		
    public void DrawGUI()
    {		
		SetDepth();
		SetGUIMatrix ();
		ClearHotspots ();

        // For each screen, check if it is the modal screen, then render appropriately
        foreach (ScreenInfo screen in screens)
        {
            if (screen == modal)
            {
                if(modalBack)
                    GUI.Box(new Rect(0, 0, this.Width, this.Height), "", GUI.skin.box);
                else
                    GUI.Box(new Rect(0, 0, this.Width, this.Height), "", GUI.skin.label);
                screen.Windowed = true;
                screen.Execute();
                screen.Windowed = false;
            }
            else
                screen.Execute();
        }
		
		// draw overlays (if window gets drawn about then these are also called in GUIScreen.DoWindow)
		DrawCursor();
		DrawCurtain();		
		
        if (Debug.isDebugBuild && displayDebug && screens.Count > 0)
        {
            string[] selStrings = new string[screens.Count];
            // Fill out the grid
            for (int i = 0; i < screens.Count; i++)
            {
                selStrings[i] = screens[i].Screen.name;
            }

            // Display the grid
            selected = GUI.SelectionGrid(new Rect(0, 0, 100, this.Height), selected, selStrings, 1);
            // Check for change
            if (oldSelected != selected)
            {
                controlledScreen = screens[selected];
                MoveToFront(controlledScreen);
                selected = screens.Count - 1;
                oldSelected = selected;
            }
        }	

		ClearGUIMatrix();
    }
	
	public float ScaleX=1.0f;
	float lastScaleX=1.0f;
	
	public float ScaleY=1.0f;
	float lastScaleY=1.0f;
	
	public void SetScale()
	{
#if SCALE_ELEMENTS
		if ( FitToScreen == true )
		{
			if ( NativeSize.x != Screen.width )
				ScaleX = Screen.width/NativeSize.x;
			if ( NativeSize.y != Screen.height )
				ScaleY = Screen.height/NativeSize.y;
		}
		
		if ( lastScaleX != ScaleX || lastScaleY != ScaleY )
		{
			foreach( ScreenInfo si in screens )
			{
				si.Screen.SetScale(ScaleX,ScaleY);
			}
		}
#endif
	}
	
	public bool CursorEnable=false;
	public Texture2D CursorTexture=null;
	public Rect CursorRect=new Rect(-16,-16,32,32);
	
	public void DrawCursor()
	{
		if ( CursorEnable == true )
		{
			if ( CursorTexture != null )
			{
				Rect position = CursorRect;
				position.x += Input.mousePosition.x;
				position.y += (Screen.height-Input.mousePosition.y);
				GUI.DrawTexture(position,CursorTexture);
			}
		}
	}
	
	public bool CrossFade=false;
	public bool UseFadeCurtain=true;
	
	Texture2D curtainTexture;
	Color curtainColor = new Color(0,0,0,0);
	float curtainAlpha = 0.0f;
	float curtainStartAlpha;
	float curtainSeekAlpha = 0.0f;
	float curtainSeekTime;
	float curtainSeekStartTime;
	
	public bool Curtain
	{
		get { return ( curtainAlpha > 0.0f ); }
	}

	public float CurtainAlpha
	{
		get { return curtainAlpha; }
	}
	
	public void DrawCurtain()
	{
		if ( curtainTexture == null )
		{
			curtainTexture = new Texture2D(1,1);
		}
		if ( curtainAlpha > 0.0f )
		{
			curtainColor.a = curtainAlpha;
			GUI.color = curtainColor;
			GUI.DrawTexture(new Rect(0,0,this.Width,this.Height), curtainTexture, ScaleMode.StretchToFill, false, 0f);
		}
	}
	
	public void SetFadeCurtain( float alpha, float time, float startAlpha=-1 )
	{
		//UnityEngine.Debug.Log("GUIManager.SetFadeCurtain() : alpha=" + alpha + " time=" + time + " startAlpha=" + startAlpha);

		if ( Time.timeScale == 0.0f )
			return;
		
		if ( startAlpha != -1 )
			curtainAlpha = startAlpha;
		
		curtainSeekAlpha = alpha;
		curtainStartAlpha = curtainAlpha;
		
		curtainSeekTime = time;
		curtainSeekStartTime = Time.time;
	}
	
	private void FadeCurtain()
	{
		// don't draw curtain if time scale is 0
		if ( Time.timeScale == 0.0f || UseFadeCurtain == false )
		{
			curtainAlpha = 0.0f;
			return;
		}
		
		// check fade screen (auto screen fades)
		if ( fadeScreenIn != null )
		{
			if ( curtainAlpha == 1.0f )
			{
	        	commandQueueAdd.Add(new QueueCommand(GM_Command.ADD, fadeScreenIn));
				SetFadeCurtain(0.0f,FadeSpeed);
				fadeScreenIn = null;
			}
		}
		
		// check fade screen (auto screen fades)
		if ( fadeScreenOut != null )
		{
			if ( curtainAlpha == 1.0f )
			{
	        	commandQueueAdd.Add(new QueueCommand(GM_Command.DELETE, fadeScreenOut));
				SetFadeCurtain(0.0f,FadeSpeed);
				fadeScreenOut = null;
			}
		}
		
		if ( curtainSeekAlpha != curtainAlpha )
		{
			// get elapsed time since start of fade
			float elapsed = Time.time - curtainSeekStartTime;
			// fraction of time elapsed
			float fraction=1.0f;
			if ( curtainSeekTime > 0 )
				fraction = elapsed/curtainSeekTime;
			if ( fraction > 1.0f ) 
				fraction = 1.0f;
			// lerp color by fraction
			curtainAlpha = Mathf.Lerp(curtainStartAlpha,curtainSeekAlpha,fraction);
			curtainColor.a = curtainAlpha;
		}
	}
	
	ScreenInfo fadeScreenIn;
	private void FadeScreenIn( ScreenInfo screen )
	{
		// set fade screen
		fadeScreenIn = screen;
		// see if there is a time
		if ( screen.Screen.fadespeed != -1 )
			SetFadeCurtain(1.0f,screen.Screen.fadespeed);
		else
			SetFadeCurtain(1.0f,FadeSpeed);
	}

	ScreenInfo fadeScreenOut;
	private void FadeScreenOut( ScreenInfo screen )
	{
		fadeScreenOut = screen;
		// set fader out
		if ( screen.Screen.fadespeed != -1 )
			SetFadeCurtain(1.0f,screen.Screen.fadespeed);
		else
			SetFadeCurtain(1.0f,FadeSpeed);
	}
	
	private bool IsFadingDown()
	{
		// if crossFade is set then everything is ok!
		if ( CrossFade == true || UseFadeCurtain == true )
			return false;
		
		foreach( QueueCommand item in commandQueue )
		{
			if ( item.command == GM_Command.DELETE )
			{
				if ( item.screenInfo.Screen.FadeDone () == false )
					return true;
			}
		}
		return false;
	}
	
	public ScreenInfo LoadFromFileRaw(string name, string forceType=null)
	{
        ScreenInfo newInfo = ScreenInfo.Load("GUIScripts/" + name);
        if (newInfo != null && newInfo.Screen != null)
        {
            newInfo.Initialize(forceType);
        }
        return newInfo;
	}

    public ScreenInfo LoadFromFile(string name, string forceType=null)
    {
        ScreenInfo newInfo = ScreenInfo.Load("GUIScripts/" + name);
        if (newInfo != null && newInfo.Screen != null)
        {
            newInfo.Initialize(forceType);
            Add(newInfo);
        }
		else
			UnityEngine.Debug.LogError ("GUIManager.LoadFromFile(" + name + ") didn't load!");
        return newInfo;
    }
	
	public ScreenInfo LoadFromDisk( string name, string forceType=null)
	{
        ScreenInfo newInfo = ScreenInfo.LoadFromDisk(name);
        if (newInfo != null && newInfo.Screen != null)
        {
            newInfo.Initialize(forceType);
            Add(newInfo);
        }
        return newInfo;
	}
	
    public void Add(ScreenInfo screenInfo)
    {
		// go immediately to screen if timeScale is 0
		if ( screenInfo.Screen != null && (Fade == true || screenInfo.Screen.fade == true) && Time.timeScale != 0.0f && UseFadeCurtain == true )
			FadeScreenIn(screenInfo);
		else
        	commandQueueAdd.Add(new QueueCommand(GM_Command.ADD, screenInfo));
    }

	public void AddImmediate( ScreenInfo screenInfo )
	{
		Add_Implement(screenInfo);
	}
	
	private void Add_Implement(ScreenInfo screen)
    {
		if ( UseFadeCurtain == false )
		{
			if ( IsFadingDown() == true )
			{
				commandQueueAdd.Add(new QueueCommand(GM_Command.ADD, screen));
				return;
			}
			if ( Time.timeScale != 0.0f && Fade == true )
				screen.Screen.FadeOpen (0.5f);
		}
       	screens.Add(screen);        
    }
	
    public void Remove(ScreenInfo screenInfo)
    {
		if ( Time.timeScale != 0.0f && Fade == true && UseFadeCurtain == false )
			screenInfo.Screen.FadeClose (0.5f);

		// go immediately to screen if timeScale is 0
		if ( screenInfo.Screen != null && (Fade == true || screenInfo.Screen.fade == true) && Time.timeScale != 0.0f  && UseFadeCurtain == true )
			FadeScreenOut(screenInfo);
		else
        	commandQueueAdd.Add(new QueueCommand(GM_Command.DELETE, screenInfo));
    }

	public void RemoveImmediate( ScreenInfo screenInfo )
	{
		Remove_Implement(screenInfo);
	}
	
	public void RemoveNoFade( ScreenInfo screenInfo )
	{
       	commandQueueAdd.Add(new QueueCommand(GM_Command.DELETE, screenInfo));
	}
	
	public void RemoveScreen(GUIScreen screen)
	{
		foreach( ScreenInfo si in screens )
		{
			if ( si.Screen == screen )
			{
				Remove(si);
				return;
			}
		}
	}
	
	bool Dequeue(string name)
	{
		// check to see if this screen is in the queue already, if so remove so it doesn't get loaded
		foreach( QueueCommand item in commandQueueAdd )
		{
			if ( item.screenInfo.Screen.name == name )
			{
				// found it, this screen was about to be added but dequeue it
				commandQueueAdd.Remove (item);
				return true;
			}
		}	
		return false;
	}
	
	public void Remove( string screen, bool forceNoFade=false )
	{
		// first try to dequeue
		if ( Dequeue (screen) == true )
			return;
		// not queued, remove existing screen (if available)
		foreach( ScreenInfo si in screens )
		{
			if ( si.Screen.name == screen )
			{
				if ( forceNoFade == true )
					RemoveNoFade(si);
				else
					Remove(si);
				return;
			}
		}		
	}
		
	public void RemoveAllScreens()
	{
		screens = new List<ScreenInfo>();
	}

    private void Remove_Implement(ScreenInfo screen)
    {
		// if screen is not done fading then queue again
		if ( screen.Screen.FadeDone() == false )
		{
			commandQueueAdd.Add(new QueueCommand(GM_Command.DELETE, screen));
			return;
		}

        foreach (ScreenInfo info in screens)
        {
            if (info == screen)
            {
                info.OnClose();
                screens.Remove(info);
                if (modal == screen)
                    modal = null;
                break;
            }
        }
        // debug window check
        if (screens.Count == 0)
            displayDebug = false;
    }
	
	public void CloseDialogs()
	{
        foreach (ScreenInfo info in screens)
        {
			GUIDialog d = info.Screen as GUIDialog;
			if ( d != null )
			{
				d.Close();
			}
		}
	}

    public void MoveForward(ScreenInfo screen)
    {
        commandQueueAdd.Add(new QueueCommand(GM_Command.MOVE_FORWARD, screen));
    }

    private void MoveForward_Implement(ScreenInfo screen)
    {
        // Find screen, remove, and reinsert at one index higher.
        // Skip last itme, as that can't go higher in the array
        for (int i = 0; i < screens.Count-1; i++)
        {
            if (screens[i] == screen)
            {
                if (screens[i + 1] == modal)
                    return;
                screens.RemoveAt(i);
                screens.Insert(i + 1, screen);
            }
        }
    }

    public void MoveBack(ScreenInfo screen)
    {
        if (screen == modal)
            return;
        commandQueueAdd.Add(new QueueCommand(GM_Command.MOVE_BACK, screen));
    }

    private void MoveBack_Implement(ScreenInfo screen)
    {
        // Find screen, remove, and reinsert at one index lower.
        // Skip first item, as that can't go back any further.
        for (int i = 1; i < screens.Count; i++ )
        {
            if (screens[i] == screen)
            {
                screens.RemoveAt(i);
                screens.Insert(i - 1, screen);
            }
        }
    }

    public void MoveToFront(ScreenInfo screen)
    {
        commandQueueAdd.Add(new QueueCommand(GM_Command.MOVE_TO_FRONT, screen));
    }

    private void MovetoFront_Implement(ScreenInfo screen)
    {
        if (screens.Contains(screen))
        {
            screens.Remove(screen);
            screens.Add(screen);
        }
    }

    public void MoveToBack(ScreenInfo screen)
    {
        commandQueueAdd.Add(new QueueCommand(GM_Command.MOVE_TO_BACK, screen));
    }

    private void MoveToBack_Implement(ScreenInfo screen)
    {
        if (screens.Contains(screen))
        {
            screens.Remove(screen);
            screens.Insert(0, screen);
        }
    }

    public void SetModal(ScreenInfo screenInfo)
    {
        commandQueueAdd.Add(new QueueCommand(GM_Command.MODAL, screenInfo));
    }
    public void SetModal(ScreenInfo screenInfo, bool background)
    {
        modalBack = background;
        SetModal(screenInfo);
    }

    private void SetModal_Implement(ScreenInfo screenInfo)
    {
        modal = screenInfo;
        modal.isModal = true;
        if (screens.Count > 0 && screens[screens.Count - 1] != screenInfo)
        {
            MovetoFront_Implement(screenInfo);
        }
    }

    public void ClearModal()
    {
        modal.isModal = false;
        modal = null;
        GUIUtility.hotControl = 0;
        modalBack = true;
    }

    public bool IsModal()
    {
        return modal != null;
    }

    public GUISkin FindSkin(string skinName)
    {
        foreach (GUISkin skin in skins)
        {
            if (skin.name == skinName)
                return skin;
        }
        return null;
    }
	
	public GUISkin LoadSkin(string skinName)
	{
		GUISkin newskin = Resources.Load("GUISkins/" + skinName,typeof(GUISkin)) as GUISkin;
		if ( newskin != null )
			skins.Add(newskin);
		return FindSkin(skinName);
	}

    public GUIScreen FindScreen(string name)
    {
		if ( name == null )
			return null;

        GUIScreen screen;
        foreach(ScreenInfo screenInfo in screens)
        {
            screen = screenInfo.FindScreen(name);
            if (screen != null)
                return screen;
        }
        return null;
    }

	public GUIScreen FindScreenByType( string className )
	{
		System.Type screenType = System.Type.GetType(className);
		if (screenType == null)
		{
			Debug.LogError("GUIManager.FindScreenByType() : can't find type <" + className + ">");
			return null;
		}
		
		// Find by screen type
		return FindScreenByType(screenType);
	}
	
	public GUIScreen FindScreenByType<T>()
    {
        GUIScreen screen;
        foreach (ScreenInfo screenInfo in screens)
        {
            screen = screenInfo.FindScreenByType<T>();
            if (screen != null)
                return screen;
        }
        return null;
    }

    public GUIScreen FindScreenByType(System.Type type)
    {
        GUIScreen screen;
        foreach (ScreenInfo screenInfo in screens)
        {
            screen = screenInfo.FindScreenByType(type);
            if (screen != null)
                return screen;
        }
        return null;
    }

    public GUIScreen FindScreenByTypeInQueue(System.Type type)
    {
        GUIScreen screen;
        foreach (QueueCommand qc in commandQueue)
        {
            screen = qc.screenInfo.FindScreenByType(type);
            if (screen != null)
                return screen;
        }
        return null;
    }
	
	// generic putmessage handles open/close
	public void PutMessage( GameMsg msg )
	{
		// handle dialog msg
		DialogMsg dmsg = msg as DialogMsg;
		if ( dmsg != null )
		{
			// open is for a new dialog
			if ( dmsg.command == DialogMsg.Cmd.open || dmsg.command == DialogMsg.Cmd.none )// treat 'none' like 'open'
				LoadDialog (dmsg);
			else
			{
				GUIDialog dialog;
				// rest of the commands are for an existing dialog.... find out
				// which dialog...
				dialog = FindScreen (dmsg.name) as GUIDialog;
				if ( dialog != null )
				{
					dialog.PutMessage (dmsg);
					return;
				}
				// we got here, we couldn't find the dialog by name, look by class
				dialog = FindScreenByType(dmsg.className) as GUIDialog;
				if ( dialog != null )
				{
					dialog.PutMessage (dmsg);
					return;
				}
			}
		}		

		GUIScreenMsg smsg = msg as GUIScreenMsg;
		if ( smsg != null && smsg.ScreenName != null )
		{
			GUIScreen screen = FindScreen (smsg.ScreenName);
			if ( screen != null)
				screen.PutMessage(smsg);
		}

		// callback handling
		HandleCallbacks(msg);
	}

    public GUIScreen LoadDialog(DialogMsg msg)
    {
        if (msg == null)
        {
            Debug.LogError("GUIManager.LoadDialog() : GameMsg is null");
            return null;
        }

		// allow NULL className.  This defaults the classname
		// to GUIDialog.
		if ( msg.className == null )
			msg.className = "GUIDialog";

		System.Type screenType = System.Type.GetType(msg.className);
        if (screenType == null)
        {
            Debug.LogError("GUIManager.LoadDialog() : can't find type <" + msg.className + ">");
            return null;
        }

		GUIScreen screen = null;
		
		// newInstance allows us to have duplicate dialogs of the same class
		if ( msg.newInstance == false )
		{
	        // Find by screen type
	        screen = FindScreenByType(screenType);

	        // Find if loaded
	        if (screen != null)
	        {
	            GUIDialog gDialog = (screen as GUIDialog);
	            if (gDialog != null)
	                gDialog.Load(msg);
	            if (msg.modal)
	                SetModal(screen.Parent, msg.modal);
	        }
	        else // Check if loaded but still in the queue
	        {
	            screen = FindScreenByTypeInQueue(screenType);
	            if (screen != null)
	            {
	                GUIDialog gDialog = (screen as GUIDialog);
	                if (gDialog != null)
	                    gDialog.Load(msg);
	                if (msg.modal)
	                    SetModal(screen.Parent, msg.modal);
	            }
	        }
		}

        ScreenInfo screenInfo = null;
        // Screen not found loaded, so load from XML
        if (screen == null && msg.xmlName != null)
        {
            //Debug.Log("GUIManager.LoadDialog() : load xmlName=" + dmsg.xmlName + " : type=" + dmsg.screenType);
            // Screen wasn't found. Try to load the XML
            screenInfo = LoadFromFile(msg.xmlName);
			// load this screen type
            if (screenInfo != null)
            {
				// check to see if screen type differs with msg.className
				if ( screenInfo.Screens != null && screenInfo.Screens.Count > 0 )
				{
					string type = screenInfo.Screens[0].type;
					if ( msg.className != type )
					{
						if ( msg.useMsgClassName == true )
						{
							Debug.Log ("GUIManager.LoadDialog() : swapping type <" + type + "> with <" + msg.className + ">");
							// change type of screen to match msg type
							screenInfo.Screens[0].type = msg.className;
						}
						else
						{
							Debug.Log ("GUIManager.LoadDialog() : swapping type <" + msg.className + "> with <" + type + ">");
							// swap system type to type listed in the screen file
							screenType = System.Type.GetType(screenInfo.Screens[0].type);
							if ( screenType == null )
							{
								Debug.LogError("GUIManager.LoadDialog() : can't find type <" + screen.type + ">");
								return null;
							}
						}
					}
				}
				// change 
				screen = screenInfo.FindScreenByType(screenType);
                if (screen != null)
                {
                    GUIDialog gDialog = (screen as GUIDialog);
                    if(gDialog != null)
                        gDialog.Load(msg);
                    if (msg.modal == true)
                        SetModal(screen.Parent, msg.modal);
                }
                else
                {
                    // Type not found, though file loaded.
                    Debug.LogWarning("GUIManager.LoadDialog() : <" + msg.className + "> not found in loaded ScreenInfo. First Screen in file will be displayed.");
                    if (msg.modal == true)
                        SetModal(screenInfo, msg.modal);
                }
            }
        }
        
        if(screen == null && screenInfo == null)
        {
            Debug.LogError("GUIManager.LoadDialog() : <" + msg.className + "> Dialog couldn't be loaded.");
            return null;
        }

		// this handles positioning and anchor for the open
		if (screen != null)
			screen.PutMessage(msg);

        if (screen != null)
            return screen;
        else
            return screenInfo.Screen;
    }

    public void CloseDialog(DialogMsg msg)
    {
        if (msg == null)
        {
            Debug.LogError("GUIManager.LoadDialog() : GameMsg is null");
            return;
        }

		GUIScreen screen;

		// first try by name
		screen = FindScreen (msg.name);
		if ( screen != null )
		{
			Remove (screen.Parent);
			return;
		}

        // Find by screen type
        screen = FindScreenByType(msg.className);
        if (screen != null)
        {
            Remove(screen.Parent);
        }
    }

    public void CloseDialogByName(DialogMsg msg, string name)
    {
        if (msg == null)
        {
            Debug.LogError("GUIManager.LoadDialog() : GameMsg is null");
            return;
        }

        GUIScreen screen;
        if (msg.command == DialogMsg.Cmd.close)
        {
            // Find by name
            screen = FindScreen(name);
            if (screen != null)
            {
                Remove(screen.Parent);
            }
        }
    }
	
	// this method will convert all the current GUIScreen alternate types to GUIscreen for a save
	public void ConvertToGUIScreen()
	{
		foreach( ScreenInfo si in Screens )
		{
			si.ConvertToGUIScreen();
		}
	}
	
	List<GUIArea> registeredAreas = new List<GUIArea>();
	
	public void RegisterArea( GUIArea area )
	{
		foreach( GUIArea registered in registeredAreas )
		{
			if ( registered == area )
				return;
		}
		registeredAreas.Add (area);
	}

	public void UnRegisterArea( GUIArea area )
	{
		foreach( GUIArea registered in registeredAreas )
		{
			if ( registered == area )
			{
				registeredAreas.Remove (area);
				return;
			}
		}
	}
	
	public void HandleRegisteredAreas()
	{
		foreach( GUIArea area in registeredAreas )
		{
			if ( Application.isEditor == true )
				area.HandleEditorEdit();
			else
				area.HandleEdit();
		}
	}
	
	public bool MouseOverGUI( Vector2 position )
	{
		// invert y
		position.y = Screen.height-position.y;
		// check in screens
		foreach ( ScreenInfo item in Screens )
		{
			if ( item.Screen.MouseOverGUI(position) == true )
				return true;
		}
		return false;
	}
	
#if UNITY_EDITOR
	
	// CUT AND PASTE

	GUIEditObject PasteObject=null;	
	public void SetPasteObject( GUIEditObject paste )
	{
		PasteObject = paste;
	}
	
	public GUIEditObject GetPasteObject()
	{
		return PasteObject;
	}
	
#endif

	public delegate bool PutMessageCallback(GameMsg msg);
	private PutMessageCallback putMessageCallback;
	
	public void AddCallback(PutMessageCallback callback)
	{
		putMessageCallback += callback;
	}
	public void RemoveCallback(PutMessageCallback callback)
	{
		putMessageCallback -= callback;	
	}
	
	public void HandleCallbacks( GameMsg msg )
	{
		if ( putMessageCallback != null )
			putMessageCallback(msg);
	}
	
}

public class DialogMsg : GameMsg
{
	public DialogMsg()
		: base()
	{
		command = Cmd.none;
		title = "";
		text = "";
		x = y = -1;
		time = 0.0f;
		modal = false;
		anchor = "none";
		arguments = new List<string>();
		useMsgClassName = false;
		newInstance = false;
	}
	
	public enum Cmd
	{
		none = 0,
		open,
		close,
		position,
		size,
		anchor,
	}
	
	public Cmd command;
	public int x;
	public int y;
	public int w;
	public int h;
	public string text;
	public string title;
	public float time;
	public bool modal;
	public string anchor; // these are GUIScreen.Anchor
	
	public string name;
	public string xmlName;
	public string className;
	public List<string> arguments;
	public GUIDialog.GUIDialogCallback callback;
	public bool useMsgClassName;
	public bool newInstance;
}



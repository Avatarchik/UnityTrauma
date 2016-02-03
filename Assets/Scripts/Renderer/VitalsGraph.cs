using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VitalsGraph : MonoBehaviour
{
	// allow default parser
	protected VitalsParser parser;
	
    //static private VitalsGraph instance;

    protected GameObject[] trailGenerator;
    protected TrailRenderer[] trailRenderer;
    protected RenderTexture texture;
	Texture2D tex;
	
	public Texture2D Marker;

    protected GameObject active;  // these, and the render textures and trail generators, can be deprecated
									// see the Unity 3.5 branch if you need to know how this used to work
    protected GameObject inactive;
    protected int activeIndex = 0;

    public int textureWidth = 1024;
    public int textureHeight = 512;
	public Color lineColor;

	public float Scale=1.0f;
	public float Offset=0.0f;

    protected float x, y;
	int prev_y;
    protected float timer;
	float elapsedTime = 0;
    public int length = 10;
    public bool run = false;
	float lastRenderTime=0;
	public float renderRate = 0.1f; // how often in sec. to let the camera render. 0 for highest update rate
									// set this to 0 while the GUI is up, 0.1 for in scene display, large when not needed.

    int returnInactive = -1;

    public Texture2D GetTexture()
    {
		if (!run) return null;
        return tex;//ture;
    }

	// Use this for initialization
	protected virtual void Start ()
    {
		tex = new Texture2D(textureWidth,textureHeight,TextureFormat.ARGB32,false);
		Color[] px = new Color[textureWidth*textureHeight];
		for (int p = 0; p<px.Length; p++)
			px[p] = Color.black;
		tex.SetPixels(0,0, textureWidth, textureHeight, px);
		prev_y=textureHeight/2;		
		clearColor = new Color(0,0,0,0);
	}
	
	Color[] pix;
	Color clearColor;
	int lastW=-1,lastH=-1;
	
	protected Color[] InitPix( int width, int height )
	{
		// only create new pix buffer if not big enough
		if ( width <= lastW && height <= lastH )
		{
			// clear it
			for (int i=0 ; i<4*width*height ; i++)
				pix[i] = clearColor;
		}
		else
		{
			pix = new Color[4*width*height];
			lastW = width;
			lastH = height;
		}
		
		return pix;
	}
	
	public int LineWidth=2;
	// Update is called once per frame
	protected virtual void Update ()
    {
		run=Connected(); // this is excessive, it getting attributes is costly, check less often
        if (!run){			
            return;
		}
		// how many texture rows should we draw during the time passed in this update
		int cols = (int)(textureWidth * (Time.deltaTime+elapsedTime) * (1.0f / length)); 
		if (cols == 0){
			elapsedTime += Time.deltaTime;
			return;
		}
		elapsedTime=0;
		      
		Color[] px = InitPix(cols,textureHeight);
		
		for (int p = 0; p<px.Length; p++)
			px[p] = Color.black;
		if ((x+4*cols)<textureWidth) // clear the bar
			tex.SetPixels((int)x+1,0, 4*cols, textureHeight, px); // will fail at end
		
		for (int c=0;c<cols;c++){
			
			float cTime = Time.time-Time.deltaTime*(float)(cols-c)/cols;
			Resample(cTime); // should cause an update of 'y'
			int py = (int)((0.1f+y)*textureHeight);
			bool up=true;
			int rh = py-prev_y;
			int rb = prev_y;
			if (rh<0){
				rh=-rh;
				rb=py;
				up=false;
			}
			rh+=3;
			prev_y=py;
			
			x++;
	        if (x >= textureWidth-1)
	        {
	            x = 0;
	        }			
				
			RenderMarker((int)x); // render marker if there is one
			
			if (x>=0 && x<textureWidth && rb>0 && (rb+rh)<textureHeight)
			{
				//px = new Color[rh*2];
				// read the pixels into px, then blend the alphas up and down
				px = tex.GetPixels((int)x, rb, 1, rh);
				Color pc;
				
				for (int p = 0; p<px.Length; p++)
				{
					pc = lineColor;
					//if (c==0) pc=Color.white;
					//pc.a = px[p].a+((float)p/px.Length);
					px[p] = pc;
				}
				// draw the line thicker in the - direction
				int pos = (int)x;
				for (int i=0 ; i<LineWidth ; i++)
				{
					if ( pos-i >= 0 )
						tex.SetPixels(pos-i, rb, 1, rh, px);					
				}
			}
		}

		if (Time.time-lastRenderTime > renderRate){
			lastRenderTime = Time.time;
			tex.Apply ();
		}
	}
	
	public void SetVitalsParser( VitalsParser parser )
	{
		this.parser = parser;
	}
	
	public VitalsParser GetVitalsParser()
	{
		return parser;
	}
	
	public virtual void Resample(float time)
	{
		if ( parser != null )
		{
        	parser.bpmMod = 1.0f;
			parser.Resample(time);
			VitalsParser.VitalDataPoint pt = parser.GetNextPoint();
			if ( pt != null )
       			y = (parser.DataMax - pt.point)/parser.MaxValue*Scale + Offset;
		}
	}
	
	public virtual bool Connected(){ return true;} // return true if patient attributes indicate the sensors have been connected	
	
	// code to handle markers
	
	public bool newFrame=true;
	public bool NewFrame
	{
		set { newFrame=value; }
		get { return newFrame; }
	}
	public virtual void CheckMode()
	{
		newFrame = true;
	}
	
	bool drawMarker=false;
	public bool DrawMarker
	{
		set { drawMarker = value; }
		get { return drawMarker; }
	}
	
	Color[] markerPixels = null;
	public int MarkerPosition=0;
	
	public virtual void RenderMarker( int col )
	{
		if ( Marker != null && drawMarker == true )
		{
			// set back so that marker doesn't get erased
			col -= Marker.width;
			// clear flag
			drawMarker = false;
			// copy marker to tex
			if ( markerPixels == null )
				markerPixels = Marker.GetPixels();
			// copy image
			if ( col >= 0 )
				tex.SetPixels(col,tex.height-MarkerPosition,Marker.width,Marker.height,markerPixels);
		}
	}
	
	public virtual void ChangeWaveform( string name )
	{
		if ( this.parser != null )
			this.parser.ChangeWaveform(name);
	}
	
	public virtual void ChangeWaveform( string signal, string mode )
	{
		if ( this.parser != null )
			this.parser.ChangeWaveform(signal,mode);
	}

	public delegate void Callback( string command );
	public Callback VitalsCallback;

	public virtual void DoCallback( string command )
	{
		if ( VitalsCallback != null )
			VitalsCallback(command);
	}

    public bool display = false;
    void OnGUI()
    {
        if (display)
            GUILayout.Label(tex);
    }
}
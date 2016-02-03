#define USE_AREA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

public class GUIRenderTexture : MonoBehaviour
{
	public GUIRenderTexture()
	{
	}

	public string XMLName;
	public string ClassName;	
	GUIScreen guiScreen;
	
	bool visible = true;
	public bool Visible
	{
		set { visible = value; }
		get { return visible; }
	}
	
	public bool ForceToGUISize = true;
	bool forceToGUISize = false;
	
	public GameObject gameObject;
	public Rect screenSource;	
	Rect lastRect;
	RenderTexture rt;
	int viewport_x;
	int viewport_y;
	public int xSize = 256; // should use matching power of two here...
	public int ySize = 256;
	public float MipMapBias = -0.5f;	//default bias should result in good render
	Matrix4x4 mapSourceToTextureXfm;
	
	public float updateRate=0.0f;	// 0 is update every frame
	float updateTime=0.0f;
	
	public void Start()
	{
		// load dialog
		Load(XMLName,ClassName);			
	}
	
	public GUIScreen RenderGUI
	{
		get { return guiScreen; }
	}
	
	public GUIScreen Load( string XMLName, string ClassName )
	{
		if ( XMLName == null || XMLName == "" || ClassName == null || ClassName == "" )
			return null;
	
		// this code loads the screen but doesn't start it in the GUI manager
		ScreenInfo si = GUIManager.GetInstance().LoadFromFileRaw(XMLName,ClassName);
		guiScreen = si.Screen;
		// call the class Load method for init
		GUIDialog dialog = si.Screen as GUIDialog;
		if ( dialog != null )
		{			
			// dummy, not really needed
			DialogMsg dmsg = new DialogMsg();
			dmsg.className = ClassName;
			dmsg.xmlName = XMLName;
			// load
			dialog.Load(dmsg);		
		}
		
		// get W/H of overall area
		if ( ForceToGUISize == true )
		{
#if USE_AREA
			screenSource.x = guiScreen.Area.x;
			screenSource.y = guiScreen.Area.y;
			screenSource.width = guiScreen.Area.width;
			screenSource.height = guiScreen.Area.height;
#else
			screenSource.x = guiScreen.Style.contentOffset.x;
			screenSource.y = guiScreen.Style.contentOffset.y;
			screenSource.width = guiScreen.Style.fixedWidth;
			screenSource.height = guiScreen.Style.fixedHeight;
#endif
		}
		// save startup state
		forceToGUISize = ForceToGUISize;
		return guiScreen;
	}
	
	public void Update()
	{
		if ( gameObject == null || guiScreen == null )
			return;
		
		if ( viewport_x != Screen.width || viewport_y != Screen.height || lastRect != screenSource )
		{
			viewport_x = Screen.width;
			viewport_y = Screen.height;
			mapSourceToTextureXfm = SetupMatrix();
			lastRect = screenSource;
		}
		if ( forceToGUISize != ForceToGUISize )
		{
			forceToGUISize = ForceToGUISize;
			if ( ForceToGUISize == true )
			{
#if USE_AREA
				screenSource.x = guiScreen.Area.x;
				screenSource.y = guiScreen.Area.y;
				screenSource.width = guiScreen.Area.width;
				screenSource.height = guiScreen.Area.height;
#else
				screenSource.x = guiScreen.Style.contentOffset.x;
				screenSource.y = guiScreen.Style.contentOffset.y;
				screenSource.width = guiScreen.Style.fixedWidth;
				screenSource.height = guiScreen.Style.fixedHeight;
#endif
			}
		}
		
		CheckClick();
	}
	
	void CheckClick()
	{
		if ( Input.GetMouseButtonUp (0) == true )
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if ( Physics.Raycast(ray,out hit) )
			{
				// make sure there is a mesh collider
				MeshCollider mc = gameObject.GetComponent<MeshCollider>();
				if ( mc != null )
				{
					// make sure the click is on this object
					if ( hit.rigidbody != null && hit.rigidbody.name == gameObject.name )
					{
						//UnityEngine.Debug.Log ("hitname=" + hit.rigidbody.name);
						//UnityEngine.Debug.Log ("UV=" + hit.textureCoord.x + "," + hit.textureCoord.y);
						
						// map this coordinate to the screen
						float x = hit.textureCoord.x * screenSource.width;
						float y = (1.0f-hit.textureCoord.y) * screenSource.height;			
						
						GUIButton b = guiScreen.CheckButtons(new Vector2(x,y));
						if ( b != null )
							UnityEngine.Debug.Log ("GUIRenderTexture.CheckClick() : button=" + b.name);
					}
				}
				else
					UnityEngine.Debug.LogWarning("GUIRenderTexture.CheckClick() : No mesh collider on object <" + gameObject.name + ">");
			}
		}
	}
	
	void OnGUI()
	{
		if ( visible == false )
			return;
		
		if ( gameObject == null || guiScreen == null )
			return;
		
		// handle update rate....if 0 then do every frame
		if ( updateRate != 0.0f )
		{	
			if ( updateTime < Time.time )
				// we can update now, set time for next frame
				updateTime = Time.time - (Time.time-updateTime) + updateRate;
			else
				// not ready, return
				return;
		}
		
		// allocate a render texture to draw thg vitals gui into
		if (rt == null)
		{ 
			rt = new RenderTexture(xSize, ySize, 24); 
			rt.Create();
			// USE THE Render Texture DIRECTLY in our material!
			gameObject.renderer.material.mainTexture = rt; // this so works and saves all the performance impace of ReadPixel!
		}
		// set mipmap bias
		gameObject.renderer.material.mainTexture.mipMapBias = MipMapBias;

		// swap in our render texture as THE ONE to render to...		
		RenderTexture oldRT = RenderTexture.active;
		RenderTexture.active = rt;
		
		// save matrix
		Matrix4x4 temp = GUI.matrix;
		
		// swap in the matrix to map our rectangle to the texture size
		GUI.matrix = mapSourceToTextureXfm;
		
		// we have to draw the vitals gui every frame, or some code that relies on cached control number breaks...
		guiScreen.Execute();
				
		// restore the GUI matrix, we are done with it
		GUI.matrix = temp; 
		
		// restore the RenderTexture
		RenderTexture.active = oldRT; 
	}
	
	public void SetLabel( string labelname, string val )
	{
		GUILabel label = guiScreen.Find(labelname) as GUILabel;
		if ( label != null )
		{
			label.text = val;
		}
	}
	
	// that's all, folks!
	Matrix4x4 SetupMatrix()
	{
		// offset the gui drawing so the corner of the desired area aligns with the corner of the rendertexture,
		// and the size of the desired area completely fills the texture rectangle		
		float gx,gy,tx,ty; // just so i can see these to debug...
		
		// set up the gui matrix, which affects both position and size of rendered items, even fonts!
		gx = xSize/screenSource.width; // scale our desired rectangle into the texture size we are using 
		gy = ySize/screenSource.height; 

		// these offsets are in PRE scaled pixels.
		tx = screenSource.x;
		ty = screenSource.y; 
		
		// There is a hidden transform concatenated with the GUImatrix, which is determined by the screen size.
		// To invert this, we must y offset by screenhieght-textureheight, prescaled by inverse of whatever our y scale factor will be
		// this was corrected in Unity 4.0 and is no longer needed, but please leave the comment just in case... !
		//ty -= (viewport_y-ySize)/gy;

		tx*=gx; // prescale these translations, as they actually happen last, in the sense of matrix concatenation
		ty*=gy;
		
		// although the GUIMatrix appears to be identity when you read it, setting it to
		// a scale factor of 1.0 gives a completely different result than leaving it alone.		
		
		return Matrix4x4.TRS (new Vector3(-tx, -ty, 0), Quaternion.identity, new Vector3 (gx, gy, 1));		
	}
}


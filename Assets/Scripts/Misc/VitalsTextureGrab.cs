using UnityEngine;
using System.Collections;

public class VitalsTextureGrab : MonoBehaviour {
	
	public Rect screenSource = new Rect(-356,-124,626,340);
	public GameObject overlay; // game object of a plane that will use this texture
	public GameObject alsoUse;
	public GUIScreen vitalsGUI;
	RenderTexture rt;
	int viewport_x;
	int viewport_y;
	public int xSize = 256; // should use matching power of two here...
	public int ySize = 256;
	Matrix4x4 mapSourceToTextureXfm;
	Color color1,color2;

	// set the screenSource top and left to be relative to the screen center, although if the dialog is allowed
	// to move, we can add a centerX and centerY

	// Use this for initialization
	void Start () {		
		// build a matrix to map the screenSource rectangle to a render texture
		viewport_x = Screen.width;
		viewport_y = Screen.height;
		mapSourceToTextureXfm = SetupMatrix();

		if (alsoUse != null){
			alsoUse.renderer.material = overlay.renderer.material;
		}
		color1 = new Color(1f,1f,1f,.95f);
		color2 = new Color(1f,1f,1f,.4f);
		
		// create the dialog
		DialogMsg dmsg = new DialogMsg();
		dmsg.xmlName = "dialog.vitals.grapher";
		dmsg.className = "VitalsGUI";
		vitalsGUI = GUIManager.GetInstance().LoadDialog( dmsg );
		vitalsGUI.Close();
	}
	
	// this ongui method is used to draw the vitals monitor GUI dialog into a texture for use in the scene
	
	void OnGUI()
	{	
		if ( vitalsGUI == null )
			return;
		
		overlay.renderer.material.color = color1;
		if (alsoUse != null)
			alsoUse.renderer.material.color = color1;
		if (vitalsGUI == null) {
			overlay.renderer.material.color = color2;
			if (alsoUse != null)
				alsoUse.renderer.material.color = color2;
			return;
		}
		
		// allocate a render texture to draw thg vitals gui into
		if (rt == null){ 
			rt = new RenderTexture(xSize, ySize, 24); 
			rt.Create();
			// USE THE Render Texture DIRECTLY in our material!
			overlay.renderer.material.mainTexture = rt; // this so works and saves all the performance impace of ReadPixel!
			if (alsoUse != null)
				alsoUse.renderer.material.mainTexture = rt;
		}
		
		RenderTexture oldRT = RenderTexture.active;
		RenderTexture.active = rt;

		// since the window can move now, update its coordinates
		// ToDo, adjust for window title bar from gui area data ?
		float titleBarHeight=30;
		screenSource = vitalsGUI.Area;
		screenSource.height-= titleBarHeight;
		screenSource.y += titleBarHeight;
		screenSource.x -= viewport_x/2;
		screenSource.y -= viewport_y/2;
		mapSourceToTextureXfm = SetupMatrix();
		// swap in the matrix to map our rectangle to the texture size
		GUI.matrix = mapSourceToTextureXfm;
		
		// we have to draw the vitals gui every frame, or some code that relies on cached control number breaks...
		vitalsGUI.Execute();
				
		GUI.matrix = Matrix4x4.identity; // restore the GUI matrix, we are done with it

		RenderTexture.active = oldRT; // restore the RenderTexture
	}
	// that's all, folks!
	
	Matrix4x4 SetupMatrix(){
		// offset the gui drawing so the corner of the desired area aligns with the corner of the rendertexture,
		// and the size of the desired area completely fills the texture rectangle		
		float gx,gy,tx,ty; // just so i can see these to debug...
		
		// set up the gui matrix, which affects both position and size of rendered items, even fonts!
		 gx = xSize/screenSource.width; // scale our desired rectangle into the texture size we are using 
		 gy = ySize/screenSource.height; 

		// these offsets are in PRE scaled pixels.
		 tx = viewport_x/2 +screenSource.x;
		 ty = viewport_y/2 +screenSource.y; 
		// There is a hidden transform concatenated with the GUImatrix, which is determined by the screen size.
		// To invert this, we must y offset by screenhieght-textureheight, prescaled by inverse of whatever our y scale factor will be
		// =========== THIS WAS CORRECTED IN UNITY 4.0, so we no longer need it! but leave the comment, PLEASE!
		//ty -= (viewport_y-ySize)/gy;

		tx*=gx; // prescale these translations, as they actually happen last, in the sense of matrix concatenation
		ty*=gy;
		
		// although the GUIMatrix appears to be identity when you read it, setting it to
		// a scale factor of 1.0 gives a completely different result than leaving it alone.		
		
		return Matrix4x4.TRS (new Vector3(-tx, -ty, 0), Quaternion.identity, new Vector3 (gx, gy, 1));		
	}
}

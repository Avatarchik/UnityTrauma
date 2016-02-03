using UnityEngine;

public class GUIPieChartMeshController : MonoBehaviour
{
	public Camera target;
	public int texW=256, texH=256;
	public bool debug=false;
	RenderTexture targetTexture;
	Texture2D texture2D;
	
	public Texture2D Texture 
	{
		get { return texture2D; }
	}
	
    GUIPieChartMesh mPieChart;
    float[] mData;

    void Start()
    {
        mPieChart = gameObject.AddComponent("GUIPieChartMesh") as GUIPieChartMesh;
        if (mPieChart != null)
        {
            mPieChart.Init(mData, 100, 0, 100, null);
            mData = GenerateRandomValues(4);
            mPieChart.Draw(mData);
        }
		
		target.targetTexture = new RenderTexture(texW,texH,24);
    }

    float[] GenerateRandomValues(int length)
    {
        float[] targets = new float[length];

        for (int i = 0; i < length; i++)
        {
            targets[i] = Random.Range(0f, 100f);
        }
        return targets;
    }
	
	public void SetData( float[] data )
	{
        mPieChart.Draw(data);
	}
	
	public Texture2D CreateTexture()
	{
		RenderTexture currentRT = RenderTexture.active;
		RenderTexture.active = target.targetTexture;
		target.Render();
		texture2D = new Texture2D(texW,texH);
		texture2D.ReadPixels(new Rect(0,0,texW,texH),0,0);
		texture2D.Apply();
		RenderTexture.active = currentRT;
		return texture2D;
	}
	
	public void OnGUI()
	{
		if ( debug == false )
			return;
		
		RenderTexture currentRT = RenderTexture.active;
		RenderTexture.active = target.targetTexture;
		target.Render();
		RenderTexture.active = currentRT;
		
		GUI.DrawTexture(new Rect(0,0,256,256),target.targetTexture);	
	}
}

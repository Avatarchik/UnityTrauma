#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections;

public class CubeManager : MonoBehaviour
{
    public bool generate;
    private string path = "cubeGen/Resources/Cubemaps";
    private int size;
    public TextureFormat textureFormat = TextureFormat.ARGB32;
    public bool mipmap;
    public CubePoint[] points;
    public Object[] cubemaps;
	public Camera camera1;
    public int index;
	
	[UnityEditor.MenuItem("HTX Designs/cubeGen %Q")]
    private static void Init()
    {
        GameObject obj1 = GameObject.Find("CubeManager");
        if (null == obj1)
        {
            print("Adding default CubeManager master gameobject");
            obj1 = new GameObject("CubeManager")
            {
                transform = { position = new Vector3(0f, 0f, 0f) }
            };

            obj1.AddComponent<CubeManager>();
        }
    }



    [UnityEditor.MenuItem("HTX Designs/AutoSwap Cubemap for Selected %A")]
    private static void SwapSetup()
    {
        if (Selection.activeTransform != null)
        {
            GameObject temp = Selection.gameObjects[0];
            if (!temp.GetComponent<CubeSwapper>())
                temp.AddComponent<CubeSwapper>();
        }
    }
	
    void Awake()
    {
        if (Application.isEditor)
        {
            switch (index)
            {
                //This sets our resolution from our editor script.
                case 0:
                    size = 32;
                    break;
                case 1:
                    size = 64;
                    break;
                case 2:
                    size = 128;
                    break;
                case 3:
                    size = 256;
                    break;
                case 4:
                    size = 512;
                    break;
                case 5:
                    size = 1024;
                    break;
                case 6:
                    size = 2048;
                    break;
            }
        }
    }

    IEnumerator Start()
    {
        points = GetComponentsInChildren<CubePoint>();
        cubemaps = Resources.LoadAll("Cubemaps", typeof(Cubemap));

        foreach (Object cube in cubemaps)
        {
            foreach (CubePoint point in points)
            {
                if (point.cubePointName == cube.name)
                    point.cubemaps = (Cubemap)cube;
            }
        }
        
        if (Application.isEditor)
        {
			if (generate)
            {
	            foreach (CubePoint point in points)
	            {
	                yield return StartCoroutine(CreateCubemap(point));
	            }
			}
			else {
				yield return new WaitForEndOfFrame();
			}
        }


    }


    IEnumerator CreateCubemap(CubePoint point)
    {
        GameObject camObject = new GameObject("CubemapCamera");
        camObject.transform.parent = point.transform;
        camObject.transform.localPosition = Vector3.zero;
        Camera cam = (Camera)camObject.AddComponent(typeof(Camera));

        cam.fieldOfView = 90;
        cam.depth = 10;
        cam.nearClipPlane = 0.001f;
        cam.farClipPlane = 1000.0f;

        Cubemap cubemap = new Cubemap(this.size, this.textureFormat, this.mipmap);
		//yield return new WaitForEndOfFrame();
       yield return StartCoroutine(Snapshot(cubemap, CubemapFace.PositiveZ, cam));
       yield return StartCoroutine(Snapshot(cubemap, CubemapFace.PositiveX, cam));
        yield return StartCoroutine(Snapshot(cubemap, CubemapFace.NegativeX, cam));
       yield return StartCoroutine(Snapshot(cubemap, CubemapFace.NegativeZ, cam));
       yield return StartCoroutine(Snapshot(cubemap, CubemapFace.PositiveY, cam));
       yield return StartCoroutine(Snapshot(cubemap, CubemapFace.NegativeY, cam));

        cubemap.Apply(mipmap);
        AssetDatabase.CreateAsset(cubemap, "Assets/" + path + "/" + point.cubePointName + ".cubemap");
        AssetDatabase.SaveAssets();

        Destroy(camObject);
    }

    IEnumerator Snapshot(Cubemap cubemap, CubemapFace face, Camera cam)
    {
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(height, height, textureFormat, mipmap);

        cam.transform.localRotation = Rotation(face);
        yield return new WaitForEndOfFrame();

        tex.ReadPixels(new Rect((width - height) / 2, 0, height, height), 0, 0);
        tex.Apply();
        tex = Scale(tex, this.size, this.size);

        Color[] colors = tex.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            cubemap.SetPixel(face, this.size - (i % this.size) - 1, (int)Mathf.Floor(i / this.size), colors[colors.Length - i - 1]);
        }
    }

    private Quaternion Rotation(CubemapFace face)
    {
        Quaternion result = new Quaternion();
        switch (face)
        {
            case CubemapFace.PositiveX:
                result = Quaternion.Euler(0, 90, 0);
                break;
            case CubemapFace.NegativeX:
                result = Quaternion.Euler(0, -90, 0);
                break;
            case CubemapFace.PositiveY:
                result = Quaternion.Euler(-90, 0, 0);
                break;
            case CubemapFace.NegativeY:
                result = Quaternion.Euler(90, 0, 0);
                break;
            case CubemapFace.NegativeZ:
                result = Quaternion.Euler(0, 180, 0);
                break;
            default:
                result = Quaternion.identity;
                break;
        }
        return result;
    }

    private static Texture2D Scale(Texture2D source, int width, int height)
    {
        Texture2D result = new Texture2D(width, height, source.format, true);
        Color[] pixels = result.GetPixels(0);
        float X = ((float)1 / source.width) * ((float)source.width / width);
        float Y = ((float)1 / source.height) * ((float)source.height / height);
        for (int px = 0; px < pixels.Length; px++)
        {
            pixels[px] = source.GetPixelBilinear(X * ((float)px % width),
                                Y * ((float)Mathf.Floor(px / width)));
        }
        result.SetPixels(pixels, 0);
        result.Apply();
        return result;
    }
}
#endif
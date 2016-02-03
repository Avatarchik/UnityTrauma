using UnityEngine;
using System.Collections;

public class ToonOutline : MonoBehaviour
{
    public Material material;
    public Color color = Color.white;
    public float borderSize = 1;
    public bool selected = false;
    bool running = false;
    Renderer myRenderer;
    Material[] oldArray;

    void Start()
    {
        // Hold the renderer to toss the material onto
        myRenderer = renderer;
        //if (myRenderer == null)
        //    myRenderer = GetComponentInChildren<Renderer>();

        // Check if there is a renderer and a set material
        if (myRenderer == null || material == null)
            enabled = false;
    }

    void Update()
    {
        if (selected && !running)
        {
            Material[] newArray = new Material[myRenderer.materials.Length+1];

            // Copy the original array
            oldArray = myRenderer.materials;

            // Copy data to new array.
            myRenderer.materials.CopyTo(newArray, 0);

            // Add custom material to the array
            newArray[newArray.Length - 1] = material;
            myRenderer.materials = newArray;

            material.SetColor("_OutlineColor", color);
            material.SetFloat("_Outline", borderSize * 0.01f);

            running = true;
        }
        else if (!selected && running)
        {
            // Set it back to normal
            myRenderer.materials = oldArray;

            running = false;
        }
    }
}

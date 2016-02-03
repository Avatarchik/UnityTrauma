#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections;

[CustomEditor(typeof(CubePoint))]
class CubePointEditor : Editor
{
    Texture2D aTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/cubeGen/Editor/cubeGen.png", typeof(Texture2D));

    public override void OnInspectorGUI()
    {
        CubePoint temp = (CubePoint)target;
        if (temp != null)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);

            temp.cubePointName = EditorGUILayout.TextField("Cubemap Name:", temp.cubePointName);
			temp.name = temp.cubePointName;
            

            if (aTexture != null)
                GUI.DrawTexture(new Rect(60, 300, 149, 151), aTexture);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            temp.tag = "Cubemap";
        }
    }
}
#endif
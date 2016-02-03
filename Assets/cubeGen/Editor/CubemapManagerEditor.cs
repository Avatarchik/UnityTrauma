#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections;

[CustomEditor(typeof(CubeManager))]
class CubemapManagerEditor : Editor
{
    Texture2D aTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/cubeGen/Editor/cubeGen.png", typeof(Texture2D));

    string[] options = { "32x32", "64x64", "128x128", "256x256", "512x512", "1024x1024", "2048x2048" };

    public override void OnInspectorGUI()
    {
        CubeManager temp = (CubeManager)target;
        if (temp != null)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            EditorGUILayout.LabelField("Resolution");
            temp.index = EditorGUILayout.Popup(temp.index, this.options);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            temp.mipmap = EditorGUILayout.Toggle("Use Mipmaps?", temp.mipmap);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            temp.generate = EditorGUILayout.Toggle("Generate?", temp.generate);
            GUILayout.EndHorizontal();
			
			if (!temp.generate)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Point"))
                {
                    GameObject go = new GameObject("#CubePoint - rename#");
                    if (Selection.activeTransform != null)
                    {
                        go.transform.parent = Selection.activeTransform;
						go.transform.position = go.transform.parent.position;
                        go.AddComponent<CubePoint>();
                    }
                }
                GUILayout.EndHorizontal();
            }
			
            if (temp.generate)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10.0f);
                if (GUILayout.Button("Generate Cubemaps!"))
                    EditorApplication.isPlaying = true;
                GUILayout.EndHorizontal(); 
            }

            if (aTexture != null)
                GUI.DrawTexture(new Rect(60, 300, 149, 151), aTexture);

            GUILayout.EndVertical();
        }
    }
}
#endif
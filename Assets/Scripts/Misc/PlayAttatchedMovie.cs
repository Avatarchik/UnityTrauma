using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class PlayAttatchedMovie : MonoBehaviour
{

#if UNITY_ANDROID
    void Start()
    {
        Application.LoadLevel("MainCombined");
    }
#endif

#if UNITY_STANDALONE_WIN
    public MovieTexture movie;

    void Start()
    {
        renderer.material.mainTexture = movie;
        transform.Rotate(new Vector3(90, 180, 0));
        movie.Play();
        audio.clip = movie.audioClip;
        audio.Play();
    }

    void Update()
    {
        if (!movie.isPlaying)
            Application.LoadLevel("MainCombined");
    }
#endif

#if UNITY_WEBPLAYER
    public MovieTexture movie;

    void Start()
    {
        renderer.material.mainTexture = movie;
        transform.Rotate(new Vector3(90, 180, 0));
        movie.Play();
        audio.clip = movie.audioClip;
        audio.Play();
    }

    void Update()
    {
        if (!movie.isPlaying)
            Application.LoadLevel("MainCombined");
    }
#endif
}
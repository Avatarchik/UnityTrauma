using UnityEngine;
using System.Collections;

public class AudioUtilities : MonoBehaviour
{
    public bool fading = false;

    float startTime;
    float endTime;
    AudioSource start;
    AudioSource end;
    float time;

    void Update()
    {
        if (fading)
        {
            if (Time.time < endTime)
            {

                float i = (Time.time - startTime) / time;

                if (start != null) {
                    start.volume = (1 - i);
                }

                if (end != null)
                {
                	end.volume = i;
                }
            }
            if (Time.time >= endTime)
            {
                if (start != null)
                start.volume = 0f;

                if (end != null)
                {
                    end.volume = 1f;
                }

                start = end = null;
                fading = false;
            }
        }
    }

    public void Crossfade(AudioSource to, AudioSource from, float duration)
    {
        time = duration;
        startTime = Time.time;
        endTime = startTime + duration;

        start = from;
        end = to;
        fading = true;
    }
}
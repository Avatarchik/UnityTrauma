using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

public class AnimationSequenceController : MonoBehaviour
{
    List<AnimationSequence> sequences = new List<AnimationSequence>();

    static AnimationSequenceController instance;

    public AnimationSequenceController()
        : base()
    {
        instance = this;
    }

    public static AnimationSequenceController GetInstance()
    {
        return instance;
    }

    void Update()
    {
        foreach (AnimationSequence sequence in sequences)
        {
            if (!sequence.Update())
            {
                sequences.RemoveAt(0);
                print("Remaining: " + sequences.Count);
                if(sequences.Count > 0) 
                    sequences[0].Begin();
                break;
            }
        }
    }

    public void LoadXML(string filename)
    {
        sequences.Add(new AnimationSequence(filename));
        if (sequences.Count == 1)
        {
            if (!sequences[0].animating)
                sequences[0].Begin();
        }
    }

    public void NextAnim(string origin)
    {
        if (sequences.Count > 0)
            sequences[0].NextAnim(origin);
    }

    public void Clear()
    {
        sequences.Clear();
    }
}
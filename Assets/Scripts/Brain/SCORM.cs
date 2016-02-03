using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// NOTE!  For these routines to work the WEB version must be wrapped in a javascript wrapper with the
// external calls embedded.  Otherwise no go.

public class SCORM
{
    public SCORM()
    {
    }

    /// <summary>
    /// The singleton instance
    /// </summary>
    private static SCORM instance;
    /// <summary>
    /// Creates the game screen manager using the specified texture
    /// </summary>
    /// <param name="texture">The texture to use when fading the screen</param>
    /// <returns>The singleton instance of this class</returns>
    public static SCORM CreateInstance()
    {
        if (instance == null)
            instance = new SCORM();

        return instance;
    }

    /// <summary>
    /// Returns the singleton instance of this class
    /// </summary>
    /// <returns></returns>
    public static SCORM GetInstance()
    {
        if (instance == null)
            instance = CreateInstance();
        return instance;
    }

    public void SaveTurn( int turn, int id, bool result )
    {
        Application.ExternalCall("saveturn", turn.ToString(), id.ToString(), result.ToString());
    }

    public void SaveScore(int score)
    {
        //Application.ExternalCall("saveandscore",score.ToString(),EFMScore.GetInstance().OverallScore().ToString());
    }

    //function SaveObjective(n, id, rawscore, minscore, maxscore, status){
    public void SaveObjective(string n, string id, string rawscore, string minscore, string maxscore, string status)
    {
        // call SCORM
        Application.ExternalCall("SaveObjective",n,id,rawscore,minscore,maxscore,status);

        // also log results
        LogMgr.GetInstance().Add(new ObjectiveLogItem(Time.time,n,id,rawscore,minscore,maxscore,status));
    }
}

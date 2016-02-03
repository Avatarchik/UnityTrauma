using UnityEngine;
using System.Collections;

public class Clock : MonoBehaviour 
{
    public int startTime = 0; // in minutes from midnight
    public bool Mode24Clock = false;
    float timer = 0;
    int hours;
    int minutes;
    bool am = true;

	// Use this for initialization
	void Start () 
    {
        timer += startTime * 60;
	}
	
	// Update is called once per frame
	void Update () 
    {
        timer += Time.deltaTime;
        if (timer >= 86400)
            timer -= 86400;
        Convert();
	}

    void Convert()
    {
        hours = (int)(timer / 3600);
        minutes = (int)(timer / 60) - (hours * 60);

        if (!Mode24Clock)
        {
            if(hours >= 12)
            {
                am = false;
                if (hours > 12)
                    hours -= 12;
            }
            else
                am = true;

            if (hours == 0)
                hours = 12;
        }
    }

    // In minutes
    public void AddTime(int time)
    {
        timer += time * 60;
    }

    public string GetTimeText()
    {
        string hour = hours.ToString().PadLeft(2, '0');
        string minute = minutes.ToString().PadLeft(2, '0');

        if (Mode24Clock)
            return hour + " : " + minute;
        else
            return hour + " : " + minute + (am ? " AM" : " PM");
    }
}

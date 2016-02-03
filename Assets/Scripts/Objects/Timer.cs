using UnityEngine;
using System.Collections;

public class Timer : MonoBehaviour
{
    public int startTime = 0; // in seconds
    int minutes;
    int seconds;
    float timer = 0;

    // Use this for initialization
    void Start()
    {
        timer += startTime;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        Convert();
    }

    void Convert()
    {
        minutes = (int)(timer / 60);
        seconds = (int)timer - (minutes * 60);
    }

    // In seconds
    public void AddTime(int time)
    {
        timer += time;
    }


	public float GetTime(){
		return timer;
	}

    public string GetTimeText()
    {
        string minute = minutes.ToString().PadLeft(2, '0');
        string second = seconds.ToString().PadLeft(2, '0');

        return minute + " : " + second;
    }
}

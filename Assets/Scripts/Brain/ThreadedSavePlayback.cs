using System.Text;
using System.IO;
using System.Net;

public class ThreadedSavePlayback
{
    public volatile bool ThreadRunning = false; // monitor this to shut the thread down when done
	public volatile bool ThreadComplete = false;

	public LogRecord record = null;
	public string filePath = ""; // set this before starting the thread
	public string error = "";

    public void StartSaveCall( ) // this call does the work, passed in when thread is created, executes on thread.start
    {
        ThreadRunning = true;
		ThreadComplete = false;
		try
        {

			// create a playback file
			InteractPlaybackList list = new InteractPlaybackList();
			list.Save(record);
			// now save this file
			list.SaveXML(filePath);
        }
		catch
		{
			error = "error";
		}
        ThreadRunning = false;
		ThreadComplete = true;
    }
}
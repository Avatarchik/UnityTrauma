//#define USE_TEXTURE_MINMAX
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;
using System.IO;
using System.Xml.Serialization;

///
/// Create a heartbeat data file parser that interpolates between data points. Apply a time modifier to time between points to handle point
/// in the beat. Add a datapoint (Y coordinate) to the grapher, which just moves across X at a set time.
///
public class VitalsParser : MonoBehaviour
{
    public class VitalDataPoint
    {
        public float time;
        public float point;

        public VitalDataPoint() { time = 0; point = 0; }
        public VitalDataPoint(float time, float point)
        {
            this.time = time;
            this.point = point;
        }
    }
	
	// class to save/load data from XML
	[System.Serializable]
	public class Waveform
	{
		public string name;
		public string signal;
		public string mode;
		public float rangeMin;
		public float rangeMax;
		public float normalRate;
		public int cycles;
		public int samples;
		public float[] data;
	}
	
	void Serialize(string filename){
		// normalizes the data to the {0.0-1.0f} interval and saves as xml
		Waveform w = new Waveform();
		w.name = filename;
		w.cycles=1;
		w.signal = signal;
		w.mode=filename;
		w.normalRate = normal;
		w.samples = dataPoints.Count;
		
		float scale = dataMax-dataMin;
		if ( scale == 0.0f )
			scale = 1.0f;
		
		w.data = new float[dataPoints.Count];
		
		for (int i=0;i< dataPoints.Count;i++){
			w.data[i]= (dataPoints[i].point-dataMin)/scale;
			
		}
		XmlSerializer serializer = new XmlSerializer(typeof(Waveform));
		FileStream stream = new FileStream(filename + ".xml", FileMode.Create);
		serializer.Serialize(stream, w);
		stream.Close();	
	}	
	
	// NOTE!!  This will only work if textures are set to Read/Write under advanced properties!!!
	// the XML files will be written to the root project folder.  They then need to be copied to
	// the Waveform folder and defined under Waveforms.
	protected void LoadFromTexture( Texture2D texture )
	{
		if ( texture == null )
			return;
		
		int w = texture.width;
		int h = texture.height;
		
		int maxPixel = 0;
		int minPixel = texture.height;
		
		dataPoints = new List<VitalDataPoint>();
		
		// we need to scan from left to right
		for (int i=0 ; i<texture.width ; i++)
		{
			// get column
			Color[] pixels = texture.GetPixels(i,0,1,texture.height,0);
			Color black = new Color(0,0,0,1);
			// check pixels
			int total = 0;
			int count = 0;
			for ( int j=0 ; j<pixels.Length ; j++)
			{
				if ( pixels[j] != black )
				{
					total++;
					count += j;
				}
			}
			// compute average pixel value for this column
			int avePixel=0;
			if ( total == 0 )
				UnityEngine.Debug.LogError ("VitalsParser.LoadFromTexture(" + texture.name + ") : Zero count on column <" + i + ">");
			else
				avePixel = (texture.height-count/total);
			// add point
			dataPoints.Add(new VitalDataPoint(0.0f,(float)avePixel));

			if ( avePixel > maxPixel ) maxPixel = avePixel;
			if ( avePixel < minPixel ) minPixel = avePixel;
		}
		
		// ok, we have the points, now create the parser data
#if USE_TEXTURE_MINMAX
		dataMin = 0;
		dataMax = texture.height;
#else		
		dataMin = minPixel;
		dataMax = maxPixel;
#endif
		signal = texture.name;

		// serialize this!
		Serialize(texture.name);
	}
	
    [System.Serializable]
    public class DataInfo
    {
        public TextAsset dataFile;
        public int normal;
    }

    //public TextAsset[] dataFile;
    public DataInfo[] dataFile;

	public bool buildWaveforms=false;
	public TextAsset[] waveformFiles;
	public Waveform[] waveforms;

	public bool BuildFromTexture=false;
	public Texture2D[] textures;
	
	public string signal;
	public string mode="normal";
    public int dataIndex = 0;
    int oldIndex = 0;
    List<VitalDataPoint> dataPoints = new List<VitalDataPoint>();

    public float length = 0;
    public int normal = 60;
    public int maxValue = 100;
    public float timer = 0;
	float prev_sampleTime = 0;
    public float dataMax = 0;
	float dataMin = 9999999;
	VitalsGraph graph;

    public int nextIndex = 0;
    VitalDataPoint nextPoint;
    VitalDataPoint lastPoint;
	bool increasing = false;
	int nextPointIndex = -1;

    public float bpmMod = 1f;

    public float Length
    {
        get { return length; }
    }
    public float Timer
    {
        get { return timer; }
    }
    public int MaxValue
    {
        get { return maxValue; }
    }
    public float DataMax
    {
        get { return dataMax; }
    }
	
	public string Mode // setting Mode will switch to a waveform with that 'mode' if one exists, ignored otherwise.
	{
		get { return mode; }
		
		set{
			if (value != mode)
				if ( Load(value)) mode = value;
		}
	}

    void Awake()
    {
		if (buildWaveforms){
			for (int i = 0; i< dataFile.Count(); i++){	
				dataIndex = i;
				Load ();
				Serialize (dataFile[i].dataFile.name);
			}
		}

		if ( BuildFromTexture)
		{
			if ( textures != null && textures.Length > 0)
			{
				foreach( Texture2D tex in textures )
					LoadFromTexture(tex);
			}
		}
		
		waveforms = new Waveform[waveformFiles.Length];
		Serializer<Waveform> serializer = new Serializer<Waveform>();
		for (int i=0;i<waveformFiles.Length;i++){
			// use Rob's serializer to load from compiled resources folder at runtime
			waveforms[i] = serializer.Load("XML/Patient/Waveforms/"+waveformFiles[i].name);	
		}
		graph = GetComponent<VitalsGraph>();
    }

    void Start()
    {
		if (!Load(mode)){
			Debug.LogError(name+" vitals parser could not load waveform for mode="+mode);
			enabled = false;
		}
    }
	
	public float MarkerPercentage=0.0f;	
	public bool EnableMarker=true;
	
	void CheckMarker()
	{
		if ( graph == null || EnableMarker == false )
			return;
		
		// make sure to only draw the marker once per frame
		if ( graph.NewFrame == false )
			return;
		
		// limit
		if ( MarkerPercentage > 1.0f )
			MarkerPercentage = 1.0f;
		if ( MarkerPercentage < 0.0f )
			MarkerPercentage = 0.0f;
		
		if ( timer > length*MarkerPercentage )
		{
			// set to draw
			graph.DrawMarker = true;
			// clear flag until frame end
			graph.NewFrame = false;
		}
	}
	
	// we need to keep a historic time value, and apply the current bpmMod to the interval since the last sample
	public void Resample(float time)
	{
		float deltaTime = time-prev_sampleTime;
		prev_sampleTime = time;
		timer += deltaTime * bpmMod * (length * ((float)normal / 60.0f));
		
		CheckMarker ();
		
		nextIndex = (int)timer;//(int)timer;
		while (nextIndex >= (int)length && length != 0){
			nextIndex -= (int)length;
			timer=nextIndex;
			graph.CheckMode();
			graph.DoCallback("EOF");
		}
		
		if (dataPoints != null && nextIndex >=0 && nextIndex < dataPoints.Count)
			nextPoint = dataPoints[nextIndex];

		//UpdatePoints ();
	}
	
	public void Reset()
	{
		nextIndex = 0;
	}

    void UpdatePoints()
    {
        // Modify timer based on Beats-per-minute and ratio of length of data to average beats-per-minute
//        timer += Time.deltaTime * bpmMod * (length * (normal / 60f));
		
		// scan thru all the samples up to present time, and choose the max or min of the curve over the interval.
		// this should really only be done when a point is requested, so we determine from all the sampled points between updates
		// this algorithm is coded so we don't miss extremes of the curve.
		
		float sumValue = 0;
		int numSamples=0;
		int maxIndex = nextIndex;
		int minIndex = nextIndex;
	
		while (true)
        {
            if (nextIndex <= timer)
            {
                // Save last point
                //lastPoint = dataPoints[nextIndex];

                //hitPoints.Add(dataPoints[nextIndex]);
                nextIndex++;
                if (nextIndex >= dataPoints.Count)
                {
                    nextIndex = 0;
                    timer = 0;
					graph.CheckMode();
                }
				
				sumValue+=dataPoints[nextIndex].point;
				numSamples++;
				if (dataPoints[nextIndex].point > dataPoints[maxIndex].point)
					maxIndex = nextIndex;
				if (dataPoints[nextIndex].point < dataPoints[minIndex].point)
					minIndex = nextIndex;
            }
            else
                break;
        }
		if ( sumValue/numSamples > lastPoint.point )
			nextPointIndex = maxIndex;
		else
			nextPointIndex = minIndex;
		nextPoint = dataPoints[nextPointIndex];
		
    }

    public VitalDataPoint GetNextPoint()
    {
		lastPoint = nextPoint;
		nextPointIndex=-1; // causes to recheck increasing/decreasing in update loop.
        return nextPoint;
    }

    public VitalDataPoint GetLastPoint()
    {
        return lastPoint;
    }
	
	// legacy load from old integer text files
    protected void Load()
    {
        dataPoints = new List<VitalDataPoint>();
        string text = dataFile[dataIndex].dataFile.text;
        string temp = "";
        int textLength = text.Length;
        int step = 0;
        float tempValue = 0;
        VitalDataPoint newData = new VitalDataPoint();
		
        bool ranges = true;
		float timePast = -1;
		int numSamples=1;
		float sum = 0;
		dataMax = 0;
		dataMin = 999999;
		
        // For each character in the file
        for (int i = 0; i < textLength; i++)
        {
            // Check if within normal character range
            if (text[i] >= 0x21 && text[i] <= 0x7e)
            {
                // Add to temp string
                temp = temp.Insert(temp.Length, new String(text[i], 1));
            }   // If special character found and temp has data, convert to float and assign to proper variable
            else if (temp.Length != 0)
            {
                tempValue = Convert.ToSingle(temp);
                temp = "";

                switch (step)
                {
                    case 0:
                        {
                            // Grab time
                            newData = new VitalDataPoint();
                            newData.time = tempValue;
                            //newData.timeText = tempValue.ToString();

                            step++;
                        }
                        break;
                    case 1:
                        {
                            // Grab heartrate
                            //newData.values.Add(tempValue);
                            //newData.values.TrimExcess();
                            newData.point = tempValue;
							if (tempValue < dataMin) dataMin=tempValue;
                            if (ranges)
                            {
                                length = newData.time;
                           //     dataMax = (int)newData.point;
							//	dataMin=dataMax;
                                ranges = false;
                            }
                            else
							{
								if (newData.point > dataMax) dataMax=newData.point;
								if (newData.point < dataMin) dataMin=newData.point;
								//These data contain multiple y values per x, reduce to single average value

								// if time value has changed, add the point, otherwise keep an average
								if (newData.time > timePast){
									VitalDataPoint avgData = new VitalDataPoint();
									avgData.time = timePast;
									avgData.point = sum/(float)numSamples;
									if (avgData.time >=0) dataPoints.Add(avgData);
									timePast = newData.time;
									sum=0;
									numSamples=0;
    							}
								sum+=newData.point;
								numSamples++;
							}
                            step = 0;
                        }
                        break;
                }
            }
        }
        dataPoints.TrimExcess();
    }
	
	// if a waveform exists for the requested mode, make it active and return true.
	string currentMode="";	
	bool Load(string mode)
	{
		if ( currentMode == mode )
			return true	;
		
		for (int i=0;i<waveforms.Length;i++){
			if (waveforms[i] != null && waveforms[i].mode == mode)
			{
				currentMode = mode;
				Load (waveforms[i]);	
				return true;
			}
		}
		return false;
	}
	

    protected void Load(Waveform wav,float scale=1.0f)
    {
        dataPoints = new List<VitalDataPoint>();
        VitalDataPoint newData = new VitalDataPoint();
	
		 length = wav.samples;
         dataMax = 0;
		dataMin=9999;

        // For each character in the file
        for (int i = 0; i < length; i++)
        {
			newData = new VitalDataPoint();
            newData.time = i;
	
			newData.point = wav.data[i]*scale;
			if (newData.point>dataMax) dataMax=newData.point;
			if (newData.point<dataMin) dataMin=newData.point;
			dataPoints.Add(newData);
        }
        dataPoints.TrimExcess();
		lastPoint = dataPoints[0];
		nextPoint = dataPoints[0];
    }	
	
	public void ChangeWaveform( string signal, string mode )
	{
		foreach( Waveform w in waveforms )
		{
			if ( w.signal == signal && w.mode == mode )
			{
				Load(w);
				return;
			}
		}
	}
	
	public void ChangeWaveform( string name )
	{
		Load (name);
	}
	
	public void ChangeWaveform( int index )
	{
		for (int i=0 ; i<waveforms.Length ; i++) 
		{
			if ( i == index )
			{
				Load(waveforms[i]);
				return;
			}
		}
	}
}
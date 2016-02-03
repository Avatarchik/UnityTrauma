//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
 
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
 
public static class SaveWav {
 
	const int HEADER_SIZE = 44;
	
	public static MemoryStream Save( AudioClip clip )
	{
		MemoryStream memStream = new MemoryStream();
		ConvertAndWrite(memStream,clip);
		WriteHeader(memStream,clip);
		return memStream;
	}
	
	public static AudioClip TrimSilence(AudioClip clip, float min) 
	{
		// do nothing if clip size is 0
		if ( clip.samples == 0 )
			return clip;
		
		var samples = new float[clip.samples];
 
		clip.GetData(samples, 0);

		return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
	}
 
	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz) {
		return TrimSilence(samples, min, channels, hz, false, false);
	}
 
	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream) {
		int i;
 
		// remove samples at the beginning
		for (i=0; i<samples.Count; i++) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		} 
		samples.RemoveRange(0, i);

		// sanity check
		if ( samples.Count != 0 )
		{ 
			// remove samples at the end
			for (i=samples.Count - 1; i>0; i--) {
				if (Mathf.Abs(samples[i]) > min) {
					break;
				}
			}
			samples.RemoveRange(i, samples.Count - i);
			UnityEngine.Debug.Log ("SaveWav.TrimSilence() samples after begin trim = " + samples.Count);
		}
		else
			UnityEngine.Debug.Log ("SaveWav.TrimSilence() no samples!");

		var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);
 
		if ( samples.Count != 0 )
			clip.SetData(samples.ToArray(), 0);

		return clip;
	}
 
	static FileStream CreateEmpty(string filepath) {
		var fileStream = new FileStream(filepath, FileMode.Create);
	    byte emptyByte = new byte();
 
	    for(int i = 0; i < HEADER_SIZE; i++) //preparing the header
	    {
	        fileStream.WriteByte(emptyByte);
	    }
 
		return fileStream;
	}
 
	static void ConvertAndWrite(MemoryStream memStream, AudioClip clip) {
 
		var samples = new float[clip.samples];
 
		clip.GetData(samples, 0);
 
		Int16[] intData = new Int16[samples.Length];
		//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]
 
		Byte[] bytesData = new Byte[samples.Length * 2];
		//bytesData array is twice the size of
		//dataSource array because a float converted in Int16 is 2 bytes.
 
		int rescaleFactor = 32767; //to convert float to Int16
 
		for (int i = 0; i<samples.Length; i++) {
			intData[i] = (short) (samples[i] * rescaleFactor);
			Byte[] byteArr = new Byte[2];
			byteArr = BitConverter.GetBytes(intData[i]);
			byteArr.CopyTo(bytesData, i * 2);
		}
		
		memStream.Write(bytesData, 0, bytesData.Length);
	}
	
 	static void WriteHeader(MemoryStream memStream, AudioClip clip )
	{
		var hz = clip.frequency;
		var channels = clip.channels;
		var samples = clip.samples;
 
		memStream.Seek(0, SeekOrigin.Begin);
 
		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		memStream.Write(riff, 0, 4);
 
		Byte[] chunkSize = BitConverter.GetBytes(memStream.Length - 8);
		memStream.Write(chunkSize, 0, 4);
 
		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		memStream.Write(wave, 0, 4);
 
		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		memStream.Write(fmt, 0, 4);
 
		Byte[] subChunk1 = BitConverter.GetBytes(16);
		memStream.Write(subChunk1, 0, 4);
 
		UInt16 two = 2;
		UInt16 one = 1;
 
		Byte[] audioFormat = BitConverter.GetBytes(one);
		memStream.Write(audioFormat, 0, 2);
 
		Byte[] numChannels = BitConverter.GetBytes(channels);
		memStream.Write(numChannels, 0, 2);
 
		Byte[] sampleRate = BitConverter.GetBytes(hz);
		memStream.Write(sampleRate, 0, 4);
 
		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		memStream.Write(byteRate, 0, 4);
 
		UInt16 blockAlign = (ushort) (channels * 2);
		memStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
 
		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		memStream.Write(bitsPerSample, 0, 2);
 
		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		memStream.Write(datastring, 0, 4);
 
		Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
		memStream.Write(subChunk2, 0, 4);
	}
 
}
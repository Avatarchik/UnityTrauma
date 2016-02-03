// comment or uncomment the following #define directives
// depending on whether you use KinectExtras together with KinectManager

//#define USE_KINECT_MANAGER

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;


public class SAPIWrapper 
{
	[DllImport("Kernel32.dll", SetLastError = true)]
	static extern uint FormatMessage( uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr pArguments);
 	[DllImport("kernel32.dll", SetLastError = true)]
	static extern IntPtr LocalFree(IntPtr hMem);
	
	// DLL Imports to pull in the necessary functions to access SAPI from Unity
	[DllImport("SAPIUnityWrapper")]
	public static extern int InitSpeechRecognizer([MarshalAs(UnmanagedType.LPWStr)]string sRecoCriteria, bool bUseDictation, bool bAdaptationOff);
	[DllImport("SAPIUnityWrapper")]
	public static extern void FinishSpeechRecognizer();
	[DllImport("SAPIUnityWrapper")]
	public static extern int UpdateSpeechRecognizer();
	[DllImport("SAPIUnityWrapper")]
	public static extern int Speak( [MarshalAs(UnmanagedType.LPWStr)]string sTextToSpeak );
	[DllImport("SAPIUnityWrapper")]
	public static extern int ResetFileProcessing();
	[DllImport("SAPIUnityWrapper")]
	public static extern int ProcessFile( [MarshalAs(UnmanagedType.LPWStr)]string sFilename );
	[DllImport("SAPIUnityWrapper")]
	public static extern int StartListening();
	[DllImport("SAPIUnityWrapper")]
	public static extern int StopListening();
	[DllImport("SAPIUnityWrapper")]
	public static extern int Pause();
	[DllImport("SAPIUnityWrapper")]
	public static extern int Resume();
	[DllImport("SAPIUnityWrapper")]
	public static extern int Mute();
	[DllImport("SAPIUnityWrapper")]
	public static extern int UnMute();
	
	[DllImport("SAPIUnityWrapper")]
	public static extern int LoadSpeechGrammar([MarshalAs(UnmanagedType.LPWStr)]string sFileName, short iNewLangCode);
	[DllImport("SAPIUnityWrapper")]
	public static extern int SetGrammarState( int state );
	[DllImport("SAPIUnityWrapper")]
	public static extern int SetRuleState( [MarshalAs(UnmanagedType.LPWStr)]string sRuleName, int state );
	[DllImport("SAPIUnityWrapper")]
	public static extern void SetRequiredConfidence(float fConfidence);

	[DllImport("SAPIUnityWrapper")]
	public static extern bool IsSoundStarted();
	[DllImport("SAPIUnityWrapper")]
	public static extern bool IsSoundEnded();
	[DllImport("SAPIUnityWrapper")]
	public static extern bool IsPhraseRecognized();
	[DllImport("SAPIUnityWrapper")]
	public static extern IntPtr GetRecognizedTag();
	[DllImport("SAPIUnityWrapper")]
	public static extern IntPtr GetSpokenText();
	[DllImport("SAPIUnityWrapper")]
	public static extern IntPtr GetDebugText();
	[DllImport("SAPIUnityWrapper")]
	public static extern void ClearPhraseRecognized();
	
	public delegate void SpeechStatusDelegate();
	public delegate void SpeechRecoDelegate([MarshalAs(UnmanagedType.LPWStr)]string sRecognizedTag);
//	
	//	[DllImport("SAPIUnityWrapper")]
//	public static extern void SetSoundStartCallback(SpeechStatusDelegate SoundStartDelegate);
	//	[DllImport("SAPIUnityWrapper")]
//	public static extern void SetSoundEndCallback(SpeechStatusDelegate SoundEndDelegate);
	[DllImport("SAPIUnityWrapper")]
	public static extern void SetSpeechRecoCallback(SpeechRecoDelegate SpeechRecognizedDelegate);
	[DllImport("SAPIUnityWrapper")]
	public static extern void SetSpeechRejectCallback(SpeechStatusDelegate SpeechStatusDelegate);
	
	
	// Returns the system error message
	public static string GetSystemErrorMessage(int hr)
	{
		string message = string.Empty;
		uint uhr = (uint)hr;
		
	    const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
	    const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200;
	    const uint FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
	
	    IntPtr lpMsgBuf = IntPtr.Zero;
	
	    uint dwChars = FormatMessage(
	        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
	        IntPtr.Zero,
	        (uint)hr,
	        0, // Default language
	        ref lpMsgBuf,
	        0,
	        IntPtr.Zero);
		
	    if (dwChars > 0)
		{
		    message = Marshal.PtrToStringAnsi(lpMsgBuf).Trim();
		
		    // Free the buffer.
		    LocalFree(lpMsgBuf);
		}
		else
	    {
	        // Handle the error.
	        message = "hr=0x" + uhr.ToString("X");
	    }
	
		return message;
	}

	public static void CopySystemDlls(){

		bool bOneCopied = false;
		bool bAllCopied = false; // not sure why we have these,

		CopyResourceFile("msvcp120.dll", "msvcp120.dll", ref bOneCopied, ref bAllCopied);
		CopyResourceFile("msvcr120.dll", "msvcr120.dll", ref bOneCopied, ref bAllCopied);

	}

	// Copy a resource file to the target
	public static bool ExtractGrammarFile(string targetFilePath, string resFileName)
	{
		// pull the grammar file out of Unity's resources bundle so the SAPI recognizer can load it.
#if UNITY_STANDALONE_WIN
		//UnityEngine.Object testRes = Resources.Load (resFileName);
		TextAsset textRes = Resources.Load (resFileName, typeof(TextAsset)) as TextAsset;
		if(textRes == null)
		{
			return false;
		}
		
		FileInfo targetFile = new FileInfo(Application.dataPath+targetFilePath);
		if(!targetFile.Directory.Exists)
		{
			targetFile.Directory.Create();
		}
		
		if(!targetFile.Exists || targetFile.Length !=  textRes.bytes.Length)
		{
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream (Application.dataPath+targetFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write(textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bool bFileCopied = File.Exists(Application.dataPath+targetFilePath);

				return bFileCopied;
			}
		}

#endif
		return true;
	}
	

	// Copy a resource file to the target
	private static bool CopyResourceFile(string targetFilePath, string resFileName, ref bool bOneCopied, ref bool bAllCopied)
	{
#if UNITY_STANDALONE_WIN
		TextAsset textRes = Resources.Load(resFileName, typeof(TextAsset)) as TextAsset;
		if(textRes == null)
		{
			bOneCopied = false;
			bAllCopied = false;
			
			return false;
		}
		
		FileInfo targetFile = new FileInfo(targetFilePath);
		if(!targetFile.Directory.Exists)
		{
			targetFile.Directory.Create();
		}
		
		if(!targetFile.Exists || targetFile.Length !=  textRes.bytes.Length)
		{
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream (targetFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write(textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bool bFileCopied = File.Exists(targetFilePath);
				
				bOneCopied = bOneCopied || bFileCopied;
				bAllCopied = bAllCopied && bFileCopied;
				
				return bFileCopied;
			}
		}
#endif		
		return false;
	}

	/*
	
	// Copies the needed resources into the project directory
	public static bool EnsureKinectWrapperPresence()
	{
		bool bOneCopied = false, bAllCopied = true;
		
		CopyResourceFile("KinectUnityWrapper.dll", "KinectUnityWrapper.dll", ref bOneCopied, ref bAllCopied);
		CopyResourceFile("KinectInteraction180_32.dll", "KinectInteraction180_32.dll", ref bOneCopied, ref bAllCopied);
		CopyResourceFile("FaceTrackData.dll", "FaceTrackData.dll", ref bOneCopied, ref bAllCopied);
		CopyResourceFile("FaceTrackLib.dll", "FaceTrackLib.dll", ref bOneCopied, ref bAllCopied);
		
		CopyResourceFile("msvcp100d.dll", "msvcp100d.dll", ref bOneCopied, ref bAllCopied);
		CopyResourceFile("msvcr100d.dll", "msvcr100d.dll", ref bOneCopied, ref bAllCopied);

		if(!File.Exists("SpeechGrammar.grxml"))
		{
			TextAsset textRes = Resources.Load("SpeechGrammar.grxml", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				string sResText = textRes.text;
				File.WriteAllText("SpeechGrammar.grxml", sResText);
				
				bOneCopied = bOneCopied || File.Exists("SpeechGrammar.grxml");
				bAllCopied = bAllCopied && bOneCopied;
			}
		}

		
		return bOneCopied && bAllCopied;
	}
	*/
}

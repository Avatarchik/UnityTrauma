using UnityEngine;
using System.Collections;

public class GUIFader
{
	float currAlpha=1.0f;
	float seekAlpha=1.0f;
	float startAlpha;
	float alphaTime=0.0f;
	float alphaStartTime=0.0f;
	Color beforeFadeColor;

	public delegate void Callback();
	Callback callback;
	
	public virtual void Fade( float alpha, float time, Callback callback=null, float forceAlpha=-1 )
	{
		// if startAlpha is set we are forcing alpha
		if ( forceAlpha != -1 )
			currAlpha = forceAlpha;
		// set starting Fade value
		startAlpha = currAlpha;
		// seek fade is where we're going
		seekAlpha = alpha;
		// alpha time is time to get there
		alphaTime = time;
		// set alphaStartTime
		alphaStartTime = Time.time;
		// set callback
		this.callback = callback;
	}
	
	public virtual void FadeIn()
	{
		if ( alphaTime == 0.0f )
			return;

		// get elapsed time since start of fade
		float elapsed = Time.time - alphaStartTime;
		// fraction of time elapsed
		float fraction=1.0f;
		if ( alphaTime > 0.0f )
			fraction = elapsed/alphaTime;
		if ( fraction > 1.0f ) 
			fraction = 1.0f;
		// lerp color by fraction
		beforeFadeColor = GUI.color;
		// set alpha to Lerped value
		currAlpha = Mathf.Lerp(startAlpha,seekAlpha,fraction);
		GUI.color = new Color(beforeFadeColor.r,beforeFadeColor.g,beforeFadeColor.b,currAlpha);
	}
	
	public virtual void FadeOut()
	{
		if ( alphaTime == 0.0f )
			return;
		
		// restore original color
		GUI.color = beforeFadeColor;
		// do callback when we're done with fade
		if ( FadeDone() == true )
		{
			// do callback
			if ( callback != null )
				callback();
			// only do callback once
			callback = null;
		}
		// for testing
		//Test ();
	}

	public virtual bool FadeDone()
	{
		return ( currAlpha == seekAlpha );
	}

	void Test()
	{
		if ( FadeDone() == true )
		{
			if ( currAlpha == 1.0f )
				Fade(0.0f,2.0f);
			if ( currAlpha == 0.0f )
				Fade(1.0f,2.0f);
		}
	}
}


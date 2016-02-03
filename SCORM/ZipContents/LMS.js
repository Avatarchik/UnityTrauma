<!-- Inserted by:
// SCORM Wrapper
// version 1.0     10.31.07


// Set up handle for SCORM API
var SCORM_API = null;

// Function to locate SCORM API
function find_SCORM_API()
{
  var myAPI = null;
  var tries = 0, triesMax = 500;
  while (tries < triesMax && myAPI == null)
  {
    window.status = 'Looking for API object ' + tries + '/' + triesMax;
    myAPI = grab_SCORM_API(window);
    if (myAPI == null && typeof(window.parent) != 'undefined') myAPI = grab_SCORM_API(window.parent)
    if (myAPI == null && typeof(window.top) != 'undefined') myAPI = grab_SCORM_API(window.top);
    if (myAPI == null && typeof(window.opener) != 'undefined') if (window.opener != null && !window.opener.closed) myAPI = grab_SCORM_API(window.opener);
    tries++;
  }
  if (myAPI == null)
  {
    window.status = 'API was not found';
  }
  else
  {
    SCORM_API = myAPI;
    window.status = 'API has been found';
  }
}

// Function to grab the SCORM API
function grab_SCORM_API(win)
{
  // look in this window
  if (typeof(win) != 'undefined' ? typeof(win.API) != 'undefined' : false)
  {
    if (win.API != null )  return win.API;
  }
  // look in this window's frameset kin (except opener)
  if (win.frames.length > 0)  for (var i = 0 ; i < win.frames.length ; i++);
  {
    if (typeof(win.frames[i]) != 'undefined' ? typeof(win.frames[i].API) != 'undefined' : false)
    {
	     if (win.frames[i].API != null)  return win.frames[i].API;
    }
  }
  return null;
}

// call LMSInitialize()
function initLMS()
{
  if (SCORM_API != null)
  {
    SCORM_API.LMSInitialize("");
  }
}


// call LMSFinish()
function finishLMS()
{
  if (SCORM_API != null)
  {
    // set status
    SCORM_API.LMSCommit("");
    SCORM_API.LMSFinish("");
  }
}


function saveinteraction(n, id, correctanswernum, student_response){
  // compare students answer to correct answer
  if(correctanswernum == student_response){
    result = "correct"
  } else {
    result = "wrong"
  }

  // If the API is available
  if (SCORM_API != null)
  {
    // write the interaction info
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".id", id);
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".type", "choice");
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".correct_responses.0.pattern", correctanswernum);
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".weighting", 1);
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".student_response", student_response);
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".result", result);

    SCORM_API.LMSCommit("");
  }
}

function saveturn(n,id,passfail)
{
  // If the API is available
  if (SCORM_API != null)
  {
    // write the interaction info
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".id", id);
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".type", "choice");
    SCORM_API.LMSSetValue("cmi.interactions." + n + ".result", passfail);

    SCORM_API.LMSCommit("");
  }
}

function saveandscore(scoretosend, scoremax)
{
  // If the API is available
  if (SCORM_API != null)
  {
    // write the score values
    SCORM_API.LMSSetValue("cmi.core.score.min", 0);
    SCORM_API.LMSSetValue("cmi.core.score.max", scoremax);
    SCORM_API.LMSSetValue("cmi.core.score.raw", scoretosend);

    // if the score is above the 'high' score
    if(parseInt(scoretosend) >= parseInt(scoremax)){
	// set as passed
        SCORM_API.LMSSetValue("cmi.core.lesson_status", "passed");
    } else {
	// set as complete
        SCORM_API.LMSSetValue("cmi.core.lesson_status", "incomplete");
    }
    SCORM_API.LMSCommit("");
  }
}


function closewindow()
{
   self.close();
}


// Get the API
find_SCORM_API();

// Initialize the LMS
initLMS();



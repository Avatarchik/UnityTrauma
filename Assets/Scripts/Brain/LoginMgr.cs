#define USE_LMS_LOGIN
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GameMgr
{
	private static GameMgr instance;
    public static GameMgr GetInstance()
    {
        if (instance == null)
            instance = new GameMgr();
        return instance;
    }
	
	public GameMgr()
	{
		// default to trauma for now
		DatabaseURL = "http://unity.sitelms.org/Trauma/traumaDB.php";
		Game = "trauma";
	}
	
	public string DatabaseURL;
	public string Game;
	public string Scenario;
}

public class LoginMgr
{
	public LoginMgr ()
	{
		Username = "guest";
		Game = "trauma";
		validLogin = false;
	}

	private static LoginMgr instance;
    public static LoginMgr GetInstance()
    {
        if (instance == null)
            instance = new LoginMgr();

        return instance;
    }

	public string Game;
	public string URL;	

	public string AdminName="admin";
	public string AdminPassword="s!tel";

	public bool AllowGuest=false;

	string username;
	string password;
	bool validLogin;
	bool admin;	
	
	public List<string> Cases;
	
	public class LoginInfo
	{
		public string username;
		public string password;
		public string datetime;
		public string first;
		public string last;
		public bool admin;
	}
	public List<LoginInfo> LoginList = new List<LoginInfo>();
	
	public string Username
	{
		get { return username; }
		set { username=value; }
	}
	
	public bool Admin
	{
		get { return admin; }
	}
	
	public bool ValidLogin
	{
		get { return validLogin; }
		set { validLogin = value; }
	}

	DatabaseMgr.Callback checkLoginCallback;
	
	public void CheckLogin( string username, string password, DatabaseMgr.Callback callback )
	{
		this.username = username;
		this.password = password;

		// check for built-in admin
		if ( username == AdminName && password == AdminPassword )
		{
			admin = true;
			validLogin = true;
			if ( callback != null )
				callback(true,"","",null);
			return;
		}

		if ( AllowGuest == true && username == "guest" )
		{
			admin = false;
			validLogin = true;
			if ( callback != null )
				callback(true,"","",null);
			return;
		}

		checkLoginCallback = callback;

#if USE_LMS_LOGIN
		LMSIntegration.GetInstance().LMSLoginWithPing(username,password,LoginCallback);
#else

		WWWForm form = new WWWForm();
		form.AddField("command","login");
        form.AddField("username", username);
        form.AddField("password", password);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,LoginCallback);		
#endif
	}
	
	public void CheckLoginWithPing( string username, string password, DatabaseMgr.Callback callback )
	{
		this.username = username;
		this.password = password;
		
		// check for built-in admin
		if ( username == AdminName && password == AdminPassword )
		{
			admin = true;
			validLogin = true;
			if ( callback != null )
				callback(true,"","",null);
			return;
		}
		
		checkLoginCallback = callback;
		
		LMSIntegration.GetInstance().LMSLoginWithPing(username,password,LoginCallback);
	}

	void LoginCallback(bool status, string data, string error_msg, WWW download)
	{
		// clear
		validLogin = false;
		// if not username or everything ok then just return
		if ( status == true )
		{
#if USE_LMS_LOGIN
			if ( data == "ok" )
			{
				validLogin = true;
				admin = false;
			}
			else
			{
				// not valid but check to see if username is empty
				if ( AllowGuest == true && username == "" )
				{
					// empty login, use guest
					username = "guest";
					admin = false;
					// make ok for now
					validLogin = true;
				}
			}
#else
			// parse the reply
			string[] tokens = data.Split('&');
			if ( tokens.Length == 3 )
			{
				if ( tokens[0] != "" )
				{
					if ( tokens[1] == password )
						validLogin = true;
					admin = (tokens[2]=="1")?true:false;
				}
				else
				{
					if ( AllowGuest == true )
					{
						// empty login, use guest
						username = "guest";
						admin = false;
						// make ok for now
						validLogin = true;
					}
				}
			}
#endif
		}
		else
		{
			// no connection or error
			// not valid but check to see if username is empty
			if ( AllowGuest == true && username == "" )
			{
				// empty login, use guest
				username = "guest";
				admin = false;
				// make ok for now
				validLogin = true;
			}
		}
		if ( checkLoginCallback != null )
			checkLoginCallback(status,data,error_msg,download);
	}

	public void GetAdminRights()
	{
		WWWForm form = new WWWForm();
		form.AddField("command","getAdminRights");
        form.AddField("username", username);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,RightsCallback);		
	}
	
	void RightsCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			if ( data == "true" )
				admin = true;
			else
				admin = false;
		}
	}
	
	public void SetAdminRights( bool admin )
	{
		WWWForm form = new WWWForm();
		form.AddField("command","setAdminRights");
        form.AddField("username", username);
		form.AddField("admin", admin.ToString());
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,null);		
	}
	
	public void GetValidCases()
	{
		WWWForm form = new WWWForm();
		form.AddField("command","getValidCases");
        form.AddField("username",username);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,CasesCallback);				
	}
	
	public void CasesCallback(bool status, string data, string error_msg, WWW download)
	{
		if ( status == true )
		{
			Serializer<List<string>> serializer = new Serializer<List<string>>();
			Cases = serializer.FromString(data);
		}
	}
	
	public void setValidCases( List<string> cases )
	{
		Serializer<List<string>> serializer = new Serializer<List<string>>();
		string caseData = serializer.ToString(cases);
		
		WWWForm form = new WWWForm();
		form.AddField("command","setValidCases");
        form.AddField("username", username);
		form.AddField("data", caseData);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,null);				
	}

	DatabaseMgr.Callback loginsCallback;
	
	public void getLoginsCallback(bool status, string data, string error_msg, WWW download)
	{
		// fill list
		LoginList.Clear();
		
		string[] split = data.Split('#');
		foreach( string item in split )
		{
			string[] fields = item.Split('&');
			if ( fields.Length == 6 )
			{
				LoginInfo ci = new LoginInfo();
				ci.username = fields[0];
				ci.password = fields[1];
				ci.datetime = fields[2];
				ci.first = fields[3];
				ci.last = fields[4];
				ci.admin = (fields[5]=="1")?true:false;
				LoginList.Add(ci);
			}
		}
		
		// do callback
		if ( loginsCallback != null )
			loginsCallback(status,data,error_msg,download);
	}
	
	public LoginInfo GetLoginInfo( string username )
	{
		foreach( LoginInfo info in LoginList )
		{
			if ( info.username == username )
				return info;
		}
		return null;
	}
	
	public void GetLogins( DatabaseMgr.Callback callback )
	{
		WWWForm form = new WWWForm();
		form.AddField("command","loadLogins");
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,getLoginsCallback);				
		loginsCallback = callback;
	}
	
	public bool UserExists( string name )
	{
		if ( GetLoginInfo(name) != null )
			return true;
		else
			return false;
	}
	
	public void CreateUser( string username, string password, string first, string last, bool admin, bool tutorial, DatabaseMgr.Callback createCallback )
	{
		// create empty case list
		Serializer<List<string>> serializer = new Serializer<List<string>>();
		string emptyCaseList = serializer.ToString(new List<string>());
		
		// create user in logins
		WWWForm form = new WWWForm();
		form.AddField("command","createUser");
		form.AddField("username",username);
		form.AddField("password",password);
		form.AddField("first",first);
		form.AddField("last",last);
		form.AddField("admin",(admin==true)?"1":"0");
		form.AddField("tutorial",(tutorial==true)?"1":"0");
		form.AddField("data",emptyCaseList);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,createCallback);		
	}
	
	public void AddUserCase( string username, string casename )
	{
		WWWForm form = new WWWForm();
		form.AddField("command","addUserCase");
		form.AddField("username",username);
		form.AddField("casename",casename);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,null);				
	}
	
	public void removeUserCase( string username, string casename )
	{
		WWWForm form = new WWWForm();
		form.AddField("command","removeUserCase");
		form.AddField("username",username);
		form.AddField("casename",casename);
		DatabaseMgr.GetInstance().DBCall(GameMgr.GetInstance().DatabaseURL,form,null);				
	}
}


using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Serializable]
public class StringMap
{
    public string key;
    public string value;

    public StringMap()
    {
    }

    public StringMap(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

public class StringMgr
{
    public StringMgr()
    {
    }

    protected List<StringMap> stringmap;

    static StringMgr instance;
    public static StringMgr GetInstance()
    {
        if (instance == null)
        {
            instance = new StringMgr();
            instance.Load();
        }
        return instance;
    }

    public void Load( string filename=null )
    {
//        Debug.Log("StringMgr:Load XML/stringmap");

        if ( stringmap == null )
            stringmap = new List<StringMap>();
		
		if ( filename != null )
			LoadXML (filename);
		else
        	LoadXML("XML/stringmap");
        return;
    }

    public virtual void LoadXML(string filename)
    {
        Serializer<List<StringMap>> serializer = new Serializer<List<StringMap>>();
        List<StringMap> tmp = serializer.Load(filename);
        if (tmp != null)
            stringmap = tmp;
    }

    public void SaveXML(string filename, List<StringMap> stringmap )
    {
        Serializer<List<StringMap>> serializer = new Serializer<List<StringMap>>();
        serializer.Save(filename, stringmap);
    }

    public void Load(string tag, string value)
    {
		// first look up to see if we need to replace
		foreach( StringMap map in stringmap )
		{
			if ( map.key == tag )
			{
				map.value = value;				
				return;
			}
		}
		// not found, just add a new one
        StringMap newmap = new StringMap(tag, value);
        stringmap.Add(newmap);
    }

    public string Get(string tag)
    {
        if (stringmap == null)
        {
            Debug.Log("StringMgr.Get() stringmap is null.  Calling .Get in a constructor?");
            Load();
        }

        if (stringmap != null)
        {
            foreach (StringMap map in stringmap)
            {
                if (map.key == tag)
                    return map.value;
            }
        }
        return tag;
    }
	
	// save edits or add new entry, and update the XML file.
	public void UpdateOrAdd(string key, string newvalue){
		for (int i = 0; i< stringmap.Count; i++){
			if (stringmap[i].key == key){
				stringmap[i].value = newvalue;
				SaveXML ("Assets/Resources/XML/stringmap.xml",stringmap);
				return;
			}
		}
		// key not found, so add
		Load (key,newvalue);
		SaveXML ("Assets/Resources/XML/stringmap.xml",stringmap);
	}
}

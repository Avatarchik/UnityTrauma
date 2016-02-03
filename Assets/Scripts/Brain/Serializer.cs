using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

public class Serializer<T>
{
    public T Load(string filename)
    {
        try
        {
            // Create an instance of the XmlSerializer class;
            // specify the type of object to be deserialized.
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            /* If the XML document has been altered with unknown 
            nodes or attributes, handle them with the 
            UnknownNode and UnknownAttribute events.*/
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

            // read from Unity TextAsset
            TextAsset ta = (TextAsset)Resources.Load(filename, typeof(TextAsset));
            if (ta == null)
            {
                Debug.Log("Serializer: can't find TextAsset <" + filename + ">");
                return default(T);
            }
            StringReader reader = new StringReader(ta.text);

#if DEBUG_STRINGS
            string txt;
            while ( (txt = reader.ReadLine()) != null )
                Debug.Log("-->" + txt);
#endif

#if USE_FILE
            // read from FILE
            // A FileStream is needed to read the XML document.
            FileStream reader = new FileStream(filename, FileMode.Open);
#endif

            /* Use the Deserialize method to restore the object's state with
            data from the XML document. */
            T ss = (T)serializer.Deserialize(reader);
            return ss;
        }
        catch (Exception ex)
        {
            Debug.Log("Serializer Exception: " + ex.ToString() + "in filename< " + filename + ">");
        }

        return default(T);
    }
	
	public T Load(StreamReader reader)
	{
		try
		{
            // Create an instance of the XmlSerializer class;
            // specify the type of object to be deserialized.
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            /* If the XML document has been altered with unknown 
            nodes or attributes, handle them with the 
            UnknownNode and UnknownAttribute events.*/
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

            /* Use the Deserialize method to restore the object's state with
            data from the XML document. */
            T ss = (T)serializer.Deserialize(reader);
            return ss;
		}
		catch (Exception ex)
		{
            Debug.Log("Serializer Exception <StreamReader>: " + ex.ToString());
		}		
		
		return default(T);
	}

    private void serializer_UnknownNode
    (object sender, XmlNodeEventArgs e)
    {
        Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
    }

    private void serializer_UnknownAttribute
    (object sender, XmlAttributeEventArgs e)
    {
        System.Xml.XmlAttribute attr = e.Attr;
        Console.WriteLine("Unknown attribute " +
        attr.Name + "='" + attr.Value + "'");
    }

    public bool Save(string filename, T ss)
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (serializer != null)
            {
                TextWriter writer = new StreamWriter(filename);
                serializer.Serialize(writer, ss);
                writer.Close();

                Debug.Log("FILE:" + filename + " saved...");
#if UNITY_EDITOR
				// call to update the database to make sure this asset
				// will be reimported
				UnityEditor.AssetDatabase.Refresh();
#endif
				return true;
			}
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
		return false;
	}

    public string ToString(T ss)
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (serializer != null)
            {
                MemoryStream memory = new MemoryStream();
                serializer.Serialize(memory, ss);

                System.Text.Encoding encoding = new System.Text.ASCIIEncoding();
                return encoding.GetString(memory.ToArray());
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        return null;
    }

    public T FromString( string data )
    {
        try
        {
            // Create an instance of the XmlSerializer class;
            // specify the type of object to be deserialized.
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            /* If the XML document has been altered with unknown 
            nodes or attributes, handle them with the 
            UnknownNode and UnknownAttribute events.*/
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

            StringReader reader = new StringReader(data);

#if DEBUG_STRINGS
            string txt;
            while ( (txt = reader.ReadLine()) != null )
                Debug.Log("-->" + txt);
#endif

#if USE_FILE
            // read from FILE
            // A FileStream is needed to read the XML document.
            FileStream reader = new FileStream(filename, FileMode.Open);
#endif

            /* Use the Deserialize method to restore the object's state with
            data from the XML document. */
            T ss = (T)serializer.Deserialize(reader);
            return ss;
        }
        catch (Exception ex)
        {
            Debug.Log("Serializer Exception: " + ex.ToString());
        }

        return default(T);
    }
}

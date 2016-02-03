using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(ScreenInfoData))]

public class ScreenInfoInspector : Editor 
{
    protected SIIMenuTreeNode menuTree;
    protected bool onSelected = false;
    protected bool showDefaultInspector = true;
    protected ScreenInfoData myObject;

	public void Awake()
    {
	}
	
	virtual public void OnSelected() // look at a particular instance object
	{
        myObject = target as ScreenInfoData;
        menuTree = SIIMenuTreeNode.BuildMenu(myObject.XMLFile.name);
	}

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Show Default Inspector");
        GUILayout.FlexibleSpace();
        Color oldColor = GUI.color;
        showDefaultInspector = GUILayout.Toggle(showDefaultInspector, "");
        GUILayout.EndHorizontal();

        if (showDefaultInspector)
            DrawDefaultInspector();

        if (!onSelected) // this must be Pauls name.
        {
            onSelected = true;
            OnSelected();  //?this is called just to get OnSelected called when the first gui call happens ?
        }

        // Save / Load
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SAVE TO OBJECT"))
        {
            if (menuTree.info != null)
            {
                //myObject.screenInfo = menuTree.info;
                myObject.screenInfo = new ScreenInfo();
                myObject.screenInfo.Screens = new List<GUIScreen>();
                foreach (GUIScreen screen in menuTree.info.Screens)
                {
                    GUIScreen newScreen = new GUIScreen();
                    newScreen.CopyFrom(screen);
                    myObject.screenInfo.Screens.Add(newScreen);
                }
            }
        }
        if (GUILayout.Button("READ FROM OBJECT"))
        {
            menuTree = SIIMenuTreeNode.BuildMenu(myObject.screenInfo);
        }
        GUILayout.EndHorizontal();

        GUI.color = Color.cyan;
        if (menuTree != null)
            menuTree.ShowInspectorGUI(0);
        else
        {
            GUILayout.Button("No Menu exists for XMLName");
        }
        GUI.color = oldColor;
        return;
    }

    void Update()
    {
    }
}

public abstract class GUIMenuTreeNode
{
    public bool expanded = false;
    public bool expandList = false;
    public bool valid = true; // when adding a new node, set to true when all required fields have values.

    // these local varaibles are for the editor GUI, letting you open one task,list or stringmap per node for editing.
    public string editingTaskKey = ""; // the task at this node open for edit
    public string editingStrmapKey = "";
    public StringMap editStrmap = new StringMap(); // this persistent value lets us edit a stringmap

    public string indent = "";
    public string xp = "";
    public virtual void ShowInspectorGUI(int level)
    {
        EditorGUILayout.Space();
        if (level == -1)
            GUILayout.Label("============ CREATING NEW ITEM =============");
        else if (level == 0)
        {
            //GUILayout.Label("VVV >> FOR TESTING ONLY, STILL WORK IN PROGRESS << VVV");
            GUILayout.Label("============ GUI MENUS =============");
        }

        indent = "";
        xp = expanded ? "V " : "> ";
        for (int i = 0; i < level; i++)
            indent += "__"; // just a string to add space on the left
    }

    protected bool DeleteButton()
    {
        Color old = GUI.color;
        GUI.color = Color.red;
        bool clicked = GUILayout.Button("X", GUILayout.ExpandWidth(false));
        GUI.color = old;
        return clicked;
    }
}

public class SIIMenuTreeNode : GUIMenuTreeNode
{
    public string filePath = null;
    public ScreenInfo info;

    public List<ScreenMenuTreeNode> children;
    ScreenMenuTreeNode newNode; // used when building up new menus


    public static SIIMenuTreeNode BuildMenu(string fileName) // pass in the relative path
    {
        SIIMenuTreeNode returnNode = new SIIMenuTreeNode();
        returnNode.filePath = fileName;

        Serializer<ScreenInfo> serializer = new Serializer<ScreenInfo>();
        ScreenInfo screenInfo = serializer.Load("GUIScripts/" + fileName); // if this load fails, then we should probably create a default empty file
        // If null, the load failed.
        if (screenInfo == null) return null;

        returnNode.info = new ScreenInfo();
        returnNode.info.Screens = screenInfo.Screens;
        returnNode.info.Initialize();

        returnNode.children = new List<ScreenMenuTreeNode>();

        foreach (GUIScreen screen in returnNode.info.Screens)
        {
            ScreenMenuTreeNode newNode = ScreenMenuTreeNode.BuildMenu(screen);
            newNode.parent = returnNode;
            returnNode.children.Add(newNode);
        }

        return returnNode;
    }

    public static SIIMenuTreeNode BuildMenu(ScreenInfo screenInfo)
    {
        if (screenInfo == null) return null;

        SIIMenuTreeNode returnNode = new SIIMenuTreeNode();
        returnNode.filePath = "Object";
        returnNode.info = new ScreenInfo();
        returnNode.info.Screens = new List<GUIScreen>();

        returnNode.children = new List<ScreenMenuTreeNode>();

        foreach(GUIScreen screen in screenInfo.Screens)
        {
            GUIScreen newScreen = new GUIScreen();
            newScreen.CopyFrom(screen);
            returnNode.info.Screens.Add(newScreen);

            ScreenMenuTreeNode newNode = ScreenMenuTreeNode.BuildMenu(newScreen);
            newNode.parent = returnNode;
            returnNode.children.Add(newNode);
        }

        return returnNode;
    }

    public void RemoveChild(ScreenMenuTreeNode child)
    {
        info.RemoveScreen(child.info);
        children.Remove(child);
    }

	// this method recursively displays the menu tree for the Editor inspector, and can call methods to add and save
    public override void ShowInspectorGUI(int level)
    {
        base.ShowInspectorGUI(level);

        GUILayout.BeginHorizontal();
        GUILayout.Label(indent + "ScreenInfo");
        bool clicked = GUILayout.Button(xp + "Screen Info", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();
        if (clicked) expanded = !expanded;
        if (expanded)
        {
            Color topColor = GUI.color;
            Color nextColor = topColor;
            nextColor.r = 0.75f;
            nextColor.g = 0.9f;
            GUI.color = nextColor;

            // Display source
            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Loaded From:");
            //filePath = GUILayout.TextField(filePath);
            //GUILayout.EndHorizontal();

            GUILayout.Label("Screens:");
            // Display children
            foreach (ScreenMenuTreeNode n in children)
                n.ShowInspectorGUI(level + 1);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Menus:");
            if (newNode == null)
            {
                if (GUILayout.Button("Add Screen", GUILayout.ExpandWidth(false)))
                {
                    // Add a screen
                    newNode = new ScreenMenuTreeNode();
                    newNode.expanded = true;
                    newNode.info = new GUIScreen();
                    newNode.info.name = "ENTER Screen Name";
                    newNode.children = new List<ElementMenuTreeNode>();
                }
            }
            //if (newNode == null && CheckIsValid(this))
            //{
            //    if(GUILayout.Button("Save Changes!", GUILayout.ExpandWidth(false)))
            //    {
            //        // Save changes into myObject
            //        Save();
            //    }
            //}
            GUILayout.EndHorizontal();
            if(newNode != null)
                ShowAddNodeGUI();
            GUI.color = topColor;
        }
    }

    public void ShowAddNodeGUI()
    {
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Add GUIScene Below to SceneInfo");
        if (GUILayout.Button("CANCEL"))
        {
            newNode = null;
            return;
        }

        if (newNode.CheckIsValid(newNode))
        {
            if (GUILayout.Button("SAVE"))
            {
                newNode.expanded = false;
                newNode.parent = this;
                children.Add(newNode);
                info.Screens.Add(newNode.info);
                newNode = null;
            }
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (newNode != null)
            newNode.ShowInspectorGUI(-1);
    }

    public bool CheckIsValid(SIIMenuTreeNode node)
    {
        if (node == null) return false;
        if (node.filePath != null && node.filePath.Contains("ENTER")) return false;
        // here, depending on the node type, see if there is enough info to use this node
        return node.valid;
    }

    void Save()
    {
        if (info != null)
            info.Screens.Clear();

        //info = new ScreenInfo();
        info.Screens = new List<GUIScreen>();
        foreach (ScreenMenuTreeNode node in children)
        {
            info.Screens.Add(node.info);
        }
    }
}


public class ScreenMenuTreeNode : GUIMenuTreeNode
{
    public GUIScreen info;

    public List<ElementMenuTreeNode> children;
    public SIIMenuTreeNode parent = null;

    ElementMenuTreeNode newNode; // used when building up new menus

    public static ScreenMenuTreeNode BuildMenu(GUIScreen info) // pass in the relative path
    {
        ScreenMenuTreeNode returnNode = new ScreenMenuTreeNode();
        returnNode.info = info;
        returnNode.children = new List<ElementMenuTreeNode>();

        foreach (GUIObject obj in returnNode.info.Elements)
        {
            // Containers nest, so deal with it slightly different.
            if (obj.GetType() == typeof(GUIContainer) || obj.GetType().BaseType == typeof(GUIContainer))
            {
                ElementMenuTreeNode newNode = ElementMenuTreeNode.BuildMenu(returnNode, obj as GUIContainer);
                if (newNode != null)
                {
                    newNode.screenParent = returnNode;
                    returnNode.children.Add(newNode);
                }
            }
            else
            {
                ElementMenuTreeNode newNode = new ElementMenuTreeNode();
                newNode.screenParent = returnNode;
                returnNode.children.Add(newNode);
            }
        }

        return returnNode;
    }

	// this method recursively displays the menu tree for the Editor inspector, and can call methods to add and save
    public override void ShowInspectorGUI(int level)
    {
        base.ShowInspectorGUI(level);

        GUILayout.BeginHorizontal();
        GUILayout.Label(indent + "Screen: " + (info.name != null || info.name.Length == 0 ? info.name : "INSERT NAME"));
        bool clicked = GUILayout.Button(xp + "Screen", GUILayout.ExpandWidth(false));
        if (clicked) expanded = !expanded;
        if (parent != null && DeleteButton())
        {
            parent.RemoveChild(this);
            return;
        }
        GUILayout.EndHorizontal();
        if (expanded)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Type:", GUILayout.Width(125));
            info.type = GUILayout.TextField(info.type);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Name:", GUILayout.Width(125));
            info.name = GUILayout.TextField(info.name);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "X:", GUILayout.Width(125));
            info.x = System.Convert.ToInt32(GUILayout.TextField(info.x.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Y:", GUILayout.Width(125));
            info.y = System.Convert.ToInt32(GUILayout.TextField(info.y.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Width:", GUILayout.Width(125));
            info.width = System.Convert.ToInt32(GUILayout.TextField(info.width.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Height:", GUILayout.Width(125));
            info.height = System.Convert.ToInt32(GUILayout.TextField(info.height.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Skin:", GUILayout.Width(125));
            info.skin = GUILayout.TextField(info.skin);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Style:", GUILayout.Width(125));
            info.style = GUILayout.TextField(info.style);
            GUILayout.EndHorizontal();

#if NEEDED			
            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Relative:", GUILayout.Width(125));
            info._relative = GUILayout.Toggle(info._relative, "");
            info.relative = info._relative ? "" : null;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(indent + "Centered:", GUILayout.Width(125));
            info._centered = GUILayout.Toggle(info._centered,  "");
            info.centered = info._centered ? "" : null;
            GUILayout.EndHorizontal();
#endif

            GUILayout.Label("Elements:");
            // Display children
            foreach (ElementMenuTreeNode n in children)
                n.ShowInspectorGUI(level + 1);
        }
    }

    void ShowAddNodeGUI()
    {
    }

    public bool CheckIsValid(ScreenMenuTreeNode node)
    {
        return true;
    }

    public void RemoveChild(ElementMenuTreeNode child)
    {
        info.Remove(child.info);
        children.Remove(child);
    }
}

public class ElementMenuTreeNode : GUIMenuTreeNode
{
    public GUIObject info;

    public List<ElementMenuTreeNode> children;
    public ScreenMenuTreeNode screenParent = null;
    public ElementMenuTreeNode containerParent = null;

    ElementMenuTreeNode newNode; // used when building up new menus

    // Called only for GUIContainer type classes
    public static ElementMenuTreeNode BuildMenu(ScreenMenuTreeNode screenParent, GUIContainer info)
    {
        ElementMenuTreeNode returnNode = new ElementMenuTreeNode();
        returnNode.info = info;
        returnNode.children = new List<ElementMenuTreeNode>();

        GUIContainer container = returnNode.info as GUIContainer;
        foreach (GUIObject obj in container.Elements)
        {
            // Containers nest, so deal with it slightly different.
            if (obj.GetType() == typeof(GUIContainer) || obj.GetType().BaseType == typeof(GUIContainer))
            {
                ElementMenuTreeNode newNode = ElementMenuTreeNode.BuildMenu(screenParent, obj as GUIContainer);
                if (newNode != null)
                {
                    newNode.screenParent = screenParent;
                    newNode.containerParent = returnNode;
                    returnNode.children.Add(newNode);
                }
            }
            else
            {
                ElementMenuTreeNode newNode = new ElementMenuTreeNode();
                newNode.screenParent = screenParent;
                newNode.containerParent = returnNode;
                returnNode.children.Add(newNode);
            }
        }

        return returnNode;
    }

	// this method recursively displays the menu tree for the Editor inspector, and can call methods to add and save
    public override void ShowInspectorGUI(int level)
    {
        base.ShowInspectorGUI(level);

        GUILayout.BeginHorizontal();
        GUILayout.Label(indent + "Element: " + (info.name != null || info.name.Length == 0 ? info.name : "INSERT NAME"));
        bool clicked = GUILayout.Button(xp + info.GetType().ToString(), GUILayout.ExpandWidth(false));
        if (clicked) expanded = !expanded;
        if ((containerParent != null || screenParent != null) && DeleteButton())
        {
            if (containerParent != null)
                containerParent.RemoveChild(this);
            else if (screenParent != null)
                screenParent.RemoveChild(this);
        }
        GUILayout.EndHorizontal();
        if (expanded)
        {
        }
    }

    public bool CheckIsValid(ElementMenuTreeNode node)
    {
        return true;
    }

    public void RemoveChild(ElementMenuTreeNode child)
    {
        (info as GUIContainer).Elements.Remove(child.info);
        children.Remove(child);
    }
}
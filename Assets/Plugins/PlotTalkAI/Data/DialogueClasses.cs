using System;
using System.Collections.Generic;

[Serializable]
public class MyScenes
{
    public List<MyScene> scene;
}

[Serializable]
public class MyScene
{
    public string npc_name;
    public string hero_name;
    public List<DialogueNode> data;
}

[Serializable]
public class DialogueNode
{
    public int id;
    public string info;
    public string type;
    public string mood;
    public GoalAchieved goal_achieved;
    public string line;
    public MetaData meta;
    public List<DialogueLink> to;
}

[Serializable]
public class GoalAchieved
{
    public int item;
    public int info;
}

[Serializable]
public class DialogueLink
{
    public int id;
    public string mood;
    public string line;
    public string info;
}

[Serializable]
public class MetaData
{
    public float x;
    public float y;
}
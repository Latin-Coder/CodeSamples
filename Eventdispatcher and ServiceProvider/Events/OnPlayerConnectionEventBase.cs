using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnPlayerConnectionEventBase 
{
    public string id;
    public string name;
    public bool isBot;

    public  OnPlayerConnectionEventBase(string id, PlayerInfo info)
    {
        this.id = id;
        this.name = info.Name;
        this.isBot = info.IsBot;
    }
}

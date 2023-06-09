using UnityEngine;
using VivoxUnity;
/// <summary>
/// Scriptable object containing the data to connect to the Vivox application
/// </summary>
[CreateAssetMenu(fileName ="Vivox Settings", menuName ="Networking/Vivox Settings")]
public class VivoxSettings : ScriptableObject
{
    [Header("Connection Settings")]
    public string Domain;
    public string TokenIssuer;
    public string TokenKey;
    public string Server;

    [Header("Channel Settings")]
    public string ChannelName = "Default";
    public ChannelType ChannelType = ChannelType.NonPositional;


}

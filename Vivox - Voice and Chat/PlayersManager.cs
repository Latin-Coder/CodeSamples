using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayersManager : NetworkBehaviour
{
    public readonly SyncDictionary<string, PlayerInfo> ConnectedPlayers = new SyncDictionary<string, PlayerInfo>();

    public Dictionary<string, OnlinePlayer> ConnectedPlayersExtended = new Dictionary<string, OnlinePlayer>();

    public List<OnlinePlayer> ConnectedPlayersValues => ConnectedPlayersExtended.Values.ToList();

    private void Awake()
    {
        ConnectedPlayers.Callback += ConnectedPlayers_Callback;
    }

    public struct CallbackInfo
    {
        public SyncIDictionary<string, PlayerInfo>.Operation op;
        public string key;
        public PlayerInfo item;

        public bool Equals(CallbackInfo info)
        {
            return op == info.op && key == info.key && item.Equals(info.item);
        }
    }

    private CallbackInfo lastCallback;

    private void ConnectedPlayers_Callback(SyncIDictionary<string, PlayerInfo>.Operation op, string key, PlayerInfo item)
    {
        CallbackInfo callbackInfo = new CallbackInfo { op = op, key = key, item = item };

        if (callbackInfo.Equals(lastCallback)) return;

        lastCallback = callbackInfo;

        switch (op)
        {
            case SyncIDictionary<string, PlayerInfo>.Operation.OP_ADD:
                ServiceManager.EventDispatcher.TriggerEvent(new OnPlayerConnectedEvent(key, item));
                break;
            case SyncIDictionary<string, PlayerInfo>.Operation.OP_REMOVE:
                ServiceManager.EventDispatcher.TriggerEvent(new OnPlayerDisconnectedEvent(key, item));
                break;
        }
    }

    public bool IsPlayerConnected(string playerId)
    {
        return ConnectedPlayers.ContainsKey(playerId);
    }

    public int NumPlayersConnected()
    {
        return ConnectedPlayers.Count;
    }

    public OnlinePlayer GetPlayer(string playerId)
    {
        ConnectedPlayersExtended.TryGetValue(playerId, out OnlinePlayer player);
        return player;
    }

    public string GetPlayerName(string playerId)
    {
        ConnectedPlayers.TryGetValue(playerId, out PlayerInfo info);
        return info.Name;
    }

    public void AddPlayer(string playerId, OnlinePlayer player, bool isBot = false)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        if (isServer)
        {
            PlayerInfo info = new PlayerInfo { Name = player.PlayerName, IsBot = isBot };
            ConnectedPlayers.TryAdd(playerId, info);
        }

        ConnectedPlayersExtended.TryAdd(playerId, player);
    }

    public void RemovePlayer(string playerId, bool disconnect = false)
    {
        try
        {
            if (disconnect)
            {
                ConnectedPlayers.Remove(playerId);
            }

            ConnectedPlayersExtended.Remove(playerId);
        }
        catch (Exception e)
        {
            Debug.LogError("ConnectedPlayers dictionary does not contain key: " + playerId + "\n" + e.Message);
        }
    }
}

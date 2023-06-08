using static PlayersManager;

public class OnPlayerConnectedEvent : OnPlayerConnectionEventBase
{
    public OnPlayerConnectedEvent(string id, PlayerInfo info):base(id,info)
    {
        
    }
}

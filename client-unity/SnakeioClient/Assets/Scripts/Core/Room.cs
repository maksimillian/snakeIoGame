using System;
using Newtonsoft.Json;

[Serializable]
public class Room
{
    [JsonProperty("id")]
    public int id;
    
    [JsonProperty("name")]
    public string name;
    
    [JsonProperty("friendCode")]
    public string friendCode;
    
    [JsonProperty("players")]
    public int players;
    
    [JsonProperty("currentPlayers")]
    public int currentPlayers;
    
    [JsonProperty("maxPlayers")]
    public int maxPlayers;
    
    [JsonProperty("botCount")]
    public int botCount;
} 
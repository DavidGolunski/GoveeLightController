using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum LeagueEventTypes {
    NO_EVENT,
    GAME_LOST,
    GAME_WON,
    GAME_STARTED,

    BARON_KILLED,
    HERALD_KILLED,
    VOID_GRUBS_KILLED,
    AIR_DRAGON_KILLED,
    EARTH_DRAGON_KILLED,
    FIRE_DRAGON_KILLED,
    WATER_DRAGON_KILLED,
    HEXTECH_DRAGON_KILLED,
    CHEMTECH_DRAGON_KILLED,
    ELDER_DRAGON_KILLED,

    HAS_REVIVED,
    HAS_DIED,
    HAS_KILLED,
    HAS_PENTAKILLED,
    HAS_ASSISTED,
    HAS_KILLED_TURRET,
    HAS_ASSISTED_TURRET
}

public enum RuneTreeTypes {
    PRECISION = 8000,
    DOMINATION = 8100,
    SORCERY = 8200,
    INSPIRATION = 8300,
    RESOLVE = 8400
}

public class LeagueAPI {
    private double minSuccessfulUpdateDelay;
    private double minUnsuccessfulUpdateDelay;

    private long latestEventId;
    private DateTime lastUpdate;
    private bool lastUpdateSuccessful;
    private LeagueEventTypes _event;

    private string activePlayerName;
    private RuneTreeTypes? primaryRuneTree;
    private bool playerWasDead;


    private static readonly HttpClient client;
    static LeagueAPI() {
        // Configure HttpClient to bypass SSL certificate validation
        var handler = new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        client = new HttpClient(handler) {
            Timeout = TimeSpan.FromMilliseconds(300)
        };
    }


    public LeagueAPI(double minSuccessfulUpdateDelay = 200, double minUnsuccessfulUpdateDelay = 1000) {
        this.minSuccessfulUpdateDelay = minSuccessfulUpdateDelay;
        this.minUnsuccessfulUpdateDelay = minUnsuccessfulUpdateDelay;

        latestEventId = -1;
        Reset();
        lastUpdateSuccessful = true;
    }

    public void Reset() {
        lastUpdate = DateTime.Now;
        lastUpdateSuccessful = false;
        _event = LeagueEventTypes.NO_EVENT;

        activePlayerName = null;
        primaryRuneTree = null;
        playerWasDead = false;
    }

    private bool RetrieveData() {
        DateTime now = DateTime.Now;
        double timeDiff = (now - lastUpdate).TotalMilliseconds;

        if(!lastUpdateSuccessful && timeDiff < minUnsuccessfulUpdateDelay)
            return false;
        if(lastUpdateSuccessful && timeDiff < minSuccessfulUpdateDelay)
            return false;

        try {
            if(activePlayerName == null) {
                var responsePlayerData = client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/activeplayer").Result;

                dynamic playerData = JsonConvert.DeserializeObject(responsePlayerData);

                activePlayerName = playerData.riotIdGameName;
                primaryRuneTree = (RuneTreeTypes) playerData.fullRunes.primaryRuneTree.id;
                latestEventId = -1;
            }

            if(playerWasDead) {
                var responsePlayerList = client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/playerlist").Result;

                var playerList = JsonConvert.DeserializeObject<List<dynamic>>(responsePlayerList);
                var activePlayer = playerList.FirstOrDefault(player => player.riotIdGameName == activePlayerName);

                if(activePlayer == null) {
                    Reset();
                    return false;
                }

                if(activePlayer.isDead == false) {
                    playerWasDead = false;
                    _event = LeagueEventTypes.HAS_REVIVED;
                    lastUpdateSuccessful = true;
                    lastUpdate = now;
                    return true;
                }
            }

            var responseEventData = client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/eventdata").Result;
            var eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseEventData);

            // Parse the "Events" JArray into a strongly-typed list
            var events = ((JArray) eventData["Events"])
                .Select(eventObj => eventObj.ToObject<Dictionary<string, object>>())
                .ToList();
            // Filter events based on EventID
            var newEventData = events.FindAll(eventObj => (long) eventObj["EventID"] > latestEventId);

            if(newEventData.Count > 0) {
                latestEventId = (long) newEventData[newEventData.Count - 1]["EventID"];
            }

            ProcessEvents(newEventData);
            lastUpdate = now;
            lastUpdateSuccessful = true;
            return true;        
        }
        catch(Exception e) {
            Reset();
            return false;
        }
    }

    private void ProcessEvents(List<Dictionary<string, object>> eventsJson) {

        // check for LeagueEventTypes.GAME_WON and LeagueEventTypes.GAME_LOST
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "GameEnd") {
                continue;
            }

            if(eventObj["Result"].ToString() == "Win") {
                _event = LeagueEventTypes.GAME_WON;
            }
            else {
                _event = LeagueEventTypes.GAME_LOST;
            }
            return;
        }

        // Check for LeagueEventTypes.GAME_STARTED
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() == "GameStart") {
                _event = LeagueEventTypes.GAME_STARTED;
                return;
            }
        }

        // check for LeagueEventTypes.BARON_KILLED
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() == "BaronKill") {
                _event = LeagueEventTypes.BARON_KILLED;
                return;
            }
        }

        // check for LeagueEventTypes.HERALD_KILLED
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() == "HeraldKill") {
                _event = LeagueEventTypes.HERALD_KILLED;
                return;
            }
        }

        // Check for LeagueEventTypes.VOID_GRUBS_KILLED  
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "HordeKill") {
                continue;
            }

            if(eventObj["KillerName"].ToString() == activePlayerName) {
                _event = LeagueEventTypes.VOID_GRUBS_KILLED;
                return;
            }

            var assistList = eventObj["Assisters"] as List<object>;
            if(assistList != null && assistList.Contains(activePlayerName)) {
                _event = LeagueEventTypes.VOID_GRUBS_KILLED;
                return;
            }
        }

        // Check for LeagueEventTypes.[DragonType]_DRAGON_KILL
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "DragonKill") {
                continue;
            }
            var dragonType = eventObj["DragonType"].ToString();
            switch(dragonType) {
                case "Air":
                    _event = LeagueEventTypes.AIR_DRAGON_KILLED;
                    return;
                case "Earth":
                    _event = LeagueEventTypes.EARTH_DRAGON_KILLED;
                    return;
                case "Fire":
                    _event = LeagueEventTypes.FIRE_DRAGON_KILLED;
                    return;
                case "Water":
                    _event = LeagueEventTypes.WATER_DRAGON_KILLED;
                    return;
                case "Hextech":
                    _event = LeagueEventTypes.HEXTECH_DRAGON_KILLED;
                    return;
                case "Chemtech":
                    _event = LeagueEventTypes.CHEMTECH_DRAGON_KILLED;
                    return;
                default:
                    _event = LeagueEventTypes.ELDER_DRAGON_KILLED;
                    return;
            }
        }

        // Check for LeagueEventTypes.HAS_PENTAKILLED
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "Multikill") {
                continue;
            }

            if(eventObj["KillerName"].ToString() == activePlayerName) {
                var killstreak = eventObj["KillStreak"].ToString();
                if(killstreak == "5") {
                    _event = LeagueEventTypes.HAS_PENTAKILLED;
                    return;
                }
            }
        }

        // Check for LeagueEventTypes.HAS_DIED, LeagueEventTypes.HAS_KILLED, and LeagueEventTypes.HAS_ASSISTED
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "ChampionKill") {
                continue;
            }

            if(eventObj["VictimName"].ToString() == activePlayerName) {
                _event = LeagueEventTypes.HAS_DIED;
                playerWasDead = true;
                return;
            }

            if(eventObj["KillerName"].ToString() == activePlayerName) {
                _event = LeagueEventTypes.HAS_KILLED;
                return;
            }

            var assistList = eventObj["Assisters"] as List<object>;
            if(assistList == null) {
                continue;
            }
            foreach(var assist in assistList) {
                if(assist.ToString() == activePlayerName) {
                    _event = LeagueEventTypes.HAS_ASSISTED;
                    return;
                }
            }
        }

        // Check for LeagueEventTypes.HAS, LeagueEventTypes.HAS_KILLED_TURRET and LeagueEventTypes.HAS_ASSISTED_TURRET
        foreach(var eventObj in eventsJson) {
            if(eventObj["EventName"].ToString() != "TurretKilled") {
                continue;
            }

            if(eventObj["KillerName"].ToString() == activePlayerName) {
                _event = LeagueEventTypes.HAS_KILLED_TURRET;
                return;
            }

            var assistList = eventObj["Assisters"] as List<object>;
            if(assistList == null) {
                continue;
            }
            foreach(var assist in assistList) {
                if(assist.ToString() == activePlayerName) {
                    _event = LeagueEventTypes.HAS_ASSISTED_TURRET;
                    return;
                }
            }
        }


        _event = LeagueEventTypes.NO_EVENT;
    }

    public bool IsInGame() {
        RetrieveData();
        return activePlayerName != null;
    }

    public bool IsDead() {
        RetrieveData();
        return playerWasDead;
    }

    public RuneTreeTypes? GetPrimaryRuneTree() {
        return primaryRuneTree;
    }

    public LeagueEventTypes GetEvent(bool popEvent = true) {
        RetrieveData();
        if(!popEvent || _event == LeagueEventTypes.NO_EVENT) 
            return _event;

        var eventToReturn = _event;
        _event = LeagueEventTypes.NO_EVENT;
        return eventToReturn;
        
    }
}

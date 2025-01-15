using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum LeagueEventTypes {
    NO_EVENT,
    GAME_LOST,
    GAME_WON,
    GAME_STARTED,
    GAME_STARTED_DOMINATION,
    GAME_STARTED_INSPIRATION,
    GAME_STARTED_RESOLVE,
    GAME_STARTED_SORCERY,
    GAME_STARTED_PRECISION,

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

    /*
     * A Class that tries to fetch data from the League Client.
     * It stores the most recent event in a variable, to be retrieved and interpreted by other classes
     */

    public static readonly LeagueAPI Instance = new LeagueAPI();

    // we do not want to spam the client API. These delays ensure that
    private readonly double minSuccessfulUpdateDelay;
    private readonly double minUnsuccessfulUpdateDelay;

    private long latestEventId;
    private DateTime lastUpdate;
    private bool lastUpdateSuccessful;
    private LeagueEventTypes _event;

    private string activePlayerName;
    public RuneTreeTypes? PrimaryRuneTree { get; private set; }
    public bool IsDead { get; private set; }


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


    private LeagueAPI(double minSuccessfulUpdateDelay = 200, double minUnsuccessfulUpdateDelay = 1000) {
        this.minSuccessfulUpdateDelay = minSuccessfulUpdateDelay;
        this.minUnsuccessfulUpdateDelay = minUnsuccessfulUpdateDelay;

        latestEventId = -1;
        Reset();
        lastUpdateSuccessful = true;
    }

    // resets the League API. Getting ready for a new Game
    public void Reset() {
        lastUpdate = DateTime.Now;
        lastUpdateSuccessful = false;
        _event = LeagueEventTypes.NO_EVENT;

        activePlayerName = null;
        PrimaryRuneTree = null;
        IsDead = false;
        Logger.Instance.LogMessage(TracingLevel.DEBUG, "Resetting League API");
    }

    // Retrieve data from the client. Returns true if successfull.
    // Fetched Data is stored in the classes attributes
    private bool RetrieveData() {
        DateTime now = DateTime.Now;
        double timeDiff = (now - lastUpdate).TotalMilliseconds;

        if(!lastUpdateSuccessful && timeDiff < minUnsuccessfulUpdateDelay)
            return false;
        if(lastUpdateSuccessful && timeDiff < minSuccessfulUpdateDelay)
            return false;

        try {
            // if the name is null, it means that a new game has started
            if(activePlayerName == null) {
                var responsePlayerData = client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/activeplayer").Result;

                dynamic playerData = JsonConvert.DeserializeObject(responsePlayerData);

                activePlayerName = playerData.riotIdGameName;
                PrimaryRuneTree = (RuneTreeTypes) playerData.fullRunes.primaryRuneTree.id;

                latestEventId = -1;
            }

            // there is no "player revived" event. This code simulates the event
            if(IsDead) {
                var responsePlayerList = client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/playerlist").Result;

                var playerList = JsonConvert.DeserializeObject<List<dynamic>>(responsePlayerList);
                var activePlayer = playerList.FirstOrDefault(player => player.riotIdGameName == activePlayerName);

                if(activePlayer == null) {
                    Reset();
                    return false;
                }

                if(activePlayer.isDead == false) {
                    IsDead = false;
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
            // Filter events based on EventID. We do not want outdated events to be interpreted
            var newEventData = events.FindAll(eventObj => (long) eventObj["EventID"] > latestEventId);

            if(newEventData.Count > 0) {
                latestEventId = (long) newEventData[newEventData.Count - 1]["EventID"];
                Console.WriteLine("Latest Event ID: " + latestEventId);

                // Debug Code
                foreach(var eventObj in newEventData) {
                    string json = JsonConvert.SerializeObject(eventObj, Formatting.Indented);
                    Console.WriteLine(json);
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, json);
                }
            }

            // process events in seperate function
            ProcessEvents(newEventData);
            lastUpdate = now;
            lastUpdateSuccessful = true;
            return true;        
        }
        catch(Exception e) {
            // this exception is expected if there is no active League game (timeout)
            Console.WriteLine(e.StackTrace);
            Reset();
            return false;
        }
    }
    
    // interprets a list of new events
    // it will pick the most "relevant" of the events to be storent in the "_event" variable
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
            if(eventObj["EventName"].ToString() != "GameStart") {
                continue;
            }
            if(PrimaryRuneTree == RuneTreeTypes.DOMINATION) {
                _event = LeagueEventTypes.GAME_STARTED_DOMINATION;
            }
            else if(PrimaryRuneTree == RuneTreeTypes.INSPIRATION) {
                _event = LeagueEventTypes.GAME_STARTED_INSPIRATION;
            }
            else if(PrimaryRuneTree == RuneTreeTypes.RESOLVE) {
                _event = LeagueEventTypes.GAME_STARTED_RESOLVE;
            }
            else if(PrimaryRuneTree == RuneTreeTypes.SORCERY) {
                _event = LeagueEventTypes.GAME_STARTED_SORCERY;
            }
            else if(PrimaryRuneTree == RuneTreeTypes.PRECISION) {
                _event = LeagueEventTypes.GAME_STARTED_PRECISION;
            }
            else {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No Rune Tree Type was detected");
                Console.WriteLine("No Rune Tree Type was detected");
            }
            return;
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
                IsDead = true;
                return;
            }

            if(eventObj["KillerName"].ToString() == activePlayerName) {
                _event = LeagueEventTypes.HAS_KILLED;
                return;
            }

            List<string> assistList = ((JArray) eventObj["Assisters"]).ToObject<List<string>>();
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
    
    // function to retrieve information if the player is currently in a game
    public bool IsInGame() {
        RetrieveData();
        return activePlayerName != null;
    }

    // function to retrieve information about the latest event.
    // if "popEvent" is true, it will set it to "NO_EVENT" aferwards, ensuring no double usage of the same event
    public LeagueEventTypes GetEvent(bool popEvent = true) {
        RetrieveData();
        if(!popEvent || _event == LeagueEventTypes.NO_EVENT) 
            return _event;

        var eventToReturn = _event;
        _event = LeagueEventTypes.NO_EVENT;
        return eventToReturn;
        
    }
}

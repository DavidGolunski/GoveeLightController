using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;

namespace GoveeLightController {

    public enum CsEventTypes {
        NO_EVENT,
        ENTERRED_MAIN_MENU,

        ROUND_STARTED_T,
        ROUND_STARTED_CT,
        ROUND_WON,
        ROUND_LOST,

        GAME_WON,
        GAME_LOST,

        HAS_DIED,
        HAS_KILLED,
        HAS_ASSISTED,
        HAS_REVIVED
    }

    public enum CsTeam {
        UNAVAILABLE,
        T,
        CT,
        BOTH
    }

    public enum CsActivities {
        UNAVAILABLE,
        MENU,
        PLAYING,
        TEXT_INPUT
    }

    public enum CsRoundPhases {
        LIVE,
        UNAVAILABLE,
        OVER,
        FREEZETIME
    }

    public enum CsMapPhases {
        UNAVAILABLE,
        WARMUP,
        LIVE,
        INTERMISSION,
        GAME_OVER
    }

    public class GameState {

        // provider information
        public ulong ProviderSteamID { get; private set; }
        public ulong TimeStamp { get; private set; }

        

        // player information
        public bool HasPlayerInformation { get; private set; }
        public ulong PlayerSteamID { get; private set; } 
        public string PlayerName { get; private set; }
        public int ObserverSlot { get; private set; }
        public CsTeam PlayerTeam { get; private set; }
        public CsActivities PlayerActivity { get; private set; }

        // this field is actually not sent by the game. It is needed to store the players team, as the "player information" is about the player that is being spectated
        public CsTeam ProviderTeam { get; private set; }
        // this field is also not sent by the game. It is needed for the "if" statements in the effects. Is only updated in the "GetEvent" function
        public bool IsProviderDead { get; private set; }


        // player state information
        public bool HasPlayerStateInformation { get; private set; }
        public int PlayerHealth { get; private set; }
        public int PlayerFlashed { get; private set; }
        public int RoundKills { get; private set; }

        // round information
        public bool HasRoundInformation { get; private set; }
        public CsRoundPhases RoundPhase { get; private set; } //The phase countdown section contains the same information, but also includes additional phases like "warm up" or "bomb"
        public CsTeam WinTeam { get; private set; }

        // map information
        public bool HasMapInformation { get; private set; }
        public CsMapPhases MapPhase { get; private set; }
        public int Round { get; private set; }


        public bool IsObserving {
            get => ProviderSteamID > 0 && PlayerSteamID > 0 && ProviderSteamID != PlayerSteamID;
        }


        // creates a "default" gamestate, in which nothing is set
        public GameState() {
            ProviderSteamID = 0;
            TimeStamp = 0;

            HasPlayerInformation = false;
            PlayerSteamID = 0;
            PlayerName = null;
            ObserverSlot = -1;
            PlayerTeam = CsTeam.UNAVAILABLE;
            PlayerActivity = CsActivities.UNAVAILABLE;

            ProviderTeam = CsTeam.UNAVAILABLE;
            IsProviderDead = true;

            HasPlayerStateInformation = false;
            PlayerHealth = -1;
            PlayerFlashed = -1;
            RoundKills = -1;

            HasRoundInformation = false;
            RoundPhase = CsRoundPhases.UNAVAILABLE;
            WinTeam = CsTeam.UNAVAILABLE;
            
            HasMapInformation = false;
            MapPhase = CsMapPhases.UNAVAILABLE;
            Round = -1;
        }

        // parses data from the jsonPayload into the class automatically. Can not copy data from previous state
        public GameState(JObject jsonPayload) : base() {
            ParseProviderInformation(jsonPayload);
            HasPlayerInformation = ParsePlayerInformation(jsonPayload);
            HasPlayerStateInformation = ParsePlayerStateInformation(jsonPayload);
            HasRoundInformation = ParseRoundInformation(jsonPayload);
            HasMapInformation = ParseMapInformation(jsonPayload);
        }

        // parses data from the JSON Payload, also copying data from previous state if necessary
        public GameState(JObject jsonPayload, GameState previousState) : base() {
            ParseProviderInformation(jsonPayload);
            HasPlayerInformation = ParsePlayerInformation(jsonPayload, previousState);
            HasPlayerStateInformation = ParsePlayerStateInformation(jsonPayload, previousState);
            HasRoundInformation = ParseRoundInformation(jsonPayload, previousState);
            HasMapInformation = ParseMapInformation(jsonPayload, previousState);
        }

        // this is always sent. no need to look for "previous" or "added" things from a previous state
        private bool ParseProviderInformation(JObject jsonPayload) {
            if(jsonPayload == null || jsonPayload["provider"] == null)
                return false;

            JToken provider = jsonPayload["provider"];

            string steamIdString = provider["steamid"]?.ToString();
            string timeStamp = provider["timestamp"]?.ToString();

            if(string.IsNullOrEmpty(steamIdString)|| string.IsNullOrEmpty(timeStamp)) 
                return false;

            ProviderSteamID = Convert.ToUInt64(steamIdString);
            TimeStamp  = Convert.ToUInt64(timeStamp);
          
            return true;
        }

        private bool ParsePlayerInformation(JObject jsonPayload, GameState previousGameState = null) {
            if(jsonPayload == null) return false;



            if(jsonPayload["previously"]?["player"] != null) {
                // if we need the previous gamestate, but it is null, we can not complete the "parsing" correctly
                if(previousGameState == null) return false;

                this.PlayerSteamID = previousGameState.PlayerSteamID;
                this.PlayerName = previousGameState.PlayerName;
                this.ObserverSlot = previousGameState.ObserverSlot;
                this.PlayerTeam = previousGameState.PlayerTeam;
                this.PlayerActivity = previousGameState.PlayerActivity;

                this.ProviderTeam = previousGameState.ProviderTeam;
                this.IsProviderDead = previousGameState.IsProviderDead;

                if(jsonPayload["player"] == null)
                    return true;
            }


            // parse player data from the payload
            if(jsonPayload["player"] == null) return false;

            JToken player = jsonPayload["player"];

            string steamId = player["steamid"]?.ToString();
            if(string.IsNullOrEmpty(steamId)) // we always expect a player steam id. It is necessary to set the provider team correctly
                return false;

            PlayerSteamID = Convert.ToUInt64(steamId);

            string name = player["name"]?.ToString();
            if(!string.IsNullOrEmpty(name))
                PlayerName = name;

            string observerSlotString = player["observer_slot"]?.ToString();
            if(!string.IsNullOrEmpty(observerSlotString))
                ObserverSlot = Convert.ToInt32(observerSlotString);

            string playerTeamString = player["team"]?.ToString();
            if("T".Equals(playerTeamString)) {
                PlayerTeam = CsTeam.T;
                if(PlayerSteamID == ProviderSteamID)
                    ProviderTeam = CsTeam.T;
            }
            else if("CT".Equals(playerTeamString)) {
                PlayerTeam = CsTeam.CT;
                if(PlayerSteamID == ProviderSteamID)
                    ProviderTeam = CsTeam.CT;
            }

            string playerActivity = player["activity"]?.ToString();
            if("playing".Equals(playerActivity)) {
                PlayerActivity = CsActivities.PLAYING;
            }
            else if("menu".Equals(playerActivity)) {
                PlayerActivity = CsActivities.MENU;
            }
            else if("textinput".Equals(playerActivity)) {
                PlayerActivity = CsActivities.TEXT_INPUT;
            }
            else if(!string.IsNullOrEmpty(playerActivity)) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Unknown player activity: " + playerActivity);
            }

            return true;
        }

        private bool ParsePlayerStateInformation(JObject jsonPayload, GameState previousGameState = null) {
            if(jsonPayload == null) return false;


            if(jsonPayload["previously"]?["state"] != null) {
                if(previousGameState == null) return false;

                this.PlayerHealth = previousGameState.PlayerHealth;
                this.PlayerFlashed = previousGameState.PlayerFlashed;
                this.RoundKills = previousGameState.RoundKills;

                if(jsonPayload["player"]?["state"] == null)
                    return true;
            }


            // parse player data from the payload
            if(jsonPayload["player"]?["state"] == null)  return false;

            JToken state = jsonPayload["player"]["state"];

            string healthString = state["health"]?.ToString();
            if(!string.IsNullOrEmpty(healthString)) PlayerHealth = Convert.ToInt32(healthString);

            string flashedString = state["flashed"]?.ToString();
            if(!string.IsNullOrEmpty(flashedString)) PlayerFlashed = Convert.ToInt32(flashedString);

            string roundKills = state["round_kills"]?.ToString();
            if(!string.IsNullOrEmpty(roundKills)) RoundKills = Convert.ToInt32(roundKills);


            return true;
        }

        private bool ParseRoundInformation(JObject jsonPayload, GameState previousGameState = null) {
            if(jsonPayload == null) return false;

            if(jsonPayload["previously"]?["round"] != null) {
                if(previousGameState == null) return false;

                this.Round = previousGameState.Round;
                this.WinTeam = previousGameState.WinTeam;

                if(jsonPayload["round"] == null)
                    return true;
            }

            if(jsonPayload["round"] == null) return false;

            JToken round = jsonPayload["round"];

            string phaseString = round["phase"]?.ToString();
            if(phaseString.Equals("live")) {
                RoundPhase = CsRoundPhases.LIVE;
            }
            else if(phaseString.Equals("freezetime")) {
                RoundPhase = CsRoundPhases.FREEZETIME;
            }
            else if(phaseString.Equals("over")) {
                RoundPhase = CsRoundPhases.OVER;
            }

            string winTeamString = round["win_team"]?.ToString();
            if(winTeamString == null)
                return true;
            if(winTeamString.Equals("T")) {
                WinTeam = CsTeam.T;
            }
            else if(winTeamString.Equals("CT")) {
                WinTeam = CsTeam.CT;
            }
            else {
                WinTeam = CsTeam.BOTH;
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Win Team: " + winTeamString);
            }

            return true;
        }

        private bool ParseMapInformation(JObject jsonPayload, GameState previousGameState = null) {
            if(jsonPayload == null) return false;


            if(jsonPayload["previously"]?["map"] != null) {
                if(previousGameState == null)  return false;

                this.MapPhase = previousGameState.MapPhase;
                this.Round = previousGameState.Round;

                if(jsonPayload["map"] == null)
                    return true;
            }


            if(jsonPayload["map"] == null) return false;

            JToken map = jsonPayload["map"];
            string mapPhaseString = map["phase"]?.ToString();

            if(mapPhaseString.Equals("live")) {
                MapPhase = CsMapPhases.LIVE;
            }
            else if(mapPhaseString.Equals("warmup")) {
                MapPhase = CsMapPhases.WARMUP;
            }
            else if(mapPhaseString.Equals("intermission")) {
                MapPhase = CsMapPhases.INTERMISSION;
            }
            else if(mapPhaseString.Equals("gameover")) {
                MapPhase = CsMapPhases.GAME_OVER;
            }

            string roundString = map["round"]?.ToString();
            if(!string.IsNullOrEmpty(roundString)) Round = Convert.ToInt32(roundString);


            return true;
        }

        // returns the "most important" event that happened between this GameState and the previousGameState
        public CsEventTypes GetEvent(GameState previousGameState) {
            // special case: the previous GameState is null. This means that the plugin was turned on mid game
            if(previousGameState == null) {
                if(HasPlayerInformation && !HasRoundInformation && !HasMapInformation && PlayerActivity == CsActivities.MENU)
                    return CsEventTypes.ENTERRED_MAIN_MENU;
                
                if(HasPlayerInformation && !IsObserving) {
                    if(ProviderTeam == CsTeam.T)
                        return CsEventTypes.ROUND_STARTED_T;
                    if(ProviderTeam == CsTeam.CT)
                        return CsEventTypes.ROUND_STARTED_CT;
                }
                return CsEventTypes.NO_EVENT;
            }




            if(this == previousGameState) return CsEventTypes.NO_EVENT;


            // game ended events
            if(HasMapInformation && previousGameState.HasMapInformation
                && HasPlayerInformation && HasRoundInformation
                && MapPhase == CsMapPhases.GAME_OVER && previousGameState.MapPhase == CsMapPhases.LIVE) {

                
                if(WinTeam == CsTeam.T && ProviderTeam == CsTeam.T) {
                    return CsEventTypes.GAME_WON;
                }
                else if(WinTeam == CsTeam.CT && ProviderTeam == CsTeam.CT) {
                    return CsEventTypes.GAME_WON;
                }
                else if(WinTeam == CsTeam.T && ProviderTeam == CsTeam.CT) {
                    return CsEventTypes.GAME_LOST;
                }
                else if(WinTeam == CsTeam.CT && ProviderTeam == CsTeam.T) {
                    return CsEventTypes.GAME_LOST;
                }
                else {
                    Console.WriteLine("The game has concluded, but we were unable to determine who has won!\n" + this.ToString());
                    Logger.Instance.LogMessage(TracingLevel.WARN, "The game has concluded, but we were unable to determine who has won!\n" + this.ToString());
                    return CsEventTypes.NO_EVENT;
                }
            }
            /*
            else {
                string message = "Game Ended Event failed because: ";
                if(!HasMapInformation)
                    message += "[HasMapInformation was False]";
                if(!previousGameState.HasMapInformation)
                    message += "[previousGameState.HasMapInformation was False]";
                if(!HasPlayerInformation)
                    message += "[HasPlayerInformation was False]";
                if(!HasRoundInformation)
                    message += "[HasRoundInformation was False]";
                if(MapPhase != CsMapPhases.GAME_OVER)
                    message += "[MapPhase was not GAME_OVER (" + MapPhase + ")]";
                if(previousGameState.MapPhase != CsMapPhases.LIVE)
                    message += "[previousGameState.MapPhase was not LIVE (" + previousGameState.MapPhase + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/

            // round ended events
            if(HasRoundInformation && previousGameState.HasRoundInformation
                && HasPlayerInformation && HasMapInformation
                && RoundPhase == CsRoundPhases.OVER && previousGameState.RoundPhase != CsRoundPhases.OVER) {
                if(ProviderTeam == WinTeam) {
                    return CsEventTypes.ROUND_WON;
                }
                else {
                    return CsEventTypes.ROUND_LOST;
                }
            }
            /*
            else {
                string message = "Round Ended Event failed because: ";
                if(!HasRoundInformation)
                    message += "[HasRoundInformation was False]";
                if(!previousGameState.HasRoundInformation)
                    message += "[previousGameState.HasRoundInformation was False]";
                if(!HasPlayerInformation)
                    message += "[HasPlayerInformation was False]";
                if(!HasMapInformation)
                    message += "[HasMapInformation was False]";
                if(RoundPhase != CsRoundPhases.OVER)
                    message += "[RoundPhase was not OVER (" + RoundPhase + ")]";
                if(previousGameState.RoundPhase == CsRoundPhases.OVER)
                    message += "[previousGameState.RoundPhase was OVER (" + previousGameState.RoundPhase + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/

            // round started events
            if(HasRoundInformation && previousGameState.HasRoundInformation
                && HasPlayerInformation
                && RoundPhase == CsRoundPhases.FREEZETIME && previousGameState.RoundPhase != CsRoundPhases.FREEZETIME) {
                if(PlayerTeam == CsTeam.T) {
                    IsProviderDead = false;
                    return CsEventTypes.ROUND_STARTED_T;
                }
                else {
                    IsProviderDead = false;
                    return CsEventTypes.ROUND_STARTED_CT;
                }
            }
            /*
            else {
                string message = "Round Started Event failed because: ";
                if(!HasRoundInformation)
                    message += "[HasRoundInformation was False]";
                if(!previousGameState.HasRoundInformation)
                    message += "[previousGameState.HasRoundInformation was False]";
                if(!HasPlayerInformation)
                    message += "[HasPlayerInformation was False]";
                if(RoundPhase != CsRoundPhases.FREEZETIME)
                    message += "[RoundPhase was not FREEZETIME (" + RoundPhase + ")]";
                if(previousGameState.RoundPhase == CsRoundPhases.FREEZETIME)
                    message += "[previousGameState.RoundPhase was FREEZETIME (" + previousGameState.RoundPhase + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/

            // team select events
            if(!IsObserving && !previousGameState.IsObserving
                && HasPlayerInformation
                && ProviderTeam != previousGameState.ProviderTeam) {

                if(ProviderTeam == CsTeam.T) {
                    IsProviderDead = true;
                    return CsEventTypes.ROUND_STARTED_T;
                }
                if(ProviderTeam == CsTeam.CT) {
                    IsProviderDead = true;
                    return CsEventTypes.ROUND_STARTED_CT;
                }
                    
            }/*
            else {
                string message = "Team Select Event failed because: ";
                if(IsObserving)
                    message += "[IsObserving was True]";
                if(previousGameState.IsObserving)
                    message += "[previousGameState.IsObserving was True]";
                if(!HasPlayerInformation)
                    message += "[HasPlayerInformation was False]";
                if(ProviderTeam == previousGameState.ProviderTeam)
                    message += "[ProviderTeam was not different from previousGameState.ProviderTeam (ProviderTeam: " + ProviderTeam + ", previousGameState.ProviderTeam: " + previousGameState.ProviderTeam + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/



            // kill events
            if(!IsObserving && HasPlayerStateInformation && previousGameState.HasPlayerInformation && RoundKills > previousGameState.RoundKills)
                return CsEventTypes.HAS_KILLED;
            /*
            else {
                string message = "Player Kill Event failed because: ";
                if(IsObserving)
                    message += "[IsObserving was True]";
                if(!HasPlayerStateInformation)
                    message += "[HasPlayerStateInformation was False]";
                if(!previousGameState.HasPlayerInformation)
                    message += "[previousGameState.HasPlayerInformation was False]";
                if(RoundKills <= previousGameState.RoundKills)
                    message += "[RoundKills was not greater than previousGameState.RoundKills (RoundKills: " + RoundKills + ", previousGameState.RoundKills: " + previousGameState.RoundKills + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/

            // death events
            if(!IsObserving && PlayerHealth == 0 && !previousGameState.IsObserving && previousGameState.PlayerHealth > 0) {
                IsProviderDead = true;
                return CsEventTypes.HAS_DIED;
            }
            /*
            else {
                string message = "Player Death Event failed because: ";
                if(IsObserving)
                    message += "[IsObserving was True]";
                if(PlayerHealth != 0)
                    message += "[PlayerHealth was not 0 (" + PlayerHealth + ")]";
                if(previousGameState.IsObserving)
                    message += "[previousGameState.IsObserving was True]";
                if(previousGameState.PlayerHealth <= 0)
                    message += "[previousGameState.PlayerHealth was not greater than 0 (" + previousGameState.PlayerHealth + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/


            // revive events
            if(IsProviderDead && !IsObserving && PlayerHealth > 0) {
                IsProviderDead = false;
                return CsEventTypes.HAS_REVIVED;
            }
            /*
            else {
                string message = "Provider Revive Event failed because: ";
                if(!IsProviderDead)
                    message += "[IsProviderDead was False]";
                if(IsObserving)
                    message += "[IsObserving was True]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/


            // main menu switch events
            if(HasPlayerInformation && !HasPlayerStateInformation && !HasMapInformation && !HasRoundInformation && PlayerActivity == CsActivities.MENU)
                return CsEventTypes.ENTERRED_MAIN_MENU;
            /*
            else {
                string message = "Player Menu Activity Event failed because: ";
                if(!HasPlayerInformation)
                    message += "[HasPlayerInformation was False]";
                if(HasPlayerStateInformation)
                    message += "[HasPlayerStateInformation was True]";
                if(HasMapInformation)
                    message += "[HasMapInformation was True]";
                if(HasRoundInformation)
                    message += "[HasRoundInformation was True]";
                if(PlayerActivity != CsActivities.MENU)
                    message += "[PlayerActivity was not MENU (" + PlayerActivity + ")]";
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
            }*/

            return CsEventTypes.NO_EVENT;
        }

        public override bool Equals(object obj) {
            if(obj.GetType() != this.GetType()) return false;
            GameState otherGameState = obj as GameState;

            return this.ProviderSteamID == otherGameState.ProviderSteamID
                && this.HasPlayerInformation == otherGameState.HasPlayerInformation
                && this.PlayerSteamID == otherGameState.PlayerSteamID
                && this.PlayerName == otherGameState.PlayerName
                && this.ObserverSlot == otherGameState.ObserverSlot
                && this.PlayerTeam == otherGameState.PlayerTeam
                && this.PlayerActivity == otherGameState.PlayerActivity
                && this.ProviderTeam == otherGameState.ProviderTeam
                && this.IsProviderDead == otherGameState.IsProviderDead
                && this.HasPlayerStateInformation == otherGameState.HasPlayerStateInformation
                && this.PlayerHealth == otherGameState.PlayerHealth
                && this.PlayerFlashed == otherGameState.PlayerFlashed
                && this.RoundKills == otherGameState.RoundKills
                && this.HasRoundInformation == otherGameState.HasRoundInformation
                && this.RoundPhase == otherGameState.RoundPhase
                && this.WinTeam == otherGameState.WinTeam
                && this.HasMapInformation == otherGameState.HasMapInformation
                && this.MapPhase == otherGameState.MapPhase
                && this.Round == otherGameState.Round;
        }

        public override int GetHashCode() {
            unchecked // Allow overflow, ignore arithmetic overflow/underflow
            {
                int hash = 17;

                hash = hash * 23 + (ProviderSteamID.GetHashCode());
                hash = hash * 23 + HasPlayerInformation.GetHashCode();
                hash = hash * 23 + (PlayerSteamID.GetHashCode());
                hash = hash * 23 + (PlayerName?.GetHashCode() ?? 0);
                hash = hash * 23 + ObserverSlot.GetHashCode();
                hash = hash * 23 + (PlayerTeam.GetHashCode());
                hash = hash * 23 + (PlayerActivity.GetHashCode());
                hash = hash * 23 + (ProviderTeam.GetHashCode());
                hash = hash * 23 + IsProviderDead.GetHashCode();
                hash = hash * 23 + HasPlayerStateInformation.GetHashCode();
                hash = hash * 23 + PlayerHealth.GetHashCode();
                hash = hash * 23 + PlayerFlashed.GetHashCode();
                hash = hash * 23 + RoundKills.GetHashCode();
                hash = hash * 23 + HasRoundInformation.GetHashCode();
                hash = hash * 23 + (RoundPhase.GetHashCode());
                hash = hash * 23 + (WinTeam.GetHashCode());
                hash = hash * 23 + HasMapInformation.GetHashCode();
                hash = hash * 23 + (MapPhase.GetHashCode());
                hash = hash * 23 + Round.GetHashCode();

                return hash;
            }
        }


        public override string ToString() {
            string output = "GameState";
            output += "\nProviderInformation: Always";
            output += "\n\tProviderSteamID: " + ProviderSteamID;
            output += "\n\tTimesStamp: " + TimeStamp;
            output += "\n\tProviderTeam: " + ProviderTeam;
            output += "\n\tIsProviderDead: " + IsProviderDead; 

            output += "\nHasPlayerInformation: " + HasPlayerInformation;
            if(HasPlayerInformation) {
                output += "\n\tPlayerSteamID: " + PlayerSteamID;
                output += "\n\tPlayerName: " + PlayerName;
                output += "\n\tObserverSlot: " + ObserverSlot;
                output += "\n\tPlayerTeam: " + PlayerTeam.ToString();
                output += "\n\tPlayerActivity: " + PlayerActivity.ToString();
            }

            output += "\nHasPlayerStateInformation: " + HasPlayerStateInformation;
            if(HasPlayerStateInformation) {
                output += "\n\tPlayerHealth: " + PlayerHealth;
                output += "\n\tPlayerFlashed: " + PlayerFlashed;
                output += "\n\tRoundKills: " + RoundKills;
            }

            output += "\nHasRoundInformation: " + HasRoundInformation;
            if(HasRoundInformation) {
                output += "\n\tRoundPhase: " + RoundPhase.ToString();
                output += "\n\tWinTeam: " + WinTeam.ToString();
            }

            output += "\nHasMapInformation: " + HasMapInformation;
            if(HasMapInformation) {
                output += "\n\tMapPhase: " + MapPhase.ToString();
                output += "\n\tRound: " + Round;
            }
            
            return output; 
        }

    }
}

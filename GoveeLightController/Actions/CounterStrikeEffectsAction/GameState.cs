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
        ROUND_WON_BOMB_EXPLODED,
        ROUND_WON_BOMB_DEFUSED,
        ROUND_LOST,
        ROUND_LOST_BOMB_EXPLODED,
        ROUND_LOST_BOMB_DEFUSED,

        GAME_WON,
        GAME_LOST,
        GAME_TIED,

        HAS_DIED,
        HAS_KILLED,
        HAS_KILLED_HEADSHOT,
        HAS_ASSISTED,
        HAS_REVIVED,

        HAS_BOUGHT_EQUIPMENT,


        STARTED_FLASHED,
        STARTED_SMOKED,
        STARTED_BURNING,

        STOPPED_SMOKED,
        STOPPED_BURNING,

        SPECTATOR_FREECAM,
        SPECTATOR_WATCHING_T,
        SPECTATOR_WATCHING_CT,
        SPECTATOR_BOMB_PLANTED,
        SPECTATOR_ROUND_WON_T,
        SPECTATOR_ROUND_WON_T_BOMB_EXPLODED,
        SPECTATOR_ROUND_WON_CT,
        SPECTATOR_ROUND_WON_CT_BOMB_DEFUSED,
        SPECTATOR_GAME_WON_T,
        SPECTATOR_GAME_WON_CT,
        SPECTATOR_GAME_TIED
    }

    public enum CsTeam {
        UNAVAILABLE,
        T,
        CT
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

    public enum CsBombStatus {
        UNAVAILABLE,
        PLANTED,
        EXPLODED,
        DEFUSED
    }

    public class GameState {

        // provider information
        private ulong ProviderSteamID { get; set; } = 0;
        private ulong TimeStamp { get; set; } = 0;

        // player information
        private bool HasPlayerInformation { get; set; } = false;
        private ulong PlayerSteamID { get; set; } = 0;
        private CsTeam PlayerTeam { get; set; } = CsTeam.UNAVAILABLE;

        // player state information
        private bool HasPlayerStateInformation { get; set; } = false;
        private int PlayerHealth { get; set; } = -1;
        private int PlayerFlashed { get; set; } = -1;
        private int PlayerSmoked { get; set; } = -1;
        private int PlayerBurning { get; set; } = -1;
        private int RoundKills { get; set; } = -1;
        private int RoundKillsHS { get; set; } = -1;
        private int EquipValue { get; set; } = -1;
        private int RoundTotalDamage { get; set; } = -1; // this is only sent if the user is a spectator to a game (not a dead player spectating someone else). If this is set, then the provider is definetely spectating

        // round information
        private bool HasRoundInformation { get; set; } = false;
        private CsRoundPhases RoundPhase { get; set; } = CsRoundPhases.UNAVAILABLE; //The phase countdown section contains the same information, but also includes additional phases like "warm up" or "bomb"
        private CsTeam WinTeam { get; set; } = CsTeam.UNAVAILABLE;
        private CsBombStatus BombStatus { get; set; } = CsBombStatus.UNAVAILABLE;

        // map information
        private bool HasMapInformation { get; set; } = false;
        private CsMapPhases MapPhase { get; set; } = CsMapPhases.UNAVAILABLE;
        private int ScoreTeamT { get; set; } = -1;
        private int ScoreTeamCT { get; set; } = -1;


        // Data not sent directly by CS, but calculated instead
        private bool IsSpectator { get; set; } = false; // set if the provider is a spectator to the game (not only dead and watching teammates)


        private bool IsProviderPlaying {
            get => ProviderSteamID > 0 && PlayerSteamID > 0 && ProviderSteamID == PlayerSteamID;
        }
        public bool IsProviderPlayingAndAlive {
            get => IsProviderPlaying && HasPlayerStateInformation && PlayerHealth > 0;
        }


        // this field is actually not sent by the game. It is needed to store the players team, as the "player information" is about the player that is being spectated
        private CsTeam ProviderTeam { get; set; } = CsTeam.UNAVAILABLE;


        // parses data from the JSON Payload, also copying data from previous state if necessary
        public GameState(JObject jsonPayload, GameState previousState = null) {
            ParseProviderInformation(jsonPayload);
            HasPlayerInformation = ParsePlayerInformation(jsonPayload);
            HasPlayerStateInformation = ParsePlayerStateInformation(jsonPayload);
            HasRoundInformation = ParseRoundInformation(jsonPayload);
            HasMapInformation = ParseMapInformation(jsonPayload);

            InitCalculatedFields(previousState);
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

        private bool ParsePlayerInformation(JObject jsonPayload) {
            if(jsonPayload == null || jsonPayload["player"] == null)
                return false;


            JToken player = jsonPayload["player"];

            string steamId = player["steamid"]?.ToString();
            if(string.IsNullOrEmpty(steamId)) // we always expect a player steam id. It is necessary to set the provider team correctly
                return false;

            PlayerSteamID = Convert.ToUInt64(steamId);

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

            return true;
        }

        private bool ParsePlayerStateInformation(JObject jsonPayload) {
            if(jsonPayload == null || jsonPayload["player"]?["state"] == null) return false;


            JToken state = jsonPayload["player"]["state"];

            string healthString = state["health"]?.ToString();
            if(!string.IsNullOrEmpty(healthString)) PlayerHealth = Convert.ToInt32(healthString);

            string flashedString = state["flashed"]?.ToString();
            if(!string.IsNullOrEmpty(flashedString)) PlayerFlashed = Convert.ToInt32(flashedString);

            string smokedString = state["smoked"]?.ToString();
            if(!string.IsNullOrEmpty(smokedString)) PlayerSmoked = Convert.ToInt32(smokedString);

            string burningString = state["burning"]?.ToString();
            if(!string.IsNullOrEmpty(burningString)) PlayerBurning = Convert.ToInt32(burningString);

            string roundKills = state["round_kills"]?.ToString();
            if(!string.IsNullOrEmpty(roundKills)) RoundKills = Convert.ToInt32(roundKills);

            string roundHsKills = state["round_killhs"]?.ToString();
            if(!string.IsNullOrEmpty(roundHsKills)) RoundKillsHS = Convert.ToInt32(roundHsKills);

            string equipmentValue = state["equip_value"]?.ToString();
            if(!string.IsNullOrEmpty(equipmentValue)) EquipValue = Convert.ToInt32(equipmentValue);

            string roundTotalDamage = state["round_totaldmg"]?.ToString();
            if(!string.IsNullOrEmpty(roundTotalDamage)) RoundTotalDamage = Convert.ToInt32(roundTotalDamage); // if this field exists, the provider is a spectator to a game as they can see information that players of the game can't see


            return true;
        }

        private bool ParseRoundInformation(JObject jsonPayload) {
            if(jsonPayload == null || jsonPayload["round"] == null) return false;

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
            if(!string.IsNullOrEmpty(winTeamString)) {
                if(winTeamString.Equals("T")) {
                    WinTeam = CsTeam.T;
                }
                else if(winTeamString.Equals("CT")) {
                    WinTeam = CsTeam.CT;
                }
            }

            string bombStatusString = round["bomb"]?.ToString();
            if(!string.IsNullOrEmpty(bombStatusString)) {
                if(bombStatusString.Equals("planted")) {
                    BombStatus = CsBombStatus.PLANTED;
                }
                else if(bombStatusString.Equals("exploded")) {
                    BombStatus = CsBombStatus.EXPLODED;
                }
                else if(bombStatusString.Equals("defused")) {
                    BombStatus = CsBombStatus.DEFUSED;
                }
            }

            return true;
        }

        private bool ParseMapInformation(JObject jsonPayload) {
            if(jsonPayload == null || jsonPayload["map"] == null) return false;


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

            string scoreTeamTString = map["team_t"]?["score"]?.ToString();
            if(!string.IsNullOrEmpty(scoreTeamTString)) ScoreTeamT = Convert.ToInt32(scoreTeamTString);

            string scoreTeamCTString = map["team_ct"]?["score"]?.ToString();
            if(!string.IsNullOrEmpty(scoreTeamCTString)) ScoreTeamCT = Convert.ToInt32(scoreTeamCTString);

            return true;
        }

        private void InitCalculatedFields(GameState previousState = null) {
            // easy case to check if the provider is definetely spectating (not being a dead playing watching someone). No Provider Team is needed
            if(HasPlayerStateInformation && RoundTotalDamage >= 0) {
                IsSpectator = true;
                ProviderTeam = CsTeam.UNAVAILABLE;
                return;
            }

            // easy check to see if the provider is in the main menu. This should reset the Provider Team
            if(HasPlayerInformation && !HasPlayerStateInformation && !HasMapInformation && !HasRoundInformation && ProviderSteamID == PlayerSteamID) {
                IsSpectator = false;
                ProviderTeam = CsTeam.UNAVAILABLE;
                return;
            }

            // check if the player is spectating (also happens if the player is in a pre-game team selection). No Provider team is avaialble
            if(!HasPlayerInformation && !HasPlayerStateInformation && HasMapInformation) {
                IsSpectator = true;
                ProviderTeam = CsTeam.UNAVAILABLE;
                return;
            }

            // check if the Player is actively Playing
            if(IsProviderPlaying && HasMapInformation && HasRoundInformation) {
                IsSpectator = false;
                ProviderTeam = PlayerTeam;
                return;
            }

            // check if the Player is Playing and spectating another player from their own team. This is the case if they don't have access to the RoundTotalDamage Stat
            if(HasPlayerInformation && HasPlayerStateInformation && HasMapInformation && HasRoundInformation && RoundTotalDamage < 0) {
                IsSpectator = false;
                ProviderTeam = PlayerTeam;
                return;
            }

            // from here we have to rely on the previous state to deliver missing information
            if(previousState != null) {
                IsSpectator = previousState.IsSpectator;
                ProviderTeam = previousState.ProviderTeam;
                return;
            }

            // if no previous state is given, assume the provider is a spectator, just to not provide confusing events
            IsSpectator = true;
            ProviderTeam = CsTeam.UNAVAILABLE;
        }


        // returns the "most important" event that happened between this GameState and the previousGameState
        public CsEventTypes GetEvent(GameState previousGameState) {
            if(this.Equals(previousGameState))
                return CsEventTypes.NO_EVENT;

            // always check for main menu first
            // the information sent is always the same and does not need the previousGameState
            if(HasPlayerInformation && !HasPlayerStateInformation
                && !HasMapInformation && !HasRoundInformation
                && ProviderSteamID == PlayerSteamID)
                return CsEventTypes.ENTERRED_MAIN_MENU;


            // From here we can assume the player is in game (or in a pregame teamselection)


            // special case: there is no previous game state. Try to find out what the best approach for initializing the lights is
            if(previousGameState == null) {
                if(IsSpectator) {
                    // if you join the game as a spectator and the round is in progress
                    if(HasMapInformation && HasRoundInformation && MapPhase == CsMapPhases.LIVE && (RoundPhase == CsRoundPhases.LIVE || RoundPhase == CsRoundPhases.FREEZETIME)) {
                        if(!HasPlayerInformation)
                            return CsEventTypes.SPECTATOR_FREECAM;

                        if(PlayerTeam == CsTeam.T)
                            return CsEventTypes.SPECTATOR_WATCHING_T;

                        if(PlayerTeam == CsTeam.CT)
                            return CsEventTypes.SPECTATOR_WATCHING_CT;
                    }

                    // it probably does not make sense to throw an event. As it would probably be heavily delayed anyways.
                    return CsEventTypes.NO_EVENT;
                }

                // if the provider is dead, don't throw an event. Wait until the next round
                if(!IsProviderPlaying)
                    return CsEventTypes.NO_EVENT;

                if(ProviderTeam == CsTeam.T)
                    return CsEventTypes.ROUND_STARTED_T;
                if(ProviderTeam == CsTeam.CT)
                    return CsEventTypes.ROUND_STARTED_CT;

                return CsEventTypes.NO_EVENT;
            }

            // first go through events if the provider is a spectator
            if(IsSpectator) {
                
                // Game Ended Event
                if(HasMapInformation && previousGameState.HasMapInformation
                    && HasRoundInformation && previousGameState.HasRoundInformation
                    && MapPhase == CsMapPhases.GAME_OVER && previousGameState.MapPhase != CsMapPhases.GAME_OVER) {

                    if(ScoreTeamT > ScoreTeamCT)
                        return CsEventTypes.SPECTATOR_GAME_WON_T;
                    if(ScoreTeamCT > ScoreTeamCT)
                        return CsEventTypes.SPECTATOR_GAME_WON_CT;

                    return CsEventTypes.SPECTATOR_GAME_TIED;
                }

                // Round Ended Event
                if(HasMapInformation && previousGameState.HasMapInformation
                    && HasRoundInformation && previousGameState.HasRoundInformation
                    && (MapPhase == CsMapPhases.LIVE || MapPhase == CsMapPhases.INTERMISSION)
                    && RoundPhase == CsRoundPhases.OVER && previousGameState.RoundPhase != CsRoundPhases.OVER) {

                    if(WinTeam == CsTeam.T)
                        return BombStatus == CsBombStatus.EXPLODED ? CsEventTypes.SPECTATOR_ROUND_WON_T_BOMB_EXPLODED : CsEventTypes.SPECTATOR_ROUND_WON_T;

                    if(WinTeam == CsTeam.CT)
                        return BombStatus == CsBombStatus.DEFUSED ? CsEventTypes.SPECTATOR_ROUND_WON_CT_BOMB_DEFUSED : CsEventTypes.SPECTATOR_ROUND_WON_CT;

                    // this should not be possible, it's just here for safety
                    return CsEventTypes.NO_EVENT;
                }


                // Bomb Planted Event
                if(HasRoundInformation && previousGameState.HasRoundInformation && RoundPhase == CsRoundPhases.LIVE
                    && BombStatus == CsBombStatus.PLANTED && previousGameState.BombStatus == CsBombStatus.UNAVAILABLE) 
                    return CsEventTypes.SPECTATOR_BOMB_PLANTED;

                // Switched to Freecam
                if(!HasPlayerInformation && previousGameState.HasPlayerInformation
                    && !HasPlayerStateInformation && previousGameState.HasPlayerStateInformation)
                    return CsEventTypes.SPECTATOR_FREECAM;

                // Switched to Spectate a player
                if(HasPlayerInformation && HasPlayerStateInformation) {

                    // if was freecam before
                    if(!previousGameState.HasPlayerInformation) {
                        if(PlayerTeam == CsTeam.T)
                            return CsEventTypes.SPECTATOR_WATCHING_T;
                        if(PlayerTeam == CsTeam.CT)
                            return CsEventTypes.SPECTATOR_WATCHING_CT;

                        Logger.Instance.LogMessage(TracingLevel.WARN, "The provider switched to specating a player, but the Player did not have a team (" + PlayerTeam.ToString() + ")");
                        return CsEventTypes.NO_EVENT;
                    }

                    // if switched from ct to t
                    if(previousGameState.PlayerTeam == CsTeam.CT && PlayerTeam == CsTeam.T)
                        return CsEventTypes.SPECTATOR_WATCHING_T;
                    // if switched from t to ct
                    if(previousGameState.PlayerTeam == CsTeam.T && PlayerTeam == CsTeam.CT)
                        return CsEventTypes.SPECTATOR_WATCHING_CT;

                    // if provider was not spectating before
                    if(!previousGameState.IsSpectator && PlayerTeam == CsTeam.T)
                        return CsEventTypes.SPECTATOR_WATCHING_T;
                    if(!previousGameState.IsSpectator && PlayerTeam == CsTeam.CT)
                        return CsEventTypes.SPECTATOR_WATCHING_CT;

                    // no switch in teams happend
                    return CsEventTypes.NO_EVENT;
                }

                return CsEventTypes.NO_EVENT;
            }

            // Game Ended Events
            if(HasMapInformation && previousGameState.HasMapInformation
                && HasRoundInformation && previousGameState.HasRoundInformation
                && MapPhase == CsMapPhases.GAME_OVER && previousGameState.MapPhase != CsMapPhases.GAME_OVER) {

                if(ProviderTeam == CsTeam.T && ScoreTeamT > ScoreTeamCT) {
                    return CsEventTypes.GAME_WON;
                }
                else if(ProviderTeam == CsTeam.CT && ScoreTeamCT > ScoreTeamT) {
                    return CsEventTypes.GAME_WON;
                }
                else if(ProviderTeam == CsTeam.CT && ScoreTeamT > ScoreTeamCT) {
                    return CsEventTypes.GAME_LOST;
                }
                else if(ProviderTeam == CsTeam.T && ScoreTeamCT > ScoreTeamT) {
                    return CsEventTypes.GAME_LOST;
                }
                return CsEventTypes.GAME_TIED;
            }

            // Round Ended Events
            if(HasMapInformation && previousGameState.HasMapInformation
                && HasRoundInformation && previousGameState.HasRoundInformation
                && (MapPhase == CsMapPhases.LIVE || MapPhase == CsMapPhases.INTERMISSION)
                && RoundPhase == CsRoundPhases.OVER && previousGameState.RoundPhase != CsRoundPhases.OVER) {

                if(BombStatus == CsBombStatus.EXPLODED)
                    return ProviderTeam == WinTeam ? CsEventTypes.ROUND_WON_BOMB_EXPLODED : CsEventTypes.ROUND_LOST_BOMB_EXPLODED;
                else if(BombStatus == CsBombStatus.DEFUSED)
                    return ProviderTeam == WinTeam ? CsEventTypes.ROUND_WON_BOMB_DEFUSED : CsEventTypes.ROUND_LOST_BOMB_DEFUSED;

                return ProviderTeam == WinTeam ? CsEventTypes.ROUND_WON : CsEventTypes.ROUND_LOST;
            }

            // Round Started Events
            if(HasPlayerInformation && HasMapInformation && HasRoundInformation
                && ((RoundPhase == CsRoundPhases.FREEZETIME && previousGameState.RoundPhase != CsRoundPhases.FREEZETIME) // if round started normaly
                || (ProviderTeam != CsTeam.UNAVAILABLE && ProviderTeam != previousGameState.ProviderTeam)) // if switched within a round or a non competetive gamemode is selected
                ) {

                if(ProviderTeam == CsTeam.T)
                    return CsEventTypes.ROUND_STARTED_T;

                if(ProviderTeam == CsTeam.CT)
                    return CsEventTypes.ROUND_STARTED_CT;

                Logger.Instance.LogMessage(TracingLevel.WARN, "Wanted to send Round Started Event, but ProviderTeam was not a valid Team.\n" + this.ToString());
                return CsEventTypes.NO_EVENT;
            }

            // death events
            if(HasMapInformation && IsProviderPlaying && PlayerHealth == 0 && previousGameState.PlayerHealth > 0 && 
                HasRoundInformation && RoundPhase == CsRoundPhases.LIVE)
                return CsEventTypes.HAS_DIED;

            // revive events
            if(HasMapInformation && IsProviderPlayingAndAlive && !previousGameState.IsProviderPlayingAndAlive)
                return CsEventTypes.HAS_REVIVED;

            // has bought events
            if(HasMapInformation && IsProviderPlayingAndAlive && HasRoundInformation && HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && RoundPhase == CsRoundPhases.FREEZETIME && EquipValue > previousGameState.EquipValue)
                return CsEventTypes.HAS_BOUGHT_EQUIPMENT;


            // from here on out all events should only happen if the provider is alive and the round is live (don't flash green when you make kills after the round has ended)
            if(!IsProviderPlayingAndAlive || RoundPhase != CsRoundPhases.LIVE)
                return CsEventTypes.NO_EVENT; // exit early, since no other events will happen


            // flash events
            if(HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && previousGameState.PlayerFlashed < PlayerFlashed && PlayerFlashed > 0) // player flashed is either 1 or 0
                return CsEventTypes.STARTED_FLASHED;

            // kill headshot event
            if(HasMapInformation && HasPlayerStateInformation && previousGameState.HasPlayerStateInformation && RoundKillsHS > previousGameState.RoundKillsHS)
                return CsEventTypes.HAS_KILLED_HEADSHOT;

            // kill events
            if(HasMapInformation && HasPlayerStateInformation && previousGameState.HasPlayerStateInformation && RoundKills > previousGameState.RoundKills)
                return CsEventTypes.HAS_KILLED;

            // stopped smoke events
            if(HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && previousGameState.PlayerSmoked > PlayerSmoked && previousGameState.PlayerSmoked > 80 && PlayerSmoked <= 80)
                return CsEventTypes.STOPPED_SMOKED;

            // started smoked events
            if(HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && previousGameState.PlayerSmoked < PlayerSmoked && previousGameState.PlayerSmoked <= 80 && PlayerSmoked > 80) // can go up to 255, but with a value around 80 your vision in cs starts to get grey
                return CsEventTypes.STARTED_SMOKED;

            // stopped burning events
            if(HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && previousGameState.PlayerBurning > PlayerBurning && PlayerBurning < 255) // the value of 255 is only slowly going down, even if you are totally out of the fire already
                return CsEventTypes.STOPPED_BURNING;

            // started burning events
            if(HasPlayerStateInformation && previousGameState.HasPlayerStateInformation
                && previousGameState.PlayerBurning < PlayerBurning && PlayerBurning == 255) // 255 is the value when you are standing in the fire
                return CsEventTypes.STARTED_BURNING;

          
            return CsEventTypes.NO_EVENT;
        }




        public override bool Equals(object obj) {
            if(obj == null || obj.GetType() != this.GetType()) return false;
            GameState otherGameState = obj as GameState;

            return this.ProviderSteamID == otherGameState.ProviderSteamID
                && this.HasPlayerInformation == otherGameState.HasPlayerInformation
                && this.PlayerSteamID == otherGameState.PlayerSteamID
                && this.PlayerTeam == otherGameState.PlayerTeam
                && this.HasPlayerStateInformation == otherGameState.HasPlayerStateInformation
                && this.PlayerHealth == otherGameState.PlayerHealth
                && this.PlayerFlashed == otherGameState.PlayerFlashed
                && this.PlayerSmoked == otherGameState.PlayerSmoked
                && this.PlayerBurning == otherGameState.PlayerBurning
                && this.RoundKills == otherGameState.RoundKills
                && this.RoundKillsHS == otherGameState.RoundKillsHS
                && this.EquipValue == otherGameState.EquipValue
                && this.RoundTotalDamage == otherGameState.RoundTotalDamage
                && this.HasRoundInformation == otherGameState.HasRoundInformation
                && this.RoundPhase == otherGameState.RoundPhase
                && this.WinTeam == otherGameState.WinTeam
                && this.BombStatus == otherGameState.BombStatus
                && this.HasMapInformation == otherGameState.HasMapInformation
                && this.MapPhase == otherGameState.MapPhase
                && this.ScoreTeamT == otherGameState.ScoreTeamT
                && this.ScoreTeamCT == otherGameState.ScoreTeamCT
                && this.IsSpectator == otherGameState.IsSpectator
                && this.ProviderTeam == otherGameState.ProviderTeam;
        }

        public override int GetHashCode() {
            unchecked // Allow overflow, ignore arithmetic overflow/underflow
            {
                int hash = 17;

                hash = hash * 23 + ProviderSteamID.GetHashCode();
                hash = hash * 23 + HasPlayerInformation.GetHashCode();
                hash = hash * 23 + PlayerSteamID.GetHashCode();
                hash = hash * 23 + PlayerTeam.GetHashCode();
                hash = hash * 23 + HasPlayerStateInformation.GetHashCode();
                hash = hash * 23 + PlayerHealth.GetHashCode();
                hash = hash * 23 + PlayerFlashed.GetHashCode();
                hash = hash * 23 + PlayerSmoked.GetHashCode();
                hash = hash * 23 + PlayerBurning.GetHashCode();
                hash = hash * 23 + RoundKills.GetHashCode();
                hash = hash * 23 + RoundKillsHS.GetHashCode();
                hash = hash * 23 + EquipValue.GetHashCode();
                hash = hash * 23 + RoundTotalDamage.GetHashCode();
                hash = hash * 23 + HasRoundInformation.GetHashCode();
                hash = hash * 23 + RoundPhase.GetHashCode();
                hash = hash * 23 + WinTeam.GetHashCode();
                hash = hash * 23 + BombStatus.GetHashCode();
                hash = hash * 23 + HasMapInformation.GetHashCode();
                hash = hash * 23 + MapPhase.GetHashCode();
                hash = hash * 23 + ScoreTeamT.GetHashCode();
                hash = hash * 23 + ScoreTeamCT.GetHashCode();
                hash = hash * 23 + IsSpectator.GetHashCode();
                hash = hash * 23 + ProviderTeam.GetHashCode();

                return hash;
            }
        }


        public override string ToString() {
            string output = "GameState";
            output += "\nProviderInformation: Always";
            output += "\n\tProviderSteamID: " + ProviderSteamID;
            output += "\n\tTimesStamp: " + TimeStamp;
            output += "\n\tProviderTeam: " + ProviderTeam;
            output += "\n\tIsSpectator: " + IsSpectator;

            output += "\nHasPlayerInformation: " + HasPlayerInformation;
            if(HasPlayerInformation) {
                output += "\n\tPlayerSteamID: " + PlayerSteamID;
                output += "\n\tPlayerTeam: " + PlayerTeam.ToString();
            }

            output += "\nHasPlayerStateInformation: " + HasPlayerStateInformation;
            if(HasPlayerStateInformation) {
                output += "\n\tPlayerHealth: " + PlayerHealth;
                output += "\n\tPlayerFlashed: " + PlayerFlashed;
                output += "\n\tPlayerSmoked: " + PlayerSmoked;
                output += "\n\tPlayerBurning: " + PlayerBurning;
                output += "\n\tRoundKills: " + RoundKills;
                output += "\n\tRoundKillsHS: " + RoundKillsHS;
                output += "\n\tEquipValue: " + EquipValue;
                output += "\n\tRoundTotalDamage: " + RoundTotalDamage;
            }

            output += "\nHasRoundInformation: " + HasRoundInformation;
            if(HasRoundInformation) {
                output += "\n\tRoundPhase: " + RoundPhase.ToString();
                output += "\n\tWinTeam: " + WinTeam.ToString();
                output += "\n\tBombStatus: " + BombStatus.ToString();
            }

            output += "\nHasMapInformation: " + HasMapInformation;
            if(HasMapInformation) {
                output += "\n\tMapPhase: " + MapPhase.ToString();
                output += "\n\tScoreTeamT: " + ScoreTeamT;
                output += "\n\tScoreTeamCT: " + ScoreTeamCT;
            }
            
            return output; 
        }

    }
}

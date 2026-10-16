using System;
using System.Collections.Generic;

namespace TimingShow
{
    public sealed class UrStats
    {
        private int _count;
        private double _mean;
        private double _s;

        public void Add(double x)
        {
            int n = ++_count;
            double d = x - _mean;
            _mean += d / n;
            _s += d * (x - _mean);
        }

        public void Reset()
        {
            _count = 0;
            _mean = 0.0;
            _s = 0.0;
        }

        public double Mean()
        {
            return _count == 0 ? 0.0 : _mean;
        }

        public double StdDev()
        {
            if (_count == 0) return 0.0;
            return Math.Sqrt(_s / _count) * 10.0;
        }
    }

    public sealed class PlayerSession
    {
        public readonly int PlayerId;

        public bool HasHit;

        public double LastTiming;
        public double LastAngle;
        public double LastSongTimeMs = -1.0;
        public HitMan LastJudge = HitMan.Unknown;
        public int LastRawMargin = -1;
        public bool LastIsXP;
        public double LastBpm;
        public double LastSpeed = 1.0;
        public double LastPitch = 1.0;

        public readonly List<double> Offsets = new List<double>();

        public readonly UrStats Ur = new UrStats();

        public int NormalPerfectCount;
        public int PerfectFamilyCount;
        public int XPerfectCount;
        public int TotalHitsCount;

        public PlayerSession(int playerId)
        {
            PlayerId = playerId;
        }

        public bool IsXPerfect(HitMan judge)
        {
            if (HitMarginCompat.IsGame34) return judge == HitMan.XPerfect;
            return LastIsXP;
        }

        public void ResetLastHit()
        {
            HasHit = false;
            LastTiming = 0.0;
            LastAngle = 0.0;
            LastSongTimeMs = -1.0;
            LastJudge = HitMan.Unknown;
            LastRawMargin = -1;
            LastIsXP = false;
            LastBpm = 0.0;
            LastSpeed = 1.0;
            LastPitch = 1.0;
        }

        public void ResetCounters()
        {
            NormalPerfectCount = 0;
            PerfectFamilyCount = 0;
            XPerfectCount = 0;
            TotalHitsCount = 0;
        }

        public void ResetSession()
        {
            ResetLastHit();
            ResetCounters();
            Offsets.Clear();
            Ur.Reset();
        }
    }

    public static class Players
    {
        public const int MaxPlayers = 4;

        public const int Unset = -1;

        private static readonly PlayerSession[] _all = CreateAll();

        public static int Count
        {
            get
            {
                int count;
                try
                {
                    count = scrPlayerManager.playerCount;
                }
                catch
                {
                    return 1;
                }

                if (count < 1) return 1;
                if (count > MaxPlayers) return MaxPlayers;
                return count;
            }
        }

        public static bool IsCoop
        {
            get { return Count > 1; }
        }

        public static int CurrentId { get; set; } = Unset;

        public static PlayerSession Current
        {
            get { return Get(CurrentId); }
        }

        private static PlayerSession[] CreateAll()
        {
            PlayerSession[] all = new PlayerSession[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
                all[i] = new PlayerSession(i);
            return all;
        }

        public static PlayerSession Get(int playerId)
        {
            if (playerId < 0 || playerId >= MaxPlayers) playerId = 0;
            return _all[playerId];
        }

        public static void ResetAll()
        {
            for (int i = 0; i < MaxPlayers; i++)
                _all[i].ResetSession();
            CurrentId = Unset;
        }

        public static int ResolvePlayerId(scrPlanet planet)
        {
            if (planet == null) return 0;
            if (Count <= 1) return 0;

            scrPlayer owner = planet.player;
            if (owner != null) return ClampId(owner.playerID);

            return ResolveByPlanetarySystem(planet);
        }

        private static int ResolveByPlanetarySystem(scrPlanet planet)
        {
            try
            {
                PlanetarySystem system = planet.planetarySystem;
                if (system == null) return 0;

                scrPlayerManager manager = ADOBase.playerManager;
                if (manager == null) return 0;

                scrPlayer[] players = manager.players;
                if (players == null) return 0;

                for (int i = 0; i < players.Length; i++)
                {
                    scrPlayer player = players[i];
                    if (player != null && player.planetarySystem == system) return ClampId(i);
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("Players.ResolvePlayerId(planet) fallback failed: " + e.Message);
            }

            return 0;
        }

        public static int ResolvePlayerId(scrMarginTracker tracker)
        {
            if (tracker == null) return 0;

            try
            {
                scrMarginTracker[] trackers = scrMistakesManager.marginTrackers;
                if (trackers != null)
                {
                    for (int i = 0; i < trackers.Length; i++)
                    {
                        if (ReferenceEquals(trackers[i], tracker)) return ClampId(i);
                    }
                }
            }
            catch (Exception e)
            {
                ModContext.Logger?.Log("Players.ResolvePlayerId(tracker) failed: " + e.Message);
            }

            return 0;
        }

        private static int ClampId(int playerId)
        {
            if (playerId < 0) return 0;
            if (playerId >= MaxPlayers) return MaxPlayers - 1;
            return playerId;
        }
    }
}

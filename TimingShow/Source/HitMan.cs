namespace TimingShow
{
    public enum HitMan
    {
        Unknown = -1,
        TooEarly = 0,
        VeryEarly = 1,
        EarlyPerfect = 2,
        PerfectMinus = 3,
        LegacyPerfect = 3,
        XPerfect = 4,
        PerfectPlus = 5,
        LatePerfect = 6,
        VeryLate = 7,
        TooLate = 8,
        Multipress = 9,
        FailMiss = 10,
        FailOverload = 11,
        Auto = 12,
        OverPress = 13,
        Midspin = 14,
        FailedFloor = 15
    }

    public enum HitMarginVersion
    {
        Unknown = 0,
        Legacy = 1,
        Game34 = 2
    }
}

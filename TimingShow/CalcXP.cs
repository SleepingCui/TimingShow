using System;
using UnityEngine;

namespace TimingShow
{
    public static class CalcXP
    {
        public static readonly Color32 XPColor = new Color32(77, 204, byte.MaxValue, byte.MaxValue);
        public static Color XPc(scrPlanet planet, double diff, double bpm, double speed, double pitch)
        {
            ColourSchemeHitMargin hitMarginColours = RDConstants.data.hitMarginColours;

            if (!Main.Settings.EnableXPerfect) return hitMarginColours.colourPerfect;

            if (RDC.auto) return XPColor;

            double absDiff = Math.Abs(diff);
            double xpAngleInRad = 0.01667 * (Math.PI * bpm * speed * pitch / 60.0);
            double xpAngleInDeg = xpAngleInRad * 57.295780181884766;
            double finalXpBoundaryDeg = Math.Max(15.0, xpAngleInDeg);
            double finalXpBoundary = (finalXpBoundaryDeg * 60000.0) / (57.295780181884766 * Math.PI * bpm * speed * pitch);

            return (absDiff <= finalXpBoundary) ? (Color)XPColor : hitMarginColours.colourPerfect;
        }
    }
}

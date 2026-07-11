using System;
using UnityEngine;

namespace TimingShow
{
    public static class CalcXP
    {
        public static readonly Color32 XPColor = new Color32(77, 204, byte.MaxValue, byte.MaxValue);

        public static Color XPc(scrPlanet planet, double diff, double bpm, double speed, double pitch, bool enableXP)
        {
            ColourSchemeHitMargin hitMarginColours = RDConstants.data.hitMarginColours;

            if (!enableXP) return hitMarginColours.colourPerfect;
            if (RDC.auto) return XPColor;

            double absDiff = Math.Abs(diff);
            double angleR = 0.01667 * (Math.PI * bpm * speed * pitch / 60.0);
            double angleD = angleR * 57.295780181884766;
            double fBoundaryD = Math.Max(15.0, angleD);
            double fBoundary = (fBoundaryD * 60000.0) / (57.295780181884766 * Math.PI * bpm * speed * pitch);

            return (absDiff <= fBoundary) ? (Color)XPColor : hitMarginColours.colourPerfect;
        }
    }
}
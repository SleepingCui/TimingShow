using System;
using UnityEngine;

namespace TimingShow
{
    public static class CalcXP
    {
        public static readonly Color32 XPColor = new Color32(77, 204, byte.MaxValue, byte.MaxValue);
        public static readonly Color32 HkModeColor = new Color32(255, 150, 180,byte.MaxValue);

        public static Color XPc(scrPlanet planet, double diff, double bpm, double speed, double pitch, bool enableXP, HitMargin margin)
        {
            ColourSchemeHitMargin hitMarginColours = RDConstants.data.hitMarginColours;
            bool isPS = margin == HitMargin.Perfect || margin == HitMargin.EarlyPerfect || margin == HitMargin.LatePerfect;

            if (isPS)
            {
                if (!enableXP) return hitMarginColours.colourPerfect;

                // use xp mod
                if (XPerfectBridge.IsAvailable)
                {
                    if (Main.Settings.DisplayCurrMode) return (Color)HkModeColor;
                    else return XPerfectBridge.IsXPerfect() ? (Color)XPColor : hitMarginColours.colourPerfect;
                }

                if (RDC.auto) return XPColor;

                double denominator = Math.PI * bpm * speed * pitch;
                if (denominator == 0) return hitMarginColours.colourPerfect;

                double absDiff = Math.Abs(diff);
                double angleR = 0.01667 * (denominator / 60.0);
                double angleD = angleR * 57.295780181884766;
                double fBoundaryD = Math.Max(15.0, angleD);
                double fBoundary = (fBoundaryD * 60000.0) / (57.295780181884766 * denominator);

                return (absDiff <= fBoundary) ? (Color)XPColor : hitMarginColours.colourPerfect;
            }

            switch (margin)
            {
                case HitMargin.TooEarly: return hitMarginColours.colourTooEarly;
                case HitMargin.VeryEarly: return hitMarginColours.colourVeryEarly;
                case HitMargin.VeryLate: return hitMarginColours.colourVeryLate;
                case HitMargin.TooLate: return hitMarginColours.colourTooLate;
                case HitMargin.Multipress: return hitMarginColours.colourMultipress;
                case HitMargin.FailMiss:
                case HitMargin.FailOverload:
                case HitMargin.OverPress: return hitMarginColours.colourFail;
                default: return hitMarginColours.colourPerfect;
            }
        }
    }
}
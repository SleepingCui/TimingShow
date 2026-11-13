namespace TimingShow
{

    internal static class HitWindowScale
    {
        internal const float Unknown = 0f;

        internal static float Sanitize(double scale)
        {
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0.0) return 1f;
            return (float)scale;
        }

        internal static float ReadControllerScale()
        {
            try
            {
                scrController controller = scrController.instance;
                if (controller == null || controller.currFloor == null) return 1f;
                return Sanitize(controller.currFloor.marginScale);
            }
            catch
            {
                return 1f;
            }
        }

        internal static float ReadFloorScale(scrPlanet planet)
        {
            try
            {
                if (planet != null && planet.currfloor != null && planet.currfloor.nextfloor != null)
                    return Sanitize(planet.currfloor.nextfloor.marginScale);
            }
            catch
            {
            }

            return ReadControllerScale();
        }
    }
}

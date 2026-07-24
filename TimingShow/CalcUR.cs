using System.Collections.Generic;
using System;

namespace TimingShow
{
    public static class CalcUR
    {
        public static double calc(List<double> offsets)
        {
            if (offsets == null || offsets.Count == 0) return 0.0;

            double avg = 0.0;
            int count = offsets.Count;
            for (int i = 0; i < count; i++) avg += offsets[i];
            avg /= count;

            double sumOfSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                double diff = offsets[i] - avg;
                sumOfSquares += diff * diff;
            }

            double stdDev = Math.Sqrt(sumOfSquares / count);
            return stdDev * 10.0;
        }
    }
}

using System.Collections.Generic;
using System;

namespace TimingShow
{
    public static class CalcUR
    {
        private static int _count;
        private static double _mean;
        private static double _s;

        public static void AddSample(double x)
        {
            int n = ++_count;
            double d = x - _mean;
            _mean += d / n;
            _s += d * (x - _mean);
        }

        public static void Reset()
        {
            _count = 0;
            _mean = 0.0;
            _s = 0.0;
        }
        
        public static double Calc()
        {
            if (_count == 0) return 0.0;
            return Math.Sqrt(_s / _count) * 10.0;
        }
        
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

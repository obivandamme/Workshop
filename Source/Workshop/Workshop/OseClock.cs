using System;

namespace Workshop
{
    using UnityEngine;

    public class OseClock
    {
        public const double FLOAT_TOLERANCE = 0.000000001d;

        public const double MAX_DELTA_TIME = 86400;

        private double _lastUpdateTime;

        public double GetDeltaTime()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return 0;
                }

                if (Math.Abs(_lastUpdateTime) < FLOAT_TOLERANCE)
                {
                    // Just started running
                    _lastUpdateTime = Planetarium.GetUniversalTime();
                    return 0;
                }

                var deltaTime = Math.Min(Planetarium.GetUniversalTime() - _lastUpdateTime, MAX_DELTA_TIME);
                _lastUpdateTime += deltaTime;
                return deltaTime;
            }
            catch (Exception e)
            {
                Debug.LogError("[OSE] - OseClock_GetDeltaTime - " + e.Message);
                return 0;
            }
        }
    }
}

using System;

namespace Workshop
{
    using UnityEngine;

    public class Clock
    {
        private double _lastUpdateTime;

        public double GetDeltaTime()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return 0;
                }

                if (Math.Abs(_lastUpdateTime) < 0.000000001d)
                {
                    // Just started running
                    _lastUpdateTime = Planetarium.GetUniversalTime();
                    return 0;
                }

                var deltaTime = Math.Min(Planetarium.GetUniversalTime() - _lastUpdateTime, 86400);
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

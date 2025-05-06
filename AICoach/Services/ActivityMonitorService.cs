using System;
using System.Runtime.InteropServices;

namespace AICoach.Services
{
    public class ActivityMonitorService
    {
        // Structure for GetLastInputInfo
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        // Import GetLastInputInfo function
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // Default idle time threshold (in milliseconds)
        private readonly uint _idleThreshold;

        public ActivityMonitorService(uint idleThresholdMs = 60000)
        {
            _idleThreshold = idleThresholdMs;
        }

        public bool IsUserActive()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                // Could not get last input info, assume inactive or handle error
                Logger.Instance.Log("Error getting last input info.");
                return false;
            }

            // Get the current tick count
            uint currentTickCount = (uint)Environment.TickCount;

            // Calculate idle time in milliseconds
            uint idleTime = currentTickCount - lastInputInfo.dwTime;

            // Consider user active if idle time is less than the threshold
            return idleTime < _idleThreshold;
        }
    }
}
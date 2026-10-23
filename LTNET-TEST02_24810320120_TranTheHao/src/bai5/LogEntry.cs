using System;

namespace IoTMonitor
{
    public class LogEntry
    {
        public const double Threshold = 37.0;   // trên ngưỡng này là cảnh báo

        public int No { get; set; }
        public DateTime Time { get; set; }
        public double Temperature { get; set; }

        public bool IsWarning => Temperature > Threshold;
        public string Status => IsWarning ? "Cảnh báo" : "Bình thường";
    }
}

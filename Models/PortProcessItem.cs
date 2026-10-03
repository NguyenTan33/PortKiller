using System;

namespace PortKiller.Models
{
    public class PortProcessItem
    {
        public int Port { get; set; }
        public string Protocol { get; set; } = "TCP";
        public string State { get; set; } = "LISTENING";
        public int Pid { get; set; }
        public string ProcessName { get; set; } = "Unknown";
        public double MemoryMb { get; set; }
        public string ExecutablePath { get; set; } = "";
        public string LocalAddress { get; set; } = "";

        public string FormattedMemory => MemoryMb > 0 ? $"{MemoryMb:N1} MB" : "-";
        public string DisplayName => string.IsNullOrWhiteSpace(ProcessName) ? $"PID {Pid}" : ProcessName;

        public bool IsDevPort => Port is 3000 or 3001 or 5000 or 5001 or 5173 or 8000 or 8080 or 8888 or 1433 or 5432 or 27017;
        public string IconKind => IsDevPort ? "⚡" : (State == "LISTENING" ? "🌐" : "⚙️");
    }
}

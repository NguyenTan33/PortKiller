using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PortKiller.Models;

namespace PortKiller.Services
{
    public class PortService
    {
        public async Task<List<PortProcessItem>> GetActivePortsAsync()
        {
            var list = new List<PortProcessItem>();

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano -p tcp",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return;

                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var dict = new Dictionary<string, PortProcessItem>();

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 4) continue;

                        string proto = parts[0];
                        if (!proto.Equals("TCP", StringComparison.OrdinalIgnoreCase) && !proto.Equals("UDP", StringComparison.OrdinalIgnoreCase)) continue;

                        string localAddr = parts[1];
                        string state = parts.Length >= 5 ? parts[3] : "LISTENING";
                        string pidStr = parts.Length >= 5 ? parts[4] : parts[3];

                        if (!int.TryParse(pidStr, out int pid) || pid <= 0) continue;

                        int lastColon = localAddr.LastIndexOf(':');
                        if (lastColon <= 0 || lastColon >= localAddr.Length - 1) continue;

                        string portStr = localAddr.Substring(lastColon + 1);
                        if (!int.TryParse(portStr, out int port) || port <= 0) continue;

                        string key = $"{proto}:{port}:{pid}";
                        if (dict.ContainsKey(key)) continue;

                        var item = new PortProcessItem
                        {
                            Port = port,
                            Protocol = proto.ToUpper(),
                            State = state.ToUpper(),
                            Pid = pid,
                            LocalAddress = localAddr
                        };

                        try
                        {
                            var p = Process.GetProcessById(pid);
                            item.ProcessName = p.ProcessName + ".exe";
                            item.MemoryMb = p.WorkingSet64 / (1024.0 * 1024.0);

                            try
                            {
                                item.ExecutablePath = p.MainModule?.FileName ?? "";
                            }
                            catch { }
                        }
                        catch
                        {
                            item.ProcessName = "System / Service";
                        }

                        dict[key] = item;
                    }

                    list = dict.Values
                        .OrderByDescending(x => x.IsDevPort)
                        .ThenBy(x => x.Port)
                        .ToList();
                }
                catch { }
            });

            return list;
        }

        public async Task<(bool Success, string Message)> KillProcessByPidAsync(int pid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /PID {pid} /T",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return (false, "Không thể khởi động lệnh taskkill");

                    string err = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode == 0)
                    {
                        return (true, $"✅ Đã diệt thành công tiến trình PID {pid}!");
                    }
                    else
                    {
                        return (false, $"❌ Lỗi diệt PID {pid}: {err.Trim()}");
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"❌ Lỗi: {ex.Message}");
                }
            });
        }

        public async Task<(bool Success, string Message)> KillProcessByPortAsync(int port)
        {
            var ports = await GetActivePortsAsync();
            var target = ports.FirstOrDefault(x => x.Port == port);

            if (target == null)
            {
                return (false, $"⚠️ Không tìm thấy tiến trình nào đang chiếm Cổng {port}.");
            }

            return await KillProcessByPidAsync(target.Pid);
        }

        public async Task<(int KilledCount, string Message)> KillProcessesByNameAsync(string processName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string cleanName = processName.Replace(".exe", "").Trim();
                    var procs = Process.GetProcessesByName(cleanName);
                    int count = 0;

                    foreach (var p in procs)
                    {
                        try
                        {
                            p.Kill(true);
                            count++;
                        }
                        catch { }
                    }

                    if (count > 0)
                    {
                        return (count, $"✅ Đã diệt thành công {count} tiến trình '{cleanName}.exe'!");
                    }
                    else
                    {
                        return (0, $"⚠️ Không có tiến trình '{cleanName}.exe' nào đang chạy.");
                    }
                }
                catch (Exception ex)
                {
                    return (0, $"❌ Lỗi: {ex.Message}");
                }
            });
        }
    }
}

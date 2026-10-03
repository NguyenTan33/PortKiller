using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PortKiller.Models;
using PortKiller.Services;

namespace PortKiller
{
    public partial class MainWindow : Window
    {
        private readonly PortService _portService = new();
        private List<PortProcessItem> _allPorts = new();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                Activate();
                Focus();
                Topmost = true;
                Topmost = false;

                await RefreshPortsAsync();
            };
        }

        private async Task RefreshPortsAsync()
        {
            if (OverlayScanning == null || TxtStatus == null || GridResults == null) return;

            OverlayScanning.Visibility = Visibility.Visible;
            TxtStatus.Text = "⏳ Đang quét danh sách Cổng Port & Tiến trình ngầm...";

            try
            {
                _allPorts = await _portService.GetActivePortsAsync();
                ApplyFilter();
                TxtStatus.Text = $"✅ Phát hiện {_allPorts.Count:N0} cổng active. Quét lúc {DateTime.Now:HH:mm:ss}.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Lỗi quét port: {ex.Message}";
            }
            finally
            {
                OverlayScanning.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter()
        {
            if (TxtSearch == null || GridResults == null) return;

            string query = TxtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                GridResults.ItemsSource = _allPorts;
                return;
            }

            var filtered = _allPorts.Where(p =>
                p.Port.ToString().Contains(query) ||
                p.ProcessName.ToLower().Contains(query) ||
                p.Pid.ToString().Contains(query) ||
                p.LocalAddress.ToLower().Contains(query) ||
                p.ExecutablePath.ToLower().Contains(query)
            ).ToList();

            GridResults.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            TxtSearch.Focus();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPortsAsync();
        }

        private async void BtnKillTargetPort_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtTargetPort.Text.Trim(), out int port) && port > 0)
            {
                await KillPortDirectlyAsync(port);
            }
            else
            {
                MessageBox.Show("Vui lòng nhập số cổng Port hợp lệ (ví dụ: 3000, 8080)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void TxtTargetPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnKillTargetPort_Click(sender, e);
            }
        }

        private async Task KillPortDirectlyAsync(int port)
        {
            var (success, msg) = await _portService.KillProcessByPortAsync(port);
            TxtStatus.Text = msg;
            MessageBox.Show(msg, success ? "Diệt Thành Công" : "Thông Báo", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await RefreshPortsAsync();
        }

        private async void BtnQuickKill3000_Click(object sender, RoutedEventArgs e) => await KillPortDirectlyAsync(3000);
        private async void BtnQuickKill5000_Click(object sender, RoutedEventArgs e) => await KillPortDirectlyAsync(5000);
        private async void BtnQuickKill5173_Click(object sender, RoutedEventArgs e) => await KillPortDirectlyAsync(5173);
        private async void BtnQuickKill8080_Click(object sender, RoutedEventArgs e) => await KillPortDirectlyAsync(8080);

        private async void BtnKillAllNode_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Bạn có chắc chắn muốn diệt TẤT CẢ các tiến trình Node.js (node.exe) đang chạy ngầm?", "Xác Nhận Diệt Node.js", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (count, msg) = await _portService.KillProcessesByNameAsync("node");
                TxtStatus.Text = msg;
                MessageBox.Show(msg, "Kết Quả Diệt Node.js", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshPortsAsync();
            }
        }

        private async void BtnKillAllDotnet_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Bạn có chắc chắn muốn diệt TẤT CẢ các tiến trình .NET (dotnet.exe) đang chạy ngầm?", "Xác Nhận Diệt .NET", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (count, msg) = await _portService.KillProcessesByNameAsync("dotnet");
                TxtStatus.Text = msg;
                MessageBox.Show(msg, "Kết Quả Diệt .NET", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshPortsAsync();
            }
        }

        private async void BtnKillAllPython_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Bạn có chắc chắn muốn diệt TẤT CẢ các tiến trình Python (python.exe) đang chạy ngầm?", "Xác Nhận Diệt Python", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var (count, msg) = await _portService.KillProcessesByNameAsync("python");
                TxtStatus.Text = msg;
                MessageBox.Show(msg, "Kết Quả Diệt Python", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshPortsAsync();
            }
        }

        private async void GridResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await KillSelectedProcessAsync();
        }

        private async void MenuKillProcess_Click(object sender, RoutedEventArgs e)
        {
            await KillSelectedProcessAsync();
        }

        private async Task KillSelectedProcessAsync()
        {
            if (GridResults.SelectedItem is PortProcessItem selected)
            {
                var res = MessageBox.Show($"Bạn có chắc chắn muốn diệt tiến trình '{selected.ProcessName}' (PID {selected.Pid}) đang chiếm Port {selected.Port}?", "Xác Nhận Diệt Tiến Trình", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    var (success, msg) = await _portService.KillProcessByPidAsync(selected.Pid);
                    TxtStatus.Text = msg;
                    MessageBox.Show(msg, success ? "Diệt Thành Công" : "Lỗi", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
                    await RefreshPortsAsync();
                }
            }
        }

        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is PortProcessItem selected && !string.IsNullOrEmpty(selected.ExecutablePath))
            {
                try
                {
                    if (File.Exists(selected.ExecutablePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{selected.ExecutablePath}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuCopyPort_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is PortProcessItem selected)
            {
                Clipboard.SetText(selected.Port.ToString());
                TxtStatus.Text = $"📋 Đã copy Port: {selected.Port}";
            }
        }

        private void MenuCopyPid_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is PortProcessItem selected)
            {
                Clipboard.SetText(selected.Pid.ToString());
                TxtStatus.Text = $"📋 Đã copy PID: {selected.Pid}";
            }
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is PortProcessItem selected)
            {
                Clipboard.SetText(selected.ExecutablePath);
                TxtStatus.Text = $"📋 Đã copy đường dẫn: {selected.ExecutablePath}";
            }
        }

        private void GridResults_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                await RefreshPortsAsync();
            }
            else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
            }
            else if (e.Key == Key.Escape)
            {
                if (TxtSearch.IsFocused)
                {
                    TxtSearch.Clear();
                }
            }
        }
    }
}
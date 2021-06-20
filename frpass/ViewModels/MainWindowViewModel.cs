using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using frpass.Models;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using ReactiveUI;


namespace frpass.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static string frpDir = AppDomain.CurrentDomain.BaseDirectory + "frp";
        private static string frpPath = frpDir + "\\frpc.exe";
        private static string frpiniPath = frpDir + "\\frpc.ini";
        private static string ServerjsonPath = frpDir + "\\server.json";
        private static string ForwardjsonPath = frpDir + "\\forward.json";

        public Process _process;

        #region 数组

        public ObservableCollection<ServerConfig> ServerConfigs { get; set; }

        public ObservableCollection<ForwardConfig> ForwardConfigs { get; set; }


        public MainWindowViewModel()
        {
            ServerConfigs = new ObservableCollection<ServerConfig>();
            ForwardConfigs = new ObservableCollection<ForwardConfig>();
            ReadConfig();
        }

        #endregion

        #region 字符串和数字

        private int _SelectedServer = 0;
        private string _FrpProxy = "";
        private string _FrpLog;

        public int SelectedServer
        {
            get => _SelectedServer;
            set { this.RaiseAndSetIfChanged(ref _SelectedServer, value); }
        }

        public string FrpProxy
        {
            get => _FrpProxy;
            set { this.RaiseAndSetIfChanged(ref _FrpProxy, value); }
        }

        public string FrpLog
        {
            get => _FrpLog;
            set { this.RaiseAndSetIfChanged(ref _FrpLog, value); }
        }

        #endregion

        #region 添加和保存按键

        public void SaveServer()
        {
            string jsonString = JsonSerializer.Serialize(ServerConfigs);
            File.WriteAllText(ServerjsonPath, jsonString);
        }

        public void SaveForward()
        {
            string jsonString = JsonSerializer.Serialize(ForwardConfigs);
            File.WriteAllText(ForwardjsonPath, jsonString);
        }

        public void AddServer()
        {
            ServerConfigs.Add(new ServerConfig()
            {
                IP = "127.0.0.1",
                Port = 7000,
                Token = "example"
            });
        }

        public void AddForward()
        {
            ForwardConfigs.Add(new ForwardConfig()
            {
                config_name = "eg",
                type = "tcp",
                local_ip = "0.0.0.0",
                local_port = 3389,
                remote_port = 13389
            });
        }

        public void RemoveServer()
        {
            if (ServerConfigs.Count != 0)
            {
                ServerConfigs.Remove(ServerConfigs.Last());
            }
        }


        public void RemoveForward()
        {
            if (ForwardConfigs.Count != 0)
            {
                ForwardConfigs.Remove(ForwardConfigs.Last());
            }
        }

        #endregion

        #region 配置文件读写逻辑

        public void ReadConfig()
        {
            if (File.Exists(ServerjsonPath))
            {
                string jsonString = File.ReadAllText(ServerjsonPath);
                var config = JsonSerializer.Deserialize<ObservableCollection<ServerConfig>>(jsonString);
                ServerConfigs = config;
                Trace.WriteLine(config);
            }
            else
            {
                AddServer();
                string jsonString = JsonSerializer.Serialize(ServerConfigs);
                File.WriteAllText(ServerjsonPath, jsonString);
            }

            if (File.Exists(ForwardjsonPath))
            {
                string jsonString = File.ReadAllText(ForwardjsonPath);
                var config = JsonSerializer.Deserialize<ObservableCollection<ForwardConfig>>(jsonString);
                ForwardConfigs = config;
                Trace.WriteLine(config);
            }
            else
            {
                AddForward();
                string jsonString = JsonSerializer.Serialize(ForwardConfigs);
                File.WriteAllText(ForwardjsonPath, jsonString);
            }
        }

        public void SaveFrpini()
        {
            ServerConfig s = ServerConfigs[SelectedServer];
            using (StreamWriter writer = new StreamWriter(frpiniPath))
            {
                writer.WriteLine($"[common]");
                writer.WriteLine($"server_addr = {s.IP}");
                writer.WriteLine($"server_port = {s.Port}");
                writer.WriteLine($"token = {s.Token}");
                writer.WriteLine($"http_proxy = {FrpProxy}");
                writer.WriteLine($"login_fail_exit = false");

                writer.WriteLine();

                foreach (var f in ForwardConfigs)
                {
                    writer.WriteLine($"[{f.config_name}]");
                    writer.WriteLine($"type = {f.type}");
                    writer.WriteLine($"local_ip = {f.local_ip}");
                    writer.WriteLine($"local_port = {f.local_port}");
                    writer.WriteLine($"remote_port = {f.remote_port}");
                    writer.WriteLine();
                }
            }
        }

        #endregion

        #region frp运行逻辑

        public void ShowLog(string log)
        {
            FrpLog += log + Environment.NewLine;
        }

        private async void runFrp()
        {
            Trace.WriteLine(frpPath);
            if (!File.Exists(frpPath))
            {
                var msBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "错误",
                        ContentMessage = "Frp客户端不存在",
                        FontFamily = "Microsoft YaHei,Simsun",
                        Icon = Icon.Warning,
                        Style = Style.UbuntuLinux,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    });
                await msBoxStandardWindow.Show();
            }
            else
            {
                if (_process != null)
                    stopFrp();
                ShowLog("writing frpc.ini ...");
                SaveFrpini();
                execFrp("-c frpc.ini");
            }
        }

        private void KillProcess(Process p)
        {
            try
            {
                p.CloseMainWindow();
                p.WaitForExit(100);
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit(100);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        private void stopFrp()
        {
            try
            {
                if (_process != null)
                {
                    ShowLog($"kill frpc.exe Process ID {_process.Id}");
                    KillProcess(_process);
                    _process.Dispose();
                    _process = null;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        private void execFrp(string arg)
        {
            try
            {
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = frpPath,
                        Arguments = arg,
                        WorkingDirectory = frpDir,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    }
                };
                p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        ShowLog(e.Data);
                    }
                });
                ShowLog("run frpc.exe");
                p.Start();
                p.PriorityClass = ProcessPriorityClass.High;
                p.BeginOutputReadLine();
                _process = p;

                // if (p.WaitForExit(1000))
                // {
                //     ShowLog(p.StandardError.ReadToEnd());
                //     throw new Exception(p.StandardError.ReadToEnd());
                // }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        #endregion

        #region 关于

        public async void AboutApp()
        {
            var msBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "关于",
                    ContentMessage = "Frp客户端管理工具",
                    FontFamily = "Microsoft YaHei,Simsun",
                    Icon = Icon.Info,
                    Style = Style.Windows,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                });
            await msBoxStandardWindow.Show();
        }

        #endregion
    }
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using NotificationIconSharp;
using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using frpass.Views;
using frpass.ViewModels;

namespace frpass
{
    public class App : Application
    {
        private MainWindow _mainWindow = null;
        private NotificationIcon _notificationIcon = null;
        private string icon_path = AppDomain.CurrentDomain.BaseDirectory + "tray.ico";
        private string icon_assets_path = "avares://frpass/Assets/tray.ico";

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //desktop.MainWindow = new MainWindow
                //{
                //    DataContext = new MainWindowViewModel(),
                //    WindowStartupLocation = WindowStartupLocation.CenterScreen
                //};
            }

            base.OnFrameworkInitializationCompleted();

            if (_notificationIcon == null)
            {
                // Create the notification tray icon, this should be the main thread anyways

                Trace.WriteLine(icon_path);

                if (!File.Exists(icon_path))
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    var icon_stream = assets.Open(new Uri(icon_assets_path));

                    using (var fileStream = File.Create(icon_path))
                    {
                        icon_stream.Seek(0, SeekOrigin.Begin);
                        icon_stream.CopyTo(fileStream);
                    }
                }

                _notificationIcon = new NotificationIcon(icon_path);
                _notificationIcon.NotificationIconSelected += Icon_NotificationIconSelected;
                //_notificationIcon.NotificationIconSelected += TrayIcon_NotificationIconSelected;
            }
        }

        private void TrayIcon_NotificationIconSelected(NotificationIcon icon)
        {
            if (icon.MenuItems.Count > 0) return;
            var openMenuItem = new NotificationMenuItem("打开");
            var quitMenuItem = new NotificationMenuItem("退出");
            openMenuItem.NotificationMenuItemSelected += OpenMenuItem_NotificationMenuItemSelected;
            quitMenuItem.NotificationMenuItemSelected += QuitMenuItem_NotificationMenuItemSelected;
            icon.AddMenuItem(openMenuItem);
            icon.AddMenuItem(quitMenuItem);
        }

        private void OpenMenuItem_NotificationMenuItemSelected(NotificationMenuItem menuItem)
            => RestoreMainWindow();

        private void QuitMenuItem_NotificationMenuItemSelected(NotificationMenuItem menuItem)
            => Exit();


        /// <summary>
        /// show and/or create the main window instance if necessary
        /// </summary>
        public void RestoreMainWindow()
        {
            var desktopApplicationLifetime = Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                _mainWindow.Closing += MainWindow_Closing;
            }

            if (desktopApplicationLifetime?.MainWindow != _mainWindow)
            {
                desktopApplicationLifetime.MainWindow = _mainWindow;
            }

            _mainWindow.Show();
            //_mainWindow.WindowState = WindowState.Normal;
            //_mainWindow.BringIntoView();
            //_mainWindow.Focus();
        }


        /// <summary>
        /// Exit the application
        /// </summary>
        public void Exit()
        {
            (Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(0);
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    Style = Style.UbuntuLinux,
                    Topmost = true,
                    ContentTitle = "提示",
                    ContentMessage = "退出程序或关闭窗口保持后台运行状态",
                    FontFamily = "Microsoft YaHei,Simsun",
                    Icon = Icon.Warning,
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition {Name = "退出", IsCancel = false},
                        new ButtonDefinition {Name = "取消", IsCancel = true},
                        new ButtonDefinition {Name = "关闭窗口", Type = ButtonType.Colored, IsDefault = true}
                    },
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                });

            var result = await messageBoxCustomWindow.ShowDialog(_mainWindow);
            Trace.WriteLine(result);
            if (result == "退出")
                Exit();
            else if (result == "关闭窗口")
                _mainWindow?.Hide();
        }

        private void Icon_NotificationIconSelected(NotificationIcon icon)
        {
            Dispatcher.UIThread.Post(() =>
            {
                RestoreMainWindow(); // Restore main window on notification icon click
            });
        }
    }
}
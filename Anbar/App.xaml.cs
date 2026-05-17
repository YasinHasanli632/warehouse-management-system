using Anbar.Data;
using Anbar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Anbar
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            await InitializeSystemSettingsAsync();
        }

        private async Task InitializeSystemSettingsAsync()
        {
            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer("Server=DESKTOP-JFHUTM3;Database=AnbarDb;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

                await using var context = new AppDbContext(options);

                var settingsService = new SettingsService(context);

                await settingsService.EnsureDefaultSettingsAsync();
                await settingsService.LoadCacheAsync(forceReload: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Sistem ayarları başlanğıcda hazırlanmadı:\n{ex.Message}",
                    "Startup settings xətası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Tutulmamış UI xətası", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject?.ToString() ?? "Naməlum xəta", "Tutulmamış sistem xətası", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Async xəta", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved();
        }
    }
}
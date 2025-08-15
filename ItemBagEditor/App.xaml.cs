/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using ItemBagEditor.Services;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace ItemBagEditor
{
    public partial class App : Application
    {
        private ServiceProvider? serviceProvider;
        private Microsoft.Extensions.Logging.ILogger<App>? logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure Serilog logging
                ConfigureLogging();
                
                base.OnStartup(e);

                // Configure services
                var services = new ServiceCollection();
                ConfigureServices(services);
                serviceProvider = services.BuildServiceProvider();
                
                // Create logger for App after services are configured
                logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();
                logger.LogInformation("Application starting up...");
                
                logger.LogInformation("Services configured successfully");

                // Show main window
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                logger.LogInformation("Main window created, showing...");
                mainWindow.Show();
                
                logger.LogInformation("Application startup completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during application startup");
                MessageBox.Show($"Application failed to start: {ex.Message}\n\nCheck the log file for details.", 
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void ConfigureLogging()
        {
            // Check if file logging is enabled via environment variable or command line
            bool enableFileLogging = ShouldEnableFileLogging();
            
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console();

            // Only add file logging if explicitly enabled
            if (enableFileLogging)
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "ItemBagEditor-.log");
                var logDir = Path.GetDirectoryName(logPath);
                
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                loggerConfig = loggerConfig.WriteTo.File(logPath, 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();
            
            // Log whether file logging is enabled
            if (enableFileLogging)
            {
                Log.Information("File logging enabled - logs will be written to logs/ folder");
            }
            else
            {
                Log.Information("File logging disabled - only console logging is active");
            }
        }

        private bool ShouldEnableFileLogging()
        {
            // Check environment variable
            var envVar = Environment.GetEnvironmentVariable("ITEMBAG_EDITOR_ENABLE_FILE_LOGGING");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                       envVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                       envVar.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            // Check command line arguments
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("--enable-file-logging") || args.Contains("-f"))
            {
                return true;
            }

            // Check for a config file
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logging.config");
            if (File.Exists(configPath))
            {
                try
                {
                    var configContent = File.ReadAllText(configPath).Trim().ToLower();
                    return configContent == "true" || configContent == "1" || configContent == "yes";
                }
                catch
                {
                    // If config file can't be read, default to disabled
                    return false;
                }
            }

            // Default to enabled for better debugging
            return true;
        }

        private void ConfigureServices(ServiceCollection services)
        {
            try
            {
                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddSerilog(dispose: true);
                    builder.SetMinimumLevel(LogLevel.Debug);
                });

                // Add main window
                services.AddTransient<MainWindow>();
                
                // Add services
                services.AddSingleton<IItemListService, ItemListService>();
                services.AddSingleton<IItemBagService, ItemBagService>();
                services.AddSingleton<IItemBagItemService, ItemBagItemService>();
                
                Log.Information("Services configuration completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error configuring services");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                logger?.LogInformation("Application shutting down...");
                serviceProvider?.Dispose();
                Log.Information("Application shutdown completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application shutdown");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            
            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled exception in UI thread");
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nCheck the log file for details.", 
                "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Set up global exception handling
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception in application domain");
            }
        }
    }
}

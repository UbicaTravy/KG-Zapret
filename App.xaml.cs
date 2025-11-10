using System;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using KG_Zapret.Services;
using KG_Zapret.ViewModels;
using KG_Zapret.Views;

namespace KG_Zapret {
    public partial class App : Application {
        private ServiceProvider? _serviceProvider;
        private static Mutex? _mutex;
        
        public static IServiceProvider Services => ((App)Current)._serviceProvider!;

        protected override async void OnStartup(StartupEventArgs e) {
            bool createdNew;
            _mutex = new Mutex(true, "KG-ZapretSingleInstance", out createdNew);
            
            if (!createdNew) {
                MessageBox.Show("KG-Zapret уже запущен!", "KG-Zapret", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            var splash = new SplashScreen();
            var splashTask = splash.ShowSplashAsync(2000);

            System.IO.Directory.CreateDirectory("logs");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/kg-zapret-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 50)
                .WriteTo.Debug()
                .CreateLogger();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            await splashTask;

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            
            var systemTrayService = _serviceProvider.GetRequiredService<ISystemTrayService>();
            systemTrayService.Initialize(mainWindow);
            
            splash.Close();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services) {
            services.AddLogging(builder => {
                builder.AddSerilog();
                builder.AddDebug();
            });

            // services
            services.AddSingleton<IRegistryService, RegistryService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDpiService, DpiService>();
            services.AddSingleton<IDnsService, DnsService>();
            services.AddSingleton<IHostsService, HostsService>();
            services.AddSingleton<IStrategyService, StrategyService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();
            services.AddSingleton<IAutostartService, AutostartService>();

            // view models
            services.AddTransient<MainWindowViewModel>();

            // views
            services.AddTransient<MainWindow>();
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
            // лоджик запуска находится в OnStartup
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            _serviceProvider?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            Log.CloseAndFlush();
        }
    }
}


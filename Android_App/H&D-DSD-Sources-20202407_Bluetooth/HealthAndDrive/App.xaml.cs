using Crossover.Bazarin.Languages;
using Crossover.Bazarin.Permissions;
using HealthAndDrive.Protocol;
using HealthAndDrive.Repositories;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using HealthAndDrive.ViewModels;
using HealthAndDrive.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Realms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace HealthAndDrive
{
    [AutoRegisterForNavigation]
    public partial class App
    {
        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */

        /// <summary>
        /// The App Center Secret for DSD app
        /// </summary>
        private const string APP_CENTER_DSD_APPSECRET = "android=c34d983e-4afb-4ad0-ab02-98b918d9a47c;";

        /// <summary>
        /// Needed for navigation through notifications
        /// </summary>
        public INavigationService PrismNavigation { get; private set; }

        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override async void OnInitialized()
        {
            InitializeComponent();
            TranslateExtension.ResourceId = "HealthAndDrive.AppResources";
            TranslateExtension.ResourcesAssemblyType = typeof(App);

            PrismNavigation = base.NavigationService;

            // Permissions
            InitPermissions();

            // AppCenter analytics
            AppCenter.Start(APP_CENTER_DSD_APPSECRET,
                  typeof(Analytics), typeof(Crashes));

            await NavigationService.NavigateAsync("/SplashPage");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var config = new RealmConfiguration()
            {
                SchemaVersion = 26
            };

            containerRegistry.RegisterSingleton<AppSettings>();

            // Register repositories
            containerRegistry.RegisterInstance(Realm.GetInstance(config));
            containerRegistry.Register<IUserRepository, UserRepository>();
            containerRegistry.Register<IUserSettingsRepository, UserSettingsRepository>();
            containerRegistry.Register<ISensorRepository, SensorRepository>();
            containerRegistry.Register<IGlucoseMeasureRepository, GlucoseMeasureRepository>();

            //containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<PrismNavigationPage>();
            containerRegistry.RegisterForNavigation<DrivePage, DrivePageViewModel>();

            var eventAggregator = Container.Resolve<IEventAggregator>();
            var appSettings = Container.Resolve<AppSettings>();
            var userRepository = Container.Resolve<IUserRepository>();

            var glucoseService = new GlucoseService(eventAggregator, Container.Resolve<IUserRepository>(), Container.Resolve<IUserSettingsRepository>(), Container.Resolve<ISensorRepository>(), Container.Resolve<IGlucoseMeasureRepository>(), appSettings);
            containerRegistry.RegisterInstance(glucoseService);
            containerRegistry.RegisterInstance(new MiaoMiaoProtocol(eventAggregator, userRepository, glucoseService, appSettings));
            containerRegistry.RegisterInstance(new BubbleProtocol(eventAggregator, userRepository, glucoseService, appSettings));
            
            
        }

        /// <summary>
        /// This method initialize permissions
        /// </summary>
        private void InitPermissions()
        {
            CrossPermissions.Current.RegisterPermissionSetting(Permission.Storage, (bool permissionGranted) => { },
           "Permission storage");

            CrossPermissions.Current.RegisterPermissionSetting(Permission.Location, (bool permissionGranted) => { },
            "Permission localisation");

            CrossPermissions.Current.RegisterPermissionSetting(Permission.Phone, (bool permissionGranted) => { },
            "Permission phone");
        }

        public INavigationService GetNavigationService() => this.NavigationService;

    }
}

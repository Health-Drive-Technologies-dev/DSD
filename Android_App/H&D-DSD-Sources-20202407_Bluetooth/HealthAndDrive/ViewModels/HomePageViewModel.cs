using Crossover.Bazarin.PrismLib.ViewModels;
using HealthAndDrive.DependencyServices;
using HealthAndDrive.Events;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using Microsoft.AppCenter.Analytics;
using Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace HealthAndDrive.ViewModels
{
    public class HomePageViewModel : ViewModelBase, IActiveAware
    {
        public event EventHandler IsActiveChanged;

        private readonly IEventAggregator eventAggregator;

        /// <summary>
        /// Gets or sets the navigate to drive command
        /// </summary>
        public ICommand NavigateToDriveCommand { get; }

        /// <summary>
        /// Gets or sets the navigate to alerts command
        /// </summary>
        public ICommand NavigateToAlertsCommand { get; }

        /// <summary>
        /// Gets or sets the navigate to reports command
        /// </summary>
        public ICommand NavigateToReportsCommand { get; }

        /// <summary>
        /// Gets or sets the navigate to bluetooth command
        /// </summary>
        public ICommand NavigateToBluetoothCommand { get; }

        /// <summary>
        /// Get the alert command
        /// </summary>
        public ICommand AlertsCommand { get; }

        /// <summary>
        /// Gets or sets a boolean value indicating if the tab is active
        /// </summary>
        private bool isActive;
        /// <summary>
        /// Gets or sets a boolean value indicating if the tab is active
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                SetProperty(ref isActive, value);
                if (value)
                {
                    eventAggregator.GetEvent<BarTitleChangedEvent>().Publish(AppResources.HomePage_Title);
                    Analytics.TrackEvent(AnalyticsEvent.HomePageViewed);
                }
            }
        }

        private bool isAlert;
        public bool IsAlert
        {
            get { return isAlert; }
            set { SetProperty(ref isAlert, value); }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating if the widget is enabled
        /// </summary>
        private bool isWidgetEnable;
        /// <summary>
        /// Gets or sets a boolean value indicating if the widget is enabled
        /// </summary>
        public bool IsWidgetEnable
        {
            get { return isWidgetEnable; }
            set
            {
                SetProperty(ref isWidgetEnable, value); 
                this.userRepository.ChangeIsWidgetEnable(this.userRepository.GetCurrentUser(), isWidgetEnable);
                DependencyService.Get<IFloatingWidgetService>().IsEnable = value;
            }
        }

        /// <summary>
        /// The user repository
        /// </summary>
        private readonly IUserRepository userRepository;

        private readonly IGlucoseMeasureRepository glucoseMeasureRepository;

        private AppSettings settings;

        public HomePageViewModel(IEventAggregator ea, INavigationService navigationService, IUserRepository userRepository,
            IGlucoseMeasureRepository glucoseMeasureRepository, AppSettings settings) : base(navigationService)
        {
            eventAggregator = ea;
            this.userRepository = userRepository;
            this.glucoseMeasureRepository = glucoseMeasureRepository;
            this.settings = settings;



            this.NavigateToDriveCommand = new DelegateCommand(async () =>
            {
                await this.NavigationService.NavigateAsync("/RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=DrivePage");
            });

            this.NavigateToAlertsCommand = new DelegateCommand(async () =>
            {
                await this.NavigationService.NavigateAsync("/RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=AlertsPage");
            });

            this.NavigateToReportsCommand = new DelegateCommand(async () =>
            {
                await this.NavigationService.NavigateAsync("/RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=ReportsPage");
            });

            this.NavigateToBluetoothCommand = new DelegateCommand(async () =>
            {
                await this.NavigationService.NavigateAsync("/RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=BluetoothPage");
            });

            this.AlertsCommand = new DelegateCommand(async () =>
            {
                await this.NavigationService.NavigateAsync("/RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=AlertsPage");
            });

            /*if (!Utils.IsValidTDE(settings.TDE))
            {
                var closer = DependencyService.Get<ICloseApplication>();
                closer?.closeApplication();
            }*/

            IsAlert = this.userRepository.GetCurrentUser().IsAlert; 
            IsWidgetEnable = this.userRepository.GetCurrentUser().IsWidgetEnable;
        }

    }
}
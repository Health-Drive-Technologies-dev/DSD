using Crossover.Bazarin.PrismLib.ViewModels;
using HealthAndDrive.Events;
using HealthAndDrive.Models;
using HealthAndDrive.RepositoryContracts;
using Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using System;
using HealthAndDrive.Tools;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;
using HealthAndDrive.Services;
using System.Threading.Tasks;
using HealthAndDrive.Protocol;
using System.Collections.Generic;
using HealthAndDrive.Models.Enums;
using HealthAndDrive.Events.Notifications;
using Microsoft.AppCenter.Analytics;
using Android.Util;
using Android.Bluetooth;

namespace HealthAndDrive.ViewModels
{
    public class BluetoothPageViewModel : ViewModelBase, IActiveAware
    {

        public event EventHandler IsActiveChanged;

        private readonly IEventAggregator eventAggregator;

        /// <summary>
        /// Gets or sets the ble service
        /// </summary>
        public BluetoothScanService BleService { get; set; }

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
                    eventAggregator.GetEvent<BarTitleChangedEvent>().Publish(AppResources.BluetoothPage_Title);
                    Analytics.TrackEvent(AnalyticsEvent.BluetoothPageViewed);
                    this.CalibrationSourceValue = UserRepository.GetCurrentUser().CurrentMeasure;
                    this.CalibrationRevisedValue = this.CalibrationSourceValue;
                }
            }
        }

        /// <summary>
        /// The current user value
        /// </summary>
        private User currentUser;

        /// <summary>
        /// Gets or sets the current user value
        /// </summary>
        public User CurrentUser
        {
            get
            {
                return currentUser;
            }

            set
            {
                SetProperty(ref currentUser, value);
            }
        }

        /// <summary>
        /// The current user settings value
        /// </summary>
        private UserSettings currentUserSettings;

        /// <summary>
        /// Gets or sets the current user settings value
        /// </summary>
        public UserSettings CurrentUserSettings
        {
            get
            {
                return currentUserSettings;
            }

            set
            {
                SetProperty(ref currentUserSettings, value);
            }
        }


        /// <summary>
        /// Is the device is bounded to current user
        /// </summary>
        private bool deviceIsBoundedToUser;

        /// <summary>
        /// Gets or sets the deviceIsBoundedToUser
        /// </summary>
        public bool DeviceIsBoundedToUser
        {
            get
            {
                return deviceIsBoundedToUser;
            }

            set
            {
                SetProperty(ref deviceIsBoundedToUser, value);
            }
        }

        /// <summary>
        /// Gets or sets the current carousel tab position
        /// </summary>
        private long carouselPosition;

        /// <summary>
        /// Gets or sets the current user value
        /// </summary>
        public long CarouselPosition
        {
            get
            {
                return carouselPosition;
            }

            set
            {
                SetProperty(ref carouselPosition, value);
            }
        }

        /// <summary>
        /// The user repository
        /// </summary>
        private readonly IUserRepository UserRepository;

        /// <summary>
        /// The user settings repository
        /// </summary>
        public readonly IUserSettingsRepository UserSettingsRepository;

        /// <summary>
        /// The glucosemeasure repository
        /// </summary>
        public readonly IGlucoseMeasureRepository GlucoseMeasureRepository;


        /// <summary>
        /// The device associated to the current user
        /// NULL if the current user does not have any device bounded
        /// </summary>
        public Models.Device device;

        /// <summary>
        /// Gets or sets the device
        /// </summary>
        public Models.Device Device
        {
            get
            {
                return device;
            }

            set
            {
                SetProperty(ref device, value);
            }
        }

        /// <summary>
        /// The datatemplate to use to build the device list Item
        /// </summary>
        public DataTemplate DeviceItemTemplate { get; set; }

        /// <summary>
        /// Is the bluetooth is scanning or pairing
        /// </summary>
        private bool isScanningOrPairingBlueTooth = false;

        /// <summary>
        /// Gets or sets Is the bluetooth is scanning or pairing
        /// </summary>
        public bool IsScanningOrPairingBlueTooth
        {
            get
            {
                return isScanningOrPairingBlueTooth;
            }

            set
            {
                SetProperty(ref isScanningOrPairingBlueTooth, value);
            }
        }

        /// <summary>
        /// The calibration source
        /// </summary>
        private float calibrationSourceValue = 0;

        /// <summary>
        /// Gets or sets Is the The calibration source value
        /// </summary>
        public float CalibrationSourceValue
        {
            get
            {
                return calibrationSourceValue;
            }

            set
            {
                SetProperty(ref calibrationSourceValue, value);
            }
        }

        /// <summary>
        /// The calibration revised
        /// </summary>
        private float calibrationRevisedValue = 0;

        /// <summary>
        /// Gets or sets Is the The calibration revised value
        /// </summary>
        public float CalibrationRevisedValue
        {
            get
            {
                return calibrationRevisedValue;
            }

            set
            {
                SetProperty(ref calibrationRevisedValue, value);
            }
        }

        /// <summary>
        /// Gets or sets the background measure service
        /// </summary>
        IMeasure MeasureService { get; set; }

        /// <summary>
        /// Gets the public RegisterDeviceCommand command
        /// </summary>
        private Command registerDeviceCommand;

        /// <summary>
        /// Gets or sets the RegisterDeviceCommand command
        /// </summary>
        public Command RegisterDeviceCommand
        {
            get { return registerDeviceCommand; }
            protected set { registerDeviceCommand = value; }
        }

        /// <summary>
        /// Gets the public UnregisterDeviceCommand command
        /// </summary>
        public ICommand UnregisterDeviceCommand { get; }

        /// <summary>
        /// Gets the public ScanDeviceCommand command
        /// </summary>
        public ICommand ScanDeviceCommand { get; }

        public ICommand UnfocusCalibrationCommand { get; }

        /// <summary>
        /// Is the bluetooth is scanning 
        /// </summary>
        private bool isScanningBlueTooth = false;

        /// <summary>
        /// Gets or sets Is the bluetooth is scanning 
        /// </summary>
        public bool IsScanningBlueTooth
        {
            get
            {
                return isScanningBlueTooth;
            }

            set
            {
                SetProperty(ref isScanningBlueTooth, value);
            }
        }

        /// <summary>
        /// Is Displaying scan results .
        /// </summary>
        private bool isDisplayingScanResult = false;

        /// <summary>
        /// Gets or sets the display result flag
        /// </summary>
        public bool IsDisplayingScanResult
        {
            get
            {
                return isDisplayingScanResult;
            }

            set
            {
                SetProperty(ref isDisplayingScanResult, value);
            }
        }


        /// <summary>
        /// List of the available devices
        /// </summary>
        public ObservableCollection<Models.Device> availableDevices;

        /// <summary>
        /// Gets the list of the available devices
        /// </summary>
        public ObservableCollection<Models.Device> AvailableDevices
        {
            get
            {
                return availableDevices;
            }
        }


        /// <summary>
        /// Is Bluetooth active
        /// </summary>
        private bool isBluetoothActive = true;

        /// <summary>
        /// Gets or sets the active
        /// </summary>
        public bool IsBluetoothActive
        {
            get
            {
                return isBluetoothActive;
            }

            set
            {
                SetProperty(ref isBluetoothActive, value);
            }
        }

        ///-----------------------------------------------------------
        /// Pairing properties

        /// <summary>
        /// Is the device is pairing
        /// </summary>
        private bool isPairingBlueTooth = false;

        /// <summary>
        /// Gets or sets Is the bluetooth is pairing 
        /// </summary>
        public bool IsPairingBlueTooth
        {
            get
            {
                return isPairingBlueTooth;
            }

            set
            {
                SetProperty(ref isPairingBlueTooth, value);
            }
        }

        /// <summary>
        /// Pairing first step flag. Indicates if the connection to the device is OK
        /// </summary>
        private bool hasConnected = false;

        /// <summary>
        /// Gets or set the device connection flag
        /// </summary>
        public bool HasConnected
        {
            get
            {
                return hasConnected;
            }
            set
            {
                SetProperty(ref hasConnected, value);
            }
        }

        /// <summary>
        /// Pairing second step. Indicates if the device is initialized
        /// </summary>
        private bool hasInitializedDevice = false;

        /// <summary>
        /// Gets or sets the intialize device flag
        /// </summary>
        public bool HasInitializedDevice
        {
            get
            {
                return hasInitializedDevice;
            }
            set
            {
                SetProperty(ref hasInitializedDevice, value);
            }
        }

        /// <summary>
        /// Pairing third step flag. Indicates if the subscription to the device is OK
        /// </summary>
        private bool hasSubscribed = false;

        /// <summary>
        /// Gets or sets the subsription flag indicator
        /// </summary>
        public bool HasSubscribed
        {
            get
            {
                return hasSubscribed;
            }
            set
            {
                SetProperty(ref hasSubscribed, value);
            }
        }

        /// <summary>
        /// Pairing fourth step. Indicates if the reading is OK 
        /// </summary>
        private bool hasReadFirstMeasure = false;

        /// <summary>
        /// Gets or sets the measure reading indicator.
        /// </summary>
        public bool HasReadFirstMeasure
        {
            get
            {
                return hasReadFirstMeasure;
            }
            set
            {
                SetProperty(ref hasReadFirstMeasure, value);
            }
        }

        private AppSettings settings;

        /// <summary>
        /// Gets or sets the private adjusted value
        /// </summary>
        private bool isFocused;

        /// <summary>
        /// Gets or sets the public adjusted value
        /// </summary>
        public bool IsFocused
        {
            get { return isFocused; }
            set { SetProperty(ref isFocused, value); }
        }

        /// <summary>
        /// Gets or sets the public focused max value button command
        /// </summary>
        public ICommand FocusAdjustedValueCommand { get; set; }

        /// <summary>
        /// Save temporary the adjusted old value for default value
        /// </summary>
        private float AjustedValueTmp;

        /// <summary>
        /// Initializes a new instance of the class <see cref="BluetoothPageViewModel"/>
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="navigationService"></param>
        /// <param name="userRepository"></param>
        /// <param name="userSettingsRepository"></param>
        /// <param name="sensorRepository"></param>
        /// <param name="bleService"></param>
        public BluetoothPageViewModel(IEventAggregator eventAggregator, INavigationService navigationService, IUserRepository userRepository,
            IUserSettingsRepository userSettingsRepository, ISensorRepository sensorRepository, IGlucoseMeasureRepository glucoseMeasureRepository, BluetoothScanService bleService, AppSettings settings) : base(navigationService)
        {
            this.eventAggregator = eventAggregator;
            this.settings = settings;

            InitializeDeviceItemTemplate();

            this.BleService = bleService;
            this.UserRepository = userRepository;
            this.UserSettingsRepository = userSettingsRepository;
            this.GlucoseMeasureRepository = glucoseMeasureRepository;
            this.CurrentUser = this.UserRepository.GetCurrentUser();
            this.CurrentUserSettings = this.UserSettingsRepository.GetCurrentUserSettings();
            this.DeviceIsBoundedToUser = this.CurrentUser.DeviceIsBounded;
            this.availableDevices = new ObservableCollection<Models.Device>();

            this.CalibrationSourceValue = this.CurrentUserSettings.CalibrationSourcedValue;
            this.CalibrationRevisedValue = this.CurrentUserSettings.CalibrationRevisedValue;
            this.IsBluetoothActive = this.BleService.Ble.IsOn;

            //Visual var initialization
            this.IsFocused = false;

            // The measure background service
            this.MeasureService = DependencyService.Get<IMeasure>(DependencyFetchTarget.GlobalInstance);

            // if current user has an affected device, we set the position to the 3rd tab
            if (DeviceIsBoundedToUser)
            {
                this.CarouselPosition = this.settings.BLUETOOTH_INDEX_PAGE_DEVICE;
            }
            else
            {
                this.CarouselPosition = this.settings.BLUETOOTH_INDEX_PAGE_DEFAULT;
            }

            // Unregister device command
            this.UnregisterDeviceCommand = new DelegateCommand(() => UnregisterDevice(true));

            // Register scan device command
            this.ScanDeviceCommand = new DelegateCommand(() => ScanForDevices());

            // Register the save calibration command
            this.UnfocusCalibrationCommand = new DelegateCommand(() =>
            {
                this.IsFocused = false;
                //Reset value
                if (CalibrationRevisedValue == 0f)
                {
                    CalibrationRevisedValue = AjustedValueTmp;
                }
                SaveCalibrationValues(true);
                App.Current.MainPage.DisplayAlert("", Utils.GetTranslation("BluetoothPage_CalibrationSaved"), Utils.GetTranslation("OK"));

                this.CalibrationSourceValue = CalibrationRevisedValue;

                Analytics.TrackEvent(AnalyticsEvent.BluetoothCalibrationChanged);
            });

            //
            this.FocusAdjustedValueCommand = new DelegateCommand(() =>
            {
                this.IsFocused = true;
                AjustedValueTmp = CalibrationRevisedValue;
                this.CalibrationRevisedValue = 0f;
            });

            // Register to events
            this.eventAggregator.GetEvent<BluetoothStateChangeEvent>().Subscribe((value) =>
            {

                switch (value)
                {
                    case Plugin.BLE.Abstractions.Contracts.BluetoothState.TurningOn:
                        this.IsBluetoothActive = true;
                        break;
                    case Plugin.BLE.Abstractions.Contracts.BluetoothState.TurningOff:
                        this.IsBluetoothActive = false;
                        break;
                }
            });

            this.eventAggregator.GetEvent<NotificationMeasureEvent>().Subscribe(value =>
            {
                this.CalibrationSourceValue = value.NewMeasure;
                this.CalibrationRevisedValue = value.NewMeasure;
            });
            // Subscribe to the ReconnectionBluetoothEvent
            this.eventAggregator.GetEvent<ReconnectBLEEvent>().Subscribe((value) =>
            {
                if (this.MeasureService == null)
                {
                    // intialize the measure service. We know he's awaken so we can get the dependancy
                    this.MeasureService = Xamarin.Forms.DependencyService.Get<IMeasure>(DependencyFetchTarget.GlobalInstance);
                }

                //*Analytics.TrackEvent(AnalyticsEvent.BluetoothReconnectionActivated);
                this.UnregisterDevice(true);

            });

        }

        public override void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);

            if (this.CurrentUser.DeviceIsBounded)
            {
                Device = new Models.Device(this.CurrentUser.DeviceId, this.CurrentUser.DeviceName);
            }
            else
            {
                Device = null;
            }
        }

        /// <summary>
        /// Save the calibration values
        /// </summary>
        private bool SaveCalibrationValues(bool spreadLastMeasure)
        {
            try
            {
                // Save the calibration in database 
                this.UserSettingsRepository.UpdateSensorCalibration(this.CurrentUserSettings, this.CalibrationSourceValue, this.CalibrationRevisedValue);

                this.CurrentUserSettings = this.UserSettingsRepository.GetCurrentUserSettings();

                // Get the last measure
                GlucoseMeasure lastMeasure = this.GlucoseMeasureRepository.GetLastMeasureByUser(this.CurrentUser.Id);

                if (lastMeasure != null)
                {

                    Log.Debug("BluetoothPageViewModel", $"SaveCalibrationValues : lastMeasure=[{lastMeasure.GlucoseLevelMGDL}], Offset=[{this.CurrentUserSettings.MeasureOffset}]");

                    // Update the last measure offset
                    this.GlucoseMeasureRepository.UpdateMeasureOffset(lastMeasure, this.CurrentUserSettings.MeasureOffset, this.settings);

                    // Spread the value
                    if (spreadLastMeasure)
                    {
                        GlucoseMeasure updated = this.GlucoseMeasureRepository.GetLastMeasureByUser(this.CurrentUser.Id);
                        this.eventAggregator.GetEvent<LastMeasureReceivedEvent>().Publish(this.CalibrationRevisedValue);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// This methods intialize the unregister actions
        /// </summary>
        private void UnregisterDevice(bool needToTrackEvent)
        {
            try
            {
                //Reset the current user settings 
                this.UserSettingsRepository.UpdateSensorCalibration(this.CurrentUserSettings, 0, 0);

                Models.Device device = new Models.Device(this.CurrentUser.DeviceId, this.CurrentUser.DeviceName);
                this.MeasureService.UnregisterDevice(device);

                this.UserRepository.UnregisterDevice(this.CurrentUser);

                this.CurrentUser = this.UserRepository.GetCurrentUser();
                DeviceIsBoundedToUser = false;
                this.CalibrationSourceValue = 0;
                this.CalibrationRevisedValue = 0;
                if (needToTrackEvent)
                {
                    Analytics.TrackEvent(AnalyticsEvent.BluetoothUnPaired);
                }
            }
            catch (Exception e)
            {
                Log.Error("BluetoothPageViewModel", $"UnregisterDevice : {e.Message}");
            }

        }

        /// <summary>
        /// This methods initialize the Scan actions
        /// </summary>
        private async void ScanForDevices()
        {
            ComputeScanningPropertiesIndicators(ScanningWizardState.Scanning);
            availableDevices.Clear();

            await Task.Delay(100);

            // when the timeout is reached, raise the event to stop the animation
            Xamarin.Forms.Device.StartTimer(TimeSpan.FromSeconds(this.settings.BLUETOOTH_SCAN_TIMEOUT), () =>
            {
                // Also stop the inprogress scanning
                this.BleService.StopScanning();

                // Scan for devices
                if (this.BleService.DiscoveredDevices != null)
                {
                    foreach (BluetoothDevice btDevice in this.BleService.DiscoveredDevices)
                    {
                        Models.Device device = new Models.Device(btDevice.Address, btDevice.Name);
                        if (!this.availableDevices.Contains(device))
                        {
                            this.availableDevices.Add(device);
                        }
                    }
                }

                if (this.availableDevices.Count == 0)
                {
                    App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_AlertNoDevicesTitle"), Utils.GetTranslation("BluetoothPage_AlertNoDevicesMessage"), Utils.GetTranslation("OK"));
                    ComputeScanningPropertiesIndicators(ScanningWizardState.Off);
                }
                else
                {
                    ComputeScanningPropertiesIndicators(ScanningWizardState.DisplayResult);
                }

                return false;
            });
            // Scan for devices
            BleService.ScanForDevices();
        }

        /// <summary>
        /// This methods executes the pairing actions
        /// </summary>
        private async Task<bool> PairDeviceAsync(Models.Device device)
        {
            ComputeScanningPropertiesIndicators(ScanningWizardState.Pairing);
            availableDevices.Clear();

            await Task.Delay(1500);
            this.HasConnected = await MeasureService.RegisterDeviceAsync(device);
            await Task.Delay(500);

            if (HasConnected)
            {
                this.HasInitializedDevice = await MeasureService.InitializeBLEServicesAsync(device, MiaoMiaoProtocol.UART_SERVICE_ID);

            }
            await Task.Delay(500);

            if (this.HasInitializedDevice)
            {
                this.HasSubscribed = await MeasureService.SubsrcibeCharacteristicAsync(MiaoMiaoProtocol.NRF_UART_TX);

            }
            await Task.Delay(500);

            if (this.HasSubscribed)
            {

                // MIAOMIAO AND BUBBLE PROTOCOL
                List<byte[]> resetPacket = new List<byte[]>();
                resetPacket.Add(FreeStyleLibreUtils.GenerateResetPacket(device.DeviceType));
                this.HasReadFirstMeasure = await MeasureService.WriteCharacteristicAsync(MiaoMiaoProtocol.NRF_UART_RX, resetPacket);


            }
            await Task.Delay(1500);

            bool HasReadCorrectData = true;
            if (this.HasReadFirstMeasure)
            {
                HasReadCorrectData = MeasureService.CurrentState() != Models.Enums.MeasureServiceState.REFUSED_DATA_THEN_WAIT;
            }



            return this.HasConnected && this.HasInitializedDevice && this.HasSubscribed && this.HasReadFirstMeasure && HasReadCorrectData;
        }

        /// <summary>
        /// Action raised when the user taps on the item pair button
        /// </summary>
        /// <param name="item">The item button tapped</param>
        private async void OnPairButtonItemTapped(object item)
        {
            var device = item as Models.Device;

            // Confirmation popup
            bool confirmed = await App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_ConfirmPairDeviceTitle"),
                Utils.GetTranslation("BluetoothPage_ConfirmPairDevice"),
                Utils.GetTranslation("YES"), Utils.GetTranslation("NO"));

            if (confirmed)
            {
                string title;
                string message;

                // try to pair to the device
                bool isPaired = await PairDeviceAsync(device);

                // not paired => no need to go further
                if (!isPaired)
                {
                    ComputeScanningPropertiesIndicators(ScanningWizardState.Off);
                    await App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_PairingErrorTitle"), Utils.GetTranslation("BluetoothPage_PairingErrorMessage"), Utils.GetTranslation("OK"));
                    return;
                }

                // try to register the device and save the first calibration values
                UserRepository.RegisterDevice(this.currentUser, device.Id, device.Name);
                DeviceIsBoundedToUser = true;
                this.CurrentUser = UserRepository.GetCurrentUser();

                var lastMeasure = this.GlucoseMeasureRepository.GetLastMeasureByUser(this.CurrentUser.Id);


                // no a good calibration measuer => no need to go further
                if (lastMeasure.GlucoseLevelMGDL <= 0)
                {
                    UnregisterDevice(false);
                    ComputeScanningPropertiesIndicators(ScanningWizardState.Off);
                    await App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_PairingErrorTitle"), Utils.GetTranslation("BluetoothPage_CalibrationPairingError"), Utils.GetTranslation("OK"));
                    return;
                }

                this.CalibrationSourceValue = lastMeasure.GlucoseLevelMGDL;
                this.CalibrationRevisedValue = lastMeasure.GlucoseLevelMGDL;
                bool calibrationSaved = SaveCalibrationValues(true);

                // calibration not saved => Error
                if (!calibrationSaved)
                {
                    ComputeScanningPropertiesIndicators(ScanningWizardState.Off);
                    await App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_PairingErrorTitle"), Utils.GetTranslation("BluetoothPage_PairingErrorMessage"), Utils.GetTranslation("OK"));
                    return;
                }

                // ok in any other cases
                Analytics.TrackEvent(AnalyticsEvent.BluetoothPaired);
                ComputeScanningPropertiesIndicators(ScanningWizardState.Off);
                await App.Current.MainPage.DisplayAlert(Utils.GetTranslation("BluetoothPage_PairingSuccessTitle"), Utils.GetTranslation("BluetoothPage_PairingSuccessMessage"), Utils.GetTranslation("OK"));
            }

        }

        /// <summary>
        /// Because of an issue on the CarouselView, ItemTemplate should not be defined inside the <ListView> tag 
        /// see(https://github.com/alexrainman/CarouselView/issues/408)
        //  So we defined it in the the ViewModel and bind it from the XAML
        /// </summary>
        private void InitializeDeviceItemTemplate()
        {
            this.DeviceItemTemplate = new DataTemplate(() =>
            {
                var frame = new Frame();
                frame.SetBinding(Frame.StyleProperty, "Bluetooth_Box");

                var grid = new Grid { HeightRequest = 50 };
                grid.RowDefinitions.Add(new RowDefinition { Height = 40 });

                var stack = new StackLayout();

                var idLabel = new Label();
                var nameLabel = new Label();
                var pairButton = new Button();

                idLabel.SetBinding(Label.TextProperty, "Id");
                idLabel.SetDynamicResource(Label.StyleProperty, "Bluetooth_DeviceLabelName");

                nameLabel.SetBinding(Label.TextProperty, "Name");
                nameLabel.SetDynamicResource(Label.StyleProperty, "Bluetooth_DeviceLabelId");

                pairButton.SetDynamicResource(Button.StyleProperty, "Bluetooth_DeviceActionUnpair");

                pairButton.Text = AppResources.BluetoothPage_Tab2_DeviceActionPair;
                pairButton.Command = new Command(OnPairButtonItemTapped);
                pairButton.SetBinding(Button.CommandParameterProperty, ".");

                stack.Children.Add(nameLabel);
                stack.Children.Add(idLabel);

                grid.Children.Add(stack);
                grid.Children.Add(pairButton);

                var stackParent = new StackLayout();
                stackParent.Children.Add(grid);

                return new ViewCell
                {
                    View = stackParent
                };

            });
        }
        /// <summary>
        /// This methods computes the scanning wizzard flags
        /// </summary>
        /// <param name="scanningState"></param>
        private void ComputeScanningPropertiesIndicators(ScanningWizardState scanningState)
        {
            switch (scanningState)
            {
                case ScanningWizardState.Off:
                    this.IsScanningBlueTooth = false;
                    this.IsDisplayingScanResult = false;
                    this.IsPairingBlueTooth = false;
                    this.IsScanningOrPairingBlueTooth = false;
                    this.HasConnected = false;
                    this.HasInitializedDevice = false;
                    this.HasSubscribed = false;
                    this.HasReadFirstMeasure = false;
                    break;

                case ScanningWizardState.Scanning:
                    this.IsScanningBlueTooth = true;
                    this.IsDisplayingScanResult = false;
                    this.IsPairingBlueTooth = false;
                    this.IsScanningOrPairingBlueTooth = true;
                    break;

                case ScanningWizardState.DisplayResult:
                    this.IsScanningBlueTooth = false;
                    this.IsDisplayingScanResult = true;
                    this.IsPairingBlueTooth = false;
                    this.IsScanningOrPairingBlueTooth = true;
                    break;

                case ScanningWizardState.Pairing:
                    this.IsScanningBlueTooth = false;
                    this.IsDisplayingScanResult = false;
                    this.IsPairingBlueTooth = true;
                    this.IsScanningOrPairingBlueTooth = true;
                    break;
            }
        }
    }
}


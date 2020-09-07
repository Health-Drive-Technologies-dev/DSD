using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using CarouselView.FormsPlugin.Android;
using Crossover.Bazarin.Permissions;
using HealthAndDrive.Droid.Services;
using HealthAndDrive.Services;
using Lottie.Forms.Droid;
using Prism;
using Prism.Ioc;
using Xamarin.Forms;
using Android.Provider;
using Android.Runtime;
using Prism.Events;
using HealthAndDrive.Events;
using Android.Widget;

namespace HealthAndDrive.Droid
{
    [Activity(Label = "DSD", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private const int ACTION_MANAGE_OVERLAY_PERMISSION_REQUEST_CODE = 5469;

        /// <summary>
        /// Bluetooth enable request code
        /// </summary>
        private const int PERMISSION_REQUEST_ENABLE_BLUETOOTH = 2;

        private Intent widgetServiceIntent;

        /// <summary>
        /// Coarse Location enable permission request
        /// </summary>
        private const int PERMISSION_REQUEST_COARSE_LOCATION = 456;

        /// <summary>
        /// Action Manage Overlay permission request 
        /// </summary>
        private const int PERMISSION_ACTION_MANAGE_OVERLAY_REQUEST_CODE = 5469;


        /// <summary>
        /// Widget Background service
        /// </summary>
        private Intent serviceWidget;

        /// <summary>
        /// Measure Service Background
        /// </summary>
        private Intent measureService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bundle"></param>
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App(new AndroidInitializer()));

            // Init the carousel view renderer
            CarouselViewRenderer.Init();

            // Init the animation view renderer
            AnimationViewRenderer.Init();

            // Check permissions for bluetooth or location
            CheckPermissions();

            DependencyService.Register<IMeasure, MeasureService>();
            DependencyService.Register<IFloatingWidgetService, MeasureWidgetService>();

            // Init the background service
            this.measureService = new Intent(this, typeof(MeasureService));
            StopService(this.measureService);
            StartService(this.measureService);

            this.serviceWidget = new Intent(this, typeof(MeasureWidgetService));
            StopService(this.serviceWidget);


            DependencyService.Get<IFloatingWidgetService>().IsWidgetEnabled += ((sender, _) =>
            {
                if (!Settings.CanDrawOverlays(this))
                {
                    Intent intent = new Intent(Settings.ActionManageOverlayPermission,
                        Uri.Parse("package:" + PackageName.ToString()));
                    StartActivityForResult(intent, PERMISSION_ACTION_MANAGE_OVERLAY_REQUEST_CODE);
                }
            });
        }

        /// <summary>
        /// Called when App started or resumed
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            if (serviceWidget != null)
            {
                StopService(serviceWidget);
            }
        }

        /// <summary>
        /// Called when app isn't focused
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            if (Settings.CanDrawOverlays(this) && (DependencyService.Get<IFloatingWidgetService>().IsEnable))
            {
                StartService(serviceWidget);
            }
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            (CrossPermissions.Current as PermissionsImplementation).OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        /// <summary>
        /// Checks the need permissions and 
        /// If it's not enabled
        /// </summary>
        private void CheckPermissions()
        {
            // BLUETOOTH
            // If bluetooth is not enabled, ask to enable it
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (!bluetoothAdapter.IsEnabled)
            {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, PERMISSION_REQUEST_ENABLE_BLUETOOTH);
            }


            // COARSE LOCATION
            // If location is not enabled, ask to enable it
            //if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            //{
            //    RequestPermissions(new string[] { Manifest.Permission.AccessCoarseLocation}, PERMISSION_REQUEST_COARSE_LOCATION);
            //}

        }

        /// <summary>
        /// Event fired when new intent is executed
        /// </summary>
        /// <param name="intent"></param>
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            if(intent.HasExtra("Ble_Reconnect"))
            {
                NotificationBLEClickedON(intent);
                return;
            }
            NotificationClickedOn(intent);
        }

        /// <summary>
        /// Action when the notification is clicked on
        /// </summary>
        /// <param name="intent"></param>
        private void NotificationClickedOn(Intent intent)
        {
            var navigationService = (Xamarin.Forms.Application.Current as App).PrismNavigation;
            Device.BeginInvokeOnMainThread(async () => await navigationService.NavigateAsync("RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=DrivePage"));
        }
        /// <summary>
        /// Action when the BLE notification is clicked on
        /// </summary>
        /// <param name="intent"></param>
        private void NotificationBLEClickedON(Intent intent)
        {
            IEventAggregator eventAggregator = (IEventAggregator)App.Current.Container.Resolve(typeof(IEventAggregator));
            eventAggregator.GetEvent<ReconnectBLEEvent>().Publish("");
            var navigationService = (Xamarin.Forms.Application.Current as App).PrismNavigation;
            Device.BeginInvokeOnMainThread(async () => await navigationService.NavigateAsync("RootPage/PrismNavigationPage/MainTabbedPage?selectedTab=BluetoothPage"));
            Toast.MakeText(this, "Veuillez re-initialiser votre lecteur", ToastLength.Long).Show();
            
        }

        protected override void OnDestroy()
        {
            //ContainerResolve
            // if(widget)
            System.Diagnostics.Debug.WriteLine($"OnDestroy raised");
            //StopService(this.measureService);
            base.OnDestroy();

            // Destroy Widget Service
            if (this.serviceWidget != null)
            {
                StopService(serviceWidget);
            }

            // Destroy Measure Service
            if (this.measureService != null)
            {
                StopService(this.measureService);
            }
        }
    }
    

    public class AndroidInitializer : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any platform specific implementations
            containerRegistry.RegisterSingleton<MeasureService>();

        }
    }
}


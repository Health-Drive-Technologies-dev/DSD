using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using HealthAndDrive.Events;
using HealthAndDrive.Events.Notifications;
using HealthAndDrive.Helpers;
using HealthAndDrive.Models;
using HealthAndDrive.Models.Enums;
using HealthAndDrive.Models.Protocol;
using HealthAndDrive.Protocol;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using Microsoft.AppCenter.Analytics;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Device = HealthAndDrive.Models.Device;

[assembly: Dependency(typeof(IMeasure))]
namespace HealthAndDrive.Droid.Services
{
    [Service]
    public class MeasureService : Service, IMeasure
    {
        /// <summary>
        /// Notification Channel ID
        /// </summary>
        private const string CHANNEL_ID = "HealthAndDrive.Channel";

        /// <summary>
        /// Notification Channel Name
        /// </summary>
        private const string CHANNEL_NAME = "HealthAndDrive.Measure";

        /// <summary>
        /// The measure notification Id
        /// </summary>
        private const int MEASURE_NOTIFICATION_ID = 10000;

        /// <summary>
        /// Alert Channel ID
        /// </summary>
        private const string CHANNEL_ALERT_ID = "HealthAndDrive.Channel.Alert";

        /// <summary>
        /// Alert Channel Name
        /// </summary>
        private const string CHANNEL_ALERT_NAME = "HealthAndDrive.Alert";

        /// <summary>
        /// The measure Alert Id
        /// </summary>
        private const int MEASURE_ALERT_ID = 10001;

        /// <summary>
        /// The tag for the log
        /// </summary>
        private readonly string LOG_TAG = nameof(MeasureService);

        /// <summary>
        /// Gets or setsthe device type
        /// </summary>
        private DeviceType DeviceTypeConnected { get; set; }

        /// <summary>
        /// Gets or sets the BLE
        /// </summary>
        private IBluetoothLE Ble { get; set; }

        /// <summary>
        /// Gets or sets the adapter
        /// </summary>
        private IAdapter Adapter { get; set; }

        /// <summary>
        /// The conneced device
        /// </summary>
        private IDevice ConnectedDevice { get; set; }

        /// <summary>
        /// The Service used to "talk" to our device
        /// </summary>
        IService UARTService { get; set; }

        /// <summary>
        /// Gets or setrs the current state for the service
        /// </summary>
        MeasureServiceState MeasureServiceState { get; set; }

        /// <summary>
        /// Get the last notifiaction measure
        /// </summary>
        private NotificationMeasure lastNotificationMeasure;


        /// <summary>
        /// Used for test
        /// </summary>
        private NotificationMeasure TestlastNotificationMeasure;

        /// <summary>
        /// Delay to wait for retrying reconnection
        /// </summary>
        private int Delay;

        /// <summary>
        /// Used for Age of lastnotification calculation
        /// </summary>
        private TimeSpan Temp;

        /// <summary>
        /// Info for battery for the bubble sensor
        /// </summary>
        private byte[] BubbleBatteryInfo;

        /// <summary>
        /// Info for Sensor Id
        /// </summary>
        private byte[] SensorIdInfo;

        /// <summary>
        /// Used For time convertion in secondes
        /// </summary>
        private double gapTimeInSeconde;

        IEventAggregator eventAggregator;

        private AppSettings appSettings;

        private MediaPlayer playerHypo3;
        private MediaPlayer playerHypo2;
        private MediaPlayer playerHypo1;
        private MediaPlayer playerHyper1;
        private MediaPlayer playerHyper2;
        private MediaPlayer playerHyper3;

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the current service instance
        /// </summary>
        /// <returns>The service</returns>
        public MeasureServiceState CurrentState()
        {
            return this.MeasureServiceState;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.MeasureServiceState = MeasureServiceState.OFF;
            this.Ble = CrossBluetoothLE.Current;
            this.Adapter = CrossBluetoothLE.Current.Adapter;

            // init the delay
            Delay = 0;

            //init des player MP3
            playerHypo3 = new MediaPlayer();
            playerHypo2 = new MediaPlayer();
            playerHypo1 = new MediaPlayer();
            playerHyper1 = new MediaPlayer();
            playerHyper2 = new MediaPlayer();
            playerHyper3 = new MediaPlayer();

            // creates the notification channel
            this.CreateNotificationChannel();
            this.CreateAlertChannel();

            // Show the Notifcation 
            this.StartService();

            IEventAggregator eventAggregator = (IEventAggregator)App.Current.Container.Resolve(typeof(IEventAggregator));

            appSettings = (AppSettings)App.Current.Container.Resolve(typeof(AppSettings));

            // subscribe to notification measure event (INCREASING, DECREASING, CONSTANT)
            eventAggregator.GetEvent<NotificationMeasureEvent>().Subscribe((notification) => { this.PushMeasureNotification(notification); });

            // subscribe to notification alert event
            eventAggregator.GetEvent<PushNotificationAlertEvent>().Subscribe((notification) => { this.PushAlertNotification(notification); });

            // Subscribe to the EndReadingEvent 
            // Possibly raised by the ReadingProtocol when the reading is over
            eventAggregator.GetEvent<EndReadingEvent>().Subscribe((value) => { this.MeasureServiceState = MeasureServiceState.WAITING_DATA; });
            eventAggregator.GetEvent<InitMeasureServiceEvent>().Publish("");

            // Init Reconnection Bluetooth process
            /*this.ReconnectBluetooth();*/

            // Exit event
            eventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(() => { RefreshWidget(); });


        }

        /// <summary>
        /// This method starts the service in background mode
        /// </summary>
        private void StartService()
        {
            // Create pending intent, mention the Activity which needs to be 
            //triggered when user clicks on notification(StopScript.class in this case)
            var resultIntent = new Intent(this, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.PreviousIsTop);
            resultIntent.AddCategory(Intent.CategoryLauncher);

            // Construct a back stack for cross-task navigation:
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddNextIntent(resultIntent);

            // Create the PendingIntent with the back stack:
            var resultPendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.CancelCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle($"Mesure de glycémie") // Set the title
                          .SetSmallIcon(Resource.Drawable.sigle_logo_splash_grey) // This is the icon to display
                          .SetContentText($"En attente de lecture") // the message to display.
                          .SetOnlyAlertOnce(true)
                          .SetTimeoutAfter(10000);

            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(MEASURE_NOTIFICATION_ID, builder.Build());


            // Enlist this instance of the service as a foreground service
            StartForeground(MEASURE_NOTIFICATION_ID, builder.Build());
        }

        /// <summary>
        /// This method initializes Bluetooth properties (just in case ...)
        /// </summary>
        private void InitializeBlueTooth()
        {
            if (this.Ble == null)
                this.Ble = CrossBluetoothLE.Current;

            if (this.Adapter == null)
                this.Adapter = CrossBluetoothLE.Current.Adapter;
        }

        /// <summary>
        /// This method creates a permanent notification channel to display information to the user
        /// </summary>
        private void CreateNotificationChannel()
        {
            if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Default)
            {
                Description = CHANNEL_NAME,
                LockscreenVisibility = NotificationVisibility.Secret,
                Name = CHANNEL_NAME,
                Importance = NotificationImportance.Low
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        /// <summary>
        /// This method creates a permanent notification channel to display information to the user
        /// </summary>
        private void CreateAlertChannel()
        {
            if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);

            if (notificationManager.GetNotificationChannel(CHANNEL_ALERT_ID) == null)
            {
                var channel = new NotificationChannel(CHANNEL_ALERT_ID, CHANNEL_ALERT_NAME, NotificationImportance.Default)
                {
                    Description = CHANNEL_ALERT_NAME,
                    LockscreenVisibility = NotificationVisibility.Secret,
                    Name = CHANNEL_ALERT_NAME,
                    Importance = NotificationImportance.Max
                };

                notificationManager.CreateNotificationChannel(channel);
            }
        }

        /// <summary>
        /// Register the bluetooth device and connects to it
        /// </summary>
        /// <param name="device">The device to register</param>
        /// <returns>True if the registration is successfull. False in any other case</returns>
        public async Task<bool> RegisterDeviceAsync(Device device)
        {
            try
            {
                Log.Debug(LOG_TAG, "RegisterDevice: trying to register device " + device.ToString());

                InitializeBlueTooth();

                // converting our device Id in Guid
                Guid? deviceGuid = Utils.AsGuid(device.Id, /*this.appSettings.DEVICE_GUID_FORMAT*/ "00000000-0000-0000-0000-{0}");
                if (!deviceGuid.HasValue)
                {
                    Log.Debug(LOG_TAG, "RegisterDeviceAsync: " + nameof(deviceGuid) + " is NULL");
                    return false;
                }

                // connection
                this.ConnectedDevice = await this.Adapter.ConnectToKnownDeviceAsync(deviceGuid.Value);

                if (this.ConnectedDevice == null)
                {
                    Log.Debug(LOG_TAG, "RegisterDeviceAsync: " + nameof(this.ConnectedDevice) + " is NULL");
                    return false;
                }

                Log.Debug(LOG_TAG, "RegisterDeviceAsync: Connection successfull");
                return true;
            }
            catch (DeviceConnectionException e)
            {
                DisposeAll();
                Log.Error(LOG_TAG, "RegisterDeviceAsync.DeviceConnectionException: " + e);
                return false;
            }
            catch (Exception e)
            {
                DisposeAll();
                Log.Error(LOG_TAG, "RegisterDeviceAsync.Exception: " + e);
                return false;
            }
        }

        /// <summary>
        /// Initializes a ble service within the device
        /// </summary>
        /// <param name="device">The device to initialize</param>
        /// <param name="serviceUUID">The service to initialize</param>
        /// <returns>True if the intialization is successfull. False in any other case</returns>
        public async Task<bool> InitializeBLEServicesAsync(Device device, string serviceUUID)
        {
            try
            {
                Log.Debug(LOG_TAG, "InitializeBLEServicesAsync : trying to initialize services for device " + device.ToString() + " service=[" + serviceUUID + "]");

                InitializeBlueTooth();

                // if ConnectedDevice is NULL we try to (re)Register the device
                if (this.ConnectedDevice == null)
                {
                    // if it's still not OK, then we return
                    if (!RegisterDeviceAsync(device).Result)
                    {
                        return false;
                    }
                }

                Guid? serviceGuid = Utils.AsGuid(serviceUUID);
                if (!serviceGuid.HasValue)
                {
                    Log.Debug(LOG_TAG, "InitializeBLEServicesAsync: " + nameof(serviceGuid) + " is NULL");
                    return false;
                }

                // Workaround (to let the Bluetooth GATT discover the services)
                await Task.Delay(1500);

                // connection
                this.UARTService = await this.ConnectedDevice.GetServiceAsync(serviceGuid.Value);

                // Workaround (to let the Bluetooth GATT discover the services)
                await Task.Delay(1500);

                if (this.UARTService == null)
                {
                    Log.Debug(LOG_TAG, "InitializeBLEServicesAsync: " + nameof(this.UARTService) + " is NULL");
                    return false;
                }
                this.DeviceTypeConnected = device.DeviceType;
                Log.Debug(LOG_TAG, $"InitializeBLEServicesAsync: Service connection successfull for {this.DeviceTypeConnected} device");
                return true;
            }
            catch (Exception e)
            {
                DisposeAll();
                Log.Error(LOG_TAG, "InitializeBLEServicesAsync: " + e);
                return false;
            }
        }

        /// <summary>
        /// Subscribes to a characteristic change.
        /// This method should be called (at least) after IMeasure.InitializeBLEServicesAsync 
        /// </summary>
        /// <param name="characteristicUUID">The characteristic UUID to subscribe</param>
        /// <returns>True if the subscription is successfull. False in any other case</returns>
        public async Task<bool> SubsrcibeCharacteristicAsync(string characteristicUUID)
        {
            try
            {
                Log.Debug(LOG_TAG, "SubsrcibeCharacteristicAsync : subscribing to characteristic=[" + characteristicUUID + "]");

                InitializeBlueTooth();

                Guid? characGuid = Utils.AsGuid(characteristicUUID);
                if (!characGuid.HasValue)
                {
                    Log.Debug(LOG_TAG, "SubsrcibeCharacteristicAsync: " + nameof(characGuid) + " is NULL");
                    return false;
                }

                // no need to go any further in this case
                if (this.UARTService == null)
                {
                    Log.Debug(LOG_TAG, "SubsrcibeCharacteristicAsync: cannot get characteristic. " + nameof(this.UARTService) + " is NULL");
                    return false;
                }

                // The subscription
                var txCharacteristic = await this.UARTService.GetCharacteristicAsync(characGuid.Value);
                txCharacteristic.ValueUpdated += OnCharacteristicValueChangeEvent;
                await txCharacteristic.StartUpdatesAsync();

                Log.Debug(LOG_TAG, "SubsrcibeCharacteristicAsync: subsription successful");
                return true;
            }
            catch (Exception e)
            {
                DisposeAll();
                Log.Error(LOG_TAG, "SubsrcibeCharacteristicAsync: " + e);
                return false;
            }
        }

        /// <summary>
        /// Read a characteristic and write an intializes
        /// </summary>
        /// <param name="characteristicUUID">The characteristic UUID to write</param>
        /// /// <param name="values">The values to write</param>
        /// <returns></returns>
        public async Task<bool> WriteCharacteristicAsync(String characteristicUUID, List<byte[]> values)
        {
            try
            {
                Log.Debug(LOG_TAG, "ReadDataAsync : reading characteristic=[" + characteristicUUID + "]");

                InitializeBlueTooth();

                Guid? characGuid = Utils.AsGuid(characteristicUUID);
                if (!characGuid.HasValue)
                {
                    Log.Debug(LOG_TAG, "ReadDataAsync: " + nameof(characGuid) + " is NULL");
                    return false;
                }

                if (this.UARTService == null)
                {
                    Log.Debug(LOG_TAG, "SubsrcibeCharacteristicAsync: cannot get characteristic. " + nameof(this.UARTService) + " is NULL");
                    return false;
                }

                // get RX Characteristic and Write values
                var rxCharacteristic = await this.UARTService.GetCharacteristicAsync(characGuid.Value);
                Log.Debug(LOG_TAG, "GetCharacteristic");
                foreach (byte[] value in values)
                {
                    await rxCharacteristic.WriteAsync(value);
                }
                Log.Debug(LOG_TAG, "Characteristic written");

                // the state evolves here
                this.MeasureServiceState = MeasureServiceState.WAITING_DATA;

                return true;
            }
            catch (Exception e)
            {
                DisposeAll();
                Log.Error(LOG_TAG, "WriteCharacteristicAsync: " + e);
                return false;
            }
        }

        /// <summary>
        /// Register the bluetooth device and disconnects from it
        /// </summary>
        /// <returns>True if the unregistration is successfull. False in any other case</returns>
        public void UnregisterDevice(Device device)
        {
            // try to disose all (only if the service state allows it)
            Xamarin.Forms.Device.StartTimer(TimeSpan.FromMinutes(/*AppSettings.MEASURE_SERVICE_RETRY_DEFAULT_TIME*/ 1), () =>
            {
                switch (this.MeasureServiceState)
                {
                    case MeasureServiceState.OFF:
                    case MeasureServiceState.REFUSED_DATA_THEN_WAIT:
                    case MeasureServiceState.WAITING_DATA:
                        this.DisposeAll();
                        return false;

                    // receiving data. We could not dispose for the moment 
                    case MeasureServiceState.RECEIVING_DATA:
                    default:
                        Log.Debug(LOG_TAG, GetType() + ".UnregisterDevice is reported because data is being received");
                        return true;
                }
            });
        }

        /// <summary>
        /// Event raised when the bluettoth device notify a value change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnCharacteristicValueChangeEvent(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            if (this.eventAggregator == null)
                eventAggregator = (IEventAggregator)App.Current.Container.Resolve(typeof(IEventAggregator));

            //Bubble, miaomiao in function of the DeviceTypeConnected connected we have a different response
            DialogBehaviourHolder beahaviour = FreeStyleLibreUtils.RespondToPacketBehaviour(e.Characteristic.Value, this.DeviceTypeConnected);
            Log.Debug(LOG_TAG, $"beahaviour.Response = {beahaviour.ResponseType} and DeviceTypeConnected = {this.DeviceTypeConnected}" );

            switch (beahaviour.ResponseType)
            {
                case PacketResponseType.Accept:
                    if (this.DeviceTypeConnected == DeviceType.MiaoMiao)
                    {
                        Log.Debug(LOG_TAG, $"MiaoMiao protocol called");
                        eventAggregator.GetEvent<MeasureChangeEventMiaoMiao>().Publish(beahaviour.ReceivedData);
                    }
                    else
                    {
                        
                        Log.Debug(LOG_TAG, $"Bubble protocol called");

                        //We concatenate the battery level and SensorId info into the data in order to push to extract the data
                        byte[] BatteryAndReceivedData = new byte[beahaviour.ReceivedData.Length + this.SensorIdInfo.Length 
                                                                + this.BubbleBatteryInfo.Length];
                        Buffer.BlockCopy(this.BubbleBatteryInfo, 0, BatteryAndReceivedData, 0, this.BubbleBatteryInfo.Length);
                        Buffer.BlockCopy(this.SensorIdInfo, 0, BatteryAndReceivedData, this.BubbleBatteryInfo.Length, this.SensorIdInfo.Length);
                        Buffer.BlockCopy(beahaviour.ReceivedData, 0, BatteryAndReceivedData,
                            this.BubbleBatteryInfo.Length + this.SensorIdInfo.Length,
                            beahaviour.ReceivedData.Length);


                        Log.Debug(LOG_TAG, $"The battery is : {Utils.ByteArrayToString(this.BubbleBatteryInfo)}, " +
                            $"with a length of {this.BubbleBatteryInfo.Length} and " +
                            $"the length of the ReceivedData is {beahaviour.ReceivedData.Length}," +
                            $"{Utils.ByteArrayToString(this.SensorIdInfo)} with a length of {this.SensorIdInfo.Length}");
                        eventAggregator.GetEvent<MeasureChangeEventBubble>().Publish(BatteryAndReceivedData);
                    }
                    this.MeasureServiceState = MeasureServiceState.RECEIVING_DATA;
                    break;

                case PacketResponseType.AnswerBack:
                    this.BubbleBatteryInfo = beahaviour.BatteryInfo;
                    WriteCharacteristicAsync(MiaoMiaoProtocol.NRF_UART_RX, beahaviour.Response);
                    break;

                //Further evolution should be to have an error count to retry 2 or 3 time before reconnecting
                case PacketResponseType.Refuse:
                    this.MeasureServiceState = MeasureServiceState.REFUSED_DATA_THEN_WAIT;
                    break;

                case PacketResponseType.Ignore:
                    if (beahaviour.SensorIdInfo != null)
                    {
                        this.SensorIdInfo = beahaviour.SensorIdInfo;
                    }
                    break;
                default:
                    this.MeasureServiceState = MeasureServiceState.WAITING_DATA;
                    break;

            }
        }

        /// <summary>
        /// Disposes and disconnects from all
        /// </summary>
        private void DisposeAll()
        {
            Log.Debug(LOG_TAG, "DisposeAll: called");
            if (this.UARTService != null)
            {
                this.UARTService.Dispose();
            }

            if (this.Adapter != null && this.ConnectedDevice != null)
            {
                this.Adapter.DisconnectDeviceAsync(this.ConnectedDevice);
                this.DeviceTypeConnected = new DeviceType();
                this.BubbleBatteryInfo = null;
            }

        }

        public void RefreshWidget()
        {
            if (lastNotificationMeasure != null)
            {
                NotificationMeasure cloned = new NotificationMeasure(lastNotificationMeasure);
                cloned.IsAlert = false;

                IEventAggregator eventAggregator = (IEventAggregator)App.Current.Container.Resolve(typeof(IEventAggregator));
                eventAggregator.GetEvent<NotificationMeasureEvent>().Publish(cloned);
            }
        }

        /// <summary>
        /// Pushes a notification measure for the user 
        /// </summary>
        /// <param name="message">The message to notify</param>
        /// <param name="trend">The trending for the measures</param>
        public void PushMeasureNotification(NotificationMeasure message)
        {

            // Create pending intent, mention the Activity which needs to be 
            //triggered when user clicks on notification(StopScript.class in this case)
            var resultIntent = new Intent(this, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.PreviousIsTop);
            //resultIntent.SetAction(Intent.ActionMain);
            resultIntent.AddCategory(Intent.CategoryLauncher);

            // Construct a back stack for cross-task navigation:
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddNextIntent(resultIntent);

            lastNotificationMeasure = message;

            // Create the PendingIntent with the back stack:
            var resultPendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.CancelCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle("Mesure de glycémie") // Set the title
                          .SetSmallIcon(Resource.Drawable.sigle_logo_splash_grey) // This is the icon to display
                          .SetContentText($"{message.NotificationMessage}"); // the message to display.

            switch (message.MeasureTrend)
            {
                case MeasureTrend.IncreasingHeavy:
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_arrows_top_blue));
                    break;
                case MeasureTrend.Increasing:
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_arrows_top_mid_blue));
                    break;
                case MeasureTrend.Constant:
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_arrows_straight_blue));
                    break;
                case MeasureTrend.Decreasing:
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_arrows_bot_mid_blue));
                    break;
                case MeasureTrend.DecreasingHeavy:
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_arrows_bottom_blue));
                    break;
            }

            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(MEASURE_NOTIFICATION_ID, builder.Build());

            if (message.IsAlert)
            {
                PhysicalNotification(MeasureZoneHelper.ComputeMeasureZone(message.NewMeasure, message.MinimumGlucoseTreshold, message.MaximumGlucoseTreshold));
            }
        }

        /// <summary>
        /// Pushes a notification alert for the user
        /// </summary>
        /// <param name="message">The message to notify</param>
        public void PushAlertNotification(NotificationMeasure message)
        {
            // Create pending intent, mention the Activity which needs to be 
            //triggered when user clicks on notification(StopScript.class in this case)
            var resultIntent = new Intent(this, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.PreviousIsTop);
            //resultIntent.SetAction(Intent.ActionMain);
            resultIntent.AddCategory(Intent.CategoryLauncher);

            // Construct a back stack for cross-task navigation:
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddNextIntent(resultIntent);

            // Create the PendingIntent with the back stack:
            var resultPendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.CancelCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this, CHANNEL_ALERT_ID)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle("Alerte glycémie") // Set the title
                          .SetSmallIcon(Resource.Drawable.sigle_logo_splash_grey) // This is the icon to display
                          .SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_alerte_orange))
                          .SetContentText($"{message.NotificationMessage}"); // the message to display.

            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            CreateAlertChannel();
            notificationManager.Notify(MEASURE_ALERT_ID, builder.Build());
        }

        private void PhysicalNotification(ValueKindEnum kind)
        {
            Vibrator vibrator = (Vibrator)this.ApplicationContext.GetSystemService(Context.VibratorService);
            vibrator.Cancel();
            long[] pattern1 = { 50, 500, 50, 500, 50, 500, 50, 500 };
            long[] pattern2 = { 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500 };
            long[] pattern3 = { 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500, 50, 500 };

            switch (kind)
            {
                case ValueKindEnum.Low2:
                    playerHypo3.Reset();
                    playerHypo3.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hypo3));
                    playerHypo3.Prepare();
                    playerHypo3.Start();
                    vibrator.Vibrate(pattern3, -1);
                    break;
                case ValueKindEnum.Low1:
                    playerHypo2.Reset();
                    playerHypo2.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hypo2));
                    playerHypo2.Prepare();
                    playerHypo2.Start();
                    vibrator.Vibrate(pattern2, -1);
                    break;
                case ValueKindEnum.Low:
                    playerHypo1.Reset();
                    playerHypo1.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hypo1));
                    playerHypo1.Prepare();
                    playerHypo1.Start();
                    vibrator.Vibrate(pattern1, -1);
                    break;
                case ValueKindEnum.Normal:
                    break;
                case ValueKindEnum.High:
                    playerHyper1.Reset();
                    playerHyper1.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hyper1));
                    playerHyper1.Prepare();
                    playerHyper1.Start();
                    vibrator.Vibrate(pattern1, -1);
                    break;
                case ValueKindEnum.High1:
                    playerHyper2.Reset();
                    playerHyper2.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hyper2));
                    playerHyper2.Prepare();
                    playerHyper2.Start();
                    vibrator.Vibrate(pattern2, -1);
                    break;
                case ValueKindEnum.High2:
                    playerHyper3.Reset();
                    playerHyper3.SetDataSource(this, Android.Net.Uri.Parse("android.resource://com.healthanddrive.handd/" + Resource.Raw.hyper3));
                    playerHyper3.Prepare();
                    playerHyper3.Start();
                    vibrator.Vibrate(pattern3, -1);
                    break;
                default:
                    break;
            }
        }

        public override void OnDestroy()
        {
            Log.Debug(LOG_TAG, "OnDestroy: called");
            base.OnDestroy();
        }


        /// <summary>
        /// Fonction Bluetooth reconnection
        /// Wait a certain delay and check the state of connection
        /// if ( not connectet and are not reading data ) then reconnectet the last connected device
        /// </summary>
        public async void ReconnectBluetooth()
        {

            // Show fisrt step 
            if (Delay == 0)
            {

                Log.Debug(LOG_TAG, "!!!!!!!!------------- In ReconnectionBluetooth Fonction -------------------!!!!!!");
                this.TestlastNotificationMeasure = new NotificationMeasure();
                this.TestlastNotificationMeasure.NotificationMeasureDate = new DateTimeOffset(2020, 07, 29, 9, 10, 0, TimeSpan.Zero); //The date is in UTC time

                // Show values
                Log.Debug(LOG_TAG, $"Last Notification Measure = {this.TestlastNotificationMeasure.NotificationMeasureDate.ToString()} -------------------!!!!!! ");

                Log.Debug(LOG_TAG, $"Now time = {DateTime.UtcNow} -------------------!!!!!! ");

                // Check the age of the last measure
                Temp = DateTime.UtcNow - TestlastNotificationMeasure.NotificationMeasureDate;
                Log.Debug(LOG_TAG, $"Age = {Temp} -------------------!!!!!! ");


                gapTimeInSeconde = Temp.TotalSeconds;

                Log.Debug(LOG_TAG, $"!!!!!!!!------------- Gap Time in seconds(s) = {gapTimeInSeconde} -------------------!!!!!! ");


            }
            if (Delay == 30 || Delay == 60 || Delay == 90)
            {
                Log.Debug(LOG_TAG, "!!!!!!!!------------- In ReconnectionBluetooth Fonction -------------------!!!!!!");
                this.TestlastNotificationMeasure = new NotificationMeasure();
                this.TestlastNotificationMeasure.NotificationMeasureDate = new DateTimeOffset(2020, 07, 29, 9, 10, 0, TimeSpan.Zero);

                // Show values
                Log.Debug(LOG_TAG, $"Last Notification Measure = {this.TestlastNotificationMeasure.NotificationMeasureDate.ToString()} -------------------!!!!!! ");

                Log.Debug(LOG_TAG, $"Now time = {DateTime.UtcNow} -------------------!!!!!! ");

                // Check the age of the last measure
                Temp = DateTime.UtcNow - TestlastNotificationMeasure.NotificationMeasureDate;
                Log.Debug(LOG_TAG, $"Age = {Temp} -------------------!!!!!! ");


                gapTimeInSeconde = Temp.TotalSeconds;

                Log.Debug(LOG_TAG, $"!!!!!!!!------------- Gap Time in seconds(s) = {gapTimeInSeconde} -------------------!!!!!! ");
            }

            // Check delay
            if ((Delay >= this.appSettings.RetryBluetoothDelay) && (this.MeasureServiceState == MeasureServiceState.WAITING_DATA))
            {

                // Re init Delay
                Delay = 0;

                // Retry connection
                if (this.lastNotificationMeasure != null)
                {
                    // Check the age of the last measure
                    this.Temp = DateTime.UtcNow - this.lastNotificationMeasure.NotificationMeasureDate;

                    Log.Debug(LOG_TAG, $"!!!!!!!!------------- The last Measure Date is {this.lastNotificationMeasure.NotificationMeasureDate.ToString()} -------------------!!!!!! ");
                    this.gapTimeInSeconde = this.Temp.TotalSeconds;

                    Log.Debug(LOG_TAG, $"!!!!!!!!------------- Gap Time in seconds(s) between {nameof(DateTimeOffset.UtcNow)} and Last Measure Date = {this.gapTimeInSeconde} -------------------!!!!!! ");

                    // Check the age of the last measure
                    // if most than 15 minutes reconnect the last device
                    if (this.gapTimeInSeconde >= this.appSettings.RetryBluetoothDelay)
                    {
                        // Publish event Reconnection Bluetooth
                        this.eventAggregator.GetEvent<ReconnectBluetoothEvent>().Publish("");

                        // For debug
                        Log.Debug(LOG_TAG, "!!!!!!!!------------- Event Reconnection Bluetooth published  -------------------!!!!!!");


                    }


                }
                else // Notification null until 15 minutes problem !!! 
                {
                    // reconnection
                    this.eventAggregator.GetEvent<ReconnectBluetoothEvent>().Publish("");

                    // For debug
                    Log.Debug(LOG_TAG, "!!!!!!!!------------- Event Reconnection Bluetooth published  -------------------!!!!!!");

                }
            }
            else
            {
                Delay++;

            }

            // Task Delay
            await Task.Delay(this.appSettings.MEASURE_SERVICE_RETRY_DEFAULT_TIME * 1000).ContinueWith(t => ReconnectBluetooth());

        }
    }
}
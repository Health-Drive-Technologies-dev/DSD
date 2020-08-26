using Android.Util;
using HealthAndDrive.Events;
using HealthAndDrive.Events.Notifications;
using HealthAndDrive.Helpers;
using HealthAndDrive.Models;
using HealthAndDrive.Protocol;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Tools;
using Microsoft.AppCenter.Analytics;
using Plugin.BLE.Abstractions.Exceptions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HealthAndDrive.Services
{
    /// <summary>
    /// This service manages the logic used to process the measurement data (Database saving, User updates)
    /// </summary>
    public class GlucoseService
    {
        /// <summary>
        /// The tag for the log
        /// </summary>
        private readonly string LOG_TAG = nameof(GlucoseService);

        /// <summary>
        /// The user repository
        /// </summary>
        private readonly IUserRepository userRepository;

        /// <summary>
        /// The userSettingsRepository repository
        /// </summary>
        private readonly IUserSettingsRepository userSettingsRepository;

        /// <summary>
        /// The sensor repository
        /// </summary>
        private readonly ISensorRepository sensorRepository;

        /// <summary>
        ///  The GlucoseMeasure repository
        /// </summary>
        private readonly IGlucoseMeasureRepository glucoseMeasureRepository;

        /// <summary>
        /// The IEvent aggregator
        /// </summary>
        private readonly IEventAggregator eventAggregator;

        /// <summary>
        /// The IMeasure service (In the android project)
        /// </summary>
        private IMeasure measureService;

        /// <summary>
        /// If the method WakeUpMeasureServiceAsync() is already calling, the bool is true
        /// </summary>
        public bool IsWakingUpDevice { get; set; }

        private float? precedingMeasure { get; set; } = null;

        /// <summary>
        /// Application settings
        /// </summary>
        private AppSettings appSettings;

        public GlucoseService(IEventAggregator eventAggregator, IUserRepository userRepository, IUserSettingsRepository userSettingsRepository,
            ISensorRepository sensorRepository, IGlucoseMeasureRepository GlucoseMeasureRepository, AppSettings settings)
        {
            this.eventAggregator = eventAggregator;
            this.userRepository = userRepository;
            this.userSettingsRepository = userSettingsRepository;
            this.sensorRepository = sensorRepository;
            this.glucoseMeasureRepository = GlucoseMeasureRepository;
            this.appSettings = settings;
            this.IsWakingUpDevice = false;

            // subscription to the init measureServiceEvent
            // This event is possibly raised by the background service after its full initialization
            this.eventAggregator.GetEvent<InitMeasureServiceEvent>().Subscribe(async (value) =>
            {

                // intialize the measure service. We know he's awaken so we can get the dependancy
                this.measureService = Xamarin.Forms.DependencyService.Get<IMeasure>(DependencyFetchTarget.GlobalInstance);

                // then we awake the device
                await WakeUpMeasureServiceAsync();
                
            });

            // Subscribe to the ReconnectionBluetoothEvent
            this.eventAggregator.GetEvent<ReconnectBLEEvent>().Subscribe((value) =>
            {
                /*Analytics.TrackEvent(AnalyticsEvent.BluetoothReconnectionActivated);*/
                if(this.IsWakingUpDevice != true)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() => { //To be tested !
                        WakeUpMeasureServiceAsync();
                    });
                }
                // awake device
                
            });

            // subscription to the LastMeasureReceivedEvent
            // This event could be raised by the Hidden page for the purpose of simulated data
            this.eventAggregator.GetEvent<LastMeasureReceivedEvent>().Subscribe((value) =>
            {
                SpreadLastMeasure(value);
                ProcessAveragesValues();
            }
            );
        }

        /// <summary>
        /// This method processes the reading data session
        /// </summary>
        /// <param name="readingSession">The values of the reading session</param>
        public void HandleReadingSession(MeasureReadingSession readingSession)
        {
            Log.Debug(LOG_TAG, GetType() + ".ReportReadingSession:" + readingSession.ToString());

            if (!Utils.IsValidTDE(this.appSettings.TDE))
            {
                return;
            }

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                ProcessSensorData(readingSession);

                ProcessMeasuresData(this.userRepository.GetCurrentUser().Id, readingSession);

                ProcessAveragesValues();

                Analytics.TrackEvent(AnalyticsEvent.GlucoseMeasured);
            });
        }

        /// <summary>
        /// This method processes the sensor data
        /// </summary>
        /// <param name="readingSession">The reading session</param>
        public void ProcessSensorData(MeasureReadingSession readingSession)
        {
            if (readingSession == null)
            {
                Log.Debug(LOG_TAG, GetType() + ".ProcessSensorData: readingSession is NULL");
                return;
            }

            Sensor currentSensor;
            User currentUser = this.userRepository.GetCurrentUser();
            this.userRepository.ChangeName(currentUser, "Josoa");
            currentUser = this.userRepository.GetCurrentUser();

            // if the sensor extracted from the reading session is New
            if (!this.sensorRepository.Exists(readingSession.SensorId))
            {
                Log.Debug(LOG_TAG, GetType() + ".ProcessSensorData: register new sensor. readingSession.SensorId=[" + readingSession.SensorId + "]");

                // first, find the current sensor
                currentSensor = this.sensorRepository.GetCurrentSensor();

                // if the sensor exits, we "remove" it
                if (currentSensor != null)
                {
                    Log.Debug(LOG_TAG, GetType() + ".ProcessSensorData: removing sensor=[" + currentSensor.Id + "]");
                    this.sensorRepository.Remove(currentSensor);
                }

                // then, we register the new sensor
                Sensor toRegister = new Sensor();
                toRegister.Id = readingSession.SensorId;
                this.sensorRepository.RegisterNewSensor(toRegister);
                currentSensor = this.sensorRepository.GetCurrentSensor();
            }
            else
            {
                currentSensor = this.sensorRepository.GetCurrentSensor();
                Log.Debug(LOG_TAG, GetType() + ".ProcessSensorData: updating existing sensor=[" + currentSensor.Id + "]");
            }

            GlucoseMeasure lastGlucoseMeasure = readingSession.CurrentMeasure;

            //Update the device battery level
            this.userRepository.UpdateDeviceBatteryPercentage(currentUser, readingSession.BatteryLevel);
            //Update the last value checked time
            this.userRepository.UpdateLastValueTimeStamp(currentUser, lastGlucoseMeasure.RealDateTimeOffset);
            //Send notification to update battery and data in drive page
            this.eventAggregator.GetEvent<DrivePageDataUpdateEvent>().Publish();

        }

        /// <summary>
        /// This method processes the glucose measures 
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <param name="freshMeasures">The list of the fresh measures</param>
        public void ProcessMeasuresData(string userId, MeasureReadingSession readingSession)
        {
            var currentUser = this.userRepository.GetCurrentUser();
            var userSettings = this.userSettingsRepository.GetCurrentUserSettings();
            bool isCreation = false;


            // first, we check the age of the sensor
            var currentSensor = this.sensorRepository.GetCurrentSensor();
            long newSensorAge = readingSession.TrendMeasures[readingSession.TrendMeasures.Count - 1].SensorTime;
            long savedSensorAge = currentSensor.Age;

            if (newSensorAge > savedSensorAge)
            {
                Log.Debug(LOG_TAG, GetType() + ".ProcessMeasuresData: Sensor age has advanced. Try to insert measures" + savedSensorAge);
                savedSensorAge = newSensorAge;
                this.sensorRepository.UpdateSensorAge(currentSensor, savedSensorAge);
            }
            else if (newSensorAge == savedSensorAge)
            {
                Log.Debug(LOG_TAG, GetType() + ".ProcessMeasuresData: Sensor age has not advanced. sensorAge=[" + savedSensorAge + "]");
                return; // do not try to insert again
            }
            else
            {
                Log.Debug(LOG_TAG, GetType() + ".ProcessMeasuresData: Sensor age has gone backwards!!! " + savedSensorAge);
                savedSensorAge = newSensorAge;
                this.sensorRepository.UpdateSensorAge(currentSensor, savedSensorAge);
            }

            GlucoseMeasure lastInsertedMeasure = this.glucoseMeasureRepository.GetLastMeasureByUser(userId);

            // create each GlucoseMeasure
            foreach (GlucoseMeasure measure in readingSession.GetAllMeasures())
            {
                if (lastInsertedMeasure != null && lastInsertedMeasure.Timestamp > measure.Timestamp)
                    continue;

                // possibly, the trend measures interval could be one minute
                // we only want 5 minutes interval
                if (measure.SensorTime % appSettings.MEASURE_NOTIFICATION_INTERVAL != 0)
                    continue;

                isCreation = true;


                // creation first
                measure.Id = Guid.NewGuid().ToString();
                this.glucoseMeasureRepository.Create(measure, userId);

                // then updates
                // measureoffset
                this.glucoseMeasureRepository.UpdateMeasureOffset(measure, userSettings.MeasureOffset, appSettings);

                // In The medical zone 
                //medical zone
                this.glucoseMeasureRepository.ChangeIsInTheMedicalZone(measure, InTheMedicalZone(measure));

                Log.Debug(LOG_TAG, GetType() + ".ProcessMeasuresData: Creating measure " + measure.ToString());
            }

            GlucoseMeasure lastGlucoseMeasure = readingSession.CurrentMeasure;

            //Last glucose measure additionnal setup
            if (lastGlucoseMeasure.Id == null)
            {
                //Add to the last glucose measure an generatedId
                lastGlucoseMeasure.Id = Guid.NewGuid().ToString();

                //Save the last glucose measure into the database
                this.glucoseMeasureRepository.Create(lastGlucoseMeasure, userId);
            }

            if(lastGlucoseMeasure.UserId == null)
            { 
                //Add to the last glucose measure an userId
                this.glucoseMeasureRepository.ChangeUserId(lastGlucoseMeasure, userId);
            }

            //Check if the measure is in the medical zone
            this.glucoseMeasureRepository.ChangeIsInTheMedicalZone(lastGlucoseMeasure, InTheMedicalZone(lastGlucoseMeasure));
            
            //Apply the offset
            lastGlucoseMeasure = this.glucoseMeasureRepository.UpdateMeasureOffset(lastGlucoseMeasure, userSettings.MeasureOffset, appSettings);

            Log.Debug(LOG_TAG, GetType() + ".ProcessMeasuresData: lastMeasure = " + lastGlucoseMeasure.ToString());
            var toPublish = this.appSettings.HANDLE_MGDL_MEASURE ? lastGlucoseMeasure.GlucoseLevelMGDL : lastGlucoseMeasure.GlucoseLevelMMOL;

            // Spread the current last glucose measure
            this.eventAggregator.GetEvent<LastMeasureReceivedEvent>().Publish(toPublish);
        }



        /// <summary>
        /// Spreads the last measure on the application (database, UI, alerts)
        /// </summary>
        /// <param name="lastMeasure"></param>
        private void SpreadLastMeasure(float lastMeasure)
        {
            var currentUser = this.userRepository.GetCurrentUser();
            var userSettings = this.userSettingsRepository.GetCurrentUserSettings();
            long countExistingMesures = this.glucoseMeasureRepository.CountByUser(currentUser.Id);
            bool isFirstReading = countExistingMesures == 0;

            // Last measure date
            DateTimeOffset lastMeasureDate = this.glucoseMeasureRepository.GetLastMeasureByUser(currentUser.Id).RealDateTimeOffset;


            // 1 - Driving mode
            // NOTE : We only handle MGDL Measure for the H&D MVP
            this.userRepository.ChangeCurrentMeasure(currentUser, lastMeasure);

            // 2 The notification channel 
            var notification = new NotificationMeasure();
            notification.NotificationMessage = string.Format(Utils.GetTranslation("NotificationMessageCurrentMeasure"), HiLowValueHelper.NewMeasureToString(lastMeasure));
            notification.NewMeasure = lastMeasure;
            notification.MinimumGlucoseTreshold = userSettings.MinimumGlucoseTreshold;
            notification.MaximumGlucoseTreshold = userSettings.MaximumGlucoseTreshold;
            notification.IsAlert = userRepository.GetCurrentUser().IsAlert;
            notification.NotificationMeasureDate = lastMeasureDate;


            //Current Trend management //
            GlucoseMeasure pastFifteenMinMeasure = glucoseMeasureRepository.GetPastFifteenMinutesMeasureByUser(currentUser.Id);
            float? pastFifteenMinMeasureValue = null;
            if (pastFifteenMinMeasure != null)
            {
                pastFifteenMinMeasureValue = pastFifteenMinMeasure.GlucoseLevelMGDL;
            }

            //Trend management : Builder //
            switch (TrendHelper.ValuesToTypeTrend(lastMeasure, pastFifteenMinMeasureValue))
            {
                case MeasureTrend.IncreasingHeavy:
                    notification.MeasureTrend = MeasureTrend.IncreasingHeavy;
                    break;
                case MeasureTrend.Increasing:
                    notification.MeasureTrend = MeasureTrend.Increasing;
                    break;
                case MeasureTrend.Constant:
                    notification.MeasureTrend = MeasureTrend.Constant;
                    break;
                case MeasureTrend.Decreasing:
                    notification.MeasureTrend = MeasureTrend.Decreasing;
                    break;
                case MeasureTrend.DecreasingHeavy:
                    notification.MeasureTrend = MeasureTrend.DecreasingHeavy;
                    break;
                default:
                    notification.MeasureTrend = MeasureTrend.None;
                    break;
            }

            //Trend management : Notifications //
            //Save the measure trend in the user data
            this.userRepository.UpdateMeasureTrend(currentUser, notification.MeasureTrend);
            //Send the measure trend event to the drive page
            this.eventAggregator.GetEvent<TrendEvent>().Publish(notification.MeasureTrend);
            //Send the measure trend event to the notification
            this.eventAggregator.GetEvent<NotificationMeasureEvent>().Publish(notification);
            Log.Debug(LOG_TAG, $"Notification measure = {notification.NewMeasure}");

            // 3 - Alerts raised to the user ? 
            if (userRepository.GetCurrentUser().IsAlert)
            {
                if (lastMeasure > userSettings.MaximumGlucoseTreshold || lastMeasure < userSettings.MinimumGlucoseTreshold)
                {
                    notification.NotificationMessage = string.Format(Utils.GetTranslation("NotificationMessageAlertGlycemia"), HiLowValueHelper.NewMeasureToString(lastMeasure));
                    this.eventAggregator.GetEvent<PushNotificationAlertEvent>().Publish(notification);
                }
            }

            this.precedingMeasure = lastMeasure;
        }

        /// <summary>
        /// This method computes and sets the acerage Measures. 
        /// It uses what is saved in the database
        /// </summary>
        private void ProcessAveragesValues()
        {
            Log.Debug(LOG_TAG, GetType() + ".ProcessAveragesValues: called");

            var currentUser = this.userRepository.GetCurrentUser();

            // 1 - Average glycemia
            var averageGlycemia = this.glucoseMeasureRepository.GetAverageGlycemia(currentUser.Id);
            this.userRepository.ChangeAverageGlycemia(currentUser, averageGlycemia);

            // 2 - Average measures "In the medical zone"
            float averageInTheZone = this.glucoseMeasureRepository.GetAverageInTheZone(currentUser.Id);
            this.userRepository.ChangeAverageInTheMedicalZone(currentUser, averageInTheZone);

            // 3 - HBA1C = averageGlycemia (mmol/l) + 2.59 / 1.59
            double hba1c = (averageGlycemia * this.appSettings.MGDL_TO_MMOLL + 2.59) / 1.59;
            this.userRepository.ChangeHBA1C(currentUser, (float)hba1c);

        }

        /// <summary>
        /// This method is in charge of awake the device and connects to it
        /// NOTE : If an error is raised during its execution, this method calls itself (the called is post delayed)
        /// </summary>
        /// <returns></returns>
        private async Task WakeUpMeasureServiceAsync()
        {
            this.IsWakingUpDevice = true;
            Log.Debug(LOG_TAG, GetType() + ".WakeUpMeasureServiceAsync: called");

            bool needToPostDelayExecution = false;
            /*if (this.userRepository != null)
            { 
            }*/
            User currentUser = this.userRepository.GetCurrentUser();
            if (!currentUser.DeviceIsBounded)
            {
                Log.Debug(LOG_TAG, GetType() + ".WakeUpMeasureServiceAsync: no device bounded. No need to awake the mesure service");
                this.IsWakingUpDevice = false;
                return;
            }

            Models.Device device = new Models.Device(currentUser.DeviceId, currentUser.DeviceName);

            try
            {
                bool IsDeviceConnected = true;
                bool IsInitialized = true;
                bool IsSuscribed = true;
                bool IsWritten = true;
                bool IsRead = true;

                // step 1 : register device
                IsDeviceConnected = await this.measureService.RegisterDeviceAsync(device);

                // step 2 : initializes ble services
                if (IsDeviceConnected)
                {
                    IsInitialized = await this.measureService.InitializeBLEServicesAsync(device, MiaoMiaoProtocol.UART_SERVICE_ID);
                }

                // step 3 : subscription
                if (IsInitialized)
                {
                    IsSuscribed = await this.measureService.SubsrcibeCharacteristicAsync(MiaoMiaoProtocol.NRF_UART_TX);
                }

                // step 4 : reset
                // MIAOMIAO AND BUBBLE PROTOCOL
                if (IsSuscribed)
                {
                    List<byte[]> resetPacket = new List<byte[]>();
                    resetPacket.Add(FreeStyleLibreUtils.GenerateResetPacket(device.DeviceType));
                    IsWritten = await this.measureService.WriteCharacteristicAsync(MiaoMiaoProtocol.NRF_UART_RX, resetPacket);
                }

                // step 5 : the state of the service
                if (IsWritten)
                {
                    IsRead = this.measureService.CurrentState() != Models.Enums.MeasureServiceState.REFUSED_DATA_THEN_WAIT;
                }


                // if there was an error in of the steps, we start over
                if (!IsDeviceConnected || !IsInitialized || !IsSuscribed || !IsWritten || !IsRead)
                {
                    needToPostDelayExecution = true;

                    this.measureService.UnregisterDevice(device);
                    Log.Debug(LOG_TAG, GetType() + ".WakeUpMeasureServiceAsync: awakening is incomplete");
                }
                else
                {
                    Log.Debug(LOG_TAG, GetType() + ".WakeUpMeasureServiceAsync: awakening is complete");
                }
            }
            catch (CharacteristicReadException exception)
            {
                Log.Debug(LOG_TAG, GetType() + ".WakeUpMeasureServiceAsync: " + exception);
                if (this.measureService != null && device != null)
                    this.measureService.UnregisterDevice(device);

                needToPostDelayExecution = true;
            }
            finally
            {
                // post delay the execution of this method in X minutes because something went wrong 
                if (needToPostDelayExecution)
                {
                    await Task.Delay(this.appSettings.MEASURE_SERVICE_RETRY_DEFAULT_TIME * 1000 * 60).ContinueWith(t =>
                       Xamarin.Forms.Device.BeginInvokeOnMainThread(() => { WakeUpMeasureServiceAsync(); })
                        );
                }
                this.IsWakingUpDevice = false;
            }
        }

        /// <summary>
        /// This method determines if a measure is in the medical zone
        /// </summary>
        /// <param name="measure">The measure to check</param>
        /// <returns>True if the measure is in the medical zone. False in other case</returns>
        private bool InTheMedicalZone(GlucoseMeasure measure)
        {
            // The medical zone is fixed (verified MGDL)
            return this.appSettings.MEDICAL_ZONE_MGDL_MIN <= measure.GlucoseLevelMGDL
                && measure.GlucoseLevelMGDL <= this.appSettings.MEDICAL_ZONE_MGDL_MAX;
        }

        private void RAZMeasures()
        {
            this.glucoseMeasureRepository.RemoveAll();
        }
    }
}

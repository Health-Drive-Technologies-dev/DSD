using Android.Util;
using HealthAndDrive.Events;
using HealthAndDrive.Models;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using Java.Util;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthAndDrive.Protocol
{
    public class BubbleProtocol : BaseProtocol, IProtocol
    {

        private readonly IUserRepository UserRepository;
        private readonly IUserSettingsRepository UserSettingsRepository;

        /// <summary>
        /// The tag for the log
        /// </summary>
        public static string LOG_TAG = nameof(BubbleProtocol);

        /// <summary>
        /// MiaoMiao device name
        /// </summary>
        public const string BUBBLE_DEVICE_NAME = "Bubble";

        /// <summary>
        /// Protocol : BUBBLE SERVICE UUID
        /// </summary>
        public const string UART_SERVICE_ID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";

        /// <summary>
        /// RX Characteristic UUID
        /// </summary>
        public const string NRF_UART_RX = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";

        /// <summary>
        /// Protocol : TX Characteristic UUID
        /// </summary>
        public const string NRF_UART_TX = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        /// <summary>
        /// Expected Bubble footer
        /// </summary>
        private const int BUBBLE_FOOTER = 8;

        /// <summary>
        /// ??
        /// </summary>
        private const int BUBBLE_PATCH_INFO = 0;


        /// <summary>
        /// Length of the packet transmitted
        /// </summary>
        public static int lens = 344;

        /// <summary>
        /// Gets or sets the Glucose service reference
        /// </summary>
        private GlucoseService GlucoseService { get; set; }

        /// <summary>
        /// Gets or sets the eventAggregator reference
        /// </summary>
        private IEventAggregator EventAggregator { get; set; }

        private AppSettings settings;

        public BubbleProtocol(IEventAggregator eventAggregator, IUserRepository userRepository, GlucoseService glucoseService, AppSettings settings)
        {
            this.GlucoseService = glucoseService;
            this.EventAggregator = eventAggregator;
            this.settings = settings;
            this.UserRepository = userRepository;
            eventAggregator.GetEvent<MeasureChangeEventBubble>().Subscribe((packet) =>
            {
                ProcessPacket(packet);
            });


        }



        /// <summary>
        /// The device name 
        /// </summary>
        /// <returns>The device name</returns>
        public string GetDeviceName()
        {
            return BUBBLE_DEVICE_NAME;
        }

        /// <summary>
        /// The UUID of the service we need to read and write
        /// </summary>
        /// <returns>The UUID of the service</returns>
        public string GetServiceUUID()
        {
            return UART_SERVICE_ID;
        }

        /// <summary>
        /// The UUID of the RX characteristic
        /// </summary>
        /// <returns>The UUID of the RX characs</returns>
        public string GetRXCharacteristicUUID()
        {
            return NRF_UART_RX;
        }

        /// <summary>
        /// The UUID of the TX characteristic
        /// </summary>
        /// <returns>The UUID of the TX characs</returns>
        public string GetTxCharacteristicUUID()
        {
            return NRF_UART_TX;
        }

        /// <summary>
        /// Initiliaze the FullDataBuffer 
        /// Protocol : FullDataBuffer is Its size is indi
        /// </summary>
        /// <param name="expectedSize"></param>
        private void InitFullDataBuffer(int expectedSize)
        {
            FullData = new byte[expectedSize];
            AcumulatedSize = 0;

        }

        public bool IsReadingOver()
        {
            if (AcumulatedSize < lens)
            {
                State = Models.Enums.ProtocolState.RECEIVING_DATA;
                return false;
            }
            if (AcumulatedSize > lens + BUBBLE_FOOTER)
            {
                Log.Debug(LOG_TAG, "Accumulated size > 344 + 8, Problemm!!!");
            }
                
            Log.Debug(LOG_TAG, $"The accumulated size is {AcumulatedSize}");
            State = Models.Enums.ProtocolState.WAITING_DATA_CHANGE;
            return true;


                

        }

        public void ProcessPacket(byte[] packet)
        {

            ReadingSession.Begin();

            //Battery Info is extracted from the trame
            ReadingSession.BatteryLevel = packet[0];
            Log.Debug(LOG_TAG, $"The battery level is {ReadingSession.BatteryLevel}");
            //We remove the Battery Info from the packet
            packet.CopyTo(packet, 1);
           

            int first = 0xff & packet[0];
            Log.Debug(LOG_TAG, $"The first element is {first}");

            if (first == 0x80)
            {
                Log.Debug(LOG_TAG, "Bubble is requesting a response code : 0x80");
                return;
            }
            if (first == 0xC0)
            {
                Log.Debug(LOG_TAG, "Data ignored code : 0xC0");
                return;
            }
            if (first == 0xC1)
            {
                Log.Debug(LOG_TAG, "Data ignored code : 0xC1");
                return;
            }
            if (first == 0xBF)
            {
                Log.Debug(LOG_TAG, "Data refused code : 0xC0");
                return;
            }
            if (first == 0x82)
            {
                switch(State)
                {
                    case Models.Enums.ProtocolState.WAITING_DATA_CHANGE:
                    case Models.Enums.ProtocolState.REQUEST_DATA_SENT:
                        {
                            int expectedSize = lens + BUBBLE_FOOTER;
                            if (this.FullData == null)
                            {
                                InitFullDataBuffer(expectedSize);
                            }
                            Log.Debug(LOG_TAG, $"Starting to acumulate data expectedSize={expectedSize}");
                            PushDataBubble(packet);
                            break;
                        }
                    case Models.Enums.ProtocolState.RECEIVING_DATA:
                        PushDataBubble(packet);
                        break;
                }
                
            }

            if(IsReadingOver())
            {
                //ProcessBubbleData();
                ProcessData();
            }


        }

        /*private void ProcessBubbleData()
        {

        }*/

        private void ProcessData()
        {

            //We don't need the Bubble Footer
            byte[] data = Arrays.CopyOfRange(this.FullData, 0, 344);

            // MIAOMIAO PROTOCOL : The 4th byte is where the sensor status is.
            // TODO : Quoi faire ici ? On abandonne la lecture ? Noon ... --> no one no oneeeeeee ne sait
            if (!FreeStyleLibreUtils.IsSensorReady(data[4]))
                Log.Debug(LOG_TAG, "BubbleProtocol.ProcessData: Sensor is not ready, we should Ignoring reading!");

            // Here we are !! The show is about to begin :D
            // MIAOMIAO Protocol

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ReadingSession.LastReadingTimestamp = now;

            // MIAOMIAO Protocol. Trend index is at the 26th index
            int indexTrend = data[26] & 0xFF;

            // MIAOMIAO Protocol. History index is at the 27th index
            int indexHistory = data[27] & 0xFF;

            // MIAOMIAO Protocol. SensorTime is at 317th and 316th index
            int sensorTime = 256 * (data[317] & 0xFF) + (data[316] & 0xFF);
            long sensorStartTime = now - sensorTime * this.settings.MILLISECONDS_IN_MINUTE;

            Log.Debug(LOG_TAG, "BubbleProtocol.ProcessFullData: sensorTime=[" + sensorTime + "]");

            // option to use 13 bit mask
            bool thirteen_bit_mask = true;

            // loads history values (ring buffer, starting at index_trent. byte 124-315)
            for (int index = 0; index < 32; index++)
            {
                int i = indexHistory - index - 1;
                if (i < 0) i += 32;
                GlucoseMeasure measure = new GlucoseMeasure
                {
                    GlucoseLevelRaw = FreeStyleLibreUtils.ExtractGlucoseRaw(new byte[] { data[(i * 6 + 125)], data[(i * 6 + 124)] }, thirteen_bit_mask),
                };

                // we don't need null values
                if (measure.GlucoseLevelRaw == 0)
                    continue;

                int time = Math.Max(0, Math.Abs((sensorTime - 3) / 15) * 15 - index * 15);

                measure.Timestamp = sensorStartTime + time * this.settings.MILLISECONDS_IN_MINUTE;
                measure.RealDateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(sensorStartTime + time * this.settings.MILLISECONDS_IN_MINUTE);
                measure.SensorTime = time;
                measure.GlucoseLevelMGDL = (float)Math.Round((decimal)measure.GlucoseLevelRaw / 10);
                measure.GlucoseLevelMMOL = (float)FreeStyleLibreUtils.ConvertMGDLToMMolPerLiter(this.settings, Math.Round((double)measure.GlucoseLevelRaw / 10));
                ReadingSession.PushHistoryMeasure(measure);
            }

            // loads trend values (ring buffer, starting at index_trent. byte 28-123)
            for (int index = 0; index < 16; index++)
            {
                int i = indexTrend - index - 1;
                if (i < 0) i += 16;
                GlucoseMeasure measure = new GlucoseMeasure
                {
                    GlucoseLevelRaw = FreeStyleLibreUtils.ExtractGlucoseRaw(new byte[] { data[(i * 6 + 29)], data[(i * 6 + 28)] }, thirteen_bit_mask)
                };

                // we don't need null values
                if (measure.GlucoseLevelRaw == 0)
                    continue;

                int time = Math.Max(0, sensorTime - index);
                measure.RealDateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(sensorStartTime + time * this.settings.MILLISECONDS_IN_MINUTE);
                measure.Timestamp = sensorStartTime + time * this.settings.MILLISECONDS_IN_MINUTE;
                measure.SensorTime = time;
                measure.GlucoseLevelMGDL = (float)Math.Round((decimal)measure.GlucoseLevelRaw / 10);
                measure.GlucoseLevelMMOL = (float)FreeStyleLibreUtils.ConvertMGDLToMMolPerLiter(this.settings, Math.Round((double)measure.GlucoseLevelRaw / 10));
                ReadingSession.PushTrendMeasure(measure);

                Log.Debug(LOG_TAG, $"BubbleProtocol.ProcessData: measure=[{measure.ToString()}");
            }

            // The current measure
            if (ReadingSession.TrendMeasures.Count > 0)
            {
                ReadingSession.CurrentMeasure = ReadingSession.TrendMeasures[0];
            }

            ReadingSession.CalculateSmothedData5Points();

            // At the end, we end the ReadingMesureSession
            ReadingSession.End();

            // send the event to notify that the reading is over
            this.EventAggregator.GetEvent<EndReadingEvent>().Publish("");

            // Notify the GlucoseService that it's done
            this.GlucoseService.HandleReadingSession(ReadingSession);

            int expectedSize = lens + BUBBLE_FOOTER;
            InitFullDataBuffer(expectedSize);
        }
    }
}

using System;
using Android.Util;
using HealthAndDrive.Events;
using HealthAndDrive.Models;
using HealthAndDrive.Models.Enums;
using HealthAndDrive.Repositories;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using Java.Util;
using Prism.Events;

namespace HealthAndDrive.Protocol
{

    /// <summary>
    /// 
    /// ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
    /// ░░░░░░░░░░▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄░░░░░░░░░
    /// ░░░░░░░░▄▀░░░░░░░░░░░░▄░░░░░░░▀▄░░░░░░░
    /// ░░░░░░░░█░░▄░░░░▄░░░░░░░░░░░░░░█░░░░░░░
    /// ░░░░░░░░█░░░░░░░░░░░░▄█▄▄░░▄░░░█░▄▄▄░░░
    /// ░▄▄▄▄▄░░█░░░░░░▀░░░░▀█░░▀▄░░░░░█▀▀░██░░
    /// ░██▄▀██▄█░░░▄░░░░░░░██░░░░▀▀▀▀▀░░░░██░░
    /// ░░▀██▄▀██░░░░░░░░▀░██▀░░░░░░░░░░░░░▀██░
    /// ░░░░▀████░▀░░░░▄░░░██░░░▄█░░░░▄░▄█░░██░
    /// ░░░░░░░▀█░░░░▄░░░░░██░░░░▄░░░▄░░▄░░░██░
    /// ░░░░░░░▄█▄░░░░░░░░░░░▀▄░░▀▀▀▀▀▀▀▀░░▄▀░░
    /// ░░░░░░█▀▀█████████▀▀▀▀████████████▀░░░░
    /// ░░░░░░████▀░░███▀░░░░░░▀███░░▀██▀░░░░░░
    /// ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
    /// 
    /// MIAOMIAO PROTOCOL
    /// </summary>
    public class MiaoMiaoProtocol : BaseProtocol, IProtocol
    {
        /// <summary>
        /// The tag for the log
        /// </summary>
        public static string LOG_TAG = nameof(MiaoMiaoProtocol);

        /// <summary>
        /// MiaoMiao device name
        /// </summary>
        public const string MIAO_MIAO_DEVICE_NAME = "miao";

        /// <summary>
        /// Protocol : MIAOMIAO SERVICE UUID
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
        /// Expected Tomato header length
        /// </summary>
        private const int TOMATO_HEADER_LENGTH = 18;

        /// <summary>
        /// ??
        /// </summary>
        private const int TOMATO_PATCH_INFO = 0;

        /// <summary>
        /// Gets or sets the Glucose service reference
        /// </summary>
        private GlucoseService GlucoseService { get; set; }

        /// <summary>
        /// Gets or sets the eventAggregator reference
        /// </summary>
        private IEventAggregator EventAggregator { get; set; }

        private AppSettings settings;

        public MiaoMiaoProtocol(IEventAggregator eventAggregator, IUserRepository userRepository, GlucoseService glucoseService, AppSettings settings)
        {
            this.GlucoseService = glucoseService;
            this.EventAggregator = eventAggregator;
            this.settings = settings;
            
            this.EventAggregator.GetEvent<MeasureChangeEventMiaoMiao>().Subscribe((value) =>
            {
                ProcessPacket(value);
            });
        }

        /// <summary>
        /// The device name 
        /// </summary>
        /// <returns>The device name</returns>
        public string GetDeviceName()
        {
            return MIAO_MIAO_DEVICE_NAME;
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
            PacketCount = 1;
        }

        /// <summary>
        /// This methods deteremines if the reading is over.
        /// In the MIAOMIAO protocol, the reading is over if the las byte of the FUllData is 0x28
        /// </summary>
        /// <returns>True if the  reading is over. False in any other cases</returns>
        public bool IsReadingOver()
        {
            // MIAOMIAO PROTOCOL : if the last byte received is 0x29, then ther will not be any more value
            if (FullData.Length > 0 && FullData[FullData.Length - 1] != 0x29)
            {
                State = ProtocolState.RECEIVING_DATA;
                Log.Debug(LOG_TAG, $"{this.GetType()}.IsReadingOver: State=[{State}]");
                return false;
            }

            if (AcumulatedSize < 344 + TOMATO_HEADER_LENGTH + 1)
            {
                Log.Debug(LOG_TAG, $"{this.GetType()}.IsReadingOver: State=[{State}]");
                State = ProtocolState.RECEIVING_DATA;
                return false;
            }

            if (AcumulatedSize >= 344 + TOMATO_HEADER_LENGTH && FullData[FullData.Length - 1] != 0x29)
            {
                Log.Debug(LOG_TAG, $"{this.GetType()}.IsReadingOver: PROBLEME !!!!!!!!");
            }



            // else, we are DONE !!
            // wait until another notification from device
            State = ProtocolState.WAITING_DATA_CHANGE;
            Log.Debug(LOG_TAG, $"{this.GetType()}.IsReadingOver: State=[{State}]");
            return true;
        }

        /// <summary>
        /// PRocess the packet (decoding, extracting values, creating models)
        /// </summary>
        /// <param name="packet">The acket received by the notification and to be decoded</param>
        public void ProcessPacket(byte[] packet)
        {
            // First we begin a new ReadingMesureSession
            ReadingSession.Begin();

            //Log.Debug(LOG_TAG, $"{this.GetType()}.ProcessPacket : {Utils.ByteArrayToString(packet)}");

            if (packet.Length == 0)
            {
                Log.Debug(LOG_TAG, $"{this.GetType()}.ProcessPacket: data is empty");
                return;
            }

            if (packet.Length == 1 && packet[0] == 0x32)
            {
                // Ne devrait pas arriver
                Log.Debug(LOG_TAG, $"{this.GetType()}.ProcessPacket: 0X32");
            }

            if (packet.Length == 1 && packet[0] == 0x34)
            {
                Log.Debug(LOG_TAG, $"{this.GetType()}.ProcessPacket: 0X34");
                return;
            }

            switch (State)
            {
                case ProtocolState.WAITING_DATA_CHANGE:
                case ProtocolState.REQUEST_DATA_SENT:

                    if (packet.Length >= TOMATO_HEADER_LENGTH && packet[0] == 0x28)
                    {
                        // We are starting to receive data, need to start accumulating
                        // &0xff is needed to convert to hex.
                        // MIAOMIAO PROTOCOL
                        int expectedSize = 256 * (int)(packet[1] & 0xff) + (int)(packet[2] & 0xff);
                        Log.Debug(LOG_TAG, $"Starting to acumulate data expectedSize={expectedSize}");
                        InitFullDataBuffer(expectedSize + TOMATO_PATCH_INFO);
                        PushData(packet);
                    }
                    break;

                case ProtocolState.RECEIVING_DATA:
                    PushData(packet);
                    break;
            }

            bool isReadingOver = IsReadingOver();
            PacketCount += 1; //Inutile?

            // Only if the reading is over, we process the data
            if (isReadingOver)
            {
                ProcessMiaoMiaoData();
                ProcessData();
            }
        }



        /// <summary>
        /// This method process the data coming from MiaoMiao (Battery, Firmware)
        /// </summary>
        private void ProcessMiaoMiaoData()
        {
            // in this method, we fill the MeasurementReadingSession

            // MIAOMIAO PROTOCOL : 
            ReadingSession.SensorId = Utils.ByteArrayToString(Arrays.CopyOfRange(this.FullData, 5, 13));

            // MIAOMIAO PROTOCOL : Battery level is at 13th index of the FullData
            ReadingSession.BatteryLevel = this.FullData[13];

            // MIAOMIAO PROTOCOL : Hardware name is cod ed at 14th and 15th index of the FullData
            ReadingSession.FirmwareVersion = Convert.ToBase64String(this.FullData, 14, 2);

            // MIAOMIAO PROTOCOL : Hardware name is coded at 16th and 17th index of the FullData

            Log.Debug(LOG_TAG, "MiaoMiaoProtocol.ProcessTomatoData: BatteryLevel=[" + ReadingSession.BatteryLevel + "], FirmwareVersion=[" + ReadingSession.FirmwareVersion + "], HardwareName=[" + ReadingSession.SensorId + "]");

        }

        private void ProcessData()
        {

            //WeakHashMap don't need the Tomato Header part
            byte[] data = Arrays.CopyOfRange(this.FullData, TOMATO_HEADER_LENGTH, TOMATO_HEADER_LENGTH + 344);

            // MIAOMIAO PROTOCOL : The 4th byte is where the sensor status is.
            // TODO : Quoi faire ici ? On abandonne la lecture ? Noon ... --> no one no oneeeeeee ne sait
            if (!FreeStyleLibreUtils.IsSensorReady(data[4]))
                Log.Debug(LOG_TAG, "MiaoMiaoProtocol.ProcessData: Sensor is not ready, we should Ignoring reading!");

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

            Log.Debug(LOG_TAG, "MiaoMiaoProtocol.ProcessFullData: sensorTime=[" + sensorTime + "]");

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

                Log.Debug(LOG_TAG, $"MiaoMiaoProtocol.ProcessData: measure=[{measure.ToString()}");
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
        }




    }
}

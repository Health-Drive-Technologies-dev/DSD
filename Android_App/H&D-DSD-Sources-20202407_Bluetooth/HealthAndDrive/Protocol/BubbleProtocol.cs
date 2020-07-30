using HealthAndDrive.Events;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
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

        public void ProcessPacket(byte[] packet)
        {
            throw new NotImplementedException();
        }
    }
}

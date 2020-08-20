
namespace HealthAndDrive.Tools
{
    public class AppSettings
    {
        /// <summary>
        /// Date and time format
        /// </summary>
        public string DATE_AND_TIME_FORMAT = "dd/MM/yyyy HH:mm";

        /// <summary>
        // For the MVP, there is only one user in local
        /// </summary>
        public string USER_ID = "deadbeef-cafe-babe-cafe-babecafebabe";

        /// <summary>
        /// The default index page for the carousel in the bluetooth page
        /// </summary>
        public long BLUETOOTH_INDEX_PAGE_DEFAULT = 0;

        /// <summary>
        /// The device index page for the carousel in the bluetooth page
        /// </summary>
        public long BLUETOOTH_INDEX_PAGE_DEVICE = 1;

        /// <summary>
        /// Bluetooth scan timeout in seconds
        /// </summary>
        public int BLUETOOTH_SCAN_TIMEOUT = 5;

        /// <summary>
        /// 
        /// </summary>
        public string DEVICE_GUID_FORMAT = "00000000-0000-0000-0000-{0}";

        /// <summary>
        /// Number of milliseconds in a minute
        /// </summary>
        public int MILLISECONDS_IN_MINUTE = 60000;

        /// <summary>
        /// RX Characteristic UUID
        /// </summary>
        public string NRF_UART_RX = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";

        /// <summary>
        /// Protocol : TX Characteristic UUID
        /// </summary>
        public string NRF_UART_TX = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        /// <summary>
        /// Emergency number to call
        /// </summary>
        public int EMERGENCY_NUMBER = 112;

        /// <summary>
        /// TDE
        /// </summary>
        public string TDE = "1048198951";

        /// <summary>
        /// HELP URL 
        /// </summary>
        public string HELP_URL = "https://www.healthdrivetechnologies.eu/aide";

        /// <summary>
        /// WEBSITE URL 
        /// </summary>
        public string WEBSITE_URL = "https://www.healthdrivetechnologies.eu/";

        /// <summary>
        /// Notification interval between two measures in minutes
        /// </summary>
        public int MEASURE_NOTIFICATION_INTERVAL = 5;

        /// <summary>
        /// Glucose Measure Service retry default time in minutes
        /// </summary>
        public int MEASURE_SERVICE_RETRY_DEFAULT_TIME = 1;

        /// <summary>
        /// Glucose Measure Service retry default time in minutes when no device is connected to the app
        /// </summary>
        public int MEASURE_SERVICE_RETRY_DEFAULT_TIME_DISCONNECTED = 10;

        /// <summary>
        /// The maximum of the maximum glucose value
        /// </summary>
        public int ALERTS_MAX_OF_MAX_GLUCOSE_VALUE = 220;

        /// <summary>
        /// The minimum of the maximum glucose value
        /// </summary>
        public int ALERTS_MIN_OF_MAX_GLUCOSE_VALUE = 170;

        /// <summary>
        /// The maximum of the minimum glucose value
        /// </summary>
        public int ALERTS_MAX_OF_MIN_GLUCOSE_VALUE = 80;

        /// <summary>
        /// The minimum of the minimum glucose value
        /// </summary>
        public int ALERTS_MIN_OF_MIN_GLUCOSE_VALUE = 60;

        /// <summary>
        /// MilliMol  per Deciliter constant
        /// </summary>
        public static double MMOLL_TO_MGDL = 18.0182;

        /// <summary>
        /// Milligram to Millimol per liter constant
        /// </summary>
        public double MGDL_TO_MMOLL = 1 / MMOLL_TO_MGDL;

        /// <summary>
        /// Indicates if we deal with MGDL_MEASAURE
        /// </summary>
        public bool HANDLE_MGDL_MEASURE = true;

        /// <summary>
        /// The min value for the medical zone (included)
        /// </summary>
        public float MEDICAL_ZONE_MGDL_MIN = 70 ;

        /// <summary>
        /// The max value for the medical zone (included)
        /// </summary>
        public float MEDICAL_ZONE_MGDL_MAX = 180;

        /// <summary>
        /// Red color forr drive page
        /// </summary>
        public string COLOR_RED = "#e01010";

        /// <summary>
        /// Orange color forr drive page
        /// </summary>
        public string COLOR_ORANGE = "#f45b17";

        /// <summary>
        /// Yellow color forr drive page
        /// </summary>
        public string COLOR_YELLOW = "#edad1e";

        /// <summary>
        /// Green color forr drive page
        /// </summary>
        public string COLOR_GREEN = "#62d248";

        /// <summary>
        /// The export filename for the data in the zone
        /// </summary>
        public string CSV_IN_ZONE_FILENAME = "HealthAndDrive.data_inzone.csv";

        /// <summary>
        /// The export filename for the datas not in the zone
        /// </summary>
        public string CSV_NOT_IN_ZONE_FILENAME = "HealthAndDrive.data_not_inzone.csv";

        /// <summary>
        /// The chart vertical axes max value
        /// </summary>
        public int CHART_VERT_AXE_MAX = 455;

        /// <summary>
        /// The chart vertical axes min value
        /// </summary>
        public int CHART_VERT_AXE_MIN = 0;

        /// <summary>
        /// The alert page name to avoid alert page redirection when
        /// you're already on the page
        /// </summary>
        public string ALERTS_PAGE_NAME = "ALERTES";

        /// <summary>
        /// The HI value used in drive page and widget
        /// </summary>
        public int HI_VALUE = 400;

        /// <summary>
        /// The LO value used in drive page and widget
        /// </summary>
        public int LO_VALUE = 30;

        /// <summary>
        /// Retry Delay in second
        /// </summary>
        public double RetryBluetoothDelay = 60;


        
    }
}
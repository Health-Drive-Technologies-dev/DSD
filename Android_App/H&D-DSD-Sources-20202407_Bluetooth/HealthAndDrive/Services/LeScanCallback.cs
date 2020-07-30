using Android.Bluetooth.LE;
using HealthAndDrive.Protocol;

namespace HealthAndDrive.Services
{
    /// <summary>
    /// Bluetooth Scan Callback
    /// The scan result is here !! The scan result are stored in the <code>BluetoothService.DiscoveredDevices</code> property
    /// </summary>
    public class LeScanCallback : ScanCallback
    {
        readonly BluetoothScanService Service;

        public LeScanCallback(BluetoothScanService service)
        {
            this.Service = service;
        }

        /// <summary>
        /// The scan result is here !! The scan result are stored in the <code>BluetoothService.DiscoveredDevices</code> property
        /// </summary>
        /// <param name="cbt">The ScanCallbacktype</param>
        /// <param name="res">The scan result</param>
        public override void OnScanResult(ScanCallbackType cbt, ScanResult result)
        {
            // We don't need a device without name
            if (string.IsNullOrEmpty(result.Device.Name))
            {
                return;
            }

            // NOTE: For the H&D MVP, we only look for "MiaoMiao"
            if (!result.Device.Name.Contains(MiaoMiaoProtocol.MIAO_MIAO_DEVICE_NAME) && !result.Device.Name.Contains(BubbleProtocol.BUBBLE_DEVICE_NAME))
            {
                return;
            }

            if (!Service.DiscoveredDevices.Contains(result.Device))
            {
                this.Service.DiscoveredDevices.Add(result.Device);
            }

            return;
        }
    }
}

using Android.Bluetooth;
using Plugin.BLE.Abstractions.Contracts;
using System;

namespace HealthAndDrive.Models
{
    public class Device : IComparable<Device>
    {

        /// <summary>
        /// The device unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The device name
        /// </summary>
        public string Name { get; set; }

        public DeviceType DeviceType { get; set; }
        public Device()
        {
        }

        /// <summary>
        /// This constructor initialize a device from an id and a name
        /// </summary>
        /// <param name="id">The device Id</param>
        /// <param name="name">The device name</param>
        public Device(string id, string name)
        {
            this.Id = id;
            this.Name = name;
            if (this.Name.Contains("miao"))
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
            if (this.Name.Contains("Bubble"))
            {
                this.DeviceType = DeviceType.Bubble;
            }
            else
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
        }

        /// <summary>
        /// This constructor initializes an object from a BluetoothDevice
        /// </summary>
        /// <param name="device">The source device</param>
        public Device(BluetoothDevice device)
        {
            this.Id = device.Address;
            this.Name = device.Name;
            if (this.Name.Contains("miao"))
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
            if (this.Name.Contains("Bubble"))
            {
                this.DeviceType = DeviceType.Bubble;
            }
            else
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
        }

        /// <summary>
        /// This constructor initializes an object from a IDevice
        /// </summary>
        /// <param name="device">The source device</param>
        public Device(IDevice device)
        {
            if (device == null)
            {
                throw new ArgumentException("device cannot be null");
            }

            this.Name = device.Name;
            if (this.Name.Contains("miao"))
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
            if (this.Name.Contains("Bubble"))
            {
                this.DeviceType = DeviceType.Bubble;
            }
            else
            {
                this.DeviceType = DeviceType.MiaoMiao;
            }
        }

        public int CompareTo(Device other)
        {
            if (other == null)
            {
                return 1;
            }

            return this.Id.CompareTo(other.Id);

        }
        public string ToString()
        {
            return string.Format("id=[{0}], name=[{1}]", this.Id, this.Name);
        }

        public static bool operator ==(Device obj1, Device obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (obj1 is null)
            {
                return false;
            }
            if (obj2 is null)
            {
                return false;
            }

            return obj1.Id.Equals(obj2.Id);
        }

        // this is second one '!='
        public static bool operator !=(Device obj1, Device obj2)
        {
            return !(obj1 == obj2);
        }

        public bool Equals(Device other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Id.Equals(other.Id);
        }
        public override bool Equals(object other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.GetType() == GetType() && Equals((Device)other);
        }

    }
}

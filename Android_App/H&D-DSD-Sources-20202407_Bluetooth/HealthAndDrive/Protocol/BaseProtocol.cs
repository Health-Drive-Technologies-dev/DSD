using Android.Util;
using DryIoc;
using HealthAndDrive.Events;
using HealthAndDrive.Models;
using HealthAndDrive.Models.Enums;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Services;
using HealthAndDrive.Tools;
using Java.Util;
using Prism.Events;
using Prism.Ioc;
using System;

namespace HealthAndDrive.Protocol
{
    public class BaseProtocol
    {

        

        public static string LOG_TAG = "BaseProtocol";
        /// <summary>
        /// The array containg the cumulation of the data notified in a reading session.
        /// As the receiving is divided, we need to rebuild the full data array.
        /// </summary>
        protected volatile byte[] FullData;

        /// <summary>
        /// The protocol state. As it's accessed by multiple thread AND the receiving is divided,
        /// this state is used to know if we wait for more or note.
        /// </summary>
        protected volatile ProtocolState State;

        /// <summary>
        ///  The total number of bytes received
        /// </summary>
        protected volatile int AcumulatedSize = 0;

        /// <summary>
        /// The number of packet received by the protocol
        /// </summary>
        protected volatile int PacketCount = 0;
        public DeviceType ActualDeviceType { get; set; }

        /// <summary>
        /// The SessionReading session. 
        /// At the end of the reading process, this data structure contains the 
        /// informations about the reading (battery level, sensor id, measures)
        /// </summary>
        protected volatile MeasureReadingSession ReadingSession;

    


        public BaseProtocol()
        {
           
            ReadingSession = new MeasureReadingSession();
            
            
        }

                

        /// <summary>
        /// This method push the read bytes to the FullData array
        /// Among the PROTOCOL MIAOMIAO, data starts if first_byte = 0x28 and ends if last_byte = 0x29
        /// </summary>
        /// <param name="data"></param>
        protected void PushData(byte[] data)
        {
            int bytesToIgnore = 0;

            // Testing if the total received length is larger than the expected size
            if (AcumulatedSize + data.Length > FullData.Length)
            {
                bytesToIgnore = AcumulatedSize + data.Length - FullData.Length;
                Log.Debug("BaseProtocol", $"{this.GetType()}.IsReadingOver: AcumulatedSize={AcumulatedSize}, bytesToIgnore={bytesToIgnore} ,bytesToReadInPacket={ data.Length - bytesToIgnore}");
            }

            Array.Copy(data, 0, FullData, AcumulatedSize, data.Length - bytesToIgnore);
            AcumulatedSize += data.Length - bytesToIgnore;
        }

        /// <summary>
        /// This method push the read bytes to the FullData array
        /// Among the PROTOCOL BUBBLE, data starts if first_byte = 0x28 and ends if last_byte = 0x29
        /// </summary>
        /// <param name="data"></param>
        protected void PushDataBubble(byte[] data)
        {
            
            Array.Copy(data, 4, FullData, AcumulatedSize, data.Length-4);
            AcumulatedSize += data.Length - 4;
        }



    }
}

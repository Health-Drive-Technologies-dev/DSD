using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HealthAndDrive.Tools
{
    public static class Utils
    {
        /// <summary>
        /// Return a translation for a given key
        /// </summary>
        /// <param name="key">The translation key to be found</param>
        /// <returns>The translation if it exists. NULL in any other case</returns>
        public static string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            try
            {
                return AppResources.ResourceManager.GetString(key, CultureInfo.CurrentCulture);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a given value into a Guid.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="format">The optionnal string format to use</param>
        /// <returns>The converted Guid. NULL in any other case</returns>
        public static Guid? AsGuid(string value, string format = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                // clean the value first
                value = value.Replace(":", "");

                // if specfied format
                if (format != null)
                {
                    return Guid.Parse(String.Format(format, value));
                }

                return Guid.Parse(value);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if the TDE is valid or not 
        /// </summary>
        /// <param name="s">The timestamp value (the string is reverted)</param>
        /// <returns></returns>
        /*public static bool IsValidTDE(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            var reverted = new string(charArray);

            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(reverted)) > DateTimeOffset.UtcNow;
        }*/

        /// <summary>
        /// Converts a byte array to its string representation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            if (data.Length == 0)
            {
                return null;
            }

            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

    }
}

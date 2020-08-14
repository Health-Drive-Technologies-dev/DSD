using HealthAndDrive.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HealthAndDrive.Protocol
{
    /// <summary>
    /// This class stores all the information about a reading session
    /// A reading session posesses a beginning, an end, sensor informations, glucose measures
    /// </summary>
    public class MeasureReadingSession
    {
        /// <summary>
        /// Gets or sets the begin of the reading session
        /// </summary>
        public long ReadingStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the reading session
        /// </summary>
        public long ReadingEnd { get; set; }

        /// <summary>
        /// Gets or sets the begin of sensor id
        /// </summary>
        public string SensorId { get; set; }

        /// <summary>
        /// Gets or sets the begin of the reading session
        /// </summary>
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the Battery level
        /// </summary>
        public int BatteryLevel { get; set; }

        /// <summary>
        /// Gets or sets the Sensor Serial number
        /// </summary>
        public string LibreSN { get; set; }

        /// <summary>
        /// Gets or sets the last reading timestamp
        /// </summary>
        public long LastReadingTimestamp { get; set; }

        /// Gets or sets the list of ther historical measures  given by the sensor
        public List<GlucoseMeasure> HistoryMeasures { get; set; }

        /// Gets or sets the list of the trend measures given by the sensor
        public List<GlucoseMeasure> TrendMeasures { get; set; }

        /// <summary>
        /// Current measure given by the sensor
        /// </summary>
        public GlucoseMeasure CurrentMeasure { get; set; }

    public MeasureReadingSession()
        {
            this.HistoryMeasures = new List<GlucoseMeasure>();
            this.TrendMeasures = new List<GlucoseMeasure>();
        }

        /// <summary>
        /// Push a GlucoseMeasure in the history list
        /// </summary>
        /// <param name="measure">The measure to push</param>
        public void PushHistoryMeasure(GlucoseMeasure measure)
        {
            this.HistoryMeasures.Add(measure);
        }

        /// <summary>
        /// Push a GlucoseMeasure in the trend list
        /// </summary>
        /// <param name="measure">The measure to push</param>
        public void PushTrendMeasure(GlucoseMeasure measure)
        {
            this.TrendMeasures.Add(measure);
        }

        public List<GlucoseMeasure> GetAllMeasures()
        {
            var values = new List<GlucoseMeasure>();
            values.AddRange(this.HistoryMeasures);
            values.AddRange(this.TrendMeasures);
            return values;
        }

        public void CalculateSmothedData5Points()
        {
            // In all places in the code, there should be exactly 16 points.
            // Since that might change, and I'm doing an average of 5, then in the case of less then 5 points,
            // I'll only copy the data as is (to make sure there are reasonable values when the function returns).
            if (this.TrendMeasures.Count < 5)
            {
                for (int i = 0; i < this.TrendMeasures.Count - 4; i++)
                {
                    this.TrendMeasures[i].GlucoseLevelRawSmoothed = this.TrendMeasures[i].GlucoseLevelRaw;
                }
                return;
            }

            for (int i = 0; i < this.TrendMeasures.Count - 4; i++)
            {
                this.TrendMeasures[i].GlucoseLevelRawSmoothed =
                        (this.TrendMeasures[i].GlucoseLevelRaw +
                         this.TrendMeasures[i + 1].GlucoseLevelRaw +
                         this.TrendMeasures[i + 2].GlucoseLevelRaw +
                         this.TrendMeasures[i + 3].GlucoseLevelRaw +
                         this.TrendMeasures[i + 4].GlucoseLevelRaw) / 5;
            }

            // We now have to calculate the last 4 points, will do our best...
            this.TrendMeasures[this.TrendMeasures.Count - 4].GlucoseLevelRawSmoothed =
                    (this.TrendMeasures[this.TrendMeasures.Count - 4].GlucoseLevelRaw +
                     this.TrendMeasures[this.TrendMeasures.Count - 3].GlucoseLevelRaw +
                     this.TrendMeasures[this.TrendMeasures.Count - 2].GlucoseLevelRaw +
                     this.TrendMeasures[this.TrendMeasures.Count - 1].GlucoseLevelRaw) / 4;

            this.TrendMeasures[this.TrendMeasures.Count - 3].GlucoseLevelRawSmoothed =
                   (this.TrendMeasures[this.TrendMeasures.Count - 3].GlucoseLevelRaw +
                    this.TrendMeasures[this.TrendMeasures.Count - 2].GlucoseLevelRaw +
                    this.TrendMeasures[this.TrendMeasures.Count - 1].GlucoseLevelRaw) / 3;

            // Use the last two points for both last points
            this.TrendMeasures[this.TrendMeasures.Count - 2].GlucoseLevelRawSmoothed =
                    (this.TrendMeasures[this.TrendMeasures.Count - 2].GlucoseLevelRaw +
                    this.TrendMeasures[this.TrendMeasures.Count - 1].GlucoseLevelRaw) / 2;

            this.TrendMeasures[this.TrendMeasures.Count - 1].GlucoseLevelRawSmoothed = this.TrendMeasures[this.TrendMeasures.Count - 2].GlucoseLevelRawSmoothed;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            return builder.
                Append("ReadingStart=[" + this.ReadingStart + "], ")
                .Append("ReadingEnd=[" + this.ReadingEnd + "], ")
                .Append("ReadingEnd=[" + this.ReadingEnd + "], ")
                .Append("SensorId=[" + this.SensorId + "], ")
                .Append("FirmwareVersion[" + this.FirmwareVersion + "], ")
                .Append("BatteryLevel=[" + this.BatteryLevel + "], ")
                .Append("LibreSN=[" + this.LibreSN + "], ")
                .Append("Measures.Count=[" + this.HistoryMeasures.Count + this.TrendMeasures.Count + "]")
                .ToString();
        }

        /// <summary>
        /// This methods indicates the begining of the session.
        /// It reinitilizes the values at each reading session
        /// </summary>
        public void Begin()
        {
            this.ReadingStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.ReadingEnd = -1;
            this.SensorId = null;
            this.FirmwareVersion = null;
            this.BatteryLevel = -1;
            this.LibreSN = null;
            this.LastReadingTimestamp = -1;
            this.HistoryMeasures = new List<GlucoseMeasure>();
            this.TrendMeasures = new List<GlucoseMeasure>();
        }

        /// <summary>
        /// This methods indicates the end of the session.
        /// </summary>
        public void End()
        {
            this.ReadingEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

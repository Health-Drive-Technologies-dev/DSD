using System;
using System.Collections.Generic;
using System.Linq;
using HealthAndDrive.Models;
using HealthAndDrive.Protocol;
using HealthAndDrive.RepositoryContracts;
using HealthAndDrive.Tools;
using Realms;

namespace HealthAndDrive.Repositories
{
    /// <summary>
    /// The class is a GlucoseMeasure repository and allows to access glucose measure data
    /// </summary>
    public class GlucoseMeasureRepository : IGlucoseMeasureRepository
    {
        /// <summary>
        /// Access to the realm database
        /// </summary>
        private readonly Realm realm;

        /// <summary>
        /// The app settings
        /// </summary>
        private AppSettings settings;

        /// <summary>
        /// INitializes a new instance of the class <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="realm"></param>
        public GlucoseMeasureRepository(Realm realm, AppSettings settings)
        {
            this.realm = realm;
            this.settings = settings;
        }

        /// <summary>
        /// Get a measure for a given sensortime, sensorId, userId
        /// NOTE : 
        ///     For the H&D MVP, the user will always be the same.
        ///     MiaoMiao Protocol : The Freestyle14 sensor has a lifetime of 14 days.
        ///     After a change, the sensortime starts over.
        ///     THe "primary key is representated by the composition of sensorTime | sensorId | userId
        /// </summary>
        /// <param name="timestamp">The sensor time</param>
        /// <param name="sensorId">The sensorId</param>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public GlucoseMeasure GetMeasureByTimestampSensorIdAndUser(long timestamp, string sensorId, string userId)
        {
            return this.realm.All<GlucoseMeasure>().Where(m => m.Timestamp == timestamp && m.SensorId == sensorId && m.UserId == userId).FirstOrDefault();
        }

        /// <summary>
        /// Returns the last GlucoseMeasure for a user
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public GlucoseMeasure GetLastMeasureByUser(string userId)
        {
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId).OrderByDescending(m => m.Timestamp).FirstOrDefault();
        }

        /// <summary>
        /// Determines if a measure exists or not
        /// </summary>
        /// <param name="timestamp">The sensor time</param>
        /// <param name="sensorId">The sensorId</param>
        /// <param name="userId">The userId</param>
        /// <returns>True if the measure exists. False in other case</returns>
        public bool Exists(long timestamp, string sensorId, string userId)
        {
            return GetMeasureByTimestampSensorIdAndUser(timestamp, sensorId, userId) != null;
        }

        /// <summary>
        /// Creates a GlucoseMeasure
        /// </summary>
        /// <param name="entity">The measure to create</param>
        public void Create(GlucoseMeasure entity, string userId)
        {
            this.realm.Write(() =>
            {
                entity.UserId = userId;
                this.realm.Add(entity);
            });
        }

        /// <summary>
        /// Returns the full list of GlucoseMeasure for a user 
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public List<GlucoseMeasure> GetAllMeasuresByUser(string userId)
        {

            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId).OrderBy(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the list of GlucoseMeasure for a user. 
        /// The list is limited to the measures of the past 24 hours
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public List<GlucoseMeasure> GetLastDayMeasuresByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddHours(-24);
            
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                                      && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderBy(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the list of GlucoseMeasure for a user. 
        /// The list is limited to the measures of the past week
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public List<GlucoseMeasure> GetLastMonthMeasuresByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddDays(-30);
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                          && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderBy(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the list of GlucoseMeasure for a user. 
        /// The list is limited to the measures of the 15 past days 
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>    
        public List<GlucoseMeasure> GetLastHalfMonthMeasuresByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddDays(-15);
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                          && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderBy(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the list of GlucoseMeasure for a user. 
        /// The list is limited to the measures of the past month
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public List<GlucoseMeasure> GetLastWeekMeasuresByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddDays(-7);
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                          && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderBy(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the last sixty minutes glucose measures for a user
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public List<GlucoseMeasure> GetLastSixtyMinutesMeasuresByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddMinutes(-60);
            return this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                          && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderByDescending(m => m.RealDateTimeOffset).ToList();
        }

        /// <summary>
        /// Returns the last past fifteen minutes glucose measure for a user
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public GlucoseMeasure GetPastFifteenMinutesMeasureByUser(string userId)
        {
            DateTime oldDate = DateTime.Now.AddMinutes(-15);
            List<GlucoseMeasure> glucoseMeasures = this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId
                                                      && (m.RealDateTimeOffset > oldDate && m.RealDateTimeOffset <= DateTime.Now)).OrderBy(m => m.RealDateTimeOffset).ToList();
            return glucoseMeasures.FirstOrDefault(); //Because the list is ordered by datetime ascend, the first value is the oldest one
        }

        /// <summary>
        /// Remove all the GlucoseMeasure in the database
        /// </summary>
        public void RemoveAll()
        {
            this.realm.Write(() =>
            {
                this.realm.RemoveAll<GlucoseMeasure>();
            });
        }

        /// <summary>
        /// Counts all the GlucoseMeasure for a given user
        /// </summary>
        /// <param name="userId">The userId</param>
        /// <returns></returns>
        public long CountByUser(string userId)
        {
            return this.realm.All<GlucoseMeasure>().Count(m => m.UserId == userId);
        }

        /// <summary>
        /// Gets the average of measures in the medical zone
        /// NOTE : Average and Sum functions is not supported in REALM (https://realm.io/docs/dotnet/0.81.0/api/md_linq-support.html)
        /// So we have to do it "by hand"
        /// </summary>
        /// <param name="userId">The user to filter</param>
        /// <returns>The average measure</returns>
        public float GetAverageInTheZone(string userId)
        {
            long countMeasures = CountByUser(userId);
            if (countMeasures == 0)
            {
                return 0;
            }
            long countInTheZone = this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId && m.InTheMedicalZone == true).Count();
            return ((float)countInTheZone / countMeasures) * 100;
        }

        /// <summary>
        /// Gets the average of glycemia for a user
        /// NOTE : Average and Sum functions is not supported in REALM (https://realm.io/docs/dotnet/0.81.0/api/md_linq-support.html)
        /// So we have to do it "by hand"
        /// </summary>
        /// <param name="userId">The user to filter</param>
        /// <returns>The average measure</returns>
        public float GetAverageGlycemia(string userId)
        {
            long countMeasures = CountByUser(userId);
            if (countMeasures == 0)
            {
                return 0;
            }
            float sumGlycemia = 0;
            foreach (GlucoseMeasure measure in this.realm.All<GlucoseMeasure>().Where(m => m.UserId == userId))
            {
                sumGlycemia += this.settings.HANDLE_MGDL_MEASURE ? measure.GlucoseLevelMGDL : measure.GlucoseLevelMMOL;
            }

            return sumGlycemia / countMeasures;
        }

        /// <summary>
        /// Update the measure offset for a GlucoseMeasure
        /// </summary>
        /// <param name="measure">The measure to update</param>
        /// <param name="offsetValue">The offset value coming from the calibration</param>
        /// <param name="appSettings">The appSettings</param>
        public GlucoseMeasure UpdateMeasureOffset(GlucoseMeasure measure, float calibrationOffset, AppSettings appSettings)
        {
            using (var trans = realm.BeginWrite())
            {
                measure.CalibrationOffset = calibrationOffset;
                measure.GlucoseLevelMGDL = (float)Math.Round((decimal)measure.GlucoseLevelRaw / 10) + calibrationOffset;
                measure.GlucoseLevelMMOL = (float)FreeStyleLibreUtils.ConvertMGDLToMMolPerLiter(appSettings, measure.GlucoseLevelMGDL);
                trans.Commit();
            }

            return measure;
        }

        /// <summary>
        /// Update the userId measure for a glucose measure
        /// </summary>
        /// <param name="measure"></param>
        /// <param name="userId"></param>
        public void ChangeUserId(GlucoseMeasure measure, string userId)
        {
            using (var trans = realm.BeginWrite())
            {
                measure.UserId = userId;
                trans.Commit();
            }
        }

        /// <summary>
        /// Update the userId measure for a glucose measure
        /// </summary>
        /// <param name="measure"></param>
        /// <param name="isInTheMedicalZone"></param>
        public void ChangeIsInTheMedicalZone(GlucoseMeasure measure, bool isInTheMedicalZone)
        {
            using (var trans = realm.BeginWrite())
            {
                measure.InTheMedicalZone = isInTheMedicalZone;
                trans.Commit();
            }
        }
    }
}

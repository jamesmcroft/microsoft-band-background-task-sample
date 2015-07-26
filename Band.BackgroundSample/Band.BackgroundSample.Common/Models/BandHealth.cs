// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandHealth.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// <summary>
//   Defines the BandHealth type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample.Common.Models
{
    using System;

    using Microsoft.Band.Sensors;

    /// <summary>
    /// The Band health model.
    /// </summary>
    public class BandHealth
    {
        /// <summary>
        /// Gets or sets the pace.
        /// </summary>
        public double Pace { get; set; }

        /// <summary>
        /// Gets or sets the speed.
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// Gets or sets the total distance.
        /// </summary>
        public long TotalDistance { get; set; }

        /// <summary>
        /// Gets or sets the motion type.
        /// </summary>
        public MotionType MotionType { get; set; }

        /// <summary>
        /// Gets or sets the heart rate.
        /// </summary>
        public int HeartRate { get; set; }

        /// <summary>
        /// Gets or sets the skin temperature.
        /// </summary>
        public double SkinTemperature { get; set; }

        /// <summary>
        /// Gets or sets the captured at date.
        /// </summary>
        public DateTime CapturedAt { get; set; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var health = obj as BandHealth;
            if (health == null)
            {
                return false;
            }

            return this.HeartRate == health.HeartRate && this.SkinTemperature == health.SkinTemperature
                   && this.MotionType == health.MotionType && this.Pace == health.Pace && this.Speed == health.Speed;
        }

        /// <summary>
        /// Creates a clone of the object.
        /// </summary>
        /// <returns>
        /// The <see cref="BandHealth"/>.
        /// </returns>
        public BandHealth Clone()
        {
            var bandHealth = new BandHealth
            {
                CapturedAt = this.CapturedAt,
                MotionType = this.MotionType,
                Pace = this.Pace,
                Speed = this.Speed,
                HeartRate = this.HeartRate,
                SkinTemperature = this.SkinTemperature,
            };

            return bandHealth;
        }

        /// <summary>
        /// Serves as the default hash function. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandData.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// <summary>
//   Defines the BandData type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample.Common.Models
{
    using System;

    using Microsoft.Band.Sensors;

    /// <summary>
    /// The Band data model.
    /// </summary>
    public class BandData
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
        /// Creates a clone of the object.
        /// </summary>
        /// <returns>
        /// The <see cref="BandData"/>.
        /// </returns>
        public BandData Clone()
        {
            var bandHealth = new BandData
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
    }
}

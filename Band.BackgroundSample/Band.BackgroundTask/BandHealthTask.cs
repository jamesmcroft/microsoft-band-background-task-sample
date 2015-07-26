// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandHealthTask.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Band.BackgroundSample.Common;
    using Band.BackgroundSample.Common.Models;

    using Microsoft.Band;
    using Microsoft.Band.Sensors;

    using Newtonsoft.Json;

    using Windows.ApplicationModel.Background;
    using Windows.Storage;

    /// <summary>
    ///     The band health background task.
    /// </summary>
    public sealed class BandHealthTask : IBackgroundTask, IDisposable
    {
        private const string FileName = "UserHealth.json";

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private IBandClient _band;

        private BandHealth _bandHealth;

        private BackgroundTaskDeferral _deferral;

        private IBandInfo _deviceInfo;

        private bool _distanceAcquired;

        private bool _heartRateAcquired;

        private bool _isContactRunning;

        private bool _isDistanceRunning;

        private bool _isHealthSaving;

        private bool _isHeartRateRunning;

        private bool _isSkinTemperatureRunning;

        private BandContactState _previousState;

        private bool _skinTemperatureAcquired;

        private Timer _timer;

        private List<BandHealth> RecordedHealth { get; set; }

        /// <summary>
        ///     Gets a value indicating whether all the data has been acquired.
        /// </summary>
        public bool IsDataAcquired
        {
            get
            {
                if (!this._isDistanceRunning)
                {
                    this._distanceAcquired = true;
                }

                if (!this._isHeartRateRunning)
                {
                    this._heartRateAcquired = true;
                }

                if (!this._isSkinTemperatureRunning)
                {
                    this._skinTemperatureAcquired = true;
                }

                return this._distanceAcquired && this._heartRateAcquired && this._skinTemperatureAcquired;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether any sensors are running.
        /// </summary>
        public bool AnySensorsRunning
        {
            get
            {
                return this._isDistanceRunning || this._isHeartRateRunning || this._isSkinTemperatureRunning;
            }
        }

        /// <summary>
        /// Background task entry point.
        /// </summary>
        /// <param name="taskInstance">
        /// The task instance.
        /// </param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            this._deferral = taskInstance.GetDeferral();

            taskInstance.Canceled += this.OnTaskCanceled;

            try
            {
                await this.SetupBand();
            }
            catch (Exception)
            {
                this.CompleteDeferral();
            }
        }

        private async Task SetupBand()
        {
            try
            {
                // Loads in any previously saved health data
                this.RecordedHealth = await LoadRecordedHealth();
            }
            catch (Exception)
            {
                this.RecordedHealth = new List<BandHealth>();
            }

            // Gets the first available Band from the device
            this._deviceInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

            if (this._deviceInfo == null)
            {
                throw new InvalidOperationException("No Microsoft Band available to connect to.");
            }

            var isConnecting = false;

            using (new DisposableAction(() => isConnecting = true, () => isConnecting = false))
            {
                // Attempts to connect to the Band
                this._band = await BandClientManager.Instance.ConnectAsync(this._deviceInfo);

                if (this._band == null)
                {
                    throw new InvalidOperationException("Could not connect to the Microsoft Band available.");
                }

                // This sample uses the Heart Rate sensor so the user must have granted access prior to the BG task running
                if (this._band.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                {
                    throw new InvalidOperationException(
                        "User has not granted access to the Microsoft Band heart rate sensor.");
                }

                this.ResetAcquiredFlags();

                this._bandHealth = new BandHealth();

                await this.SetupBandSensors();
            }
        }

        private async void SendData(object state)
        {
            await this.SendData();
        }

        private async Task SendData()
        {
            try
            {
                if (await this.BandNotWorn())
                {
                    await this.UpdateContactState(BandContactState.NotWorn);
                    return;
                }
            }
            catch (BandException)
            {
                this.CompleteDeferral();
                return;
            }

            if (this._isHealthSaving)
            {
                return;
            }

            if (this.IsDataAcquired)
            {
                this._isHealthSaving = true;

                this._bandHealth.CapturedAt = DateTime.Now;

                var health = this._bandHealth.Clone();

                if (this.RecordedHealth != null)
                {
                    if (!this.RecordedHealth.Contains(health))
                    {
                        this.RecordedHealth.Add(health);
                    }
                }

                await this.SaveRecordedHealth();
            }

            this._isHealthSaving = false;
        }

        private void OnSkinTemperatureChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            if (this._isHealthSaving)
            {
                return;
            }

            this._bandHealth.SkinTemperature = e.SensorReading.Temperature;
            this._skinTemperatureAcquired = true;
        }

        private void OnDistanceChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            if (this._isHealthSaving)
            {
                return;
            }

            this._bandHealth.MotionType = e.SensorReading.CurrentMotion;

            this._bandHealth.Pace = e.SensorReading.Pace;
            this._bandHealth.Speed = e.SensorReading.Speed;
            this._bandHealth.TotalDistance = e.SensorReading.TotalDistance;

            this._distanceAcquired = true;
        }

        private void OnHeartRateChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            if (this._isHealthSaving)
            {
                return;
            }

            this._bandHealth.HeartRate = e.SensorReading.HeartRate;
            this._heartRateAcquired = true;
        }

        private async Task<bool> BandNotWorn()
        {
            var bandContact = await this._band.SensorManager.Contact.GetCurrentStateAsync();

            if (bandContact.State == BandContactState.NotWorn)
            {
                return true;
            }

            return false;
        }

        private async Task SetupBandSensors()
        {
            // Band sensors don't always read at the same time so we have our own timer to acquire data at a point in time.
            this._timer = new Timer(this.SendData, null, Timeout.Infinite, Timeout.Infinite);

            try
            {
                // Check if this band isn't being worn.
                if (await this.BandNotWorn())
                {
                    // If so, stop the timer
                    this._timer.Change(Timeout.Infinite, Timeout.Infinite);

                    // If any sensors are running, stop those
                    if (this.AnySensorsRunning)
                    {
                        await this.StopDataSensors();
                    }

                    // Check the contact sensor is running to check when the Band is re-attached to the wearer
                    if (!this._isContactRunning)
                    {
                        await this.StartContactSensor();
                    }
                }
                else
                {
                    // If the Band is worn, update the contact state
                    await this.UpdateContactState(BandContactState.Worn);
                }
            }
            catch (BandException)
            {
                this.CompleteDeferral();
            }
        }

        private async Task StartContactSensor()
        {
            try
            {
                this._band.SensorManager.Contact.ReadingChanged += this.OnContactChanged;

                await this._band.SensorManager.Contact.StartReadingsAsync();
                this._isContactRunning = true;
            }
            catch (Exception)
            {
                this._isContactRunning = false;
            }
        }

        private async Task StartDataSensors()
        {
            try
            {
                this._band.SensorManager.HeartRate.ReadingChanged += this.OnHeartRateChanged;

                await this._band.SensorManager.HeartRate.StartReadingsAsync();
                this._isHeartRateRunning = true;
            }
            catch (Exception)
            {
                this._isHeartRateRunning = false;
            }

            try
            {
                this._band.SensorManager.Distance.ReadingChanged += this.OnDistanceChanged;

                await this._band.SensorManager.Distance.StartReadingsAsync();
                this._isDistanceRunning = true;
            }
            catch (Exception)
            {
                this._isDistanceRunning = false;
            }

            try
            {
                this._band.SensorManager.SkinTemperature.ReadingChanged += this.OnSkinTemperatureChanged;

                await this._band.SensorManager.SkinTemperature.StartReadingsAsync();
                this._isSkinTemperatureRunning = true;
            }
            catch (Exception)
            {
                this._isSkinTemperatureRunning = false;
            }
        }

        private async void OnContactChanged(object sender, BandSensorReadingEventArgs<IBandContactReading> e)
        {
            await this.UpdateContactState(e.SensorReading.State);
        }

        private async Task UpdateContactState(BandContactState e)
        {
            // Check that our new state isn't the same as the old
            if (this._previousState == e)
            {
                return;
            }

            // If the Band isn't worn
            if (e == BandContactState.NotWorn)
            {
                // Stop the timer
                this._timer.Change(Timeout.Infinite, Timeout.Infinite);

                // Stop any data sensors
                if (this.AnySensorsRunning)
                {
                    await this.StopDataSensors();
                }

                // Start the contact sensor
                if (!this._isContactRunning)
                {
                    await this.StartContactSensor();
                }
            }
            else if (e == BandContactState.Worn) 
            {
                // If the Band is worn

                // Stop running the contact sensor
                if (this._isContactRunning)
                {
                    await this.StopContactSensor();
                }

                // Check that the sensors aren't running and start them
                if (!this.AnySensorsRunning)
                {
                    await this.StartDataSensors();
                }

                // Set the timer going again
                this._timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }

            this._previousState = e;
        }

        private void ResetAcquiredFlags()
        {
            this._heartRateAcquired = false;
            this._distanceAcquired = false;
            this._skinTemperatureAcquired = false;
        }

        #region Microsoft Band & Task Clean-up

        private async void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            await this.CompleteDeferral();
        }

        private async Task CompleteDeferral()
        {
            try
            {
                await this.SaveRecordedHealth();
            }
            catch (Exception ex)
            {
                // Handle saving failure
            }

            if (this._band != null)
            {
                await this.CleanupBandSensors();

                this._band.Dispose();
                this._band = null;
            }

            BackgroundTaskProvider.UnregisterBandHealthTask();

            this._deferral.Complete();
        }

        private static async Task<List<BandHealth>> LoadRecordedHealth()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            var file = files.FirstOrDefault(f => f.Name == FileName);
            if (file != null)
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                if (buffer != null)
                {
                    using (var reader = new JsonTextReader(new StreamReader(buffer.AsStream())))
                    {
                        var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };

                        return serializer.Deserialize<List<BandHealth>>(reader);
                    }
                }
            }

            return new List<BandHealth>();
        }

        private async Task SaveRecordedHealth()
        {
            await this._semaphore.WaitAsync();

            try
            {
                var json = JsonConvert.SerializeObject(
                    this.RecordedHealth,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                var bytes = Encoding.UTF8.GetBytes(json);

                var file =
                    await
                    ApplicationData.Current.LocalFolder.CreateFileAsync(
                        FileName,
                        CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteBytesAsync(file, bytes);
            }
            catch
            {
            }
            finally
            {
                this._semaphore.Release();
            }
        }

        private async Task CleanupBandSensors()
        {
            await this.StopContactSensor();

            await this.StopDataSensors();
        }

        private async Task StopContactSensor()
        {
            try
            {
                await this._band.SensorManager.Contact.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this._band.SensorManager.Contact.ReadingChanged -= this.OnContactChanged;
                this._isContactRunning = false;
            }
        }

        private async Task StopDataSensors()
        {
            try
            {
                await this._band.SensorManager.HeartRate.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this._band.SensorManager.HeartRate.ReadingChanged -= this.OnHeartRateChanged;
                this._isHeartRateRunning = false;
            }

            try
            {
                await this._band.SensorManager.Distance.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this._band.SensorManager.Distance.ReadingChanged -= this.OnDistanceChanged;
                this._isDistanceRunning = false;
            }

            try
            {
                await this._band.SensorManager.SkinTemperature.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this._band.SensorManager.SkinTemperature.ReadingChanged -= this.OnSkinTemperatureChanged;
                this._isSkinTemperatureRunning = false;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public async void Dispose()
        {
            if (null != this._band)
            {
                await this.CompleteDeferral();
            }
        }

        #endregion
    }
}
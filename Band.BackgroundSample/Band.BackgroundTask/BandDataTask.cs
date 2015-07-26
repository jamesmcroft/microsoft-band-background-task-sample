// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandDataTask.cs" company="James Croft">
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
    public sealed class BandDataTask : IBackgroundTask, IDisposable
    {
        private const string FileName = "BandData.json";

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private IBandClient bandClient;

        private BandData bandData;

        private BackgroundTaskDeferral deferral;

        private IBandInfo bandInfo;

        private bool isDistanceReceived;

        private bool isHeartRateReceived;

        private bool isSkinTemperatureReceived;

        private bool isDistanceOn;

        private bool isHeartRateOn;

        private bool isSkinTemperatureOn;

        private bool isDataSaving;

        private Timer recordingTimer;

        private List<BandData> RecordedData { get; set; }

        /// <summary>
        /// Gets a value that indicates whether all the data has been received.
        /// </summary>
        public bool IsDataReceived
        {
            get
            {
                if (!this.isDistanceOn)
                {
                    this.isDistanceReceived = true;
                }

                if (!this.isHeartRateOn)
                {
                    this.isHeartRateReceived = true;
                }

                if (!this.isSkinTemperatureOn)
                {
                    this.isSkinTemperatureReceived = true;
                }

                return this.isDistanceReceived && this.isHeartRateReceived && this.isSkinTemperatureReceived;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether any sensor is currently on.
        /// </summary>
        public bool AnySensorsOn
        {
            get
            {
                return this.isDistanceOn || this.isHeartRateOn || this.isSkinTemperatureOn;
            }
        }

        /// <summary>
        /// The background tasks entry point when it is running.
        /// </summary>
        /// <param name="taskInstance">
        /// The task instance which trigger has executed this method.
        /// </param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            this.deferral = taskInstance.GetDeferral();

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
                this.RecordedData = await LoadRecordedData();
            }
            catch (Exception)
            {
                this.RecordedData = new List<BandData>();
            }

            // Gets the first available Band from the device
            this.bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

            if (this.bandInfo == null)
            {
                throw new InvalidOperationException("No Microsoft Band available to connect to.");
            }

            var isConnecting = false;

            using (new DisposableAction(() => isConnecting = true, () => isConnecting = false))
            {
                // Attempts to connect to the Band
                this.bandClient = await BandClientManager.Instance.ConnectAsync(this.bandInfo);

                if (this.bandClient == null)
                {
                    throw new InvalidOperationException("Could not connect to the Microsoft Band available.");
                }

                // This sample uses the Heart Rate sensor so the user must have granted access prior to the BG task running
                if (this.bandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                {
                    throw new InvalidOperationException(
                        "User has not granted access to the Microsoft Band heart rate sensor.");
                }

                this.ResetReceivedFlags();

                this.bandData = new BandData();

                await this.SetupSensors();
            }
        }

        private async void SaveData(object state)
        {
            await this.SaveData();
        }

        private async Task SaveData()
        {
            if (this.isDataSaving)
            {
                return;
            }

            if (this.IsDataReceived)
            {
                this.isDataSaving = true;

                this.bandData.CapturedAt = DateTime.Now;

                var health = this.bandData.Clone();

                if (this.RecordedData != null)
                {
                    if (!this.RecordedData.Contains(health))
                    {
                        this.RecordedData.Add(health);
                    }
                }

                await this.SaveRecordedHealth();
            }

            this.isDataSaving = false;
        }

        private void OnSkinTemperatureChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            if (this.isDataSaving)
            {
                return;
            }

            this.bandData.SkinTemperature = e.SensorReading.Temperature;
            this.isSkinTemperatureReceived = true;
        }

        private void OnDistanceChanged(object sender, BandSensorReadingEventArgs<IBandDistanceReading> e)
        {
            if (this.isDataSaving)
            {
                return;
            }

            this.bandData.MotionType = e.SensorReading.CurrentMotion;

            this.bandData.Pace = e.SensorReading.Pace;
            this.bandData.Speed = e.SensorReading.Speed;
            this.bandData.TotalDistance = e.SensorReading.TotalDistance;

            this.isDistanceReceived = true;
        }

        private void OnHeartRateChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            if (this.isDataSaving)
            {
                return;
            }

            this.bandData.HeartRate = e.SensorReading.HeartRate;
            this.isHeartRateReceived = true;
        }

        private async Task SetupSensors()
        {
            // Band sensors don't always read at the same time so we have our own timer to acquire data at a point in time.
            this.recordingTimer = new Timer(this.SaveData, null, Timeout.Infinite, Timeout.Infinite);

            try
            {
                if (this.AnySensorsOn)
                {
                    await this.StopSensorsRunning();
                }

                await this.StartSensorsRunning();

                // Set the timer going
                this.recordingTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }
            catch (BandException)
            {
                this.CompleteDeferral();
            }
        }

        private async Task StartSensorsRunning()
        {
            try
            {
                this.bandClient.SensorManager.HeartRate.ReadingChanged += this.OnHeartRateChanged;

                await this.bandClient.SensorManager.HeartRate.StartReadingsAsync();
                this.isHeartRateOn = true;
            }
            catch (Exception)
            {
                this.isHeartRateOn = false;
            }

            try
            {
                this.bandClient.SensorManager.Distance.ReadingChanged += this.OnDistanceChanged;

                await this.bandClient.SensorManager.Distance.StartReadingsAsync();
                this.isDistanceOn = true;
            }
            catch (Exception)
            {
                this.isDistanceOn = false;
            }

            try
            {
                this.bandClient.SensorManager.SkinTemperature.ReadingChanged += this.OnSkinTemperatureChanged;

                await this.bandClient.SensorManager.SkinTemperature.StartReadingsAsync();
                this.isSkinTemperatureOn = true;
            }
            catch (Exception)
            {
                this.isSkinTemperatureOn = false;
            }
        }

        private void ResetReceivedFlags()
        {
            this.isHeartRateReceived = false;
            this.isDistanceReceived = false;
            this.isSkinTemperatureReceived = false;
        }

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

            if (this.bandClient != null)
            {
                await this.StopSensorsRunning();

                this.bandClient.Dispose();
                this.bandClient = null;
            }

            BackgroundTaskProvider.UnregisterBandDataTask();

            this.deferral.Complete();
        }

        private static async Task<List<BandData>> LoadRecordedData()
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

                        return serializer.Deserialize<List<BandData>>(reader);
                    }
                }
            }

            return new List<BandData>();
        }

        private async Task SaveRecordedHealth()
        {
            await this.semaphore.WaitAsync();

            try
            {
                var json = JsonConvert.SerializeObject(
                    this.RecordedData,
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
                this.semaphore.Release();
            }
        }

        private async Task StopSensorsRunning()
        {
            try
            {
                await this.bandClient.SensorManager.HeartRate.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this.bandClient.SensorManager.HeartRate.ReadingChanged -= this.OnHeartRateChanged;
                this.isHeartRateOn = false;
            }

            try
            {
                await this.bandClient.SensorManager.Distance.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this.bandClient.SensorManager.Distance.ReadingChanged -= this.OnDistanceChanged;
                this.isDistanceOn = false;
            }

            try
            {
                await this.bandClient.SensorManager.SkinTemperature.StopReadingsAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                this.bandClient.SensorManager.SkinTemperature.ReadingChanged -= this.OnSkinTemperatureChanged;
                this.isSkinTemperatureOn = false;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public async void Dispose()
        {
            if (null != this.bandClient)
            {
                await this.CompleteDeferral();
            }
        }
    }
}
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundTaskProvider.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample.Common
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Windows.ApplicationModel.Background;

    /// <summary>
    /// The background task provider.
    /// </summary>
    public static class BackgroundTaskProvider
    {
        private const string BandDataTaskId = "BandDataTask";

        /// <summary>
        /// Gets the device use trigger.
        /// </summary>
        public static DeviceUseTrigger DeviceUseTrigger { get; private set; }

        /// <summary>
        /// Gets a value that indicates whether the BandDataTask is registered.
        /// </summary>
        public static bool IsBandDataTaskRegistered
        {
            get
            {
                return BandDataTask != null;
            }
        }

        /// <summary>
        /// Gets the BandDataTask from the BackgroundTaskRegistration provider.
        /// </summary>
        public static IBackgroundTaskRegistration BandDataTask
        {
            get
            {
                var task = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == BandDataTaskId).Value;
                return task;
            }
        }

        /// <summary>
        /// Registers the BandDataTask with the BackgroundTaskRegistration provider.
        /// </summary>
        /// <param name="taskName">
        /// The name of the task to register. (E.g. typeof(Task).FullName)
        /// </param>
        /// <param name="deviceId">
        /// The device id to register with the DeviceUseTrigger.
        /// </param>
        /// <returns>
        /// Returns a bool whether the task has registered.
        /// </returns>
        public static async Task<bool> RegisterBandDataTask(string taskName, string deviceId)
        {
            try
            {
                if (IsBandDataTaskRegistered)
                {
                    UnregisterBandDataTask();
                }

                var access = await BackgroundExecutionManager.RequestAccessAsync();

                if ((access == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity)
                    || (access == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity))
                {
                    await BuildBandDataTask(taskName, deviceId);
                    return true;
                }
            }
            catch (Exception)
            {
                UnregisterBandDataTask();
            }

            return false;
        }

        /// <summary>
        /// Unregisters the BandDataTask from the BackgroundTaskRegistration provider.
        /// </summary>
        public static void UnregisterBandDataTask()
        {
            if (IsBandDataTaskRegistered)
            {
                BandDataTask.Unregister(false);
            }
        }

        private static async Task BuildBandDataTask(string taskName, string deviceId)
        {
            var taskBuilder = new BackgroundTaskBuilder { Name = BandDataTaskId, TaskEntryPoint = taskName };

            DeviceUseTrigger = new DeviceUseTrigger();

            taskBuilder.SetTrigger(DeviceUseTrigger);

            taskBuilder.Register();

            var triggerResult = await DeviceUseTrigger.RequestAsync(deviceId);

            switch (triggerResult)
            {
                case DeviceTriggerResult.Allowed:
                    return;
                case DeviceTriggerResult.DeniedByUser:
                    throw new InvalidOperationException("Cannot start the background task. Access denied by user.");
                case DeviceTriggerResult.DeniedBySystem:
                    throw new InvalidOperationException("Cannot start the background task. Access denied by system.");
                case DeviceTriggerResult.LowBattery:
                    throw new InvalidOperationException("Cannot start the background task. Low battery.");
            }
        }
    }
}
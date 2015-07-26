// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundTaskProvider.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// <summary>
//   Defines the BackgroundTaskProvider type.
// </summary>
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
        private const string BandHealthTaskId = "BandHealthTask";

        /// <summary>
        /// Gets the device use trigger.
        /// </summary>
        public static DeviceUseTrigger DeviceUseTrigger { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the Band health task is registered.
        /// </summary>
        public static bool IsBandHealthTaskRegistered
        {
            get
            {
                return BandHealthTask != null;
            }
        }

        /// <summary>
        /// Gets the Band health task.
        /// </summary>
        public static IBackgroundTaskRegistration BandHealthTask
        {
            get
            {
                var task = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == BandHealthTaskId).Value;
                return task;
            }
        }

        /// <summary>
        /// Registers the Band health task.
        /// </summary>
        /// <param name="taskName">
        /// The task name.
        /// </param>
        /// <param name="deviceId">
        /// The device id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task<bool> RegisterBandHealthTask(string taskName, string deviceId)
        {
            try
            {
                if (IsBandHealthTaskRegistered)
                {
                    UnregisterBandHealthTask();
                }

                var access = await BackgroundExecutionManager.RequestAccessAsync();

                if ((access == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity) || (access == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity))
                {
                    await StartBandHealthTask(taskName, deviceId);
                    return true;
                }
            }
            catch (Exception)
            {
                UnregisterBandHealthTask();
            }

            return false;
        }

        /// <summary>
        /// Unregisters the Band health task.
        /// </summary>
        public static void UnregisterBandHealthTask()
        {
            if (IsBandHealthTaskRegistered)
            {
                BandHealthTask.Unregister(false);
            }
        }

        private static async Task StartBandHealthTask(string taskName, string deviceId)
        {
            var taskBuilder = new BackgroundTaskBuilder { Name = BandHealthTaskId, TaskEntryPoint = taskName };

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

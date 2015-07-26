// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandHealthTask.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundTask
{
    using System;

    using Windows.ApplicationModel.Background;

    /// <summary>
    /// The band health background task.
    /// </summary>
    public sealed class BandHealthTask : IBackgroundTask, IDisposable
    {
        /// <summary>
        /// Runs the background task.
        /// </summary>
        /// <param name="taskInstance">
        /// The task instance responsible for running this code.
        /// </param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {

        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}
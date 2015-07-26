// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DisposableAction.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample.Common
{
    using System;

    /// <summary>
    /// The disposable action class.
    /// </summary>
    public class DisposableAction : IDisposable
    {
        private Action dispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction"/> class.
        /// </summary>
        /// <param name="dispose">
        /// The dispose.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the dispose action is null.
        /// </exception>
        public DisposableAction(Action dispose)
        {
            if (dispose == null) throw new ArgumentNullException("dispose");

            this.dispose = dispose;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction"/> class.
        /// </summary>
        /// <param name="construct">
        /// The construct.
        /// </param>
        /// <param name="dispose">
        /// The dispose.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="construct"/> and/or <paramref name="dispose"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="Exception">
        /// A delegate callback could throw an exception.
        /// </exception>
        public DisposableAction(Action construct, Action dispose)
        {
            if (construct == null) throw new ArgumentNullException("construct");
            if (dispose == null) throw new ArgumentNullException("dispose");

            construct();

            this.dispose = dispose;
        }

        /// <summary>    
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.    
        /// </summary>    
        /// <filterpriority>2</filterpriority>    
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Calls the dispose action.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.dispose == null)
                {
                    return;
                }

                this.dispose();

                this.dispose = null;
            }
        }
    }
}

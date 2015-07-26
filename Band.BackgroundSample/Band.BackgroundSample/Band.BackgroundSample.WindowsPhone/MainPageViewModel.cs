// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainPageViewModel.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Windows.Devices.Bluetooth.Rfcomm;
    using Windows.Devices.Enumeration;
    using Windows.UI.Popups;

    using Band.BackgroundSample.Common;
    using Band.BackgroundTask;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using Microsoft.Band;

    /// <summary>
    /// The main page view model.
    /// </summary>
    public class MainPageViewModel : ViewModelBase
    {
        private bool isBandRegistered;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
        /// </summary>
        public MainPageViewModel()
        {
            this.RegisterBandCommand = new RelayCommand(async () => await this.RegisterBand());
        }

        /// <summary>
        /// Gets the register band command.
        /// </summary>
        public ICommand RegisterBandCommand { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether is band registered.
        /// </summary>
        public bool IsBandRegistered
        {
            get
            {
                return this.isBandRegistered;
            }
            set
            {
                this.Set(() => this.IsBandRegistered, ref this.isBandRegistered, value);
            }
        }

        private async Task RegisterBand()
        {
            bool backgroundRegistered = false;

            var bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

            var consentGranted = await GetConsentForHeartRate(bandInfo);

            if (consentGranted.HasValue && consentGranted.Value)
            {
                // The Guid used for the RfcommServiceId is from the Package.appxmanifest. 
                var device =
                    (await
                     DeviceInformation.FindAllAsync(
                         RfcommDeviceService.GetDeviceSelector(
                             RfcommServiceId.FromUuid(new Guid("A502CA9A-2BA5-413C-A4E0-13804E47B38F")))))
                        .FirstOrDefault(x => x.Name == bandInfo.Name);

                backgroundRegistered = device != null
                                       && await
                                          BackgroundTaskProvider.RegisterBandDataTask(
                                              typeof(BandDataTask).FullName,
                                              device.Id);
            }

            if (backgroundRegistered)
            {
                var success =
                    new MessageDialog(
                        "The BandDataTask has been registered successfully and the app can be closed.",
                        "Background task registered");

                success.Commands.Add(new UICommand("Ok", command => { this.IsBandRegistered = true; }));

                await success.ShowAsync();
            }
            else
            {
                MessageDialog error;

                if (consentGranted.HasValue && !consentGranted.Value)
                {
                    error =
                        new MessageDialog(
                            "The BandDataTask was not registered as you have rejected consent to access the heart rate sensor. Please try again.",
                            "Background task not registered");
                }
                else
                {
                    error =
                        new MessageDialog(
                            "The BandDataTask was not registered successfully. Check your Microsoft Band is connected and try again.",
                            "Background task not registered");
                }

                error.Commands.Add(new UICommand("Ok", command => { }));

                await error.ShowAsync();
            }
        }

        private static async Task<bool?> GetConsentForHeartRate(IBandInfo bandInfo)
        {
            bool consentGranted;

            IBandClient bandClient = null;

            bool isRunning = false;

            if (bandInfo != null)
            {
                using (new DisposableAction(() => isRunning = true, () => isRunning = false))
                {
                    try
                    {
                        bandClient = await BandClientManager.Instance.ConnectAsync(bandInfo);
                    }
                    catch (Exception)
                    {
                        // Handle exception
                    }

                    if (bandClient != null)
                    {
                        if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                        {
                            consentGranted = await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                        }
                        else
                        {
                            consentGranted = true;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }

            bandClient.Dispose();

            return consentGranted;
        }
    }
}
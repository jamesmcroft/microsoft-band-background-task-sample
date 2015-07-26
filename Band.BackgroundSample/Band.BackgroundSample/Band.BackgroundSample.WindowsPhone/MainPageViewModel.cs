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
        private bool _isBandRegistered;

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
                return this._isBandRegistered;
            }
            set
            {
                this.Set(() => this.IsBandRegistered, ref this._isBandRegistered, value);
            }
        }

        private async Task RegisterBand()
        {
            bool backgroundRegistered = false;

            var bandInfo = (await BandClientManager.Instance.GetBandsAsync()).FirstOrDefault();

            var consentGranted = await GetHeartRateConsent(bandInfo);

            if (consentGranted.HasValue && consentGranted.Value)
            {
                // RfCommServiceId has come from the Guid in the Package.appxmanifest. There must be a better way of doing this.
                var device = (await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(new Guid("A502CA9A-2BA5-413C-A4E0-13804E47B38F"))))).FirstOrDefault(x => x.Name == bandInfo.Name);

                backgroundRegistered = device != null
                                       && await
                                          BackgroundTaskProvider.RegisterBandHealthTask(
                                              typeof(BandHealthTask).FullName,
                                              device.Id);
            }

            if (backgroundRegistered)
            {
                var successDialog =
                    new MessageDialog(
                        "Microsoft Band background tasks have been registered and the app can now be closed.",
                        "Background Tasks Registered Successfully.");

                successDialog.Commands.Add(new UICommand("Ok", command => { this.IsBandRegistered = true; }));

                await successDialog.ShowAsync();
            }
            else
            {
                MessageDialog failDialog;

                if (consentGranted.HasValue && !consentGranted.Value)
                {
                    failDialog = new MessageDialog("Microsoft Band background tasks have not been registered as you have rejected consent to access the heart rate sensor. If this action was done in error, please try again.", "Background Tasks Not Registered.");
                }
                else
                {
                    failDialog =
                        new MessageDialog(
                            "Microsoft Band background tasks have not registered correctly. Check your Microsoft Band is connected and try again.",
                            "Background Tasks Failed To Register.");
                }

                failDialog.Commands.Add(new UICommand("Ok", command => { }));

                await failDialog.ShowAsync();
            }
        }

        private static async Task<bool?> GetHeartRateConsent(IBandInfo bandInfo)
        {
            bool granted = false;

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
                            granted = await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                        }
                        else
                        {
                            granted = true;
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
            bandClient = null;

            return granted;
        }
    }
}

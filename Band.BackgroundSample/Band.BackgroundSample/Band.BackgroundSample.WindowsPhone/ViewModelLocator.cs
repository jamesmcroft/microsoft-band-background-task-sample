// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelLocator.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample
{
    using GalaSoft.MvvmLight.Ioc;

    using Microsoft.Practices.ServiceLocation;

    /// <summary>
    /// The view model locator.
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainPageViewModel>();
        }

        /// <summary>
        /// Gets the main page view model.
        /// </summary>
        public MainPageViewModel MainPageViewModel
        {
            get
            {
                return SimpleIoc.Default.GetInstance<MainPageViewModel>();
            }
        }
    }
}

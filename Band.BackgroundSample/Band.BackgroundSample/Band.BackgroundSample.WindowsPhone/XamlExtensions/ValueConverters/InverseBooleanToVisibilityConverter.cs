// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InverseBooleanToVisibilityConverter.cs" company="James Croft">
//   Copyright (c) James Croft 2015.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Band.BackgroundSample.XamlExtensions.ValueConverters
{
    using System;

    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;

    /// <summary>
    /// The inverse boolean to visibility converter.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    { 
        /// <summary>
        /// Convert from boolean to visibility
        /// </summary>
        /// <param name="value">element value</param>
        /// <param name="targetType">target type</param>
        /// <param name="parameter">input parameter</param>
        /// <param name="language">specified language</param>
        /// <returns>Converted Visibility</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool? val = value as bool?;
            if (val == null)
                return null;

            if (!val.Value)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Convert from visibility to boolean
        /// </summary>
        /// <param name="value">element value</param>
        /// <param name="targetType">target type</param>
        /// <param name="parameter">input parameter</param>
        /// <param name="language">specified language</param>
        /// <returns>Converted Boolean</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var val = value as Visibility?;
            if (val == null)
                return null;

            return val.Value != Visibility.Visible;
        }
    }
}

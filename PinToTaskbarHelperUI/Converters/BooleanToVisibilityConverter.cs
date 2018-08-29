using System;
using System.Windows;

namespace PinToTaskbarHelperUI.Converters
{
    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        {

        }
    }
}
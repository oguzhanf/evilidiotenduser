using EvilEndUser.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EvilEndUser.App.Converters;

/// <summary>Maps an <see cref="EventSeverity"/> to a colour brush for the activity log.</summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value is EventSeverity sev
            ? sev switch
            {
                EventSeverity.Error => Color.FromArgb(255, 232, 17, 35),
                EventSeverity.Warning => Color.FromArgb(255, 247, 99, 12),
                EventSeverity.Telemetry => Color.FromArgb(255, 255, 185, 0),
                EventSeverity.Info => Color.FromArgb(255, 96, 205, 255),
                _ => Colors.Gray,
            }
            : Colors.Gray;

        return new SolidColorBrush(color);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>Maps a <see cref="RiskLevel"/> to a colour brush for badges.</summary>
public sealed class RiskToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value is RiskLevel risk
            ? risk switch
            {
                RiskLevel.Critical => Color.FromArgb(255, 197, 15, 31),
                RiskLevel.High => Color.FromArgb(255, 232, 17, 35),
                RiskLevel.Medium => Color.FromArgb(255, 247, 99, 12),
                RiskLevel.Low => Color.FromArgb(255, 255, 185, 0),
                _ => Color.FromArgb(255, 16, 137, 62),
            }
            : Colors.Gray;

        return new SolidColorBrush(color);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

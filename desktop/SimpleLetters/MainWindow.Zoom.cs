using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleLetters;

// Page zoom: how big the page looks on screen. It doesn't change the letter
// itself, its text sizes or how it prints.
public partial class MainWindow
{
    static readonly double[] ZoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5, 1.75, 2.0];

    double zoom = 1;

    void SetZoom(double value, bool remember)
    {
        zoom = Math.Clamp(value, ZoomSteps[0], ZoomSteps[^1]);
        DeskContent.LayoutTransform = Math.Abs(zoom - 1) < 0.001 ? Transform.Identity : new ScaleTransform(zoom, zoom);
        ZoomLabel.Text = $"{zoom * 100:0}%";
        ZoomOutButton.IsEnabled = zoom > ZoomSteps[0] + 0.001;
        ZoomInButton.IsEnabled = zoom < ZoomSteps[^1] - 0.001;
        if (remember)
        {
            settings.Zoom = zoom;
            settings.Save();
        }
    }

    void StepZoom(int direction)
    {
        double next = direction > 0
            ? ZoomSteps.FirstOrDefault(z => z > zoom + 0.001, ZoomSteps[^1])
            : ZoomSteps.LastOrDefault(z => z < zoom - 0.001, ZoomSteps[0]);
        SetZoom(next, remember: true);
    }

    void ZoomIn_Click(object sender, RoutedEventArgs e) => StepZoom(1);
    void ZoomOut_Click(object sender, RoutedEventArgs e) => StepZoom(-1);

    /// <summary>Ctrl + mouse wheel zooms the page (anywhere on the desk, including over the text).</summary>
    void Desk_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        StepZoom(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }
}

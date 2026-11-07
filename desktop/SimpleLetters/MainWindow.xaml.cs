using System.ComponentModel;
using System.IO;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SimpleLetters;

public partial class MainWindow : Window
{
    // A4, in millimetres.
    const double PageWidthMm = 210;
    const double PageHeightMm = 297;
    // Margins never squeeze the writing area below this.
    const double MinTextMm = 40;

    const double MinFontPt = 6;
    const double MaxFontPt = 72;

    const string FileFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

    static double Dip(double mm) => mm * 96.0 / 25.4;
    static double Mm(double dip) => dip * 25.4 / 96.0;
    static double PtToDip(double pt) => pt * 96.0 / 72.0;

    static readonly Brush GuideBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0x8C, 0x48)));

    readonly Settings settings = Settings.Load();
    readonly Dictionary<Side, MarginHandle> handles = new();
    readonly Dictionary<Side, Line> guides = new();
    readonly Border valueLabel;
    readonly TextBlock valueText;

    string? filePath;
    bool dirty;
    bool settingText;

    // Dragging a margin arrow.
    Side? dragSide;
    bool dragMoved;
    Point dragStart;

    public MainWindow()
    {
        InitializeComponent();

        Page.Width = PageShadow.Width = Dip(PageWidthMm);
        Page.Height = PageShadow.Height = Dip(PageHeightMm);
        TopRuler.Width = Page.Width;
        SideRuler.Height = Page.Height;

        valueText = new TextBlock { Foreground = (Brush)FindResource("GreenBrush"), FontSize = 12 };
        valueLabel = new Border
        {
            Background = (Brush)FindResource("BarBrush"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Child = valueText,
            Visibility = Visibility.Collapsed,
        };

        foreach (var side in Enum.GetValues<Side>())
        {
            var line = new Line { Stroke = GuideBrush, StrokeThickness = 1, SnapsToDevicePixels = true };
            guides[side] = line;
            Guides.Children.Add(line);

            var handle = new MarginHandle(side);
            handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            handle.MouseMove += Handle_MouseMove;
            handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
            handle.LostMouseCapture += (_, _) => EndDrag();
            handle.KeyDown += Handle_KeyDown;
            handle.GotKeyboardFocus += (_, _) => UpdateLayoutForMargins();
            handle.LostKeyboardFocus += (_, _) => UpdateLayoutForMargins();
            handles[side] = handle;
            (handle.IsHorizontal ? TopRuler : SideRuler).Children.Add(handle);
        }
        Guides.Children.Add(valueLabel);

        ApplyFontSize();
        UpdateLayoutForMargins();
        UpdateTitle();

        // Open a file passed on the command line (e.g. "Open with").
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && File.Exists(args[1]))
            LoadFile(args[1]);

        Loaded += (_, _) => Editor.Focus();
    }

    static T Frozen<T>(T f) where T : Freezable { f.Freeze(); return f; }

    // ---------------------------------------------------------------- Margins

    void UpdateLayoutForMargins()
    {
        double top = settings.MarginTop, bottom = settings.MarginBottom;
        double left = settings.MarginLeft, right = settings.MarginRight;
        double w = Page.Width, h = Page.Height;

        Editor.Margin = new Thickness(Dip(left), Dip(top), Dip(right), Dip(bottom));

        Place(Side.Left, Dip(left));
        Place(Side.Right, w - Dip(right));
        Place(Side.Top, Dip(top));
        Place(Side.Bottom, h - Dip(bottom));

        void Place(Side side, double at)
        {
            var handle = handles[side];
            handle.LineShown = settings.GetLine(side);
            handle.ToolTip = $"{side} margin: {settings.GetMargin(side):0} mm\nClick to show or hide its line. Drag to move it.";

            var line = guides[side];
            line.Visibility = settings.GetLine(side) || (dragSide == side && dragMoved) ? Visibility.Visible : Visibility.Collapsed;
            if (handle.IsHorizontal)
            {
                Canvas.SetLeft(handle, at - MarginHandle.Size / 2);
                Canvas.SetTop(handle, 8); // tip touches the top of the page (ruler is 28 high)
                line.X1 = line.X2 = at;
                line.Y1 = 0;
                line.Y2 = h;
            }
            else
            {
                Canvas.SetLeft(handle, 8);
                Canvas.SetTop(handle, at - MarginHandle.Size / 2);
                line.Y1 = line.Y2 = at;
                line.X1 = 0;
                line.X2 = w;
            }
        }

        UpdateValueLabel();
        CheckOverflow();
    }

    /// <summary>Shows "25 mm" beside an arrow while it is being dragged or has keyboard focus.</summary>
    void UpdateValueLabel()
    {
        Side? side = dragSide is { } d && dragMoved ? d
            : handles.Values.FirstOrDefault(h => h.IsKeyboardFocused)?.Side;
        if (side is not { } s)
        {
            valueLabel.Visibility = Visibility.Collapsed;
            return;
        }

        valueText.Text = $"{s} margin {settings.GetMargin(s):0} mm";
        valueLabel.Visibility = Visibility.Visible;
        valueLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = valueLabel.DesiredSize;
        var line = guides[s];
        const double gap = 6;

        double x, y;
        switch (s)
        {
            case Side.Left: x = line.X1 + gap; y = gap; break;
            case Side.Right: x = line.X1 - gap - size.Width; y = gap; break;
            case Side.Top: x = gap; y = line.Y1 + gap; break;
            default: x = gap; y = line.Y1 - gap - size.Height; break;
        }
        Canvas.SetLeft(valueLabel, Math.Clamp(x, 0, Page.Width - size.Width));
        Canvas.SetTop(valueLabel, Math.Clamp(y, 0, Page.Height - size.Height));
    }

    /// <summary>Sets a margin, keeping it on the page and leaving room to write.</summary>
    void SetMargin(Side side, double mm)
    {
        double opposite = side switch
        {
            Side.Left => settings.MarginRight,
            Side.Right => settings.MarginLeft,
            Side.Top => settings.MarginBottom,
            _ => settings.MarginTop,
        };
        double pageMm = side is Side.Left or Side.Right ? PageWidthMm : PageHeightMm;
        mm = Math.Clamp(Math.Round(mm), 0, pageMm - opposite - MinTextMm);
        if (mm == settings.GetMargin(side)) return;

        settings.SetMargin(side, mm);
        UpdateLayoutForMargins();
    }

    void ToggleLine(Side side)
    {
        settings.SetLine(side, !settings.GetLine(side));
        settings.Save();
        UpdateLayoutForMargins();
    }

    void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var handle = (MarginHandle)sender;
        dragSide = handle.Side;
        dragMoved = false;
        dragStart = e.GetPosition(Page);
        handle.CaptureMouse();
        e.Handled = true;
    }

    void Handle_MouseMove(object sender, MouseEventArgs e)
    {
        if (dragSide is not { } side || !((MarginHandle)sender).IsMouseCaptured) return;

        var p = e.GetPosition(Page);
        if (!dragMoved && (p - dragStart).Length < 3) return; // still a click
        if (!dragMoved)
        {
            dragMoved = true;
            UpdateLayoutForMargins(); // show the line while dragging
        }

        double mm = side switch
        {
            Side.Left => Mm(p.X),
            Side.Right => PageWidthMm - Mm(p.X),
            Side.Top => Mm(p.Y),
            _ => PageHeightMm - Mm(p.Y),
        };
        SetMargin(side, mm);
    }

    void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (dragSide is not { } side) return;
        bool wasClick = !dragMoved;
        ((MarginHandle)sender).ReleaseMouseCapture(); // calls EndDrag
        if (wasClick) ToggleLine(side);
        e.Handled = true;
    }

    void EndDrag()
    {
        if (dragSide is null) return;
        bool moved = dragMoved;
        dragSide = null;
        dragMoved = false;
        if (moved) settings.Save();
        UpdateLayoutForMargins();
    }

    /// <summary>Arrow keys nudge a focused margin arrow by 1 mm (Shift: 5 mm); Space or Enter shows its line.</summary>
    void Handle_KeyDown(object sender, KeyEventArgs e)
    {
        var handle = (MarginHandle)sender;
        double step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 5 : 1;
        double move = (handle.IsHorizontal, e.Key) switch
        {
            (true, Key.Left) => -step,
            (true, Key.Right) => step,
            (false, Key.Up) => -step,
            (false, Key.Down) => step,
            _ => 0,
        };

        if (move != 0)
        {
            // Moving an arrow right or down grows the left/top margin but shrinks the right/bottom one.
            double sign = handle.Side is Side.Left or Side.Top ? 1 : -1;
            SetMargin(handle.Side, settings.GetMargin(handle.Side) + sign * move);
            settings.Save();
            e.Handled = true;
        }
        else if (e.Key is Key.Space or Key.Enter)
        {
            ToggleLine(handle.Side);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Editor.Focus();
            e.Handled = true;
        }
    }

    // -------------------------------------------------------------- Text size

    void ApplyFontSize()
    {
        Editor.FontSize = PtToDip(settings.FontSize);
        SizeLabel.Text = $"{settings.FontSize:0} pt";
        SmallerButton.IsEnabled = settings.FontSize > MinFontPt;
        LargerButton.IsEnabled = settings.FontSize < MaxFontPt;
        CheckOverflow();
    }

    void ChangeFontSize(double delta)
    {
        settings.FontSize = Math.Clamp(settings.FontSize + delta, MinFontPt, MaxFontPt);
        settings.Save();
        ApplyFontSize();
    }

    void Smaller_Click(object sender, RoutedEventArgs e) => ChangeFontSize(-1);
    void Larger_Click(object sender, RoutedEventArgs e) => ChangeFontSize(1);

    // ------------------------------------------------------------------ Editor

    void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!settingText && !dirty)
        {
            dirty = true;
            UpdateTitle();
        }
        CheckOverflow();
    }

    /// <summary>The page doesn't scroll on its own: the mouse wheel scrolls the desk.</summary>
    void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Desk.ScrollToVerticalOffset(Desk.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    void CheckOverflow()
    {
        // Wait for layout so the text's height is up to date.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            bool over = Editor.ExtentHeight > Editor.ViewportHeight + 0.5;
            OverflowNote.Visibility = over ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    // ------------------------------------------------------------------- Files

    string DocumentName => filePath is null ? "Untitled" : System.IO.Path.GetFileNameWithoutExtension(filePath);

    void UpdateTitle() => Title = $"{(dirty ? "*" : "")}{DocumentName} – SimpleLetters";

    void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (!ConfirmDiscard()) return;
        var dialog = new OpenFileDialog { Filter = FileFilter };
        if (dialog.ShowDialog(this) == true)
            LoadFile(dialog.FileName);
    }

    void LoadFile(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"SimpleLetters couldn't open that file.\n\n{ex.Message}", "SimpleLetters",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        settingText = true;
        Editor.Text = text;
        // Start a fresh undo history for the new document.
        Editor.IsUndoEnabled = false;
        Editor.IsUndoEnabled = true;
        settingText = false;

        Editor.CaretIndex = 0;
        Editor.ScrollToHome();
        filePath = path;
        dirty = false;
        UpdateTitle();
    }

    void Save_Executed(object sender, ExecutedRoutedEventArgs e) => Save();

    /// <summary>Saves to the current file, asking where the first time. Returns false if not saved.</summary>
    bool Save()
    {
        string? path = filePath;
        if (path is null)
        {
            var dialog = new SaveFileDialog { Filter = FileFilter, DefaultExt = ".txt", FileName = "Untitled.txt" };
            if (dialog.ShowDialog(this) != true) return false;
            path = dialog.FileName;
        }

        try
        {
            File.WriteAllText(path, Editor.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"SimpleLetters couldn't save the file.\n\n{ex.Message}", "SimpleLetters",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        filePath = path;
        dirty = false;
        UpdateTitle();
        return true;
    }

    /// <summary>If there are unsaved changes, offers to save them. Returns false to stay put.</summary>
    bool ConfirmDiscard()
    {
        if (!dirty) return true;
        var answer = MessageBox.Show(this, $"Save changes to {DocumentName}?", "SimpleLetters",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return answer switch
        {
            MessageBoxResult.Yes => Save(),
            MessageBoxResult.No => true,
            _ => false,
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ConfirmDiscard()) e.Cancel = true;
        base.OnClosing(e);
    }

    // ------------------------------------------------------------------- Print

    void Print_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
        if (dialog.ShowDialog() != true) return;

        var document = new FlowDocument
        {
            PageWidth = Dip(PageWidthMm),
            PageHeight = Dip(PageHeightMm),
            PagePadding = new Thickness(Dip(settings.MarginLeft), Dip(settings.MarginTop),
                                        Dip(settings.MarginRight), Dip(settings.MarginBottom)),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = Editor.FontFamily,
            FontSize = Editor.FontSize,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Left,
        };

        var paragraph = new Paragraph { Margin = new Thickness(0) };
        var lines = Editor.Text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i]));
        }
        document.Blocks.Add(paragraph);

        try
        {
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, DocumentName);
        }
        catch (Exception ex) when (ex is PrintQueueException or PrintSystemException or InvalidOperationException)
        {
            MessageBox.Show(this, $"SimpleLetters couldn't print.\n\n{ex.Message}", "SimpleLetters",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

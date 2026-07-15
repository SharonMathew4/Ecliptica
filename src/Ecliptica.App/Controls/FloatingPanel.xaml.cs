using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ecliptica.App.Controls;

public partial class FloatingPanel : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(FloatingPanel), new PropertyMetadata("Panel Title", OnTitleChanged));

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(FloatingPanel), new PropertyMetadata(false, OnIsCollapsedChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    public FloatingPanel()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloatingPanel panel)
        {
            panel.TitleText.Text = e.NewValue as string;
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FloatingPanel panel)
        {
            bool collapsed = (bool)e.NewValue;
            panel.ContentArea.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
            panel.CollapseButton.Content = collapsed ? "▲" : "▼";
        }
    }

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        IsCollapsed = !IsCollapsed;
    }

    private System.Windows.Controls.Primitives.Popup? _parentPopup;
    private System.Windows.Point _dragStartPoint;
    private double _startHorizontalOffset;
    private double _startVerticalOffset;
    private bool _isDraggingPopup;

    private System.Windows.Controls.Primitives.Popup? GetParentPopup()
    {
        DependencyObject parent = VisualTreeHelper.GetParent(this);
        while (parent != null && !(parent is System.Windows.Controls.Primitives.Popup))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as System.Windows.Controls.Primitives.Popup;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            IsCollapsed = !IsCollapsed;
            return;
        }

        _parentPopup = GetParentPopup();
        if (_parentPopup != null)
        {
            _isDraggingPopup = true;
            _dragStartPoint = e.GetPosition(null);
            _startHorizontalOffset = _parentPopup.HorizontalOffset;
            _startVerticalOffset = _parentPopup.VerticalOffset;
            
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }
    }

    private void Header_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDraggingPopup && _parentPopup != null)
        {
            System.Windows.Point currentPoint = e.GetPosition(null);
            double deltaX = currentPoint.X - _dragStartPoint.X;
            double deltaY = currentPoint.Y - _dragStartPoint.Y;

            _parentPopup.HorizontalOffset = _startHorizontalOffset + deltaX;
            _parentPopup.VerticalOffset = _startVerticalOffset + deltaY;
            e.Handled = true;
        }
    }

    private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingPopup)
        {
            _isDraggingPopup = false;
            ((UIElement)sender).ReleaseMouseCapture();
            e.Handled = true;
        }
    }
}

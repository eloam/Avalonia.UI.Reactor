// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Represents the wrapper of a view: background, border, corners and padding; for a clickable view (<c>.OnTap</c>),
/// the hover and pressed overlays of the theme and the click.
/// </summary>
/// <remarks>
/// An inner control that handles the pointer (button, checkbox, field) does not trigger the click of the wrapper: the
/// innermost control wins.
/// </remarks>
internal sealed class Surface : Border
{
    private static readonly Cursor s_handCursor = new(StandardCursorType.Hand);

    private Primitives.Color? _background;
    private Theme? _theme;
    private bool _tappable;
    private bool _pressed;

    /// <summary>
    /// Gets or sets the action that runs the action of the current view, connected once by the reconciler.
    /// </summary>
    public Action? TapAction { get; set; }

    /// <summary>
    /// Sets the background of the view and whether it is clickable, with the hover and pressed overlays of the theme.
    /// </summary>
    /// <param name="background">The background of the view, or <see langword="null"/>.</param>
    /// <param name="theme">The theme of the view.</param>
    /// <param name="tappable">A value indicating whether the view is clickable.</param>
    public void Paint(Primitives.Color? background, Theme theme, bool tappable)
    {
        _background = background;
        _theme = theme;
        _tappable = tappable;
        if (!tappable)
        {
            _pressed = false;
        }

        Cursor = tappable ? s_handCursor : null;
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_tappable || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pressed = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_pressed)
        {
            return;
        }

        _pressed = false;
        e.Pointer.Capture(null);
        e.Handled = true;
        Refresh();
        if (new Rect(Bounds.Size).Contains(e.GetPosition(this)))
        {
            TapAction?.Invoke();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = false;
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPointerOverProperty)
        {
            Refresh();
        }
    }

    // The background shown: that of the view, with the overlay under the pointer and while pressed if it is clickable.
    // A clickable view without background keeps a transparent one, to receive the pointer on its whole surface.
    private void Refresh()
    {
        if (!_tappable || _theme is null)
        {
            this.SetBrush(BackgroundProperty, _background);
            return;
        }

        float amount = !IsPointerOver ? 0 : _pressed ? _theme.PressedOverlay : _theme.HoverOverlay;
        this.SetBrush(BackgroundProperty, _theme.Overlay(_background ?? Primitives.Color.Transparent, amount));
    }
}

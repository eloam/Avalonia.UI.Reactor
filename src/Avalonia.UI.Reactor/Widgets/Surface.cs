using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.UI.Reactor.Widgets;

using Avalonia.UI.Reactor.Reconciliation;
using Avalonia.UI.Reactor.Theming;

/// <summary>
/// Enveloppe d'une vue : fond, bordure, angles et marge intérieure ; pour une vue cliquable (<c>.OnTap</c>), le voile
/// de survol et d'appui du thème et le clic. Un contrôle intérieur qui consomme le pointeur (bouton, case, champ) ne
/// déclenche pas le clic de l'enveloppe, comme dans l'ancien moteur où le contrôle le plus intérieur gagnait.
/// </summary>
internal sealed class Surface : Border
{
    Primitives.Color? _background;
    Theme? _theme;
    bool _tappable, _pressed;

    /// <summary>Exécute l'action de la vue courante (branché une fois par le réconciliateur).</summary>
    public Action? TapAction { get; set; }

    /// <summary>Fond de la vue, et si elle est cliquable (voile de survol et d'appui pris dans le thème).</summary>
    public void Paint(Primitives.Color? background, Theme theme, bool tappable)
    {
        _background = background;
        _theme = theme;
        _tappable = tappable;
        if (!tappable) _pressed = false;
        Refresh();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_tappable || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pressed = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        Refresh();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_pressed) return;
        _pressed = false;
        e.Pointer.Capture(null);
        e.Handled = true;
        Refresh();
        if (new Rect(Bounds.Size).Contains(e.GetPosition(this))) TapAction?.Invoke();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = false;
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPointerOverProperty) Refresh();
    }

    /// <summary>
    /// Fond affiché : celui de la vue, voilé au survol et à l'appui si elle est cliquable. Une vue cliquable sans fond
    /// garde un fond transparent, pour recevoir le pointeur sur toute sa surface.
    /// </summary>
    void Refresh()
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

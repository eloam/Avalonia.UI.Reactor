using Mvu.Layout;
using Mvu.Primitives;
using Mvu.Runtime;
using Mvu.Theming;
using Mvu.Views;

namespace Mvu.Rendering;

/// <summary>
/// Dessine un arbre de layout. Aucune couleur ni dimension n'est codée en dur :
/// tout vient du thème résolu de chaque nœud, puis des surcharges de la vue.
/// Un contrôle qui a un modèle (<c>Template</c>, sur la vue ou dans le thème) est dessiné à partir
/// de la vue que renvoie ce modèle pour son état du moment (survol, appui, focus).
/// </summary>
internal static class Renderer
{
    public static void Paint(LayoutNode n, IDrawingContext ctx, UiHost s)
    {
        bool hovered = s.Hovered == n.Id, pressed = hovered && s.Pressed == n.Id, focused = s.Focused == n.Id;
        var (highlighted, mnemonics) = MenuStateOf(n.View, s);
        if (ControlTemplates.Build(n.View, n.Theme, hovered, pressed, focused, highlighted, mnemonics) is { } template)
        {
            PaintTemplated(n, s.LayoutTemplate(template, n), ctx, s);
            return;
        }
        if (n.View is TextFieldView) s.Editors.Remove(n.Id); // dessin standard : zone d'édition = contenu du champ

        PaintNode(n, ctx, s);
        foreach (var c in n.Children) Paint(c, ctx, s);
    }

    /// <summary>Dessine l'arbre produit par un modèle ; l'emplacement <c>Editor</c> reçoit le texte du champ.</summary>
    static void PaintTemplated(LayoutNode owner, LayoutNode n, IDrawingContext ctx, UiHost s)
    {
        if (n.View is EditorView && owner.View is TextFieldView tf)
        {
            PaintEditor(owner, tf, n.Content, n.Font, n.Foreground, ctx, s);
            return;
        }
        PaintNode(n, ctx, s);
        foreach (var c in n.Children) PaintTemplated(owner, c, ctx, s);
    }

    static void PaintNode(LayoutNode n, IDrawingContext ctx, UiHost s)
    {
        switch (n.View)
        {
            case ButtonView b: PaintButton(n, b, ctx, s); break;
            case TextFieldView tf: PaintTextField(n, tf, ctx, s); break;
            case CheckboxView cb: PaintBox(n, ctx, s); PaintCheckbox(n, cb, ctx, s); break;
            case MenuBarView: PaintMenuBar(n, ctx); break;
            case MenuTitleView title: PaintMenuTitle(n, title, ctx, s); break;
            case MenuRowView row: PaintMenuRow(n, row, ctx, s); break;
            case DividerView: ctx.FillRect(n.Bounds, n.Theme[n.View.Background ?? n.Theme.DividerColor]); break;
            case TextView t: PaintBox(n, ctx, s); ctx.DrawText(t.Content, n.Content, n.Font, n.Foreground, t.TextAlign); break;
            case CanvasView cv:
                PaintBox(n, ctx, s);
                ctx.PushClip(n.Bounds);
                try { cv.Draw(new DrawScope(ctx, n.Content, n.Theme, n.Foreground, n.Font)); }
                finally { ctx.PopClip(); }
                break;
            default: PaintBox(n, ctx, s); break;
        }
    }

    /// <summary>Voile de survol/appui : la couleur de texte du thème, en transparence
    /// (assombrit en clair, éclaircit en sombre — sans réglage par thème).</summary>
    static Color Overlay(Color bg, Theme t, float amount) =>
        amount <= 0 ? bg : bg.A == 0 ? t.Palette.Text.WithAlpha(amount) : bg.Mix(t.Palette.Text with { A = bg.A }, amount);

    static void PaintBox(LayoutNode n, IDrawingContext ctx, UiHost s)
    {
        var v = n.View;
        var bg = v.Background is { } b ? n.Theme[b] : Color.Transparent;
        if (v.TapAction is not null && s.Hovered == n.Id)
            bg = Overlay(bg, n.Theme, s.Pressed == n.Id ? n.Theme.PressedOverlay : n.Theme.HoverOverlay);
        ctx.FillRect(n.Bounds, bg, n.Radius);
        if (v.BorderColor is { } bc) ctx.StrokeRect(n.Bounds, n.Theme[bc], v.BorderWidth ?? 1, n.Radius);
    }

    static void PaintButton(LayoutNode n, ButtonView b, IDrawingContext ctx, UiHost s)
    {
        var t = n.Theme;
        var style = ControlTemplates.VariantOf(b, t);
        var bg = t[b.Background ?? style.Background];
        var fg = t[b.Foreground ?? style.Foreground];

        bool hover = s.Hovered == n.Id, pressed = hover && s.Pressed == n.Id;
        bg = Overlay(bg, t, pressed ? style.PressedOverlay : hover ? style.HoverOverlay : 0);

        ctx.FillRect(n.Bounds, bg, n.Radius);
        if ((b.BorderColor ?? style.BorderColor) is { } bc)
            ctx.StrokeRect(n.Bounds, t[bc], b.BorderWidth ?? t.Button.BorderWidth, n.Radius);
        ctx.DrawText(b.Label, n.Content, n.Font, fg, TextAlign.Center);
    }

    static void PaintTextField(LayoutNode n, TextFieldView tf, IDrawingContext ctx, UiHost s)
    {
        var t = n.Theme;
        var ft = t.TextField;
        bool focused = s.Focused == n.Id, hover = s.Hovered == n.Id;

        ctx.FillRect(n.Bounds, t[tf.Background ?? ft.Background], n.Radius);
        if (focused) ctx.StrokeRect(n.Bounds, t[ft.FocusBorderColor], ft.FocusBorderWidth, n.Radius);
        else ctx.StrokeRect(n.Bounds, t[hover ? ft.HoverBorderColor : tf.BorderColor ?? ft.BorderColor],
            tf.BorderWidth ?? ft.BorderWidth, n.Radius);

        var fg = tf.Foreground is { } f ? t[f] : t[ft.Foreground];
        PaintEditor(n, tf, n.Content, n.Font, fg, ctx, s);
    }

    /// <summary>Texte, texte indicatif, défilement et curseur d'un champ, dans la zone donnée.</summary>
    static void PaintEditor(LayoutNode owner, TextFieldView tf, Rect area, Font font, Color fg, IDrawingContext ctx, UiHost s)
    {
        var t = owner.Theme;
        var ft = t.TextField;
        bool focused = s.Focused == owner.Id;
        s.Editors[owner.Id] = (area, font); // pour placer le curseur au clic
        ctx.PushClip(area);

        float scroll = 0, caretX = 0;
        if (focused)
        {
            int caret = Math.Clamp(s.Caret, 0, tf.Value.Length);
            caretX = ctx.MeasureText(tf.Value[..caret], font).Width;
            scroll = s.TextScroll;
            float margin = ft.CaretWidth + 1;
            if (caretX - scroll > area.Width - margin) scroll = caretX - area.Width + margin;
            if (caretX < scroll) scroll = caretX;
            s.TextScroll = scroll;
        }

        if (tf.Value.Length == 0)
            ctx.DrawText(tf.Placeholder, area, font, t[ft.Placeholder]);
        else
        {
            float w = ctx.MeasureText(tf.Value, font).Width + 2 * ft.CaretWidth;
            ctx.DrawText(tf.Value, new Rect(area.X - scroll, area.Y, Math.Max(w, area.Width), area.Height),
                font, fg, ellipsis: false);
        }

        if (focused && s.CaretVisible)
        {
            float lineH = ctx.MeasureText("Ag", font).Height;
            ctx.FillRect(new Rect(area.X + caretX - scroll, area.Y + (area.Height - lineH) / 2, ft.CaretWidth, lineH), t[ft.Caret]);
        }
        ctx.PopClip();
    }

    static void PaintCheckbox(LayoutNode n, CheckboxView cb, IDrawingContext ctx, UiHost s)
    {
        var t = n.Theme;
        var ct = t.Checkbox;
        float size = ct.Size;
        var content = n.Content;
        var box = new Rect(content.X, content.Y + (content.Height - size) / 2, size, size);
        bool hover = s.Hovered == n.Id;

        if (cb.IsChecked)
        {
            var bg = t[ct.CheckedBackground];
            ctx.FillRect(box, hover ? Overlay(bg, t, ct.HoverOverlay) : bg, ct.Radius);
            // Coche en proportion de la taille de la case (une autre forme : Template).
            var mark = Shapes.Fit(Shapes.CheckboxMark, box);
            float thickness = ct.CheckThickness * size / Shapes.CheckboxReferenceSize;
            ctx.DrawLine(mark[0], mark[1], t[ct.CheckMark], thickness);
            ctx.DrawLine(mark[1], mark[2], t[ct.CheckMark], thickness);
        }
        else
        {
            ctx.FillRect(box, t[ct.Background], ct.Radius);
            ctx.StrokeRect(box, t[hover ? ct.HoverBorderColor : ct.BorderColor], ct.BorderWidth, ct.Radius);
        }

        if (cb.Label is { } label)
        {
            float x = box.Right + ct.LabelSpacing;
            ctx.DrawText(label, new Rect(x, content.Y, Math.Max(0, content.Right - x), content.Height), n.Font, n.Foreground);
        }
    }

    // ── Menus ──

    /// <summary>État de menu lu au moment du dessin (la mise en page, en cache, ne le connaît pas).</summary>
    static (bool Highlighted, bool Mnemonics) MenuStateOf(View v, UiHost s) => v switch
    {
        MenuTitleView t => (s.Menus.IsTitleActive(t.Index), s.Menus.ShowMnemonics),
        MenuRowView r => (s.Menus.IsHighlighted(r.Depth, r.Index) && r.IsEnabled, s.Menus.ShowMnemonics),
        _ => (false, false),
    };

    /// <summary>Fond d'un titre ouvert ou d'un élément surligné : couleur du thème, sinon voile de la couleur du texte.</summary>
    static Color MenuHighlight(Theme t) =>
        t.Menu.HighlightBackground is { } hb ? t[hb] : Overlay(Color.Transparent, t, t.Menu.HighlightOverlay);

    static Color Faded(Color c, float opacity) => c with { A = (byte)Math.Round(c.A * Math.Clamp(opacity, 0, 1)) };

    static void PaintMenuBar(LayoutNode n, IDrawingContext ctx)
    {
        if (n.Collapsed) return; // barre du système : rien à dessiner dans la fenêtre
        var t = n.Theme;
        var m = t.Menu;
        ctx.FillRect(n.Bounds, t[n.View.Background ?? m.BarBackground], n.Radius);
        float border = n.View.BorderWidth ?? m.BarBorderWidth;
        if (border > 0)
            ctx.FillRect(new Rect(n.Bounds.X, n.Bounds.Bottom - border, n.Bounds.Width, border),
                t[n.View.BorderColor ?? m.BarBorderColor]);
    }

    static void PaintMenuTitle(LayoutNode n, MenuTitleView title, IDrawingContext ctx, UiHost s)
    {
        var t = n.Theme;
        var fg = n.Foreground;
        if (s.Menus.IsTitleActive(title.Index))
        {
            ctx.FillRect(n.Bounds, MenuHighlight(t), n.Radius);
            if (t.Menu.HighlightForeground is { } hf) fg = t[hf];
        }
        else if (s.Hovered == n.Id) ctx.FillRect(n.Bounds, Overlay(Color.Transparent, t, t.HoverOverlay), n.Radius);
        PaintLabel(title.Menu.Title, n.Content, n.Font, fg, s.Menus.ShowMnemonics, ctx);
    }

    static void PaintMenuRow(LayoutNode n, MenuRowView row, IDrawingContext ctx, UiHost s)
    {
        var t = n.Theme;
        var m = t.Menu;
        var item = row.Item as MenuItemView;
        var fg = n.Foreground;
        var muted = t[m.ShortcutForeground];
        if (row.IsEnabled && s.Menus.IsHighlighted(row.Depth, row.Index))
        {
            ctx.FillRect(n.Bounds, MenuHighlight(t), n.Radius);
            if (m.HighlightForeground is { } hf) fg = muted = t[hf];
        }
        if (!row.IsEnabled)
        {
            fg = Faded(fg, m.DisabledOpacity);
            muted = Faded(muted, m.DisabledOpacity);
        }

        var c = n.Content;
        float size = m.IndicatorSize;
        if (item?.IsChecked == true)
        {
            var box = new Rect(c.X + (m.CheckColumnWidth - size) / 2, c.Y + (c.Height - size) / 2, size, size);
            ctx.DrawPolyline(Shapes.Fit(Shapes.MenuCheckMark, box), fg, m.IndicatorThickness);
        }

        float right = c.Right;
        if (item?.Gesture is { } gesture)
        {
            var text = gesture.ToString();
            float w = ctx.MeasureText(text, n.Font).Width;
            ctx.DrawText(text, new Rect(c.Right - w, c.Y, w, c.Height), n.Font, muted, ellipsis: false);
            right = c.Right - w - m.ShortcutSpacing;
        }
        else if (row.HasSubmenu)
        {
            var box = new Rect(c.Right - size, c.Y + (c.Height - size) / 2, size, size);
            ctx.DrawPolyline(Shapes.Fit(Shapes.SubmenuChevron, box), fg, m.IndicatorThickness);
            right = box.X - m.ShortcutSpacing;
        }

        float x = c.X + m.CheckColumnWidth;
        PaintLabel(row.Label, new Rect(x, c.Y, Math.Max(0, right - x), c.Height), n.Font, fg, s.Menus.ShowMnemonics, ctx);
    }

    /// <summary>Libellé sans ses <c>&amp;</c>, lettre d'accès soulignée en mode clavier.</summary>
    static void PaintLabel(string label, Rect area, Font font, Color color, bool showMnemonic, IDrawingContext ctx)
    {
        var (text, mnemonic) = Mnemonics.Parse(label);
        ctx.DrawText(text, area, font, color);
        if (showMnemonic && mnemonic >= 0) ctx.UnderlineText(text, mnemonic, 1, area, font, color);
    }
}

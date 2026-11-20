namespace Mvu.Views;

/// <summary>
/// Ce que reçoit un modèle de titre de menu (<c>MenuTheme.TitleTemplate</c>) : le texte sans <c>&amp;</c>,
/// la lettre d'accès, et l'état — menu ouvert ou titre actif au clavier, survol, lettres d'accès à souligner.
/// </summary>
public sealed record MenuTitleState(string Text, int MnemonicIndex, bool Active, bool Hovered, bool ShowMnemonic);

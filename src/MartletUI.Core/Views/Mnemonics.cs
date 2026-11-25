// Copyright (c) Yohann Breuil.
// Licensed under the MIT License.

using System.Text;

namespace MartletUI.Views;

/// <summary>
/// Reads the access keys of menu labels, in the Windows way: in <c>"&amp;File"</c>, the letter F opens the menu with
/// Alt+F; <c>"&amp;&amp;"</c> displays an ampersand.
/// </summary>
internal static class Mnemonics
{
    /// <summary>
    /// Returns the text to display and the position of the access key in that text.
    /// </summary>
    /// <param name="label">The label, with <c>&amp;</c> before the access key.</param>
    /// <returns>The text without the markers, and the index of the access key in it, or -1 if there is none.</returns>
    public static (string Text, int Index) Parse(string label)
    {
        var text = new StringBuilder(label.Length);
        int index = -1;
        for (int i = 0; i < label.Length; i++)
        {
            if (label[i] == '&' && i + 1 < label.Length)
            {
                if (label[i + 1] == '&')
                {
                    text.Append('&');
                    i++;
                    continue;
                }

                if (index < 0)
                {
                    index = text.Length;
                }

                continue;
            }

            text.Append(label[i]);
        }

        return (text.ToString(), index);
    }
}

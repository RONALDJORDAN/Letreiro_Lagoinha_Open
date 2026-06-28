using System.Collections.Generic;

namespace LetreiroDigital.Services
{
    public class FontService
    {
        public static readonly Dictionary<string, string> EmbeddedFontMap = new()
        {
            { "Digital-7", "pack://application:,,,/LetreiroDigital;component/Fonts/#Digital-7" },
            { "Digital-7 Mono", "pack://application:,,,/LetreiroDigital;component/Fonts/#Digital-7 Mono" },
            { "Led Board-7", "pack://application:,,,/LetreiroDigital;component/Fonts/#Led Board-7" },
            { "Oswald", "pack://application:,,,/LetreiroDigital;component/Fonts/#Oswald" },
        };

        public static string ResolveFontPath(string fontName)
        {
            return EmbeddedFontMap.TryGetValue(fontName, out var path) ? path : fontName;
        }
    }
}

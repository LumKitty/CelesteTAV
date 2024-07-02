using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using Eto.Drawing;

namespace CelesteStudio;

public static class FontManager {
    public const string FontFamilyBuiltin = "<builtin>";
    public const string FontFamilyBuiltinDisplayName = "JetBrains Mono (builtin)";
    
    private static Font? editorFontRegular, editorFontBold, editorFontItalic, editorFontBoldItalic, statusFont;

    public static Font EditorFontRegular => editorFontRegular ??= CreateFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom);
    public static Font EditorFontBold => editorFontBold ??= CreateFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Bold);
    public static Font EditorFontItalic => editorFontItalic ??= CreateFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Italic);
    public static Font EditorFontBoldItalic => editorFontBoldItalic ??= CreateFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Bold | FontStyle.Italic);
    public static Font StatusFont => statusFont ??= CreateFont(Settings.Instance.FontFamily, Settings.Instance.StatusFontSize);
    
    private static FontFamily? builtinFontFamily;
    public static Font CreateFont(string fontFamily, float size, FontStyle style = FontStyle.None) {
        if (fontFamily == FontFamilyBuiltin) {
            var asm = Assembly.GetExecutingAssembly();
            builtinFontFamily ??= FontFamily.FromStreams(asm.GetManifestResourceNames()
                .Where(name => name.StartsWith("JetBrainsMono/"))
                .Select(name => asm.GetManifestResourceStream(name)));
            
            return new Font(builtinFontFamily, size, style);
        } else {
            return new Font(fontFamily, size, style);
        }
    }
    
    private static readonly Dictionary<Font, float> charWidthCache = new();
    public static float CharWidth(this Font font) {
        if (charWidthCache.TryGetValue(font, out float width))
            return width;
        
        width = font.MeasureString("X").Width;
        charWidthCache.Add(font, width);
        return width;
    } 
    public static float LineHeight(this Font font) {
        if (Eto.Platform.Instance.IsWpf) {
            // WPF reports the line height a bit to small for some reason?
            return font.LineHeight + 5.0f;
        }

        return font.LineHeight;
    }
    public static float MeasureWidth(this Font font, string text) => font.CharWidth() * text.Length; 

    public static void OnFontChanged() {
        // Clear cached fonts
        editorFontRegular = editorFontBold = editorFontItalic = editorFontBoldItalic = statusFont = null;
    }
}

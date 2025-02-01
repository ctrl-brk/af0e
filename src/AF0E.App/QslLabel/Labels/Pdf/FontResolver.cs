using PdfSharp.Fonts;

namespace QslLabel.Labels.Pdf;

internal sealed class FontResolver : IFontResolver
{
    private static readonly byte[] _regularFontData = LoadFontData("consola.ttf");
    private static readonly byte[] _boldFontData = LoadFontData("consolab.ttf");
    private static readonly byte[] _symbolsFontData = LoadFontData("arialuni.ttf");
    private static string DefaultFontName => "Consolas";
    private static string SymbolsFontName => "Symbols";

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName.Equals(DefaultFontName, StringComparison.OrdinalIgnoreCase))
            return new FontResolverInfo(isBold ? "Consolas Bold" : "Consolas Regular");

        if (familyName.Equals(SymbolsFontName, StringComparison.OrdinalIgnoreCase))
            return new FontResolverInfo("Symbols");

        // Fallback to default
        return null;
    }

    public byte[] GetFont(string faceName)
    {
        return faceName switch
        {
            "Consolas Regular" => _regularFontData,
            "Consolas Bold" => _boldFontData,
            "Symbols" => _symbolsFontData,
            _ => throw new ArgumentException($"Font {faceName} is not registered.")
        };
    }

    private static byte[] LoadFontData(string fileName)
    {
        var fontPath = Path.Combine(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System))!.FullName, "Fonts", fileName);
        return !File.Exists(fontPath) ? throw new FileNotFoundException($"Font file not found: {fontPath}") : File.ReadAllBytes(fontPath);
    }
}

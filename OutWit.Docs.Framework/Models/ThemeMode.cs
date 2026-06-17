using OutWit.Common.Enums;

namespace OutWit.Docs.Framework.Models;

public sealed class ThemeMode : StringEnum<ThemeMode>
{
    #region Static Constants

    public static readonly ThemeMode Dark = new("dark");

    public static readonly ThemeMode Light = new("light");

    #endregion

    #region Constructors

    private ThemeMode(string value)
        : base(value)
    {
    }

    #endregion
}

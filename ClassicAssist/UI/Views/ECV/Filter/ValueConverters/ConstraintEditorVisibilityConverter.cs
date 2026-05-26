using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ClassicAssist.Data.Autoloot;
using ClassicAssist.Shared.Resources;
using ClassicAssist.UO.Data;
using ClassicAssist.UI.ViewModels.Agents;

namespace ClassicAssist.UI.Views.ECV.Filter.ValueConverters
{
    // Selects which editor in the value column is visible based on the row's Constraint.
    // ConverterParameter is the editor name: "SkillBonus", "Layer", "TileFlags", "MultiItemID",
    // "MultiCliloc", "AutolootMatch", "HexValue", "Additional", "PropertyName", "Text".
    public class ConstraintEditorVisibilityConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( !( value is PropertyEntry c ) || !( parameter is string which ) )
            {
                return false;
            }

            Type enumType = c.AllowedValuesEnum;
            string name = c.Name;
            bool multi = c.UseMultipleValues;

            switch ( which )
            {
                case "SkillBonus":
                    return enumType == typeof( SkillBonusSkills );

                case "Layer":
                    return enumType == typeof( Layer );

                case "TileFlags":
                    return enumType == typeof( TileFlags );

                case "MultiItemID":
                    return multi && name != Strings.Cliloc__Multiple_;

                case "MultiCliloc":
                    return name == Strings.Cliloc__Multiple_;

                case "AutolootMatch":
                    return name == Strings.Autoloot_Match;

                case "HexValue":
                    // From original WPF DataTriggers (trigger order matters; later wins):
                    // visible when AllowedValuesEnum is null or SkillBonusSkills,
                    // hidden if multi, or Name == Name, or Name == Autoloot_Match.
                    if ( !( enumType == null || enumType == typeof( SkillBonusSkills ) ) )
                        return false;
                    if ( multi ) return false;
                    if ( name == Strings.Name ) return false;
                    if ( name == Strings.Autoloot_Match ) return false;
                    return true;

                case "Additional":
                    return name == Strings.Name;

                case "PropertyName":
                    // AutolootPropertyControl fallback: show the static property name label
                    // for any constraint that doesn't have a specialised property-column editor.
                    return enumType != typeof( SkillBonusSkills );

                case "Text":
                    // AutolootValueControl fallback: show the free-text editor when no
                    // specialised value editor (Layer, MultiItemID, MultiCliloc, AutolootMatch) applies.
                    if ( enumType == typeof( Layer ) ) return false;
                    if ( multi && name != Strings.Cliloc__Multiple_ ) return false;
                    if ( name == Strings.Cliloc__Multiple_ ) return false;
                    if ( name == Strings.Autoloot_Match ) return false;
                    return true;
            }

            return false;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture ) =>
            throw new NotSupportedException();
    }
}

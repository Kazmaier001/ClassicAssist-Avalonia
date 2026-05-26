using Avalonia;
using Avalonia.Controls;

namespace ClassicAssist.UI.Misc
{
    public class SkillsGridViewColumn : DataGridTextColumn
    {
        public enum Enums
        {
            Name,
            Value,
            Base,
            Delta,
            Cap,
            LockStatus
        }

        public static readonly StyledProperty<Enums> SortFieldProperty =
            AvaloniaProperty.Register<SkillsGridViewColumn, Enums>( nameof( SortField ) );

        public Enums SortField
        {
            get => GetValue( SortFieldProperty );
            set => SetValue( SortFieldProperty, value );
        }
    }

    public class SkillsDataGridTemplateColumn : DataGridTemplateColumn
    {
        public static readonly StyledProperty<SkillsGridViewColumn.Enums> SortFieldProperty =
            AvaloniaProperty.Register<SkillsDataGridTemplateColumn, SkillsGridViewColumn.Enums>( nameof( SortField ) );

        public SkillsGridViewColumn.Enums SortField
        {
            get => GetValue( SortFieldProperty );
            set => SetValue( SortFieldProperty, value );
        }
    }
}

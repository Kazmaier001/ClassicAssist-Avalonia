using Avalonia;

namespace ClassicAssist.Misc
{
    public class BindingProxy : AvaloniaObject
    {
        // Using a StyledProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly StyledProperty<object> DataProperty = AvaloniaProperty.Register<BindingProxy, object>( "Data" );

        public object Data
        {
            get => GetValue( DataProperty );
            set => SetValue( DataProperty, value );
        }


    }
}
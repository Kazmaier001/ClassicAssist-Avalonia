using Avalonia;
using Avalonia.Markup.Xaml;

namespace TestHost
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load( this );
        }
    }
}

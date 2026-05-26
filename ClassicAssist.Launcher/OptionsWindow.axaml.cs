using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ClassicAssist.Launcher
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load( this );
        }
    }
}

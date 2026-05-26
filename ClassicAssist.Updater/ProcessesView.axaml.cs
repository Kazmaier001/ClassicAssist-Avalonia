using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ClassicAssist.Updater
{
    public partial class ProcessesView : Window
    {
        public ProcessesView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load( this );
        }
    }
}

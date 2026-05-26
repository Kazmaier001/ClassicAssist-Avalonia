using Avalonia.Controls;
using ClassicAssist.UI.ViewModels.Agents.Screenshot;

namespace ClassicAssist.UI.Views.Agents.Screenshot
{
    /// <summary>
    ///     Interaction logic for ScreenshotMobileIDWindow.xaml
    /// </summary>
    public partial class ScreenshotMobileFilterWindow : Window
    {
        public ScreenshotMobileFilterWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged( object sender, System.EventArgs e )
        {
            if ( DataContext is ScreenshotMobileFilterViewModel vm )
            {
                vm.RequestClose += Close;
            }
        }
    }
}
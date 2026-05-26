using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassicAssist.Controls.VirtualFolderBrowse
{
    /// <summary>
    ///     Interaction logic for FolderPromptWindow.axaml
    /// </summary>
    public partial class FolderPromptWindow : Window
    {
        public FolderPromptWindow()
        {
            InitializeComponent();
        }

        public string Text { get; set; }

        private void CancelOnClick( object sender, RoutedEventArgs e )
        {
            Close( false );
        }

        private void OKOnClick( object sender, RoutedEventArgs e )
        {
            Text = TextBox.Text;
            Close( true );
        }
    }
}
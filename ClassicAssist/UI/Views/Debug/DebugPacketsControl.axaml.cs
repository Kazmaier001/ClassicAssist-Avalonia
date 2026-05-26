using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ClassicAssist.UI.Views.Debug
{
    /// <summary>
    ///     Interaction logic for DebugPacketsControl.xaml
    /// </summary>
    public partial class DebugPacketsControl : UserControl
    {
        public DebugPacketsControl()
        {
            InitializeComponent();
        }

        private void ToggleGrid_OnPointerPressed( object sender, PointerPressedEventArgs e )
        {
            if ( sender is Control c )
            {
                ComboBox combo = c.FindAncestorOfType<ComboBox>();
                if ( combo != null )
                {
                    // Toggle (so a second click also closes), and mark Handled so
                    // ComboBox's own pointer plumbing doesn't immediately re-toggle.
                    combo.IsDropDownOpen = !combo.IsDropDownOpen;
                    e.Handled = true;
                }
            }
        }
    }
}
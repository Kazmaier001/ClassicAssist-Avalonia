using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views.Agents
{
    /// <summary>
    ///     Interaction logic for AutolootTabControl.xaml
    /// </summary>
    public partial class AutolootTabControl : UserControl
    {
        public AutolootTabControl()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so commands bound via Source={StaticResource Proxy}
            // (group context menu, insert popup, etc.) resolve to the VM.
            DataContextChanged += ( s, e ) => SyncProxyData();
            SyncProxyData();
        }

        private void SyncProxyData()
        {
            if ( Resources.TryGetValue( "Proxy", out object proxyObj ) && proxyObj is BindingProxy proxy )
            {
                proxy.Data = DataContext;
            }
        }

        private void DraggableTreeView_OnPreviewMouseWheel( object sender, PointerWheelEventArgs e )
        {
            /*
             * Cheap hack for our broken template, no scrollbars, bubble event to parent scrollviewer
             */
            if ( !( sender is Control control ) || e.Handled )
            {
                return;
            }

            if ( control.Parent == null )
            {
                return;
            }

            e.Handled = true;
            // Forward wheel event to parent in Avalonia
            Control parent = control.Parent as Control;
            if ( parent != null )
            {
                e.Handled = false; // Let it bubble up naturally
            }
        }
    }
}
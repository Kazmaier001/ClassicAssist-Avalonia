using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for HotkeysTab.xaml
    /// </summary>
    public partial class HotkeysTabControl : UserControl
    {
        public HotkeysTabControl()
        {
            InitializeComponent();

            // WPF's BindingProxy : Freezable inherits DataContext; Avalonia's
            // AvaloniaObject doesn't. Wire the proxy explicitly so inner
            // ListBoxes (whose SelectedItem binds back through Data.SelectedItem)
            // can actually push their selection into the VM.
            DataContextChanged += OnDataContextChanged;
            SyncProxyData();
        }

        private void OnDataContextChanged( object sender, EventArgs e ) => SyncProxyData();

        private void SyncProxyData()
        {
            if ( Resources.TryGetValue( "Proxy", out object proxyObj ) && proxyObj is BindingProxy proxy )
            {
                proxy.Data = DataContext;
            }
        }

        private void UIElement_OnPreviewMouseWheel( object sender, PointerWheelEventArgs e )
        {
            if ( e.Handled )
            {
                return;
            }

            // In Avalonia, let the event bubble up naturally to parent ScrollViewer
            e.Handled = false;
        }

        public static ScrollViewer GetScrollViewer( Visual depObj )
        {
            if ( depObj is ScrollViewer scrollViewer )
            {
                return scrollViewer;
            }

            foreach ( var child in depObj.GetVisualChildren() )
            {
                ScrollViewer result = GetScrollViewer( child );

                if ( result != null )
                {
                    return result;
                }
            }

            return null;
        }
    }
}
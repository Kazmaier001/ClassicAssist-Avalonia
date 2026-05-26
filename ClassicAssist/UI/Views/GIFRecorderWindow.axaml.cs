using System;
using Avalonia.Controls;
using Avalonia.Input;
using ClassicAssist.Misc;
using ClassicAssist.UI.ViewModels;

namespace ClassicAssist.UI.Views
{
    public partial class GIFRecorderWindow : Window
    {
        public GIFRecorderWindow()
        {
            InitializeComponent();
            DataContext = new GIFRecorderViewModel( this );

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so RecordCommand / IsRecording bindings resolve.
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

        private void ResizeGrip_PointerPressed( object sender, PointerPressedEventArgs e )
        {
            BeginResizeDrag( WindowEdge.SouthEast, e );
        }
    }
}

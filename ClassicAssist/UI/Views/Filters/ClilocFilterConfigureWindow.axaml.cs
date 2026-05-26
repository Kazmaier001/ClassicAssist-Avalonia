using System;
using Avalonia;
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views.Filters
{
    /// <summary>
    ///     Interaction logic for ClilocFilterConfigureWindow.xaml
    /// </summary>
    public partial class ClilocFilterConfigureWindow : Window
    {
        public ClilocFilterConfigureWindow()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so cliloc/hue cell commands resolve to the VM.
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
    }
}
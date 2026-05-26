using System;
using Avalonia;
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for ActiveObjectsWindow.xaml
    /// </summary>
    public partial class ActiveObjectsWindow : Window
    {
        public ActiveObjectsWindow()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so commands bound via Source={StaticResource Proxy}
            // resolve to the VM.
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
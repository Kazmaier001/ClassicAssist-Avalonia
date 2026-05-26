using System;
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views.Agents
{
    /// <summary>
    ///     Interaction logic for DressTabControl.xaml
    /// </summary>
    public partial class DressTabControl : UserControl
    {
        public DressTabControl()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so ChangeDressType context-menu command
            // resolves to the VM.
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
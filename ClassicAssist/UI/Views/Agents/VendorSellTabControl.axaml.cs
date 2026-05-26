using System;
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views.Agents
{
    /// <summary>
    ///     Interaction logic for VendorSellTabControl.xaml
    /// </summary>
    public partial class VendorSellTabControl : UserControl
    {
        public VendorSellTabControl()
        {
            InitializeComponent();

            // Avalonia's BindingProxy : AvaloniaObject doesn't inherit DataContext
            // through Resources the way WPF's Freezable does. Wire it up explicitly
            // so the Insert button's ContextMenu items can resolve
            // Data.InsertCommand back to the VM.
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
using System;
using Avalonia.Controls;
using Avalonia.Input;
using ClassicAssist.Misc;
using ClassicAssist.UI.ViewModels.Debug;

namespace ClassicAssist.UI.Views.Debug
{
    /// <summary>
    ///     Interaction logic for DebugKeyboardControl.xaml
    /// </summary>
    public partial class DebugKeyboardControl : UserControl
    {
        public delegate void dKeyDown( KeyEventArgs e );

        public DebugKeyboardControl()
        {
            DataContext = new DebugKeyboardViewModel( this );
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so RemoveItem cell command resolves to the VM.
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

        public event dKeyDown WPFKeyDownEvent;

        protected override void OnKeyDown( KeyEventArgs e )
        {
            WPFKeyDownEvent?.Invoke( e );
            base.OnKeyDown( e );
        }
    }
}
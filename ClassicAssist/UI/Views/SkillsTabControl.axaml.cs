using System;
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for SkillsTabControl.xaml
    /// </summary>
    public partial class SkillsTabControl : UserControl
    {
        public SkillsTabControl()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so SetSkillLocks / UseSkill commands resolve.
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
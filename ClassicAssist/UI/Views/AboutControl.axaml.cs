using System.Media;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Input;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for AboutControl.xaml
    /// </summary>
    public partial class AboutControl : UserControl
    {
        private Timer _timer;

        public AboutControl()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseEnter( object sender, PointerEventArgs e )
        {
            if ( !( sender is TextBlock textBlock ) )
            {
                return;
            }

            _timer = new Timer( 1000 );
            _timer.Elapsed += ( o, args ) =>
            {
                _timer.Stop();

                if ( !textBlock.IsPointerOver )
                {
                    return;
                }

                using ( SoundPlayer sound = new SoundPlayer( Properties.Resources.kiss ) )
                {
                    sound.Play();
                }
            };

            _timer.Start();
        }

        private void TextBlock_MouseLeave( object sender, PointerEventArgs e )
        {
            _timer?.Stop();
        }
    }
}
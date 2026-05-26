using ClassicAssist.Misc;
#region License

// Copyright (C) 2020 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Collections.Generic;
using System.Threading;
using Assistant;
using ClassicAssist.UI.ViewModels;
using ClassicAssist.UI.Views;
using ClassicAssist.UO.Objects.Gumps;

namespace ClassicAssist.UO.Gumps
{
    public abstract class RepositionableGump : Gump
    {
        private const int REPOSITION_BUTTON_ID = 100;

        public RepositionableGump()
        {
            
        }
        protected RepositionableGump( int width, int height, int serial, uint gumpID ) : base( 0, 0, serial, gumpID )
        {
            Width = width;
            Height = height;
        }

        public int GumpX { get; set; } = 100;
        public int GumpY { get; set; } = 100;

        public int Height { get; set; }

        public int Width { get; set; }

        public override void SendGump()
        {
            X = GumpX;
            Y = GumpY;
            AddButton( Width - 15, 5, 0x82C, 0x82C, REPOSITION_BUTTON_ID, GumpButtonType.Reply, 0 );

            base.SendGump();
        }

        public override void OnResponse( int buttonID, int[] switches,
            List<(int Key, string Value)> textEntries = null )
        {
            if ( buttonID == REPOSITION_BUTTON_ID )
            {
                // WPF original spawned an STA-pumped thread to host a modal dialog. Avalonia
                // has a single global UI dispatcher; Window ctors VerifyAccess against it and
                // throw "Call from invalid thread" if called from anywhere else. Construct and
                // show on the existing UI thread — non-modal Show() avoids needing async/await.
                // Post (not Invoke): OnResponse runs on the packet worker thread; sync Invoke
                // would risk deadlock if the UI thread is mid-callback into us.
                Avalonia.Threading.Dispatcher.UIThread.Post( () =>
                {
                    try
                    {
                        SetPosition( GumpX, GumpY );
                        RepositionableGumpViewModel vm = new RepositionableGumpViewModel( this, GumpX, GumpY );
                        RepositionableGumpWindow window = new RepositionableGumpWindow { DataContext = vm };
                        window.ShowInTaskbar = false;
                        window.Show();
                    }
                    catch ( System.Exception ex )
                    {
                        System.Console.Error.WriteLine( $"[ClassicAssist] RepositionableGump.OnResponse swallowed {ex.GetType().Name}: {ex.Message}" );
                    }
                } );
            }

            base.OnResponse( buttonID, switches, textEntries );
        }

        public virtual void SetPosition( int x, int y )
        {
            GumpX = x;
            GumpY = y;
        }
    }
}
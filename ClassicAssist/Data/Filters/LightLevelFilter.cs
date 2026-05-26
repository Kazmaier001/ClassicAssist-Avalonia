using Assistant;
using ClassicAssist.UO.Network.PacketFilter;

namespace ClassicAssist.Data.Filters
{
    [FilterOptions( Name = "Light Level", DefaultEnabled = true )]
    public class LightLevelFilter : DynamicFilterEntry
    {
        // Last value the server actually sent in a 0x4F packet (captured before
        // we overlay it). Used to restore the natural lighting when the filter
        // is toggled off — otherwise the world stays artificially bright until
        // the next zone change.
        private int _lastServerLightLevel = 0;

        public LightLevelFilter()
        {
            Options.LightLevelChanged += level =>
            {
                if ( Engine.Connected )
                {
                    SendLightLevel( level );
                }
            };

            Engine.ConnectedEvent += () => SendLightLevel( Options.CurrentOptions.LightLevel );
        }

        protected override void OnChanged( bool enabled )
        {
            // Toggling the filter on mid-session does not normally trigger a CUO
            // world re-render — the server only sends 0x4F on area changes. Push
            // our current level immediately so the screen brightens / darkens
            // without waiting for a teleporter or zone transition.
            if ( !Engine.Connected )
            {
                return;
            }

            if ( enabled && Options.CurrentOptions != null )
            {
                SendLightLevel( Options.CurrentOptions.LightLevel );
            }
            else if ( !enabled )
            {
                // Restore the server's actual ambient light so the world isn't
                // stuck on the brightness we forced while the filter was on.
                Engine.SendPacketToClient( new byte[] { 0x4F, (byte) _lastServerLightLevel }, 2 );
            }
        }

        private void SendLightLevel( int level )
        {
            if ( Enabled )
            {
                Engine.SendPacketToClient( new byte[] { 0x4F, (byte) level }, 2 );
            }
        }

        public override bool CheckPacket( ref byte[] packet, ref int length, PacketDirection direction )
        {
            if ( packet[0] == 0x4E && Enabled )
            {
                return true;
            }

            if ( packet[0] != 0x4F )
            {
                return false;
            }

            // Always remember the server's actual light level, even while the
            // filter is disabled — that's the value we restore on toggle-off.
            _lastServerLightLevel = packet[1];

            if ( !Enabled )
            {
                return false;
            }

            packet[1] = (byte) Options.CurrentOptions.LightLevel;
            return false;
        }
    }
}

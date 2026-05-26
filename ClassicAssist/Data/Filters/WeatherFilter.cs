using Assistant;
using ClassicAssist.UO.Network.PacketFilter;

namespace ClassicAssist.Data.Filters
{
    [FilterOptions( Name = "Weather", DefaultEnabled = true )]
    public class WeatherFilter : DynamicFilterEntry
    {
        public static bool IsEnabled { get; set; }

        // Last weather packet the server actually pushed (captured before
        // we suppress it). Used to restore the natural weather state when
        // the filter is toggled off — otherwise the world stays clear until
        // the next server-side weather change. Mirrors the Season/Light
        // capture-and-restore pattern.
        private byte[] _lastServerWeather;

        protected override void OnChanged( bool enabled )
        {
            IsEnabled = enabled;

            if ( !Engine.Connected )
            {
                return;
            }

            if ( enabled )
            {
                // Force "no weather" immediately so any active rain/snow
                // stops without waiting for an area change.
                byte[] stop = { 0x65, 0xFE, 0x00, 0x00 };
                Engine.SendPacketToClient( stop, stop.Length, false );
            }
            else if ( _lastServerWeather != null )
            {
                Engine.SendPacketToClient( _lastServerWeather, _lastServerWeather.Length, false );
            }
        }

        public override bool CheckPacket( ref byte[] packet, ref int length, PacketDirection direction )
        {
            if ( packet == null || direction != PacketDirection.Incoming )
            {
                return false;
            }

            // Always remember the server's intended weather state — even
            // while the filter is disabled — so we can restore it on
            // toggle-off.
            if ( packet[0] == 0x65 && length >= 4 )
            {
                _lastServerWeather = new[] { packet[0], packet[1], packet[2], packet[3] };
            }

            return IsEnabled && packet[0] == 0x65;
        }
    }
}

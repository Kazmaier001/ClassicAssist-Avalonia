using System;
using ClassicAssist.Misc;
using Assistant;
using ClassicAssist.UI.Views.Filters;
using ClassicAssist.UO.Network.PacketFilter;
using Newtonsoft.Json.Linq;

namespace ClassicAssist.Data.Filters
{
    [FilterOptions( Name = "Seasons", DefaultEnabled = false )]
    public class SeasonFilter : DynamicFilterEntry, IConfigurableFilter
    {
        public static bool IsEnabled { get; set; }
        public Season SelectedSeason { get; set; } = Season.Spring;

        // Last season the server actually pushed in a 0xBC packet (captured
        // before we suppress it). Used to restore natural lighting/foliage
        // when the filter is toggled off — otherwise the world stays stuck
        // on whatever season we forced until the next zone change.
        private Season _lastServerSeason = Season.Spring;

        public void Configure()
        {
            SeasonFilterConfigureWindow window = new SeasonFilterConfigureWindow( SelectedSeason );
            window.Closed += ( s, e ) =>
            {
                SelectedSeason = window.SelectedSeason;

                // Only push the season to the client if the filter is actually
                // enabled — otherwise picking a season in the config dialog
                // would force the world into that season even with the filter
                // checkbox off.
                if ( IsEnabled )
                {
                    SendSeason();
                }
            };
            window.ShowDialog();
        }

        public void Deserialize( JToken token )
        {
            if ( token == null )
            {
                return;
            }

            JObject config = (JObject) token;

            if ( Enum.TryParse( config["Season"]?.ToString(), out Season season ) )
            {
                SelectedSeason = season;
            }
        }

        public JObject Serialize()
        {
            JObject config = new JObject { { "Season", SelectedSeason.ToString() } };

            return config;
        }

        public void ResetOptions()
        {
            SelectedSeason = Season.Spring;
        }

        protected override void OnChanged( bool enabled )
        {
            IsEnabled = enabled;

            if ( !Engine.Connected )
            {
                return;
            }

            if ( enabled )
            {
                SendSeason();
            }
            else
            {
                byte[] restore = { 0xBC, (byte) _lastServerSeason, 0x00 };
                Engine.SendPacketToClient( restore, restore.Length, false );
            }
        }

        public override bool CheckPacket( ref byte[] packet, ref int length, PacketDirection direction )
        {
            if ( packet == null )
            {
                return false;
            }

            // Always remember the server's intended season, even while the
            // filter is disabled — that's the value we restore on toggle-off.
            if ( packet[0] == 0xBC && direction == PacketDirection.Incoming && length >= 2 )
            {
                if ( Enum.IsDefined( typeof( Season ), packet[1] ) )
                {
                    _lastServerSeason = (Season) packet[1];
                }
            }

            if ( !IsEnabled )
            {
                return false;
            }

            if ( packet[0] != 0xBF || packet[4] != 0x08 || direction != PacketDirection.Incoming )
            {
                return packet[0] == 0xBC && direction == PacketDirection.Incoming;
            }

            SendSeason();
            return false;

        }

        private void SendSeason()
        {
            byte[] season = { 0xBC, (byte) SelectedSeason, 0x00 };

            Engine.SendPacketToClient( season, season.Length, false );
        }
    }

    public enum Season : byte
    {
        Spring,
        Summer,
        Fall,
        Winter,
        Desolation
    }
}
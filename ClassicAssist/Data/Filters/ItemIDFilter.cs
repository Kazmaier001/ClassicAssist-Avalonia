#region License

// Copyright (C) 2023 Reetus
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System;
using ClassicAssist.Misc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Assistant;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.ViewModels.Filters;
using ClassicAssist.UI.Views.Filters;
using ClassicAssist.UO;
using ClassicAssist.UO.Network.PacketFilter;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using Newtonsoft.Json.Linq;

namespace ClassicAssist.Data.Filters
{
    [FilterOptions( Name = "ItemID Filter", DefaultEnabled = false )]
    public class ItemIDFilter : DynamicFilterEntry, IConfigurableFilter
    {
        public ObservableCollection<ItemIDFilterEntry> Items { get; set; } =
            new ObservableCollection<ItemIDFilterEntry>();

        // Map of items we've actively swapped while the filter was on:
        // serial → (original SourceID, original Hue). Used to restore in-view
        // items on toggle-off — without it, items rendered with the override
        // ID stay that way until the next zone change. Hue == -1 means the
        // entry's Hue override was -1 (don't touch), so we leave hue alone
        // on restore too.
        private readonly Dictionary<int, (int Id, int Hue)> _swapped = new Dictionary<int, (int, int)>();

        public void Configure()
        {
            ItemIDFilterConfigureWindowViewModel vm = new ItemIDFilterConfigureWindowViewModel( Items );
            ItemIDFilterConfigureWindow window = new ItemIDFilterConfigureWindow { DataContext = vm };
            window.ShowDialog();
        }

        public void Deserialize( JToken token )
        {
            if ( token?["Items"] == null )
            {
                return;
            }

            foreach ( JToken itemsToken in token["Items"] )
            {
                ItemIDFilterEntry entry = new ItemIDFilterEntry
                {
                    Enabled = itemsToken["Enabled"]?.ToObject<bool>() ?? false,
                    SourceID = itemsToken["SourceID"]?.ToObject<int>() ?? 0,
                    DestinationID = itemsToken["DestinationID"]?.ToObject<int>() ?? 0,
                    Hue = itemsToken["Hue"]?.ToObject<int>() ?? -1
                };

                Items.Add( entry );
            }
        }

        public JObject Serialize()
        {
            JObject config = new JObject();

            JArray items = new JArray();

            foreach ( ItemIDFilterEntry item in Items )
            {
                items.Add( new JObject
                {
                    { "Enabled", item.Enabled },
                    { "SourceID", item.SourceID },
                    { "DestinationID", item.DestinationID },
                    { "Hue", item.Hue }
                } );
            }

            config.Add( "Items", items );

            return config;
        }

        public void ResetOptions()
        {
            Items.Clear();
            _swapped.Clear();
        }

        protected override void OnChanged( bool enabled )
        {
            if ( !Engine.Connected )
            {
                if ( !enabled )
                {
                    _swapped.Clear();
                }

                return;
            }

            if ( enabled )
            {
                // Re-render items currently in view that match an enabled
                // entry — without this, only NEW packets pick up the swap.
                foreach ( Item item in Engine.Items.ToList() )
                {
                    if ( item == null )
                    {
                        continue;
                    }

                    ItemIDFilterEntry entry = Items.FirstOrDefault( e => e.Enabled && e.SourceID == item.ID );

                    if ( entry == null )
                    {
                        continue;
                    }

                    _swapped[item.Serial] = ( item.ID, entry.Hue == -1 ? -1 : item.Hue );

                    int hueOverride = entry.Hue == -1 ? -1 : entry.Hue;
                    Engine.SendPacketToClient( new SAWorldItem( item, entry.DestinationID, hueOverride ) );
                }
            }
            else
            {
                // Restore items we previously swapped to their original
                // ID + hue. Anything not in _swapped was never touched.
                foreach ( KeyValuePair<int, (int Id, int Hue)> kvp in _swapped.ToList() )
                {
                    Item item = Engine.Items.GetItem( kvp.Key );

                    if ( item == null )
                    {
                        continue;
                    }

                    int hueOverride = kvp.Value.Hue == -1 ? -1 : kvp.Value.Hue;
                    Engine.SendPacketToClient( new SAWorldItem( item, kvp.Value.Id, hueOverride ) );
                }

                _swapped.Clear();
            }
        }

        public override bool CheckPacket( ref byte[] packet, ref int length, PacketDirection direction )
        {
            if ( packet == null || !Enabled )
            {
                return false;
            }

            if ( direction != PacketDirection.Incoming )
            {
                return false;
            }

            switch ( packet[0] )
            {
                case 0xF3:
                {
                    int itemId = ( packet[8] << 8 ) | packet[9];

                    ItemIDFilterEntry entry = Items.FirstOrDefault( e => e.SourceID == itemId && e.Enabled );

                    if ( entry == null )
                    {
                        return false;
                    }

                    int serial = ( packet[4] << 24 ) | ( packet[5] << 16 ) | ( packet[6] << 8 ) | packet[7];
                    int originalHue = ( packet[21] << 8 ) | packet[22];

                    CaptureSwap( serial, itemId, originalHue, entry );

                    packet[8] = (byte) ( entry.DestinationID >> 8 );
                    packet[9] = (byte) entry.DestinationID;

                    if ( entry.Hue == -1 )
                    {
                        return false;
                    }

                    packet[21] = (byte) ( entry.Hue >> 8 );
                    packet[22] = (byte) entry.Hue;
                    break;
                }
                case 0x3C:
                {
                    bool oldStyle = false;

                    int count = ( packet[3] << 8 ) | packet[4];

                    if ( length / 20 != count )
                    {
                        oldStyle = true;
                    }

                    for ( int i = 0; i < count; i++ )
                    {
                        int offset = 5 + i * ( oldStyle ? 19 : 20 );

                        int itemId = ( packet[offset + 4] << 8 ) | packet[offset + 5];

                        ItemIDFilterEntry entry = Items.FirstOrDefault( e => e.SourceID == itemId && e.Enabled );

                        if ( entry == null )
                        {
                            continue;
                        }

                        int serial = ( packet[offset] << 24 ) | ( packet[offset + 1] << 16 ) | ( packet[offset + 2] << 8 ) | packet[offset + 3];
                        int hueOffset = offset + ( oldStyle ? 17 : 18 );
                        int originalHue = ( packet[hueOffset] << 8 ) | packet[hueOffset + 1];

                        CaptureSwap( serial, itemId, originalHue, entry );

                        packet[offset + 4] = (byte) ( entry.DestinationID >> 8 );
                        packet[offset + 5] = (byte) entry.DestinationID;

                        if ( entry.Hue == -1 )
                        {
                            continue;
                        }

                        packet[hueOffset] = (byte) ( entry.Hue >> 8 );
                        packet[hueOffset + 1] = (byte) entry.Hue;
                    }

                    break;
                }
                case 0x25:
                {
                    int itemId = ( packet[5] << 8 ) | packet[6];

                    ItemIDFilterEntry entry = Items.FirstOrDefault( e => e.SourceID == itemId && e.Enabled );

                    if ( entry == null )
                    {
                        return false;
                    }

                    int serial = ( packet[1] << 24 ) | ( packet[2] << 16 ) | ( packet[3] << 8 ) | packet[4];

                    bool oldStyle25 = length != 21;
                    int hueOffset25 = oldStyle25 ? 18 : 19;
                    int originalHue = ( packet[hueOffset25] << 8 ) | packet[hueOffset25 + 1];

                    CaptureSwap( serial, itemId, originalHue, entry );

                    packet[5] = (byte) ( entry.DestinationID >> 8 );
                    packet[6] = (byte) entry.DestinationID;

                    if ( entry.Hue == -1 )
                    {
                        return false;
                    }

                    packet[hueOffset25] = (byte) ( entry.Hue >> 8 );
                    packet[hueOffset25 + 1] = (byte) entry.Hue;
                    break;
                }
                case 0x1A:
                {
                    int serial = ( packet[3] << 24 ) | ( packet[4] << 16 ) | ( packet[5] << 8 ) | packet[6];
                    int itemId = ( packet[7] << 8 ) | packet[8];

                    ItemIDFilterEntry entry = Items.FirstOrDefault( e => e.SourceID == itemId && e.Enabled );

                    if ( entry == null )
                    {
                        return false;
                    }

                    // We don't bother extracting the original hue from 0x1A —
                    // the wire format is variable and the only consumer
                    // (restore on toggle-off) can fall back to "leave hue
                    // alone" by storing -1.
                    CaptureSwap( serial, itemId, -1, entry );

                    packet[7] = (byte) ( entry.DestinationID >> 8 );
                    packet[8] = (byte) entry.DestinationID;

                    if ( entry.Hue == -1 )
                    {
                        return false;
                    }

                    bool hasAmount = ( serial & 0x80000000 ) != 0;

                    int x = hasAmount ? ( packet[11] << 8 ) | packet[12] : ( packet[9] << 8 ) | packet[10];
                    int y = hasAmount ? ( packet[13] << 8 ) | packet[14] : ( packet[11] << 8 ) | packet[12];

                    bool hasLightSource = ( x & 0x8000 ) != 0;
                    bool hasHue = ( y & 0x8000 ) != 0;
                    bool hasFlags = ( y & 0x4000 ) != 0;

                    byte flags = 0;

                    if ( hasFlags )
                    {
                        int flagsOffset = 14;

                        if ( hasAmount )
                        {
                            flagsOffset += 2;
                        }

                        if ( hasLightSource )
                        {
                            flagsOffset += 1;
                        }

                        if ( hasHue )
                        {
                            flagsOffset += 2;
                        }

                        flags = packet[flagsOffset];
                    }

                    if ( !hasHue )
                    {
                        y |= 0x8000;

                        packet[hasAmount ? 13 : 11] = (byte) ( y >> 8 );
                        packet[hasAmount ? 14 : 12] = (byte) y;

                        Array.Resize( ref packet, length + 2 );
                        length = packet.Length;
                    }

                    int hueOffset = 14;

                    if ( hasAmount )
                    {
                        hueOffset += 2;
                    }

                    if ( hasLightSource )
                    {
                        hueOffset += 1;
                    }

                    packet[hueOffset] = (byte) ( entry.Hue >> 8 );
                    packet[hueOffset + 1] = (byte) entry.Hue;

                    if ( hasFlags )
                    {
                        packet[hueOffset + 2] = flags;
                    }

                    break;
                }
            }

            return false;
        }

        // Only capture once per (serial, source) pair during a filter-on
        // window — repeat packets after the first swap carry the override
        // values, not the originals.
        private void CaptureSwap( int serial, int sourceId, int originalHue, ItemIDFilterEntry entry )
        {
            if ( _swapped.ContainsKey( serial ) )
            {
                return;
            }

            _swapped[serial] = ( sourceId, entry.Hue == -1 ? -1 : originalHue );
        }
    }

    public class ItemIDFilterEntry : SetPropertyNotifyChanged
    {
        private int _destinationId;
        private bool _enabled;
        private int _hue = -1;
        private int _sourceId;

        public int DestinationID
        {
            get => _destinationId;
            set => SetProperty( ref _destinationId, value );
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty( ref _enabled, value );
        }

        public int Hue
        {
            get => _hue;
            set => SetProperty( ref _hue, value );
        }

        public int SourceID
        {
            get => _sourceId;
            set => SetProperty( ref _sourceId, value );
        }
    }
}

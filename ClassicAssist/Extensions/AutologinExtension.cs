#region License

// Copyright (C) 2022 Reetus
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
using ClassicAssist.Data;

namespace ClassicAssist.Extensions
{
    // ClassicAssist's autologin was a reflection bridge into LoginScene.Connect /
    // SelectServer / SelectCharacter. Under the CUO Bootstrap split (1.1.x+), those
    // managed types are decoy stubs — `Client.Game` is never assigned, `GetScene<T>()`
    // ignores T, and `LoginScene.Connect()` is just Console.WriteLine. There's no
    // supported plugin path to drive autologin under this CUO architecture (no scene
    // reflection, no login RPC opcode, no way to open the login-server TCP socket from
    // a plugin). See memory: cuo-bootstrap-split-architecture.
    //
    // The replacement is CUO's own native autologin in settings.json:
    //   "username": "...", "password": "...", "saveaccount": true,
    //   "autologin": true, "reconnect": true, "lastservernum": N
    public class AutologinExtension : IExtension
    {
        public void Initialize()
        {
            Console.WriteLine(
                "[ClassicAssist] AutologinExtension disabled under CUO Bootstrap split: " +
                "use CUO's native autologin in settings.json (username/password/saveaccount/autologin/lastservernum)." );
        }
    }

    // Retained because UI/serialization code still references ClassicAssist.Extensions.ServerEntry.
    public class ServerEntry
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }
}

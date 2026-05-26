#region License

// Copyright (C) 2021 Reetus
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

using ClassicAssist.Data.Dress;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.Data.Hotkeys.Commands;
using ClassicAssist.Data.Macros;
using ClassicAssist.Data.Organizer;

namespace ClassicAssist.Data.ClassicUO
{
    public static class Macros
    {
        // PlayCUOMacro and CreateMacroButton used to reach the live ClassicUO MacroManager /
        // UIManager via reflection on Engine.ClassicAssembly. Under the CUO Bootstrap split
        // (1.1.x+) those managed types are decoy stubs — `Client.Game` is null, `GetScene<T>()`
        // ignores T, the real instances live in native cuo.dll and aren't reachable from a
        // plugin. There is no supported replacement (no scene RPC, no UI RPC, no macro RPC).
        // See memory: cuo-bootstrap-split-architecture.
        //
        // The reflection-based in-process path is kept ONLY for ClassicAssist.Tests, which
        // doesn't run against real CUO and provides its own type fixtures.
        public static void PlayCUOMacro( string name )
        {
            UO.Commands.SystemMessage( "PlayCUOMacro: unavailable under modern ClassicUO (Bootstrap split — managed MacroManager is a stub)." );
        }

        public static void CreateMacroButton( MacroEntry macroEntry )
        {
            CreateMacroButton( macroEntry.Name, macroEntry.Name );
        }

        public static void CreateMacroButton( HotkeyEntry hotkeyEntry )
        {
            string macroText = hotkeyEntry.GetType().ToString();

            switch ( hotkeyEntry )
            {
                case MacroEntry entry:
                    macroText = entry.Name;
                    break;
                case HotkeyCommand _:
                case OrganizerEntry _:
                case DressAgentEntry _:
                    macroText = $"{hotkeyEntry.GetType()}|{hotkeyEntry.Name}";
                    break;
            }

            CreateMacroButton( hotkeyEntry.Name, macroText );
        }

        private static void CreateMacroButton( string name, string macroText )
        {
            UO.Commands.SystemMessage( "CreateMacroButton: unavailable under modern ClassicUO (Bootstrap split — managed UIManager is a stub)." );
        }
    }
}
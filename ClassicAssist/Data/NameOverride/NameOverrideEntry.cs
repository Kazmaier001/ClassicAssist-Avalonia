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

using ClassicAssist.Shared.UI;

namespace ClassicAssist.Data.NameOverride
{
    public class NameOverrideEntry : SetPropertyNotifyChanged
    {
        private bool _enabled = true;
        private string _name;
        private int _serial;
        private string _notes;

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty( ref _enabled, value );
        }

        public string Name
        {
            get => _name;
            set => SetProperty( ref _name, value );
        }

        public int Serial
        {
            get => _serial;
            set => SetProperty( ref _serial, value );
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty( ref _notes, value );
        }
    }
}

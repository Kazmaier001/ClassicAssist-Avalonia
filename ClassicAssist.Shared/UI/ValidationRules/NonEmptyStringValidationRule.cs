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

namespace ClassicAssist.Shared.UI.ValidationRules
{
    /// <summary>
    /// Simple validation helper for non-empty strings.
    /// Avalonia uses DataAnnotations or custom validation via INotifyDataErrorInfo
    /// rather than WPF's ValidationRule system. This class is kept as a utility.
    /// </summary>
    public static class NonEmptyStringValidation
    {
        public static bool IsValid( object value )
        {
            if ( !( value is string contents ) )
            {
                return false;
            }

            return !string.IsNullOrEmpty( contents );
        }
    }
}
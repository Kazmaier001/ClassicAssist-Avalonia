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

using Avalonia;
using Avalonia.VisualTree;

namespace ClassicAssist.Shared.UI
{
    public static class ExtensionMethods
    {
        public static T GetChildOfType<T>( this Visual visual ) where T : Visual
        {
            if ( visual == null )
            {
                return null;
            }

            var children = visual.GetVisualChildren();

            foreach ( var child in children )
            {
                T result = child as T ?? GetChildOfType<T>( child as Visual );

                if ( result != null )
                {
                    return result;
                }
            }

            return null;
        }
    }
}
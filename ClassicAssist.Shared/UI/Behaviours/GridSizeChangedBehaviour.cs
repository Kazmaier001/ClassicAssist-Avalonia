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

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Shared.UI.Behaviours
{
    public class GridSizeChangedBehaviour : Behavior<Grid>
    {
        public static readonly StyledProperty<double> WidthProperty =
            AvaloniaProperty.Register<GridSizeChangedBehaviour, double>( nameof( Width ), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay );

        public static readonly StyledProperty<double> HeightProperty =
            AvaloniaProperty.Register<GridSizeChangedBehaviour, double>( nameof( Height ), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay );

        public double Height
        {
            get => GetValue( HeightProperty );
            set => SetValue( HeightProperty, value );
        }

        public double Width
        {
            get => GetValue( WidthProperty );
            set => SetValue( WidthProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PropertyChanged += OnPropertyChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged( object sender, AvaloniaPropertyChangedEventArgs e )
        {
            if ( e.Property != Layoutable.BoundsProperty )
            {
                return;
            }

            if ( !( sender is Grid grid ) )
            {
                return;
            }

            var bounds = grid.Bounds;

            if ( bounds.Width > 0 )
            {
                if ( Math.Abs( Width - bounds.Width ) > 0 )
                {
                    Width = bounds.Width + grid.Margin.Left + grid.Margin.Right;
                }
            }

            if ( bounds.Height > 0 )
            {
                Height = bounds.Height + grid.Margin.Top + grid.Margin.Bottom;
            }
        }
    }
}
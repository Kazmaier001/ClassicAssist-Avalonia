using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClassicAssist.UI.Misc.Behaviours
{
    /// <summary>
    ///     Attached behaviour that toggles a "hasvbar" CSS-style class on a
    ///     <see cref="DataGrid" /> whenever its internal
    ///     <c>PART_VerticalScrollbar</c> flips visible.  DarkTheme.axaml uses
    ///     the class to apply a 20-DIP right margin to the row selection /
    ///     hover rectangle only when a scrollbar is actually present, so the
    ///     highlight never paints under the bar but no dead gap is reserved
    ///     when content fits.
    ///     Applied globally to every DataGrid via a style setter in
    ///     DarkTheme.axaml; individual grids can opt out by setting
    ///     <c>ReserveSpace="False"</c>.
    /// </summary>
    public static class DataGridScrollBarReservation
    {
        public static readonly AttachedProperty<bool> ReserveSpaceProperty =
            AvaloniaProperty.RegisterAttached<DataGrid, bool>(
                "ReserveSpace", typeof( DataGridScrollBarReservation ) );

        static DataGridScrollBarReservation()
        {
            ReserveSpaceProperty.Changed.AddClassHandler<DataGrid>( OnReserveSpaceChanged );
        }

        public static void SetReserveSpace( DataGrid grid, bool value )
        {
            grid.SetValue( ReserveSpaceProperty, value );
        }

        public static bool GetReserveSpace( DataGrid grid )
        {
            return grid.GetValue( ReserveSpaceProperty );
        }

        private static void OnReserveSpaceChanged( DataGrid grid,
            AvaloniaPropertyChangedEventArgs e )
        {
            if ( e.NewValue is bool enabled && enabled )
            {
                if ( grid.IsLoaded )
                {
                    Hook( grid );
                }
                else
                {
                    grid.Loaded += OnLoaded;
                }
            }
        }

        private static void OnLoaded( object sender, Avalonia.Interactivity.RoutedEventArgs e )
        {
            if ( sender is DataGrid grid )
            {
                grid.Loaded -= OnLoaded;
                Hook( grid );
            }
        }

        private static void Hook( DataGrid grid )
        {
            foreach ( var dv in grid.GetVisualDescendants() )
            {
                if ( dv is ScrollBar sb && sb.Name == "PART_VerticalScrollbar" )
                {
                    void Sync()
                    {
                        grid.Classes.Set( "hasvbar", sb.IsVisible );
                    }

                    Sync();
                    sb.GetPropertyChangedObservable( Visual.IsVisibleProperty )
                        .Subscribe( new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                            _ => Dispatcher.UIThread.Post( Sync, DispatcherPriority.Background ) ) );
                    return;
                }
            }
        }
    }
}

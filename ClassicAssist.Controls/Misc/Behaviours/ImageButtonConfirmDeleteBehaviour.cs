#region License

// Copyright (C) 2025 Reetus
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Controls.Misc.Behaviours
{
    public class ImageButtonConfirmDeleteBehaviour : Behavior<ImageButton>
    {
        private CancellationTokenSource _cancellationTokenSource;

        // Two pre-built DrawingImage instances we swap Image.Source between.
        // Avalonia's Image control re-renders when Source changes, but does NOT
        // re-render when you mutate a Brush inside the currently-assigned
        // DrawingImage's DrawingGroup — the GeometryDrawing.Brush change
        // doesn't propagate up to invalidate the Image. Earlier port mutated
        // brushes in place and the X stayed red forever (Reset ran, brushes
        // were reassigned, but no visible change). Swap-source is the fix.
        private DrawingImage _originalDrawingImage;
        private DrawingImage _redDrawingImage;

        private bool _isPending;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnClick;
            // We intentionally do NOT capture/null AssociatedObject.Command here.
            // Behaviors attach during XAML parsing, BEFORE Avalonia resolves the
            // Click target's Command binding — so the old "save to _deleteCommand,
            // null out Command" pattern left _deleteCommand null forever and the
            // second click never invoked anything. Instead we read Command live on
            // commit and use e.Handled=true to suppress the button's own auto-execute
            // on the first (arming) click.
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= OnClick;
        }

        private void OnClick( object sender, RoutedEventArgs e )
        {
            // Mark the click handled so Avalonia Button's default Command.Execute
            // doesn't fire on the first (arming) click. We invoke the Command
            // manually on the second click below.
            e.Handled = true;

            if ( !_isPending )
            {
                _isPending = true;
                PrepareImages();
                ShowImage( _redDrawingImage );
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = _cancellationTokenSource.Token;
                Task.Delay( 2000, token ).ContinueWith( t =>
                {
                    if ( !t.IsCanceled )
                    {
                        Reset();
                    }
                }, TaskScheduler.Default );
            }
            else
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                // Snapshot Command + parameter BEFORE executing — Execute typically
                // removes the bound item from its collection, which detaches this
                // behavior and nulls AssociatedObject.
                var cmd = AssociatedObject.Command;
                var param = AssociatedObject.CommandParameter;
                if ( cmd?.CanExecute( param ) == true )
                {
                    cmd.Execute( param );
                }

                // If we're still attached (item not removed by Execute), revert the
                // icon. After a successful delete the container is gone and there's
                // nothing to revert — skip silently.
                if ( AssociatedObject != null )
                {
                    ShowImage( _originalDrawingImage );
                }
                _isPending = false;
            }
        }

        private void PrepareImages()
        {
            if ( _originalDrawingImage != null && _redDrawingImage != null )
            {
                return;
            }

            if ( !( AssociatedObject.Content is Image img ) )
            {
                return;
            }

            if ( !( img.Source is DrawingImage original ) )
            {
                return;
            }

            _originalDrawingImage = original;

            if ( original.Drawing is DrawingGroup sourceGroup )
            {
                _redDrawingImage = new DrawingImage( CloneGroupWithBrush( sourceGroup, Brushes.Red ) );
            }
            else
            {
                // Non-group drawing — best-effort, just reuse original (no red variant).
                _redDrawingImage = original;
            }
        }

        private static DrawingGroup CloneGroupWithBrush( DrawingGroup source, IBrush brush )
        {
            var clone = new DrawingGroup();
            foreach ( var child in source.Children )
            {
                if ( child is GeometryDrawing gd )
                {
                    clone.Children.Add( new GeometryDrawing
                    {
                        Brush = brush,
                        Geometry = gd.Geometry,
                        Pen = gd.Pen
                    } );
                }
                else if ( child is DrawingGroup nested )
                {
                    // Recurse so nested groups (icons frequently nest groups for
                    // clip/opacity wrapping) also get the recolour.
                    clone.Children.Add( CloneGroupWithBrush( nested, brush ) );
                }
                else
                {
                    clone.Children.Add( child );
                }
            }
            return clone;
        }

        private void ShowImage( DrawingImage drawing )
        {
            if ( drawing == null || AssociatedObject == null )
            {
                return;
            }
            if ( AssociatedObject.Content is Image img )
            {
                img.Source = drawing;
            }
        }

        private void Reset()
        {
            Dispatcher.UIThread.Invoke( () =>
            {
                // AssociatedObject can be null if the item was removed (and behavior
                // detached) before the timer fired — guard against the late revert.
                if ( AssociatedObject == null )
                {
                    _isPending = false;
                    return;
                }
                ShowImage( _originalDrawingImage );
                _isPending = false;
            } );
        }
    }
}

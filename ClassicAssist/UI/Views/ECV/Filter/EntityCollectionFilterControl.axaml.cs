using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;

namespace ClassicAssist.UI.Views.ECV.Filter
{
    /// <summary>
    ///     Interaction logic for EntityCollectionFilterControl.xaml
    /// </summary>
    public partial class EntityCollectionFilterControl : UserControl
    {
        public static readonly StyledProperty<ICommand> CommandProperty =
            AvaloniaProperty.Register<EntityCollectionFilterControl, ICommand>( nameof( Command ) );

        public static readonly StyledProperty<ObservableCollection<Assembly>> AssembliesProperty =
            AvaloniaProperty.Register<EntityCollectionFilterControl, ObservableCollection<Assembly>>( nameof( Assemblies ) );

        static EntityCollectionFilterControl()
        {
            CommandProperty.Changed.AddClassHandler<EntityCollectionFilterControl>( ( s, e ) => PropertyChangedCallback( s, e ) );
            AssembliesProperty.Changed.AddClassHandler<EntityCollectionFilterControl>( ( s, e ) => PropertyChangedCallback( s, e ) );
            IsVisibleProperty.Changed.AddClassHandler<EntityCollectionFilterControl>( ( s, e ) => s.OnIsVisibleChanged( s, e ) );
        }

        public EntityCollectionFilterControl()
        {
            InitializeComponent();
            // Unloaded is more reliable than IsVisibleChanged for "window is going
            // away" on Avalonia — IsVisible doesn't tick to False on a typical
            // window close, so OnIsVisibleChanged below never fires and profile
            // edits were silently lost on close/reopen.
            Unloaded += ( _, _ ) =>
            {
                if ( DataContext is EntityCollectionFilterViewModel vm )
                {
                    vm.SaveFilterProfiles();
                }
            };
        }

        public ObservableCollection<Assembly> Assemblies
        {
            get => (ObservableCollection<Assembly>) GetValue( AssembliesProperty );
            set => SetValue( AssembliesProperty, value );
        }

        public ICommand Command
        {
            get => (ICommand) GetValue( CommandProperty );
            set => SetValue( CommandProperty, value );
        }

        private void OnIsVisibleChanged( object sender, AvaloniaPropertyChangedEventArgs e )
        {
            if ( DataContext is EntityCollectionFilterViewModel viewModel )
            {
                viewModel.SaveFilterProfiles();
            }
        }

        private static void PropertyChangedCallback( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            if ( !( d is EntityCollectionFilterControl control ) )
            {
                return;
            }

            if ( !( control.DataContext is EntityCollectionFilterViewModel viewModel ) )
            {
                return;
            }

            switch ( e.NewValue )
            {
                case ICommand command:
                    viewModel.Command = command;
                    break;
            }
        }
    }
}
using ClassicAssist.Misc;
#region License

// Copyright (C) 2025 Reetus
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Controls;
using ClassicAssist.Shared.Resources;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.ViewModels.Autoloot;
using ClassicAssist.UI.Views.Autoloot;
using ClassicAssist.UO;
using ClassicAssist.UO.Objects;
using UserControl = Avalonia.Controls.UserControl;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for MultiClilocSelector.xaml
    /// </summary>
    public partial class MultiClilocSelector : UserControl
    {
        public static readonly StyledProperty<ObservableCollection<int>> ValuesProperty = AvaloniaProperty.Register<MultiClilocSelector, ObservableCollection<int>>( nameof( Values ) );

        private ICommand _chooseClilocCommand;
        private ICommand _chooseFromItemCommand;

        public MultiClilocSelector()
        {
            InitializeComponent();

            // Wire Commands in code-behind: buttons live inside
            // MultiValueSelector's Popup, beyond reach of any XAML binding.
            var chooseCliloc = this.FindControl<ImageButton>( "ChooseClilocButton" );
            if ( chooseCliloc != null )
            {
                chooseCliloc.Command = ChooseClilocCommand;
            }

            var chooseFromItem = this.FindControl<ImageButton>( "ChooseFromItemButton" );
            if ( chooseFromItem != null )
            {
                chooseFromItem.Command = ChooseFromItemCommand;
            }
        }

        public ICommand ChooseClilocCommand => _chooseClilocCommand ?? ( _chooseClilocCommand = new RelayCommand( ChooseCliloc ) );
        public ICommand ChooseFromItemCommand => _chooseFromItemCommand ?? ( _chooseFromItemCommand = new RelayCommandAsync( ChooseFromItem, o => Engine.Connected ) );

        public ObservableCollection<int> Values
        {
            get => (ObservableCollection<int>) GetValue( ValuesProperty );
            set => SetValue( ValuesProperty, value );
        }

        private async void ChooseCliloc( object obj )
        {
            if ( Values == null )
            {
                Values = new ObservableCollection<int>();
            }

            ClilocSelectionViewModel vm = new ClilocSelectionViewModel();
            ClilocSelectionWindow window = new ClilocSelectionWindow { DataContext = vm };

            await window.ShowDialogAsync();

            if ( vm.DialogResult != true || Values.Contains( vm.SelectedCliloc.Key ) )
            {
                return;
            }

            Values.Add( vm.SelectedCliloc.Key );
        }

        private async Task ChooseFromItem( object obj )
        {
            if ( Values == null )
            {
                Values = new ObservableCollection<int>();
            }

            int serial = await Commands.GetTargetSerialAsync( Strings.Target_object___, 90000 );

            if ( serial == 0 )
            {
                Commands.SystemMessage( Strings.Cannot_find_item___ );
                return;
            }

            Item item = Engine.Items.GetItem( serial );

            if ( item == null )
            {
                Commands.SystemMessage( Strings.Cannot_find_item___ );
                return;
            }

            if ( item.Properties == null )
            {
                Commands.SystemMessage( Strings.Item_properties_null_or_not_loaded___ );
                return;
            }

            PropertySelectionViewModel vm = new PropertySelectionViewModel( item.Properties );
            PropertySelectionWindow window = new PropertySelectionWindow { DataContext = vm };
            await window.ShowDialogAsync();

            if ( vm.DialogResult != true )
            {
                return;
            }

            IEnumerable<SelectProperties> selectedProperties = vm.Properties.Where( p => p.Selected );

            foreach ( SelectProperties property in selectedProperties )
            {
                if ( !Values.Contains( property.Property.Cliloc ) )
                {
                    Values.Add( property.Property.Cliloc );
                }
            }
        }
    }
}
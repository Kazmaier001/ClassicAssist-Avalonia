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

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Controls;
using ClassicAssist.Shared.UI;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Objects;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for MultiItemIDSelector.xaml
    /// </summary>
    public partial class MultiItemIDSelector : UserControl
    {
        public static readonly StyledProperty<ObservableCollection<int>> ValuesProperty = AvaloniaProperty.Register<MultiItemIDSelector, ObservableCollection<int>>( nameof( Values ) );

        private ICommand _targetCommand;

        public MultiItemIDSelector()
        {
            InitializeComponent();

            // Wire the Command in code-behind: the button is hosted inside
            // MultiValueSelector's Popup, beyond reach of any binding from
            // this XAML's NameScope or visual tree.
            var targetButton = this.FindControl<ImageButton>( "TargetButton" );
            if ( targetButton != null )
            {
                targetButton.Command = TargetCommand;
            }
        }

        public ICommand TargetCommand => _targetCommand ?? ( _targetCommand = new RelayCommandAsync( Target, o => Engine.Connected ) );

        public ObservableCollection<int> Values
        {
            get => (ObservableCollection<int>) GetValue( ValuesProperty );
            set => SetValue( ValuesProperty, value );
        }

        private async Task Target( object obj )
        {
            if ( Values == null )
            {
                Values = new ObservableCollection<int>();
            }

            ( TargetType targetType, TargetFlags targetFlags, int serial, int x, int y, int z, int itemId ) = await Commands.GetTargetInfoAsync( objectTarget: true );

            if ( itemId <= 0 && serial <= 0 )
            {
                return;
            }

            if ( itemId > 0 && !Values.Contains( itemId ) )
            {
                Values.Add( itemId );
            }
            else if ( serial > 0 )
            {
                Item item = Engine.Items.GetItem( serial );

                if ( item != null && !Values.Contains( item.ID ) )
                {
                    Values.Add( item.ID );
                }
            }
        }
    }
}
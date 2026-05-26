using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Input;
using System.Windows.Input;
using ClassicAssist.Data.Macros;
using ClassicAssist.Shared.UI;

namespace ClassicAssist.UI.ViewModels.Macros
{
    public class CommandsData
    {
        public CommandsDisplayAttribute Attribute { get; set; }
        public string Category { get; set; }
        public bool IsExpanded { get; set; }
        public string Name { get; set; }
        public string Tooltip { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CommandsCategory
    {
        public string Name { get; set; }
        public ObservableCollection<CommandsData> Items { get; set; } = new ObservableCollection<CommandsData>();
    }

    public class MacrosCommandViewModel : BaseViewModel
    {
        private readonly MacrosTabViewModel _macrosViewModel;
        private ICommand _insertCommand;
        private ObservableCollection<CommandsData> _items = new ObservableCollection<CommandsData>();
        private ObservableCollection<CommandsCategory> _categories = new ObservableCollection<CommandsCategory>();
        private CommandsData _selectedItem;

        public MacrosCommandViewModel( MacrosTabViewModel macros ) : this()
        {
            _macrosViewModel = macros;
        }

        public MacrosCommandViewModel()
        {
            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where( t =>
                t.Namespace != null && t.Namespace.StartsWith( "ClassicAssist.Data.Macros.Commands" ) );

            foreach ( Type type in types )
            {
                MemberInfo[] members = type.GetMembers( BindingFlags.Public | BindingFlags.Static );

                foreach ( MemberInfo memberInfo in members )
                {
                    CommandsDisplayAttribute attr = memberInfo.GetCustomAttribute<CommandsDisplayAttribute>();

                    if ( attr == null )
                    {
                        continue;
                    }

                    CommandsData entry = new CommandsData
                    {
                        Category = attr.Category,
                        IsExpanded = false,
                        Name = memberInfo.ToString(),
                        Tooltip = attr.Description,
                        Attribute = attr
                    };

                    Items.Add( entry );
                }
            }

            foreach ( IGrouping<string, CommandsData> g in Items.GroupBy( i => i.Category ).OrderBy( g => g.Key ) )
            {
                CommandsCategory cat = new CommandsCategory { Name = g.Key };

                foreach ( CommandsData cd in g.OrderBy( c => c.Name ) )
                {
                    cat.Items.Add( cd );
                }

                Categories.Add( cat );
            }
        }

        public ICommand InsertCommand =>
            _insertCommand ?? ( _insertCommand = new RelayCommand( Insert, o => SelectedItem != null ) );

        public ObservableCollection<CommandsData> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ObservableCollection<CommandsCategory> Categories
        {
            get => _categories;
            set => SetProperty( ref _categories, value );
        }

        public CommandsData SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty( ref _selectedItem, value );
                ( _insertCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        private void Insert( object obj )
        {
            if ( !( obj is CommandsData cd ) || cd.Attribute == null || _macrosViewModel == null )
            {
                return;
            }

            string text = cd.Attribute.InsertText ?? string.Empty;
            var doc = _macrosViewModel.Document;

            if ( doc == null )
            {
                // No active editor (no macro selected) — append to the selected
                // macro's source text instead of NREing on a null TextDocument.
                if ( _macrosViewModel.SelectedItem != null )
                {
                    _macrosViewModel.SelectedItem.Macro = ( _macrosViewModel.SelectedItem.Macro ?? string.Empty ) + text;
                }

                return;
            }

            int offset = _macrosViewModel.CaretPosition;
            if ( offset < 0 || offset > doc.TextLength )
            {
                offset = doc.TextLength;
            }

            doc.Insert( offset, text );
        }
    }
}

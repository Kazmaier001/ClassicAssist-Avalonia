using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;
using ClassicAssist.Data.Skills;

namespace ClassicAssist.UI.Misc.Behaviours
{
    /// <summary>
    /// Avalonia replacement for the WPF SkillsGridViewSortBehaviour. Sorts a DataGrid bound to
    /// SkillEntry items by clicking column headers on <see cref="SkillsGridViewColumn"/> /
    /// <see cref="SkillsDataGridTemplateColumn"/> columns. Each column carries a
    /// <c>SortField</c> enum identifying which SkillEntry property to compare on; the comparison
    /// is delegated to <see cref="SkillComparer"/>.
    ///
    /// SortChangedCommand:  raised (with a <see cref="SkillsSortInfo"/>) whenever the user clicks
    ///                      a header so the VM can persist the new sort to disk.
    /// SetSortCommand:      OneWayToSource — the behaviour exposes a command back to the VM that
    ///                      applies a previously-persisted sort during profile load.
    /// </summary>
    public class SkillsGridViewSortBehaviour : Behavior<DataGrid>
    {
        public static readonly StyledProperty<ICommand> SortChangedCommandProperty =
            AvaloniaProperty.Register<SkillsGridViewSortBehaviour, ICommand>( nameof( SortChangedCommand ) );

        public static readonly StyledProperty<ICommand> SetSortCommandProperty =
            AvaloniaProperty.Register<SkillsGridViewSortBehaviour, ICommand>(
                nameof( SetSortCommand ), defaultBindingMode: BindingMode.OneWayToSource );

        private SkillsSortInfo _currentSort;

        public ICommand SortChangedCommand
        {
            get => GetValue( SortChangedCommandProperty );
            set => SetValue( SortChangedCommandProperty, value );
        }

        public ICommand SetSortCommand
        {
            get => GetValue( SetSortCommandProperty );
            set => SetValue( SetSortCommandProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Sorting += OnSorting;

            // Publish a command the VM can call to apply a persisted sort.
            SetSortCommand = new ApplySortCommand( this );
        }

        protected override void OnDetaching()
        {
            if ( AssociatedObject != null )
                AssociatedObject.Sorting -= OnSorting;
            base.OnDetaching();
        }

        private void OnSorting( object sender, DataGridColumnEventArgs e )
        {
            // Avalonia raises Sorting BEFORE applying its default sort. We want full control —
            // mark handled and do it ourselves so SkillComparer's tie-break logic wins.
            e.Handled = true;

            SkillsGridViewColumn.Enums? sortField = e.Column switch
            {
                SkillsGridViewColumn c => c.SortField,
                SkillsDataGridTemplateColumn t => t.SortField,
                _ => null
            };

            if ( sortField == null )
                return;

            ListSortDirection direction =
                _currentSort != null && _currentSort.SortBy == sortField.Value
                    ? ( _currentSort.Direction == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending )
                    : ListSortDirection.Ascending;

            var info = new SkillsSortInfo( sortField.Value, direction );
            ApplySort( info );

            SortChangedCommand?.Execute( info );
        }

        private void ApplySort( SkillsSortInfo info )
        {
            if ( info == null || AssociatedObject?.ItemsSource is not IEnumerable items )
                return;

            // Snapshot the source as SkillEntry[], sort with SkillComparer, push back as the
            // ItemsSource. We deliberately rebuild rather than mutate in-place because the
            // bound collection is typically an ObservableCollection<SkillEntry> on the VM and
            // we don't want to fire N change notifications.
            var comparer = new SkillComparerAdapter( new SkillComparer( info.Direction, info.SortBy ) );
            var sorted = items.Cast<object>().OrderBy( o => o, comparer ).ToList();
            AssociatedObject.ItemsSource = sorted;

            // Note: Avalonia DataGridColumn doesn't expose SortDirection publicly in 11.3,
            // so we can't paint the header arrow indicator. The sort itself is applied above.
            _currentSort = info;
        }

        // Lightweight ICommand exposed to the VM via OneWayToSource binding so it can push a
        // persisted SkillsSortInfo back onto the grid during profile load.
        private sealed class ApplySortCommand : ICommand
        {
            private readonly SkillsGridViewSortBehaviour _owner;
            public ApplySortCommand( SkillsGridViewSortBehaviour owner ) => _owner = owner;

            public bool CanExecute( object parameter ) => parameter is SkillsSortInfo;
            public void Execute( object parameter )
            {
                if ( parameter is SkillsSortInfo info )
                    _owner.ApplySort( info );
            }

#pragma warning disable CS0067
            public event System.EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        }

        // OrderBy<TKey> needs IComparable, but we have an IComparer. Wrap it so OrderBy can use it.
        private sealed class SkillComparerAdapter : IComparer<object>
        {
            private readonly SkillComparer _inner;
            public SkillComparerAdapter( SkillComparer inner ) => _inner = inner;
            public int Compare( object x, object y ) => _inner.Compare( x, y );
        }
    }

    public class SkillsSortInfo
    {
        public SkillsSortInfo( SkillsGridViewColumn.Enums sortBy, ListSortDirection direction )
        {
            SortBy = sortBy;
            Direction = direction;
        }

        public SkillsGridViewColumn.Enums SortBy { get; set; }
        public ListSortDirection Direction { get; set; }
    }
}

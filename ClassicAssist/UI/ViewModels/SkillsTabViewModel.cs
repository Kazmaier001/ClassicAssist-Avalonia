using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Threading;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.Data.Hotkeys.Commands;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.Data.Skills;
using ClassicAssist.Misc;
using ClassicAssist.Shared.Misc;
using ClassicAssist.Shared.Resources;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Misc;
using ClassicAssist.UI.Misc.Behaviours;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network;
using ClassicAssist.UO.Network.Packets;
using Newtonsoft.Json.Linq;

namespace ClassicAssist.UI.ViewModels
{
    public class SkillsTabViewModel : BaseViewModel, IGlobalSettingProvider
    {
        internal HotkeyCommand _hotkeyCategory;
        private ObservableCollectionEx<SkillEntry> _items = new ObservableCollectionEx<SkillEntry>();
        private ICommand _resetDeltasCommand;
        private SkillEntry _selectedItem;
        private ICommand _setAllSkillLocksCommand;
        private ICommand _setSkillLocksCommand;
        private ICommand _sortChangedCommand;

        private SkillsSortInfo _sortInfo;
        private float _totalBase;
        private ICommand _useSkillCommand;

        public SkillsTabViewModel()
        {
            Items.CollectionChanged += ( sender, args ) => { UpdateTotalBase(); };

            IncomingPacketHandlers.SkillUpdatedEvent += OnSkillUpdatedEvent;
            IncomingPacketHandlers.SkillsListEvent += OnSkillsListEvent;

            if ( Engine.Player != null && Engine.Connected )
            {
                Commands.MobileQuery( Engine.Player.Serial, MobileQueryType.SkillsRequest );
            }

            SkillManager manager = SkillManager.GetInstance();
            manager.Items = Items;
        }

        public ObservableCollectionEx<SkillEntry> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ICommand ResetDeltasCommand =>
            _resetDeltasCommand ?? ( _resetDeltasCommand = new RelayCommand( ResetDeltas, o => true ) );

        public SkillEntry SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty( ref _selectedItem, value );
                ( _useSkillCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SetAllSkillLocksCommand =>
            _setAllSkillLocksCommand ?? ( _setAllSkillLocksCommand = new RelayCommand( SetAllSkillLocks, o => true ) );

        public ICommand SetSkillLocksCommand =>
            _setSkillLocksCommand ?? ( _setSkillLocksCommand = new RelayCommand( SetSkillLocks, o => true ) );

        public float TotalBase
        {
            get => _totalBase;
            set => SetProperty( ref _totalBase, value );
        }

        public ICommand UseSkillCommand =>
            _useSkillCommand ?? ( _useSkillCommand =
                new RelayCommand( UseSkill, o => SelectedItem?.Skill.Invokable ?? false ) );

        public ICommand SetSortCommand { get; set; }

        public ICommand SortChangedCommand => _sortChangedCommand ?? ( _sortChangedCommand = new RelayCommand( OnSortChanged ) );

        public void Serialize( JObject json, bool global = false )
        {
            JArray skills = new JArray();

            if ( _hotkeyCategory?.Children == null )
            {
                return;
            }

            foreach ( HotkeyEntry hks in _hotkeyCategory.Children.Where(o => o.IsGlobal == global))
            {
                if ( Equals( hks.Hotkey, ShortcutKeys.Default ) )
                {
                    continue;
                }

                skills.Add( new JObject
                {
                    { "Name", hks.Name },
                    { "Keys", hks.Hotkey.ToJObject() },
                    { "PassToUO", hks.PassToUO },
                    { "Disableable", hks.Disableable }
                } );
            }

            json.Add( "Skills", skills );

            if ( _sortInfo == null || global )
            {
                return;
            }

            JObject obj = new JObject { { "SortField", _sortInfo.SortBy.ToString() }, { "SortDirection", _sortInfo.Direction.ToString() } };

            json.Add( "SkillsSort", obj );
        }

        public void Deserialize( JObject json, Options options, bool global = false )
        {
            HotkeyManager hotkey = HotkeyManager.GetInstance();

            if ( Skills.GetSkillsArray() == null )
            {
                return;
            }

            IOrderedEnumerable<SkillData> skills =
                Skills.GetSkillsArray().Where( s => s.Invokable ).OrderBy( s => s.Name );

            ObservableCollectionEx<HotkeyEntry> hotkeyEntries = new ObservableCollectionEx<HotkeyEntry>();

            foreach ( SkillData skill in skills )
            {
                if ( hotkeyEntries.Any( hke => hke.Name == skill.Name ) )
                {
                    continue;
                }

                hotkeyEntries.Add( new HotkeyCommand
                {
                    Action = ( hks, _ ) => SkillCommands.UseSkill( skill.Name ), Name = skill.Name
                } );
            }

            if ( json["Skills"] != null )
            {
                foreach ( HotkeyEntry hke in hotkeyEntries )
                {
                    JToken token = json["Skills"].FirstOrDefault( jo => jo["Name"].ToObject<string>() == hke.Name );

                    if ( token == null )
                    {
                        continue;
                    }

                    hke.Hotkey = new ShortcutKeys( token["Keys"] );
                    hke.PassToUO = token["PassToUO"]?.ToObject<bool>() ?? true;
                    hke.Disableable = token["Disableable"]?.ToObject<bool>() ?? true;
                    hke.IsGlobal = global;
                }
            }

            _hotkeyCategory = hotkey.Items.FirstOrDefault( hk => hk.IsCategory && hk.Name == Strings.Skills ) ?? new HotkeyCommand { Name = Strings.Skills, IsCategory = true };

            // maybe better new parameter use but implement IGlobalSettingProvider
            // global called after private profile
            // if call order change convert condition
            if ( !global )
            {
                _hotkeyCategory.Children = hotkeyEntries;
            }
            else
            {
                foreach ( HotkeyEntry hke in hotkeyEntries )
                {
                    HotkeyEntry hk = _hotkeyCategory.Children.FirstOrDefault( o => o.Name == hke.Name );

                    if ( hk != null && !hk.Hotkey.Equals( ShortcutKeys.Default ) && hke.Hotkey.Equals( ShortcutKeys.Default ) )
                    {
                        continue;
                    }

                    if ( hk == null )
                    {
                        _hotkeyCategory.Children.Add( hke );
                        continue;
                    }

                    if ( !hk.Hotkey.Equals( ShortcutKeys.Default ) && hke.Hotkey.Equals( ShortcutKeys.Default ) )
                    {
                        continue;
                    }

                    hk.Hotkey = hke.Hotkey;
                    hk.PassToUO = hke.PassToUO;
                    hk.Disableable = hke.Disableable;
                    hk.IsGlobal = hke.IsGlobal;
                }
            }

            hotkey.AddCategory( _hotkeyCategory );

            if ( !( json["SkillsSort"] is JObject obj ) || obj["SortField"] == null || obj["SortDirection"] == null || global )
            {
                return;
            }

            if ( !Enum.TryParse( obj["SortField"].ToObject<string>(), out SkillsGridViewColumn.Enums sortBy ) ||
                 !Enum.TryParse( obj["SortDirection"].ToObject<string>(), out ListSortDirection direction ) )
            {
                return;
            }

            _sortInfo = new SkillsSortInfo( sortBy, direction );
            SetSortCommand?.Execute( _sortInfo );
        }

        public string GetGlobalFilename()
        {
            return "Skills.json";
        }

        private void OnSortChanged( object obj )
        {
            if (!( obj is SkillsSortInfo info ) )
            {
                return;
            }

            _sortInfo = info;
        }

        private void ResetDeltas( object obj )
        {
            foreach ( SkillEntry skillEntry in Items )
            {
                skillEntry.Delta = 0;
            }
        }

        private void SetSkillLocks( object obj )
        {
            LockStatus lockStatus = (LockStatus) obj;

            if ( SelectedItem == null )
            {
                return;
            }

            Commands.ChangeSkillLock( SelectedItem, lockStatus );
        }

        private void SetAllSkillLocks( object obj )
        {
            LockStatus lockStatus = (LockStatus) (int) obj;

            IEnumerable<SkillEntry> skillsToSet = Items.Where( i => i.LockStatus != lockStatus );

            foreach ( SkillEntry skillEntry in skillsToSet )
            {
                Commands.ChangeSkillLock( skillEntry, lockStatus, false );
            }

            Commands.MobileQuery( Engine.Player.Serial, MobileQueryType.SkillsRequest );
        }

        private void UpdateTotalBase()
        {
            TotalBase = Items.Sum( se => se.Base );
        }

        private void OnSkillsListEvent( SkillInfo[] skills )
        {
            // Diff-update instead of Items.Clear()+rebuild so the DataGrid keeps
            // its scroll position and selection across server-pushed refreshes
            // (e.g. after the user right-clicks Set Up/Down/Locked, the server
            // replies with the full skill list). Clear+re-add destroys the row
            // containers and snaps the scrollbar back to the top.
            SkillComparer comparer = new SkillComparer( ListSortDirection.Ascending, SkillsGridViewColumn.Enums.Name );

            Dispatcher.UIThread.Invoke( () =>
            {
                System.Collections.Generic.HashSet<int> incomingIds = new System.Collections.Generic.HashSet<int>();

                foreach ( SkillInfo si in skills )
                {
                    string name = Skills.GetSkillName( si.ID );

                    if ( string.IsNullOrEmpty( name ) && si.BaseValue == 0 )
                    {
                        continue;
                    }

                    incomingIds.Add( si.ID );

                    SkillEntry existing = _items.FirstOrDefault( e => e.Skill.ID == si.ID );

                    if ( existing != null )
                    {
                        existing.Delta += si.BaseValue - existing.Base;
                        existing.Value = si.Value;
                        existing.Base = si.BaseValue;
                        existing.Cap = si.SkillCap;
                        existing.LockStatus = si.LockStatus;
                    }
                    else
                    {
                        Skill skill = new Skill
                        {
                            ID = si.ID, Name = name, Invokable = Skills.IsInvokable( si.ID )
                        };

                        SkillEntry se = new SkillEntry
                        {
                            Skill = skill,
                            Value = si.Value,
                            Base = si.BaseValue,
                            Cap = si.SkillCap,
                            LockStatus = si.LockStatus
                        };

                        Items.AddSorted( se, comparer );
                    }
                }

                // Drop any entries the server no longer reports.
                for ( int i = _items.Count - 1; i >= 0; i-- )
                {
                    if ( !incomingIds.Contains( _items[i].Skill.ID ) )
                    {
                        Items.RemoveAt( i );
                    }
                }
            } );
        }

        private void UseSkill( object obj )
        {
            if ( SelectedItem == null )
            {
                return;
            }

            if ( SelectedItem.Skill.Invokable )
            {
                Engine.SendPacketToServer( new UseSkill( SelectedItem.Skill.ID ) );
            }
        }

        private void OnSkillUpdatedEvent( int skillID, float value, float baseValue, LockStatus lockStatus,
            float skillCap )
        {
            SkillEntry entry = _items.FirstOrDefault( se => se.Skill.ID == skillID );

            if ( entry == null )
            {
                return;
            }

            Dispatcher.UIThread.Invoke( () =>
            {
                entry.Delta += baseValue - entry.Base;
                entry.Value = value;
                entry.Base = baseValue;
                entry.Cap = skillCap;
                entry.LockStatus = lockStatus;
            } );
        }
    }
}
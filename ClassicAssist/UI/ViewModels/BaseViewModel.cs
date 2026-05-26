using System.Collections.Generic;
using Avalonia.Threading;
using ClassicAssist.Data;
using ClassicAssist.Shared.UI;

namespace ClassicAssist.UI.ViewModels
{
    public class BaseViewModel : SetPropertyNotifyChanged
    {
        private static readonly List<BaseViewModel> _viewModels = new List<BaseViewModel>();
        private Options _currentOptions = Options.CurrentOptions;
        protected Dispatcher _dispatcher;

        public BaseViewModel()
        {
            _dispatcher = Dispatcher.UIThread;

            _viewModels.Add( this );

            AssistantOptions.ProfileChangingEvent += profile => { CurrentOptions = Options.CurrentOptions; };

            // If a profile has already been loaded by the time this VM is constructed
            // (common with lazily-realized TabControl tabs), hydrate from the cached JSON
            // so the user sees their saved state on first view of the tab.
            Options.HydrateIfLoaded( this );
        }

        public Options CurrentOptions
        {
            get => _currentOptions;
            set => SetProperty( ref _currentOptions, value );
        }

        public static BaseViewModel[] Instances => _viewModels.ToArray();

        ~BaseViewModel()
        {
            _viewModels.Remove( this );
        }
    }
}
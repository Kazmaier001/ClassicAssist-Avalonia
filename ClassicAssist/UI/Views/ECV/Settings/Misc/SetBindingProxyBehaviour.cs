#region License

// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using Avalonia;
using Avalonia.Controls;
using ClassicAssist.Misc;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Views.ECV.Settings.Misc
{
    public class SetBindingProxyBehaviour : Behavior<UserControl>
    {
        public static readonly StyledProperty<BindingProxy> ProxyProperty = AvaloniaProperty.Register<SetBindingProxyBehaviour, BindingProxy>( nameof( Proxy ) );

        public BindingProxy Proxy
        {
            get => (BindingProxy) GetValue( ProxyProperty );
            set => SetValue( ProxyProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if ( Proxy != null )
            {
                Proxy.Data = AssociatedObject;
            }
        }
    }
}
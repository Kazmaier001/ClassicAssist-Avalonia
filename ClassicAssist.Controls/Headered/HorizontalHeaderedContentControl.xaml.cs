// Copyright (C) 2023 Reetus
//
// Refactored 2026-05-20: was a UserControl whose axaml installed a DockPanel
// as Content; outer `<HorizontalHeaderedContentControl><ComboBox/></...>`
// overwrote Content = ComboBox so the header label disappeared.
// Now a templated HeaderedContentControl. The default ControlTemplate is
// set programmatically in the static ctor (a Style-based template in
// DarkTheme.axaml didn't take — runtime dump showed the fallback
// ContentControl template winning).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace ClassicAssist.Controls.Headered
{
    public class HorizontalHeaderedContentControl : HeaderedContentControl
    {
        public static readonly StyledProperty<Thickness> ChildMarginProperty =
            AvaloniaProperty.Register<HorizontalHeaderedContentControl, Thickness>(
                nameof( ChildMargin ), new Thickness( 0, 0, 5, 0 ) );

        public static readonly StyledProperty<double> HeaderWidthProperty =
            AvaloniaProperty.Register<HorizontalHeaderedContentControl, double>(
                nameof( HeaderWidth ), double.NaN );

        public static readonly StyledProperty<double> HeaderMinWidthProperty =
            AvaloniaProperty.Register<HorizontalHeaderedContentControl, double>(
                nameof( HeaderMinWidth ), double.NaN );

        static HorizontalHeaderedContentControl()
        {
            TemplateProperty.OverrideDefaultValue<HorizontalHeaderedContentControl>(
                new FuncControlTemplate<HorizontalHeaderedContentControl>( ( parent, _ ) =>
                {
                    var dock = new DockPanel { LastChildFill = true };

                    var headerPres = new ContentPresenter
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        [!ContentPresenter.ContentProperty] = parent[!HeaderProperty],
                        [!ContentPresenter.ContentTemplateProperty] = parent[!HeaderTemplateProperty],
                        [!Layoutable.MarginProperty] = parent[!ChildMarginProperty],
                        [!Layoutable.WidthProperty] = parent[!HeaderWidthProperty],
                        [!Layoutable.MinWidthProperty] = parent[!HeaderMinWidthProperty],
                        [!TextBlock.ForegroundProperty] = parent[!ForegroundProperty]
                    };
                    DockPanel.SetDock( headerPres, Dock.Left );
                    dock.Children.Add( headerPres );

                    var bodyPres = new ContentPresenter
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        [!ContentPresenter.ContentProperty] = parent[!ContentProperty],
                        [!ContentPresenter.ContentTemplateProperty] = parent[!ContentTemplateProperty]
                    };
                    dock.Children.Add( bodyPres );

                    return dock;
                } ) );
        }

        public Thickness ChildMargin
        {
            get => GetValue( ChildMarginProperty );
            set => SetValue( ChildMarginProperty, value );
        }

        public double HeaderMinWidth
        {
            get => GetValue( HeaderMinWidthProperty );
            set => SetValue( HeaderMinWidthProperty, value );
        }

        public double HeaderWidth
        {
            get => GetValue( HeaderWidthProperty );
            set => SetValue( HeaderWidthProperty, value );
        }
    }
}

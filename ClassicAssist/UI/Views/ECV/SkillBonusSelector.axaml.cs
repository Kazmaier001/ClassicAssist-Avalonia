// Copyright (C) 2023 Reetus
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using ClassicAssist.UI.ViewModels.Agents;

namespace ClassicAssist.UI.Views.ECV
{
    /// <summary>
    ///     Interaction logic for SkillBonusSelector.axaml
    /// </summary>
    public partial class SkillBonusSelector : UserControl
    {
        public static IReadOnlyList<string> SkillNames { get; } =
            ( from object value in typeof( SkillBonusSkills ).GetEnumValues()
                let fieldInfo = typeof( SkillBonusSkills ).GetField( value.ToString() )
                let attr = (DescriptionAttribute) fieldInfo.GetCustomAttributes( typeof( DescriptionAttribute ), false ).FirstOrDefault()
                select attr == null ? value.ToString() : attr.Description ).ToList();

        public SkillBonusSelector()
        {
            InitializeComponent();
        }
    }
}

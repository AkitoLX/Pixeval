#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2022 Pixeval/SettingsMarker.cs
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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pixeval.Attributes;

namespace Pixeval;

public enum SettingEntryCategory
{
    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.VersionSettingsGroupText))]
    Version,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.SessionSettingsGroupText))]
    Session,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.ApplicationSettingsGroupText))]
    Application,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.BrowsingExperienceSettingsGroupText))]
    BrowsingExperience,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.SearchSettingsGroupText))]
    Search,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.DownloadSettingsGroupText))]
    Download,

    [LocalizedResource(typeof(SettingsPageResources), nameof(SettingsPageResources.MiscSettingsGroupText))]
    Misc
}

public partial record SettingEntry(SettingEntryCategory Category, Type ResourceLoader, string ResourceKey)
{
    public static readonly SettingEntry AutoUpdate = new(SettingEntryCategory.Version, typeof(SettingsPageResources), nameof(SettingsPageResources.DownloadUpdateAutomaticallyEntryHeader));

    public static readonly SettingEntry SignOut = new(SettingEntryCategory.Session, typeof(SettingsPageResources), nameof(SettingsPageResources.SignOutEntryHeader));

    public static readonly SettingEntry ResetSettings = new(SettingEntryCategory.Session, typeof(SettingsPageResources), nameof(SettingsPageResources.ResetDefaultSettingsEntryHeader));

    public static readonly Lazy<IEnumerable<SettingEntry>> LazyValues = new(() =>
        typeof(SettingEntry).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(f => f.GetValue(null)).OfType<SettingEntry>());

    public string? GetLocalizedResourceContent()
    {
        return ResourceLoader.GetField(ResourceKey)?.GetValue(null) as string;
    }
}

#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/IIllustrationViewViewModel.cs
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI;
using Pixeval.CoreApi.Model;
using Pixeval.UserControls.IllustratorView;

namespace Pixeval.UserControls.Illustrate;

public abstract partial class SortableIllustrateViewViewModel<T, TViewModel> : IllustrateViewViewModel<T, TViewModel> where T : IIllustrate where TViewModel : IllustrateViewModel<T>
{
    [ObservableProperty]
    private string? _selectionLabel;

    [ObservableProperty]
    private bool _isAnyIllustrationSelected;

    public abstract void SetSortDescription(SortDescription description);

    public abstract void ClearSortDescription();
}

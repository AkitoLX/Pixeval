#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/JustifiedLayoutIllustrationViewDataProvider.cs
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Pixeval.CoreApi.Engine;
using Pixeval.CoreApi.Model;
using Pixeval.Misc;
using Pixeval.UserControls.Illustrate;
using Pixeval.Util.Ref;
using Pixeval.Utilities;

namespace Pixeval.UserControls.IllustrationView;

/// <summary>
/// 复用时调用<see cref="CloneRef"/>，<see cref="FetchEngineRef"/>和<see cref="IllustrationSourceRef"/>会在所有复用对象都Dispose时Dispose
/// </summary>
public class IllustrationViewDataProvider : ObservableObject, IDataProvider<Illustration, IllustrationViewModel>, IDisposable
{
    /// <summary>
    /// Call <see cref="ResetAndFillAsync"/> to initialize
    /// </summary>
    public IllustrationViewDataProvider()
    {
    }

    private SharedRef<IFetchEngine<Illustration?>?>? _fetchEngineRef;

    public SharedRef<IFetchEngine<Illustration?>?>? FetchEngineRef
    {
        get => _fetchEngineRef;
        private set
        {
            if (Equals(value, _fetchEngineRef))
                return;
            FetchEngine?.EngineHandle.Cancel();

            _fetchEngineRef = value;
        }
    }

    public IFetchEngine<Illustration?>? FetchEngine => _fetchEngineRef?.Value;

    public AdvancedCollectionView View { get; } = new(Array.Empty<IllustrationViewModel>());

    private SharedRef<IncrementalLoadingCollection<FetchEngineIncrementalSource<Illustration, IllustrationViewModel>, IllustrationViewModel>> _illustrationSourceRef = null!;

    protected SharedRef<IncrementalLoadingCollection<FetchEngineIncrementalSource<Illustration, IllustrationViewModel>, IllustrationViewModel>> IllustrationSourceRef
    {
        get => _illustrationSourceRef;
        set
        {
            if (Equals(_illustrationSourceRef, value))
                return;

            OnPropertyChanging();
            if (_illustrationSourceRef is { } old)
            {
                old.Value.CollectionChanged -= OnIllustrationsSourceOnCollectionChanged;
                _ = old.TryDispose(this);
            }
            _illustrationSourceRef = value;
            value.Value.CollectionChanged += OnIllustrationsSourceOnCollectionChanged;
            View.Source = value.Value;
            OnPropertyChanged();
        }
    }

    public IncrementalLoadingCollection<FetchEngineIncrementalSource<Illustration, IllustrationViewModel>, IllustrationViewModel> Source => _illustrationSourceRef.Value;

    public IllustrationViewDataProvider CloneRef()
    {
        var dataProvider = new IllustrationViewDataProvider();
        dataProvider.FetchEngineRef = FetchEngineRef?.MakeShared(dataProvider);
        dataProvider.IllustrationSourceRef = IllustrationSourceRef.MakeShared(dataProvider);
        dataProvider.View.Filter = View.Filter;
        foreach (var viewSortDescription in View.SortDescriptions)
            dataProvider.View.SortDescriptions.Add(viewSortDescription);
        dataProvider.View.CurrentItem = View.CurrentItem;
        return dataProvider;
    }

    private Predicate<object>? _filter;

    public Predicate<object>? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            FilterChanged?.Invoke(_filter, EventArgs.Empty);
        }
    }

    public event EventHandler? FilterChanged;

    public ObservableCollection<IllustrationViewModel> SelectedIllustrations { get; set; } = new();

    public void DisposeCurrent()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (IllustrationSourceRef is not null)
        {
            Source.CollectionChanged -= OnIllustrationsSourceOnCollectionChanged;
            if (IllustrationSourceRef.TryDispose(this))
                foreach (var illustrationViewModel in Source)
                    illustrationViewModel.Dispose();
        }

        SelectedIllustrations.Clear();
    }

    public async Task<int> ResetAndFillAsync(IFetchEngine<Illustration?>? fetchEngine, int limit = -1)
    {
        FetchEngineRef = new(fetchEngine, this);
        DisposeCurrent();

        IllustrationSourceRef = new(new(new IllustrationFetchEngineIncrementalSource(FetchEngine!, limit)), this);
        // TODO: 根据屏幕大小决定加载多少
        var result = (await Source.LoadMoreItemsAsync(20)).Count;
        result += (await Source.LoadMoreItemsAsync(20)).Count;
        result += (await Source.LoadMoreItemsAsync(10)).Count;
        return (int)result;
    }

    protected virtual void OnIllustrationsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void OnIsSelectedChanged(object? s, IllustrationViewModel model)
        {
            // Do not add to collection is the model does not conform to the filter
            if (!Filter?.Invoke(model) ?? false)
                return;
            if (model.IsSelected)
                SelectedIllustrations.Add(model);
            else
                _ = SelectedIllustrations.Remove(model);
        }

        switch (e)
        {
            case { Action: NotifyCollectionChangedAction.Add }:
                e.NewItems?.OfType<IllustrationViewModel>().ForEach(i => i.IsSelectedChanged += OnIsSelectedChanged);
                break;
            case { Action: NotifyCollectionChangedAction.Remove }:
                e.NewItems?.OfType<IllustrationViewModel>().ForEach(i => i.IsSelectedChanged -= OnIsSelectedChanged);
                break;
        }
    }

    public void Dispose()
    {
        DisposeCurrent();
        FetchEngineRef = null;
    }

    ~IllustrationViewDataProvider() => Dispose();
}

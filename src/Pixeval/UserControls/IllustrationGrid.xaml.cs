﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Pixeval.Events;
using Pixeval.Pages.IllustrationViewer;
using Pixeval.Util.UI;
using Pixeval.ViewModel;

namespace Pixeval.UserControls
{
    // use "load failed" image for those thumbnails who failed to load its source due to various reasons
    // note: please ALWAYS add e.Handled = true before every "tapped" event for the buttons
    public sealed partial class IllustrationGrid
    {
        public IllustrationGridViewModel ViewModel { get; set; }

        public IllustrationGrid()
        {
            InitializeComponent();
            ViewModel = new IllustrationGridViewModel();
        }

        private async void RemoveBookmarkButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var viewModel = sender.GetDataContext<IllustrationViewModel>();
            await viewModel!.RemoveBookmarkAsync();
        }

        // Make sure that a tap on the selection button will not be handled by IllustrationThumbnailContainerItem_OnTapped
        private void SelectionButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void PostBookmarkButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var viewModel = sender.GetDataContext<IllustrationViewModel>();
            await viewModel!.PostPublicBookmarkAsync();
        }

        private void IllustrationThumbnailContainerItem_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            EventChannel.Default.Publish(new MainPageFrameSetConnectedAnimationTargetEvent(sender as UIElement));

            var viewModel = sender.GetDataContext<IllustrationViewModel>()
                .GetMangaIllustrationViewModels()
                .ToArray();

            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", (UIElement) sender);
            App.RootFrameNavigate(typeof(IllustrationViewerPage), new IllustrationViewerPageViewModel(this, viewModel), new SuppressNavigationTransitionInfo());
        }

        private static readonly ExponentialEase ImageSourceSetEasingFunction = new()
        {
            EasingMode = EasingMode.EaseOut,
            Exponent = 12
        };

        private void IllustrationThumbnailContainerItem_OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            var context = sender.GetDataContext<IllustrationViewModel>();
            if (args.BringIntoViewDistanceY <= sender.ActualHeight) // one element ahead
            {
                _ = context.LoadThumbnailIfRequired().ContinueWith(task =>
                {
                    if (!task.Result) return;
                    var transform = (ScaleTransform) sender.RenderTransform;
                    var scaleXAnimation = transform.CreateDoubleAnimation(nameof(transform.ScaleX), from: 1.1, to: 1, easingFunction: ImageSourceSetEasingFunction, duration: TimeSpan.FromSeconds(2));
                    var scaleYAnimation = transform.CreateDoubleAnimation(nameof(transform.ScaleY), from: 1.1, to: 1, easingFunction: ImageSourceSetEasingFunction, duration: TimeSpan.FromSeconds(2));
                    var opacityAnimation = sender.CreateDoubleAnimation(nameof(sender.Opacity), from: 0, to: 1, easingFunction: ImageSourceSetEasingFunction, duration: TimeSpan.FromSeconds(2));
                    UIHelper.CreateStoryboard(scaleXAnimation, scaleYAnimation, opacityAnimation).Begin();
                }, TaskScheduler.FromCurrentSynchronizationContext());
                return;
            }

            // small tricks to reduce memory consumption
            switch (context)
            {
                case { LoadingThumbnail: true }:
                    context.LoadingThumbnailCancellationHandle.Cancel();
                    break;
                case { ThumbnailSource: not null }:
                    var source = context.ThumbnailSource;
                    context.ThumbnailSource = null;
                    source.Dispose();
                    break;
            }
        }

        public UIElement? GetItemContainer(IllustrationViewModel viewModel)
        {
            return IllustrationGridView.ContainerFromItem(viewModel) as UIElement;
        }
    }
}

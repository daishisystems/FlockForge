using FlockForge.ViewModels.Base;

namespace FlockForge.Views.Base;

public abstract class BaseContentPage<TViewModel> : ContentPage 
    where TViewModel : BaseViewModel
{
    protected BaseContentPage(TViewModel viewModel)
    {
        BindingContext = viewModel;
        SetupPage();
    }

    protected TViewModel ViewModel => (TViewModel)BindingContext;

    protected virtual void SetupPage()
    {
        // Configure rugged field UI defaults
        Shell.SetNavBarIsVisible(this, true);
        Shell.SetTabBarIsVisible(this, true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.RefreshCommand.CanExecute(null))
            ViewModel.RefreshCommand.Execute(null);
    }
}
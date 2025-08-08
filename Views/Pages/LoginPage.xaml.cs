using System;
using System.Reactive.Disposables;
using FlockForge.ViewModels.Pages;

namespace FlockForge.Views.Pages;

public partial class LoginPage : FlockForge.Views.Base.BaseContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as FlockForge.ViewModels.Base.BaseViewModel)?.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        (BindingContext as FlockForge.ViewModels.Base.BaseViewModel)?.OnDisappearing();
        base.OnDisappearing();
    }
}
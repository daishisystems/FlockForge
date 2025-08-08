using System;
using Microsoft.Maui.Controls;
using FlockForge.ViewModels.Pages;

namespace FlockForge.Views.Pages;

public partial class LoginPage : FlockForge.Views.Base.DisposableContentPage
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
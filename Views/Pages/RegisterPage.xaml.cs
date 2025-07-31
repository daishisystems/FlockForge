using FlockForge.ViewModels.Pages;

namespace FlockForge.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
using Microsoft.Maui.Controls;

namespace FlockForge.Controls
{
    public class SecurePasswordEntry : Entry
    {
        public SecurePasswordEntry()
        {
            IsPassword = true;
            
            // Add extra margin to prevent AutoFill overlay issues
            Margin = new Thickness(0, 10, 0, 10);
            
            // Set additional properties to help with layout
            HeightRequest = 44;
            
#if IOS
            // For iOS, we'll handle AutoFill issues through layout spacing
            // The Frame wrapper in XAML will provide the necessary spacing
#endif
        }
    }
}
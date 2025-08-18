$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path Resources/Fonts | Out-Null
New-Item -ItemType Directory -Force -Path Resources/Images | Out-Null
Invoke-WebRequest https://rsms.me/inter/font-files/Inter-Regular.ttf  -OutFile Resources/Fonts/Inter-Regular.ttf
Invoke-WebRequest https://rsms.me/inter/font-files/Inter-SemiBold.ttf -OutFile Resources/Fonts/Inter-SemiBold.ttf
Invoke-WebRequest https://rsms.me/inter/font-files/Inter-Bold.ttf     -OutFile Resources/Fonts/Inter-Bold.ttf
Write-Host "Assets fetched. You can now enable Inter in GloveFirst.xaml if desired."

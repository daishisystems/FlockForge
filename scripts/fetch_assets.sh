#!/usr/bin/env bash
set -euo pipefail
mkdir -p Resources/Fonts Resources/Images
curl -L -o Resources/Fonts/Inter-Regular.ttf  https://rsms.me/inter/font-files/Inter-Regular.ttf
curl -L -o Resources/Fonts/Inter-SemiBold.ttf https://rsms.me/inter/font-files/Inter-SemiBold.ttf
curl -L -o Resources/Fonts/Inter-Bold.ttf     https://rsms.me/inter/font-files/Inter-Bold.ttf
echo "Assets fetched. You can now enable Inter in GloveFirst.xaml if desired."

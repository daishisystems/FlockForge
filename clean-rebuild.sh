#!/bin/bash
echo "Cleaning solution..."
rm -rf bin obj
rm -rf Platforms/Android/bin Platforms/Android/obj
dotnet clean
echo "Rebuilding..."
dotnet restore
dotnet build -f net9.0-android -c Debug
echo "Done! Now deploy to device."
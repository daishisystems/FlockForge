#!/bin/bash

echo "🚀 Runtime Verification"

# Android Runtime Test
echo "Testing Android..."
dotnet build -t:Run -f net9.0-android -c Release 2>&1 | tee android.log

echo "Checking Android results..."
if grep -q "Skipped.*frames" android.log; then
    echo "❌ FAIL: Frame drops detected"
else
    echo "✅ PASS: No frame drops"
fi

# iOS Runtime Test
echo "Testing iOS..."
dotnet build -t:Run -f net9.0-ios -c Release 2>&1 | tee ios.log

echo "Checking iOS results..."
if grep -q "observer.*not disposed" ios.log; then
    echo "❌ FAIL: Observer disposal issues"
else
    echo "✅ PASS: No observer issues"
fi

if grep -q "Crashlytics initialized successfully" ios.log; then
    echo "✅ PASS: Crashlytics working"
else
    echo "❌ FAIL: Crashlytics not initialized"
fi
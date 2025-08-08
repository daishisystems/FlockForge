#!/bin/bash

echo "🔍 Comprehensive FlockForge Validation"
echo "======================================"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

ERRORS=0

# Function to check condition
check() {
    if eval "$2"; then
        echo -e "${GREEN}✓${NC} $1"
    else
        echo -e "${RED}✗${NC} $1"
        ((ERRORS++))
    fi
}

# Critical Fixes
echo -e "\n🔴 CRITICAL FIXES:"
check "UI Thread Helper exists" "[ -f 'Helpers/UIThreadHelper.cs' ]"
check "No .Result usage" "! grep -r '\.Result[^a-zA-Z]' --include='*.cs' . 2>/dev/null"
check "No Thread.Sleep" "! grep -r 'Thread\.Sleep' --include='*.cs' . 2>/dev/null"
check "iOS Observer Manager exists" "[ -f 'Platforms/iOS/Helpers/ObserverManager.cs' ]"

# High Priority Fixes
echo -e "\n🟡 HIGH PRIORITY FIXES:"
check "iOS Crashlytics key updated" "grep -q 'FirebaseCrashlyticsCollectionEnabled' Platforms/iOS/Info.plist"
check "XAML compilation enabled" "grep -q 'XamlCompilation' GlobalUsings.cs 2>/dev/null || grep -q 'XamlCompilation' MauiProgram.cs"
check "Release optimizations configured" "grep -q 'PublishTrimmed' FlockForge.csproj"
check "Material fixes applied" "[ -f 'Platforms/Android/Resources/values/material_fixes.xml' ]"

# Build Verification
echo -e "\n🏗️ BUILD VERIFICATION:"
echo "Building iOS Release..."
if dotnet build -f net9.0-ios -c Release > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} iOS Release build successful"
else
    echo -e "${RED}✗${NC} iOS Release build failed"
    ((ERRORS++))
fi

echo "Building Android Release..."
if dotnet build -f net9.0-android -c Release > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} Android Release build successful"
else
    echo -e "${RED}✗${NC} Android Release build failed"
    ((ERRORS++))
fi

# Final Report
echo -e "\n======================================"
if [ $ERRORS -eq 0 ]; then
    echo -e "${GREEN}✅ ALL CHECKS PASSED!${NC}"
    echo "Your app is production-ready!"
else
    echo -e "${RED}❌ $ERRORS CHECKS FAILED${NC}"
    echo "Please review and fix the issues above."
fi

exit $ERRORS
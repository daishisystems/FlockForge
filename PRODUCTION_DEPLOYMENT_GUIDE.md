# 🏭 FlockForge Production iOS Deployment Guide

## 📋 Overview

This guide covers the complete setup for deploying FlockForge to the App Store using your Apple Developer Programme membership.

## 🍎 Apple Developer Console Setup

### Step 1: App ID Registration

1. **Login to Apple Developer Console**
   - Go to [developer.apple.com](https://developer.apple.com)
   - Sign in with your Apple Developer account

2. **Create App ID**
   - Navigate to **Certificates, Identifiers & Profiles**
   - Click **Identifiers** → **App IDs** → **+**
   - Select **App** → **Continue**
   - **Description**: `FlockForge - Farm Management`
   - **Bundle ID**: `io.nexair.flockforge` (Explicit)
   - **Capabilities**: Enable the following:
     - ✅ App Groups
     - ✅ Associated Domains
     - ✅ Background Modes
     - ✅ Data Protection
     - ✅ Keychain Sharing
     - ✅ Push Notifications

### Step 2: Distribution Certificate

1. **Create Distribution Certificate**
   - Go to **Certificates** → **Production** → **+**
   - Select **App Store and Ad Hoc** → **Continue**
   - **Generate CSR** (Certificate Signing Request):
     ```bash
     # On Mac, open Keychain Access
     # Keychain Access → Certificate Assistant → Request Certificate from CA
     # Email: your-email@domain.com
     # Common Name: FlockForge Distribution
     # Save to disk
     ```
   - Upload the CSR file
   - Download and install the certificate

### Step 3: Provisioning Profile

1. **Create App Store Provisioning Profile**
   - Go to **Profiles** → **Distribution** → **+**
   - Select **App Store** → **Continue**
   - Select your **App ID** (`io.nexair.flockforge`)
   - Select your **Distribution Certificate**
   - **Profile Name**: `FlockForge App Store`
   - Download the profile

### Step 4: Xcode Configuration

1. **Install Certificates and Profiles**
   - Double-click downloaded certificate to install
   - Double-click provisioning profile to install
   - Open Xcode → Preferences → Accounts
   - Verify your Apple ID and certificates are present

2. **Project Signing Configuration**
   - Open FlockForge project in Xcode
   - Select project → Target → Signing & Capabilities
   - **Team**: Select your Apple Developer team
   - **Provisioning Profile**: Select "FlockForge App Store"
   - Verify **Bundle Identifier**: `io.nexair.flockforge`

## 🔥 Firebase Production Setup

### Step 1: Create Production Firebase Project

1. **Firebase Console Setup**
   - Go to [console.firebase.google.com](https://console.firebase.google.com)
   - Click **Create a project**
   - **Project name**: `flockforge-production`
   - **Enable Google Analytics**: Yes
   - **Analytics account**: Create new or use existing

2. **Add iOS App**
   - Click **Add app** → **iOS**
   - **Bundle ID**: `io.nexair.flockforge`
   - **App nickname**: `FlockForge iOS Production`
   - **App Store ID**: (Leave empty for now)
   - Download `GoogleService-Info.plist`

3. **Replace Configuration File**
   ```bash
   # Replace the template file with your actual production config
   cp ~/Downloads/GoogleService-Info.plist Platforms/iOS/GoogleService-Info-Production.plist
   ```

### Step 2: Firebase Services Configuration

1. **Authentication Setup**
   - Go to **Authentication** → **Sign-in method**
   - Enable **Email/Password**
   - Enable **Google** (optional)
   - Configure **Authorized domains** (add your domain)

2. **Firestore Database**
   - Go to **Firestore Database** → **Create database**
   - **Security rules**: Start in production mode
   - **Location**: Choose closest to your users

3. **Security Rules** (Update after testing)
   ```javascript
   rules_version = '2';
   service cloud.firestore {
     match /databases/{database}/documents {
       // Users can only access their own data
       match /users/{userId} {
         allow read, write: if request.auth != null && request.auth.uid == userId;
       }
       
       // Farm data access control
       match /farms/{farmId} {
         allow read, write: if request.auth != null && 
           resource.data.ownerId == request.auth.uid;
       }
     }
   }
   ```

## 🏗️ Production Build Process

### Step 1: Build Configuration

1. **Release Build Command**
   ```bash
   # Clean previous builds
   dotnet clean
   
   # Build for iOS Release (App Store)
   dotnet build -c Release -f net9.0-ios
   
   # Create archive for App Store
   dotnet publish -c Release -f net9.0-ios --self-contained
   ```

2. **Alternative: Xcode Archive**
   ```bash
   # Generate Xcode project
   dotnet build -t:Run -f net9.0-ios
   
   # Open in Xcode and use Product → Archive
   open bin/Debug/net9.0-ios/FlockForge.xcodeproj
   ```

### Step 2: App Store Connect

1. **Create App Record**
   - Go to [appstoreconnect.apple.com](https://appstoreconnect.apple.com)
   - **My Apps** → **+** → **New App**
   - **Platform**: iOS
   - **Name**: FlockForge
   - **Primary Language**: English
   - **Bundle ID**: `io.nexair.flockforge`
   - **SKU**: `flockforge-ios-2025`

2. **App Information**
   - **Category**: Productivity
   - **Subcategory**: Business
   - **Content Rights**: You own or have licensed all rights
   - **Age Rating**: Complete questionnaire (likely 4+)

3. **Pricing and Availability**
   - **Price**: Free or set price
   - **Availability**: All countries or select specific

### Step 3: Upload Build

1. **Using Xcode**
   - Archive → Distribute App → App Store Connect
   - Upload → Automatic signing
   - Wait for processing (10-30 minutes)

2. **Using Command Line** (Alternative)
   ```bash
   # Export archive
   xcodebuild -exportArchive \
     -archivePath FlockForge.xcarchive \
     -exportPath ./export \
     -exportOptionsPlist ExportOptions.plist
   
   # Upload to App Store Connect
   xcrun altool --upload-app \
     -f FlockForge.ipa \
     -u your-apple-id@email.com \
     -p your-app-specific-password
   ```

## 🧪 Testing Pipeline

### Step 1: TestFlight Beta Testing

1. **Internal Testing**
   - Add internal testers (up to 100)
   - No review required
   - Immediate distribution

2. **External Testing**
   - Add external testers (up to 10,000)
   - Requires App Store review
   - Public link available

### Step 2: Production Validation

1. **Pre-submission Checklist**
   - ✅ App builds and runs without crashes
   - ✅ Firebase authentication works
   - ✅ All features functional
   - ✅ Privacy policy implemented
   - ✅ App Store guidelines compliance
   - ✅ Screenshots and metadata ready

2. **App Store Review**
   - Submit for review
   - Typical review time: 24-48 hours
   - Address any rejection feedback
   - Release when approved

## 🔧 Build Commands Reference

### Development Build (Simulator)
```bash
dotnet build -c Debug -f net9.0-ios
dotnet run -c Debug -f net9.0-ios
```

### Production Build (Device/App Store)
```bash
dotnet build -c Release -f net9.0-ios
```

### TestFlight Build (AdHoc)
```bash
dotnet build -c AdHoc -f net9.0-ios
```

## 🚨 Troubleshooting

### Common Issues

1. **Code Signing Errors**
   - Verify certificates are installed
   - Check provisioning profile validity
   - Ensure Bundle ID matches exactly

2. **Firebase Configuration**
   - Verify GoogleService-Info.plist is correct
   - Check Firebase project settings
   - Validate API keys and permissions

3. **App Store Rejection**
   - Review App Store Guidelines
   - Check privacy policy requirements
   - Validate all app functionality

### Support Resources

- **Apple Developer Documentation**: [developer.apple.com/documentation](https://developer.apple.com/documentation)
- **Firebase Documentation**: [firebase.google.com/docs](https://firebase.google.com/docs)
- **App Store Review Guidelines**: [developer.apple.com/app-store/review/guidelines](https://developer.apple.com/app-store/review/guidelines)

## 📱 Next Steps

1. **Complete Apple Developer setup** following Step 1-4
2. **Set up production Firebase** following Firebase steps
3. **Test production build** locally
4. **Submit to TestFlight** for beta testing
5. **Submit to App Store** for review and release

Your FlockForge app is now configured for professional iOS deployment! 🚀
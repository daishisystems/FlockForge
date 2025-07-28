# Prompt Template: .NET MAUI Livestock Management Application

I need to develop a cross-platform mobile application using .NET MAUI and Firebase for livestock management with the following comprehensive specifications:

## **Authentication Requirements**
- **Firebase Authentication** integration with:
  - Email/password authentication
  - Firebase Single Sign-On (SSO) with Google, Apple, and Facebook providers
  - Secure token management and automatic session refresh
  - Biometric authentication support (fingerprint/face recognition) where available
  - Password reset functionality via email

## **Pages Required**

### **1. Farmer Profile Page**
**Purpose**: Personal farmer information management
**Fields**:
- First Name (required, text input)
- Surname (required, text input)
- Email (required, email validation)
- Mobile Number (required, phone number format with country code picker)
- Address (text area)
- City (text input with autocomplete)
- Province (dropdown/picker with predefined options)
- Zip Code (text input with format validation)
- Sub Region (text input)

**Features**:
- Profile photo upload with camera/gallery integration
- Form validation with real-time error display
- Auto-save functionality for partially completed forms

### **2. Farm Profile Page**
**Purpose**: Multi-farm management for individual farmers
**Fields per Farm**:
- Farm Name (required, text input)
- Company Name (text input)
- Breed (dropdown with common sheep breeds)
- Total Number Of Production Ewes (numeric input)
- Size (numeric input with unit picker - hectares/acres)
- Address (text area)
- City (text input with autocomplete)
- Province (dropdown/picker)
- GPS Location (map integration with current location detection)
- Type Of Production System (dropdown: Intensive/Extensive/Semi-intensive)
- Preferred Agent (text input with search/autocomplete)
- Preferred Abattoir (text input with search/autocomplete)
- Preferred Veterinary (text input with contact integration)
- Co Op (text input)

**Features**:
- Multiple farm support with farm switching capability
- GPS coordinate capture with map visualization
- Photo gallery for farm documentation
- Contact integration for agents/veterinarians

### **3. Groups/Lambing Season Page**
**Purpose**: Seasonal livestock group management per farm
**Fields**:
- Code (auto-generated with manual override option)
- Group Name/Lambing Season (text input)
- Mating Start (date picker)
- Mating End (date picker with validation > start date)
- Lambing Start (date picker, calculated from mating dates)
- Lambing End (date picker)
- Active (toggle switch)

**Features**:
- Calendar integration for date selection
- Automatic lambing date calculation (145-day gestation)
- Season templates for quick setup
- Color-coded status indicators

### **4. Breeding Page**
**Purpose**: Breeding event tracking per farm and group
**Fields**:
- Group Name/Lambing Season (dropdown from existing groups)
- Type of Mating (radio buttons: AI/Natural/Both)
- Number Of Ewes Mated (numeric input)
- AI Date (date picker, conditional on mating type)
- Natural Mating Start (date picker)
- Natural Mating End (date picker)
- Days (auto-calculated field)
- Did You Use Follow Up Rams? (yes/no toggle)
- Follow Up Rams In (date picker, conditional)
- Follow Up Rams Out (date picker, conditional)
- Days In (auto-calculated field)
- Date For Year Calculation (date picker)
- Year (auto-populated from date)

**Features**:
- Conditional field display based on mating type
- Automatic calculations for duration fields
- Timeline visualization of breeding events
- Ram usage tracking and analytics

### **5. Scanning Page**
**Purpose**: Pregnancy scanning results per farm and group
**Fields**:
- Group Name (dropdown from existing groups)
- Ewes Mated (auto-populated from breeding data)
- Ewes Scanned (numeric input ≤ Ewes Mated)
- Ewes Pregnant (numeric input ≤ Ewes Scanned)
- Ewes Not Pregnant (auto-calculated)
- Conception Ratio (auto-calculated percentage)
- Scanned Fetuses (numeric input)
- Ewes With Singles (numeric input)
- Ewes With Twins (numeric input)
- Ewes With Triplets (numeric input)
- Expected Lambing % of Ewes Pregnant (auto-calculated)
- Expected Lambing % Of Ewes Mated (auto-calculated)

**Features**:
- Real-time calculation of percentages and ratios
- Data validation to ensure logical consistency
- Visual charts for scanning results
- Comparison with previous seasons

### **6. Lambing Page**
**Purpose**: Lambing event recording per farm and group
**Fields**:
- Group Name (dropdown from existing groups)
- Ewes Lambed (numeric input)
- Lambs Born (numeric input)
- Lambs Dead (numeric input)
- Lambs After Mortality (auto-calculated)
- Average Birth Weight (decimal input with unit kg/lbs)
- Lambing % Of Ewes Mated (auto-calculated)
- Lambing % Of Ewes Lambed (auto-calculated)
- Lambing Mortality % (auto-calculated)
- % of Ewes Lambed From Mating (auto-calculated)
- Dry Ewes % of Ewes Mated (auto-calculated)
- Dry Ewes % of Ewes Lambed (auto-calculated)
- Lambing Mortality % of Ewes Lambed (auto-calculated)

**Features**:
- Daily lambing record entry capability
- Mortality tracking with cause categorization
- Performance benchmarking against industry standards
- Alert system for concerning mortality rates

### **7. Weaning Page**
**Purpose**: Weaning event tracking per farm and group
**Fields**:
- Group Name (dropdown from existing groups)
- Lambs Weaned (numeric input)
- Number Ram Lambs (numeric input)
- Number Ewe Lambs (numeric input)
- Lambs Born (auto-populated from lambing data)
- Rams Weaned (numeric input)
- % Ewes Weaned (auto-calculated)
- Average Wean Weight (decimal input with unit)
- Breeding Ewes Dead From Scan (numeric input)
- Lambs Weaned % of Ewes Mated (auto-calculated)
- Lambs Weaned % of Ewes Lambed (auto-calculated)
- Weaning Mortality (numeric input)
- Other Mortalities (numeric input with categorization)

**Features**:
- Weight tracking with growth rate calculations
- Gender-based performance analytics
- Mortality cause tracking and reporting
- Weaning age calculation and optimization recommendations

## **User Roles and Permissions**
- **Single-Tenant Architecture**: Each farmer has exclusive access to their own data only
- **Data Isolation**: Complete separation of farmer data at the database level
- **Role-Based Security**: Farmer role with full CRUD permissions on owned data
- **Session Management**: Secure token-based authentication with automatic logout
- **Data Privacy**: No cross-farmer data visibility or sharing capabilities

## **Shared Components**

### **Navigation System**
- **Bottom Tab Navigation** for primary pages (optimized for thumb navigation)
- **Hamburger Menu** for secondary functions and settings
- **Floating Action Button** for quick data entry
- **Breadcrumb Navigation** for hierarchical data (Farm → Group → Records)
- **Large Touch Targets** (minimum 44px) for gloved hand operation

### **Header/Top Bar**
- **Current Farm Indicator** with quick farm switching
- **Connection Status** (online/offline indicator)
- **User Avatar** and name display
- **Sync Status** indicator for data synchronization
- **Settings Access** (theme toggle, app preferences)
- **Emergency Contacts** quick access button

### **Shared UI Components**
- **Consistent Form Controls** with large, accessible inputs
- **Date Pickers** optimized for mobile interaction
- **Numeric Steppers** for quantity inputs
- **Progress Indicators** for multi-step processes
- **Confirmation Dialogs** for destructive actions
- **Toast Notifications** for user feedback

## **Technical Requirements**

### **Platform Support**
- **Android** (API level 21+)
- **iOS** (iOS 11.0+)
- **Cross-platform** UI consistency with platform-specific optimizations

### **Firebase Integration**
- **Firestore Database** with offline persistence enabled
- **Firebase Authentication** with offline token caching
- **Firebase Storage** for image uploads with offline queuing
- **Firebase Analytics** for usage tracking and crash reporting
- **Firebase Remote Config** for feature flags and app configuration

### **Offline Capabilities**
- **Complete Offline Functionality** - full app operation without network
- **Automatic Data Synchronization** when connection is restored
- **Conflict Resolution** for concurrent data modifications
- **Local SQLite Database** for offline data storage
- **Image Caching** for offline media access
- **Queue Management** for pending operations

### **Performance Optimization**
- **Lazy Loading** for large datasets
- **Image Compression** and caching
- **Background Sync** services
- **Memory Management** for resource-constrained devices
- **Battery Optimization** for extended field use

### **Field-Specific Features**
- **High Contrast Theme** for bright sunlight visibility
- **Large Button Sizes** for gloved operation
- **Voice Input Support** for hands-free data entry
- **Quick Entry Modes** for rapid data capture
- **Weather-resistant UI** considerations
- **GPS Accuracy Optimization** for remote locations

### **Data Management**
- **Backup and Restore** functionality
- **Data Export** capabilities (CSV, PDF reports)
- **Data Validation** with intelligent error correction
- **Historical Data Analysis** and trending
- **Comparative Analytics** across seasons/groups

### **Security Requirements**
- **End-to-End Encryption** for sensitive data
- **Secure Credential Storage** using platform keychains
- **Certificate Pinning** for API communications
- **Data Loss Prevention** with automatic backups
- **Audit Logging** for data modifications

## **User Experience Considerations**

### **Field Usage Optimization**
- **Single-handed Operation** support where possible
- **Quick Actions** for common tasks
- **Gestural Navigation** for efficient interaction
- **Predictive Input** to reduce typing
- **Contextual Help** and guidance tooltips

### **Accessibility Features**
- **WCAG 2.1 AA Compliance** for accessibility standards
- **Screen Reader Support** for visually impaired users
- **High Contrast Modes** for various lighting conditions
- **Adjustable Font Sizes** for readability
- **Voice Commands** for hands-free operation

### **Data Entry Efficiency**
- **Smart Defaults** based on historical data
- **Bulk Operations** for managing multiple records
- **Template Systems** for recurring entries
- **Auto-completion** for common values
- **Validation Warnings** before data loss

## **Reporting and Analytics**
- **Real-time Dashboards** with key performance indicators
- **Seasonal Comparisons** and trend analysis
- **Benchmarking** against industry standards
- **Custom Report Builder** for specific metrics
- **Export Capabilities** for external analysis

## **Additional Requirements**
- **Multi-language Support** (initially English, expandable)
- **Unit System Toggle** (metric/imperial)
- **Time Zone Handling** for accurate date recording
- **Camera Integration** for documentation photos
- **Barcode/QR Scanning** for livestock identification
- **Integration APIs** for third-party veterinary/agricultural systems

Please develop this .NET MAUI application with clean, maintainable MVVM architecture, comprehensive error handling, and an intuitive user experience optimized for agricultural field conditions. The application should be robust, reliable, and function seamlessly in challenging environmental conditions with intermittent connectivity.
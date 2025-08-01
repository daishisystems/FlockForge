# Firebase Testing Guide for FlockForge

## Overview
This guide provides detailed test cases and procedures for validating the Firebase implementation in FlockForge. All tests should be performed on both Android and iOS platforms.

## Test Environment Setup

### Prerequisites
- FlockForge app installed on test devices
- Firebase project configured with test data
- Test user accounts created
- Network simulation tools (optional)

### Test Data Setup
Create the following test data in Firebase Console:

```json
// Test Farmer
{
  "id": "test-farmer-001",
  "userId": "test-user-001",
  "name": "John Test Farmer",
  "email": "test@flockforge.com",
  "phoneNumber": "+27123456789",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "isDeleted": false
}

// Test Farm
{
  "id": "test-farm-001",
  "farmerId": "test-user-001",
  "name": "Test Farm",
  "location": "Test Location",
  "size": 100.5,
  "farmType": "Sheep",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "isDeleted": false
}
```

## Authentication Tests

### Test Case: AUTH-001 - Email/Password Registration
**Objective**: Verify new user can register with email and password

**Steps**:
1. Launch FlockForge app
2. Navigate to registration screen
3. Enter valid email: `newuser@test.com`
4. Enter strong password: `TestPass123!`
5. Confirm password
6. Tap Register button

**Expected Result**:
- Registration succeeds
- User is automatically logged in
- User is redirected to main screen
- Firebase Authentication shows new user

**Test Data**:
```
Email: newuser@test.com
Password: TestPass123!
```

### Test Case: AUTH-002 - Email/Password Login
**Objective**: Verify existing user can login with correct credentials

**Steps**:
1. Launch FlockForge app
2. Navigate to login screen
3. Enter email: `test@flockforge.com`
4. Enter password: `TestPass123!`
5. Tap Login button

**Expected Result**:
- Login succeeds
- User is redirected to main screen
- Authentication state is persisted

### Test Case: AUTH-003 - Invalid Login Attempt
**Objective**: Verify app handles invalid login credentials gracefully

**Steps**:
1. Launch FlockForge app
2. Navigate to login screen
3. Enter email: `test@flockforge.com`
4. Enter wrong password: `WrongPassword`
5. Tap Login button

**Expected Result**:
- Login fails with appropriate error message
- User remains on login screen
- No crash or unexpected behavior

### Test Case: AUTH-004 - Offline Authentication Persistence
**Objective**: Verify user stays logged in when offline

**Steps**:
1. Login to app while online
2. Verify successful login
3. Disable device internet connection
4. Close and reopen app
5. Navigate through app features

**Expected Result**:
- User remains logged in
- App functions with cached data
- No logout or authentication errors

### Test Case: AUTH-005 - Google Sign-In (Android)
**Objective**: Verify Google SSO integration works

**Steps**:
1. Launch FlockForge app
2. Navigate to login screen
3. Tap "Sign in with Google" button
4. Select Google account
5. Grant permissions

**Expected Result**:
- Google sign-in flow completes
- User is logged into FlockForge
- User profile populated from Google

### Test Case: AUTH-006 - Apple Sign-In (iOS)
**Objective**: Verify Apple SSO integration works

**Steps**:
1. Launch FlockForge app on iOS device
2. Navigate to login screen
3. Tap "Sign in with Apple" button
4. Complete Apple ID authentication
5. Choose data sharing preferences

**Expected Result**:
- Apple sign-in flow completes
- User is logged into FlockForge
- User profile populated appropriately

## Firestore Data Tests

### Test Case: DATA-001 - Create Farm Record
**Objective**: Verify user can create new farm record

**Steps**:
1. Login to app
2. Navigate to Farms section
3. Tap "Add Farm" button
4. Fill in farm details:
   - Name: "Test Farm 2"
   - Location: "Test Location 2"
   - Size: 150.0
   - Type: "Sheep"
5. Save farm

**Expected Result**:
- Farm is created successfully
- Farm appears in farms list
- Data is saved to Firestore
- All required fields are populated

### Test Case: DATA-002 - Read Farm Records
**Objective**: Verify user can view their farm records

**Steps**:
1. Login to app
2. Navigate to Farms section
3. View farms list

**Expected Result**:
- All user's farms are displayed
- Farm details are accurate
- Only user's own farms are visible
- Loading is smooth and responsive

### Test Case: DATA-003 - Update Farm Record
**Objective**: Verify user can modify existing farm record

**Steps**:
1. Login to app
2. Navigate to Farms section
3. Select existing farm
4. Tap Edit button
5. Modify farm name to "Updated Test Farm"
6. Save changes

**Expected Result**:
- Farm is updated successfully
- Changes are reflected immediately
- Updated data is saved to Firestore
- UpdatedAt timestamp is updated

### Test Case: DATA-004 - Delete Farm Record (Soft Delete)
**Objective**: Verify farm deletion uses soft delete pattern

**Steps**:
1. Login to app
2. Navigate to Farms section
3. Select farm to delete
4. Tap Delete button
5. Confirm deletion

**Expected Result**:
- Farm is removed from UI
- Farm record still exists in Firestore with isDeleted=true
- Related records are not affected
- Deletion can be undone if needed

### Test Case: DATA-005 - Offline Data Creation
**Objective**: Verify data can be created while offline

**Steps**:
1. Login to app while online
2. Disable internet connection
3. Navigate to Farms section
4. Create new farm with details
5. Save farm
6. Re-enable internet connection

**Expected Result**:
- Farm is created and cached locally
- Farm syncs to Firestore when online
- No data loss occurs
- User experience is seamless

### Test Case: DATA-006 - Offline Data Reading
**Objective**: Verify cached data is available offline

**Steps**:
1. Login to app while online
2. Browse farms and other data
3. Disable internet connection
4. Navigate through app sections
5. View previously loaded data

**Expected Result**:
- Previously loaded data is available
- App functions normally with cached data
- No network error messages for cached data
- Performance remains acceptable

## Breeding Management Tests

### Test Case: BREED-001 - Create Breeding Record
**Objective**: Verify breeding record creation

**Steps**:
1. Login to app
2. Navigate to Breeding section
3. Tap "Add Breeding" button
4. Fill in breeding details:
   - Farm: Select test farm
   - Ram ID: "RAM001"
   - Ewe ID: "EWE001"
   - Breeding Date: Current date
   - Expected Lambing Date: +150 days
5. Save breeding record

**Expected Result**:
- Breeding record is created
- All fields are validated
- Expected lambing date is calculated correctly
- Record appears in breeding list

### Test Case: BREED-002 - Breeding Record Validation
**Objective**: Verify breeding record field validation

**Steps**:
1. Navigate to Add Breeding screen
2. Try to save with empty required fields
3. Try to save with invalid dates
4. Try to save with future breeding date

**Expected Result**:
- Validation errors are displayed
- Form cannot be submitted with invalid data
- Error messages are clear and helpful
- User can correct errors and resubmit

## Scanning Tests

### Test Case: SCAN-001 - Create Scanning Record
**Objective**: Verify scanning record creation

**Steps**:
1. Login to app
2. Navigate to Scanning section
3. Tap "Add Scanning" button
4. Fill in scanning details:
   - Farm: Select test farm
   - Ewe ID: "EWE001"
   - Scanning Date: Current date
   - Lamb Count: 2
   - Scanning Result: "Twins detected"
5. Save scanning record

**Expected Result**:
- Scanning record is created successfully
- Lamb count is validated (non-negative integer)
- Record appears in scanning list
- Data is saved to Firestore

## Lambing Tests

### Test Case: LAMB-001 - Create Lambing Record
**Objective**: Verify lambing record creation

**Steps**:
1. Login to app
2. Navigate to Lambing section
3. Tap "Add Lambing" button
4. Fill in lambing details:
   - Farm: Select test farm
   - Ewe ID: "EWE001"
   - Lambing Date: Current date
   - Lambs Alive: 2
   - Lambs Dead: 0
   - Complications: "None"
5. Save lambing record

**Expected Result**:
- Lambing record is created successfully
- Lamb counts are validated (non-negative)
- Record appears in lambing list
- Data is saved to Firestore

## Weaning Tests

### Test Case: WEAN-001 - Create Weaning Record
**Objective**: Verify weaning record creation

**Steps**:
1. Login to app
2. Navigate to Weaning section
3. Tap "Add Weaning" button
4. Fill in weaning details:
   - Farm: Select test farm
   - Lamb ID: "LAMB001"
   - Weaning Date: Current date
   - Weaning Weight: 25.5
   - Health Status: "Healthy"
5. Save weaning record

**Expected Result**:
- Weaning record is created successfully
- Weight is validated (positive number)
- Record appears in weaning list
- Data is saved to Firestore

## Security Tests

### Test Case: SEC-001 - User Data Isolation
**Objective**: Verify users can only access their own data

**Steps**:
1. Login as User A
2. Create farm record
3. Logout and login as User B
4. Try to access User A's farm data

**Expected Result**:
- User B cannot see User A's farms
- Firestore security rules prevent unauthorized access
- No data leakage between users
- Appropriate error handling if access attempted

### Test Case: SEC-002 - Unauthenticated Access Prevention
**Objective**: Verify unauthenticated users cannot access data

**Steps**:
1. Ensure user is logged out
2. Try to access app features directly
3. Attempt to navigate to data screens

**Expected Result**:
- User is redirected to login screen
- No data is accessible without authentication
- App gracefully handles unauthenticated state
- Security rules prevent data access

## Performance Tests

### Test Case: PERF-001 - App Startup Time
**Objective**: Measure app startup performance

**Steps**:
1. Close FlockForge app completely
2. Start timer
3. Launch app
4. Measure time until main screen is usable

**Expected Result**:
- App starts within 3 seconds on modern devices
- Firebase initialization doesn't block UI
- Splash screen displays appropriately
- No ANR (Application Not Responding) errors

### Test Case: PERF-002 - Data Loading Performance
**Objective**: Measure data loading speed

**Steps**:
1. Login to app
2. Navigate to section with large dataset
3. Measure loading time
4. Test with various data sizes

**Expected Result**:
- Data loads within 2 seconds for typical datasets
- Loading indicators are shown during fetch
- Pagination or lazy loading for large datasets
- Smooth scrolling performance

### Test Case: PERF-003 - Offline Sync Performance
**Objective**: Measure offline sync speed

**Steps**:
1. Create multiple records while offline
2. Go back online
3. Measure time for all data to sync

**Expected Result**:
- Sync completes within reasonable time
- Progress indication during sync
- No data conflicts or loss
- App remains responsive during sync

## Error Handling Tests

### Test Case: ERROR-001 - Network Timeout Handling
**Objective**: Verify app handles network timeouts gracefully

**Steps**:
1. Configure network to simulate slow connection
2. Attempt data operations
3. Let operations timeout

**Expected Result**:
- Timeout errors are handled gracefully
- User-friendly error messages displayed
- Retry mechanisms work correctly
- App doesn't crash or freeze

### Test Case: ERROR-002 - Firebase Service Unavailable
**Objective**: Verify app handles Firebase service outages

**Steps**:
1. Block Firebase endpoints at network level
2. Attempt various app operations
3. Observe error handling

**Expected Result**:
- Service unavailable errors are handled
- Offline mode activates automatically
- User is informed of service status
- App continues to function with cached data

### Test Case: ERROR-003 - Memory Pressure Handling
**Objective**: Verify app handles low memory conditions

**Steps**:
1. Run memory-intensive apps to reduce available memory
2. Use FlockForge app extensively
3. Monitor for memory-related crashes

**Expected Result**:
- App handles memory pressure gracefully
- No out-of-memory crashes
- Performance degrades gracefully if needed
- Data integrity is maintained

## Regression Tests

### Test Case: REG-001 - Core Functionality After Updates
**Objective**: Verify core features work after app updates

**Steps**:
1. Install previous version of app
2. Create test data
3. Update to new version
4. Verify all data is accessible
5. Test all core features

**Expected Result**:
- All existing data is preserved
- Core features continue to work
- No breaking changes in user experience
- Migration (if any) completes successfully

## Test Execution Checklist

### Pre-Test Setup
- [ ] Test devices prepared (Android/iOS)
- [ ] Firebase test project configured
- [ ] Test data created
- [ ] Network simulation tools ready
- [ ] Test accounts created

### Test Execution
- [ ] All authentication tests passed
- [ ] All data operation tests passed
- [ ] All security tests passed
- [ ] All performance tests passed
- [ ] All error handling tests passed
- [ ] All regression tests passed

### Post-Test Cleanup
- [ ] Test data cleaned up
- [ ] Test accounts removed
- [ ] Test results documented
- [ ] Issues logged and prioritized
- [ ] Sign-off obtained from stakeholders

## Test Results Template

```markdown
## Test Execution Report

**Date**: [Date]
**Tester**: [Name]
**App Version**: [Version]
**Platform**: [Android/iOS]
**Device**: [Device Model]

### Test Results Summary
- Total Tests: [Number]
- Passed: [Number]
- Failed: [Number]
- Skipped: [Number]

### Failed Tests
| Test Case | Issue Description | Severity | Status |
|-----------|------------------|----------|--------|
| AUTH-001  | Login timeout    | High     | Open   |

### Performance Metrics
- App Startup Time: [Time]
- Data Loading Time: [Time]
- Sync Time: [Time]

### Recommendations
- [List of recommendations]

### Sign-off
- [ ] QA Approved
- [ ] Development Approved
- [ ] Product Owner Approved
```

## Automated Testing Integration

### Unit Tests
```csharp
[Test]
public async Task FirebaseAuthenticationService_LoginAsync_ValidCredentials_ReturnsSuccess()
{
    // Arrange
    var authService = new FirebaseAuthenticationService(mockConfig);
    var email = "test@example.com";
    var password = "TestPass123!";
    
    // Act
    var result = await authService.LoginAsync(email, password);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsNotNull(result.User);
}
```

### Integration Tests
```csharp
[Test]
public async Task FirestoreService_CreateFarmAsync_ValidData_SavesSuccessfully()
{
    // Arrange
    var firestoreService = new FirestoreService(mockConfig);
    var farm = new Farm { Name = "Test Farm", Location = "Test Location" };
    
    // Act
    var result = await firestoreService.CreateAsync(farm);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsNotNull(result.Data.Id);
}
```

This comprehensive testing guide ensures the Firebase implementation meets all requirements and handles edge cases appropriately.
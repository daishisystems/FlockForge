# Additional Requirements and Improvements for FlockForge

## Overview
This document outlines additional requirements and improvements identified during the review of entity models against the app specification. These enhancements will help ensure the application fully meets the specified requirements.

## Farmer Entity Improvements

### Missing Fields
The current Farmer entity is missing several fields required by the app specification:

1. **City** - Text input with autocomplete
2. **Province** - Dropdown/picker with predefined options
3. **Zip Code** - Text input with format validation
4. **Sub Region** - Text input

### Recommended Implementation
```csharp
public class Farmer : BaseEntity
{
    // Existing fields...
    
    /// <summary>
    /// City for address autocomplete
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }
    
    /// <summary>
    /// Province/State
    /// </summary>
    [MaxLength(100)]
    public string? Province { get; set; }
    
    /// <summary>
    /// Postal/Zip code
    /// </summary>
    [MaxLength(20)]
    public string? ZipCode { get; set; }
    
    /// <summary>
    /// Sub region or district
    /// </summary>
    [MaxLength(100)]
    public string? SubRegion { get; set; }
}
```

## Farm Entity Improvements

### Missing Fields
The current Farm entity is missing one field required by the app specification:

1. **Co Op** - Text input for cooperative information

### Recommended Implementation
```csharp
public class Farm : BaseEntity
{
    // Existing fields...
    
    /// <summary>
    /// Cooperative or association membership
    /// </summary>
    [MaxLength(200)]
    public string? CoOp { get; set; }
}
```

## BreedingRecord Entity Improvements

### Missing Fields
The current BreedingRecord entity is missing several fields required by the app specification:

1. **Did You Use Follow Up Rams?** - Yes/no toggle
2. **Follow Up Rams In** - Date picker (conditional)
3. **Follow Up Rams Out** - Date picker (conditional)
4. **Days In** - Auto-calculated field
5. **Date For Year Calculation** - Date picker
6. **Year** - Auto-populated from date

### Recommended Implementation
```csharp
public class BreedingRecord : BaseEntity
{
    // Existing fields...
    
    /// <summary>
    /// Whether follow-up rams were used
    /// </summary>
    public bool? UsedFollowUpRams { get; set; }
    
    /// <summary>
    /// Date follow-up rams were introduced
    /// </summary>
    public DateOnly? FollowUpRamsIn { get; set; }
    
    /// <summary>
    /// Date follow-up rams were removed
    /// </summary>
    public DateOnly? FollowUpRamsOut { get; set; }
    
    /// <summary>
    /// Number of days follow-up rams were present
    /// </summary>
    [NotMapped]
    public int? DaysIn => FollowUpRamsIn.HasValue && FollowUpRamsOut.HasValue 
        ? (FollowUpRamsOut.Value.DayNumber - FollowUpRamsIn.Value.DayNumber + 1) 
        : null;
    
    /// <summary>
    /// Date used for year calculation
    /// </summary>
    public DateOnly? YearCalculationDate { get; set; }
    
    /// <summary>
    /// Year extracted from calculation date
    /// </summary>
    [NotMapped]
    public int? Year => YearCalculationDate?.Year;
}
```

## ScanningRecord Entity Improvements

### Missing Fields
The current ScanningRecord entity is missing several fields required by the app specification:

1. **Ewes Mated** - Auto-populated from breeding data
2. **Scanned Fetuses** - Numeric input

### Recommended Implementation
```csharp
public class ScanningRecord : BaseEntity
{
    // Existing fields...
    
    /// <summary>
    /// Number of ewes mated (from breeding records)
    /// </summary>
    public int? EwesMated { get; set; }
    
    /// <summary>
    /// Total number of fetuses scanned
    /// </summary>
    [Range(0, 30000)]
    public int? ScannedFetuses { get; set; }
    
    // Additional computed properties for the missing percentages in the spec
    /// <summary>
    /// Expected lambing percentage of ewes pregnant
    /// </summary>
    [NotMapped]
    public decimal ExpectedLambingPercentageOfPregnant
    {
        get
        {
            if (EwesPregnant == 0) return 0;
            return Math.Round((decimal)ExpectedLambs / EwesPregnant * 100, 1);
        }
    }
    
    /// <summary>
    /// Expected lambing percentage of ewes mated
    /// </summary>
    [NotMapped]
    public decimal ExpectedLambingPercentageOfMated
    {
        get
        {
            if (!EwesMated.HasValue || EwesMated == 0) return 0;
            return Math.Round((decimal)ExpectedLambs / EwesMated.Value * 100, 1);
        }
    }
}
```

## LambingRecord Entity Improvements

### Missing Fields
The current LambingRecord entity is missing several computed fields required by the app specification. While these can be calculated, they should be explicitly documented:

### Recommended Implementation
```csharp
public class LambingRecord : BaseEntity
{
    // Existing fields...
    
    // Additional computed properties for the missing percentages in the spec
    /// <summary>
    /// Lambs after mortality (auto-calculated)
    /// </summary>
    [NotMapped]
    public int LambsAfterMortality => TotalLambsBorn - LambMortality;
    
    /// <summary>
    /// Lambing percentage of ewes mated
    /// </summary>
    [NotMapped]
    public decimal LambingPercentageOfEwesMated
    {
        get
        {
            // This would require access to breeding record data
            // Implementation would depend on how breeding data is linked
            return 0; // Placeholder
        }
    }
    
    /// <summary>
    /// Lambing percentage of ewes lambed
    /// </summary>
    [NotMapped]
    public decimal LambingPercentageOfEwesLambed
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)TotalLambsBorn / EwesLambed * 100, 1);
        }
    }
    
    /// <summary>
    /// Lambing mortality percentage
    /// </summary>
    [NotMapped]
    public decimal LambingMortalityPercentage
    {
        get
        {
            if (TotalLambsBorn == 0) return 0;
            return Math.Round((decimal)LambsBornDead / TotalLambsBorn * 100, 1);
        }
    }
    
    /// <summary>
    /// Percentage of ewes lambed from mating
    /// </summary>
    [NotMapped]
    public decimal PercentageEwesLambedFromMating
    {
        get
        {
            // This would require access to breeding record data
            // Implementation would depend on how breeding data is linked
            return 0; // Placeholder
        }
    }
    
    /// <summary>
    /// Dry ewes percentage of ewes mated
    /// </summary>
    [NotMapped]
    public decimal DryEwesPercentageOfEwesMated
    {
        get
        {
            // This would require access to breeding record data
            // Implementation would depend on how breeding data is linked
            return 0; // Placeholder
        }
    }
    
    /// <summary>
    /// Dry ewes percentage of ewes lambed
    /// </summary>
    [NotMapped]
    public decimal DryEwesPercentageOfEwesLambed
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)(EwesLambed - EwesPregnant) / EwesLambed * 100, 1);
        }
    }
    
    /// <summary>
    /// Lambing mortality percentage of ewes lambed
    /// </summary>
    [NotMapped]
    public decimal LambingMortalityPercentageOfEwesLambed
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)LambsBornDead / EwesLambed * 100, 1);
        }
    }
}
```

## WeaningRecord Entity Improvements

### Missing Fields
The current WeaningRecord entity is missing several fields required by the app specification:

1. **Rams Weaned** - Numeric input
2. **Breeding Ewes Dead From Scan** - Numeric input
3. **Other Mortalities** - Numeric input with categorization

### Recommended Implementation
```csharp
public class WeaningRecord : BaseEntity
{
    // Existing fields...
    
    /// <summary>
    /// Number of rams weaned
    /// </summary>
    [Range(0, 10000)]
    public int RamsWeaned { get; set; }
    
    /// <summary>
    /// Number of breeding ewes that died after scanning
    /// </summary>
    [Range(0, 10000)]
    public int BreedingEwesDeadFromScan { get; set; }
    
    /// <summary>
    /// Other mortalities with category
    /// </summary>
    [Range(0, 10000)]
    public int OtherMortalities { get; set; }
    
    /// <summary>
    /// Category for other mortalities
    /// </summary>
    [MaxLength(100)]
    public string? OtherMortalityCategory { get; set; }
    
    // Additional computed properties for the missing percentages in the spec
    /// <summary>
    /// Percentage ewes weaned
    /// </summary>
    [NotMapped]
    public decimal PercentageEwesWeaned
    {
        get
        {
            // This would require access to scanning record data
            // Implementation would depend on how scanning data is linked
            return 0; // Placeholder
        }
    }
    
    /// <summary>
    /// Lambs weaned percentage of ewes mated
    /// </summary>
    [NotMapped]
    public decimal LambsWeanedPercentageOfEwesMated
    {
        get
        {
            // This would require access to breeding record data
            // Implementation would depend on how breeding data is linked
            return 0; // Placeholder
        }
    }
    
    /// <summary>
    /// Lambs weaned percentage of ewes lambed
    /// </summary>
    [NotMapped]
    public decimal LambsWeanedPercentageOfEwesLambed
    {
        get
        {
            // This would require access to lambing record data
            // Implementation would depend on how lambing data is linked
            return 0; // Placeholder
        }
    }
}
```

## Implementation Priority

### High Priority (Required for App Specification Compliance)
1. Farmer entity missing fields (City, Province, ZipCode, SubRegion)
2. Farm entity missing field (CoOp)
3. BreedingRecord entity missing fields (UsedFollowUpRams, FollowUpRamsIn, FollowUpRamsOut, YearCalculationDate)

### Medium Priority (Enhances Functionality)
1. ScanningRecord entity missing fields (EwesMated, ScannedFetuses)
2. LambingRecord entity missing computed properties
3. WeaningRecord entity missing fields (RamsWeaned, BreedingEwesDeadFromScan, OtherMortalities)

### Low Priority (Nice to Have)
1. Additional computed properties for all entities to match specification exactly

## Migration Considerations

When implementing these changes, database migrations will be required to add the new fields. The migrations should:

1. Add new columns to existing tables
2. Set appropriate data types and constraints
3. Maintain backward compatibility with existing data
4. Be tested thoroughly before applying to production

## Testing Considerations

New unit tests will be required for:

1. All new fields and their validation
2. New computed properties
3. Business logic for conditional fields
4. Integration between related entities with new fields

## Conclusion

These improvements will bring the entity models into full compliance with the app specification requirements. The changes are primarily additive and should not break existing functionality, but careful implementation and testing will be required.
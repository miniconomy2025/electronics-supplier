# Simulation Time System

This system converts between decimal simulation time and canonical DateTime objects.

## Overview

- **Epoch**: 2050-01-01 00:00:00 UTC
- **Format**: Decimal values (e.g., 2.500 = Day 2, 12:00)
- **Storage**: All timestamps stored as `NUMERIC(1000,3)` in database
- **API**: Convert to canonical `DateTime` for responses

## Usage

### Basic Conversion

```csharp
// Convert simulation time to canonical DateTime
decimal simTime = 2.500m; // Day 2, 12:00
DateTime canonical = simTime.ToCanonicalTime();
// Result: 2050-01-02 12:00:00 UTC

// Convert canonical DateTime to simulation time
DateTime canonical = new DateTime(2050, 1, 2, 12, 0, 0, DateTimeKind.Utc);
decimal simTime = canonical.ToSimulationTime();
// Result: 2.500m
```

### In Controllers

```csharp
// Store with simulation time
var order = new ElectronicsOrder
{
    OrderedAt = _stateService.GetCurrentSimulationTime(3), // e.g., 2.500
    // ... other properties
};

// API response with canonical time
var dto = ElectronicsOrderCanonicalDto.FromModel(order);
// Returns: OrderedAt = 2050-01-02 12:00:00 UTC
```

### Extension Methods

```csharp
// Direct conversion
DateTime canonical = 1.500m.ToCanonicalTime();
decimal simTime = DateTime.UtcNow.ToSimulationTime();

// Nullable values
DateTime? canonical = nullableDecimal?.ToCanonicalTime();
decimal? simTime = nullableDateTime?.ToSimulationTime();
```

## Examples

| Simulation Time | Canonical DateTime | Description |
|----------------|-------------------|-------------|
| 1.000 | 2050-01-01 00:00:00 | Day 1, midnight |
| 1.500 | 2050-01-01 12:00:00 | Day 1, noon |
| 2.000 | 2050-01-02 00:00:00 | Day 2, midnight |
| 2.250 | 2050-01-02 06:00:00 | Day 2, 6 AM |
| 2.500 | 2050-01-02 12:00:00 | Day 2, noon |

## Testing

Run the unit tests to verify conversions:

```bash
dotnet test --filter "SimulationTimeServiceTests"
``` 
using esAPI.Services;
using Xunit;

namespace esAPI.Tests.Services
{
    public class SimulationTimeServiceTests
    {
        [Fact]
        public void ToCanonicalTime_WithValidSimulationTime_ReturnsCorrectDateTime()
        {
            // Arrange
            decimal simulationTime = 2.500m; // Day 2, 12:00 (noon)

            // Act
            var result = SimulationTimeService.ToCanonicalTime(simulationTime);

            // Assert
            Assert.Equal(new DateTime(2050, 1, 2, 12, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void ToCanonicalTime_WithDay1_ReturnsEpochDate()
        {
            // Arrange
            decimal simulationTime = 1.000m; // Day 1, 00:00

            // Act
            var result = SimulationTimeService.ToCanonicalTime(simulationTime);

            // Assert
            Assert.Equal(new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void ToCanonicalTime_WithDay1Noon_ReturnsCorrectTime()
        {
            // Arrange
            decimal simulationTime = 1.500m; // Day 1, 12:00

            // Act
            var result = SimulationTimeService.ToCanonicalTime(simulationTime);

            // Assert
            Assert.Equal(new DateTime(2050, 1, 1, 12, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void FromCanonicalTime_WithValidDateTime_ReturnsCorrectSimulationTime()
        {
            // Arrange
            var canonicalTime = new DateTime(2050, 1, 2, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var result = SimulationTimeService.FromCanonicalTime(canonicalTime);

            // Assert
            Assert.Equal(2.500m, result);
        }

        [Fact]
        public void FromCanonicalTime_WithEpochDate_ReturnsDay1()
        {
            // Arrange
            var canonicalTime = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = SimulationTimeService.FromCanonicalTime(canonicalTime);

            // Assert
            Assert.Equal(1.000m, result);
        }

        [Fact]
        public void RoundTripConversion_ShouldBeReversible()
        {
            // Arrange
            decimal originalSimTime = 3.750m; // Day 3, 18:00

            // Act
            var canonicalTime = SimulationTimeService.ToCanonicalTime(originalSimTime);
            var backToSimTime = SimulationTimeService.FromCanonicalTime(canonicalTime);

            // Assert
            Assert.True(Math.Abs(backToSimTime - originalSimTime) < 0.001m);
        }

        [Fact]
        public void ExtensionMethods_ShouldWorkCorrectly()
        {
            // Arrange
            decimal simTime = 2.250m; // Day 2, 06:00
            var canonicalTime = new DateTime(2050, 1, 2, 6, 0, 0, DateTimeKind.Utc);

            // Act
            var convertedCanonical = simTime.ToCanonicalTime();
            var convertedSimTime = canonicalTime.ToSimulationTime();

            // Assert
            Assert.Equal(canonicalTime, convertedCanonical);
            Assert.True(Math.Abs(convertedSimTime - simTime) < 0.001m);
        }

        [Fact]
        public void NullableExtensionMethods_ShouldWorkCorrectly()
        {
            // Arrange
            decimal? nullableSimTime = 1.500m;
            DateTime? nullableCanonicalTime = new DateTime(2050, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var convertedCanonical = nullableSimTime.ToCanonicalTime();
            var convertedSimTime = nullableCanonicalTime.ToSimulationTime();

            // Assert
            Assert.Equal(nullableCanonicalTime, convertedCanonical);
            Assert.True(convertedSimTime.HasValue && Math.Abs(convertedSimTime.Value - nullableSimTime.Value) < 0.001m);
        }

        [Fact]
        public void NullableExtensionMethods_WithNullValues_ShouldReturnNull()
        {
            // Arrange
            decimal? nullableSimTime = null;
            DateTime? nullableCanonicalTime = null;

            // Act
            var convertedCanonical = nullableSimTime.ToCanonicalTime();
            var convertedSimTime = nullableCanonicalTime.ToSimulationTime();

            // Assert
            Assert.Null(convertedCanonical);
            Assert.Null(convertedSimTime);
        }
    }
} 
using esAPI.Services;
using FluentAssertions;
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
            result.Should().Be(new DateTime(2050, 1, 2, 12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToCanonicalTime_WithDay1_ReturnsEpochDate()
        {
            // Arrange
            decimal simulationTime = 1.000m; // Day 1, 00:00

            // Act
            var result = SimulationTimeService.ToCanonicalTime(simulationTime);

            // Assert
            result.Should().Be(new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToCanonicalTime_WithDay1Noon_ReturnsCorrectTime()
        {
            // Arrange
            decimal simulationTime = 1.500m; // Day 1, 12:00

            // Act
            var result = SimulationTimeService.ToCanonicalTime(simulationTime);

            // Assert
            result.Should().Be(new DateTime(2050, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void FromCanonicalTime_WithValidDateTime_ReturnsCorrectSimulationTime()
        {
            // Arrange
            var canonicalTime = new DateTime(2050, 1, 2, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var result = SimulationTimeService.FromCanonicalTime(canonicalTime);

            // Assert
            result.Should().Be(2.500m);
        }

        [Fact]
        public void FromCanonicalTime_WithEpochDate_ReturnsDay1()
        {
            // Arrange
            var canonicalTime = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = SimulationTimeService.FromCanonicalTime(canonicalTime);

            // Assert
            result.Should().Be(1.000m);
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
            backToSimTime.Should().BeApproximately(originalSimTime, 0.001m);
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
            convertedCanonical.Should().Be(canonicalTime);
            convertedSimTime.Should().BeApproximately(simTime, 0.001m);
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
            convertedCanonical.Should().Be(nullableCanonicalTime);
            convertedSimTime.Should().BeApproximately(nullableSimTime.Value, 0.001m);
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
            convertedCanonical.Should().BeNull();
            convertedSimTime.Should().BeNull();
        }
    }
} 
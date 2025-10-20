using Xunit;
using FluentAssertions;
using esAPI.Services;
using esAPI.Models;

namespace esAPI.Tests.Services
{
    public class SimulationStateServiceTests
    {
        private readonly SimulationStateService _service;

        public SimulationStateServiceTests()
        {
            _service = new SimulationStateService();
        }

        [Fact]
        public void Initial_State_Should_Be_Stopped()
        {
            // Assert
            _service.IsRunning.Should().BeFalse();
            _service.StartTimeUtc.Should().BeNull();
            _service.CurrentDay.Should().Be(0);
            _service.GetCurrentSimulationTime().Should().Be(0);
        }

        [Fact]
        public void Start_Should_Initialize_Simulation_State()
        {
            // Arrange
            var beforeStart = DateTime.UtcNow;

            // Act
            _service.Start();

            // Assert
            _service.IsRunning.Should().BeTrue();
            _service.CurrentDay.Should().Be(1);
            _service.StartTimeUtc.Should().NotBeNull();
            _service.StartTimeUtc.Should().BeOnOrAfter(beforeStart);
            _service.StartTimeUtc.Should().BeOnOrBefore(DateTime.UtcNow);
            _service.GetCurrentSimulationTime().Should().BeGreaterThanOrEqualTo(1.0m);
        }

        [Fact]
        public void Start_WithEpochTime_Should_Use_External_Reference()
        {
            // Arrange
            var epochStartTime = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(); // 10 minutes ago

            // Act
            _service.Start(epochStartTime);

            // Assert
            _service.IsRunning.Should().BeTrue();
            _service.CurrentDay.Should().Be(1);
            var simTime = _service.GetCurrentSimulationTime();

            // Should be greater than 1 since we started 10 minutes ago
            // 10 minutes = 5 simulation days (2 minutes per day)
            simTime.Should().BeGreaterThan(5.0m);
        }

        [Fact]
        public void Stop_Should_Reset_All_State()
        {
            // Arrange
            _service.Start();
            _service.AdvanceDay(); // Make some progress

            // Act
            _service.Stop();

            // Assert
            _service.IsRunning.Should().BeFalse();
            _service.StartTimeUtc.Should().BeNull();
            _service.CurrentDay.Should().Be(0);
            _service.GetCurrentSimulationTime().Should().Be(0);
        }

        [Fact]
        public void AdvanceDay_When_Running_Should_Increment_Day()
        {
            // Arrange
            _service.Start();
            var initialDay = _service.CurrentDay;

            // Act
            _service.AdvanceDay();

            // Assert
            _service.CurrentDay.Should().Be(initialDay + 1);
            _service.IsRunning.Should().BeTrue(); // Should still be running
        }

        [Fact]
        public void AdvanceDay_When_Stopped_Should_Not_Change_Day()
        {
            // Arrange - service is stopped by default
            var initialDay = _service.CurrentDay;

            // Act
            _service.AdvanceDay();

            // Assert
            _service.CurrentDay.Should().Be(initialDay); // No change
        }

        [Fact]
        public void GetCurrentSimulationTime_Should_Calculate_Correctly()
        {
            // Arrange
            _service.Start();
            var startTime = _service.StartTimeUtc!.Value;

            // Simulate 4 minutes elapsed (2 simulation days)
            // We can't easily mock DateTime.UtcNow, so we'll test the precision instead
            var simTime = _service.GetCurrentSimulationTime();

            // Assert
            simTime.Should().BeGreaterThanOrEqualTo(1.0m); // At least day 1
            simTime.Should().BeLessThan(10.0m); // Should be reasonable

            // Test precision
            var simTimeWith2Decimals = _service.GetCurrentSimulationTime(2);
            simTimeWith2Decimals.Should().NotHaveMoreThan2DecimalPlaces();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void GetCurrentSimulationTime_Should_Respect_Precision(int precision)
        {
            // Arrange
            _service.Start();

            // Act
            var simTime = _service.GetCurrentSimulationTime(precision);

            // Assert
            simTime.Should().NotHaveMoreThanNDecimalPlaces(precision);
        }

        [Fact]
        public void RestoreFromBackup_Should_Set_All_Properties()
        {
            // Arrange
            var backupTime = DateTime.UtcNow.AddHours(-1);
            var simulation = new esAPI.Models.Simulation
            {
                IsRunning = true,
                StartedAt = backupTime,
                DayNumber = 5
            };

            // Act
            _service.RestoreFromBackup(simulation);

            // Assert
            _service.IsRunning.Should().BeTrue();
            _service.StartTimeUtc.Should().Be(backupTime);
            _service.CurrentDay.Should().Be(5);
        }

        [Fact]
        public void ToBackupEntity_Should_Create_Correct_Simulation()
        {
            // Arrange
            _service.Start();
            _service.AdvanceDay();
            _service.AdvanceDay();
            var expectedStartTime = _service.StartTimeUtc;

            // Act
            var backup = _service.ToBackupEntity();

            // Assert
            backup.Should().NotBeNull();
            backup.IsRunning.Should().BeTrue();
            backup.StartedAt.Should().Be(expectedStartTime);
            backup.DayNumber.Should().Be(3); // Started at 1, advanced twice
        }

        [Fact]
        public void ToBackupEntity_When_Stopped_Should_Create_Stopped_Simulation()
        {
            // Arrange
            _service.Start();
            _service.Stop();

            // Act
            var backup = _service.ToBackupEntity();

            // Assert
            backup.Should().NotBeNull();
            backup.IsRunning.Should().BeFalse();
            backup.StartedAt.Should().BeNull();
            backup.DayNumber.Should().Be(0);
        }

        [Fact]
        public async Task Concurrent_Operations_Should_Be_Thread_Safe()
        {
            // Arrange
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();
            var lockObject = new object();

            // Act - Simulate concurrent start/advance operations (avoid stop to prevent race conditions)
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        _service.Start();
                        _service.AdvanceDay();
                        _service.AdvanceDay();
                        // Don't stop to avoid race conditions in the test
                    }
                    catch (Exception ex)
                    {
                        lock (lockObject)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            // Assert - Should not throw exceptions and service should be in valid state
            exceptions.Should().BeEmpty("because thread-safe operations should not throw exceptions");
            _service.IsRunning.Should().BeTrue(); // Service should still be running
            _service.CurrentDay.Should().BeGreaterThanOrEqualTo(1); // Should have advanced
        }

        [Fact]
        public void Multiple_AdvanceDay_Calls_Should_Increment_Correctly()
        {
            // Arrange
            _service.Start();
            var initialDay = _service.CurrentDay;

            // Act
            for (int i = 0; i < 5; i++)
            {
                _service.AdvanceDay();
            }

            // Assert
            _service.CurrentDay.Should().Be(initialDay + 5);
        }
    }

    // Extension method to help with decimal precision testing
    public static class FluentAssertionsExtensions
    {
        public static AndConstraint<FluentAssertions.Numeric.NumericAssertions<decimal>> NotHaveMoreThanNDecimalPlaces(
            this FluentAssertions.Numeric.NumericAssertions<decimal> parent, int maxDecimalPlaces)
        {
            var value = parent.Subject;
            var multiplier = (decimal)Math.Pow(10, maxDecimalPlaces);
            var scaledValue = value * multiplier;
            var truncatedValue = Math.Truncate(scaledValue);

            scaledValue.Should().Be(truncatedValue,
                $"because the value should not have more than {maxDecimalPlaces} decimal places");

            return new AndConstraint<FluentAssertions.Numeric.NumericAssertions<decimal>>(parent);
        }

        public static AndConstraint<FluentAssertions.Numeric.NumericAssertions<decimal>> NotHaveMoreThan2DecimalPlaces(
            this FluentAssertions.Numeric.NumericAssertions<decimal> parent)
        {
            return parent.NotHaveMoreThanNDecimalPlaces(2);
        }
    }
}

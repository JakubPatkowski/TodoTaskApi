using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.UnitTests.DTOs
{
    /// <summary>
    /// Tests for todo completion related DTOs
    /// Verifies validation rules and constraints
    /// </summary>
    public class TodoCompletionDtoTests
    {
        /// <summary>
        /// Verifies that UpdateTodoCompletionDto validates percentage range correctly
        /// Tests range validation for completion percentage
        /// </summary>
        [Theory]
        [InlineData(0, true)]
        [InlineData(50, true)]
        [InlineData(100, true)]
        [InlineData(-1, false)]
        [InlineData(101, false)]
        public void UpdateTodoCompletionDto_ValidatesPercentageRange(int percentage, bool shouldBeValid)
        {
            // Arrange
            var dto = new UpdateTodoCompletionDto { PercentComplete = percentage };
            var context = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, validationResults, true);

            // Assert
            Assert.Equal(shouldBeValid, isValid);
            if (!shouldBeValid)
            {
                Assert.Contains(validationResults,
                    r => r.MemberNames.Contains("PercentComplete"));
            }
        }

        /// <summary>
        /// Verifies that UpdateTodoDoneStatusDto requires the IsDone property
        /// Tests required field validation
        /// </summary>
        [Fact]
        public void UpdateTodoDoneStatusDto_RequiresIsDoneProperty()
        {
            // Arrange
            var dto = new UpdateTodoDoneStatusDto();
            var context = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            // Act & Assert
            var isValid = Validator.TryValidateObject(dto, context, validationResults, true);
            Assert.False(isValid);
            Assert.Contains(validationResults,
                r => r.MemberNames.Contains("IsDone"));
        }
    }
}

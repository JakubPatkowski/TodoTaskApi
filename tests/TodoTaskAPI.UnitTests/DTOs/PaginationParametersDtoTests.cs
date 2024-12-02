using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Core.Exceptions;

namespace TodoTaskAPI.UnitTests.DTOs
{
    /// <summary>
    /// Unit tests for pagination parameters validation
    /// </summary>
    public class PaginationParametersDtoTests
    {
        /// <summary>
        /// Verifies that setting valid pagination parameters doesn't throw exceptions
        /// </summary>
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 50)]
        [InlineData(100, 100)]
        public void SetValidParameters_DoesNotThrow(int pageNumber, int pageSize)
        {
            // Arrange & Act
            var parameters = new PaginationParametersDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Assert - no exception should be thrown
            parameters.ValidateConsistency();
        }

        /// <summary>
        /// Verifies that setting invalid page number throws ValidationException
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetInvalidPageNumber_ThrowsValidationException(int pageNumber)
        {
            // Arrange
            var parameters = new PaginationParametersDto();

            // Act & Assert
            Assert.Throws<ValidationException>(() => parameters.PageNumber = pageNumber);
        }

        /// <summary>
        /// Verifies that setting invalid page size throws ValidationException
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public void SetInvalidPageSize_ThrowsValidationException(int pageSize)
        {
            // Arrange
            var parameters = new PaginationParametersDto();

            // Act & Assert
            Assert.Throws<ValidationException>(() => parameters.PageSize = pageSize);
        }

        /// <summary>
        /// Verifies that ValidateConsistency throws when only one parameter is set
        /// </summary>
        [Fact]
        public void ValidateConsistency_WithOnlyOneParameter_ThrowsValidationException()
        {
            // Arrange
            var parameters = new PaginationParametersDto { PageNumber = 1 };

            // Act & Assert
            Assert.Throws<ValidationException>(() => parameters.ValidateConsistency());
        }
    }
}

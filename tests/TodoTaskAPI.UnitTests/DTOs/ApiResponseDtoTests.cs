using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.UnitTests.DTOs
{
    public class ApiResponseDtoTests
    {
        /// <summary>
        /// Verifies that Success method creates correct response
        /// </summary>
        [Fact]
        public void Success_CreatesCorrectResponse()
        {
            // Arrange
            var data = "Test Data";
            var message = "Test Message";

            // Act
            var response = ApiResponseDto<string>.Success(data, message);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal(message, response.Message);
            Assert.Equal(data, response.Data);
        }

        /// <summary>
        /// Verifies that Failure method creates correct response
        /// </summary>
        [Fact]
        public void Failure_CreatesCorrectResponse()
        {
            // Arrange
            var statusCode = 400;
            var message = "Error Message";

            // Act
            var response = ApiResponseDto<string>.Failure(statusCode, message);

            // Assert
            Assert.Equal(statusCode, response.StatusCode);
            Assert.Equal(message, response.Message);
            Assert.Null(response.Data);
        }
    }
}

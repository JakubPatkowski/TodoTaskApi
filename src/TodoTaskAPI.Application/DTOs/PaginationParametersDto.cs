using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for pagination parameters with built-in validation
    /// </summary>
    public class PaginationParametersDto
    {
        // Constants for validation
        private const int MaxPageSize = 100;
        private const int MinPageSize = 1;
        private const int MinPageNumber = 1;

        // optional atributes
        private int? _pageSize;
        private int? _pageNumber;

        /// <summary>
        /// Page number for pagination. Must be greater than 0 if provided
        /// </summary>
        public int? PageNumber
        {
            get => _pageNumber;
            set
            {
                if (value.HasValue && value < MinPageNumber)
                {
                    throw new ValidationException($"Page number must be greater than or equal to {MinPageNumber}");
                }
                _pageNumber = value;
            }
        }

        /// <summary>
        /// Number of items per page. Must be between 1 and 100 if provided
        /// </summary>
        public int? PageSize
        {
            get => _pageSize;
            set
            {
                if (value.HasValue)
                {
                    if (value < MinPageSize)
                    {
                        throw new ValidationException($"Page size must be greater than or equal to {MinPageSize}");
                    }
                    if (value > MaxPageSize)
                    {
                        throw new ValidationException($"Page size cannot be greater than {MaxPageSize}");
                    }
                }
                _pageSize = value;
            }
        }

        /// <summary>
        /// Validates that either both pagination parameters are provided or neither is provided
        /// </summary>
        public void ValidateConsistency()
        {
            if (PageNumber.HasValue ^ PageSize.HasValue) // XOR operator
            {
                throw new ValidationException("Both pageNumber and pageSize must be provided together for pagination");
            }
        }
    }
}
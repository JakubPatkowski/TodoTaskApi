using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for filtering todos by time period
    /// </summary>
    public class TodoTimePeriodParametersDto
    {
        /// <summary>
        /// Predefined time periods for filtering todos
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TimePeriod
        {
            Today = 0,
            Tomorrow = 1,
            CurrentWeek = 2,
            Custom = 3
        }

        /// <summary>
        /// Time period to filter by
        /// </summary>
        [Required(ErrorMessage = "Time period is required")]
        [EnumDataType(typeof(TimePeriod))]
        public TimePeriod Period { get; set; }

        /// <summary>
        /// Custom start date (only used when Period is Custom)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Custom end date (only used when Period is Custom)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Validates that custom date range is provided when Period is Custom
        /// and that the date range is valid
        /// </summary>
        public void Validate()
        {
            if (Period == TimePeriod.Custom)
            {
                if (!StartDate.HasValue || !EndDate.HasValue)
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        "Start and end dates are required for custom time period");
                }

                if (StartDate > EndDate)
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        "Start date must be before or equal to end date");
                }

                if (StartDate < DateTime.UtcNow.Date)
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        "Start date cannot be in the past");
                }
            }
            else if (StartDate.HasValue || EndDate.HasValue)
            {
                throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                    "Custom dates should only be provided when using Custom period");
            }
        }
    }
}

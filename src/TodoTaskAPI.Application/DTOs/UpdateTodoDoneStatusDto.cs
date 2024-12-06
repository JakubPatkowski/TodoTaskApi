using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating todo done status
    /// Controls the done/undone state of a todo
    /// </summary>
    public class UpdateTodoDoneStatusDto
    {
        /// <summary>
        /// Flag indicating if the todo is done
        /// When set to true, automatically sets completion to 100%
        /// When set to false, resets completion to 0%
        /// </summary>
        [Required(ErrorMessage = "IsDone status is required")]
        public bool IsDone { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Core.Entities
{
    /// <summary>
    /// Represents a Todo item in the system.
    /// Core entity that tracks tasks and their completion status.
    /// </summary>
    public class Todo
    {
        /// <summary>
        /// Unique identifier for the Todo item
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Date and time by which the todo item should be completed.
        /// Stored in UTC format.
        /// </summary>
        public DateTime ExpiryDateTime { get; set; }

        /// <summary>
        /// Title or name of the todo item.
        /// Must be between 1 and 200 characters.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what needs to be done.
        /// Optional field that can contain up to 1000 characters.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Completion percentage of the todo item.
        /// Value between 0 and 100.
        /// Automatically set to 100 when IsDone is true.
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Indicates whether the todo item is completed.
        /// When set to true, PercentComplete is automatically set to 100.
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// UTC timestamp when the todo item was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp when the todo item was last updated.
        /// Null if the item has never been updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}

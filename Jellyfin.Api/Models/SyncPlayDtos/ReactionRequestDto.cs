using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// A request to react in the current SyncPlay group.
/// </summary>
public class ReactionRequestDto
{
    /// <summary>
    /// Gets or sets the reaction identifier.
    /// </summary>
    [Required]
    [StringLength(16)]
    public string ReactionId { get; set; } = string.Empty;
}

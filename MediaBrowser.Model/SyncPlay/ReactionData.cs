using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// A transient reaction sent to a SyncPlay group.
/// </summary>
public class ReactionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionData"/> class.
    /// </summary>
    /// <param name="reactionId">The allowlisted reaction identifier.</param>
    /// <param name="userId">The sending user's identifier.</param>
    /// <param name="userName">The sending user's display name.</param>
    public ReactionData(string reactionId, Guid userId, string userName)
    {
        ReactionId = reactionId;
        UserId = userId;
        UserName = userName;
    }

    /// <summary>Gets the allowlisted reaction identifier.</summary>
    public string ReactionId { get; }

    /// <summary>Gets the sender's user identifier.</summary>
    public Guid UserId { get; }

    /// <summary>Gets the sender's display name.</summary>
    public string UserName { get; }
}

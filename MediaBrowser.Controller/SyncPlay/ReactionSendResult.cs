namespace MediaBrowser.Controller.SyncPlay;

/// <summary>
/// Result of sending a transient SyncPlay reaction.
/// </summary>
public enum ReactionSendResult
{
    /// <summary>The reaction was sent.</summary>
    Sent,

    /// <summary>The session is not currently in a group.</summary>
    NotInGroup,

    /// <summary>The session sent a reaction too recently.</summary>
    RateLimited
}

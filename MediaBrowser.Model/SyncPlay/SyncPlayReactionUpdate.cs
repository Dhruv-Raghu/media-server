using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// A reaction update for members of a SyncPlay group.
/// </summary>
public class SyncPlayReactionUpdate : GroupUpdate<ReactionData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayReactionUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="data">The reaction data.</param>
    public SyncPlayReactionUpdate(Guid groupId, ReactionData data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.Reaction)]
    public override GroupUpdateType Type => GroupUpdateType.Reaction;
}

namespace GPSaveConverter.Xbox
{
    /// <summary>
    /// The values containers.index uses to say what the cloud has not seen yet. The Xbox services read
    /// them when the game starts, and upload whatever is not marked as synced.
    /// </summary>
    /// <remarks>
    /// The names come from libNOM.io, which writes this format for No Man's Sky, and they agree with
    /// what a save looks like right after the Xbox app has synced it: the save marked 3, each
    /// container marked 1.
    /// </remarks>
    internal static class XboxSyncState
    {
        /// <summary>The save as a whole: something in it has changed since the last upload.</summary>
        internal const uint IndexModified = 2;

        /// <summary>The save as a whole: it is the same as the copy in the cloud.</summary>
        internal const uint IndexSynced = 3;

        /// <summary>A container that is the same as its copy in the cloud.</summary>
        internal const uint ContainerSynced = 1;

        /// <summary>A container the cloud has, changed on this PC since.</summary>
        internal const uint ContainerModified = 2;

        /// <summary>A container removed on this PC that the cloud still has.</summary>
        internal const uint ContainerDeleted = 3;

        /// <summary>A container made on this PC that the cloud has never seen.</summary>
        internal const uint ContainerCreated = 5;
    }
}

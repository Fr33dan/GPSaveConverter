using System;

namespace GPSaveConverter.Library
{
    /// <summary>
    /// What players have reported about copying a game's saves, for a game whose saves do not carry
    /// over, or carry over only after a step the tool does not do. The tool shows it when the game
    /// is selected, and again before each transfer in the direction it is about.
    /// </summary>
    internal class GameWarning
    {
        internal const string ToXbox = "ToXbox";
        internal const string FromXbox = "FromXbox";

        /// <summary>The game's name, for whoever reads the library. The tool does not use it.</summary>
        public string Name { get; set; }

        public string PackageName { get; set; }

        /// <summary>
        /// <see cref="ToXbox"/> or <see cref="FromXbox"/> for a warning about one direction only, or
        /// null for one about both.
        /// </summary>
        public string Direction { get; set; }

        /// <summary>What players saw, in words for the user, with the issue it comes from.</summary>
        public string Text { get; set; }

        internal bool IsFor(string packageName)
        {
            return String.Equals(PackageName, packageName, StringComparison.OrdinalIgnoreCase);
        }

        internal bool AppliesTo(string packageName, bool toXbox)
        {
            return IsFor(packageName) && (Direction == null || Direction == (toXbox ? ToXbox : FromXbox));
        }

        /// <summary>
        /// The warning as the notice shown when the game is selected puts it, with its direction.
        /// </summary>
        internal string WithDirection()
        {
            if (Direction == ToXbox) return "Copying to Xbox: " + Text;
            if (Direction == FromXbox) return "Copying from Xbox: " + Text;
            return Text;
        }

        /// <summary>
        /// Checks the warning for mistakes that would stop it being shown.
        /// </summary>
        /// <returns>A description of the first problem found, or null if there is none.</returns>
        internal string FindProblem()
        {
            if (String.IsNullOrEmpty(PackageName)) return "A warning is missing its PackageName.";
            if (String.IsNullOrWhiteSpace(Text)) return "The warning for " + PackageName + " has no Text.";
            if (Direction != null && Direction != ToXbox && Direction != FromXbox)
            {
                return "The warning for " + PackageName + " has the Direction \"" + Direction + "\". It must be \"" + ToXbox + "\", \"" + FromXbox + "\", or left out for both.";
            }
            return null;
        }
    }
}

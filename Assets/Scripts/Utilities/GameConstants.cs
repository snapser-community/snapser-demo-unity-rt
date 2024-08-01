namespace Utilities
{
    public static class GameConstants
    {
        public static readonly string DefaultPlayerName = "UnityPlayer";

        public static readonly string DefaultClusterURL = "https://gateway.snapser.com/j38qb17h";
        public static readonly string UId = "UserId";
        public static readonly string UName = "UserName";
        public static readonly string SToken = "SessionToken";
        public static readonly string CAS = "CAS";

        public static readonly string Gamertag = "GamerTag";
        public static readonly int GamertagCharacterLimit = 15;

        public static readonly int GameStatsZeroPadding = 4;

        public static readonly string PlayerColor = "PlayerColor";
        public static readonly string PlayerColorRed = "red";
        public static readonly string PlayerColorBlue = "blue";
        public static readonly string StorageColorKey = "color";

        public static readonly string SnapshipLeaderboardName = "max_distance";
        public static readonly string InterstellarLeaderboardName = "most_wins";
        public static readonly string LeaderboardTopRange = "top";
        public static readonly string LeaderboardAroundRange = "around";
        public static readonly long LeaderboardEntryCount = 5;

        public static readonly string LifetimeDistanceTraveledKey = "distance_traveled";

        public static readonly string[] LoadingTexts = new[]
        {
            "Cleaning Basement...",
            "Building Pipes...",
            "Installing Wires...",
            "Configuring Modem/Router...",
            "Connecting Dial-Up...",
            "Creating Networks...",
            "Packing Packets...",
            "Sending Pigeons...",
            "Signing Packages...",
            "Encoding Data...",
            "Untangling Cables...",
            "Decoding Data...",
            "Writing Code...",
            "Testing Endpoint...",
            "Pushing Builds...",
            "Finding Bugs...",
            "Blaming Colleagues...",
            "Yelling At IDE...",
            "Debugging...",
            "Realizing Mistakes...",
            "Fixing Bugs...",
            "Patching Updates...",
            "Launching Games..."
        };

    }
}
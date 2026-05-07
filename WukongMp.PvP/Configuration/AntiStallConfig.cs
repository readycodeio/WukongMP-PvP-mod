namespace WukongMp.PvP.Configuration
{
    public static class AntiStallConfig
    {
        public const int WarningDuration = 6; // seconds
        public const float ActiveDuration = 2f; // seconds

        public const float RoomEngagementThreshold = 1f;
        public const float MaxRoomEngagementScore = 100f;
        public const float RoomEngagementDecayScore = 7f;
        public const float DamageRoomEngagementScore = 25f;
        public const float AttackRoomEngagementScore = 12f;

        public const float BaseAttributeDecayRate = 7f; // % per second
        public const float AttributeDecayMultiplier = 5f;

        public const float PlayerEngagementMultiplierIncrease = 0.1f; // 8 seconds to go from 1.4 to 0.6
        public const float PlayerEngagementMultiplierDecay = 0.08f; // 10 seconds to go from 0.6 to 1.4
        public const float PlayerEngagementMultiplierMax = 1.4f;
        public const float PlayerEngagementMultiplierMin = 0.6f;

        public const float RandomCoefficientMin = 0.95f;
        public const float RandomCoefficientMax = 1.05f;

        public const float PlayersFacingThreshold = 0.7f;
    }
}

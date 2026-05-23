namespace _TeamFolder.JCJ.Script
{
    public static class MixamoBattleLocomotionCatalog
    {
        public const string AssetFolder = "Assets/#TeamFolder/JCJ/MixamoBattle/";
        public const string ResourcesRelativeFolder = "JCJBattleLocomotion";

        public const string UrlIdle = "https://www.mixamo.com/#/?page=1&event=Motion%2CMotionPack&query=rifle%20aiming%20idle";
        public const string UrlWalkAiming = "https://www.mixamo.com/#/?page=1&event=Motion%2CMotionPack&query=walking%20aiming%20rifle";
        public const string UrlRun = "https://www.mixamo.com/#/?page=1&event=Motion%2CMotionPack&query=rifle%20run";
        public const string UrlStop = "https://www.mixamo.com/#/?page=1&event=Motion%2CMotionPack&query=stop%20walking%20rifle";
        public const string UrlYBot = "https://www.mixamo.com/#/?page=1&event=Character&query=y%20bot";

        public const string PartyCharacterIdleAimingFbx = "party_character@Idle Aiming.fbx";
        public const string MixamoTitleIdle = "Rifle Aiming Idle";
        public const string MixamoDescIdle = "Rifle Standing Aiming Idle";
        public const string MixamoTitleWalk = "Walking";
        public const string MixamoDescWalk = "Walking While Aiming Rifle";
        public const string MixamoTitleRun = "Rifle Run";
        public const string MixamoDescRunAimed = "Running With Rifle Aimed";
        public const string MixamoTitleStop = "Stop Walking";
        public const string MixamoDescStop = "Stops Walking While Aiming Rifle";

        public static readonly string[] IdleFb =
        {
            PartyCharacterIdleAimingFbx,
            "Rifle Aiming Idle.fbx",
            "Rifle_Aiming_Idle.fbx",
            "RifleAimingIdle.fbx",
        };

        public static readonly string[] WalkFb =
        {
            "Walking.fbx",
            "Walking_Aiming_Rifle.fbx",
        };

        public static readonly string[] RunFb =
        {
            "Rifle Run.fbx",
            "Rifle_Run.fbx",
        };

        public static readonly string[] StopFb =
        {
            "Stop Walking.fbx",
            "Stop_Walking.fbx",
            "Stop Walking With Rifle.fbx",
        };

        public const string YBotFbx = "Y Bot.fbx";
    }
}

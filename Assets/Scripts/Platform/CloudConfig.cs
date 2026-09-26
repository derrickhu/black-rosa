namespace InkLine
{
    // 后端是 CloudBase 云函数 cloudfunctions/blackrosa-api，和 huahua / caizhu 同一个环境。
    // GameKey、云函数目录名、云端 GAME_KEY 环境变量三处必须一致。
    // 新建网关路由要几分钟才能从 service 域名访问，刚建时会报 INVALID_PATH。
    public static class CloudConfig
    {
        public const string GameKey = "blackrosa";
        public const string BaseUrl = "https://rosa-env-d7grf78r5dbd37323.service.tcloudbase.com";
        const string Prefix = "/" + GameKey + "-api";

        public const string LoginPath = Prefix + "/login";
        public const string PullPath = Prefix + "/save/pull";
        public const string PushPath = Prefix + "/save/push";
        public const string RankSubmitPath = Prefix + "/rank/submit";
        public const string RankListPath = Prefix + "/rank/list";
        public const int RankListLimit = 50;

        public const int RequestTimeoutSec = 10;
        public const int SchemaVersion = 1;

        // 只存本机，不上云。
        public const string TokenKey = GameKey + "_token";
        public const string AnonKey = GameKey + "_anon_id";
        public const string SyncMetaKey = GameKey + "_cloud_meta";
        public const string ProfileKey = GameKey + "_wx_profile";
        public const string RankSentKey = GameKey + "_rank_sent";

        public const float StartupTimeout = 6f;
        public const float Debounce = 1.5f;
        public const float BaseDelay = 1.5f;
        public const float MaxBackoff = 30f;
        public const int MaxFail = 5;
        public const float RetryInterval = 60f;
    }
}

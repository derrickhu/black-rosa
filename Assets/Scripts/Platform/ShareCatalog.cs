using UnityEngine;

namespace InkLine
{
    // 微信后台「游戏能力地图 - 运营素材管理 - 分享图片」里过审的图。
    // 图片编号和图片地址必须成对填写，缺一个微信就不会用这张审核图。
    // 点右上角「转发」或「分享到朋友圈」时随机抽一句。
    // 聊天卡片大约 14 个字就换行，所以都写成一句嘴欠的话，不介绍玩法。
    public static class ShareCatalog
    {
        public struct Card
        {
            public string Title;
            public string ImageUrlId;
            public string ImageUrl;
        }

        // 后台过审的两张图。编号和地址必须成对，转发时两张轮着出现。
        const string GrassId = "qqG+XiEjTjOIRZhvuzNGiw==";
        const string GrassUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ2eVic3RDvGvsbxrHK6CWSIeIsCvicKCZxBHocre0HLJmruh8S4SpfGWF/0";
        const string SandId = "uuqPNAVORjyJyLLHWmtH3A==";
        const string SandUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ0ibXUrmAVF5n0joBhR4R9ic5zfogvTUia8Xcd62KhkuEwtVBf8ex7rdBJ/0";
        // 续命转发专用：哭脸炮弹。右上角转发仍用上面两张。
        const string ReviveId = "KN7DGivERI2hceKSl4VAWw==";
        const string ReviveUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ0bduCia9LrPw11ndaudWMcXyItHTiaLOt8FtMxeXwOBgz16G5FFU45mJ/0";

        public static readonly Card[] Cards =
        {
            new Card { Title = "据说只有1%的人玩得下去", ImageUrlId = GrassId, ImageUrl = GrassUrl },
            new Card { Title = "玻璃心，别点开", ImageUrlId = SandId, ImageUrl = SandUrl },
            new Card { Title = "删了三次，又装回来了", ImageUrlId = GrassId, ImageUrl = GrassUrl },
            new Card { Title = "睡前别玩，会通宵", ImageUrlId = SandId, ImageUrl = SandUrl },
            new Card { Title = "这游戏有毒，我先说了", ImageUrlId = GrassId, ImageUrl = GrassUrl },
            new Card { Title = "我赌你撑不过三分钟", ImageUrlId = SandId, ImageUrl = SandUrl },
            new Card { Title = "你同事都在偷偷玩", ImageUrlId = GrassId, ImageUrl = GrassUrl },
        };

        // 续命转发专属话术。只在这里用，不抽右上角那套。朋友看到的是求救，不提复活。
        // 聊天卡片大约 14 个字就换行，每句都卡在一行里。
        static readonly Card[] ReviveCards =
        {
            new Card { Title = "救救我救救我！防线告急！", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
            new Card { Title = "救救我！这关我守不住了", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
            new Card { Title = "防线要破了，快来救我", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
            new Card { Title = "救救我！敌人冲上来了", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
            new Card { Title = "就差一口气，救救我！", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
            new Card { Title = "救救我救救我，要输了", ImageUrlId = ReviveId, ImageUrl = ReviveUrl },
        };

        // 游戏圈转发刷新专用。朋友看到的是炫耀今天的奖，不提刷新。
        // 聊天卡片大约 14 个字就换行，每句都卡在一行里。
        // 图必须用后台过审的编号和地址。空着时微信会截当前画面，小游戏进程会被打重启。
        const string ClubBunId = "uTuxnQgvTP2KaTcx3LWaRQ==";
        const string ClubBunUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ0IQxyj3G0LJdGWEia7UHJaVo3Ou0kaQcMEibIUJrdXYua0tcuhicYRwTT/0";
        const string ClubKingId = "gSKWEPXPTSC2db/1Z6h8iw==";
        const string ClubKingUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ3c1MtQP1urZqfHcFk2J4ejCBHcezxT38GWwxyLrG0z7iaSDV6a5gC5z/0";
        const string ClubTileId = "phShsAlrQXWwCKGuqUB7xw==";
        const string ClubTileUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ0jnBbPVImvTd420qoUaXBhCicK9zAJRiakCrHRibV76xYaO7dCcOFibp34/0";
        const string ClubChestId = "kXQesEsyS/Wo8vfklFRzQA==";
        const string ClubChestUrl = "https://mmocgame.qpic.cn/wechatgame/gkKEMSAOfJ3Hvauf7oSc2icpfX0ciczV315oP7jK4twUdt2tm9LS1zickfjalgwU5jv/0";

        static readonly string[] ClubLines =
        {
            "今天这奖，我不信",
            "金箱子自己跳出来了",
            "这波奖励也太狠了",
            "我抽中了，你敢看吗",
            "今天手气好到离谱",
            "这奖励，我先藏着",
        };

        static readonly Card[] ClubArts =
        {
            new Card { ImageUrlId = ClubBunId, ImageUrl = ClubBunUrl },
            new Card { ImageUrlId = ClubKingId, ImageUrl = ClubKingUrl },
            new Card { ImageUrlId = ClubTileId, ImageUrl = ClubTileUrl },
            new Card { ImageUrlId = ClubChestId, ImageUrl = ClubChestUrl },
        };

        public static Card Pick() => PickFrom(Cards);

        public static Card PickRevive() => PickFrom(ReviveCards);

        public static Card PickClub()
        {
            Card card = ClubArts[Random.Range(0, ClubArts.Length)];
            card.Title = ClubLines[Random.Range(0, ClubLines.Length)];
            return card;
        }

        static Card PickFrom(Card[] cards)
        {
            if (cards == null || cards.Length == 0)
                return new Card { Title = "墨字防线" };
            return cards[Random.Range(0, cards.Length)];
        }

        public static bool HasImage(Card card)
        {
            return !string.IsNullOrEmpty(card.ImageUrlId) && !string.IsNullOrEmpty(card.ImageUrl);
        }
    }
}

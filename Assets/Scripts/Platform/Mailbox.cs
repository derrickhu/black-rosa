using System;
using System.Collections;
using UnityEngine;

namespace InkLine
{
    // 邮箱。信在云上，进游戏只拉未领列表亮红点，点进去才领取。
    public static class Mailbox
    {
        public static int Pending { get; private set; }
        public static MailRow[] Rows { get; private set; } = new MailRow[0];
        public static bool Ready { get; private set; }
        public static bool Failed { get; private set; }

        static bool _busy;

        [Serializable]
        sealed class ClaimBody
        {
            public string[] ids;
        }

        public static IEnumerator Refresh(Action done)
        {
            if (_busy)
            {
                if (done != null) done();
                yield break;
            }
            _busy = true;
            CloudReply reply = null;
            string err = null;
            yield return Backend.Call(CloudConfig.MailListPath, "{}", (s, r, e) =>
            {
                reply = r;
                err = e;
            });
            Failed = err != null || reply == null || reply.data == null;
            if (!Failed)
            {
                Rows = reply.data.mails ?? new MailRow[0];
                Pending = Rows.Length;
            }
            Ready = true;
            _busy = false;
            if (done != null) done();
        }

        public static IEnumerator Claim(MetaProgress meta, string[] ids, Action<bool, GiftGrant> done)
        {
            if (meta == null || ids == null || ids.Length == 0)
            {
                if (done != null) done(false, null);
                yield break;
            }
            CloudReply reply = null;
            string err = null;
            string json = JsonUtility.ToJson(new ClaimBody { ids = ids });
            yield return Backend.Call(CloudConfig.MailClaimPath, json, (s, r, e) =>
            {
                reply = r;
                err = e;
            });
            if (err != null || reply == null || reply.data == null)
            {
                if (done != null) done(false, null);
                yield break;
            }
            MailRow[] got = reply.data.mails ?? new MailRow[0];
            int ink = 0, stamina = 0, diamond = 0, wood = 0, silver = 0, gold = 0, royal = 0, cannon = 0;
            for (int i = 0; i < got.Length; i++)
            {
                MailRow row = got[i];
                if (row == null) continue;
                ink += row.ink;
                stamina += row.stamina;
                diamond += row.diamond;
                wood += row.wood;
                silver += row.silver;
                gold += row.gold;
                royal += row.royal;
                cannon += row.cannon;
                Drop(row.id);
            }
            GiftGrant grant = null;
            if (ink > 0 || stamina > 0 || diamond > 0 || wood > 0 || silver > 0 || gold > 0 || royal > 0 || cannon > 0)
                grant = meta.GrantWechatGift(ink, stamina, diamond, wood, silver, gold, royal, cannon);
            Pending = Rows.Length;
            if (done != null) done(true, grant);
        }

        static void Drop(string id)
        {
            if (string.IsNullOrEmpty(id) || Rows == null) return;
            int n = 0;
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i] != null && Rows[i].id != id) n++;
            var next = new MailRow[n];
            int w = 0;
            for (int i = 0; i < Rows.Length; i++)
            {
                if (Rows[i] == null || Rows[i].id == id) continue;
                next[w++] = Rows[i];
            }
            Rows = next;
        }
    }
}

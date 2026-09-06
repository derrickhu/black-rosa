using System;

namespace InkLine
{
    public static class AdStub
    {
        public static void Reward(string slot, Action onOk)
        {
            // 微信激励视频下一阶段再接。MVP 直接成功，方便验循环。
            onOk?.Invoke();
        }
    }
}

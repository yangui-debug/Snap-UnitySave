using System;

namespace Playables
{
    public enum EffectPlayMode
    {
        Free,
        Clamp,
        Loop,
    }

    [Flags]
    public enum EffectMode
    {
        Both = 0, // 默认都是0
        Targets = 1,
        Positions = 2,
        Self = 4,
    }
}
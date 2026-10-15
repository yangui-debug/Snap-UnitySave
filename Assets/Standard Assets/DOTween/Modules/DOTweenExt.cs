using DG.Tweening;

namespace Functions.Components.Animations
{
    public static class DOTweenExt
    {
        public static void SafeComplete(this Tween t)
        {
            t.SafeComplete(false);
        }

        public static void SafeComplete(this Tween t, bool withCallbacks)
        {
            if (t != null && t.active)
            {
                t.Complete(withCallbacks);
            }
        }

        public static void SafeKill(this Tween t, bool complete = false)
        {
            if (t != null && t.active)
            {
                t.Kill(complete);
            }
        }
    }
}
using UnityEngine;

namespace ArtDefines
{
    public static class AnimDefines
    {
        public static readonly int Click1 = Animator.StringToHash("Click1");
        public static readonly int Click = Animator.StringToHash("Click");

        public static readonly int End = Animator.StringToHash("End");
        public static readonly int Begin = Animator.StringToHash("Begin");
        public static readonly int SitLoop = Animator.StringToHash("Sit_Loop");

        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Panic = Animator.StringToHash("Panic");
        public static readonly int Panic_Run = Animator.StringToHash("Panic_Run");
        public static readonly int Drag = Animator.StringToHash("Drag");
    }
}
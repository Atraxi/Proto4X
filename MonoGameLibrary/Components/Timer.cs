namespace MonoGameLibrary.Components
{
    public struct Timer(float duration, bool shouldRepeat)
    {
        public float Duration = duration;
        public float Remaining = duration;
        public bool ShouldRepeat = shouldRepeat;
        public bool HasJustElapsed = false;
    }
}

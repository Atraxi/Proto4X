using System;

namespace MonoGameLibrary.Events
{
    public class EventStream<T> where T : struct
    {
        private T[] published = new T[16];
        private T[] pending = new T[16];
        private int publishedCount;
        private int pendingCount;

        internal ReadOnlySpan<T> Events => new(published, 0, publishedCount);

        public void Publish(in T e)
        {
            if (pendingCount == pending.Length)
            {
                Array.Resize(ref pending, pending.Length * 2);
            }
            pending[pendingCount] = e;
            pendingCount++;
        }

        // Publishing from an event system therefore lands next frame, which bounds cascades to one hop per frame.
        public void Swap()
        {
            (published, pending) = (pending, published);
            publishedCount = pendingCount;
            pendingCount = 0;
        }
    }
}

namespace BTree
{
    using System;

    public class Entry<TK, TP> : IEquatable<Entry<TK, TP>>
    {
        public TK Key { get; set; }

        public TP Pointer { get; set; }

        public bool Equals(Entry<TK, TP> other)
        {
            return this.Key.Equals(other.Key) && this.Pointer.Equals(other.Pointer);
        }
    }
}

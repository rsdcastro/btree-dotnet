namespace BTree
{
    using System.Collections.Generic;

    public class Node<TK, TP>
    {
        private int degree;

        public Node(int degree)
        {
            this.degree = degree;
            this.Children = new List<Node<TK, TP>>(degree);
            this.Entries = new List<Entry<TK, TP>>(degree);
        }

        public List<Node<TK, TP>> Children { get; set; }

        public List<Entry<TK, TP>> Entries { get; set; }

        public bool IsLeaf
        {
            get
            {
                return this.Children.Count == 0;
            }
        }

        public bool HasReachedMaxEntries
        {
            get
            {
                return this.Entries.Count == (2 * this.degree) - 1;
            }
        }

        public bool HasReachedMinEntries
        {
            get
            {
                return this.Entries.Count == this.degree - 1;
            }
        }
    }
}

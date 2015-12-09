namespace BTree
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Based on BTree chapter in "Introduction to Algorithms", by Thomas Cormen, Charles Leiserson, Ronald Rivest.
    /// 
    /// This implementation is not thread-safe, and user must handle thread-safety.
    /// </summary>
    /// <typeparam name="TK">Type of BTree Key.</typeparam>
    /// <typeparam name="TP">Type of BTree Pointer associated with each Key.</typeparam>
    public class BTree<TK, TP> where TK : IComparable<TK>
    {
        public BTree(int degree)
        {
            if (degree < 2)
            {
                throw new ArgumentException("BTree degree must be at least 2", "degree");
            }

            this.Root = new Node<TK, TP>(degree);
            this.Degree = degree;
            this.Height = 1;
        }

        public Node<TK, TP> Root { get; private set; }

        public int Degree { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        /// Searches a key in the BTree, returning the entry with it and with the pointer.
        /// </summary>
        /// <param name="key">Key being searched.</param>
        /// <returns>Entry for that key, null otherwise.</returns>
        public Entry<TK, TP> Search(TK key)
        {
            return this.SearchInternal(this.Root, key);
        }

        /// <summary>
        /// Inserts a new key associated with a pointer in the BTree. This
        /// operation splits nodes as required to keep the BTree properties.
        /// </summary>
        /// <param name="newKey">Key to be inserted.</param>
        /// <param name="newPointer">Pointer to be associated with inserted key.</param>
        public void Insert(TK newKey, TP newPointer)
        {
            // there is space in the root node
            if (!this.Root.HasReachedMaxEntries)
            {
                this.InsertNonFull(this.Root, newKey, newPointer);
                return;
            }

            // need to create new node and have it split
            Node<TK, TP> oldRoot = this.Root;
            this.Root = new Node<TK, TP>(this.Degree);
            this.Root.Children.Add(oldRoot);
            this.SplitChild(this.Root, 0, oldRoot);
            this.InsertNonFull(this.Root, newKey, newPointer);

            this.Height++;
        }

        /// <summary>
        /// Deletes a key from the BTree. This operations moves keys and nodes
        /// as required to keep the BTree properties.
        /// </summary>
        /// <param name="keyToDelete">Key to be deleted.</param>
        public void Delete(TK keyToDelete)
        {
            this.DeleteInternal(this.Root, keyToDelete);

            // if root's last entry was moved to a child node, remove it
            if (this.Root.Entries.Count == 0 && !this.Root.IsLeaf)
            {
                this.Root = this.Root.Children.Single();
                this.Height--;
            }
        }

        /// <summary>
        /// Internal method to delete keys from the BTree
        /// </summary>
        /// <param name="node">Node to use to start search for the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        private void DeleteInternal(Node<TK, TP> node, TK keyToDelete)
        {
            int i = node.Entries.TakeWhile(entry => keyToDelete.CompareTo(entry.Key) > 0).Count();

            // found key in node, so delete if from it
            if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(keyToDelete) == 0)
            {
                this.DeleteKeyFromNode(node, keyToDelete, i);
                return;
            }

            // delete key from subtree
            if (!node.IsLeaf)
            {
                this.DeleteKeyFromSubtree(node, keyToDelete, i);
            }
        }

        /// <summary>
        /// Helper method that deletes a key from a subtree.
        /// </summary>
        /// <param name="parentNode">Parent node used to start search for the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        /// <param name="subtreeIndexInNode">Index of subtree node in the parent node.</param>
        private void DeleteKeyFromSubtree(Node<TK, TP> parentNode, TK keyToDelete, int subtreeIndexInNode)
        {
            Node<TK, TP> childNode = parentNode.Children[subtreeIndexInNode];

            // node has reached min # of entries, and removing any from it will break the btree property,
            // so this block makes sure that the "child" has at least "degree" # of nodes by moving an 
            // entry from a sibling node or merging nodes
            if (childNode.HasReachedMinEntries)
            {
                int leftIndex = subtreeIndexInNode - 1;
                Node<TK, TP> leftSibling = subtreeIndexInNode > 0 ? parentNode.Children[leftIndex] : null;

                int rightIndex = subtreeIndexInNode + 1;
                Node<TK, TP> rightSibling = subtreeIndexInNode < parentNode.Children.Count - 1
                                                ? parentNode.Children[rightIndex]
                                                : null;
                
                if (leftSibling != null && leftSibling.Entries.Count > this.Degree - 1)
                {
                    // left sibling has a node to spare, so this moves one node from left sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    childNode.Entries.Insert(0, parentNode.Entries[subtreeIndexInNode - 1]);
                    parentNode.Entries[subtreeIndexInNode - 1] = leftSibling.Entries.Last();
                    leftSibling.Entries.RemoveAt(leftSibling.Entries.Count - 1);

                    if (!leftSibling.IsLeaf)
                    {
                        childNode.Children.Insert(0, leftSibling.Children.Last());
                        leftSibling.Children.RemoveAt(leftSibling.Children.Count - 1);
                    }
                }
                else if (rightSibling != null && rightSibling.Entries.Count > this.Degree - 1)
                {
                    // right sibling has a node to spare, so this moves one node from right sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    childNode.Entries.Add(parentNode.Entries[subtreeIndexInNode]);
                    parentNode.Entries[subtreeIndexInNode] = rightSibling.Entries.First();
                    rightSibling.Entries.RemoveAt(0);

                    if (!rightSibling.IsLeaf)
                    {
                        childNode.Children.Add(rightSibling.Children.First());
                        rightSibling.Children.RemoveAt(0);
                    }
                }
                else
                {
                    // this block merges either left or right sibling into the current node "child"
                    if (leftSibling != null)
                    {
                        childNode.Entries.Insert(0, parentNode.Entries[subtreeIndexInNode - 1]);
                        var oldEntries = childNode.Entries;
                        childNode.Entries = leftSibling.Entries;
                        childNode.Entries.AddRange(oldEntries);
                        if (!leftSibling.IsLeaf)
                        {
                            var oldChildren = childNode.Children;
                            childNode.Children = leftSibling.Children;
                            childNode.Children.AddRange(oldChildren);
                        }

                        parentNode.Children.RemoveAt(leftIndex);
                        parentNode.Entries.RemoveAt(subtreeIndexInNode - 1);
                    }
                    else
                    {
                        Debug.Assert(rightSibling != null, "Node should have at least one sibling");
                        childNode.Entries.Add(parentNode.Entries[subtreeIndexInNode]);
                        childNode.Entries.AddRange(rightSibling.Entries);
                        if (!rightSibling.IsLeaf)
                        {
                            childNode.Children.AddRange(rightSibling.Children);
                        }

                        parentNode.Children.RemoveAt(rightIndex);
                        parentNode.Entries.RemoveAt(subtreeIndexInNode);
                    }
                }
            }

            // at this point, we know that "child" has at least "degree" nodes, so we can
            // move on - this guarantees that if any node needs to be removed from it to
            // guarantee BTree's property, we will be fine with that
            this.DeleteInternal(childNode, keyToDelete);
        }
        
        /// <summary>
        /// Helper method that deletes key from a node that contains it, be this
        /// node a leaf node or an internal node.
        /// </summary>
        /// <param name="node">Node that contains the key.</param>
        /// <param name="keyToDelete">Key to be deleted.</param>
        /// <param name="keyIndexInNode">Index of key within the node.</param>
        private void DeleteKeyFromNode(Node<TK, TP> node, TK keyToDelete, int keyIndexInNode)
        {
            // if leaf, just remove it from the list of entries (we're guaranteed to have
            // at least "degree" # of entries, to BTree property is maintained
            if (node.IsLeaf)
            {
                node.Entries.RemoveAt(keyIndexInNode);
                return;
            }

            Node<TK, TP> predecessorChild = node.Children[keyIndexInNode];
            if (predecessorChild.Entries.Count >= this.Degree)
            {
                Entry<TK, TP> predecessorEntry = this.GetLastEntry(predecessorChild);
                this.DeleteInternal(predecessorChild, predecessorEntry.Key);
                node.Entries[keyIndexInNode] = predecessorEntry;
            }
            else
            {
                Node<TK, TP> successorChild = node.Children[keyIndexInNode + 1];
                if (successorChild.Entries.Count >= this.Degree)
                {
                    Entry<TK, TP> successorEntry = this.GetFirstEntry(successorChild);
                    this.DeleteInternal(successorChild, successorEntry.Key);
                    node.Entries[keyIndexInNode] = successorEntry;
                }
                else
                {
                    predecessorChild.Entries.Add(node.Entries[keyIndexInNode]);
                    predecessorChild.Entries.AddRange(successorChild.Entries);
                    predecessorChild.Children.AddRange(successorChild.Children);

                    node.Entries.RemoveAt(keyIndexInNode);
                    node.Children.RemoveAt(keyIndexInNode + 1);

                    this.DeleteInternal(predecessorChild, keyToDelete);
                }
            }
        }

        /// <summary>
        /// Helper method that gets the last entry (i.e. rightmost key) for a given node.
        /// </summary>
        private Entry<TK, TP> GetLastEntry (Node<TK, TP> node)
        {
            if (node.IsLeaf) {
                return node.Entries.Last ();
            }

            return this.GetLastEntry (
                node.Children.Last ()
            );
        }

        /// <summary>
        /// Helper method that gets the first entry (i.e. leftmost key) for a given node.
        /// </summary>
        private Entry<TK, TP> GetFirstEntry (Node<TK, TP> node)
        {
            if (node.IsLeaf) {
                return node.Entries.First ();
            }

            return this.GetFirstEntry (
                node.Children.First ()
            );
        }

        /// <summary>
        /// Helper method that search for a key in a given BTree.
        /// </summary>
        /// <param name="node">Node used to start the search.</param>
        /// <param name="key">Key to be searched.</param>
        /// <returns>Entry object with key information if found, null otherwise.</returns>
        private Entry<TK, TP> SearchInternal(Node<TK, TP> node, TK key)
        {
            int i = node.Entries.TakeWhile(entry => key.CompareTo(entry.Key) > 0).Count();

            if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(key) == 0)
            {
                return node.Entries[i];
            }

            return node.IsLeaf ? null : this.SearchInternal(node.Children[i], key);
        }

        /// <summary>
        /// Helper method that splits a full node into two nodes.
        /// </summary>
        /// <param name="parentNode">Parent node that contains node to be split.</param>
        /// <param name="nodeToBeSplitIndex">Index of the node to be split within parent.</param>
        /// <param name="nodeToBeSplit">Node to be split.</param>
        private void SplitChild(Node<TK, TP> parentNode, int nodeToBeSplitIndex, Node<TK, TP> nodeToBeSplit)
        {
            var newNode = new Node<TK, TP>(this.Degree);

            parentNode.Entries.Insert(nodeToBeSplitIndex, nodeToBeSplit.Entries[this.Degree - 1]);
            parentNode.Children.Insert(nodeToBeSplitIndex + 1, newNode);

            newNode.Entries.AddRange(nodeToBeSplit.Entries.GetRange(this.Degree, this.Degree - 1));
            
            // remove also Entries[this.Degree - 1], which is the one to move up to the parent
            nodeToBeSplit.Entries.RemoveRange(this.Degree - 1, this.Degree);

            if (!nodeToBeSplit.IsLeaf)
            {
                newNode.Children.AddRange(nodeToBeSplit.Children.GetRange(this.Degree, this.Degree));
                nodeToBeSplit.Children.RemoveRange(this.Degree, this.Degree);
            }
        }

        private void InsertNonFull(Node<TK, TP> node, TK newKey, TP newPointer)
        {
            int positionToInsert = node.Entries.TakeWhile(entry => newKey.CompareTo(entry.Key) >= 0).Count();

            // leaf node
            if (node.IsLeaf)
            {
                node.Entries.Insert(positionToInsert, new Entry<TK, TP>() { Key = newKey, Pointer = newPointer });
                return;
            }

            // non-leaf
            Node<TK, TP> child = node.Children[positionToInsert];
            if (child.HasReachedMaxEntries)
            {
                this.SplitChild(node, positionToInsert, child);
                if (newKey.CompareTo(node.Entries[positionToInsert].Key) > 0)
                {
                    positionToInsert++;
                }
            }

            this.InsertNonFull(node.Children[positionToInsert], newKey, newPointer);
        }
    }
}

namespace BTree
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Based on BTree chapter in "Introduction to Algorithms", by Thomas Cormen, Charles Leiserson, Ronald Rivest
    /// </summary>
    /// <typeparam name="TK">Type of BTree Key</typeparam>
    /// <typeparam name="TP">Type of BTree Pointer associated with each Key</typeparam>
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
        }

        public Node<TK, TP> Root { get; private set; }

        public int Degree { get; private set; }

        public Entry<TK, TP> Search(TK key)
        {
            return this.SearchInternal(this.Root, key);
        }

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
        }

        public void Delete(TK keyToDelete)
        {
            this.DeleteInternal(this.Root, keyToDelete);

            // if root's last entry was moved to a child node, remove it
            if (this.Root.Entries.Count == 0 && !this.Root.IsLeaf)
            {
                this.Root = this.Root.Children.Single();
            }
        }

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
            this.DeleteKeyFromSubtree(node, keyToDelete, i);
        }

        private void DeleteKeyFromSubtree(Node<TK, TP> node, TK keyToDelete, int subtreeIndexInNode)
        {
            Node<TK, TP> child = node.Children[subtreeIndexInNode];

            // node has reached min # of entries, and removing any from it will break the btree property,
            // so this block makes sure that the "child" has at least "degree" # of nodes
            if (child.HasReachedMinEntries)
            {
                int leftIndex = subtreeIndexInNode - 1;
                Node<TK, TP> leftSibling = subtreeIndexInNode > 0 ? node.Children[leftIndex] : null;

                int rightIndex = subtreeIndexInNode + 1;
                Node<TK, TP> rightSibling = subtreeIndexInNode < node.Children.Count - 1
                                                ? node.Children[rightIndex]
                                                : null;
                
                if (leftSibling != null && leftSibling.Entries.Count > this.Degree - 1)
                {
                    // left sibling has a node to spare, so this moves one node from left sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    child.Entries.Insert(0, node.Entries[subtreeIndexInNode]);
                    node.Entries[subtreeIndexInNode] = leftSibling.Entries.Last();
                    leftSibling.Entries.RemoveAt(leftSibling.Entries.Count - 1);

                    if (!leftSibling.IsLeaf)
                    {
                        child.Children.Insert(0, leftSibling.Children.Last());
                        leftSibling.Children.RemoveAt(leftSibling.Children.Count - 1);
                    }
                }
                else if (rightSibling != null && rightSibling.Entries.Count > this.Degree - 1)
                {
                    // right sibling has a node to spare, so this moves one node from right sibling 
                    // into parent's node and one node from parent into this current node ("child")
                    child.Entries.Add(node.Entries[subtreeIndexInNode]);
                    node.Entries[subtreeIndexInNode] = rightSibling.Entries.First();
                    rightSibling.Entries.RemoveAt(0);

                    if (!rightSibling.IsLeaf)
                    {
                        child.Children.Add(rightSibling.Children.First());
                        rightSibling.Children.RemoveAt(0);
                    }
                }
                else
                {
                    // this block merges either left or right sibling into the current node "child"
                    if (leftSibling != null)
                    {
                        child.Entries.Insert(0, node.Entries[subtreeIndexInNode]);
                        var oldEntries = child.Entries;
                        child.Entries = leftSibling.Entries;
                        child.Entries.AddRange(oldEntries);
                        if (!leftSibling.IsLeaf)
                        {
                            var oldChildren = child.Children;
                            child.Children = leftSibling.Children;
                            child.Children.AddRange(oldChildren);
                        }

                        node.Children.RemoveAt(leftIndex);
                        node.Entries.RemoveAt(subtreeIndexInNode);
                    }
                    else
                    {
                        Debug.Assert(rightSibling != null, "Node should have at least one sibling");
                        child.Entries.Add(node.Entries[subtreeIndexInNode]);
                        child.Entries.AddRange(rightSibling.Entries);
                        if (!rightSibling.IsLeaf)
                        {
                            child.Children.AddRange(rightSibling.Children);
                        }

                        node.Children.RemoveAt(rightIndex);
                        node.Entries.RemoveAt(subtreeIndexInNode);
                    }
                }
            }

            // at this point, we know that "child" has at least "degree" nodes, so we can
            // move on - this guarantees that if any node needs to be removed from it to
            // guarantee BTree's property, we will be fine with that
            this.DeleteInternal(child, keyToDelete);
        }

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
                Entry<TK, TP> predecessor = this.DeletePredecessor(predecessorChild);
                node.Entries[keyIndexInNode] = predecessor;
            }
            else
            {
                Node<TK, TP> successorChild = node.Children[keyIndexInNode + 1];
                if (successorChild.Entries.Count >= this.Degree)
                {
                    Entry<TK, TP> successor = this.DeleteSuccessor(predecessorChild);
                    node.Entries[keyIndexInNode] = successor;
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

        private Entry<TK, TP> DeletePredecessor(Node<TK, TP> node)
        {
            if (node.IsLeaf)
            {
                var result = node.Entries[node.Entries.Count - 1];
                node.Entries.RemoveAt(node.Entries.Count - 1);
                return result;
            }

            return this.DeletePredecessor(node.Children.Last());
        }

        private Entry<TK, TP> DeleteSuccessor(Node<TK, TP> node)
        {
            if (node.IsLeaf)
            {
                var result = node.Entries[0];
                node.Entries.RemoveAt(0);
                return result;
            }

            return this.DeletePredecessor(node.Children.First());
        }

        private Entry<TK, TP> SearchInternal(Node<TK, TP> node, TK key)
        {
            int i = node.Entries.TakeWhile(entry => key.CompareTo(entry.Key) > 0).Count();

            if (i < node.Entries.Count && node.Entries[i].Key.CompareTo(key) == 0)
            {
                return node.Entries[i];
            }

            return node.IsLeaf ? null : this.SearchInternal(node.Children[i], key);
        }

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

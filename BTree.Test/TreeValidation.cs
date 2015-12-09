using NUnit.Framework;

namespace BTree.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;
	    
    public static class TreeValidation
    {
        public static void ValidateTree(Node<int, int> tree, int degree, params int[] expectedKeys)
        {
            var foundKeys = new Dictionary<int, List<Entry<int, int>>>();
            ValidateSubtree(tree, tree, degree, int.MinValue, int.MaxValue, foundKeys);

            Assert.AreEqual(0, expectedKeys.Except(foundKeys.Keys).Count());
            foreach (var keyValuePair in foundKeys)
            {
                Assert.AreEqual(1, keyValuePair.Value.Count);
            }
        }

        private static void UpdateFoundKeys(Dictionary<int, List<Entry<int, int>>> foundKeys, Entry<int, int> entry)
        {
            List<Entry<int, int>> foundEntries;
            if (!foundKeys.TryGetValue(entry.Key, out foundEntries))
            {
                foundEntries = new List<Entry<int, int>>();
                foundKeys.Add(entry.Key, foundEntries);
            }

            foundEntries.Add(entry);
        }

        private static void ValidateSubtree(Node<int, int> root, Node<int, int> node, int degree, int nodeMin, int nodeMax, Dictionary<int, List<Entry<int, int>>> foundKeys)
        {
            if (root != node)
            {
                Assert.IsTrue(node.Entries.Count >= degree - 1);
                Assert.IsTrue(node.Entries.Count <= (2 * degree) - 1);
            }

            for (int i = 0; i <= node.Entries.Count; i++)
            {
                int subtreeMin = nodeMin;
                int subtreeMax = nodeMax;

                if (i < node.Entries.Count)
                {
                    var entry = node.Entries[i];
                    UpdateFoundKeys(foundKeys, entry);
                    Assert.IsTrue(entry.Key >= nodeMin && entry.Key <= nodeMax);

                    subtreeMax = entry.Key;
                }

                if (i > 0)
                {
                    subtreeMin = node.Entries[i - 1].Key;
                }

                if (!node.IsLeaf)
                {
                    Assert.IsTrue(node.Children.Count >= degree);
                    ValidateSubtree(root, node.Children[i], degree, subtreeMin, subtreeMax, foundKeys);
                }
            }
        }
    }
}

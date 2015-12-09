using NUnit.Framework;
using System;

namespace BTree.UnitTest
{
    using System.Linq;

    [TestFixture]
    public class BTreeTest
    {
        private const int Degree = 2;

        private readonly int[] testKeyData = new int[] { 10, 20, 30, 50 };
        private readonly int[] testPointerData = new int[] { 50, 60, 40, 20 };

        [Test]
        public void CreateBTree()
        {
            var btree = new BTree<int, int>(Degree);

            Node<int, int> root = btree.Root;
            Assert.IsNotNull(root);
            Assert.IsNotNull(root.Entries);
            Assert.IsNotNull(root.Children);
            Assert.AreEqual(0, root.Entries.Count);
            Assert.AreEqual(0, root.Children.Count);
        }

        [Test]
        public void InsertOneNode()
        {
            var btree = new BTree<int, int>(Degree);
            this.InsertTestDataAndValidateTree(btree, 0);
            Assert.AreEqual(1, btree.Height);
        }

        [Test]
        public void InsertMultipleNodesToSplit()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestDataAndValidateTree(btree, i);
            }

            Assert.AreEqual(2, btree.Height);
        }

        [Test]
        public void DeleteNodes()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
            }

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                btree.Delete(this.testKeyData[i]);
                TreeValidation.ValidateTree(btree.Root, Degree, this.testKeyData.Skip(i + 1).ToArray());
            }

            Assert.AreEqual(1, btree.Height);
        }

        [Test]
        public void DeleteNodeBackwards()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
            }

            for (int i = this.testKeyData.Length - 1; i > 0; i--)
            {
                btree.Delete(this.testKeyData[i]);
                TreeValidation.ValidateTree(btree.Root, Degree, this.testKeyData.Take(i).ToArray());
            }

            Assert.AreEqual(1, btree.Height);
        }

        [Test]
        public void DeleteNonExistingNode()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
            }

            btree.Delete(99999);
            TreeValidation.ValidateTree(btree.Root, Degree, this.testKeyData.ToArray());
        }

        [Test]
        public void SearchNodes()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
                this.SearchTestData(btree, i);
            }
        }

        [Test]
        public void SearchNonExistingNode()
        {
            var btree = new BTree<int, int>(Degree);

            // search an empty tree
            Entry<int, int> nonExisting = btree.Search(9999);
            Assert.IsNull(nonExisting);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
                this.SearchTestData(btree, i);
            }

            // search a populated tree
            nonExisting = btree.Search(9999);
            Assert.IsNull(nonExisting);
        }

		[Test]
		public void RemoveFromLastWhichReachedMin()
		{
			var btree = new BTree<int, int>(Degree);
			var leftNode = new Node<int, int>(Degree);
			var rightNode = new Node<int, int>(Degree);

			btree.Root.Children.Add(leftNode);
			btree.Root.Children.Add(rightNode);

			leftNode.Entries.Add(new Entry<int, int> (){ Key = 1, Pointer = 1 });
			leftNode.Entries.Add(new Entry<int, int> (){ Key = 2, Pointer = 2 });
			btree.Root.Entries.Add(new Entry<int, int> (){ Key = 3, Pointer = 3 });
			rightNode.Entries.Add(new Entry<int, int> (){ Key = 4, Pointer = 4 });
			btree.Delete(4);
		}

        [Test]
        public void BruteForceTest ()
        {
            for (int i = 0; i < 100; i++) {
                RunBruteForce ();
            }
        }

        public void RunBruteForce ()
        {
            var degree = 2;

            var btree = new BTree<string, int> (degree);

            var rand = new Random ();
            for (int i = 0; i < 1000; i++) {
                var value = (int)rand.Next () % 100;
                var key = value.ToString ();

                if (rand.Next () % 2 == 0) {
                    if (btree.Search (key) == null) {
                        btree.Insert (key, value);
                    }
                    Assert.AreEqual (value, btree.Search (key).Pointer);
                } else {
                    btree.Delete (key);
                    Assert.IsNull (btree.Search (key));
                }
                CheckNode (btree.Root, degree);
            }
        }

        private void CheckNode (Node<string, int> node, int degree)
        {
            if (node.Children.Count > 0 &&
            node.Children.Count != node.Entries.Count + 1) {
                Assert.Fail ("There are children, but they don't match the number of entries.");
            }

            if (node.Entries.Count > (2 * degree) - 1) {
                Assert.Fail ("Too much entries in node");
            }

            if (node.Children.Count > degree * 2) {
                Assert.Fail ("Too much children in node");
            }

            foreach (var child in node.Children) {
                CheckNode (child, degree);
            }
        }

        #region Private Helper Methods
        private void InsertTestData(BTree<int, int> btree, int testDataIndex)
        {
            btree.Insert(this.testKeyData[testDataIndex], this.testPointerData[testDataIndex]);
        }

        private void InsertTestDataAndValidateTree(BTree<int, int> btree, int testDataIndex)
        {
            btree.Insert(this.testKeyData[testDataIndex], this.testPointerData[testDataIndex]);
            TreeValidation.ValidateTree(btree.Root, Degree, this.testKeyData.Take(testDataIndex + 1).ToArray());
        }

        private void SearchTestData(BTree<int, int> btree, int testKeyDataIndex)
        {
            for (int i = 0; i <= testKeyDataIndex; i++)
            {
                Entry<int, int> entry = btree.Search(this.testKeyData[i]);
                Assert.IsNotNull(this.testKeyData[i]);
                Assert.AreEqual(this.testKeyData[i], entry.Key);
                Assert.AreEqual(this.testPointerData[i], entry.Pointer);
            }
        }

        #endregion
    }
}

namespace BTree.UnitTest
{
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BTreeTest
    {
        private const int Degree = 2;

        private readonly int[] testKeyData = new int[] { 10, 20, 30, 50 };
        private readonly int[] testPointerData = new int[] { 50, 60, 40, 20 };

        [TestMethod]
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

        [TestMethod]
        public void InsertOneNode()
        {
            var btree = new BTree<int, int>(Degree);
            this.InsertTestDataAndValidateTree(btree, 0);
        }

        [TestMethod]
        public void InsertMultipleNodesToSplit()
        {
            var btree = new BTree<int, int>(Degree);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestDataAndValidateTree(btree, i);
            }
        }

        [TestMethod]
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
        }

        [TestMethod]
        public void SearchNodes()
        {
            var btree = new BTree<int, int>(Degree);

            const int NewKey1 = 10;
            const int NewPointer1 = 50;
            btree.Insert(NewKey1, NewPointer1);

            for (int i = 0; i < this.testKeyData.Length; i++)
            {
                this.InsertTestData(btree, i);
                this.SearchTestData(btree, i);
            }

            Entry<int, int> nonExisting = btree.Search(9999);
            Assert.IsNull(nonExisting);
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

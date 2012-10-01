Simple BTree implemention in C#, developed and tested in Visual Studio 2012.

This is an in-memory implementation currently and does not have its state persisted to disk yet.

Since this BTree implementation has Database in mind, each key added to the key has a correspondent pointer (which would be a pointer to a disk block).

-- Details
This implementation is based on the chapter on BTrees in "Introduction to Algorithms", by Thomas Cormen, Charles Leiserson, Ronald Rivest.

It provides these operations on a BTree:
- Search key
- Insert key/pointer
- Delete key

Projects:
- BTree: main project
- BTree.UnitTest: unit tests

How to use it:
- Take a look at BTree.UnitTest for examples on how to use it
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S3Append.Extensions;
using System;
using System.Collections.Generic;

namespace Tests.Unit.Extensions
{
    [TestClass]
    public class S3ClientExtensionsTest
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNoParts()
        {
            // setup
            var tooSmall = S3ClientExtensions.PART_MIN_BYTES - 1;

            // act
            S3ClientExtensions.GetPartRanges(tooSmall, S3ClientExtensions.PART_MAX_BYTES);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSmallParts()
        {
            // setup
            var tooSmall = S3ClientExtensions.PART_MIN_BYTES - 1;

            // act
            S3ClientExtensions.GetPartRanges(tooSmall, 2 * S3ClientExtensions.PART_MIN_BYTES - 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBigParts()
        {
            // setup
            var tooSmall = S3ClientExtensions.PART_MIN_BYTES - 1;

            // act
            S3ClientExtensions.GetPartRanges(tooSmall, S3ClientExtensions.PART_MAX_BYTES + 1);
        }

        [TestMethod]
        public void TestLowerSinglePart()
        {
            // setup 
            var justRight = S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>
            {
                Tuple.Create(0L, justRight - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(justRight, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestUpperSinglePart()
        {
            // setup 
            var stillRight = S3ClientExtensions.PART_MAX_BYTES;
            var expResult = new List<Tuple<long, long>>
            {
                Tuple.Create(0L, stillRight - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(stillRight, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestLowerTwoParts()
        {
            // setup 
            var justWrong = S3ClientExtensions.PART_MAX_BYTES + 1;
            var boundary = S3ClientExtensions.PART_MAX_BYTES - S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>
            {
                Tuple.Create(0L, boundary - 1),
                Tuple.Create(boundary, justWrong - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(justWrong, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }

        [TestMethod]
        public void TestTwoParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_MAX_BYTES + S3ClientExtensions.PART_MIN_BYTES - 1;
            var boundary = S3ClientExtensions.PART_MAX_BYTES - S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, boundary - 1),
                Tuple.Create(boundary, expSize - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }

        [TestMethod]
        public void TestUpperTwoParts()
        {
            // setup
            var expSize = 2 * S3ClientExtensions.PART_MAX_BYTES;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_MAX_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_MAX_BYTES, expSize - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }


        [TestMethod]
        public void TestLowerThreeParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_MAX_BYTES * 2 + 1;
            var boundary = S3ClientExtensions.PART_MAX_BYTES * 2 - S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_MAX_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_MAX_BYTES, boundary - 1),
                Tuple.Create(boundary, expSize - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[2].Item2 - result[2].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }

        [TestMethod]
        public void TestThreeParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_MAX_BYTES * 2 + S3ClientExtensions.PART_MIN_BYTES - 1;
            var boundary = S3ClientExtensions.PART_MAX_BYTES * 2 - S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_MAX_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_MAX_BYTES, boundary - 1),
                Tuple.Create(boundary, expSize - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[2].Item2 - result[2].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }

        [TestMethod]
        public void TestUpperThreeParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_MAX_BYTES * 3;
            var boundary = S3ClientExtensions.PART_MAX_BYTES * 2;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_MAX_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_MAX_BYTES, boundary - 1),
                Tuple.Create(boundary, expSize - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize, S3ClientExtensions.PART_MAX_BYTES);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
            Assert.IsTrue(result[0].Item2 - result[0].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[1].Item2 - result[1].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
            Assert.IsTrue(result[2].Item2 - result[2].Item1 >= S3ClientExtensions.PART_MIN_BYTES - 1);
        }
    }
}

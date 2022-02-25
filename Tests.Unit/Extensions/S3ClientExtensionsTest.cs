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
            S3ClientExtensions.GetPartRanges(tooSmall);
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
            var result = S3ClientExtensions.GetPartRanges(justRight);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestUpperSinglePart()
        {
            // setup 
            var justWrong = S3ClientExtensions.PART_OPT_BYTES + S3ClientExtensions.PART_MIN_BYTES - 1;
            var expResult = new List<Tuple<long, long>>
            {
                Tuple.Create(0L, justWrong - 1)
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(justWrong);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestLowerTwoParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_OPT_BYTES + S3ClientExtensions.PART_MIN_BYTES;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_OPT_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_OPT_BYTES, expSize - 1),
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestTwoParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_OPT_BYTES * 2;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_OPT_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_OPT_BYTES, expSize - 1),
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }

        [TestMethod]
        public void TestUpperTwoParts()
        {
            // setup
            var expSize = S3ClientExtensions.PART_OPT_BYTES * 2 + S3ClientExtensions.PART_MIN_BYTES - 1;
            var expResult = new List<Tuple<long, long>>()
            {
                Tuple.Create(0L, S3ClientExtensions.PART_OPT_BYTES - 1),
                Tuple.Create(S3ClientExtensions.PART_OPT_BYTES, expSize - 1),
            };

            // act
            var result = S3ClientExtensions.GetPartRanges(expSize);

            // assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expResult, result);
        }
    }
}

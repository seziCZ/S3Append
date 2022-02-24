using Microsoft.VisualStudio.TestTools.UnitTesting;
using S3Append.Models;
using System;
using System.IO;
using System.Text;

namespace Tests.Unit.Models
{
    [TestClass]
    public class AppendObjectRequestTest
    {

        [TestMethod]
        public void TestAppendStream()
        {
            // setup
            var prefixBytes = Encoding.UTF8.GetBytes("Hello ");
            var prefixStream = new MemoryStream(prefixBytes);

            var suffixBytes = Encoding.UTF8.GetBytes("world!");
            var suffixStream = new MemoryStream(suffixBytes);

            var request = new AppendObjectRequest
            {
                BucketName = "9cecab7c-72a9-4584-889c-70dc985ca992",
                Key = "2a6a2387-6693-4639-930e-cc48f8fade49",
                InputStream = suffixStream
            };

            // act
            var result = request.ToPutObjectRequest(prefixStream);
            var stream = (MemoryStream)result.InputStream;
            var message = Encoding.UTF8.GetString(stream.ToArray());

            // assert
            Assert.AreEqual("Hello world!", message);
        }

        [TestMethod]
        public void TestAppendContent()
        {
            // setup
            var prefixBytes = Encoding.UTF8.GetBytes("Hello ");
            var prefixStream = new MemoryStream(prefixBytes);

            var request = new AppendObjectRequest
            {
                BucketName = "9cecab7c-72a9-4584-889c-70dc985ca992",
                Key = "2a6a2387-6693-4639-930e-cc48f8fade49",
                ContentBody = "world!"
            };

            // act
            var result = request.ToPutObjectRequest(prefixStream);
            var stream = (MemoryStream)result.InputStream;
            var message = Encoding.UTF8.GetString(stream.ToArray());

            // assert
            Assert.AreEqual("Hello world!", message);
            Assert.IsNull(result.ContentBody);
        }

        [TestMethod]
        public void TestUploadStream()
        {
            // setup
            var suffixBytes = Encoding.UTF8.GetBytes("world!");
            var suffixStream = new MemoryStream(suffixBytes);

            var request = new AppendObjectRequest
            {
                BucketName = "9cecab7c-72a9-4584-889c-70dc985ca992",
                Key = "2a6a2387-6693-4639-930e-cc48f8fade49",
                InputStream = suffixStream
            };

            // act
            var result = request.ToUploadPartRequest(String.Empty, 0);
            var stream = (MemoryStream)result.InputStream;
            var message = Encoding.UTF8.GetString(stream.ToArray());

            // assert
            Assert.AreEqual("world!", message);
        }

        [TestMethod]
        public void TestUploadContent()
        {
            // setup
            var request = new AppendObjectRequest
            {
                BucketName = "9cecab7c-72a9-4584-889c-70dc985ca992",
                Key = "2a6a2387-6693-4639-930e-cc48f8fade49",
                ContentBody = "world!"
            };

            // act
            var result = request.ToUploadPartRequest(String.Empty, 0);
            var stream = (MemoryStream)result.InputStream;
            var message = Encoding.UTF8.GetString(stream.ToArray());

            // assert
            Assert.AreEqual("world!", message);
            Assert.IsNull(request.ContentBody);
        }
    }
}

using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using S3Append.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Unit.Models
{
    [TestClass]
    public class AppendObjectResponseTest
    {

        [TestMethod]
        public void TestComposeResponse()
        {
            // setup
            var first = new AmazonWebServiceResponse { HttpStatusCode = HttpStatusCode.OK };
            var second = new AmazonWebServiceResponse { HttpStatusCode = HttpStatusCode.Accepted };

            // act
            var result = new AppendObjectResponse(first, second);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.ResponseHistory.Count);
            Assert.AreEqual(HttpStatusCode.Accepted, result.HttpStatusCode);
            Assert.AreEqual(HttpStatusCode.OK, result.ResponseHistory[0].HttpStatusCode);
        }
    }
}

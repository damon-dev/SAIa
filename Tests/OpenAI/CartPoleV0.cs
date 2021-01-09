using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAI.SDK;
using System;

namespace Tests.OpenAI
{
    [TestClass]
    public class CartPoleV0
    {
        [TestMethod]
        public void APIServiceTest()
        {
            var client = new ApiService();

            try
            {
                client.Test().GetAwaiter().GetResult();
            }
            catch(Exception)
            {
                Assert.Fail();
            }
        }
    }
}

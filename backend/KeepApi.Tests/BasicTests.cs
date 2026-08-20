using NUnit.Framework;

namespace KeepApi.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void WrongTest()
        {
            NUnit.Framework.Assert.That(false, Is.False);
        }
    }
}

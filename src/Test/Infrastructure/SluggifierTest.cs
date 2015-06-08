namespace Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Otter;

    [TestClass]
    public class SluggifierTest
    {
        [TestMethod]
        public void GenerateSlug_Null()
        {
            string slug = Sluggifier.GenerateSlug(null);
            Assert.AreEqual(string.Empty, slug);
        }

        [TestMethod]
        public void GenerateSlug_Identity()
        {
            string slug = Sluggifier.GenerateSlug("test");
            Assert.AreEqual("test", slug);
        }

        [TestMethod]
        public void GenerateSlug_Lowercase()
        {
            string slug = Sluggifier.GenerateSlug("Test");
            Assert.AreEqual("test", slug);
        }

        [TestMethod]
        public void GenerateSlug_NormalizeUnicodeDropAccents()
        {
            string slug = Sluggifier.GenerateSlug("tést");
            Assert.AreEqual("test", slug);
        }

        [TestMethod]
        public void GenerateSlug_Spaces()
        {
            string slug = Sluggifier.GenerateSlug("this is a test title");
            Assert.AreEqual("this-is-a-test-title", slug);
        }

        [TestMethod]
        public void GenerateSlug_IgnoreLeadingSpace()
        {
            string slug = Sluggifier.GenerateSlug(" this is a test title");
            Assert.AreEqual("this-is-a-test-title", slug);
        }

        [TestMethod]
        public void GenerateSlug_MergeSequentialSpaces()
        {
            string slug = Sluggifier.GenerateSlug("this is a  test title");
            Assert.AreEqual("this-is-a-test-title", slug);
        }

        [TestMethod]
        public void GenerateSlug_DashesUnderscoresEtc()
        {
            string slug = Sluggifier.GenerateSlug("this-is_a-test/title--with (weird) stuff");
            Assert.AreEqual("this-is-a-test-title-with-weird-stuff", slug);
        }
        
        [TestMethod]
        public void GenerateSlug_FinalDash()
        {
            string slug = Sluggifier.GenerateSlug("test (test)");
            Assert.AreEqual("test-test", slug);
        }
    }
}

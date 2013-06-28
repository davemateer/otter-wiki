namespace Test
{
    using Otter.Domain;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting.Web;

    [TestClass]
    public class SecurityEntityTest
    {
        [TestMethod]
        public void TryParse_Null()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse(null, out result);
            Assert.IsFalse(ok);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TryParse_EmptyString()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse(string.Empty, out result);
            Assert.IsFalse(ok);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Roundtrip_UserName()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse("jim", out result);
            Assert.IsTrue(ok);
            Assert.AreEqual("jim", result.EntityId);
            Assert.AreEqual(SecurityEntityTypes.User, result.EntityType);
            Assert.AreEqual("jim", result.Name);
            Assert.AreEqual("jim", result.ToString());
        }

        [TestMethod]
        public void Roundtrip_UserNameAndId()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse("jim [jd]", out result);
            Assert.IsTrue(ok);
            Assert.AreEqual("jd", result.EntityId);
            Assert.AreEqual(SecurityEntityTypes.User, result.EntityType);
            Assert.AreEqual("jim", result.Name);
            Assert.AreEqual("jim [jd]", result.ToString());
        }

        [TestMethod]
        public void Roundtrip_GroupName()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse("admins (Group)", out result);
            Assert.IsTrue(ok);
            Assert.AreEqual("admins", result.EntityId);
            Assert.AreEqual(SecurityEntityTypes.Group, result.EntityType);
            Assert.AreEqual("admins", result.Name);
            Assert.AreEqual("admins (Group)", result.ToString());
        }

        [TestMethod]
        public void Roundtrip_GroupNameAndId()
        {
            SecurityEntity result;
            bool ok = SecurityEntity.TryParse("admin group [admins] (Group)", out result);
            Assert.IsTrue(ok);
            Assert.AreEqual("admins", result.EntityId);
            Assert.AreEqual(SecurityEntityTypes.Group, result.EntityType);
            Assert.AreEqual("admin group", result.Name);
            Assert.AreEqual("admin group [admins] (Group)", result.ToString());
        }
    }
}

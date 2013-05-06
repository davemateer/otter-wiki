namespace Test
{
    using Otter.Controllers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
    using Otter.Models;
    using Otter.Domain;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using Moq;
    using Otter.Repository;
    
    [TestClass]
    public class ArticleControllerTest
    {
        [TestMethod]
        public void PopulateSecurityRecords_ModifyAndViewEveryone()
        {
            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Everyone,
                ModifyOption = PermissionOption.Everyone
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", null);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[0].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_ModifyAndViewJustMe()
        {
            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.JustMe,
                ModifyOption = PermissionOption.JustMe
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", null);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[0].Scope);
            Assert.AreEqual("testid", actual[0].EntityId);
        }

        [TestMethod]
        public void PopulateSecurityRecords_ModifyEveryoneOverridesView()
        {
            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.JustMe,
                ModifyOption = PermissionOption.Everyone
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", null);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[0].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_SpecifyIndividual()
        {
            var securityRepository = new Mock<ISecurityRepository>(MockBehavior.Strict);
            securityRepository.Setup(r => r.Find("Test User [tuser]")).Returns(new SecurityEntity() { EntityId = "tuser", IsGroup = false, Name = "Test User" });

            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Everyone,
                ModifyOption = PermissionOption.Specified,
                ModifyAccounts = "Test User [tuser]"
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", securityRepository.Object);
            Assert.AreEqual(2, actual.Count);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[0].Scope);
            Assert.AreEqual("tuser", actual[0].EntityId);

            Assert.AreEqual(ArticleSecurity.PermissionView, actual[1].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[1].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_SpecifyMultipleIndividuals()
        {
            var securityRepository = new Mock<ISecurityRepository>(MockBehavior.Strict);
            securityRepository.Setup(r => r.Find("Test User [tuser]")).Returns(new SecurityEntity() { EntityId = "tuser", IsGroup = false, Name = "Test User" });
            securityRepository.Setup(r => r.Find("Test User 2 [tuser2]")).Returns(new SecurityEntity() { EntityId = "tuser2", IsGroup = false, Name = "Test User 2" });

            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Everyone,
                ModifyOption = PermissionOption.Specified,
                ModifyAccounts = "Test User [tuser]; Test User 2 [tuser2]"
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", securityRepository.Object);
            Assert.AreEqual(3, actual.Count);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[0].Scope);
            Assert.AreEqual("tuser", actual[0].EntityId);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[1].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[1].Scope);
            Assert.AreEqual("tuser2", actual[1].EntityId);

            Assert.AreEqual(ArticleSecurity.PermissionView, actual[2].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[2].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_SpecifyGroup()
        {
            var securityRepository = new Mock<ISecurityRepository>(MockBehavior.Strict);
            securityRepository.Setup(r => r.Find("Test Group (Group)")).Returns(new SecurityEntity() { EntityId = "MyGroup", IsGroup = true, Name = "Test Group" });

            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Everyone,
                ModifyOption = PermissionOption.Specified,
                ModifyAccounts = "Test Group (Group)"
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", securityRepository.Object);
            Assert.AreEqual(2, actual.Count);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeGroup, actual[0].Scope);
            Assert.AreEqual("MyGroup", actual[0].EntityId);

            Assert.AreEqual(ArticleSecurity.PermissionView, actual[1].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[1].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_DoNotDuplicateUser()
        {
            var securityRepository = new Mock<ISecurityRepository>(MockBehavior.Strict);
            securityRepository.Setup(r => r.Find("Test User [tuser]")).Returns(new SecurityEntity() { EntityId = "tuser", IsGroup = false, Name = "Test User" });

            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Everyone,
                ModifyOption = PermissionOption.Specified,
                ModifyAccounts = "Test User [tuser]; Test User [tuser]"
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", securityRepository.Object);
            Assert.AreEqual(2, actual.Count);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[0].Scope);
            Assert.AreEqual("tuser", actual[0].EntityId);

            Assert.AreEqual(ArticleSecurity.PermissionView, actual[1].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeEveryone, actual[1].Scope);
        }

        [TestMethod]
        public void PopulateSecurityRecords_DoNotDuplicateViewUser()
        {
            var securityRepository = new Mock<ISecurityRepository>(MockBehavior.Strict);
            securityRepository.Setup(r => r.Find("Test User [tuser]")).Returns(new SecurityEntity() { EntityId = "tuser", IsGroup = false, Name = "Test User" });

            PermissionModel model = new PermissionModel()
            {
                ViewOption = PermissionOption.Specified,
                ViewAccounts = "Test User [tuser]",
                ModifyOption = PermissionOption.Specified,
                ModifyAccounts = "Test User [tuser]"
            };

            List<ArticleSecurity> actual = ArticleController_Accessor.PopulateSecurityRecords(model, null, "testid", securityRepository.Object);
            Assert.AreEqual(1, actual.Count);

            Assert.AreEqual(ArticleSecurity.PermissionModify, actual[0].Permission);
            Assert.AreEqual(ArticleSecurity.ScopeIndividual, actual[0].Scope);
            Assert.AreEqual("tuser", actual[0].EntityId);
        }
    }
}

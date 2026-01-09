using System.Collections.Generic;
using NUnit.Framework;

namespace Rino.GameFramework.Core.ModuleInstaller.Tests
{
    [TestFixture]
    public class ModuleDataTests
    {
        [Test]
        public void ModuleInfo_DefaultValues_ShouldBeEmpty()
        {
            var info = new ModuleInfo();

            Assert.That(info.id, Is.Null);
            Assert.That(info.name, Is.Null);
            Assert.That(info.description, Is.Null);
            Assert.That(info.version, Is.Null);
            Assert.That(info.dependencies, Is.Not.Null);
            Assert.That(info.files, Is.Not.Null);
        }

        [Test]
        public void ModuleRuntimeData_Constructor_ShouldInitializeWithModuleInfo()
        {
            var info = new ModuleInfo
            {
                id = "test-module",
                name = "Test Module",
                description = "A test module",
                version = "1.0.0",
                dependencies = new List<string> { "dep1", "dep2" },
                files = new List<string> { "file1.cs", "file2.cs" }
            };

            var runtimeData = new ModuleRuntimeData(info);

            Assert.That(runtimeData.Info, Is.SameAs(info));
            Assert.That(runtimeData.Status, Is.EqualTo(ModuleInstallStatus.NotInstalled));
            Assert.That(runtimeData.MissingFiles, Is.Empty);
            Assert.That(runtimeData.InstalledFiles, Is.Empty);
            Assert.That(runtimeData.MissingDependencies, Is.Empty);
        }

        [Test]
        public void ModuleRuntimeData_HasUnmetDependencies_ShouldReturnFalse_WhenNoDependencies()
        {
            var info = new ModuleInfo { id = "test" };
            var runtimeData = new ModuleRuntimeData(info);

            Assert.That(runtimeData.HasUnmetDependencies, Is.False);
        }

        [Test]
        public void ModuleRuntimeData_HasUnmetDependencies_ShouldReturnTrue_WhenHasMissingDependencies()
        {
            var info = new ModuleInfo { id = "test" };
            var runtimeData = new ModuleRuntimeData(info);
            runtimeData.MissingDependencies.Add("missing-dep");

            Assert.That(runtimeData.HasUnmetDependencies, Is.True);
        }

        [Test]
        public void ModuleManifest_DefaultValues_ShouldBeEmpty()
        {
            var manifest = new ModuleManifest();

            Assert.That(manifest.version, Is.Null);
            Assert.That(manifest.baseUrl, Is.Null);
            Assert.That(manifest.modules, Is.Not.Null);
        }

        [Test]
        public void ModuleInstallStatus_ShouldHaveCorrectValues()
        {
            Assert.That((int)ModuleInstallStatus.NotInstalled, Is.EqualTo(0));
            Assert.That((int)ModuleInstallStatus.Installed, Is.EqualTo(1));
            Assert.That((int)ModuleInstallStatus.PartiallyInstalled, Is.EqualTo(2));
        }
    }
}

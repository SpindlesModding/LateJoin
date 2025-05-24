using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;

namespace CakeBuild {
	public static class Program {
		public static int Main(string[] args) {
			return new CakeHost()
				.UseContext<BuildContext>()
				.Run(args);
		}
	}
	public struct ModManifest {
		public string name;
		public string version_number;
	}

	public class BuildContext : FrostingContext {
		public const string ProjectName = "LateJoin";
		public string BuildConfiguration {
			get;
		}
		public string Version {
			get;
		}
		public string Name {
			get;
		}

		public BuildContext(ICakeContext context) : base(context) {
			BuildConfiguration = context.Argument("configuration", "Release");
			var modInfo = context.DeserializeJsonFromFile<ModManifest>($"../{ProjectName}/manifest.json");
			Version = modInfo.version_number;
			Name = modInfo.name;
		}
	}

	[TaskName("ValidateManifest")]
	public sealed class ValidateManifestTask : FrostingTask<BuildContext> {
		public override void Run(BuildContext context) {
			if (context.Version != PrivateLateJoin.Entry.modVersion)
				throw new Exception("Version for manifest and mod info does not match! Refusing to build");
		}
	}

	[TaskName("Build")]
	[IsDependentOn(typeof(ValidateManifestTask))]
	public sealed class BuildTask : FrostingTask<BuildContext> {
		public override void Run(BuildContext context) {
			context.EnsureDirectoryExists("../Releases");

			context.DotNetClean($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
				new DotNetCleanSettings {
					Configuration = context.BuildConfiguration
				});


			context.DotNetBuild($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
				new DotNetBuildSettings {
					Configuration = context.BuildConfiguration
				});
		}
	}

	[TaskName("Package")]
	[IsDependentOn(typeof(BuildTask))]
	public sealed class PackageTask : FrostingTask<BuildContext> {
		public override void Run(BuildContext context) {
			context.EnsureDirectoryExists($"../Releases/tmp");
			context.CopyFiles($"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/netstandard2.1/*", $"../Releases/tmp");
			context.DeleteFile($"../Releases/tmp/{BuildContext.ProjectName}.deps.json");

			if (!context.FileExists($"../Releases/{context.Name}-{context.Version}.zip")) {
				context.Zip($"../Releases/tmp", $"../Releases/{context.Name}-{context.Version}.zip");
			} else {
				context.Zip($"../Releases/tmp", $"../Releases/{context.Name}-{context.Version}-{DateTime.Now.ToFileTimeUtc()}.zip");
			}
		}
	}

	[TaskName("CleanTemp")]
	[IsDependentOn(typeof(PackageTask))]
	public sealed class CleanTempTask : FrostingTask<BuildContext> {
		public override void Run(BuildContext context) {
			context.EnsureDirectoryDoesNotExist($"../Releases/tmp");
		}
	}

	[TaskName("Default")]
	[IsDependentOn(typeof(CleanTempTask))]
	public class DefaultTask : FrostingTask {
	}
}
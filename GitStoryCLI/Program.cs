using ConsoleAppFramework;
using GitStory.Core;
using LibGit2Sharp;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SystemEx;

namespace GitStoryCLI
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	public class Program : ConsoleAppBase
	{
		static string dir;
		static Repository repo;

		static async Task Main(string[] args)
		{
			dir = Repository.Discover(Directory.GetCurrentDirectory());
			using (repo = new Repository(dir))
			{
				await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
			}
		}

		[Command("enable")]
		public async Task Enable(bool global = false)
		{
			repo.SetEnabled(true, global ? ConfigurationLevel.Global : ConfigurationLevel.Local);
		}

		[Command("disable")]
		public async Task Disable(bool global = false)
		{
			repo.SetEnabled(true, global ? ConfigurationLevel.Global : ConfigurationLevel.Local);
		}

		[Command("set-mode")]
		public async Task SetRepositoryMode([Option(0)]string mode, bool global = false)
		{
			if (Enum.TryParse<StoryRepositoryMode>(mode, true, out var v))
				repo.SetRepositoryMode(v, global ? ConfigurationLevel.Global : ConfigurationLevel.Local);
			else
				Console.WriteLine($"{mode} is unsopported. Try {Enum.GetValues<StoryRepositoryMode>().Select(e => e.ToString()).Join(", ")}");
		}

		[Command("fix")]
		public async Task Fix()
		{
			repo.Fix();
		}

		[Command("status")]
		public async Task Status()
		{
			try
			{
				repo.Status();
			}
			catch { }
		}

		[Command("get-uuid")]
		public async Task GetUuid()
		{
			Console.WriteLine(repo.GetUuid());
		}

		[Command("set-uuid")]
		public async Task SetUuid([Option(0)] string uuid)
		{
			repo.SetUuid(uuid);
		}

		[Command("change-uuid")]
		public async Task ChangeUuid([Option(0)] string oldUuid, [Option(1)] string newUuid = null)
		{
			repo.ChangeUuid(oldUuid, newUuid);
		}

		[Command("rename-branches")]
		public async Task RenameBranches([Option(0)] string oldPattern, [Option(1)] string newPattern = null)
		{
			repo.RenameStoryBranches(oldPattern, newPattern);
		}

		[Command("diff")]
		public async Task Diff()
		{
			repo.Diff();
		}

		public async Task Run()
		{
			try
			{
				repo.Store(repo.GetEnabled());
			}
			catch { }
		}
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

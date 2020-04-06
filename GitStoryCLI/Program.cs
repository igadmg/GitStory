using ConsoleAppFramework;
using GitStory.Core;
using LibGit2Sharp;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GitStoryCLI
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	class Program : ConsoleAppBase
	{
		static string dir;
		static Repository repo;

		static async Task Main(string[] args)
		{
			dir = Repository.Discover(Directory.GetCurrentDirectory());
			using (repo = new Repository(dir))
			{
				var s = fn(repo.GetUuid(), repo.Head, repo.Head.Tip);

				await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
			}
		}

		[Command("fix")]
		public async Task Fix()
		{
			repo.Fix();
		}

		[Command("status")]
		public async Task Status()
		{
			repo.Status();
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
		public async Task ChangeUuid([Option(0)] string oldUuid)
		{
			repo.ChangeUuid(oldUuid, string.Empty);
		}

		[Command("change-uuid")]
		public async Task ChangeUuid([Option(0)] string oldUuid, [Option(1)] string newUuid)
		{
			repo.ChangeUuid(oldUuid, newUuid);
		}

		[Command("diff")]
		public async Task Diff()
		{
			repo.Diff();
		}

		public void Run()
		{
			repo.Store();
		}
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

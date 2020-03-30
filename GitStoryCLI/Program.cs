using ConsoleAppFramework;
using GitStory.Core;
using LibGit2Sharp;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GitStoryCLI
{
	class Program : ConsoleAppBase
	{
		static async Task Main(string[] args)
		{
			string dir = Repository.Discover(Directory.GetCurrentDirectory());
			using (var repo = new Repository(dir))
			{
				await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
			}
		}

		[Command("fix")]
		public async Task Fix()
		{
			repo.Fix();
		}
				else if (args.Length == 1 && args[0] == "status")
				{
					repo.Status();
				}
				else if (args.Length == 2 && args[0] == "get-uuid")
				{
					Console.WriteLine(repo.GetUuid());
				}
				else if (args.Length == 2 && args[0] == "set-uuid")
				{
					repo.SetUuid(args[1]);
				}
				else if (args.Length >= 2 && args[0] == "change-uuid")
				{
					repo.ChangeUuid(args[1], args.Length > 2 ? args[2] : string.Empty);
				}
				else
				{
					repo.Store();
				}
		}
	}
}

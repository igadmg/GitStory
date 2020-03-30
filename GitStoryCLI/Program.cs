using ConsoleAppFramework;
using GitStory.Core;
using LibGit2Sharp;
using System;
using System.IO;

namespace GitStoryCLI
{
	class Program : ConsoleAppBase
	{
		static void Main(string[] args)
		{
			string dir = Repository.Discover(Directory.GetCurrentDirectory());

			using (var repo = new Repository(dir))
			{
				if (args.Length == 1 && args[0] == "fix")
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
}

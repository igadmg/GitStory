using GitStory.Core;
using LibGit2Sharp;
using System;
using System.IO;

namespace GitStoryCLI
{
	class Program
	{
		static void Main(string[] args)
		{
			string dir = Directory.GetCurrentDirectory();

			using (var repo = new Repository(dir))
			{
				if (args.Length == 1 && args[0] == "fix")
				{
					repo.Fix();
				}
				else if (args.Length == 1 && args[0] == "status")
				{

				}
				else
				{
					repo.Store();
				}
			}
		}
	}
}

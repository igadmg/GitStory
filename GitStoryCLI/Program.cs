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
				Console.WriteLine("Hello World!");
			}
		}
	}
}

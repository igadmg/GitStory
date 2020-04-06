using LibGit2Sharp;
using System;
using System.Linq;

namespace GitStory.Core
{
	public class CheckoutBranch : IDisposable
	{
		Repository repo;
		Branch oldBranch;

		public CheckoutBranch(Repository repo, Branch branch)
		{
			this.repo = repo;
			if (repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false }).Any())
				throw new Exception("Repository must have no changes.");

			oldBranch = repo.Head;
			Commands.Checkout(repo, branch);
		}

		public void Dispose()
		{
			if (repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false }).Any())
				throw new Exception("Repository must have no changes.");

			Commands.Checkout(repo, oldBranch);
		}
	}
}

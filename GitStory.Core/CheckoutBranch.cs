using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitStory.Core
{
	public class CheckoutBranch : IDisposable
	{
		Branch oldBranch;

		public CheckoutBranch(Repository repo, Branch branch)
		{
			if (repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false }).Any())
				throw new Exception("Repository must have no changes.");

			oldBranch = repo.Head;
			Commands.Checkout(repo, branch);
		}

		public void Dispose()
		{
			
		}
	}
}

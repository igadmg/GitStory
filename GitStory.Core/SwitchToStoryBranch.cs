using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitStory.Core
{
	class SwitchToStoryBranch : IDisposable
	{
		Repository repo;
		Reference headRef;

		public SwitchToStoryBranch(Repository repo, StoryBranchNameDelegate storyBranchNameFn)
		{
			this.repo = repo;
			ToStoryBranch(repo, storyBranchNameFn, out headRef);
		}

		public void Dispose()
		{
			ToHeadBranch(repo, headRef);
		}
	}
}

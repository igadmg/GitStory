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

		static void ToStoryBranch(Repository repo, StoryBranchNameDelegate storyBranchNameFn, out Reference headRef)
		{
			var storyBranch = repo.GetStoryBranch(repo.Head, storyBranchNameFn, out var storyBranchName);
			storyBranch = storyBranch ?? repo.CreateBranch(storyBranchName);

			headRef = (repo.Head.Reference as SymbolicReference).Target;
			repo.Refs.UpdateTarget("HEAD", storyBranch.Reference.CanonicalName);
		}

		static void ToHeadBranch(Repository repo, Reference headRef)
		{
			repo.Refs.UpdateTarget("HEAD", headRef.CanonicalName);
		}
	}
}

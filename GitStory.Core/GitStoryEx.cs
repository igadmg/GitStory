using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemEx;

namespace GitStory.Core
{
	public static class GitStoryEx
	{
		public static void Store(this Repository repo)
		{
			repo.Store(
				storyBranchNameFn: (head, commit) => $"{head.FriendlyName}_{commit.Sha}_story",
				message: "update");
		}

		public static void Store(this Repository repo, Func<Branch, Commit, string> storyBranchNameFn, string message)
		{
			var head = repo.Head;
			var lastHeadCommit = repo.Head.Commits.First();

			var storyBranchName = storyBranchNameFn(head, lastHeadCommit);
			var storyBranch = repo.Branches.Where(b => b.FriendlyName == storyBranchName).FirstOrDefault();

			storyBranch = storyBranch ?? repo.CreateBranch(storyBranchName);

			var headRef = repo.Refs.Where(r => r.CanonicalName == head.CanonicalName).FirstOrDefault();
			var oldHeadRef = headRef;

			var storyBranchRef = repo.Refs.Where(r => r.CanonicalName == storyBranch.CanonicalName).FirstOrDefault();

			// got branches

			List<string> filesNotStaged = new List<string>();

			foreach (var item in repo.RetrieveStatus(new StatusOptions { ExcludeSubmodules = true, IncludeIgnored = false }))
			{
				if (!item.State.HasFlag(FileStatus.ModifiedInIndex))
				{
					filesNotStaged.Add(item.FilePath);
				}
			}
			repo.Refs.UpdateTarget("HEAD", storyBranchRef.CanonicalName);

			// saved HEAD

			Commands.Stage(repo, "*");

			try
			{
				var author = new Signature(
					new Identity(repo.Config.Get<string>("user.name").Value, repo.Config.Get<string>("user.email").Value)
					, DateTime.Now);
				repo.Commit(message, author, author);
			}
			catch (Exception e)
			{
			}

			// restore HEAD

			repo.Refs.UpdateTarget("HEAD", oldHeadRef.CanonicalName);

			Commands.Unstage(repo, filesNotStaged);
		}

		public static void Fix(this Repository repo)
		{
			var diaryBranch = repo.Head;

			var headBranchName = diaryBranch.CanonicalName.CutEnd('_').CutEnd('_');
			var headBranch = repo.Branches.Where(b => b.CanonicalName == headBranchName).FirstOrDefault();

			repo.Refs.UpdateTarget("HEAD", headBranch.CanonicalName);
		}
	}
}

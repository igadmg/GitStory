using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemEx;

namespace GitStory.Core
{
	public static class GitStoryEx
	{
		public delegate string StoryBranchNameDelegate(string id, Branch branch, Commit commit);

		public static StoryBranchNameDelegate DefaultStoryBranchNameFn = (id, head, commit) => $"story/{id}/{head.FriendlyName}_{commit.Sha}";
		public static string DefaultCommitMessage = "update";

		public static Guid GetRepositoryGuid(this Repository repo)
		{
			return Guid.NewGuid();
		}

		static void SwitchToStoryBranch(this Repository repo, Func<Branch, Commit, string> storyBranchNameFn, out Reference headRef, out List<string> filesNotStaged)
		{
			filesNotStaged = new List<string>();

			var head = repo.Head;
			var lastHeadCommit = repo.Head.Commits.First();

			var storyBranchName = storyBranchNameFn(head, lastHeadCommit);
			var storyBranch = repo.Branches.Where(b => b.FriendlyName == storyBranchName).FirstOrDefault();

			storyBranch = storyBranch ?? repo.CreateBranch(storyBranchName);

			headRef = repo.Refs.Where(r => r.CanonicalName == head.CanonicalName).FirstOrDefault();

			var storyBranchRef = repo.Refs.Where(r => r.CanonicalName == storyBranch.CanonicalName).FirstOrDefault();

			// got branches

			foreach (var item in repo.RetrieveStatus(new StatusOptions { ExcludeSubmodules = true, IncludeIgnored = false }))
			{
				if (!item.State.HasFlag(FileStatus.ModifiedInIndex))
				{
					filesNotStaged.Add(item.FilePath);
				}
			}
			repo.Refs.UpdateTarget("HEAD", storyBranchRef.CanonicalName);
		}

		static void SwitchToHeadBranch(this Repository repo, Reference headRef, List<string> filesNotStaged)
		{
			repo.Refs.UpdateTarget("HEAD", headRef.CanonicalName);

			Commands.Unstage(repo, filesNotStaged);
		}

		class ToStoryBranch : IDisposable
		{
			Repository repo;
			Reference headRef;
			List<string> filesNotStaged;

			public ToStoryBranch(Repository repo, Func<string, Branch, Commit, string> storyBranchNameFn)
			{
				this.repo = repo;
				repo.SwitchToStoryBranch(storyBranchNameFn, out headRef, out filesNotStaged);
			}

			public void Dispose()
			{
				repo.SwitchToHeadBranch(headRef, filesNotStaged);
			}
		}

		public static Repository Store(this Repository repo)
			=> repo.Store(
				storyBranchNameFn: DefaultStoryBranchNameFn,
				message: DefaultCommitMessage);

		public static Repository Store(this Repository repo, Func<string, Branch, Commit, string> storyBranchNameFn, string message)
		{
			foreach (var sm in repo.Submodules)
			{
				if (sm.RetrieveStatus() == SubmoduleStatus.Unmodified)
					continue;

				try
				{
					new Repository(sm.Path).Store(storyBranchNameFn, message);
				}
				catch { }
			}

			using (new ToStoryBranch(repo, storyBranchNameFn))
			{
				Commands.Stage(repo, "*");

				try
				{
					var author = new Signature(
						new Identity(repo.Config.Get<string>("user.name").Value, repo.Config.Get<string>("user.email").Value)
						, DateTime.Now);
					var commiter = new Signature(
						new Identity(repo.Config.Get<string>("user.name").Value, repo.Config.Get<string>("user.email").Value)
						, DateTime.Now);
					repo.Commit(message, author, author);
				}
				catch { }
			}

			return repo;
		}

		public static Repository Status(this Repository repo)
			=> repo.Status(storyBranchNameFn: DefaultStoryBranchNameFn);

		public static Repository Status(this Repository repo, Func<Branch, Commit, string> storyBranchNameFn)
		{
			foreach (var sm in repo.Submodules)
			{
				if (sm.RetrieveStatus() == SubmoduleStatus.Unmodified)
					continue;

				try
				{
					new Repository(sm.Path).Status(storyBranchNameFn);
				}
				catch { }
			}

			using (new ToStoryBranch(repo, storyBranchNameFn))
			{
				Commands.Stage(repo, "*");

				try
				{
					foreach (var item in repo.RetrieveStatus(new StatusOptions { ExcludeSubmodules = true, IncludeIgnored = false }))
					{
						Console.WriteLine($"{item.State}: {item.FilePath}");
					}
				}
				catch { }
			}

			return repo;
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

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

		public static string GetRepositoryUuid(this Repository repo)
		{
			var uuid = repo.Config.Get<string>("gitstory.uuid").Value;
			if (uuid == null)
			{
				uuid = Guid.NewGuid().ToString("N");
				repo.Config.Set("gitstory.uuid", uuid);
			}

			return uuid;
		}

		public static Signature GetAuthorSignature(this Repository repo, DateTime time)
			=> new Signature(
				new Identity(
					repo.Config.Get<string>("user.name").Value,
					repo.Config.Get<string>("user.email").Value)
				, time);

		public static Signature GetCommiterSignature(this Repository repo, DateTime time)
			=> new Signature(
				new Identity(
					repo.Config.GetValueOrDefault("gitstory.commiter.name", () => "Git Story"),
					repo.Config.GetValueOrDefault("gitstory.commiter.email", () => repo.Config.Get<string>("user.email").Value))
				, time);

		static void SaveStatus(this Repository repo, out Dictionary<string, FileStatus> filesStatus)
		{
			filesStatus = new Dictionary<string, FileStatus>();

			foreach (var item in repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false }))
			{
				filesStatus.Add(item.FilePath, item.State);
			}
		}

		static void RestoreStatus(this Repository repo, Dictionary<string, FileStatus> filesStatus)
		{
			Commands.Unstage(repo, filesStatus.Where(p => !p.Value.HasFlag(FileStatus.ModifiedInIndex)).Select(p => p.Key));
		}

		class CaptureStatus : IDisposable
		{
			Repository repo;
			Dictionary<string, FileStatus> filesStatus;

			public bool IsEmpty => filesStatus.Count == 0;

			public CaptureStatus(Repository repo)
			{
				this.repo = repo;
				repo.SaveStatus(out filesStatus);
			}

			public void Dispose()
			{
				repo.RestoreStatus(filesStatus);
			}
		}

		static void SwitchToStoryBranch(this Repository repo, StoryBranchNameDelegate storyBranchNameFn, out Reference headRef)
		{
			var id = repo.GetRepositoryUuid();
			var head = repo.Head;
			var lastHeadCommit = repo.Head.Commits.First();

			var storyBranchName = storyBranchNameFn(id, head, lastHeadCommit);
			var storyBranch = repo.Branches.Where(b => b.FriendlyName == storyBranchName).FirstOrDefault();

			storyBranch = storyBranch ?? repo.CreateBranch(storyBranchName);

			headRef = repo.Refs.Where(r => r.CanonicalName == head.CanonicalName).FirstOrDefault();

			var storyBranchRef = repo.Refs.Where(r => r.CanonicalName == storyBranch.CanonicalName).FirstOrDefault();

			// got branches

			repo.Refs.UpdateTarget("HEAD", storyBranchRef.CanonicalName);
		}

		static void SwitchToHeadBranch(this Repository repo, Reference headRef)
		{
			repo.Refs.UpdateTarget("HEAD", headRef.CanonicalName);
		}

		class ToStoryBranch : IDisposable
		{
			Repository repo;
			Reference headRef;

			public ToStoryBranch(Repository repo, StoryBranchNameDelegate storyBranchNameFn)
			{
				this.repo = repo;
				repo.SwitchToStoryBranch(storyBranchNameFn, out headRef);
			}

			public void Dispose()
			{
				repo.SwitchToHeadBranch(headRef);
			}
		}

		public static Repository Store(this Repository repo)
			=> repo.Store(
				storyBranchNameFn: DefaultStoryBranchNameFn,
				message: DefaultCommitMessage);

		public static Repository Store(this Repository repo, StoryBranchNameDelegate storyBranchNameFn, string message)
		{
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

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
						var now = DateTime.Now;
						var author = repo.GetAuthorSignature(now);
						var commiter = repo.GetCommiterSignature(now);
						repo.Commit(message, author, commiter);
					}
					catch { }
				}
			}

			return repo;
		}

		public static Repository Status(this Repository repo)
			=> repo.Status(storyBranchNameFn: DefaultStoryBranchNameFn);

		public static Repository Status(this Repository repo, StoryBranchNameDelegate storyBranchNameFn)
		{
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

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

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

		public static string GenerateUuid(this Repository repo)
		{
			var author = repo.GetAuthorSignature(DateTime.Now);
			return author.ToString().HashSHA1();
		}

		public static string GetUuid(this Repository repo)
		{
			var uuid = repo.Config.Get<string>("gitstory.uuid")?.Value;
			if (uuid == null)
			{
				uuid = repo.GenerateUuid();
				repo.SetUuid(uuid);
			}

			return uuid;
		}

		public static Repository SetUuid(this Repository repo, string uuid)
		{
			repo.Config.Set("gitstory.uuid", uuid);

			return repo;
		}

		public static Repository ChangeUuid(this Repository repo, string oldUuid, string newUuid)
		{
			if (newUuid.null_ws_())
				newUuid = repo.GenerateUuid();

			foreach (var b in repo.Branches)
			{
				//int i = 0;
				// TODO: rename branches.
			}

			return repo;
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

		public static Branch GetStoryBranch(this Repository repo
			, Branch branch, StoryBranchNameDelegate storyBranchNameFn, out string storyBranchName)
		{
			var id = repo.GetUuid();
			var currentCommit = branch.Commits.First();

			storyBranchName = storyBranchNameFn(id, branch, currentCommit);
			var n = storyBranchName;
			return repo.Branches.Where(b => b.FriendlyName == n).FirstOrDefault();
		}

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
			var filesToUnstage = filesStatus
				.Where(p => !p.Value.HasFlag(FileStatus.ModifiedInIndex))
				.Select(p => p.Key);
			if (filesToUnstage.Any())
			{
				Commands.Unstage(repo, filesToUnstage);
			}
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
			var storyBranch = repo.GetStoryBranch(repo.Head, storyBranchNameFn, out var storyBranchName);
			storyBranch = storyBranch ?? repo.CreateBranch(storyBranchName);

			var headRef2 = repo.Refs.Where(r => r.CanonicalName == repo.Head.CanonicalName).FirstOrDefault();
			headRef = repo.Head.Reference;
			repo.Refs.UpdateTarget("HEAD", storyBranch.Reference.CanonicalName);
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

using LibGit2Sharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemEx;

namespace GitStory.Core
{
	public class StoryBranchNameDelegateParameters { public string id; public Branch branch; public Commit commit; }
	public delegate string StoryBranchNameDelegate(string id, Branch branch, Commit commit);
	
	public static class GitStoryEx
	{
		static Dictionary<string, StoryBranchNameDelegate> StoryBranchNameFns = new Dictionary<string, StoryBranchNameDelegate>();

		public static StoryBranchNameDelegate DefaultStoryBranchNameFn = (id, branch, commit) => $"story/{id}/{branch.FriendlyName}/{commit.Sha}";
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

		public static Identity GetCommiterIdentity(this Repository repo)
			=> new Identity(
					repo.Config.GetValueOrDefault("gitstory.commiter.name", () => "Git Story"),
					repo.Config.GetValueOrDefault("gitstory.commiter.email", () => repo.Config.Get<string>("user.email").Value));

		public static Signature GetCommiterSignature(this Repository repo, DateTime time)
			=> new Signature(
				repo.GetCommiterIdentity(), time);

		public static StoryBranchNameDelegate GetStoryBranchNameFn(this string pattern)
			=> pattern.null_ws_()
			? DefaultStoryBranchNameFn
			: StoryBranchNameFns.GetOrAdd(pattern, p => {
				var barnchNameScript = CSharpScript.Create<string>($"$\"{p}\""
					, globalsType: typeof(StoryBranchNameDelegateParameters));
				barnchNameScript.Compile();

				return (id, branch, commit) =>
				{
					var globals = new StoryBranchNameDelegateParameters
					{
						id = id,
						branch = branch,
						commit = commit
					};
					return barnchNameScript.RunAsync(globals).Result.ReturnValue;
				};
			});

		public static StoryBranchNameDelegate GetStoryBranchNameFn(this Repository repo)
		{
			var namePattern = repo.Config.GetValueOrDefault("gitstory.branchnamepattern", string.Empty);
			return namePattern.GetStoryBranchNameFn();
		}

		public static Branch GetStoryBranch(this Repository repo
			, Branch branch, StoryBranchNameDelegate storyBranchNameFn)
			=> repo.GetStoryBranch(branch, storyBranchNameFn, out string storyBranchName);

		public static Branch GetStoryBranch(this Repository repo
			, Branch branch, StoryBranchNameDelegate storyBranchNameFn, out string storyBranchName)
			=> repo.GetStoryBranch(repo.GetUuid(), branch, branch.Tip, storyBranchNameFn, out storyBranchName);

		public static Branch GetStoryBranch(this Repository repo
			, Branch branch, Commit commit, StoryBranchNameDelegate storyBranchNameFn)
			=> repo.GetStoryBranch(branch, commit, storyBranchNameFn, out string storyBranchName);

		public static Branch GetStoryBranch(this Repository repo
			, Branch branch, Commit commit, StoryBranchNameDelegate storyBranchNameFn, out string storyBranchName)
			=> repo.GetStoryBranch(repo.GetUuid(), branch, commit, storyBranchNameFn, out storyBranchName);

		public static Branch GetStoryBranch(this Repository repo
			, string id, Branch branch, Commit commit
			, StoryBranchNameDelegate storyBranchNameFn, out string storyBranchName)
		{
			storyBranchName = storyBranchNameFn(id, branch, commit);
			var n = storyBranchName;
			return repo.Branches.Where(b => b.FriendlyName == n).FirstOrDefault();
		}

		public static Repository RenameStoryBranches(this Repository repo
			, string oldPattern, string newPattern = null)
			=> repo.RenameStoryBranches(oldPattern.GetStoryBranchNameFn(), newPattern.GetStoryBranchNameFn());

		public static Repository RenameStoryBranches(this Repository repo
			, StoryBranchNameDelegate oldBranchNameFn
			, StoryBranchNameDelegate newBranchNameFn)
		{
			var now = DateTime.Now;

			foreach (var commit in repo.Head.Commits)
			{
				var oldStoryBranch = repo.GetStoryBranch(repo.Head, commit, oldBranchNameFn);
				if (oldStoryBranch != null)
				{
					var newStoryBranch = repo.GetStoryBranch(repo.Head, commit, newBranchNameFn, out var newStoryBranchName);

					if (newStoryBranch != null)
					{
						try
						{
							repo.Checkout(newStoryBranch.Tip.Tree
								, repo.Submodules.Select(s => s.Path), new CheckoutOptions());

							var rebase = repo.Rebase.Start(newStoryBranch.Tip, oldStoryBranch.Tip, oldStoryBranch.Tip, repo.GetCommiterIdentity()
								, new RebaseOptions {
									FileConflictStrategy = CheckoutFileConflictStrategy.Ours
								});
							if (rebase.Status != RebaseStatus.Complete)
							{

							}

							int i = 0;

							/*
							var result = repo.Merge(oldStoryBranch, repo.GetCommiterSignature(now),
								new MergeOptions()
								{
									FileConflictStrategy = CheckoutFileConflictStrategy.Ours
								});
							repo.Branches.Remove(oldStoryBranch);
							foreach (var c in repo.Index.Conflicts.ToArray())
							{
								repo.Index.Add(c.Ours.Path);
							}

							Commands.Stage(repo, "*");

							repo.Commit("merge"
								, repo.GetAuthorSignature(now)
								, repo.GetCommiterSignature(now));
								*/
						}
						catch (Exception e)
						{
							int i = 0;
						}
					}
					else
					{
						repo.Branches.Rename(oldStoryBranch, newStoryBranchName);
					}
				}
			}

			return repo;
		}

		public static Repository Store(this Repository repo)
			=> repo.Store(
				storyBranchNameFn: repo.GetStoryBranchNameFn(),
				message: DefaultCommitMessage);

		public static Repository Store(this Repository repo, StoryBranchNameDelegate storyBranchNameFn, string message)
		{
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

				repo.Submodules
					.Where(sm => sm.RetrieveStatus() != SubmoduleStatus.Unmodified)
					.ForEachSubmodule(sm => {
						new Repository(sm.Path).Store(storyBranchNameFn, message);
					});

				using (new SwitchToStoryBranch(repo, storyBranchNameFn))
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
			=> repo.Status(storyBranchNameFn: repo.GetStoryBranchNameFn());

		public static Repository Status(this Repository repo, StoryBranchNameDelegate storyBranchNameFn)
		{
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

				repo.Submodules
					.Where(sm => sm.RetrieveStatus() != SubmoduleStatus.Unmodified)
					.ForEachSubmodule(sm => {
						new Repository(sm.Path).Status(storyBranchNameFn);
					});

				using (new SwitchToStoryBranch(repo, storyBranchNameFn))
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

		private static IEnumerable<(Commit oldCommit, Commit newCommit)>
			EnumCommitPairsUntil(this ICommitLog log, Commit endCommit)
		{
			Commit newCommit = null;
			foreach (var oldCommit in log.TakeWhile(c => c.Sha != endCommit.Sha))
			{
				if (newCommit != null)
				{
					yield return (oldCommit, newCommit);
				}

				newCommit = oldCommit;
			}
		}

		public static void Diff(this Repository repo)
			=> repo.Diff(repo.GetStoryBranchNameFn());

		public static void Diff(this Repository repo, StoryBranchNameDelegate storyBranchNameFn)
		{
			var branch = repo.GetStoryBranch(repo.Head, storyBranchNameFn);
			if (branch == null)
				return;

			Dictionary<string, SourceHeatmap> heatmaps = new Dictionary<string, SourceHeatmap>();

			foreach (var p in branch.Commits.EnumCommitPairsUntil(repo.Head.Tip))
			{
				var diff = repo.Diff.Compare<Patch>(p.oldCommit.Tree, p.newCommit.Tree);

				foreach (var change in diff)
				{
					var heatmap = heatmaps.GetOrAdd(change.Path, s => new SourceHeatmap(s));

					heatmap.Prepend(change);
				}
			}
		}
	}
}

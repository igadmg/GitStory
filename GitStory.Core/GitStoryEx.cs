using LibGit2Sharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SystemEx;

namespace GitStory.Core
{
	public class StoryBranchNameDelegateParameters { public string id; public Branch branch; public Commit commit; }
	public delegate string StoryBranchNameDelegate(string id, Branch branch, Commit commit);
	
	public enum StoryRepositoryMode
	{
		Embedded,
		Separate,
	}

	public static class GitStoryEx
	{
		static Dictionary<string, StoryBranchNameDelegate> StoryBranchNameFns = new Dictionary<string, StoryBranchNameDelegate>();

		public static string DefaultStoryBranchNamePattern = "story/{id}/{branch?.FriendlyName}/{commit?.Sha}";
		public static string DefaultCommitMessage = "update";

		public static string ToMD5(this string str, string format = "x2")
		{
			var hashBuilder = new System.Text.StringBuilder();
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				foreach (byte b in md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str)))
				{
					hashBuilder.Append(b.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
				}
			}

			return hashBuilder.ToString();
		}

		public static string GetInternalDataPath(this Repository repo)
		{
			var path = Path.Combine(repo.Info.Path, ".git-story");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			return path;
		}

		public static Assembly GenerateBranchNameFnAssembly(this string path, string pattern)
		{
			var script = CSharpScript.Create<string>($"$\"{pattern}\""
					, globalsType: typeof(StoryBranchNameDelegateParameters));
			script.Compile();
			var compilation = script.GetCompilation();

			using (var ilstream = File.Create(path))
			{
				compilation.Emit(ilstream);
			}

			var entryPoint = compilation.GetEntryPoint(CancellationToken.None);

			return Assembly.Load(File.ReadAllBytes(path));
		}

		public static Assembly GetBranchNameFnAssembly(this string path, string pattern)
		{
			var assemblyPath = Path.Combine(path, $"{pattern.ToMD5()}.bnfa");

			if (File.Exists(assemblyPath))
				return Assembly.Load(File.ReadAllBytes(assemblyPath));

			return assemblyPath.GenerateBranchNameFnAssembly(pattern);
		}

		public static MethodInfo GetBranchNameFnMethod(this string path, string pattern)
		{
			var assembly = path.GetBranchNameFnAssembly(pattern);
			var type = assembly.GetType("Submission#0");
			if (type == null)
			{
				path.RemoveBranchNameFnAssembly(pattern);
				assembly = path.GetBranchNameFnAssembly(pattern);
				type = assembly.GetType("Submission#0");
			}

			return type.GetMethod("<Factory>");
		}

		public static void RemoveBranchNameFnAssembly(this string path, string pattern)
		{
			var assemblyPath = Path.Combine(path, $"{pattern.ToMD5()}.bnfa");

			if (File.Exists(assemblyPath))
				File.Delete(assemblyPath);
		}

		public static bool GetEnabled(this Repository repo)
		{
			return repo.Config.Get<bool>("gitstory.enabled")?.Value
				?? repo.Config.Get<bool>("gitstory.enabled", ConfigurationLevel.Global)?.Value
				?? false;
		}

		public static Repository SetEnabled(this Repository repo, bool enabled, ConfigurationLevel level = ConfigurationLevel.Local)
		{
			repo.Config.Set("gitstory.enabled", enabled, level);

			return repo;
		}

		public static StoryRepositoryMode GetRepository(this Repository repo)
		{
			string v = repo.Config.Get<string>("gitstory.repository")?.Value
				?? repo.Config.Get<string>("gitstory.repository",  ConfigurationLevel.Global)?.Value
				?? string.Empty;
			return Enum.TryParse<StoryRepositoryMode>(v, true, out var e) ? e : StoryRepositoryMode.Embedded;
		}

		public static Repository SetRepository(this Repository repo, StoryRepositoryMode mode, ConfigurationLevel level = ConfigurationLevel.Local)
		{
			repo.Config.Set("gitstory.repository", mode.ToString(), level);

			return repo;
		}

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
			if (newUuid.IsNullOrWhiteSpace())
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

		public static StoryBranchNameDelegate GetStoryBranchNameFn(this string pattern, Repository repo)
			=> StoryBranchNameFns.GetOrAdd(pattern, p => {
				var method = repo.GetInternalDataPath().GetBranchNameFnMethod(pattern);

				return (id, branch, commit) =>
				{
					var globals = new StoryBranchNameDelegateParameters
					{
						id = id,
						branch = branch,
						commit = commit
					};

					var submissionArray = new object[2];
					submissionArray[0] = globals;

					try
					{
						return ((Task<string>)method.Invoke(null, new object[] { submissionArray })).Result;
					}
					catch (Exception e)
					{
						repo.GetInternalDataPath().RemoveBranchNameFnAssembly(pattern);
						method = repo.GetInternalDataPath().GetBranchNameFnMethod(pattern);
					}

					return ((Task<string>)method.Invoke(null, new object[] { submissionArray })).Result;
				};
			});

		public static string GetStoryBranchNamePattern(this Repository repo)
			=> repo.Config.GetValueOrDefault("gitstory.branchnamepattern", DefaultStoryBranchNamePattern);

		public static StoryBranchNameDelegate GetStoryBranchNameFn(this Repository repo)
			=> repo.GetStoryBranchNamePattern().GetStoryBranchNameFn(repo);

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
			=> repo.RenameStoryBranches(oldPattern.GetStoryBranchNameFn(repo), newPattern.GetStoryBranchNameFn(repo));

		public static Repository RenameStoryBranches(this Repository repo
			, StoryBranchNameDelegate oldBranchNameFn
			, StoryBranchNameDelegate newBranchNameFn)
		{
			var now = DateTime.Now;

			using (var head = DisposableLock.Lock(repo.Head, h => {
				Commands.Checkout(repo, h);
				repo.Submodules.UpdateAll(new SubmoduleUpdateOptions());
			}))
			{
				foreach (var (commit, oldStoryBranch, newStoryBranch, newStoryBranchName) in head.Value.Commits
					.Select(c => (commit: c, oldStoryBranch: repo.GetStoryBranch(head, c, oldBranchNameFn)))
					.Where(p => p.oldStoryBranch != null)
					.Select(p => {
						var b = repo.GetStoryBranch(head, p.commit, newBranchNameFn, out var newStoryBranchName);
						return (p.commit, p.oldStoryBranch, newStoryBranch: b, newStoryBranchName);
					}))
				{
					if (newStoryBranch != null)
					{
						try
						{
							//Commands.Checkout(repo, newStoryBranch);
							var rebase = repo.Rebase.Start(newStoryBranch, oldStoryBranch, null, repo.GetCommiterIdentity()
								, new RebaseOptions());
							if (rebase.Status != RebaseStatus.Complete)
							{

							}

							repo.Branches.Remove(oldStoryBranch);
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
			if (!repo.GetEnabled())
				return repo;

			using (var aes = new AggregateExceptionScope()) 
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

				aes.Execute(() => {
					repo.Submodules
						.Where(sm => sm.RetrieveStatus() != SubmoduleStatus.Unmodified)
						.Execute(sm =>
						{
							new Repository(Path.Combine(repo.Info.WorkingDirectory, sm.Path)).Store(storyBranchNameFn, message);
						});
				});

				using (new SwitchToStoryBranch(repo, storyBranchNameFn))
				{
					Commands.Stage(repo, "*");

					aes.Execute(() => {
						var now = DateTime.Now;
						var author = repo.GetAuthorSignature(now);
						var commiter = repo.GetCommiterSignature(now);

						try
						{
							repo.Commit(message, author, commiter);
						}
						catch (EmptyCommitException e) { }
					});
				}
			}

			return repo;
		}

		public static Repository Status(this Repository repo)
			=> repo.Status(storyBranchNameFn: repo.GetStoryBranchNameFn());

		public static Repository Status(this Repository repo, StoryBranchNameDelegate storyBranchNameFn)
		{
			Console.WriteLine($"Git story is {(repo.GetEnabled() ? "enabled" : "disabled")}.");
			Console.WriteLine($"Git story is in {repo.GetRepository()} mode.");

			if (!repo.GetEnabled())
				return repo;

			using (var aes = new AggregateExceptionScope())
			using (var st = new CaptureStatus(repo))
			{
				if (st.IsEmpty)
					return repo;

				aes.Execute(() => {
					repo.Submodules
						.Where(sm => sm.RetrieveStatus() != SubmoduleStatus.Unmodified)
						.Execute(sm =>
						{
							new Repository(Path.Combine(repo.Info.WorkingDirectory, sm.Path)).Status(storyBranchNameFn);
						});
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
			=> repo.Fix(repo.GetStoryBranchNamePattern());

		public static void Fix(this Repository repo, string StoryBranchNamePattern)
		{
			var diaryBranch = repo.Head;

			List<string> groupNames = new List<string>();
			Regex r = new Regex($"^{StoryBranchNamePattern.format(p => { groupNames.Add(p); return $"(.+?)"; })}$");
			var m = r.Matches(diaryBranch.FriendlyName);

			if (m.Count > 0)
			{
				// TODO: not tested, never was used.
				var headBranchName = m[0].Groups[groupNames.IndexOf("branch.FriendlyName") + 1].Value;
				var headBranch = repo.Branches.Where(b => b.FriendlyName == headBranchName).FirstOrDefault();

				repo.Refs.UpdateTarget("HEAD", headBranch.CanonicalName);
			}
		}

		private static IEnumerable<(Commit oldCommit, Commit newCommit)>
			EnumCommitPairsUntil(this ICommitLog log, Commit endCommit)
		{
			Commit newCommit = null;
			foreach (var oldCommit in log.TakeWhileAndLast((c) => c.Sha != endCommit.Sha))
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

			foreach ((var oldCommit, var newCommit) in branch.Commits.EnumCommitPairsUntil(repo.Head.Tip))
			{
				var diff = repo.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);

				foreach (var change in diff)
				{
					var heatmap = heatmaps.GetOrAdd(change.Path, s => new SourceHeatmap(s));

					heatmap.Prepend(change);
				}
			}
		}
	}
}

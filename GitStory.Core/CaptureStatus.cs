using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitStory.Core
{
	internal class CaptureStatus : IDisposable
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
	}
}

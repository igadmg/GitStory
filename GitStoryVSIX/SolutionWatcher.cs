using EnvDTE80;
using GitStory.Core;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using VSIXEx;
using VSIXEx.Events;

namespace GitStoryVSIX
{
	public class SolutionWatcher : IDisposable
	{
		private DTE2 dte;
		private IVsStatusbar statusBar;
		private RunningDocTableEvents rdte;
		private Repository repo;


		public SolutionWatcher(AsyncPackage package)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
			statusBar = package.GetServiceAsync(typeof(SVsStatusbar)).Result as IVsStatusbar;

			try
			{
				var solutionDir = Path.GetDirectoryName(dte.Solution.FileName);
				repo = new Repository(solutionDir);
				rdte = new RunningDocTableEvents(package,
					OnAfterSaveAll: () =>
					{
						ThreadHelper.ThrowIfNotOnUIThread();

						if (repo?.GetEnabled() ?? false)
						{
							print("Saving Story...");
							try
							{
								repo?.Store();
							}
							catch (Exception e)
							{
								var errorPane = dte.ToolWindows.OutputWindow.OutputWindowPanes.PaneByName("VSIX: Errors");

								errorPane.OutputString(e.ToString());
							}
						}
					});
			}
			catch
			{
				Dispose(true);
			}
		}

		#region IDisposable Support
		private bool disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					rdte?.Dispose();
					repo?.Dispose();
				}

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~SolutionWatcher()
		// {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		public void Dispose()
		{
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		void print(string message)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (statusBar == null)
				return;

			statusBar.IsFrozen(out int frozen);
			bool unlocked = frozen == 0;
			if (unlocked)
			{
				statusBar.SetText(message);
			}
		}
	}
}

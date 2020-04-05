using GitStory.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Reflection;

namespace GitStoryVSIX
{
	public static class RunningDocumentInfoEx
	{
		static PropertyInfo pIsDirty = typeof(RunningDocumentInfo).GetProperty("IsDirty");

		public static bool IsDirty(this RunningDocumentInfo rdi)
		{
			if (pIsDirty == null)
				return false;

			return (bool)pIsDirty.GetValue(rdi);
		}
	}

	public class RunningDocTableEvents : IVsRunningDocTableEvents3, IDisposable
	{
		RunningDocumentTable rdt;
		private uint cookie;

		public RunningDocTableEvents(GitStoryVSPackage serviceProvider,
			Action OnAfterSaveFn)
		{
			rdt = new RunningDocumentTable(serviceProvider);
			cookie = rdt.Advise(this);
		}

		public void Dispose()
		{
			rdt.Unadvise(cookie);
		}

#region IVsRunningDocTableEvents3

		public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
		public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
		public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
		public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
		public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
		public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;
		public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

		Action OnAfterSaveFn;
		public int OnAfterSave(uint docCookie)
		{
			var dirtyCount = rdt.Count(d => d.IsDirty());

			if (dirtyCount == 0)
			{
				OnAfterSaveFn();
			}

			return VSConstants.S_OK;
		}

		#endregion IVsRunningDocTableEvents3
	}
}

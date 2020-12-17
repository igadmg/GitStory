using LibGit2Sharp;
using System;
using System.Collections.Generic;
using SystemEx;

namespace GitStory.Core
{
	public enum DiffStatus
	{
		New,
		Changed,
		Removed
	}

	public class LineRange
	{
		public int FirstLine;
		public int LastLine;
		public DiffStatus status;
		public int ChangeCount = 0;

		public LineRange(int first, int last)
		{
			FirstLine = first;
			LastLine = last;
		}
	}
#if false
	public static class LineRangeEx
	{
		public static LineRange AsLineRange(this ContentChangeLine line)
		{
			LineRange result = null;

			if (line.OldLineNo < 0 && line.NewLineNo > 0)
			{
				result = new LineRange(line.NewLineNo, line.NewLineNo + line.NumLines);
				result.status = DiffStatus.New;
			}
			else if (line.OldLineNo < 0 && line.NewLineNo > 0)
			{
				result = new LineRange(line.OldLineNo, line.OldLineNo + line.NumLines);
				result.status = DiffStatus.Removed;
			}

			return result;
		}

		public static IEnumerable<LineRange> GroupLineRanges(IEnumerable<ContentChangeLine> changes)
		{
			LineRange prev = null;

			foreach (var c in changes)
			{
				var current = c.AsLineRange();

				if (prev == null)
				{
					prev = current;
					continue;
				}
			}

			yield break;
		}

		public static List<LineRange> Insert(this List<LineRange> list, LineRange item)
		{
			int index = list.BinarySearch(item, (a, b) => a.FirstLine.CompareTo(item.FirstLine));
			index = Math.Abs(index);

			return list;
		}
	}
#endif

	public class SourceHeatmap
	{
		private string path;
		private List<LineRange> lines = new List<LineRange>();

		public SourceHeatmap(string path)
		{
			this.path = path;
		}

		public void Prepend(PatchEntryChanges changes)
		{
			List<LineRange> patch = new List<LineRange>();

			//foreach (var l in changes)
			//{
			//	
			//}
		}
	}
}

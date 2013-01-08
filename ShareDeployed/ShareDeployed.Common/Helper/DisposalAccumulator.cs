using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Helper
{
	using IDisposable = System.IDisposable;
	using System.Collections.Generic;

	public class DisposalAccumulator : IDisposable
	{
		private readonly Stack<IDisposable> stack = new Stack<IDisposable>();

		public T Add<T>(T element) where T : IDisposable
		{
			if (element != null)
				stack.Push(element);
			return element;
		} //Add

		void IDisposable.Dispose()
		{
			foreach (IDisposable element in stack)
				element.Dispose();

		} //IDisposable.Dispose

	} //class DisposalAccumulator
}

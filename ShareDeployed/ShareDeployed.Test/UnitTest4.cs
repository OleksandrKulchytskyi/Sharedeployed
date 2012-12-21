using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Helper;
using System.Text;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest4
	{
		[TestMethod]
		public void LazyWeakReferenceTest()
		{
			LazyWeakReference<StringBuilder> builder = new LazyWeakReference<StringBuilder>(
															new StringBuilder("A builder"),
															() => new StringBuilder("Acopy"));
			StringBuilder aTarget = builder.Target;
			aTarget = null;
			GC.Collect();

			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			GC.Collect();
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			GC.Collect();
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			aTarget = builder.Target;
			aTarget = null;
			Console.WriteLine("Reinitialized {0} times", builder.Reinitialized);
			
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
		}
	}
}

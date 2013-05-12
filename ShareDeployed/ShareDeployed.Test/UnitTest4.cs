using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Helper;
using System.Text;
using System.Linq;
using ShareDeployed.Proxy;

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

		[TestMethod]
		public void TetsTypeGetHashCode()
		{
			SafeCollection<int> coll = new SafeCollection<int>();

			var types = from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
						let tps = a.GetTypes()
						from t in tps

						where !a.FullName.Contains("System") && t.IsPublic && t != null & !t.IsNested
						select new { Asm= a, Types= tps };
			
			foreach(var item in types)
			{
				if (item.Asm == null)
					continue;
				foreach (Type t in item.Types)
				{
					if (t != null)
					{
						coll.Add(t.GetHashCode());
					}
				}
			}
		}
	}
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ShareDeployed.Test
{
	[TestClass]
	public class SimpleObjectPoolTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
			CancellationTokenSource cts = new CancellationTokenSource();

			// Create an opportunity for the user to cancel.
			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(TimeSpan.FromSeconds(9));
				cts.Cancel();
			});

			SimpleObjectPool<MyClass> pool = new SimpleObjectPool<MyClass>(() => new MyClass());

			// Create a high demand for MyClass objects.
			Parallel.For(0, 1000000, opt, (i, loopState) =>
			{
				MyClass mc = pool.GetObject();
				Console.CursorLeft = 0;

				// This is the bottleneck in our application. All threads in this loop
				// must serialize their access to the static Console class.
				System.Diagnostics.Debug.WriteLine("{0:####.####}", mc.GetValue(i));

				pool.PutObject(mc);
				if (cts.Token.IsCancellationRequested)
				{
					System.Diagnostics.Debug.WriteLine("Loop stop is requested.");
					loopState.Stop();
				}
			});

			System.Diagnostics.Debug.WriteLine("Object count is " + pool.Count.ToString());
			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(4));
		}

		[TestMethod]
		public void TestMethod2()
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodInfo.GetCurrentMethod().Name);
			var reusablePool = new ReusablePool();
			var thrd1 = new Thread(Run);
			var thrd2 = new Thread(Run);
			var thisObject1 = reusablePool.GetObject();
			var thisObject2 = reusablePool.GetObject();
			thrd1.Start(reusablePool);
			thrd2.Start(reusablePool);
			ViewObject(thisObject1);
			ViewObject(thisObject2);
			Thread.Sleep(3000);
			reusablePool.Release(thisObject1);
			Thread.Sleep(3000);
			reusablePool.Release(thisObject2);
			Thread.Sleep(3000);
		}

		private static void Run(Object obj)
		{
			System.Diagnostics.Debug.WriteLine("\t" + System.Reflection.MethodInfo.GetCurrentMethod().Name);
			var reusablePool = (ReusablePool)obj;
			System.Diagnostics.Debug.WriteLine("\tstart wait");
			var thisObject1 = reusablePool.WaitForObject();
			ViewObject(thisObject1);
			System.Diagnostics.Debug.WriteLine("\tend wait");
			reusablePool.Release(thisObject1);
		}

		private static void ViewObject(Reusable thisObject)
		{
			foreach (var obj in thisObject.Objs)
			{
				Console.Write(obj.ToString() + @" ");
			}
			System.Diagnostics.Debug.WriteLine("\n\r");
		}
	}
	//http://ru.wikipedia.org/wiki/%D0%9E%D0%B1%D1%8A%D0%B5%D0%BA%D1%82%D0%BD%D1%8B%D0%B9_%D0%BF%D1%83%D0%BB
	public class SimpleObjectPool<T>
	{
		private ConcurrentBag<T> _objects;
		private Func<T> _objectGenerator;

		public SimpleObjectPool(Func<T> objectGenerator)
		{
			if (objectGenerator == null)
				throw new ArgumentNullException("objectGenerator");

			_objects = new ConcurrentBag<T>();
			_objectGenerator = objectGenerator;
		}

		public T GetObject()
		{
			T item;
			if (_objects.TryTake(out item))
				return item;
			return _objectGenerator();
		}

		public int Count { get { return _objects.Count; } }

		public void PutObject(T item)
		{
			_objects.Add(item);
		}
	}

	class MyClass
	{
		public int[] Nums { get; set; }

		public double GetValue(long i)
		{
			return Math.Sqrt(Nums[i]);
		}

		public MyClass()
		{
			Nums = new int[1000000];
			Random rand = new Random();
			for (int i = 0; i < Nums.Length; i++)
				Nums[i] = rand.Next();
		}
	}

	/// <summary>
	/// Интерфейс для использования шаблона "Object Pool" <see cref="Object_Pool"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ICreation<T>
	{
		/// <summary>
		/// Возвращает вновь созданный объект
		/// </summary>
		/// <returns></returns>
		T Create();
	}

	/// <summary>
	/// Реализация пула объектов, использующий "мягкие" ссылки
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class WikiObjectPool<T> where T : class
	{
		/// <summary>
		/// Объект синхронизации
		/// </summary>
		private Semaphore semaphore;

		/// <summary>
		/// Коллекция содержит управляемые объекты
		/// </summary>
		private ArrayList pool;

		/// <summary>
		/// Ссылка на объект, которому делегируется ответственность 
		/// за создание объектов пула
		/// </summary>
		private ICreation<T> creator;

		/// <summary>
		/// Количество объектов, существующих в данный момент
		/// </summary>
		private Int32 instanceCount;

		/// <summary>
		/// Максимальное количество управляемых пулом объектов
		/// </summary>
		private Int32 maxInstances;

		/// <summary>
		/// Создание пула объектов
		/// </summary>
		/// <param name="creator">Объект, которому пул будет делегировать ответственность
		/// за создание управляемых им объектов</param>
		public WikiObjectPool(ICreation<T> creator)
			: this(creator, Int32.MaxValue)
		{
		}

		/// <summary>
		/// Создание пула объектов
		/// </summary>
		/// <param name="creator">Объект, которому пул будет делегировать ответственность
		/// за создание управляемых им объектов</param>
		/// <param name="maxInstances">Максимальное количество экземпляров класс,
		/// которым пул разрешает существовать одновременно
		/// </param>
		public WikiObjectPool(ICreation<T> creator, Int32 maxInstances)
		{
			this.creator = creator;
			this.instanceCount = 0;
			this.maxInstances = maxInstances;
			this.pool = new ArrayList();
			this.semaphore = new Semaphore(0, this.maxInstances);
		}

		/// <summary>
		/// Возвращает количество объектов в пуле, ожидающих повторного
		/// использования. Реальное количество может быть меньше
		/// этого значения, поскольку возвращаемая 
		/// величина - это количество "мягких" ссылок в пуле.
		/// </summary>
		public Int32 Size
		{
			get
			{
				lock (pool)
				{
					return pool.Count;
				}
			}
		}

		/// <summary>
		/// Возвращает количество управляемых пулом объектов,
		/// существующих в данный момент
		/// </summary>
		public Int32 InstanceCount { get { return instanceCount; } }

		/// <summary>
		/// Получить или задать максимальное количество управляемых пулом
		/// объектов, которым пул разрешает существовать одновременно.
		/// </summary>
		public Int32 MaxInstances
		{
			get { return maxInstances; }
			set { maxInstances = value; }
		}

		/// <summary>
		/// Возвращает из пула объект. При пустом пуле будет создан
		/// объект, если количество управляемых пулом объектов не 
		/// больше или равно значению, возвращаемому методом 
		/// <see cref="ObjectPool{T}.MaxInstances"/>. Если количество управляемых пулом 
		/// объектов превышает это значение, то данный метод возварщает null 
		/// </summary>
		/// <returns></returns>
		public T GetObject()
		{
			lock (pool)
			{
				T thisObject = RemoveObject();
				if (thisObject != null)
					return thisObject;

				if (InstanceCount < MaxInstances)
					return CreateObject();

				return null;
			}
		}

		/// <summary>
		/// Возвращает из пула объект. При пустом пуле будет создан
		/// объект, если количество управляемых пулом объектов не 
		/// больше или равно значению, возвращаемому методом 
		/// <see cref="ObjectPool{T}.MaxInstances"/>. Если количество управляемых пулом 
		/// объектов превышает это значение, то данный метод будет ждать до тех
		/// пор, пока какой-нибудь объект не станет доступным для
		/// повторного использования.
		/// </summary>
		/// <returns></returns>
		public T WaitForObject()
		{
			lock (pool)
			{
				T thisObject = RemoveObject();
				if (thisObject != null)
					return thisObject;

				if (InstanceCount < MaxInstances)
					return CreateObject();
			}
			semaphore.WaitOne();
			return WaitForObject();
		}



		/// <summary>
		/// Удаляет объект из коллекции пула и возвращает его 
		/// </summary>
		/// <returns></returns>
		private T RemoveObject()
		{
			while (pool.Count > 0)
			{
				var refThis = (WeakReference)pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				var thisObject = (T)refThis.Target;
				if (thisObject != null)
					return thisObject;
				instanceCount--;
			}
			return null;
		}

		/// <summary>
		/// Создать объект, управляемый этим пулом
		/// </summary>
		/// <returns></returns>
		private T CreateObject()
		{
			T newObject = creator.Create();
			instanceCount++;
			return newObject;
		}

		/// <summary>
		/// Освобождает объект, помещая его в пул для
		/// повторного использования
		/// </summary>
		/// <param name="obj"></param>
		/// <exception cref="NullReferenceException"></exception>
		public void Release(T obj)
		{
			if (obj == null)
				throw new NullReferenceException();
			lock (pool)
			{
				var refThis = new WeakReference(obj);
				pool.Add(refThis);
				semaphore.Release();
			}
		}
	}

	public class Reusable
	{
		public Object[] Objs { get; protected set; }

		public Reusable(params Object[] objs)
		{
			this.Objs = objs;
		}
	}

	public class Creator : ICreation<Reusable>
	{
		private static Int32 iD = 0;

		public Reusable Create()
		{
			++iD;
			return new Reusable(iD);
		}
	}

	public class ReusablePool : WikiObjectPool<Reusable>
	{
		public ReusablePool()
			: base(new Creator(), 2)
		{

		}
	}
}
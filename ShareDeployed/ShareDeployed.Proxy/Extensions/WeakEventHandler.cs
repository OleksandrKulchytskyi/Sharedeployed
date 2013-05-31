using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy.Extensions
{
	public class WeakEventHandler<E> where E : EventArgs
	{
		private delegate void EventHandlerThunk(object @this, object sender, E e);
		private static int _NextThunkID = 1;

		private WeakReference _TargetRef;
		private EventHandlerThunk _Thunk;
		private EventHandler<E> _Handler;
		public int Id { get; private set; }

		public WeakEventHandler(EventHandler<E> eventHandler)
		{
			_TargetRef = new WeakReference(eventHandler.Target);
			_Thunk = CreateDynamicThunk(eventHandler);
			_Handler = Invoke;
		}

		public void Invoke(object sender, E e)
		{
			object target = _TargetRef.Target;
			if (target != null)
				_Thunk(target, sender, e);
		}

		public bool IsAlive()
		{
			object target = _TargetRef.Target;
			return (_TargetRef.IsAlive && target != null);
		}

		public static implicit operator EventHandler<E>(WeakEventHandler<E> weh)
		{
			return weh._Handler;
		}

		public static implicit operator WeakEventHandler<E>(EventHandler<E> handler)
		{
			return new WeakEventHandler<E>(handler);
		}

		private EventHandlerThunk CreateDynamicThunk(EventHandler<E> eventHandler)
		{
			MethodInfo method = eventHandler.Method;
			Type declaringType = method.DeclaringType;

			Id = System.Threading.Interlocked.Increment(ref _NextThunkID);
			DynamicMethod dm = new DynamicMethod("EventHandlerThunk" + Id, typeof(void),
			  new Type[] { typeof(object), typeof(object), typeof(E) }, declaringType);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);// load and cast "this" pointer
			il.Emit(OpCodes.Castclass, declaringType);
			il.Emit(OpCodes.Ldarg_1);// load arguments...
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);// call method
			il.Emit(OpCodes.Ret);// done , emit IL return.

			return (EventHandlerThunk)dm.CreateDelegate(typeof(EventHandlerThunk));
		}
	}
}
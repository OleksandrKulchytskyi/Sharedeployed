using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public delegate void UnregisterWeakCallback<E>(EventHandler<E> eventhandler) where E : EventArgs;

	public interface IWeakEventHandler<E> where E : EventArgs
	{
		EventHandler<E> Handler { get; }
	}

	/// <summary>
	/// Provides methods for creating WeakEvent handlers
	/// </summary>
	/// <typeparam name="T">The type of the event source</typeparam>
	/// <typeparam name="E">The EventArgs</typeparam>
	public class WeakEventHandler<T, E> : IWeakEventHandler<E>
		where T : class
		where E : EventArgs
	{
		#region Data
		private delegate void OpenEventHandler(T @this, object sender, E e);
		private WeakReference _TargetRef;
		private OpenEventHandler _OpenHandler;
		private EventHandler<E> _Handler;
		private UnregisterWeakCallback<E> _Unregister;
		#endregion

		#region Ctor
		/// <summary>
		/// Constructs a new WeakEventHandler
		/// </summary>
		/// <param name="eventHandler">The Event handler</param>
		/// <param name="unregister">Unregister delegate</param>
		public WeakEventHandler(EventHandler<E> eventHandler, UnregisterWeakCallback<E> unregister)
		{
			_TargetRef = new WeakReference(eventHandler.Target);
			_OpenHandler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler),
			  null, eventHandler.Method);
			_Handler = Invoke;
			_Unregister = unregister;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Invokes the event handler if the source is still alive
		/// </summary>
		public void Invoke(object sender, E e)
		{
			T target = (T)_TargetRef.Target;

			if (target != null)
				_OpenHandler.Invoke(target, sender, e);
			else if (_Unregister != null)
			{
				_Unregister(_Handler);
				_Unregister = null;
			}
		}

		public EventHandler<E> Handler
		{
			get { return _Handler; }
		}

		public static implicit operator EventHandler<E>(WeakEventHandler<T, E> weh)
		{
			return weh._Handler;
		}
		#endregion
	}

	/// <summary>
	/// Provides extension method for EventHandler&lt;E&gt;
	/// </summary>
	/// <example>
	/// <![CDATA[
	/// 
	///    //SO DECLARE LISTENERS LIKE
	///    workspace.CloseWorkSpace +=
	///        new EventHandler<EventArgs>(OnCloseWorkSpace).
	///           MakeWeak(eh => workspace.CloseWorkSpace -= eh);
	///           
	///    private void OnCloseWorkSpace(object sender, EventArgs e)
	///    {
	///
	///    }
	///    
	///    //OR YOU COULD CREATE ACTUAL EVENTS LIKE
	///    public class EventProvider
	///    {
	///         private EventHandler<EventArgs> closeWorkSpace;
	///         public event EventHandler<EventArgs> CloseWorkSpace
	///         {
	///             add
	///             {
	///                 closeWorkSpace += value.MakeWeak(eh => closeWorkSpace -= eh);
	///             }
	///             remove
	///             {
	///             }
	///         }
	///    }
	/// ]]>
	/// </example>
	public static class WeakEventHelper
	{
		#region EventHandler<E> extensions
		/// <summary>
		/// Sxtesion method for EventHandler<E>
		/// </summary>
		/// <typeparam name="E">The type</typeparam>
		/// <param name="eventHandler">The EventHandler</param>
		/// <param name="unregister">EventHandler unregister delegate</param>
		/// <returns>An EventHandler</returns>
		public static EventHandler<E> MakeWeak<E>(this EventHandler<E> eventHandler,
			UnregisterWeakCallback<E> unregister) where E : EventArgs
		{
			if (eventHandler == null)
				throw new ArgumentNullException("eventHandler");

			if (eventHandler.Method.IsStatic || eventHandler.Target == null)
				throw new ArgumentException("Only instance methods are supported.", "eventHandler");

			Type wehType = typeof(WeakEventHandler<,>).MakeGenericType(eventHandler.Method.DeclaringType, typeof(E));

			ConstructorInfo wehConstructor = wehType.GetConstructor(new Type[] { 
					typeof(EventHandler<E>),
					typeof(UnregisterWeakCallback<E>)
			});

			IWeakEventHandler<E> weh = (IWeakEventHandler<E>)wehConstructor.Invoke(new object[] { eventHandler, unregister });

			return weh.Handler;
		}
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ShareDeployed.Mailgrabber.Behaviour
{
	internal class PassworBoxBehavior : System.Windows.Interactivity.Behavior<PasswordBox>
	{
		public string PasswordValue
		{
			get { return (string)GetValue(PasswordValueProperty); }
			set { SetValue(PasswordValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PasswordValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PasswordValueProperty =
			DependencyProperty.Register("PasswordValue", typeof(string), typeof(PassworBoxBehavior), new PropertyMetadata(string.Empty));


		protected override void OnAttached()
		{
			this.AssociatedObject.PasswordChanged += AssociatedObject_PasswordChanged;
			base.OnAttached();
		}

		private void AssociatedObject_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
		{
			this.PasswordValue = (sender as PasswordBox).Password;
		}

		protected override void OnDetaching()
		{
			this.AssociatedObject.PasswordChanged -= AssociatedObject_PasswordChanged;
			base.OnDetaching();
		}
	}
}

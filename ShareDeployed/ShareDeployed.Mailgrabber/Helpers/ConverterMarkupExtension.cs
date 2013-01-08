using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public abstract class ConverterMarkupExtension<T> : MarkupExtension, IValueConverter, IMultiValueConverter where T : class,new()
	{
		private static T _converter = null;

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (_converter == null)
			{
				_converter = new T();
			}

			return _converter;
		}


		#region IValueConverter Members

		public virtual object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}

		public virtual object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}

		#endregion

		#region IMultiValueConverter Members
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}

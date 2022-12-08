﻿using System;
namespace AberrantSMPP
{
	internal class OptionsMonitor<T> : Microsoft.Extensions.Options.IOptionsMonitor<T>
	{
		private readonly T options;

		public OptionsMonitor(T options)
		{
			this.options = options;
		}

		public T CurrentValue => options;

		public T Get(string name) => options;

		public IDisposable OnChange(Action<T, string> listener) => new NullDisposable();

		private class NullDisposable : IDisposable
		{
			public void Dispose() { }
		}
	}

}


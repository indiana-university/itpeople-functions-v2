using System;

namespace web
{
	public class DisposableLoader : IDisposable
	{
		Action<bool> _setLoaderState;
		Action _stateChanged;
		
		public DisposableLoader(Action<bool> setLoaderState, Action stateChanged)
		{
			_stateChanged = stateChanged;
			_setLoaderState = setLoaderState;
			_setLoaderState(true);
			_stateChanged();
		}

		public void Dispose()
		{
			_setLoaderState(false);
			_stateChanged();
		}
	}
}
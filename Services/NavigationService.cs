using System;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;

namespace DevDeck.Services
{
    public sealed class NavigationService : INavigationService
    {
        private Frame? _frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public void Initialize(Frame frame)
        {
            _frame = frame;
        }

        public bool Navigate(Type pageType, object? parameter = null)
        {
            if (_frame == null) return false;
            return _frame.Navigate(pageType, parameter);
        }

        public void GoBack()
        {
            _frame?.GoBack();
        }
    }
}

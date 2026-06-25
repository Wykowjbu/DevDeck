using System;
using Microsoft.UI.Xaml.Controls;

namespace DevDeck.Contracts
{
    public interface INavigationService
    {
        void Initialize(Frame frame);
        bool Navigate(Type pageType, object? parameter = null);
        void GoBack();
        bool CanGoBack { get; }
    }
}

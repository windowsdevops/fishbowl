using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ClientManager.View
{
    public class EventResourceDictionary : ResourceDictionary
    {
        public void OnNavigateToContentButtonClicked(object sender, RoutedEventArgs args)
        {
            ServiceProvider.ViewManager.NavigateToContent(sender);
        }
    }
}

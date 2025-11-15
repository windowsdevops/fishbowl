
using System.Windows.Threading;
namespace ClientManager.View
{
    public class HomePage
    {
        private class _Navigator : Navigator 
        {
            public _Navigator(Navigator parent, HomePage page, Dispatcher dispatcher)
                : base(page, "[homepage]", parent)
            { }
        }

        public Navigator GetNavigator(Navigator parent, Dispatcher dispatcher)
        {
            return new _Navigator(parent, this, dispatcher);
        }
    }

}

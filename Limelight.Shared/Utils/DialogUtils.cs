using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace Limelight.Utils
{
    public class DialogUtils
    {
        public static void DisplayDialog(CoreDispatcher dispatcher, string content, string title)
        {
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(content, title);
                await dialog.ShowAsync();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            DisplayDialog(dispatcher, content, title, null);
        }

        public static void DisplayDialog(CoreDispatcher dispatcher, string content, string title, UICommandInvokedHandler okHandler)
        {
            Debug.WriteLine("Dialog requested: " + title + " - " + content);

            var unused = dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new MessageDialog(content, title);

                if (okHandler != null)
                {
                    dialog.Commands.Add(new UICommand("OK", okHandler));
                }
                else
                {
                    dialog.Commands.Add(new UICommand("OK"));
                }

                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 0;

                await dialog.ShowAsync();
            });
        }
    }
}

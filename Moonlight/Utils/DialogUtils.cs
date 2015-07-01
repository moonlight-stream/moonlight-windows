using System;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace Moonlight.Utils
{
    /// <summary>
    /// Utility class for displaying a dialog box
    /// </summary>
    public class DialogUtils
    {
        /// <summary>
        /// Display a dialog box
        /// </summary>
        /// <param name="dispatcher">UI thread's dispatcher</param>
        /// <param name="content">Message box content</param>
        /// <param name="title">Message box title</param>
        public static void DisplayDialog(CoreDispatcher dispatcher, string content, string title)
        {
            DisplayDialog(dispatcher, content, title, null);
        }

        /// <summary>
        /// Display a dialog box with a custom action when the user presses "OK"
        /// </summary>
        /// <param name="dispatcher">UI thread's dispatcher</param>
        /// <param name="content">Message box content</param>
        /// <param name="title">Message box title</param>
        /// <param name="okHandler">Callback executed when the user presses "OK"</param>
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

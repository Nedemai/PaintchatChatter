using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PaintchatChatter
{
    static class Program
    {
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChatWindow());


           




        }
    }
}

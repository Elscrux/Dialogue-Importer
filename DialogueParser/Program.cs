using System;
using System.Windows.Forms;


namespace DialogueParser
{
    static class Program
    {
        [STAThread]

        static void Main() {
            Application.EnableVisualStyles();
            
            Form1 form = new Form1();
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            Application.Run(form);
        }
    }
}

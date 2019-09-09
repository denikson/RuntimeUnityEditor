using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RuntimeUnityEditor.UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Shown += OnShown;
        }

        private void OnShown(object sender, EventArgs e)
        {
            RuntimeUnityEditor.InitializeConnection();
            RuntimeUnityEditor.Service.Echo("Hello from UI!");
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            RuntimeUnityEditor.Service.Echo(textBox1.Text);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPSaveConverter
{
    public partial class EditBaseLocationForm : Form
    {
        public string BaseLocation
        {
            get { return this.textBox1.Text; }
            set { this.textBox1.Text = value; }
        }
        public EditBaseLocationForm()
        {
            InitializeComponent();
        }
    }
}

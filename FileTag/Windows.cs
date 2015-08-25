using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System;

namespace FileTag
{
    public partial class ColumnHeaderWindow : System.Windows.Forms.Form
    {
        ProgramInformation prog_info;

        Dictionary<String, ListViewItem> header_items;

        public ColumnHeaderWindow(ProgramInformation prog_info)
        {
            this.prog_info = prog_info;

            InitializeComponent();

            GetSetHeaders();

        }

        private void InitializeComponent()
        {

            SuspendLayout();

            // Window Settings
            ClientSize = new System.Drawing.Size(250, 370);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            MinimizeBox = false;
            CancelButton = chwCancel;
            AcceptButton = chwOkay;

            Name = "ColumnHeaderWindow";
            Text = "Choose Column Headers...";

            // Controls
            //Splitter
            chwSplit = new TableLayoutPanel();
            chwSplit.SuspendLayout();

            chwSplit.ColumnCount = 2;
            chwSplit.RowCount = 2;
            chwSplit.Dock = DockStyle.Fill;

            chwSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Header List
            headerList = new ListView();
            headerList.CheckBoxes = true;
            headerList.View = View.List;
            headerList.MultiSelect = true;

            header_items = new Dictionary<string, ListViewItem>();

            foreach (String header in MainWindow.headers)
            {
                ListViewItem item = new ListViewItem(header);
                header_items.Add(header, item);
                headerList.Items.Add(item);
            }


            headerList.Dock = DockStyle.Fill;
            headerList.Height = 330;

            // Buttons
            int button_width = 100;
            int button_height = 30;
            int button_pad_side = 10;

            chwOkay = new Button();
            //chwOkay.Dock = DockStyle.Fill;
            chwOkay.Text = "Okay";
            chwOkay.Margin = new Padding(button_pad_side, 0, 0, 0);
            chwOkay.Height = button_height;
            chwOkay.Width = button_width;
            chwOkay.Click += new EventHandler(chw_okay_Click);

            chwCancel = new Button();
            //chwCancel.Dock = DockStyle.Fill;
            chwCancel.Text = "Cancel";
            chwCancel.Margin = new Padding(0, 0, button_pad_side, 0);
            chwCancel.Height = button_height;
            chwCancel.Width = button_width;
            chwCancel.Click += new EventHandler(chw_cancel_Click);

            // Add Controls to split
            chwSplit.Controls.Add(headerList, 0, 0);
            chwSplit.SetColumnSpan(headerList, 2);
            chwSplit.Controls.Add(chwOkay, 0, 1);
            chwSplit.Controls.Add(chwCancel, 1, 1);

            Controls.Add(chwSplit);

            chwSplit.ResumeLayout();
            ResumeLayout();

        }

        private TableLayoutPanel chwSplit;
        private ListView headerList;
        private Button chwOkay;
        private Button chwCancel;


        private void GetSetHeaders()
        {
            foreach (String header in prog_info.column_headers)
                header_items[header].Checked = true;
        }

        private void chw_cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void chw_okay_Click(object sender, EventArgs e)
        {
            prog_info.column_headers = new List<String>();
            int index = 0;
            foreach (ListViewItem item in headerList.CheckedItems)
            {
                prog_info.column_headers.Insert(index, item.Text);
                index++;
            }

            Close();
        }
        
    }
    public partial class TagFromFileParse : Form
    {

        public TagFromFileParse()
        {

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            SuspendLayout();

            // Window Settings
            ClientSize = new System.Drawing.Size(400, 150);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            MinimizeBox = false;
            CancelButton = fp_cancel;
            AcceptButton = fp_okay;
            

            Name = "File Parse Style";
            Text = "Choose Parse Style...";

            // Controls
            parse_template = new TextBox();
            parse_template.Width = 360;
            parse_template.Location = new Point(20, 30);
            parse_template.KeyDown += new KeyEventHandler(pt_key_press);

            fp_cancel = new Button();
            fp_cancel.Text = "Cancel";
            fp_cancel.Width = 60;
            fp_cancel.Location = new Point(100, 100);
            fp_cancel.Click += new EventHandler(fp_click_cancel);

            fp_okay = new Button();
            fp_okay.Text = "Okay";
            fp_okay.Width = 60;
            fp_okay.Location = new Point(20, 100);
            fp_okay.Click += new EventHandler(fp_click_okay);

            this.Controls.Add(parse_template);
            this.Controls.AddRange(new Control[] { fp_okay, fp_cancel });
            ResumeLayout();
        }

        private TextBox parse_template;

        private Button fp_okay;
        private Button fp_cancel;

        public string parse
        {
            get;
            private set;
        }

        private void fp_click_cancel(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void fp_click_okay(object sender, EventArgs e)
        {
            parse = parse_template.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void pt_key_press(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    fp_click_okay(this, (KeyEventArgs)e);
                    return;
            }

            return;
        }
    }

    // From Stackoverflow user L.B
    // http://stackoverflow.com/questions/7844306/can-i-detect-if-a-user-right-clicked-on-a-listview-column-header-in-winforms
    public partial class RightClickableListView : ListView
    {
        public RightClickableListView() { }

        public delegate void ColumnContextMenuHandler(object sender, System.Drawing.Point loc);
        public event ColumnContextMenuHandler ColumnContextMenuClicked;

        bool _OnItemsArea = false;
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _OnItemsArea = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _OnItemsArea = false;
        }

        // Right-click
        const int WM_CONTEXTMENU = 0x007B;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CONTEXTMENU && !_OnItemsArea)
                ColumnContextMenuClicked(this, base.PointToClient(MousePosition));

            base.WndProc(ref m);
        }
    }
}

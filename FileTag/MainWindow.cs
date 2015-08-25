// File Tag
// Main Window
// Matt Stone

using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using TagReader;

namespace FileTag
{
    partial class MainWindow : System.Windows.Forms.Form
    {
        ProgramInformation prog_info;

        #region Headers and Properties
        public static String[] headers = new String[]
                {
                    "Filename",
                    "Path",
                    "Title",
                    "Artist",
                    "Album",
                    "Track",
                    "Discnumber",
                    "Year",
                    "Comment",
                    "Composer",
                    "BPM",
                    "Length",
                    "Size",
                    "Modified",
                    //"Tag",
                    //"Album Artist",
                    //"Genre",  
                    //"Bitrate",
                    //"Frequency",
                    //"VBR",
                    //"Mode",
                };

        public static String[] properties = new String[]
                {
                    "Track",
                    "Title",
                    "Album",
                    "Artist",
                    "Discnumber",
                    "Year",
                    "Comment",
                };

        #endregion

        public MainWindow()
        {
            // Create Program Info
            prog_info = new ProgramInformation();

            // Init Window
            InitializeComponent();

            RefreshFileList();

        }

        private void InitializeComponent()
        {

            //this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            #region Bars
            // Component init for various toolbars

            // Status Bar
            // Bottom bar of application
            statusBar = new StatusStrip();
            statusBar.Name = "statusBar";
            statusBar.Location = new Point(0, 590);

            // Tool Bar
            // Main toolbar
            toolStrip = new ToolStrip();
            toolStrip.Location = new Point(0, 24);
            toolStrip.Name = "toolStrip";
            toolStrip.Items.Add("Write Tag", null, click_write_tag);
            toolStrip.Items.Add("Tag from File", null, click_tag_from_file);
            toolStrip.Items.Add("Tag from Filename", null, click_tag_from_filename);


            // Menu Bar
            // Top bar of application
            menuBar = new MenuStrip();
            menuBar.SuspendLayout();
            menuBar.Name = "menuBar";
            menuBar.Location = new Point(0, 0);

            #region Menu Bar Items

            //  File
            mb_file = new ToolStripMenuItem();
            mb_file.Name = "mb_file";
            mb_file.Text = "File";

            //      Open
            mb_file_open = new ToolStripMenuItem();
            mb_file_open.Name = "mb_file_open";
            mb_file_open.Text = "Open File(s)";
            mb_file_open.Click += new EventHandler(mb_file_open_Click);

            //      Exit
            mb_file_exit = new ToolStripMenuItem();
            mb_file_exit.Name = "mb_file_exit";
            mb_file_exit.Text = "Exit";
            mb_file_exit.Click += new EventHandler(mb_file_exit_Click);

            mb_file.DropDownItems.AddRange(new ToolStripItem[] {
                mb_file_open,
                mb_file_exit});

            // View
            mb_view = new ToolStripMenuItem();
            mb_view.Name = "mb_view";
            mb_view.Text = "View";

            //      Columns
            mb_view_columns = new ToolStripMenuItem();
            mb_view_columns.Name = "mb_view_columns";
            mb_view_columns.Text = "Columns...";
            mb_view_columns.Click += new EventHandler(mb_view_columns_Click);

            mb_view.DropDownItems.AddRange(new ToolStripItem[] {
                mb_view_columns});

            //  Help
            mb_help = new ToolStripMenuItem();
            mb_help.Name = "mb_help";
            mb_help.Text = "Help";

            //      About
            mb_help_about = new ToolStripMenuItem();
            mb_help_about.Name = "mb_help";
            mb_help_about.Text = "About";

            mb_help.DropDownItems.AddRange(new ToolStripItem[] {
                mb_help_about});


            menuBar.Items.AddRange(new ToolStripItem[] {
                mb_file,
                mb_view,
                mb_help});

            #endregion

            #endregion

            #region Main Interface
            // Main window components 

            // Main window splitter
            mainSplit = new SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(mainSplit)).BeginInit();
            mainSplit.Panel1.SuspendLayout();
            mainSplit.Panel2.SuspendLayout();
            mainSplit.SuspendLayout();

            mainSplit.BorderStyle = BorderStyle.Fixed3D;
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.Location = new Point(0, 49);
            mainSplit.Name = "mainSplit";

            // Control Side
            mainSplit.Panel1MinSize = 300;

            // File Side
            mainSplit.Panel2.Controls.Add(fileList);
            mainSplit.Size = new Size(984, 541);
            mainSplit.SplitterDistance = 300;
            mainSplit.TabIndex = 3;

            // File tag controls
            controlGrid = new DataGridView();
            controlGrid.Dock = DockStyle.Fill;
            controlGrid.AllowUserToAddRows = false;
            controlGrid.CellValueChanged += new DataGridViewCellEventHandler(control_grid_value_change);
            controlGrid.RowHeadersVisible = false;

            DataGridViewColumn cg_property = new DataGridViewTextBoxColumn();
            cg_property.HeaderText = "Property";
            cg_property.ReadOnly = true;
            cg_property.SortMode = DataGridViewColumnSortMode.NotSortable;

            DataGridViewColumn cg_value = new DataGridViewTextBoxColumn();
            cg_value.HeaderText = "Value";
            cg_value.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            cg_value.SortMode = DataGridViewColumnSortMode.NotSortable;

            controlGrid.Columns.AddRange(new DataGridViewColumn[] {cg_property, cg_value});

            foreach (String property in properties)
                controlGrid.Rows.Add(new String[] { property, "" });



            // File list view
            fileList = new RightClickableListView();

            fileList.Name = "fileList";
            fileList.AllowColumnReorder = true;
            fileList.AllowDrop = true; // Drag-n-Drop
            fileList.Dock = DockStyle.Fill;
            fileList.Location = new Point(0, 0);
            fileList.View = View.Details; // Details View
            fileList.FullRowSelect = true;
            fileList.MultiSelect = true;
            fileList.HideSelection = false;
            fileList.KeyDown += new KeyEventHandler(fileList_key_press);
            fileList.ColumnContextMenuClicked += new RightClickableListView.ColumnContextMenuHandler(fileList_right_click);
            fileList.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(fileList_selection);

            #endregion

            #region Dialogs
            // Dialog components

            openFileDialog = new OpenFileDialog();
            openFileDialog.FileName = "openFileDialog";

            #endregion

            #region Menus
            headerContext = new ContextMenu();

            foreach (String header in MainWindow.headers)
            {
                MenuItem item = new MenuItem(header);
                item.Click += new EventHandler(header_menu_item_click);
                headerContext.MenuItems.Add(item);
            }
            

            #endregion

            #region Main Window
            // Settings for main window

            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 612);
            MinimumSize = new Size(700, 500);

            Name = "MainWindow";
            Text = "File Tag";

            #endregion

            #region Add Controls

            Controls.Add(mainSplit);
            Controls.Add(toolStrip);
            Controls.Add(statusBar);
            Controls.Add(menuBar);
            MainMenuStrip = menuBar;

            mainSplit.Panel1.Controls.Add(controlGrid);
            mainSplit.Panel2.Controls.Add(fileList);

            #endregion

            #region Resume Layouts

            menuBar.ResumeLayout(false);
            menuBar.PerformLayout();

            mainSplit.Panel1.ResumeLayout(false);
            mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(mainSplit)).EndInit();
            mainSplit.ResumeLayout(false);

            ResumeLayout(false);
            PerformLayout();

            #endregion

        }

        #region Component Declarations

        // Bars
        private StatusStrip statusBar;

        // Menu Bar
        private MenuStrip menuBar;

        #region Menu Bar Items

        private ToolStripMenuItem mb_file;
        private ToolStripMenuItem mb_file_open;
        private ToolStripMenuItem mb_file_exit;

        private ToolStripMenuItem mb_view;
        private ToolStripMenuItem mb_view_columns;

        private ToolStripMenuItem mb_help;
        private ToolStripMenuItem mb_help_about;

        private ToolStrip toolStrip;

        #endregion

        // Menus
        private ContextMenu headerContext;

        // Main Window
        private SplitContainer mainSplit;

        private DataGridView controlGrid;

        private RightClickableListView fileList;

        // Dialogs
        private OpenFileDialog openFileDialog;

        #endregion
    }


    // Running Functions
    partial class MainWindow
    {
        private void RefreshFileList()
        {
            statusBar.Text = "Loading Files...";
            fileList.Items.Clear();
            controlGrid.Rows.Clear();

            List<String> current_headers = new List<String>();
            foreach (ColumnHeader column in fileList.Columns)
                current_headers.Add(column.Text);

            // Re-populate Header Columns
            foreach (String header in prog_info.column_headers)
                if (!current_headers.Contains(header))
                    fileList.Columns.Add(header);

            foreach (ColumnHeader column in fileList.Columns)
                if (!prog_info.column_headers.Contains(column.Text))
                    fileList.Columns.Remove(column);

            // Re-populate file list
            foreach (String filename in prog_info.open_files.Keys)
            {
                TagReader.File file = prog_info.open_files[filename];

                // Get file info from file
                file.updateFileInfo();
                Dictionary<String, String> file_info = file.file_info;

                // Add to list view
                String[] list_row = new String[prog_info.column_headers.Count];
                for (int i = 0; i < list_row.Length; i++)
                    list_row[i] = file_info[prog_info.column_headers[i]];

                ListViewItem item = new ListViewItem(list_row[0]);
                item.SubItems.AddRange(TagReader.File.Subset(list_row, 1, list_row.Length - 1));
                item.Tag = filename;

                fileList.Items.Add(item);                
            }

            statusBar.Text = "Done.";         
        }
        private void RemoveSelectedFiles()
        {
            statusBar.Text = "Removing Files";

            foreach (ListViewItem item in fileList.SelectedItems)
            {
                prog_info.open_files[(String)item.Tag].closeFile();
                prog_info.open_files.Remove((String)item.Tag);
            }

            RefreshFileList();
        }
        private void UpdateTagsFromFile(String filename, Tuple<String, List<String>> regex_info)
        {
            String regex = regex_info.Item1;
            List<String> regex_groups = regex_info.Item2;

            StreamReader stream = new StreamReader(filename);
            String line;
            while ((line = stream.ReadLine()) != null)
            {
                // Match regex to file input
                Match match = Regex.Match(line, regex);
                if (match.Groups.Count == regex_groups.Count + 1)
                    // Look for tracks with corresponding unique identifiers
                    foreach (String id_prop in new String[] {"Track", "Title"})
                        if (regex_groups.Contains(id_prop))
                            foreach (ListViewItem item in fileList.SelectedItems)
                            {
                                TagReader.File file = prog_info.open_files[(String)item.Tag];
                                // Check for same file as input data
                                if (file.tag.getProperty(id_prop) == match.Groups[regex_groups.IndexOf(id_prop) + 1].Value)
                                    foreach (String prop in regex_groups)
                                        file.tag.writeProperty(prop, match.Groups[regex_groups.IndexOf(prop) + 1].Value);

                            }

            }

            stream.Close();
            RefreshFileList();

        }
        private void UpdateTagsFromFilename(Tuple<String, List<String>> regex_info)
        {
            String regex = regex_info.Item1;
            List<String> regex_groups = regex_info.Item2;

            foreach (ListViewItem item in fileList.SelectedItems)
            {
                Match match = Regex.Match(prog_info.open_files[(string)item.Tag].file_info["Filename"], regex);
                if (match.Groups.Count == regex_groups.Count + 1)
                    foreach (String prop in regex_groups)
                        prog_info.open_files[(String)item.Tag].tag.writeProperty(prop, match.Groups[regex_groups.IndexOf(prop) + 1].Value);
            }

            RefreshFileList();
        }
        private Tuple<String, List<String>> GetParseString(String parse)
        {
            statusBar.Text = "Paring Files";
            Dictionary<char, String> parse_types = new Dictionary<char, string>
            {
                {'t', "Track"},
                {'T', "Title"},
                {'A', "Artist"},
                {'a', "Album"},
                {'y', "Year"},
                {'d', "Discnumber"},
            };

            String regex = "^";
            List<String> regex_groups = new List<String>();
            int index = 0;

            while (index < parse.Length)
            {
                if (parse[index] == '%')
                {
                    try
                    {
                        index++;
                        regex_groups.Add(parse_types[parse[index]]);
                        regex += "(.*)";
                    }
                    catch (KeyNotFoundException e)
                    {
                        throw new NotImplementedException();
                    }
                    index++;
                }

                else if (parse[index] == '\\')
                {
                    if (index + 1 < parse.Length && parse[index + 1] == '%')
                    {
                        regex += '%';
                        index++;
                    }
                    else
                        regex += '\\';

                    index++;
                }

                while (index < parse.Length && parse[index] != '%' && parse[index] != '\\')
                {
                    regex += parse[index];
                    index++;
                }
            }

            regex += "$";

            if (!regex_groups.Contains("Track") && !regex_groups.Contains("Title"))
                throw new Exception("Parse string must have unique property");

            return Tuple.Create(regex, regex_groups);
        }
    }

    // Store Running Information
    public class ProgramInformation
    {
        public Dictionary<String, TagReader.File> open_files;
        public List<String> column_headers;

        public ProgramInformation()
        {
            open_files = new Dictionary<String, TagReader.File>();
            column_headers = new List<String> { "Track", "Title", "Album", "Artist" };
        }
    }

}
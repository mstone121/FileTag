// File Tag
// Event Handlers
// Matt Stone

using System;
using System.Windows;
using System.Windows.Forms;
using System.Collections.Generic;
using TagReader;

namespace FileTag
{
    partial class MainWindow
    {
        #region Menu Bar Items

        private void mb_file_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void mb_file_open_Click(object sender, EventArgs e)
        {
            // Open dialog
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (String filename in openFileDialog.FileNames)
                    if (!prog_info.open_files.ContainsKey(filename))
                        prog_info.open_files.Add(filename, new TagReader.File(filename));

                RefreshFileList();
            }
        }

        private void mb_view_columns_Click(object sender, EventArgs e)
        {
            ColumnHeaderWindow chw = new ColumnHeaderWindow(prog_info);

            chw.Owner = this;

            chw.ShowDialog();

            RefreshFileList();
        }

        #endregion

        private void fileList_key_press(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    RemoveSelectedFiles();
                    break;
            }
        }
        private void fileList_right_click(object sender, System.Drawing.Point loc)
        {
            foreach (MenuItem item in headerContext.MenuItems)
                if (prog_info.column_headers.Contains(item.Text))
                    item.Checked = true;

            headerContext.Show(fileList, loc);
        }
        private void header_menu_item_click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item.Checked)
            {
                prog_info.column_headers.Remove(item.Text);
                item.Checked = false;
            }
            else
            {
                prog_info.column_headers.Add(item.Text);
                item.Checked = true;
            }
            RefreshFileList();
        }
        private void fileList_selection(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (fileList.SelectedItems.Count > 0)
            {
                controlGrid.Rows.Clear();
                Dictionary<String, String> final_values = new Dictionary<string, string>();
                foreach (String property in properties)
                    foreach (ListViewItem item in fileList.SelectedItems)
                    {
                        String current_prop = prog_info.open_files[(String)item.Tag].tag.getProperty(property);
                        if (!final_values.ContainsKey(property))
                            final_values.Add(property, current_prop);
                        else if (final_values[property] != current_prop)
                            final_values[property] = "(different)";
                    }

                foreach (String property in properties)
                    controlGrid.Rows.Add(new String[] { property, final_values[property] });
            }
        }

        private void control_grid_value_change(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = controlGrid.Rows[e.RowIndex];
            String property = (String)row.Cells[0].Value;
            String value    = (String)row.Cells[1].Value;

            // Can't write null values
            if (value == null)
                value = "";

            foreach (ListViewItem item in fileList.SelectedItems)
            {
                File file = prog_info.open_files[(String)item.Tag];
                file.tag.writeProperty(property, value);
            }
            
            RefreshFileList();
        }

        private void click_write_tag(object sender, EventArgs e)
        {
            foreach (ListViewItem item in fileList.SelectedItems)
                prog_info.open_files[(String)item.Tag].writeTag();

            RefreshFileList();
        }
        private void click_tag_from_file(object sender, EventArgs e)
        {
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                TagFromFileParse tfp = new TagFromFileParse();

                if (tfp.ShowDialog() == DialogResult.OK)
                    UpdateTagsFromFile(openFileDialog.FileName, GetParseString(tfp.parse));

            }
        }
        private void click_tag_from_filename(object sender, EventArgs e)
        {
            TagFromFileParse tfp = new TagFromFileParse();

            if (tfp.ShowDialog() == DialogResult.OK)
                UpdateTagsFromFilename(GetParseString(tfp.parse));
        }
    }
}

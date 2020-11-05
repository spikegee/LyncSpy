using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LyncTracker.Logic;
using LyncTracker.Properties;
using Microsoft.Lync.Model;

namespace LyncTracker
{
    public partial class MainForm : Form
    {
        Tracker tracker;
        public static readonly string mailFile = @"mails.txt";

        public MainForm()
        {
            InitializeComponent();
            InitializeImageList();
            InitializeGroups();
            InitializeCheckList();
            this.Opacity = 0;
            this.Shown += MainForm_Shown;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            StartCallBack startCallBack = new StartCallBack(start);
            this.BeginInvoke(startCallBack);
            HotKeyManager.RegisterHotKey(Keys.J, KeyModifiers.Control);
            HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;
            this.Visible = false;
            this.WindowState = FormWindowState.Minimized;
            // encrypt the files
            try
            {
                File.Encrypt(tbLog.Text);
                File.Encrypt(mailFile);
            } catch { }
        }

        private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            this.Visible = !this.Visible;
            if(this.Visible)
            { 
                this.Opacity = 100;
                this.WindowState = FormWindowState.Normal;
            }
        }

        void initializeMailList()
        {
            if (!File.Exists(mailFile))
                return;
            string line;
            using (var file = new StreamReader(mailFile))
                while ((line = file.ReadLine()) != null)
                    lvList.Items.Add(line, "grey");
        }

        void saveMailList()
        {
            using (var outputfile = new StreamWriter(mailFile))
                foreach (ListViewItem lvi in lvList.Items)
                    outputfile.WriteLine(lvi.Text);
        }

        void InitializeCheckList()
        {
            for(int i = 0; i < clbStatuses.Items.Count;++i)
                clbStatuses.Items[i].Checked = true;
        }

        void InitializeImageList()
        {
            ImageList il = new ImageList();
            il.Images.Add("grey", Resources.grey);
            il.Images.Add("yellow", Resources.yellow);
            il.Images.Add("red", Resources.red);
            il.Images.Add("green", Resources.green);
            lvList.SmallImageList = il;
        }

        void InitializeGroups()
        {
            //Send mail
            ControlsEnabler ctrlEnabler = new ControlsEnabler();
            ctrlEnabler.AddControl(tbSendEmail);
            cbSendActive.CheckedChanged += ctrlEnabler.CheckedChanged;
            //Save to log
            ctrlEnabler = new ControlsEnabler();
            ctrlEnabler.AddControl(tbLog);
            ctrlEnabler.AddControl(bSearch);
            cbSaveActive.CheckedChanged += ctrlEnabler.CheckedChanged;
            //
            cbSendActive.Checked = cbSaveActive.Checked = false;
        }

        private void bAdd_Click(object sender, EventArgs e)
        {
            if (tbEmail.Text.Length > 0)
                lvList.Items.Add(tbEmail.Text, "grey");
            tbEmail.Text = "";
            saveMailList();
        }

        delegate string StartCallBack();

        private string start ()
        {
            var errorMessage = canStart();
            if (errorMessage != null)
                return errorMessage;

            ChangeAdapter ca = new ChangeAdapter(this);
            try
            {
                tracker = new Tracker(ca);
                tracker.Start(ToStringList(lvList.Items), GetStatusList());
            }
            catch (Exception ex)
            {
                rbOn.Checked = false;
                ca.SetStatus("Off");
                ca = null;
                return "Lync not turned on";
            }
            return null;
        }

        private void stop()
        {
            if (tracker != null)
            {
                tracker.Stop();
                tracker = null;
            }
            tsslStatus.Text = "Off";
        }

        private string canStart()
        {
            if (cbSendActive.Checked && tbSendEmail.Text.Length == 0)
                return "Please input the email to which notifications are to be sent";
            if (cbSaveActive.Checked && tbLog.Text.Length == 0)
                return "Please input the csv log path to which notifications are to be save";
            return null;
        }

        private void rbOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rbOn.Checked)
            {
                var errorMessage = canStart();
                if (errorMessage != null)
                    MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                stop();
        }

        private List<ContactAvailability> GetStatusList()
        {
            List<ContactAvailability> statusList = new List<ContactAvailability>();
            System.Windows.Forms.ListView.CheckedListViewItemCollection s = clbStatuses.CheckedItems;
            IEnumerable<ListViewItem> checkedList = (IEnumerable<ListViewItem>)s.Cast<ListViewItem>();

            if (checkedList.Any(it => it.Text == "All"))
            {
                statusList.Add(ContactAvailability.Away);
                statusList.Add(ContactAvailability.Busy);
                statusList.Add(ContactAvailability.BusyIdle);
                statusList.Add(ContactAvailability.DoNotDisturb);
                statusList.Add(ContactAvailability.Free);
                statusList.Add(ContactAvailability.FreeIdle);
                statusList.Add(ContactAvailability.Invalid);
                statusList.Add(ContactAvailability.None);
                statusList.Add(ContactAvailability.Offline);
                statusList.Add(ContactAvailability.TemporarilyAway);
                return statusList;
            }
            if (checkedList.Any(it => it.Text == "Available"))
            {
                statusList.Add(ContactAvailability.Free);
                statusList.Add(ContactAvailability.FreeIdle);
            }
            if (checkedList.Any(it => it.Text == "Away"))
                statusList.Add(ContactAvailability.Away);
            if (clbStatuses.CheckedItems.ContainsKey("Busy"))
            {
                statusList.Add(ContactAvailability.Busy);
                statusList.Add(ContactAvailability.BusyIdle);
            }
            if (clbStatuses.CheckedItems.ContainsKey("Do not disturb"))
                statusList.Add(ContactAvailability.DoNotDisturb);
            return statusList;
        }

        List<string> ToStringList(System.Windows.Forms.ListView.ListViewItemCollection oc)
        {
            List<string> sList = new List<string>();
            foreach (ListViewItem lvi in oc)
                sList.Add(lvi.Text);
            return sList;
        }


        
        private void bRemove_Click(object sender, EventArgs e)
        {
            foreach(ListViewItem lvi in lvList.SelectedItems)
                lvList.Items.Remove(lvi);
            saveMailList();
        }


        private void bSearch_Click(object sender, EventArgs e)
        {
            sfdSave.Filter = "CSV (*.csv)|*.*";
            if (sfdSave.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                tbLog.Text = sfdSave.FileName.Contains(".csv") ? sfdSave.FileName : sfdSave.FileName+".csv";
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Lync.Model;

namespace LyncTracker.Logic
{
    class ChangeAdapter:ChangeInterface
    {
        char separator = ';';
        string firstRow = "Firstname;LastName;Status;IdleStartTime;ChangeDate;ActivityId";
        MainForm _mf;

        delegate void ImageCallBack(string email, ContactAvailability status);
        
        public ChangeAdapter(MainForm mf)
        {
            this._mf = mf;
            SendStatusChange("Started", "", "", ContactAvailability.None, new DateTime(), DateTime.Now, "");
        }

        private void ChangeImage(string email, ContactAvailability status)
        {
            if (_mf.lvList.InvokeRequired)
            {
                ImageCallBack d = new ImageCallBack(ChangeImage);
                _mf.Invoke(d, new object[] { email, status });
            }
            else
            {
                ListViewItem lvi = _mf.lvList.FindItemWithText(email);
                if (lvi == null) // requested email not found - typically: "Started" specific case
                    return;

                switch (status)
                {
                    case ContactAvailability.Offline:
                    case ContactAvailability.None:
                        lvi.ImageKey = "grey";
                        break;
                    case ContactAvailability.Away:
                        lvi.ImageKey = "yellow";
                        break;
                    case ContactAvailability.Busy:
                    case ContactAvailability.BusyIdle:
                    case ContactAvailability.DoNotDisturb:
                        lvi.ImageKey = "red";
                        break;
                    case ContactAvailability.Free:
                    case ContactAvailability.FreeIdle:
                        lvi.ImageKey = "green";
                        break;
                    default:
                        lvi.ImageKey = "grey";
                        break;
                }
            }
        }

        public void SendStatusChange(string email, ContactAvailability status)
        {
            ChangeImage(email, status);
        }

        public void SendStatusChange(string email, string firstName, string lastName, ContactAvailability status, DateTime idleStartTime, DateTime date, string activityId)
        {
            _mf.tsslChange.Text = "Changed " + date.ToLocalTime();
            if (_mf.cbSaveActive.Checked)
            {
                CSVManager m = new CSVManager(_mf.tbLog.Text, firstRow);
                m.WriteLine(firstName + separator + lastName + separator + status + separator + idleStartTime.ToLocalTime() + separator + date.ToLocalTime() + separator + activityId);
            }
            if (_mf.cbSendActive.Checked)
            {
                EmailManager em = new EmailManager();
                em.SendEmail("Lync Alert", firstName + " " + lastName + " changed status to " + status + " at " + date.ToLocalTime(), _mf.tbSendEmail.Text);
            }

            ChangeImage(email, status);
        }

        public void SetStatus(string status)
        {
            _mf.tsslStatus.Text = "On. Tracking Lync status";
        }

    }
}

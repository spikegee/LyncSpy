using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Extensibility;

namespace LyncTracker.Logic
{
    class Tracker
    {
        ChangeInterface _cn;
        List<Contact> _cList = new List<Contact>();
        LyncClient _lc = LyncClient.GetClient();
        List<ContactAvailability> _statusList;

        public Tracker(ChangeInterface cn)
        {
            _lc = LyncClient.GetClient();
            _cn=cn;

            _lc.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            _lc.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
    
        }

        void Callback(IAsyncResult ar, string requestedEmail)
        {
            try
            {
                Automation _Automation = LyncClient.GetAutomation();
                SearchResults sr = _lc.ContactManager.EndSearch(ar);
                Contact c = sr.Contacts.First();
                _cList.Add(c);
                c.ContactInformationChanged += (object sender, ContactInformationChangedEventArgs eventArgs) => c_ContactInformationChanged (sender, eventArgs, requestedEmail);
                getContactInfoAndNotify(c, requestedEmail);
            }
            catch
            {
            }
        }

        void ConversationManager_ConversationRemoved(object sender, ConversationManagerEventArgs e)
        {
            //MessageBox.Show("Removed");
            
        }

        void ConversationManager_ConversationAdded(object sender, ConversationManagerEventArgs e)
        {
            e.Conversation.ContextDataReceived += Conversation_ContextDataReceived;
            e.Conversation.ContextDataSent += Conversation_ContextDataSent;
            e.Conversation.ParticipantAdded +=Conversation_ParticipantAdded;
            //MessageBox.Show("Added");

        }

        void Conversation_ParticipantAdded(object sender, ParticipantCollectionChangedEventArgs e)
        {
            if (e.Participant.IsSelf)
            {
            }
            else
            {
                if (((Conversation)sender).Modalities.ContainsKey(ModalityTypes.InstantMessage))
                {
                    ((InstantMessageModality)e.Participant.Modalities[ModalityTypes.InstantMessage]).InstantMessageReceived += Tracker_InstantMessageReceived;

                }
            }
        }

        void Tracker_InstantMessageReceived(object sender, MessageSentEventArgs e)
        {
            //MessageBox.Show(e.Text);
        } 

        void Conversation_ContextDataSent(object sender, ContextEventArgs e)
        {
            //MessageBox.Show("Sent");
        }

        void Conversation_ContextDataReceived(object sender, ContextEventArgs e)
        {
            //MessageBox.Show("Received");
        }

        object getContactProperyNoThrow(Contact contact, ContactInformationType infoType)
        {
            object obj = null;
            try { obj = contact.GetContactInformation(infoType); }
            catch (COMException ex) { Console.WriteLine(ex.Message); }
            catch (ItemNotFoundException ex) { Console.WriteLine(ex.Message); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return obj;
        }

        void getContactInfoAndNotify(Contact contact, string requestedEmail)
        {
            ContactAvailability availEnum = (ContactAvailability)getContactProperyNoThrow(contact, ContactInformationType.Availability);
            if (!CheckStatus(availEnum))
                return;

            var activityString = getContactProperyNoThrow(contact, ContactInformationType.Activity);
            var customActivity = getContactProperyNoThrow(contact, ContactInformationType.CustomActivity);
            var locName = getContactProperyNoThrow(contact, ContactInformationType.LocationName);
            var title = getContactProperyNoThrow(contact, ContactInformationType.Title);
            var calState = getContactProperyNoThrow(contact, ContactInformationType.CurrentCalendarState);
            var timeZone = getContactProperyNoThrow(contact, ContactInformationType.TimeZone);
            var availID = getContactProperyNoThrow(contact, ContactInformationType.ActivityId);

            // to avoid having to look through the list of emails to find the one we are interested in, we'll just used the email saved in the callback
            var listEmails = (List<object>)getContactProperyNoThrow(contact, ContactInformationType.EmailAddresses);
            var idleStartTimeObj = getContactProperyNoThrow(contact, ContactInformationType.IdleStartTime);
            var idleStartTime = idleStartTimeObj == null ? new DateTime() : (DateTime)idleStartTimeObj;
            var lastName = (string)getContactProperyNoThrow(contact, ContactInformationType.LastName);
            var firstName = (string)getContactProperyNoThrow(contact, ContactInformationType.FirstName);
            _cn.SendStatusChange(requestedEmail, lastName, firstName, availEnum, idleStartTime, DateTime.Now.ToLocalTime());
        }

        void c_ContactInformationChanged(object sender, ContactInformationChangedEventArgs e, string requestedEmail)
        {
            if (e.ChangedContactInformation.Contains(ContactInformationType.Availability))
                getContactInfoAndNotify((Contact)sender, requestedEmail);
        }

        private bool CheckStatus(ContactAvailability availEnum)
        {  
            foreach (ContactAvailability ca in _statusList)
            {
                if (ca == availEnum)
                    return true;
            }
            return false;
        }

        public void Start(List<string> sList, List<ContactAvailability> statusList)
        {
            _statusList = statusList;
            foreach (string requestedEmail in sList)
            {
                IAsyncResult ar = _lc.ContactManager.BeginSearch(requestedEmail, (IAsyncResult ar_) => Callback(ar_, requestedEmail), null);
            }
            _cn.SetStatus("On");
        }

        public void Stop()
        {
            foreach (Contact c in _cList) ;
                // not sure how to remove the anonymous delegate created to store the requested email
                // c.ContactInformationChanged -= c_ContactInformationChanged;
        }
    }
}

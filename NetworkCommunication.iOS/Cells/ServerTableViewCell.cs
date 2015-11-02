
using System;

using Foundation;
using UIKit;
using NetworkCommunication.Core;

namespace NetworkCommunication.iOS
{
    public partial class ServerTableViewCell : UITableViewCell
    {
        public static readonly UINib Nib = UINib.FromName("ServerTableViewCell", NSBundle.MainBundle);
        public static readonly NSString Key = new NSString("ServerTableViewCell");

        public ServerTableViewCell(IntPtr handle)
            : base(handle)
        {
        }

        public static ServerTableViewCell Create()
        {
            return (ServerTableViewCell)Nib.Instantiate(null, null)[0];
        }

        public void Update(ServerInfo info)
        {
            lblTitle.Text = info.name;
        }
    }
}


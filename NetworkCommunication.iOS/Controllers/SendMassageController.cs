using System;
using UIKit;
using NetworkCommunication.Core;
using System.Text;
using System.Collections.Generic;
using Foundation;
using System.Linq;

namespace NetworkCommunication.iOS
{
	public partial class SendMassageController : UIViewController
	{
		public SendMassageController (IntPtr handle) : base (handle)
		{
            
		}
            
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();


            tblServerList.RegisterNibForCellReuse (ServerTableViewCell.Nib, ServerTableViewCell.Key);

            serverList = new List<ServerInfo>();

            var serverListSource = new ServerListTableViewSource (serverList);

            serverListSource.SelectedItem += ServerListSource_SelectedItem;

            tblServerList.Source = serverListSource;

            tblServerList.ReloadData ();


            btnConnect.TouchUpInside += BtnConnect_TouchUpInside;

            txtIPAddress.ShouldReturn = SearchShouldReturn;

            txtPort.ShouldReturn = SearchShouldReturn;

            txtMessage.ShouldReturn = SearchShouldReturn;


            socketClient = new SocketClient();

            socketClient.StateChanged += SocketClient_StateChanged;


            socketBroadcastServer = new SocketBroadcastServer();

            socketBroadcastServer.ReceivedMessage += SocketBroadcastServer_ReceivedMessage;

            socketBroadcastServer.Run(BROADCAST_PORT);


            socketBroadcastClient = new SocketBroadcastClient(BROADCAST_PORT);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            socketBroadcastClient.SendMessage(new byte[]{ 1 });
        }

        protected void ServerListSource_SelectedItem (object sender, ServerInfo e)
        {
            txtIPAddress.Text = e.address;
        }

        protected void BtnConnect_TouchUpInside (object sender, EventArgs e)
        {
            if (socketClient.State == SocketClientState.Disconnected)
            {
                int port = 6000;

                int.TryParse(txtPort.Text, out port);

                socketClient.Connect(txtIPAddress.Text, port);
            }
            else
            {
                socketClient.Disconnect();
            }          
        }

        protected void SendMessage()
        {
            if (socketClient.State == SocketClientState.Connected)
            {
                socketClient.SendMessage(Encoding.UTF8.GetBytes(txtMessage.Text));

                txtMessage.Text = string.Empty;
            }
        }

        protected void SocketClient_StateChanged (object sender, SocketClientState state)
        {
            InvokeOnMainThread(() =>
                {
                    switch (state)
                    {
                        case SocketClientState.Connected:

                            lblConnectStatus.Text = "Connected";

                            btnConnect.SetTitle("Disconnect", UIControlState.Normal);

                            lblConnectStatus.TextColor = UIColor.Green;

                            break;

                        case SocketClientState.Disconnected:

                            lblConnectStatus.Text = "Disconnected";

                            btnConnect.SetTitle("Connect", UIControlState.Normal);

                            lblConnectStatus.TextColor = UIColor.Red;

                            break;
                    }
                });
        }

        protected void SocketBroadcastServer_ReceivedMessage (object sender, ReceiveMessageEventArgs e)
        {
            InvokeOnMainThread(() =>
                {
                    var serverInfo = SerializationHelper.ToObject<ServerInfo>(e.Message);

                    if (serverInfo != null)
                    {
                        serverInfo.address = e.Host.Address;

                        if (serverInfo.started == ServerInfoState.Started)
                        {
                            if (serverList.SingleOrDefault(p => p.id == serverInfo.id) == null)
                            {
                                serverList.Add(serverInfo);
                            }
                        }
                        else
                        {
                            serverList.RemoveAll(p => p.id == serverInfo.id);
                        }

                        tblServerList.ReloadData();
                    }
                });
        }

        protected bool SearchShouldReturn (UITextField view)
        {
            view.ResignFirstResponder ();

            if (view == txtMessage)
            {
                SendMessage();
            }

            return true;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            if (socketClient != null)
            {
                if (socketClient.State != SocketClientState.Disconnected)
                {
                    socketClient.Disconnect();
                }

                socketClient.StateChanged -= SocketClient_StateChanged;

                socketClient = null;
            }

            if (socketBroadcastServer != null)
            {
                if (socketBroadcastServer.State != SocketServerState.Stopped)
                {
                    socketBroadcastServer.Stop();
                }

                socketBroadcastServer.ReceivedMessage -= SocketBroadcastServer_ReceivedMessage;

                socketBroadcastServer = null;
            }

            if (socketBroadcastClient != null)
            {
                socketBroadcastClient.Dispose();

                socketBroadcastClient = null;
            }

            serverList.Clear();

            serverList = null;
        }

        private List<ServerInfo> serverList;

        private SocketClient socketClient;

        private SocketBroadcastClient socketBroadcastClient;

        private SocketBroadcastServer socketBroadcastServer;

        private const int SERVER_PORT = 6000;

        private const int BROADCAST_PORT = 6001;
	}

    public class ServerListTableViewSource : UITableViewSource
    {
        public event EventHandler<ServerInfo> SelectedItem;

        public ServerListTableViewSource(List<ServerInfo> countries)
        {
            this.servers = countries;
        }

        public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (ServerTableViewCell)tableView.DequeueReusableCell ("ServerTableViewCell", indexPath);

            var source = servers [indexPath.Row];

            cell.Update (source);

            return cell;
        }

        public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
        {
            var handler = SelectedItem;

            if (handler != null) 
            {
                handler (tableView, servers [indexPath.Row]);
            }
        }

        public override nint NumberOfSections (UITableView tableView)
        {
            return 1;
        }

        public override nint RowsInSection (UITableView tableview, nint section)
        {
            return servers.Count;
        }

        public void Update(List<ServerInfo> countries)
        {
            this.servers = countries; 
        }

        private List<ServerInfo> servers;
    }
}

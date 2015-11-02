using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using NetworkCommunication.Core;
using Android.Graphics;
using Android.Views.InputMethods;
using Android.Content.PM;
using System.Text;
using System.Collections.Generic;
using Android.Util;

namespace NetworkCommunication.Android
{
    [Activity(Label = "Client", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ClientActivity : Activity, TextView.IOnEditorActionListener
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.client_layout);


            lblConnectStatus = FindViewById<TextView>(Resource.Id.lblConnectStatus);
			
            txtIPAddress = FindViewById<EditText>(Resource.Id.txtIPAddress);

            txtPort = FindViewById<EditText>(Resource.Id.txtPort);

            txtMessage = FindViewById<EditText>(Resource.Id.txtMessage);

            btnConnect = FindViewById<Button>(Resource.Id.btnConnect);


            serverListView = FindViewById<ListView>(Resource.Id.ServerListView);

            serverListViewAdapter = new ServerListViewAdapter(this, new List<ServerInfo>());

            serverListView.Adapter = serverListViewAdapter;

            serverListView.ItemClick += ServerListView_ItemClick;


            txtIPAddress.SetOnEditorActionListener(this);

            txtPort.SetOnEditorActionListener(this);

            txtMessage.SetOnEditorActionListener(this);

            btnConnect.Click += BtnConnect_Click;


            socketClient = new SocketClient();

            socketClient.StateChanged += SocketClient_StateChanged;


            socketBroadcastServer = new SocketBroadcastServer();

            socketBroadcastServer.ReceivedMessage += SocketBroadcastServer_ReceivedMessage;

            socketBroadcastServer.Run(BROADCAST_PORT);


            socketBroadcastClient = new SocketBroadcastClient(BROADCAST_PORT);
        }

        protected override void OnResume()
        {
            base.OnResume();

            socketBroadcastClient.SendMessage(new byte[]{ 1 });
        }

        protected void ServerListView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            txtIPAddress.Text = serverListViewAdapter.GetItem(e.Position).address;
        }
            
        protected void BtnConnect_Click (object sender, EventArgs e)
        {
            if (socketClient.State == SocketClientState.Disconnected)
            {
                int port = SERVER_PORT;

                int.TryParse(txtPort.Text, out port);

                socketClient.Connect(txtIPAddress.Text, port);
            }
            else
            {
                socketClient.Disconnect();
            }      
        }

        protected void SocketClient_StateChanged (object sender, SocketClientState state)
        {
            RunOnUiThread(() =>
                {
                    switch (state)
                    {
                        case SocketClientState.Connected:

                            lblConnectStatus.Text = "Connected";

                            btnConnect.Text = "Disconnect";

                            lblConnectStatus.SetTextColor(Color.Green);

                            break;

                        case SocketClientState.Disconnected:

                            lblConnectStatus.Text = "Disconnected";

                            btnConnect.Text = "Connect";

                            lblConnectStatus.SetTextColor(Color.Red);

                            break;
                    }
                });
        }
            
        protected void SendMessage()
        {
            if (socketClient.State == SocketClientState.Connected)
            {
                socketClient.SendMessage(Encoding.UTF8.GetBytes(txtMessage.Text));

                txtMessage.Text = string.Empty;
            } 
        }
   
        protected void SocketBroadcastServer_ReceivedMessage (object sender, ReceiveMessageEventArgs e)
        {
            RunOnUiThread(() =>
                {
                    var serverInfo = SerializationHelper.ToObject<ServerInfo>(e.Message);

                    if (serverInfo != null)
                    {
                        serverInfo.address = e.Host.Address;

                        if (serverInfo.started == ServerInfoState.Started)
                        {
                            serverListViewAdapter.Add(serverInfo);
                        }
                        else
                        {
                            serverListViewAdapter.RemoveById(serverInfo.id);
                        }
                    }
                });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

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

            lblConnectStatus = null;

            txtPort = null;

            txtIPAddress = null; 

            txtMessage = null; 

            btnConnect = null;

            serverListView = null;

            serverListViewAdapter.Clear();

            serverListViewAdapter = null;
        }
                     
        public bool OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
        {
            if (actionId == ImeAction.Done)
            {
                var inputMethodManager = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);

                inputMethodManager.HideSoftInputFromWindow(v.WindowToken, 0);

                if (v == txtMessage)
                {
                    SendMessage();
                }

                return true;
            }

            return false;
        }
            
        private TextView lblConnectStatus;

        private EditText txtPort;

        private EditText txtIPAddress; 

        private EditText txtMessage; 

        private Button btnConnect;

        private ListView serverListView;

        private ServerListViewAdapter serverListViewAdapter;

        private SocketClient socketClient;

        private SocketBroadcastClient socketBroadcastClient;

        private SocketBroadcastServer socketBroadcastServer;

        private const int SERVER_PORT = 6000;

        private const int BROADCAST_PORT = 6001;
    }


    public class ServerListViewAdapter : MonoArrayAdapter<ServerInfo>
    {
        public ServerListViewAdapter(Context context, List<ServerInfo> source) : base(context, source)
        {
            
        }
        
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                var view = new TextView(context);

                view.LayoutParameters = new AbsListView.LayoutParams(AbsListView.LayoutParams.MatchParent, (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 50, Application.Context.Resources.DisplayMetrics));

                view.Gravity = GravityFlags.Left | GravityFlags.CenterVertical;

                view.SetTextColor(Color.Black);

                convertView = view;
            }

            ((TextView)convertView).Text = objects[position].name;

            return convertView;
        }

        public void RemoveById(Guid id)
        {             
            objects.RemoveAll(p=>p.id == id);

            NotifyDataSetChanged();
        }

        public override void Add(ServerInfo item)
        {
            if (objects.SingleOrDefault(p => p.id == item.id) == null)
            {
                base.Add(item);
            }
        }
    }
}



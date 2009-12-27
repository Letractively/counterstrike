﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using doru;
using System.Net.Sockets;
using System.IO;
using System.Collections.ObjectModel;
using doru.Tcp;
#if(SILVERLIGHT)

#else
using System.Diagnostics;
#endif
using System.Collections.Specialized;
using System.Windows.Threading;


namespace CSLIVE.Menu.View
{
    public partial class ServerList : UserControl, IUpdate //provides list of avaible games
    {
        public static LocalDatabase _LocalDatabase { get { return Page._LocalDatabase; } set { Page._LocalDatabase = value; } }
        public static Config _Config { get { return Page._Config; } set { Page._Config = value; } }
        public ServerList()
        {
            
            _Local._Server = false;
            _Local._Name = _LocalDatabase._Nick;
            _Local._IpAddress = "unkown";
            InitializeComponent();        
        }
        public void Start()
        {
            
            Helper.Connect(_Config._BossServerIp, Dispatcher, Connected);               
        }
        Listener _Listener;
        Sender _Sender;
        NetworkStream _NetworkStream;
        public void Connected(SocketAsyncEventArgs e2) // connected to boss server
        {
            Debug.Assert(e2.SocketError == SocketError.Success);
            Socket _Socket = (Socket)e2.UserToken;
            _NetworkStream = new NetworkStream((Socket)e2.UserToken);
            _Listener = new Listener { _NetworkStream = _NetworkStream, _Socket = _Socket};
            _Listener.StartAsync("ServerList");
            _Sender = new Sender { _NetworkStream = _NetworkStream, _Socket = _Socket};
            _Sender.Send(PacketType.getrooms);
            _BossClients.CollectionChanged += delegate { UpdateList(); };
        }

        void UpdateList()
        {
            _ListBox.ItemsSource = _BossClientsLoaded;
        }
        ObservableCollection<ServerListGame> _BossClients = new ObservableCollection<ServerListGame>();
        public IEnumerable<ServerListGame> _BossClientsLoaded
        {
            get
            {
                return (from a in _BossClients where a._RemoteSharedObj._Serialized && a._Server == true select a);
            }
        }
        public void Update()
        {
            if (_Listener != null)
            {
                foreach (ServerListGame cla in _BossClientsLoaded)
                {
                    cla.Update();
                }
                foreach (byte[] bts in _Listener.GetMessages())
                {
                    MemoryStream _MemoryStream = new MemoryStream(bts);
                    byte _IdFrom = _MemoryStream.ReadB();
                    PacketType pk = (PacketType)_MemoryStream.ReadB();
                    Trace.Assert(pk.IsValid());
                    if (_IdFrom == Common._ServerId)
                        switch (pk)
                        {
                            case PacketType.rooms:
                                {
                                    List<RoomInfo> _RoomInfo = (List<RoomInfo>)Common._XmlSerializerRoom.Deserialize(_MemoryStream);
                                    BossRoomInfo bs = (BossRoomInfo)_RoomInfo.First(room => room is BossRoomInfo);
                                    _Sender.Send(PacketType.joinroom, new byte[] { (byte)_RoomInfo.IndexOf(bs) });
                                }
                                break;
                            case PacketType.JoinRoomSuccess:
                                OnJoinedRoom(_MemoryStream);
                                break;
                            case PacketType.PlayerJoined: //adding new client, sending shared object                                
                                    CreateClient(_IdFrom);
                                    //_Sender.Send(PacketType.sharedObject, _Local._LocalSharedObj.Serialize(), _IdFrom);                                
                                break;
                            case PacketType.pong:
                                
                                break;
                            case PacketType.PlayerLeaved: //remocing client
                                _BossClients.Remove(GetClient(_MemoryStream.ReadB()));
                                break;
                            default:
                                Trace.Fail("wrong packet");
                                break;
                        }
                    else
                    {
                        ServerListGame _Client = GetClient(_IdFrom);
                        Trace.Assert(_Client != null);
                        switch (pk)
                        {
                            case PacketType.sharedObject:
                                _Client._RemoteSharedObj.OnBytesToRead(_MemoryStream); //updating client`s sharedobject
                                UpdateList();
                                break;                            
                            default:
                                Trace.Fail("wrong packet");
                                break;
                        }
                    }
                    Trace.Assert(_MemoryStream.Length == _MemoryStream.Position);
                }
                if (_Status == Status.Connected)
                {
                    byte[] bts2 = _Local._LocalSharedObj.GetChanges();
                    if (bts2 != null) _Sender.Send(PacketType.sharedObject, bts2);
                }
            }
        }

        private void OnJoinedRoom(MemoryStream _MemoryStream)
        {
            _Status = Status.Connected;
            _id = _MemoryStream.ReadB();
            foreach (byte playerid in _MemoryStream.Read())
            {
                CreateClient(playerid);
            }                        
        }

        private void CreateClient(byte playerid)
        {
            ServerListGame bs = new RemoteSharedObj<ServerListGame>();
            bs.Dispatcher = Dispatcher;
            bs._Id = playerid;
            bs.Load();
            _BossClients.Add(bs);
        }
        
        


        public ServerListGame GetClient(int id)
        {            
            return _BossClients.FirstOrDefault(a => a._Id == id); 
        }
        public int _id;
        public BossClient _Local = new LocalSharedObj<BossClient>();
        Status _Status = Status.Disconnected;
        public enum Status { Connected, Disconnected }
    }
}
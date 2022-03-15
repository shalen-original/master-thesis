using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using static GeodesyHelper;


#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
using System.IO;
#endif

/*
    Receives UDP packets and dispatches them to the rest of the application
    via `UnityEvent`. The conditional compilation is needed because the
    Unity Editor and the Hololens use different network APIs.
*/

public class SimulatorStatusUpdate
{
	public WGS84Point planePositionOnEarth;
	public float roll;
	public float pitch;
	public float yaw;
}

public class UDPManager : MonoBehaviour
{
	public int ReceiveUdpPort = 53941;
	public int SendUdpPort = 53942;
	public string SendUdpIp = "192.168.0.201";

	public UnityEvent<string, JObject> OnUDPCommandReceived;

#if UNITY_EDITOR
	private UdpClient _UdpSocket;
#endif

#if !UNITY_EDITOR
	private DatagramSocket _UdpSocket;
#endif

	public SimulatorStatusUpdate CurrentSimulatorStatus
	{
		get { return _CurrentSimulatorStatus; }
	}
	
	private readonly ConcurrentQueue<Action> _ExecuteOnMainThreadQueue = new ConcurrentQueue<Action>();
	private SimulatorStatusUpdate _CurrentSimulatorStatus;

	async void Start()
	{
		// This changes the configuration used by `JObject.Parse` below
		// such that camel case properties (e.g. `objectName`) in the JSON
		// are automatically mapped to the respective Pascale case ones
		// (e.g. `ObjectName`), which is the one normally used in .NET.
		// This makes creating command classes way easier because you
		// don't have to specify a `JsonProperty` for every field.
		// Additionally, it registers the appropriate converters so that
		// Vector3 and other Unity types are correctly serialized
		// and deserialized
		JsonConvert.DefaultSettings = () =>
		{
			var s = new JsonSerializerSettings()
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				DefaultValueHandling = DefaultValueHandling.Populate
			};

			s.Converters.Add(new Vector3JsonConverter());
			s.Converters.Add(new Vector3NullableJsonConverter());
			s.Converters.Add(new ColorJsonConverter());

			return s;
		};

		_CurrentSimulatorStatus = new SimulatorStatusUpdate();

		await StartReceiving();
	}

	void Update()
	{
		/*
			This is a bit "weird". In summary, Unity has its own main thread, where it runs all the Start() and Update()
			of the various components for each GameObject. The `DatagramSocket` that is used to receive UDP packet on UWP
			(= on the Hololens) uses C# async, which basically means that `Task`s can technically be executed in additional
			worker threads, different from the main one from Unity. Now. I am not sure that this is the precise cause,
			but getting this UDP receiver to work on the Hololens was a pain, and a certain point during debugging I choose
			to be extra-safe with thread safety, which leads to the current implementation.

			The idea is that, whenever a packet is received, it is enqueued on the _ExecuteOnMainThreadQueue, which, being
			a `ConcurrentQueue` should work reglardless of threads. Then, Unity executes UDPReceiver::Update (on its main
			thread): here the enqueued packet are dequeued, processed, and the state of the Unity application updated.
		 */

		while (!_ExecuteOnMainThreadQueue.IsEmpty)
		{
			if (_ExecuteOnMainThreadQueue.TryDequeue(out Action a))
			{
				a.Invoke();
			}
		}
	}

	public void SendUDPJSONMessage(object message)
    {
		SendUDPMessage(JsonConvert.SerializeObject(message));
    }

	public void SendUDPMessage(string message)
    {
#if UNITY_EDITOR
		SendUdpMessageUnityEditor(message);
#endif

#if !UNITY_EDITOR
		SendUdpMessageUWP(message);
#endif
	}

	private void ProcessPacket(byte[] packet)
	{

		var byteReprOfOpenSquareBracket = Encoding.UTF8.GetBytes("{")[0];
		if (packet[0] == byteReprOfOpenSquareBracket)
        {
			var jobj = JObject.Parse(Encoding.UTF8.GetString(packet));
			OnUDPCommandReceived.Invoke(jobj["type"].ToString(), jobj);
		} else
        {
			WGS84Point p;
			p.LatitudeRadians = BitConverter.ToDouble(packet, 1);
			p.LongitudeRadians = BitConverter.ToDouble(packet, 9);
			p.AltitudeMeters = BitConverter.ToDouble(packet, 17);

			_CurrentSimulatorStatus = new SimulatorStatusUpdate
			{
				planePositionOnEarth = p,
				roll = (float)BitConverter.ToDouble(packet, 25),
				pitch = (float)BitConverter.ToDouble(packet, 33),
				yaw = (float)BitConverter.ToDouble(packet, 41)
			};
		}
	}



#if UNITY_EDITOR
	public async Task StartReceiving()
    {
		_UdpSocket = new UdpClient(this.ReceiveUdpPort);
		_UdpSocket.BeginReceive(new AsyncCallback(OnUdpData), _UdpSocket);

		// To remove the warning
		await Task.CompletedTask;
	}

	void OnUdpData(IAsyncResult result)
	{
		var socket = result.AsyncState as UdpClient;

		IPEndPoint source = new IPEndPoint(0, 0);
		byte[] packet = socket.EndReceive(result, ref source);

		_ExecuteOnMainThreadQueue.Enqueue(() => {
			ProcessPacket(packet);
		});

		socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
	}

	void SendUdpMessageUnityEditor(string message)
    {
		byte[] msg = Encoding.ASCII.GetBytes(message);
		_UdpSocket.Send(msg, msg.Length, SendUdpIp, SendUdpPort);
		_UdpSocket.BeginReceive(new AsyncCallback(OnUdpData), _UdpSocket);
	}
#endif

#if !UNITY_EDITOR
	public async Task StartReceiving()
	{
		_UdpSocket = new Windows.Networking.Sockets.DatagramSocket();
		_UdpSocket.MessageReceived += OnUdpDataUWP;
		await _UdpSocket.BindEndpointAsync(null, this.ReceiveUdpPort.ToString());
        
		// This is stupid: https://stackoverflow.com/a/58834417
		// In summary, apparently, the DatagramSocket can only receive
		// UDP packets after *sending* at least one. After adding these
		// five lines it magically started working on the Hololens.
		// I can't believe it. I hope this is a bug and not intended behaviour.

		await Task.Delay(3000);
		await SendUdpMessageUWP("Datagram socket initialized");
	}

	public async Task SendUdpMessageUWP(string message){
		using (Stream outputStream = (await _UdpSocket.GetOutputStreamAsync(new HostName(SendUdpIp), this.SendUdpPort.ToString())).AsStreamForWrite())
		{
			using (var streamWriter = new StreamWriter(outputStream))
			{
				await streamWriter.WriteLineAsync(message);
				await streamWriter.FlushAsync();
			}
		}
	}

	private async void OnUdpDataUWP(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
	{
		using (var dataReader = args.GetDataReader())
		{
			byte[] packet = new byte[dataReader.UnconsumedBufferLength];
			dataReader.ReadBytes(packet);
			_ExecuteOnMainThreadQueue.Enqueue(() => {
				ProcessPacket(packet);
			});
		}

	}
#endif


}

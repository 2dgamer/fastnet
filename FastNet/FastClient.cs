using System;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;

namespace FastNet
{
	public class FastClient
	{
		
		const int packetHeadSize = 6;

		private NetworkStream stream;
		private byte[] headBuf;
		private int headReaded;
		private int bodyReaded;
		private Queue<Message> messageQueue;

		private Dictionary<string, ProcessDelegate>  handlers; 

		public delegate void ProcessDelegate(byte[] content);


		public FastClient ()
		{
			this.headBuf = new byte[packetHeadSize];
			this.handlers = new Dictionary<string, ProcessDelegate> ();
			this.messageQueue = new Queue<Message> ();
		}

		public void registHandle(byte serviceId, byte messageId, ProcessDelegate handle) {
			string identity = serviceId.ToString () +","+ messageId.ToString ();
			this.handlers.Add (identity, handle);
		}

		public void Connect(string host, int port) 
		{
			
			TcpClient client = new TcpClient();
			var ar = client.BeginConnect(host, port, null, null);
			ar.AsyncWaitHandle.WaitOne(new TimeSpan(0,0,0,0, 10000));
			if (!ar.IsCompleted) {
				throw new TimeoutException();
			}
			client.EndConnect(ar);
			this.stream = client.GetStream();

			this.ReadHead ();
		}


		private void ReadHead() 
		{
			try {
				this.stream.BeginRead(headBuf, headReaded, headBuf.Length - headReaded, (IAsyncResult result)=> {
					int readed;

					try {
						readed = this.stream.EndRead(result);
					} catch {
						this.Close();
						return;
					}

					if (readed == 0) {
						this.Close();
						return;
					}

					headReaded += readed;


					if (headReaded != headBuf.Length) {
						this.ReadHead();
						return;
					}

					headReaded = 0;

					int length;
					byte serviceId;
					byte messageId;
					using(MemoryStream ms = new MemoryStream(this.headBuf)) {
						using (BinaryReader br = new BinaryReader(ms)) {
							length = (int)br.ReadUInt32();
							serviceId = br.ReadByte();
							messageId = br.ReadByte();
						}
					}

					if (length == 0) {
						this.Close();
						return;
					}

					byte[] buf = new byte[length];
					this.ReadBody(buf,serviceId, messageId);
				}, null);

			} catch {
				this.Close ();
			}
		}

		private void ReadBody(byte[] buf, byte serviceId, byte messageId)
		{
			try {
				this.stream.BeginRead(buf, bodyReaded, buf.Length - bodyReaded, (IAsyncResult result) => {
					int readed;

					try {
						readed = this.stream.EndRead(result);
					} catch {
						this.Close();
						return;
					}

					if (readed == 0) {
						this.Close();
						return;
					}

					bodyReaded += readed;


					if (bodyReaded != buf.Length) {
						this.ReadBody(buf, serviceId, messageId);
						return;
					}

					bodyReaded = 0;

					this.AddMessageToQueue(new Message(serviceId, messageId, buf));

					this.ReadHead();

				}, null);
			} catch {
				this.Close ();
			}
		}


		private void AddMessageToQueue(Message message)
		{
			lock (messageQueue) {
				messageQueue.Enqueue (message);
			}
		}

		private Message getMessageFromQueue() 
		{
			lock (messageQueue) {
				if (messageQueue.Count > 0) {
					return messageQueue.Dequeue ();
				} else {
					return null;
				}
			}
		}

		class Message {
			public byte ServiceId { get; }
			public byte MessageId { get; }
			public string Identity { get; }
			public byte[] Content { get; }

			public Message(byte serviceId, byte messageId, byte[] content)
			{
				ServiceId = serviceId;
				MessageId = messageId;
				Identity = serviceId.ToString() + "," + messageId.ToString();
				Content = content;
			}
		}

		public void Send(byte serviceId, byte messageId, byte[] content)
		{
			try {
				byte[] buffer = new byte[packetHeadSize+content.Length];
				int n = 0;
				Gobuf.WriteUint32((uint)content.Length, buffer, ref n);
				buffer[n++] = serviceId;
				buffer[n++] = messageId;

				Buffer.BlockCopy(content, 0, buffer, n, content.Length);
				this.stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(SendCallback), this.stream);
			} catch (Exception e) {
				Console.WriteLine (e);
				this.Close ();
			}
		}

		public void SendCallback(IAsyncResult ar) 
		{
			// 发送成功
			Console.WriteLine("---------发送成功---------");
		}

		public void Process() {
			try {
				Message msg = this.getMessageFromQueue();
				if (msg != null) {
					if (this.handlers.ContainsKey(msg.Identity)) {
						var handle = this.handlers[msg.Identity];
						Console.WriteLine("Identity " + msg.Identity );
						handle(msg.Content);
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}

		private void Close() 
		{
			this.stream.Close ();
		}



		public static void Main (string[] args)
		{
			FastClient client = new FastClient ();
			Module1 module1 = new Module1 (client);
			client.Connect ("127.0.0.1", 9000);

			module1.addRspHandle = delegate(Module1.AddRsp addRsp) {
				Console.WriteLine("----addRsp C1: " + addRsp.C);
			};
				
			Module1.AddReq addReq = new Module1.AddReq();
			addReq.A = 1;
			addReq.B = 2;
			module1.SendAddReq (addReq);

			for (;;){
				client.Process ();
				System.Threading.Thread.Sleep (5000);
			}
		}
	}
}


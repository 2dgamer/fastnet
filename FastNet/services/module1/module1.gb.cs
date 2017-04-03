using System;
using System.Collections.Generic;
using System.Net.Sockets;
using FastNet;


namespace FastNet
{
	class Module1 {

		public enum MessageID : byte {
			MsgID_Add = 0
		};

		private FastClient client;
		
		public delegate void AddRspHandler(AddRsp addRsp);
		public AddRspHandler addRspHandle;

		public Module1(FastClient c) {
			this.client = c;
			this.client.registHandle ((byte)ServiceID.ServiceID_Module1, (byte)Module1.MessageID.MsgID_Add, this.handleAdd);
		}

		public void handleAdd(byte[] content) {
			AddRsp addRsp = new AddRsp ();
			addRsp.Unmarshal (content, 0);
			if (addRspHandle != null) {
				addRspHandle (addRsp);
			}
		}

		public void SendAddReq(AddReq addReq) {
			byte[] b = new byte[addReq.Size()];
			addReq.Marshal (b, 0);			
			client.Send((byte)ServiceID.ServiceID_Module1, (byte)MessageID.MsgID_Add, b);
		}


		public class AddReq {
			public long A;
			public long B;
			public int Size() {
				int size = 0;
				size += Gobuf.VarintSize(this.A);
				size += Gobuf.VarintSize(this.B);
				return size;
			}
			public int Marshal(byte[] b, int n) {
				Gobuf.WriteVarint(this.A, b, ref n);
				Gobuf.WriteVarint(this.B, b, ref n);
				return n;
			}
			public int Unmarshal(byte[] b, int n) {
				this.A = Gobuf.ReadVarint(b, ref n);
				this.B = Gobuf.ReadVarint(b, ref n);
				return n;
			}
		}
		public class AddRsp {
			public long C;
			public int Size() {
				int size = 0;
				size += Gobuf.VarintSize(this.C);
				return size;
			}
			public int Marshal(byte[] b, int n) {
				Gobuf.WriteVarint(this.C, b, ref n);
				return n;
			}
			public int Unmarshal(byte[] b, int n) {
				this.C = Gobuf.ReadVarint(b, ref n);
				return n;
			}
		}
	}
}




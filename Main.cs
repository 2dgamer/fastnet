using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FastNet;

public class Main : MonoBehaviour {

	private FastClient client;
	private Module1 module1;

	// Use this for initialization
	void Start () {
		client = new FastClient ();
		module1 = new Module1 (client);
		client.Connect ("127.0.0.1", 9000);

		module1.addRspHandle = delegate(Module1.AddRsp addRsp) {
			Debug.Log("addRsp c: "+ addRsp.C);
		};

		Module1.AddReq addReq = new Module1.AddReq();
		addReq.A = 1;
		addReq.B = 2;
		module1.SendAddReq (addReq);
	}
	
	// Update is called once per frame
	void Update () {
		client.Process ();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class UDPCommManager : MonoBehaviour
{
    const string GALAGA_IP = "18.219.219.134";
    const int GALAGA_PORT = 1981;
    
    [SerializeField] bool _debug = false;

    [Header("Test packet")]
    [SerializeField] uint _frameTest = 0;
    [SerializeField] Enums.Input _inputTest = Enums.Input.NONE;
    [SerializeField] uint _ackTest = 0;

    IPEndPoint _galagaIPEndPoint;
    UdpClient _udpClient;
    // byte _xorKey = 0;

    void Start() 
    {
        Init();   
    }

    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            SendPacket(_frameTest, _inputTest, _ackTest);
            ReceivePacket();
        }    
    }
    
    void OnDestroy() 
    {
        _udpClient?.Close();
    }

    void Init()
    {
        _galagaIPEndPoint = new IPEndPoint(IPAddress.Parse(GALAGA_IP), GALAGA_PORT);
        _udpClient = new UdpClient();
        _udpClient.Connect(_galagaIPEndPoint);
    }

    public void SendPacket(uint p_frame, Enums.Input p_input, uint p_ack)
    {
        uint __frame = p_frame << 1;
        uint __input = (uint)p_input;
        __frame = __frame | ((__input & 2) >> 1);
        uint __ack = p_ack | ((__input & 1) << 7);
        byte[] __packet = {(byte)__ack, (byte)__frame};

        if (_debug) Debug.Log
        (  
            "[UDP] \nSent data: " + Convert.ToString(BitConverter.ToUInt16(__packet, 0), 2).PadLeft(__packet.Length * 8, '0') +
            "\nTo: " + _galagaIPEndPoint.Address.ToString() +
            "\nOn port: " + _galagaIPEndPoint.Port.ToString()
        );

        _udpClient.Send(__packet, __packet.Length);
    }

    public byte ReceivePacket()
    {
        IPEndPoint __anyIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            byte[] __packet = _udpClient.Receive(ref __anyIPEndPoint);
            int __packetLength = __packet.Length;
            
            byte[] __mainHeader = {__packet[__packetLength - 1], __packet[__packetLength - 2]};
            if (_debug) Debug.Log
            (  
                "[UDP] \nReceived data header: " + Convert.ToString(BitConverter.ToUInt16(__mainHeader, 0), 2).PadLeft(__mainHeader.Length * 8, '0') +
                "\nSent from: " + __anyIPEndPoint.Address.ToString() +
                "\nOn port: " + __anyIPEndPoint.Port.ToString()
            );
        }
        catch(Exception p_exception)
        {
            Debug.LogError("[UDP] Receive exception: \n" + p_exception.ToString());
        }
        
        return 0;
    }
}

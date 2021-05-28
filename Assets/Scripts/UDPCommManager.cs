using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class UDPCommManager : MonoBehaviour
{
    const string GALAGA_IP = "18.219.219.134";
    const int GALAGA_PORT = 1981;

    public class GalagaObject
    {
        public byte id;
        public byte horizontalPosition;
        public byte verticalPosition;

        public GalagaObject(byte p_id, byte p_horizontalPosition, byte p_verticalPosition)
        {
            id = p_id;
            horizontalPosition = p_horizontalPosition;
            verticalPosition = p_verticalPosition;
        }
    }
    
    public Action<uint, uint, List<GalagaObject>> ReceivedPacket;
    
    [SerializeField] bool _debug = false;

    [Header("Packet Sender")]
    [SerializeField] bool _sendPacketPersistently = false;
    [SerializeField] [Range(0, 1)] float _sendTimeIntervals = 1;

    [Header("Test packet")]
    [SerializeField] uint _frameTest = 0;
    [SerializeField] Enums.Input _inputTest = Enums.Input.NONE;
    [SerializeField] uint _ackTest = 0;

    IPEndPoint _galagaIPEndPoint;
    UdpClient _udpClient;
    byte[] _packetToSend;
    byte[] _lastReceivedPacket;
    Coroutine _sendCoroutine;

    bool _tmpFlag = false;

    void Start() 
    {
        Init();

        ReceivedPacket += (p_frame, p_seq, p_objectList) =>
        {
            if (_tmpFlag) 
            {
                RequestSendPacket(++p_frame, Enums.Input.NONE, p_seq);
            }
            else
            {
                RequestSendPacket(p_frame, Enums.Input.NONE, p_seq);
                _tmpFlag = true;
            }
        };
    }

    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            RequestSendPacket(_frameTest, _inputTest, _ackTest);
        } 
    }
    
    void OnDisable() 
    {
        _udpClient?.Close();
    }

    void Init()
    {
        _galagaIPEndPoint = new IPEndPoint(IPAddress.Parse(GALAGA_IP), GALAGA_PORT);
        _udpClient = new UdpClient();
        _udpClient.Connect(_galagaIPEndPoint);
        StartCoroutine(ReceivePacket());
    }

    public void RequestSendPacket(uint p_frame, Enums.Input p_input, uint p_ack)
    {
        byte __frame = (byte)(p_frame << 1);
        byte __ack = (byte)(p_ack & 0b01111111);
        byte __input = (byte)p_input;
        __frame = (byte)(__frame | ((__input & 2) >> 1));
        __ack = (byte)(__ack | (__input << 7));
        byte[] __packet = {__frame, __ack};

        _packetToSend = __packet;
        if (_sendCoroutine != null)
        {
            StopCoroutine(_sendCoroutine);
        }
        _sendCoroutine = StartCoroutine(SendPacket());
    }

    IEnumerator SendPacket()
    {
        do
        {
            _udpClient.Send(_packetToSend, _packetToSend.Length);
            byte[] __packet = (byte[])_packetToSend.Clone();
            Array.Reverse(__packet);
            if (_debug) Debug.Log("[UDP] \nSent packet: " + Convert.ToString(BitConverter.ToUInt16(__packet, 0), 2).PadLeft(__packet.Length * 8, '0') + "\n");

            yield return new WaitForSecondsRealtime(_sendTimeIntervals);
        }
        while (_sendPacketPersistently);
        _sendCoroutine = null;
    }

    IEnumerator ReceivePacket()
    {
        while (true) 
        {
            IPEndPoint __anyIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Task<UdpReceiveResult> __receiveTask = _udpClient.ReceiveAsync();
            
            while (!__receiveTask.IsCompleted)
            {
                yield return null;
            }

            if (__receiveTask.IsFaulted || __receiveTask.IsFaulted) 
            {
                if (__receiveTask.Exception != null) 
                {
                    Debug.LogError("[UDP] Receive Exception: \n" + __receiveTask.Exception.Message);
                }
                else
                {
                    Debug.LogError("[UDP] Receive Failed.");
                }
            }
            else
            {
                if (IPAddress.Equals(__receiveTask.Result.RemoteEndPoint.Address, _galagaIPEndPoint.Address))
                {
                    _lastReceivedPacket = __receiveTask.Result.Buffer;
                    
                    // int __length = _lastReceivedPacket.Length;
                    // byte[] __headerCoded = {_lastReceivedPacket[1], _lastReceivedPacket[0]};
                    // if (_debug) Debug.Log("[UDP] \nReceived packet: " + Convert.ToString(BitConverter.ToUInt16(__headerCoded, 0), 2).PadLeft(__headerCoded.Length * 8, '0') + "\n");
                    
                    DecodeReceivedPacket();
                    
                    // byte[] __headerDecoded = {_lastReceivedPacket[1], _lastReceivedPacket[0]};
                    // if (_debug) Debug.LogWarning("[UDP] \nReceived packet: " + Convert.ToString(BitConverter.ToUInt16(__headerDecoded, 0), 2).PadLeft(__headerDecoded.Length * 8, '0') + "\n");
                    
                    ExtractDataFromReceivedPacket(_lastReceivedPacket);
                }
            }
        }
    }

    void DecodeReceivedPacket()
    {
        byte __key = (byte)(_lastReceivedPacket[0] ^ _packetToSend[0]);
        for (int i = 0; i < _lastReceivedPacket.Length; i++)
        {
            _lastReceivedPacket[i] = (byte)(_lastReceivedPacket[i] ^ __key);
        }
    }

    void ExtractDataFromReceivedPacket(byte[] p_packet) 
    {
        uint __frame = (uint)(p_packet[0] >> 1);
        uint __seq = (uint)(p_packet[1] & 0b01111111);
        uint __input = (uint)((p_packet[0] & 0b00000001) << 1);
        __input |= (uint)((p_packet[1] & 0b10000000) >> 7);

        if (_debug) Debug.LogWarning
        (
            "[UDP] Received data:" +
            "\nFrame: " + Convert.ToString((uint)__frame, 2).PadLeft(7, '0') +
            "\nInput: " + Convert.ToString((uint)__input, 2).PadLeft(2, '0') +
            "\nSEQ: " + Convert.ToString((uint)__seq, 2).PadLeft(7, '0') +
            "\n"
        );

        byte __objectCount = p_packet[2];
        List<GalagaObject> __objectList = new List<GalagaObject>();
        for (int i = 0; i < __objectCount; i++)
        {
            GalagaObject __newObject = new GalagaObject(p_packet[3 + i * 3], p_packet[4 + i * 3], p_packet[5 + i * 3]);
            __objectList.Add(__newObject);
        }

        ReceivedPacket?.Invoke(__frame, __seq, __objectList);
    }
}

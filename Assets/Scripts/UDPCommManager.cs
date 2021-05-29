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

    #region Public Classes

    public class InputPacket
    {
        public byte frame;
        public byte input;
        public byte ack;
        public byte[] packet;
    }

    public class ResponsePacket
    {
        public byte frame;
        public byte input;
        public byte seq;
        public byte[] packet;
        public List<GalagaObject> objectList; 

        public ResponsePacket() { }

        public ResponsePacket(byte p_frame, byte p_input, byte p_seq, byte[] p_packet, List<GalagaObject> p_objectList)
        {
            frame = p_frame;
            input = p_input;
            seq = p_seq;
            packet = p_packet;
            objectList = p_objectList;
        }
    }

    #endregion
    
    public Action<ResponsePacket> ReceivedPacket;
    public Action SendTimedOut;
    public static UDPCommManager Instance;
    
    [SerializeField] bool _debugMode = false;

    [Header("Packet Sender")]
    [SerializeField] bool _sendPacketPersistently = false;
    [SerializeField] [Range(0, 1)] float _sendTimeIntervals = 1;
    [SerializeField] float _sendTimeOut = 1;

    IPEndPoint _galagaIPEndPoint;
    UdpClient _udpClient;
    bool _isWaitingFirstPacket = false;
    
    Coroutine _sendCoroutine;
    InputPacket _inputPacket = new InputPacket();
    ResponsePacket _responsePacket = new ResponsePacket();

    void Awake() 
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start() 
    {
        Init();
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

    public void InitiateCommunication()
    {
        _isWaitingFirstPacket = true;
        RequestSendPacket(0, Enums.Input.NONE, 0);
    }

    public void EndCommunication()
    {
        if (_sendCoroutine != null)
        {
            StopCoroutine(_sendCoroutine);
        }
    }

    void RequestSendPacket(uint p_frame, Enums.Input p_input, uint p_ack)
    {
        _inputPacket.frame = (byte)p_frame;
        byte __frame = (byte)(p_frame << 1);
        
        byte __ack = (byte)(p_ack & 0b01111111);
        _inputPacket.ack = __ack;
        
        byte __input = (byte)p_input;
        _inputPacket.input = __input;
        
        __frame = (byte)(__frame | ((__input & 2) >> 1));
        __ack = (byte)(__ack | (__input << 7));
        byte[] __packet = {__frame, __ack};
        _inputPacket.packet = __packet;

        if (_sendCoroutine != null)
        {
            StopCoroutine(_sendCoroutine);
        }
        _sendCoroutine = StartCoroutine(SendPacket());
    }

    IEnumerator SendPacket()
    {
        float __timer = 0;
        do
        {
            _udpClient.Send(_inputPacket.packet, _inputPacket.packet.Length);
            byte[] __packet = (byte[])_inputPacket.packet.Clone();
            Array.Reverse(__packet);
            if (_debugMode) Debug.Log("[UDP] \nSent packet: " + Convert.ToString(BitConverter.ToUInt16(__packet, 0), 2).PadLeft(__packet.Length * 8, '0') + "\n");

            yield return new WaitForSecondsRealtime(_sendTimeIntervals);
            __timer += _sendTimeIntervals;
            if (__timer > _sendTimeOut) 
            {
                _sendCoroutine = null;
                SendTimedOut?.Invoke();
                yield break;
            }
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
                    ProcessReceivedPacket(__receiveTask.Result.Buffer);
                }
            }
        }
    }

    void ProcessReceivedPacket(byte[] p_packet)
    {
        p_packet = DecodeReceivedPacket(p_packet);
        ResponsePacket __extractedData = ExtractDataFromReceivedPacket(p_packet);

        if (__extractedData == null) 
        {
            return;
        }
        
        if (__extractedData.frame != _inputPacket.frame || __extractedData.input != _inputPacket.input) 
        {
            Debug.LogWarning(
                "[UDP] Received packet mismatch." +
                "\nSended frame: " + _inputPacket.frame.ToString() +
                "\nReceived frame: " + __extractedData.frame.ToString() +
                "\nSended input: " + _inputPacket.input.ToString() +
                "\nReceived input: " + __extractedData.input.ToString() +
                "\n"
            );
            return;
        }
        
        _responsePacket = __extractedData;
        ReceivedPacket?.Invoke(_responsePacket);

        if (_debugMode) Debug.Log
        (
            "[UDP] Received data:" +
            "\nFrame: " + Convert.ToString((byte)_responsePacket.frame, 2).PadLeft(7, '0') +
            "\nInput: " + Convert.ToString((byte)_responsePacket.input, 2).PadLeft(2, '0') +
            "\nSEQ: " + Convert.ToString((byte)_responsePacket.seq, 2).PadLeft(7, '0') +
            "\n"
        );

        if (_isWaitingFirstPacket)
        {
            _isWaitingFirstPacket = false;
            RequestSendPacket(0, PlayerInput.Instance.GetInput(), _responsePacket.seq);
        }
        else
        {
            RequestSendPacket(++_responsePacket.frame, PlayerInput.Instance.GetInput(), _responsePacket.seq);
        }
    }
    
    byte[] DecodeReceivedPacket(byte[] p_packet)
    {
        byte __key = (byte)(p_packet[0] ^ _inputPacket.packet[0]);
        for (int i = 0; i < p_packet.Length; i++)
        {
            p_packet[i] = (byte)(p_packet[i] ^ __key);
        }
        return p_packet;
    }

    ResponsePacket ExtractDataFromReceivedPacket(byte[] p_packet) 
    {
        if ((p_packet.Length - 3) % 3 != 0) 
        {
            Debug.LogWarning("[UDP] Received packet size mismatch");
            return null;
        }
        
        byte __frame = (byte)(p_packet[0] >> 1);
        byte __seq = (byte)(p_packet[1] & 0b01111111);
        byte __input = (byte)((p_packet[0] & 0b00000001) << 1);
        __input |= (byte)((p_packet[1] & 0b10000000) >> 7);

        byte __objectCount = p_packet[2];
        if ((p_packet.Length - 3) / 3 != __objectCount) 
        {
            Debug.LogWarning("[UDP] Received packet objects size mismatch");
            return null;
        }
        List<GalagaObject> __objectList = new List<GalagaObject>();
        for (int i = 0; i < __objectCount; i++)
        {
            GalagaObject __newObject = new GalagaObject(p_packet[3 + i * 3], p_packet[4 + i * 3], p_packet[5 + i * 3]);
            __objectList.Add(__newObject);
        }

        return new ResponsePacket(__frame, __input, __seq, p_packet, __objectList);
    }
}

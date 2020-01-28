using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Socket : MonoBehaviour
{
    static Socket instance;
    TcpClient socket;

    string host = "localhost";
    int port = 64242;

    void Awake()
    {
        instance = this;
        socket = new TcpClient(host, port);
    }

    public bool isConnected()
    {
        return socket != null && socket.Connected;
    }

    public static Socket GetInstance() { return instance; }

    static public string SendMessageQT(GameObject piece)
    {
        if (instance.isConnected())
        {
            NetworkStream stream = instance.socket.GetStream();
            Byte[] bytes = new Byte[1024];

            string message = "";

            message += piece.GetComponent<Piece>().type.ToString();

            bytes = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
            Array.Clear(bytes, 0, bytes.Length);
            message = "";

            for (int i = 0; i < 3; i++)
            {
                int j = stream.Read(bytes, 0, bytes.Length);
                if (j > 0)
                    message += System.Text.Encoding.ASCII.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
            }
            if (message.Contains("!"))
                message = message.Trim('!');
            return message;
        }
        else return null;
    }
}

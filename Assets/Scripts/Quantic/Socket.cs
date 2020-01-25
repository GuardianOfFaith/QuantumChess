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

    void Start()
    {
        instance = this;
        socket = new TcpClient(host, port);
    }

    public bool isConnected()
    {
        return socket != null && socket.Connected;
    }

    public static Socket GetInstance() { return instance; }

    static public string SendMessage(/*A passer UNITEE ET ETAT DU JEU*/)
    {
        if (instance.isConnected())
        {
            NetworkStream stream = instance.socket.GetStream();
            Byte[] bytes = new Byte[2048];

            string message = "";

            //THERE MUS BE SOME TREATMENT

            bytes = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
            Array.Clear(bytes, 0, bytes.Length);

            message = "";
            int nb = 0;
            do
            {
                nb = stream.Read(bytes, 0, bytes.Length);
                if (nb > 0)
                    message += System.Text.Encoding.ASCII.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
            } while (nb == bytes.Length);

            return message;
        }
        else return null;
    }
}

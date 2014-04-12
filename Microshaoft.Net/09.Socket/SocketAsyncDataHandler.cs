﻿namespace Microshaoft
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Diagnostics;
    public class SocketAsyncDataHandler<T>
    {
        private Socket _socket;
        public Socket WorkingSocket
        {
            get
            {
                return _socket;
            }
        }
        //public int ReceiveDataBufferLength
        //{
        //	get;
        //	private set;
        //}
        public T Token
        {
            get;
            set;
        }
        public IPAddress RemoteIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.RemoteEndPoint).Address;
            }
        }
        public IPAddress LocalIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.LocalEndPoint).Address;
            }
        }
        public int SocketID
        {
            get;
            private set;
        }
        public bool HasNoWorkingSocket
        {
            get
            {
                return (_socket == null);
            }
        }
        //public SocketAsyncDataHandler()
        //{

        //}
        public SocketAsyncDataHandler
                            (
                                Socket socket
                                , int socketID
                            )
        {
            _socket = socket;
            _isUdp = (_socket.ProtocolType == ProtocolType.Udp);
            _sendSocketAsyncEventArgs = new SocketAsyncEventArgs();
            SocketID = socketID;
        }
        private SocketAsyncEventArgs _sendSocketAsyncEventArgs;
        public int HeaderBytesLength
        {
            get;
            private set;
        }
        public int HeaderBytesOffset
        {
            get;
            private set;
        }
        private long _receivedCount = 0;
        public long ReceivedCount
        {
            get
            {
                return _receivedCount;
            }
        }
        public int HeaderBytesCount
        {
            get;
            private set;
        }
        public SocketAsyncEventArgs ReceiveSocketAsyncEventArgs
        {
            get;
            private set;
        }
        private bool _isStartedReceiveData = false;
        private bool _isHeader = true;
        public bool StartReceiveWholeDataPackets
                            (
                                int headerBytesLength
                                , int headerBytesOffset
                                , int headerBytesCount
                                , Func<SocketAsyncEventArgs> getReceiveSocketAsyncEventArgsProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onOneWholeDataPacketReceivedProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onReceivedDataPacketErrorProcessFunc = null
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , SocketAsyncEventArgs
                                        , Exception
                                        , bool
                                    > onCaughtExceptionProcessFunc = null
                            )
        {
            return
                StartReceiveWholeDataPackets
                    (
                        headerBytesLength
                        , headerBytesOffset
                        , headerBytesCount
                        , getReceiveSocketAsyncEventArgsProcessFunc()
                        , onOneWholeDataPacketReceivedProcessFunc
                        , onReceivedDataPacketErrorProcessFunc
                        , onCaughtExceptionProcessFunc
                    );
        }
        public bool StartReceiveWholeDataPackets
                            (
                                int headerBytesLength
                                , int headerBytesOffset
                                , int headerBytesCount
                                , SocketAsyncEventArgs receiveSocketAsyncEventArgs
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onOneWholeDataPacketReceivedProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onReceivedDataPacketErrorProcessFunc = null
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , SocketAsyncEventArgs
                                        , Exception
                                        , bool
                                    > onCaughtExceptionProcessFunc = null
                            )
        {
            if (!_isStartedReceiveData)
            {
                HeaderBytesLength = headerBytesLength;
                HeaderBytesOffset = headerBytesOffset;
                HeaderBytesCount = headerBytesCount;
                int bodyLength = 0;
                ReceiveSocketAsyncEventArgs = receiveSocketAsyncEventArgs;
                ReceiveSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>
                                (
                                    (sender, e) =>
                                    {
                                        var socket = sender as Socket;
                                        if (e.BytesTransferred >= 0)
                                        {
                                            byte[] buffer = e.Buffer;
                                            int r = e.BytesTransferred;
                                            int p = e.Offset;
                                            int l = e.Count;
                                            if (r < l)
                                            {
                                                p += r;
                                                e.SetBuffer(p, l - r);
                                            }
                                            else if (r == l)
                                            {
                                                if (_isHeader)
                                                {
                                                    byte[] data = new byte[headerBytesCount];
                                                    Buffer.BlockCopy
                                                                (
                                                                    buffer
                                                                    , HeaderBytesOffset
                                                                    , data
                                                                    , 0
                                                                    , data.Length
                                                                );
                                                    byte[] intBytes = new byte[4];
                                                    l = (intBytes.Length < HeaderBytesCount ? intBytes.Length : HeaderBytesCount);
                                                    Buffer.BlockCopy
                                                                (
                                                                    data
                                                                    , 0
                                                                    , intBytes
                                                                    , 0
                                                                    , l
                                                                );
                                                    //Array.Reverse(intBytes);
                                                    bodyLength = BitConverter.ToInt32(intBytes, 0);
                                                    p += r;
                                                    e.SetBuffer(p, bodyLength);
                                                    Console.WriteLine(bodyLength);
                                                    _isHeader = false;
                                                }
                                                else
                                                {
                                                    byte[] data = new byte[bodyLength + HeaderBytesLength];
                                                    bodyLength = 0;
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    _isHeader = true;
                                                    e.SetBuffer(0, HeaderBytesLength);
                                                    if (onOneWholeDataPacketReceivedProcessFunc != null)
                                                    {
                                                        onOneWholeDataPacketReceivedProcessFunc
                                                            (
                                                                this
                                                                , data
                                                                , e
                                                            );
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (onReceivedDataPacketErrorProcessFunc != null)
                                                {
                                                    byte[] data = new byte[p + r + HeaderBytesLength];
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    bool b = onReceivedDataPacketErrorProcessFunc
                                                                (
                                                                    this
                                                                    , data
                                                                    , e
                                                                );
                                                    if (b)
                                                    {
                                                        bool i = DestoryWorkingSocket();
                                                    }
                                                    else
                                                    {
                                                        _isHeader = true;
                                                        e.SetBuffer(0, HeaderBytesLength);
                                                    }
                                                }
                                            }
                                        }
                                        if (!_isWorkingSocketDestoryed)
                                        {
                                            try
                                            {
                                                socket.ReceiveAsync(e);
                                            }
                                            catch (Exception exception)
                                            {
                                                var r = false;
                                                if (onCaughtExceptionProcessFunc != null)
                                                {
                                                    r = onCaughtExceptionProcessFunc
                                                                (
                                                                    this
                                                                    , e
                                                                    , exception
                                                                );
                                                }
                                                if (r)
                                                {
                                                    DestoryWorkingSocket();
                                                }
                                            }
                                        }
                                    }
                                );
                _socket.ReceiveAsync(ReceiveSocketAsyncEventArgs);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        private bool _isUdp = false;
        public bool IsUdp
        {
            get
            {
                return _isUdp;
            }
        }
        private bool _isWorkingSocketDestoryed = false;
        public bool DestoryWorkingSocket()
        {
            //bool r = false;
            try
            {
                if (_socket.Connected)
                {
                    _socket.Disconnect(false);
                }
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
#if NET45
//#endif
                _socket.Dispose();
//#if NET45
#endif
                _socket = null;
                _isWorkingSocketDestoryed = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                //r = false;
            }
            return _isWorkingSocketDestoryed;
        }
        public bool StartReceiveDataFrom
            (
                EndPoint remoteEndPoint
                , Func<SocketAsyncEventArgs> getReceiveSocketAsyncEventArgsProcessFunc
                , Func
                    <
                        SocketAsyncDataHandler<T>
                        , EndPoint
                        , byte[]
                        , SocketAsyncEventArgs
                        , bool
                    > onDataReceivedProcessFunc
                , Func
                    <
                        SocketAsyncDataHandler<T>
                        , SocketAsyncEventArgs
                        , Exception
                        , bool
                    > onCaughtExceptionProcessFunc = null
            )
        {
            return
                StartReceiveDataFrom
                    (
                        remoteEndPoint
                        , getReceiveSocketAsyncEventArgsProcessFunc()
                        , onDataReceivedProcessFunc
                        , onCaughtExceptionProcessFunc
                    );
        }
        public bool StartReceiveDataFrom
                    (
                        EndPoint remoteEndPoint
                        , SocketAsyncEventArgs receiveSocketAsyncEventArgs
                        , Func
                            <
                                SocketAsyncDataHandler<T>
                                , EndPoint
                                , byte[]
                                , SocketAsyncEventArgs
                                , bool
                            > onDataReceivedProcessFunc
                        , Func
                            <
                                SocketAsyncDataHandler<T>
                                , SocketAsyncEventArgs
                                , Exception
                                , bool
                            > onCaughtExceptionProcessFunc = null
                    )
        {
            if (!_isStartedReceiveData)
            {
                ReceiveSocketAsyncEventArgs = receiveSocketAsyncEventArgs;
                ReceiveSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
                ReceiveSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>
                                                (
                                                    (sender, e) =>
                                                    {
                                                        Interlocked.Increment(ref _receivedCount);
                                                        var socket = sender as Socket;
                                                        int l = e.BytesTransferred;
                                                        //Console.WriteLine(l);
                                                        if (l > 0)
                                                        {
                                                            byte[] data = new byte[l];
                                                            var buffer = e.Buffer;
                                                            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                            if (onDataReceivedProcessFunc != null)
                                                            {
                                                                var fromRemoteIPEndPoint = e.RemoteEndPoint;
                                                                onDataReceivedProcessFunc
                                                                        (
                                                                            this
                                                                            , fromRemoteIPEndPoint
                                                                            , data
                                                                            , e
                                                                        );
                                                                //Console.WriteLine(_receivedCount);
                                                            }
                                                        }
                                                        if (!_isWorkingSocketDestoryed)
                                                        {
                                                            try
                                                            {
                                                                socket.ReceiveFromAsync(ReceiveSocketAsyncEventArgs);
                                                            }
                                                            catch (Exception exception)
                                                            {
                                                                //Console.WriteLine(exception.ToString());
                                                                var r = false;
                                                                if (onCaughtExceptionProcessFunc != null)
                                                                {
                                                                    r = onCaughtExceptionProcessFunc(this, ReceiveSocketAsyncEventArgs, exception);
                                                                }
                                                                if (r)
                                                                {
                                                                    DestoryWorkingSocket();
                                                                }
                                                            }
                                                        }
                                                    }
                                                );
                _socket.ReceiveFromAsync(ReceiveSocketAsyncEventArgs);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        private static object _sendStaticSyncLockObject = new object();
        private object _sendSyncLockObject = new object();
        public int SendDataSync(byte[] data)
        {
            var r = -1;
            if (!_isUdp)
            {
                lock (_sendSyncLockObject)
                {
                    r = _socket.Send(data);
                }
            }
            return r;
        }
        public bool SendDataToSyncWaitResponseWithRetry<TToken>
              (
                  byte[] data
                  , IPEndPoint remoteIPEndPoint
                  , bool blockForResponse
                  , AutoResetEvent waiter
                  , TToken token
                  , Func<TToken, Tuple<bool, bool>> onSetAutoResetEventProcessFunc
                  , out int sendTimes
                  , out long elapsedMilliseconds
                  , Stopwatch stopWatch = null
                  , Func<int, TToken, byte[]> onBeforeSendOnceProcessFunc = null
                  , Action<int> onAfterSendedOnceProcessAction = null
                  , int sendMaxTimes = 100
                  , int resendWaitOneIntervalsInMillisecondsFactor = 1
                  , int waitOneIntervalsInMilliseconds = 1000

              )
        {
            var r = false;
            elapsedMilliseconds = 0;
            if (stopWatch != null)
            {
                if (!stopWatch.IsRunning)
                {
                    stopWatch.Start();
                }
            }
            AutoResetEvent autoResetEvent = waiter;
            int i = 0;
            var nextWaitOneIntervalsInMilliseconds = waitOneIntervalsInMilliseconds;
            try
            {
                var continueWaiting = true;
                while
                    (
                        i < sendMaxTimes
                        && continueWaiting
                        && !r
                    )
                {
                    #region loop body
                    if (onBeforeSendOnceProcessFunc != null)
                    {
                        data = onBeforeSendOnceProcessFunc(i, token);
                    }
                    SendDataToSync
                        (
                            data
                            , remoteIPEndPoint
                        );
                    i++;
                    if (onAfterSendedOnceProcessAction != null)
                    {
                        onAfterSendedOnceProcessAction(i);
                    }
                    if (!blockForResponse)
                    {
                        break;
                    }
                    Tuple<bool, bool> tuple = null;
                    if (autoResetEvent != null)
                    {
                        tuple = onSetAutoResetEventProcessFunc(token);
                        if (tuple.Item1)                 //是否继续等待
                        {
                            //继续等待
                            bool b = autoResetEvent.WaitOne(nextWaitOneIntervalsInMilliseconds, true);
                            if (b)
                            {
                                // 有信号
                                r = true;
                                break;
                            }
                            nextWaitOneIntervalsInMilliseconds += (waitOneIntervalsInMilliseconds * resendWaitOneIntervalsInMillisecondsFactor);
                        }
                        else
                        {
                            //不继续等待
                            continueWaiting = false;
                            if (!r)
                            {
                                r = tuple.Item2;       //可能的送达结果=false 或未知 因为不继续等待了
                            }
                            break;
                        }

                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (blockForResponse)
                {
                    if (stopWatch != null)
                    {
                        if (stopWatch.IsRunning)
                        {
                            stopWatch.Stop();
                        }
                        elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                    }
                    if (autoResetEvent != null)
                    {
                        autoResetEvent.Close();
#if NET45
                        autoResetEvent.Dispose();
#endif
                        autoResetEvent = null;
                        //autoResetEventWaiter = null;
                    }
                }
                sendTimes = i;
            }
            return r;
        }
        public int SendDataSyncWithRetry
                (
                    byte[] data
                    , int retry = 3
                    , int sleepSeconds = 1
                )
        {
            //增加就地重试机制
            int r = -1;
            int i = 0;

            while (i < retry)
            {
                r = -1;
                //lock (_sendSyncLockObject)
                {
                    try
                    {
                        if (_socket != null)
                        {
                            r = SendDataSync(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                i++;
                if (r > 0 || i == retry)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(sleepSeconds * 1000);
                }
            }
            return r;
        }
        public int SendDataToSync
                        (
                            byte[] data
                            , EndPoint remoteEndPoint
            //, Action onBeforeSendProcessAction = null
            //, Action onAfterSendedProcessAction = null
                            , int sleepMilliseconds = 0
                        )
        {
            var r = -1;
            if (_isUdp)
            {
                //if (onBeforeSendProcessAction != null)
                //{
                //    onBeforeSendProcessAction();
                //}
                //lock (_sendStaticSyncLockObject)
                {

                    r = _socket.SendTo(data, remoteEndPoint);
                }
                if (sleepMilliseconds > 0)
                {
                    Thread.Sleep(sleepMilliseconds);
                }

                //if (onAfterSendedProcessAction != null)
                //{
                //    onAfterSendedProcessAction();
                //}

            }
            return r;
        }

    }
}

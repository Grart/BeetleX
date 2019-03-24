using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Buffers
{
    public class SocketAsyncEventArgsX : SocketAsyncEventArgs
    {
        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);//to call Completed
			if (e.SocketError != SocketError.Success)
            {
                LastSocket = null;
            }
        }

        public IBuffer BufferX
        {
            get;
            internal set;
        }

        public bool IsReceive
        {
            get;
            set;
        }

        public ISession Session { get; internal set; }

        [ThreadStatic]
        private static System.Net.Sockets.Socket LastSocket;

        [ThreadStatic]
        private static int LoopCount;

        public void InvokeCompleted()
        {
            OnCompleted(this);
        }

        public void AsyncFrom(System.Net.Sockets.Socket socket, object useToken, int size)
        {
            this.IsReceive = true;
            this.UserToken = useToken;
#if (NETSTANDARD2_0)
            this.SetBuffer(BufferX.Data, 0, size);
#else
            this.SetBuffer(BufferX.Memory);
#endif
            var lastSocket = LastSocket;
            LastSocket = socket;
            if (!socket.ReceiveAsync(this))///to call => base.Completed <see cref="SocketAsyncEventArgs.Completed"/> 
			{
				if (lastSocket == socket)
                {
                    LoopCount++;
                }
                else
                {
                    LoopCount = 0;
                }
                if (LoopCount > 50)
                {
                    LoopCount = 0;
                    Task.Run(() => { this.InvokeCompleted(); });
                }
                else
                {
                    this.InvokeCompleted();
                }
            }
            else
            {
                LastSocket = null;
                LoopCount = 0;
            }
        }

        public void AsyncFrom(ISession session, object useToken, int size)
        {
            this.Session = session;
            AsyncFrom(session.Socket, useToken, size);
        }

        public void AsyncTo(System.Net.Sockets.Socket socket, object userToken, int length)
        {
            this.IsReceive = false;
            this.UserToken = userToken;
#if (NETSTANDARD2_0)
            this.SetBuffer(BufferX.Data, 0, length);
#else
            this.SetBuffer(BufferX.Memory.Slice(0, length));
#endif
            var lastSocket = LastSocket;
            LastSocket = socket;
			/*https://docs.microsoft.com/zh-cn/dotnet/api/system.net.sockets.socket.sendasync?view=netframework-4.7.2
			 public bool SendAsync (System.Net.Sockets.SocketAsyncEventArgs e);
			参数
			e
			SocketAsyncEventArgs
			要用于此异步套接字操作的 SocketAsyncEventArgs 对象。
			返回
			Boolean
			如果 I/O 操作挂起，则为 true。 操作完成时，将引发 e 参数的 Completed 事件。

			如果 I/O 操作同步完成，则为 false。 在这种情况下，将不会引发 e 参数的 Completed 事件，
			并且可能在方法调用返回后立即检查作为参数传递的 e 对象以检索操作的结果。
			*/
			if (!socket.SendAsync(this))
            {
                if (lastSocket == socket)
                {
                    LoopCount++;
                }
                else
                {
                    LoopCount = 0;
                }
                if (LoopCount > 50)
                {
                    LoopCount = 0;
                    Task.Run(() => { this.InvokeCompleted(); });
                }
                else
                {
                    this.InvokeCompleted();
                }
            }
            else
            {
                LastSocket = null;
                LoopCount = 0;
            }
        }

        public void AsyncTo(ISession session, object userToken, int length)
        {
            this.Session = session;
            AsyncTo(Session.Socket, userToken, length);
        }
    }
}

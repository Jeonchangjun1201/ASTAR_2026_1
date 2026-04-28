using System.Net.Sockets;

namespace KSY.Client {
    public class Connector {
        private Socket _socket;

        public void Connect() {
            var args = new SocketAsyncEventArgs();
            _connect(args);
        }

        private void _connect(SocketAsyncEventArgs args) {
            bool pending = _socket.ConnectAsync(args);
            if(!pending) {
                _onConnectedComplete(args);
            }
        }
        private void _onConnectedComplete(SocketAsyncEventArgs args) {
            bool isSuccess = !(args.SocketError == SocketError.SocketError);
            if(isSuccess)
            {

            }
            else
            {
                // 오류 처리 
            }
        }
    }
}


using System.Net.Sockets;
using System.Net;

TcpListener server = new TcpListener(IPAddress.Any, порт);
server.Start();

while (true)
{
    TcpClient client = server.AcceptTcpClient(); // Принимаем подключение от клиента

    // Читаем данные от клиента
    StreamReader reader = new StreamReader(client.GetStream());
    string data = reader.ReadToEnd();
    client.Close();

    // Сохраняем данные в текстовый файл
    string fileName = $"данные_{DateTime.Now:yyyyMMddHHmmss}.txt";
    File.WriteAllText(fileName, data);
}


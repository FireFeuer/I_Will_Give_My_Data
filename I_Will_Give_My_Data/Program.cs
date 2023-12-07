using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Win32;
using WUApiLib;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Net.NetworkInformation;





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

// Сохраняем время
string time = DateTime.Now.ToString("dd.MM.yyyy HH.mm");



// Создаем словарь для отправки наших данных
Dictionary<string, string> data = new Dictionary<string, string>()
{
    { "os", os },
    { "IsWindowsUpdateNeeded", isWindowsUpdateNeeded },
    { "driveInfo", driveInfo },
    { "name", machineName },
    { "time", time}
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



    // Здесь данные должны отправляться на сервер, если в json-файле для временного хранения данных есть данные, данные из json-файла так же отправятся, а сам json-файл - очистится.

    StreamWriter writer = new StreamWriter(client.GetStream());


    // Вот здесь с временного json хранилища данные будут отправляться 
    string json = File.ReadAllText("data.json");
    Json_objects json_objects = JsonConvert.DeserializeObject<Json_objects>(json);

    foreach(var item in json_objects.Items)
    {
        Dictionary<string, string> data_json = new Dictionary<string, string>()
        {
            { "os", item.os },
            { "IsWindowsUpdateNeeded", item.isWindowsUpdateNeeded },
            { "driveInfo", item.driveInfo },
            { "name", item.machineName },
            { "time", item.time}
        };
        writer.WriteLine(System.Text.Json.JsonSerializer.Serialize(data_json, options));
    }
    File.WriteAllText("data.json", "");

    //Здесь уже отправляются данные текущей сессии
    writer.WriteLine(System.Text.Json.JsonSerializer.Serialize(data, options));
    writer.Flush();
    client.Close();
}
catch
{
    // Сохраняем данные, которые не смогли отправиться из-за неработающего сервера, до следующего запуска программы с работающим сервером 

    string json = "";
    string dataJson = "";
    if (File.Exists("data.json"))
    {
        if (File.ReadAllText("data.json") != "")
        {
            dataJson = "," + File.ReadAllText("data.json");
            dataJson = dataJson.Remove(1, 1);
            json = JsonConvert.SerializeObject(data, Formatting.Indented) + dataJson;
        }
    }
    else
    {
        json = JsonConvert.SerializeObject(data, Formatting.Indented) + "\n]";
    }
    json = "[\n" + json;
    Console.WriteLine(json);// просто посмотреть, потом удалю 
    File.WriteAllText("data.json", json);


    Console.WriteLine("Программа не смогла подключиться к серверу");

}





class Data_being_sent
{
    public string os { get; set; }
    public string isWindowsUpdateNeeded { get; set; }

    public string driveInfo { get; set; }

    public string machineName { get; set; }

    public string time { get; set; }
}

class Json_objects
{
    public List<Data_being_sent> Items { get; set; }
}


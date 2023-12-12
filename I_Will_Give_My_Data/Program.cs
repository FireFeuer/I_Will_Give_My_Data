using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Unicode;
using WUApiLib;


class Program
{
    public static async Task Main(string[] args)
    {
        // Код на получение информации о необходимости обновления Windows 

        static bool IsWindowsUpdateNeeded()
        {
            UpdateSession updateSession = new UpdateSession();
            IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
            ISearchResult searchResult = updateSearcher.Search("IsInstalled=0");

            return searchResult.Updates.Count > 0;
        }

        string isWindowsUpdateNeeded = "";
        if (IsWindowsUpdateNeeded() == true)
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
        int i = 0;
        int b = 0;
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);

            if (subkey.GetValueNames().Contains("DisplayName"))
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

        // Получаем события связанные с системой.

        EventLog eventLogSystem = new EventLog("System");

        // Получаем события связанные с приложениями.

        EventLog eventLogApplication = new EventLog("Application");

        // Создаем переменные для хранения нужных нам событий

        string eventsSystem = "";

        string eventsApplication = "";


        foreach (EventLogEntry entry in eventLogSystem.Entries)
        {
            // Поучаем только ошибки и критические ошибки
            if (entry.EntryType == EventLogEntryType.Error || entry.EntryType == EventLogEntryType.FailureAudit)
            {
                eventsSystem += entry.EventID.ToString() + "\n" + entry.Source + "\n" + entry.TimeGenerated.ToString() + "\n" + entry.Message + "\n\n";
            }
        }

        foreach (EventLogEntry entry in eventLogApplication.Entries)
        {
            // Поучаем только ошибки и критические ошибки
            if (entry.EntryType == EventLogEntryType.Error || entry.EntryType == EventLogEntryType.FailureAudit)
            {
                eventsApplication += entry.EventID.ToString() + "\n" + entry.Source + "\n" + entry.TimeGenerated.ToString() + "\n" + entry.Message + "\n\n";
            }
        }




        // код конфигурационного файла config.env

        string path = Directory.GetCurrentDirectory();
        string pathToEnvFile = Path.Combine(path, "config.env");
        if (!File.Exists(pathToEnvFile))
        {
            await File.WriteAllTextAsync(pathToEnvFile, $"IP=\r\nPORT=3333");
        }

        string ip = "";
        int port = 0;

        using (var streamReader = new StreamReader(pathToEnvFile))
        {
            string fileContents = await streamReader.ReadToEndAsync();
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

         if (ip == "")
        {
            Console.Write("IP пуст. Введите IP сервера. Вы можете вписать IP сервера в созданный файл config.env, чтобы больше не видеть это сообщение:  ");
            ip = Console.ReadLine();            
        }

        // Сохраняем время
        string time = DateTime.Now.ToString("dd.MM.yyyy HH.mm");


        // Создаем словарь для отправки наших данных
        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { "os", os },
            { "IsWindowsUpdateNeeded", isWindowsUpdateNeeded },
            { "driveInfo", driveInfo },
            { "name", machineName },
            { "time", time},
            { "programNames", displayName },
            { "eventsSystem", eventsSystem },
            { "eventsApplication", eventsApplication }
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

            string json_from_jsonFile = "";
            string dataJson = "";
            if (File.Exists("data.json"))
            {
                if (await File.ReadAllTextAsync("data.json") != "")
                {
                    dataJson = "," + File.ReadAllText("data.json");
                    dataJson = dataJson.Remove(1, 1);
                    json_from_jsonFile = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + dataJson;
                    await File.WriteAllTextAsync("data.json", json_from_jsonFile);
                }
                else
                {
                    dataJson = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + "\n]";
                    await File.WriteAllTextAsync("data.json", dataJson);
                }
            }
            else
            {
                json_from_jsonFile = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + "\n]";
                await File.WriteAllTextAsync("data.json", json_from_jsonFile);
            }



            // Вот здесь с временного json хранилища данные будут отправляться 

            string json = await File.ReadAllTextAsync("data.json");
            List<Data_being_sent> json_objects = JsonConvert.DeserializeObject<List<Data_being_sent>>(json);

            Dictionary<string, string> data_json = null;
            await writer.WriteLineAsync("[");
            foreach (var item in json_objects)
            {
                data_json = new Dictionary<string, string>()
                    {
                        { "os", item.os },
                        { "IsWindowsUpdateNeeded", item.isWindowsUpdateNeeded },
                        { "driveInfo", item.driveInfo },
                        { "name", item.name },
                        { "time", item.time},
                        { "programNames", item.programNames },
                        { "eventsSystem", item.eventsSystem },
                        { "eventsApplication", item.eventsApplication}
                    };
                await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(data_json, options));
                await writer.WriteLineAsync(",");
            }
            await writer.WriteLineAsync("]");
            await File.WriteAllTextAsync("data.json", "");
            //Здесь уже отправляются данные текущей сессии
            writer.Flush();
            client.Close();
        }
        catch
        {
            // сохраняем данные, которые не смогли отправиться из-за неработающего сервера, до следующего запуска программы с работающим сервером 

  
            
            string json = "";
            string datajson = "";
            if (File.Exists("data.json"))
            {
                if (await File.ReadAllTextAsync("data.json") != "")
                {
                    datajson = "," + await File.ReadAllTextAsync("data.json");
                    datajson = datajson.Remove(1, 1);
                    json = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + datajson;
                    await File.WriteAllTextAsync("data.json", json);
                }
                else
                {
                    json = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + "\n]";
                    await File.WriteAllTextAsync("data.json", json);
                }
            }
            else
            {
                json = "[\n" + JsonConvert.SerializeObject(data, Formatting.Indented) + "\n]";
                await File.WriteAllTextAsync("data.json", json);
            }
            Console.WriteLine("программа не смогла подключиться к серверу");

        }

    }
}

class Data_being_sent
{
    public string os { get; set; }
    public string isWindowsUpdateNeeded { get; set; }
    public string driveInfo { get; set; }
    public string name { get; set; }
    public string time { get; set; }
    public string programNames { get; set; }
    public string eventsSystem { get; set; }
    public string eventsApplication { get; set; }
}





using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Unicode;
using WUApiLib;
using System.Management;
using LibreHardwareMonitor.Hardware;


class Program
{
    public static async Task Main(string[] args)
    {
        // Код на получение информации о необходимости обновления Windows 

        string isWindowsUpdateNeeded = "";
        if (IsWindowsUpdateNeeded() == true)
        {
            isWindowsUpdateNeeded = "Windows необходимо обновить";
        }
        else
        {
            isWindowsUpdateNeeded = "Нет необходимости обновлять Windows";
        }

        // Код на получение температуры проца (норм библиотек для новых процов нет, и часто для многих новых процов (5 и менее лет) он будет показывать 0 градусов, тут уже ничего не поделаешь
        string cpuTemp = await GetCPUTemperature();
        Console.WriteLine(cpuTemp);
        // Код на получение программ
        string applicationsList = await GetApplicationsList();
        string drivers = await GetDriversList();
        string os = Environment.OSVersion.ToString();
        // Получение названия ПК
        string machineName = Environment.MachineName;

        // Получаем события связанные с системой.
        string eventsSystem = await GetEventLog("System");
        // Получаем события связанные с приложениями.
        string eventsApplication = await GetEventLog("Application");


        // код конфигурационного файла config.env
        string configFilePath = await CreateConfigFile();
        Dictionary<string, string> network = await ReadConfigFile(configFilePath);

        string ip = "";
        int port = 0;

        ip = network["ip"];
        try
        {
            port = int.Parse(network["port"]);
        }
        catch
        {
            Console.WriteLine($"Используемый порт ({network["port"]}) имеет не верный формат, пожалуйста изменить его в файле config.env и перезапустите программу");
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
            { "CPUTemp", cpuTemp + "(Температура процессора)" },
            { "driveInfo", drivers },
            { "name", machineName },
            { "time", time},
            { "programNames", applicationsList },
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
            TcpClient client = null;
            try
            {
                client = new TcpClient(ip, port); // подключаемся к серверу и создаём клиента 
            }
            catch
            {
                client = new TcpClient(ip, port); // подключаемся к серверу и создаём клиента 
            }
            // Здесь данные должны отправляться на сервер, если в json-файле для временного хранения данных есть данные, данные из json-файла так же отправятся, а сам json-файл - очистится.
            StreamWriter writer = new StreamWriter(client.GetStream());
            await CreateDataJsonFile(data);
            // Вот здесь с временного json хранилища данные будут отправляться 
            await SendDataToServer(writer, client, options);
        }
        catch
        {
            // сохраняем данные, которые не смогли отправиться из-за неработающего сервера, до следующего запуска программы с работающим сервером         
            await CreateDataJsonFile(data);
        }
    }

    static bool IsWindowsUpdateNeeded()
    {
        UpdateSession updateSession = new UpdateSession();
        IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
        ISearchResult searchResult = updateSearcher.Search("IsInstalled=0");

        return searchResult.Updates.Count > 0;
    }

    private static async Task<string> GetApplicationsList()
    {
        string applicationsList = "";
        RegistryKey key;
        int i = 0;
        int b = 0;
        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        foreach (String keyName in key.GetSubKeyNames())
        {
            RegistryKey subkey = key.OpenSubKey(keyName);

            if (subkey.GetValueNames().Contains("DisplayName"))
                applicationsList += subkey.GetValue("DisplayName") as string + "\n";
        }
        return applicationsList;
    }

    private static async Task<string> GetDriversList()
    {
        string driversList = "";
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                double AvailableFreeSpace_Gb = Convert.ToDouble(((drive.AvailableFreeSpace / Convert.ToDouble(1024)) / Convert.ToDouble(1024)) / Convert.ToDouble(1024));
                AvailableFreeSpace_Gb = Math.Round(AvailableFreeSpace_Gb, 2);
                driversList += $"Диск {drive.Name}: Свободное место - {AvailableFreeSpace_Gb} Gb\n";
            }
        }
        return driversList;
    }

    private static async Task<string> GetEventLog(string logType)
    {
        EventLog eventLog = new EventLog(logType);

        // Создаем переменные для хранения нужных нам событий

        string events = "";

        foreach (EventLogEntry entry in eventLog.Entries)
        {
            // Поучаем только ошибки и критические ошибки
            if ((entry.EntryType == EventLogEntryType.Error || entry.EntryType == EventLogEntryType.FailureAudit) && entry.TimeGenerated > DateTime.Today)
            {
                events += entry.EventID.ToString() + "\n" + entry.Source + "\n" + entry.TimeGenerated.ToString() + "\n" + entry.Message + "\n\n";
            }
        }
        return events;
    }

    private async static Task<string> CreateConfigFile()
    {
        string path = Directory.GetCurrentDirectory();
        string pathToEnvFile = Path.Combine(path, "config.env");
        if (!File.Exists(pathToEnvFile))
        {
            await File.WriteAllTextAsync(pathToEnvFile, $"IP=\r\nPORT=3333");
        }
        return pathToEnvFile;
    }

    private async static Task<Dictionary<string, string>> ReadConfigFile(string configFilePath)
    {
        using (var streamReader = new StreamReader(configFilePath))
        {
            string fileContents = await streamReader.ReadToEndAsync();
            string[] lines = fileContents.Split('\n');
            Dictionary<string, string> network = new Dictionary<string, string>(){
                { "ip", "-" },
                {"port","-" }
            };

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
                            network["ip"] = value;
                            break;
                        case "PORT":
                            network["port"] = value;
                            break;
                    }
                }

            }
            return network;
        }
    }

    private async static Task CreateDataJsonFile(Dictionary<string, string> data)
    {
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
    }

    private async static Task SendDataToServer(StreamWriter writer, TcpClient client, JsonSerializerOptions options)
    {
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
                        { "CPUTemp", item.CPUTemp},
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

    private static async Task<string> GetCPUTemperature()
    {
        UpdateVisitor updateVisitor = new UpdateVisitor();
        Computer computer = new Computer();
        computer.Open();
        computer.IsCpuEnabled = true;
        computer.Accept(updateVisitor);
        string cpuTemp = " ...ТЕМПЕРАТУРА ПРОЦЕССОРА НЕ НАЙДЕНА... ";
        for (int i = 0; i < computer.Hardware.Count; i++)
        {
            if (computer.Hardware[i].HardwareType == HardwareType.Cpu)
            {
                for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                {
                    if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                    {
                        cpuTemp = (computer.Hardware[i].Sensors[j].Name + ":" + computer.Hardware[i].Sensors[j].Value.ToString() + "\r");                    
                    }

                }
            }
        }
        computer.Close();
        return cpuTemp;
    }
}


public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}

class Data_being_sent
{
    public string os { get; set; }
    public string isWindowsUpdateNeeded { get; set; }
    public string CPUTemp { get; set;  }
    public string driveInfo { get; set; }
    public string name { get; set; }
    public string time { get; set; }
    public string programNames { get; set; }
    public string eventsSystem { get; set; }
    public string eventsApplication { get; set; }
}





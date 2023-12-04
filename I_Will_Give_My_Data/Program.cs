using System.Net.Sockets;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Win32;
using System.Net;
using WUApiLib; // Эта штука как бы есть, но она не работает и UpdateSession, IUpdateSearcher и ISearchResult попросту не находит. Где брать WUApiLib я без понятия 

//using Microsoft.UpdateServices.Administration;
//using Microsoft.Update.Clients;


// Код на получение информации о необходимости обновления Windows 

/*

static void IsWindowsUpdateNeeded2() // Версия 1 
    {
        UpdateSession updateSession = new UpdateSession();
        IUpdateSearcher searcher = updateSession.CreateUpdateSearcher();
        searcher.Online = true;
        ISearchResult result = searcher.Search("isInstalled=0 and type='Software'");

        if (result.Updates.Count() > 0)
        {
            foreach (IUpdate update in result.Updates)
            {
                Console.WriteLine($"{update.Identity.SecurityClassification}: {update.Identity.Title}");
            }
        }
        else
        {
            Console.WriteLine("No updates found.");
        }
    }


static bool IsWindowsUpdateNeeded() // Версия 2 
{

    UpdateSession updateSession = new UpdateSession();
    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();

    ISearchResult searchResult = updateSearcher.Search("IsInstalled=0");

    return searchResult.Updates.Count > 0;
}

*/



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


// Получение названия ПК
string machineName = Environment.MachineName;

//string data = $"Операционная система: {os}\nСвободное место на диске:\n{driveInfo}\n\nСписок программ:\n{displayName}"; // подготавливаем наши данные в переменную

// Создаем словарь для отправки наших данных
Dictionary<string, string> data = new Dictionary<string, string>()
{
    { "os", os },
    { "driveInfo", driveInfo },
    { "name", machineName }
};

// Здесь создаем настройки для сериализации данных
var options = new JsonSerializerOptions
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
    WriteIndented = true
};

TcpClient client = new TcpClient("26.194.255.228", 3333); // подключаемся к серверу и создаём клиента 

// Здесь данные должны отправляться на сервер 

StreamWriter writer = new StreamWriter(client.GetStream());
writer.WriteLine(JsonSerializer.Serialize(data, options));
writer.Flush();
client.Close();






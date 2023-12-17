using System;
using System.Collections.Generic;
using System.Management;

namespace I_Will_Give_My_Data.Classes
{
    public class DriveInformation
    {
        Dictionary<int, Drive> dicDrives = new Dictionary<int, Drive>();

        public DriveInformation() 
        {
            InitDrives();
            GetSerialDrive();
            GetPredictFailure();
            GetFlags();
            GetThreshold();
        }

        public Dictionary<int, Drive> GetDictDrive()
        {
            return dicDrives;
        }

        private void InitDrives()
        {
            var wdSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            int iDriveIndex = 0;

            // Извлечь информацию о модели и интерфейсе.

            foreach (ManagementObject driveMO in wdSearcher.Get())
            {
                var drive = new Drive();
                drive.Model = driveMO["Model"].ToString().Trim();
                drive.Type = driveMO["InterfaceType"].ToString().Trim();
                dicDrives.Add(iDriveIndex, drive);
                iDriveIndex++;
            }
        }

        private void GetSerialDrive()
        {
            var pmsearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");

            // Получить серийный номер жесткого диска
            int iDriveIndex = 0;
            foreach (ManagementObject drive in pmsearcher.Get())
            {
                // поскольку все физические носители будут возвращены, нам нужно выйти
                // после извлечения серийной информации жесткого диска
                if (iDriveIndex >= dicDrives.Count)
                    break;

                dicDrives[iDriveIndex].Serial = drive["SerialNumber"] == null ? "None" : drive["SerialNumber"].ToString().Trim();
                iDriveIndex++;
            }
        }

        private void GetPredictFailure()
        {
            // получить доступ к жесткому диску через wmi 
            var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive");
            searcher.Scope = new ManagementScope(@"\root\wmi");

            // проверьте, сообщает ли SMART, что диск неисправен
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictStatus");
            int iDriveIndex = 0;
            foreach (ManagementObject drive in searcher.Get())
            {
                dicDrives[iDriveIndex].IsOK = (bool)drive.Properties["PredictFailure"].Value == false;
                iDriveIndex++;
            }
        }

        private void GetFlags()
        {
            var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive");
            searcher.Scope = new ManagementScope(@"\root\wmi");

            // получить флаги атрибутов, значение худшего значения и информацию о поставщике
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictData");
            int iDriveIndex = 0;
            foreach (ManagementObject data in searcher.Get())
            {
                Byte[] bytes = (Byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 30; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];

                        int flags = bytes[i * 12 + 4]; // младший байт состояния, +3 наиболее значащего байта, но не используется, поэтому игнорируется.
                        //bool advisory = (flags & 0x1) == 0x0;
                        bool failureImminent = (flags & 0x1) == 0x1;
                        //bool onlineDataCollection = (flags & 0x2) == 0x2;

                        int value = bytes[i * 12 + 5];
                        int worst = bytes[i * 12 + 6];
                        int vendordata = BitConverter.ToInt32(bytes, i * 12 + 7);
                        if (id == 0) continue;

                        var attr = dicDrives[iDriveIndex].Attributes[id];
                        attr.Current = value;
                        attr.Worst = worst;
                        attr.Data = vendordata;
                        attr.IsOK = failureImminent == false;
                    }
                    catch
                    {
                        // данный ключ не существует в коллекции атрибутов (атрибут отсутствует в словаре атрибутов
                    }
                }
                iDriveIndex++;
            }
        }

        private void GetThreshold()
        {
            var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive");
            searcher.Scope = new ManagementScope(@"\root\wmi");

            // получить пороговые значения для каждого атрибута
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictThresholds");
            int iDriveIndex = 0;
            foreach (ManagementObject data in searcher.Get())
            {
                Byte[] bytes = (Byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 30; ++i)
                {
                    try
                    {

                        int id = bytes[i * 12 + 2];
                        int thresh = bytes[i * 12 + 3];
                        if (id == 0) continue;

                        var attr = dicDrives[iDriveIndex].Attributes[id];
                        attr.Threshold = thresh;
                    }
                    catch
                    {
                        // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                    }
                }

                iDriveIndex++;
            }
        }




    }
}

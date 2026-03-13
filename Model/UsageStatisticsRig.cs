using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NORCE.Drilling.Rig.Model
{
    public struct CountPerDay
    {
        public DateTime Date { get; set; }
        public ulong Count { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public CountPerDay() { }
        /// <summary>
        /// initialization constructor
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        public CountPerDay(DateTime date, ulong count)
        {
            Date = date;
            Count = count;
        }
    }

    public class History
    {
        public List<CountPerDay> Data { get; set; } = new List<CountPerDay>();
        /// <summary>
        /// default constructor
        /// </summary>
        public History()
        {
            if (Data == null)
            {
                Data = new List<CountPerDay>();
            }
        }

        public void Increment()
        {
            if (Data.Count == 0)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else
            {
                if (Data[Data.Count - 1].Date < DateTime.UtcNow.Date)
                {
                    Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
                }
                else
                {
                    Data[Data.Count - 1] = new CountPerDay(Data[Data.Count - 1].Date, Data[Data.Count - 1].Count + 1);
                }
            }
        }
    }
    public class UsageStatisticsRig
    {
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;

        public DateTime LastSaved { get; set; } = DateTime.MinValue;
        public TimeSpan BackUpInterval { get; set; } = TimeSpan.FromMinutes(5);

        public History GetAllRigIdPerDay { get; set; } = new History();
        public History GetAllRigMetaInfoPerDay { get; set; } = new History();
        public History GetRigByIdPerDay { get; set; } = new History();
        public History GetAllRigLightPerDay { get; set; } = new History();
        public History GetAllRigPerDay { get; set; } = new History();
        public History PostRigPerDay { get; set; } = new History();
        public History PutRigByIdPerDay { get; set; } = new History();
        public History DeleteRigByIdPerDay { get; set; } = new History();

        private static object lock_ = new object();

        private static UsageStatisticsRig? instance_ = null;

        public static UsageStatisticsRig Instance
        {
            get
            {
                if (instance_ == null)
                {
                    if (File.Exists(HOME_DIRECTORY + "history.json"))
                    {
                        try
                        {
                            string? jsonStr = null;
                            lock (lock_)
                            {
                                using (StreamReader reader = new StreamReader(HOME_DIRECTORY + "history.json"))
                                {
                                    jsonStr = reader.ReadToEnd();
                                }
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    instance_ = JsonSerializer.Deserialize<UsageStatisticsRig>(jsonStr);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsRig();
                    }
                }
                return instance_;
            }
        }

        public void IncrementGetAllRigIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllRigIdPerDay == null)
                {
                    GetAllRigIdPerDay = new History();
                }
                GetAllRigIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllRigMetaInfoPerDay()
        {
            lock (lock_)
            {
                if (GetAllRigMetaInfoPerDay == null)
                {
                    GetAllRigMetaInfoPerDay = new History();
                }
                GetAllRigMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetRigByIdPerDay()
        {
            lock (lock_)
            {
                if (GetRigByIdPerDay == null)
                {
                    GetRigByIdPerDay = new History();
                }
                GetRigByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPostRigPerDay()
        {
            lock (lock_)
            {
                if (PostRigPerDay == null)
                {
                    PostRigPerDay = new History();
                }
                PostRigPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllRigLightPerDay()
        {
            lock (lock_)
            {
                if (GetAllRigLightPerDay == null)
                {
                    GetAllRigLightPerDay = new History();
                }
                GetAllRigLightPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllRigPerDay()
        {
            lock (lock_)
            {
                if (GetAllRigPerDay == null)
                {
                    GetAllRigPerDay = new History();
                }
                GetAllRigPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPutRigByIdPerDay()
        {
            lock (lock_)
            {
                if (PutRigByIdPerDay == null)
                {
                    PutRigByIdPerDay = new History();
                }
                PutRigByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementDeleteRigByIdPerDay()
        {
            lock (lock_)
            {
                if (DeleteRigByIdPerDay == null)
                {
                    DeleteRigByIdPerDay = new History();
                }
                DeleteRigByIdPerDay.Increment();
                ManageBackup();
            }
        }

        private void ManageBackup()
        {
            if (DateTime.UtcNow > LastSaved + BackUpInterval)
            {
                LastSaved = DateTime.UtcNow;
                try
                {
                    string jsonStr = JsonSerializer.Serialize(this);
                    if (!string.IsNullOrEmpty(jsonStr) && Directory.Exists(HOME_DIRECTORY))
                    {
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlayLogger
{
    public static class DbHandler
    {
        private static HashSet<string> s_fields;
        public static HashSet<string> SongFields
        {
            get
            {
                if (s_fields == null)
                {
                    var userSettings = Convert.ToString(UserSettings.Get("SongFieldsToSave"));
                    var config = Config.Instance.Get("SongFieldsToSave");
                    string csvFields = string.IsNullOrWhiteSpace(userSettings) ? config : userSettings;
                    UserSettings.Set("SongFieldsToSave", csvFields);
                    s_fields = new HashSet<string>(csvFields.Split(','));
                }

                return s_fields;
            }
        }

        public static void ResetSongFields()
        {
            s_fields = null;
        }

        private static string extraFiledsCSV()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string field in SongFields)
            {
                sb.AppendFormat(",`{0}`", field);
            }

            return sb.ToString();
        }

        public static object s_LockDb = new object();
        public static List<SongInfo> GetHistoryFromDb(DateTime? from = null)
        {
            List<SongInfo> history = null;
            using (var dbCon = MyDbConnectionBase.CreateInstace())
            {
                if (!dbCon.IsConnect())
                {
                    return null;
                }

                try
                {
                    history = new List<SongInfo>();
                    string where = from == null ? null : $" WHERE LastPlayTime > '{from.Value.ToString("u")}'";
                    string query = $"SELECT RecordId,Id,Title,LastPlayTime,PlayLocation FROM playhistory{where}";

                    lock (s_LockDb)
                    {
                        using (var cmd = dbCon.CreateCmd(query))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SongInfo song = new SongInfo()
                                {
                                    RecordId = reader.GetInt64(0),
                                    Id = reader.GetInt32(1),
                                    Title = reader.SafeGetString(2),
                                    PlayTime = reader.GetDateTime(3),
                                    PlayLocation = reader.SafeGetString(4)
                                };

                                if (DbHandler.SongFields.Contains("ID"))
                                    song.Fields["ID"] = song.Id.ToString();

                                history.Add(song);
                            }
                        }

                        history.ReadExtraFieldsFromDb();
                    }

                }
                catch (MySqlException ex)
                {
                    MainViewModel.LogException(ex);
                }

            }

            return history;
        }

        public static void ReadExtraFieldsFromDb(this IEnumerable<SongInfo> songs)
        {
            using (var dbCon = MyDbConnectionBase.CreateInstace())
            {
                if (!dbCon.IsConnect())
                {
                    return;
                }

                string query = string.Format("SELECT RecordId, FieldName, FieldValue FROM FieldData where RecordId in ({0})", string.Join(",", (from song in songs select song.RecordId)));
                using (var cmd = dbCon.CreateCmd(query))
                using (var reader = cmd.ExecuteReader())
                {
                    Dictionary<long, List<Tuple<string, string>>> fieldData = new Dictionary<long, List<Tuple<string, string>>>();
                    while (reader.Read())
                    {
                        long recordId = reader.GetInt64(0);
                        string fieldName = reader.SafeGetString(1);
                        string value = reader.SafeGetString(2);
                        if (!fieldData.ContainsKey(recordId))
                        {
                            fieldData.Add(recordId, new List<Tuple<string, string>>() { new Tuple<string, string>(fieldName, value) });
                        }
                        else
                        {
                            fieldData[recordId].Add(new Tuple<string, string>(fieldName, value));
                        }
                    }

                    foreach (var song in songs)
                    {
                        if (fieldData.ContainsKey(song.RecordId))
                        {
                            var songFields = fieldData[song.RecordId];
                            foreach (var fData in songFields)
                            {
                                string fieldName = fData.Item1;
                                string value = fData.Item2;

                                if (!fieldName.Equals("ID",StringComparison.OrdinalIgnoreCase) && SongFields.Contains(fieldName))
                                {
                                    song.Fields[fieldName] = value;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void LogSongInfo(PlayHistorySettings args)
        {
            if (args == null)
            {
                return;
            }
            try
            {
                using (var dbCon = MyDbConnectionBase.CreateInstace())
                {
                    if (!dbCon.IsConnect())
                    {
                        return;
                    }

                    using (var cmdSong = dbCon.CreateCmd())
                    {
                        cmdSong.CommandText = "INSERT INTO playhistory (Id,Title,LastPlayTime,PlayLocation) VALUES (@id,@title,@lastPlay,@playLoc); SELECT currval('playhistory_recordid_seq');";
                        using (var cmdFields = dbCon.CreateCmd("INSERT INTO FieldData (RecordId,FieldName,FieldValue) VALUES (@rcdId,@fName,@fValue)"))
                        {
                            //cmdFields.Prepare();

                            IEnumerable<SongInfo> songs = readSongInfo(args);
                            foreach (var song in songs)
                            {
                                if (!isSongInfoExistsInDb(song, args.PlayLocation))
                                {
                                    cmdSong.Parameters.Clear();
                                    cmdSong.Parameters.Add(dbCon.CreateParam("@id", song.Id));
                                    cmdSong.Parameters.Add(dbCon.CreateParam("@title", song.Title));
                                    cmdSong.Parameters.Add(dbCon.CreateParam("@lastPlay", song.PlayTime));
                                    cmdSong.Parameters.Add(dbCon.CreateParam("@playLoc", args.PlayLocation));

                                    song.RecordId = Convert.ToInt64(cmdSong.ExecuteScalar());

                                    foreach (var item in song.Fields)
                                    {
                                        cmdFields.Parameters.Clear();
                                        cmdFields.Parameters.Add(dbCon.CreateParam("@rcdId", song.RecordId));
                                        cmdFields.Parameters.Add(dbCon.CreateParam("@fName", item.Key));
                                        cmdFields.Parameters.Add(dbCon.CreateParam("@fValue", item.Value ?? string.Empty));
                                        cmdFields.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }

        private static bool isSongInfoExistsInDb(SongInfo song, string i_PlayLocation)
        {
            using (var dbCon = MyDbConnectionBase.CreateInstace())
            {
                if (!dbCon.IsConnect())
                {
                    return false;
                }

                using (var cmd = dbCon.CreateCmd())
                {
                    cmd.CommandText = "Select RecordId from playhistory Where Id = @id AND LastPlayTime = @lastPlay AND PlayLocation = @playLoc;";

                    cmd.Parameters.Add(dbCon.CreateParam("@id", song.Id));
                    cmd.Parameters.Add(dbCon.CreateParam("@lastPlay", song.PlayTime));
                    cmd.Parameters.Add(dbCon.CreateParam("@playLoc", i_PlayLocation));

                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        private static IEnumerable<SongInfo> readSongInfo(PlayHistorySettings args)
        {
            List<SongInfo> res = null;
            try
            {

                IEnumerable<string> files = null;
                if (args.ReadFileListOnly)
                {
                    files = args.Files;
                }
                else
                {
                    if (System.IO.Directory.Exists(args.LastPlayedXmlDir))
                    {
                        files = System.IO.Directory.EnumerateFiles(args.LastPlayedXmlDir, "*.xml");
                    }
                }

                res = new List<SongInfo>();
                if (files != null)
                {
                    foreach (var filePath in files)
                    {
                        var song = SongInfo.ReadFromXml(filePath);
                        if (song != null)
                        {
                            res.Add(song);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
            return res;

        }

        public static string SafeGetString(this DbDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetString(colIndex);
            return string.Empty;
        }

        public static void RemoveSongsFromDb(IEnumerable<SongInfo> i_SongsToRemove)
        {
            try
            {
                if (i_SongsToRemove != null && i_SongsToRemove.Any())
                {
                    using (var dbCon = MyDbConnectionBase.CreateInstace())
                    {
                        if (!dbCon.IsConnect())
                        {
                            return;
                        }

                        string ids = string.Join(",", from song in i_SongsToRemove select song.RecordId.ToString());
                        using (var cmdDel = dbCon.CreateCmd())
                        {
                            cmdDel.CommandText = string.Format("Delete FROM playhistory WHERE RecordId in ({0}); Delete FROM FieldData WHERE RecordId in ({0});", ids);
                            cmdDel.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }

        public static async Task LogAppInfoAsync(PlayHistorySettings i_Settings)
        {
            await Task.Run(() => LogAppInfo(i_Settings));
        }

        public static void LogAppInfo(PlayHistorySettings i_Settings)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string version = assembly.GetName().Version.ToString();

                using (var dbCon = MyDbConnectionBase.CreateInstace())
                {
                    if (!dbCon.IsConnect())
                    {
                        return;
                    }
                    using (var cmd = dbCon.CreateCmd())
                    {
                        // CREATE TABLE clientInfo (PlayLoc TEXT,Version TEXT,LastRunDate TIMESTAMP);
                        // ALTER TABLE clientInfo ADD CONSTRAINT clietInfo_key UNIQUE (PlayLoc, Version);

                        cmd.CommandText = string.Format("Delete From clientInfo WHERE DATE_PART('day',Now() - LastRunDate) >= 7 Or (PlayLoc = @playLoc and Version = @version); Insert into clientInfo (PlayLoc,Version,LastRunDate) VALUES(@playLoc,@version,@runDate);");
                        cmd.Parameters.Add(dbCon.CreateParam("@playLoc", i_Settings.PlayLocation));
                        cmd.Parameters.Add(dbCon.CreateParam("@version", version));
                        cmd.Parameters.Add(dbCon.CreateParam("@runDate", DateTime.Now));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }

        public static void RemoveOldSongs()
        {
            try
            {
                using (var dbCon = MyDbConnectionBase.CreateInstace())
                {
                    if (!dbCon.IsConnect())
                    {
                        return;
                    }
                    using (var cmd = dbCon.CreateCmd())
                    {
                        // CREATE TABLE clientInfo (PlayLoc TEXT,Version TEXT,LastRunDate TIMESTAMP);
                        // ALTER TABLE clientInfo ADD CONSTRAINT clietInfo_key UNIQUE (PlayLoc, Version);
                        const int days = 7;
                        cmd.CommandText = $@"Delete From fielddata A USING playhistory B WHERE B.recordid = A.recordid AND DATE_PART('day',Now() - B.lastPlayTime) > {days};";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = $"Delete From playhistory WHERE DATE_PART('day',Now() - lastPlayTime) > {days};";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }
    }
}
